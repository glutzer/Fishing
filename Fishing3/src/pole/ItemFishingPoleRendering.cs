using OpenTK.Graphics.OpenGL4;
using OpenTK.Mathematics;
using System;
using System.Collections.Generic;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;
using Vintagestory.API.Util;
using Vintagestory.Client.NoObf;
using Vintagestory.GameContent;

namespace Fishing;

public partial class ItemFishingPole : Item, IRenderableItem
{
    /// <summary>
    /// Returns position of the fishing pole bobber.
    /// For gui.
    /// </summary>
    public static Vector3d GetSwayedPosition(EntityPlayer player, float distance)
    {
        ItemSlot slot = player.RightHandItemSlot;
        if (slot.Itemstack == null) return player.Pos.ToVector();

        AnimationUtility.GetRightHandPosition(player, new Vector3(0.5f - 4.5f, 0, 0.5f), out Vector3d pos);

        float xSway = slot.Itemstack.Attributes.GetFloat("xSway", 0);
        float zSway = slot.Itemstack.Attributes.GetFloat("zSway", 0);

        Vector3d swayedPos = new(pos.X - (xSway * 0.75f), pos.Y - 1.5f, pos.Z - (zSway * 0.75f));
        Vector3 swayNormal = (Vector3)(swayedPos - pos);
        swayNormal.Normalize();

        return pos + (swayNormal * distance);
    }

    public bool OnItemRender(EntityShapeRenderer instance, float dt, bool isShadowPass, ItemStack stack, AttachmentPointAndPose apap, ItemRenderInfo renderInfo)
    {
        if (isShadowPass) return true;

        if (instance.entity is not EntityPlayer player) return true;

        if (!ReadStack(0, stack, api, out ItemStack? lineStack)) return true;

        AnimationUtility.GetRightHandPosition(player, new Vector3(0.5f - 4.5f, 0, 0.5f), out Vector3d pos);
        Vec3d camPos = MainAPI.Capi.World.Player.Entity.CameraPos;
        Vec3d offset = camPos - player.Pos.XYZ;

        if (!player.IsSelf())
        {
            pos.X += offset.X;
            pos.Y += offset.Y;
            pos.Z += offset.Z;
        }

        // Calc sway.
        float xSway = stack.Attributes.GetFloat("xSway", 0);
        float zSway = stack.Attributes.GetFloat("zSway", 0);

        double lastX = stack.Attributes.GetDouble("lastX", pos.X);
        double lastZ = stack.Attributes.GetDouble("lastZ", pos.Z);

        float deltaX = (float)(pos.X - lastX);
        float deltaZ = (float)(pos.Z - lastZ);

        xSway += deltaX / 2f;
        zSway += deltaZ / 2f;

        xSway = Math.Clamp(xSway, -1f, 1f);
        zSway = Math.Clamp(zSway, -1f, 1f);

        xSway = GameMath.Lerp(xSway, 0, dt * 4f);
        zSway = GameMath.Lerp(zSway, 0, dt * 4f);

        if (Math.Abs(xSway) < 0.01) xSway = 0;
        if (Math.Abs(zSway) < 0.01) zSway = 0;

        stack.Attributes.SetFloat("xSway", xSway);
        stack.Attributes.SetFloat("zSway", zSway);
        stack.Attributes.SetDouble("lastX", pos.X);
        stack.Attributes.SetDouble("lastZ", pos.Z);

        ShaderProgramBase? lastShader = ShaderProgramBase.CurrentShaderProgram;

        Vector3d swayedPos = new(pos.X - (xSway * 0.75f), pos.Y - 1.5f, pos.Z - (zSway * 0.75f));
        Vector3 swayNormal = (Vector3)(swayedPos - pos);
        swayNormal.Normalize();

        string lineCode = lineStack?.Collectible.Attributes["lineType"].AsString() ?? "none";
        Texture lineTex = ClientCache.GetOrCache(lineCode, () =>
        {
            return Texture.Create($"fishing:textures/lines/{lineCode}.png", false, true);
        });

        long entityId = stack.Attributes.GetLong("bobber", 0);

        if (MainAPI.Capi.World.GetEntityById(entityId) is not EntityBobber bobber)
        {
            ReadStack(1, stack, api, out ItemStack? bobberStack);
            ReadStack(2, stack, api, out ItemStack? baitStack);
            ReadStack(3, stack, api, out ItemStack? catchStack);

            if (bobberStack != null)
            {
                float length = 1f;
                if (baitStack != null || catchStack != null) length += 1.5f;

                // Draw the line.
                FishingLineRenderer.RenderLine(pos, pos + (swayNormal * length), 0f, lineTex);
            }

            MainAPI.Capi.Render.StandardShader.Use();

            if (bobberStack != null)
            {
                float bobberScale = bobberStack.Collectible.GetBehavior<CollectibleBehaviorBobber>()?.modelScale ?? 1f;

                RenderBobberItemOpaqueShader(dt, pos + (swayNormal * 1f), (ShaderProgramBase)MainAPI.Capi.Render.StandardShader, swayNormal, new Vector3(0.5f), Vector3.UnitY, bobberStack, bobberScale);
            }

            // Render catch.
            if (catchStack != null && bobberStack != null)
            {
                if (catchStack.Collectible is ItemFish fish && fish.GetSpecies(catchStack) is FishSpecies species)
                {
                    double weight = ItemFish.GetWeight(catchStack);

                    RenderBobberItemOpaqueShader(dt, pos + (swayNormal * 2f), (ShaderProgramBase)MainAPI.Capi.Render.StandardShader, swayNormal, species.mouthOffset, species.mouthFacing, catchStack, ItemFish.GetScale(weight));
                }
                else
                {
                    RenderBobberItemOpaqueShader(dt, pos + (swayNormal * 2f), (ShaderProgramBase)MainAPI.Capi.Render.StandardShader, swayNormal, new Vector3(0.5f), Vector3.UnitY, catchStack);
                }
            }
            else if (baitStack != null && bobberStack != null)
            {
                RenderBobberItemOpaqueShader(dt, pos + (swayNormal * 2f), (ShaderProgramBase)MainAPI.Capi.Render.StandardShader, swayNormal, new Vector3(0.5f), Vector3.UnitY, baitStack, 1f);
            }
        }
        else
        {
            float droop = 1f;
            Vector3d bobberPos = bobber.Pos.ToVector();

            if (bobber.behavior is BobberReelable bobberFishing)
            {
                float length = (float)Vector3d.Distance(bobberPos, pos);
                droop = (bobberFishing.bobber.WatchedAttributes.GetFloat("maxDistance") / length) - 1;
                droop *= 4;
                droop = Math.Clamp(droop, 0.5f, 5); // Minimum droop.
            }

            // Draw the line.
            FishingLineRenderer.RenderLine(pos, bobberPos, droop, lineTex);

            MainAPI.Capi.Render.StandardShader.Use();
            ReadStack(1, stack, api, out ItemStack? bobberStack);
            ReadStack(2, stack, api, out ItemStack? baitStack);

            // Render bobber.
            if (bobberStack != null)
            {
                float bobberScale = bobberStack.Collectible.GetBehavior<CollectibleBehaviorBobber>()?.modelScale ?? 1f;

                RenderBobberItemOpaqueShader(dt, bobberPos, (ShaderProgramBase)MainAPI.Capi.Render.StandardShader, -Vector3.UnitY, new Vector3(0.5f), Vector3.UnitY, bobberStack, bobberScale);
            }

            if (baitStack != null) // Render bait.
            {
                Vector3d baitPos = bobberPos;
                baitPos.Y -= 2;
                FishingLineRenderer.RenderLine(bobberPos, baitPos, 0f, lineTex);

                baitPos.Y += 1;

                RenderBobberItemOpaqueShader(dt, baitPos, (ShaderProgramBase)MainAPI.Capi.Render.StandardShader, -Vector3.UnitY, new Vector3(0.5f), Vector3.UnitY, baitStack, 1f);
            }
        }

        // Fix shadow sampler???
        MainAPI.Capi.Render.StandardShader.BindTexture2D("tex2dOverlay", 0, 1);
        MainAPI.Capi.Render.StandardShader.Uniform("tex2dOverlay", 0);
        MainAPI.Capi.Render.StandardShader.Stop();

        lastShader?.Use();

        return true;
    }

    /// <summary>
    /// Renders an item in opaque. Assumes the standard shader is active.
    /// </summary>
    public static void RenderBobberItemOpaqueShader(float dt, Vector3d position, ShaderProgramBase prog, Vector3 swayNormal, Vector3 origin, Vector3 up, ItemStack stackToRender, float scale = 0.5f)
    {
        Vector3d offset = MainAPI.CameraPosition - new Vector3d(MainAPI.Capi.World.Player.Entity.CameraPos.X, MainAPI.Capi.World.Player.Entity.CameraPos.Y, MainAPI.Capi.World.Player.Entity.CameraPos.Z);

        Quaternion rotation = QuaternionUtility.FromToRotation(up, -swayNormal);
        Matrix4 modelMatrix = Matrix4.CreateTranslation(-origin)
            * Matrix4.CreateScale(scale, scale, scale)
            * Matrix4.CreateFromQuaternion(rotation)
            * RenderTools.CameraRelativeTranslation(position + offset);

        DummySlot slot = new(stackToRender);

        ItemRenderInfo renderInfo = MainAPI.Capi.Render.GetItemStackRenderInfo(slot, EnumItemRenderTarget.HandTp, dt);

        byte[] hsv = stackToRender.Collectible.LightHsv;
        if (hsv[2] > 0)
        {
            MainAPI.EnqueueBeforeFrameTask(() =>
            {
                LightingUtilities.AddPointLight(hsv, position);
            });
        }

        prog.Uniform(modelMatrix, "modelMatrix");

        prog.Uniform("dontWarpVertices", 0);
        prog.Uniform("addRenderFlags", 0);
        prog.Uniform("normalShaded", 1);
        prog.Uniform("tempGlowMode", stackToRender.ItemAttributes?["tempGlowMode"].AsInt() ?? 0);
        prog.Uniform("rgbaTint", ColorUtil.WhiteArgbVec);
        prog.Uniform("alphaTest", renderInfo.AlphaTest);
        prog.Uniform("damageEffect", renderInfo.DamageEffect);

        prog.Uniform("overlayOpacity", renderInfo.OverlayOpacity);

        prog.BindTexture2D("tex2dOverlay", renderInfo.OverlayTexture?.TextureId ?? 0, 1);

        if (renderInfo.OverlayTexture != null && renderInfo.OverlayOpacity > 0)
        {
            prog.Uniform("overlayTextureSize", new Vec2f(renderInfo.OverlayTexture.Width, renderInfo.OverlayTexture.Height));
            prog.Uniform("baseTextureSize", new Vec2f(renderInfo.TextureSize.Width, renderInfo.TextureSize.Height));
            TextureAtlasPosition texPos = MainAPI.Capi.Render.GetTextureAtlasPosition(stackToRender);
            prog.Uniform("baseUvOrigin", new Vec2f(texPos.x1, texPos.y1));
        }

        Vec4f lightRGBs = MainAPI.Capi.World.BlockAccessor.GetLightRGBs((int)position.X, (int)position.Y, (int)position.Z);

        int temp = (int)stackToRender.Collectible.GetTemperature(MainAPI.Capi.World, stackToRender);
        float[] glowColor = ColorUtil.GetIncandescenceColorAsColor4f(temp);
        int gi = GameMath.Clamp((temp - 500) / 3, 0, 255);
        prog.Uniform("extraGlow", gi);
        prog.Uniform("rgbaAmbientIn", MainAPI.Capi.Render.AmbientColor);
        prog.Uniform("rgbaLightIn", lightRGBs);
        prog.Uniform("rgbaGlowIn", new Vec4f(glowColor[0], glowColor[1], glowColor[2], gi / 255f));
        prog.Uniform("rgbaFogIn", MainAPI.Capi.Render.FogColor);
        prog.Uniform("fogMinIn", MainAPI.Capi.Render.FogMin);
        prog.Uniform("fogDensityIn", MainAPI.Capi.Render.FogDensity);
        prog.Uniform("normalShaded", renderInfo.NormalShaded ? 1 : 0);
        prog.Uniform(MainAPI.PerspectiveMatrix, "projectionMatrix");
        prog.UniformMatrix("viewMatrix", MainAPI.Capi.Render.CameraMatrixOriginf);

        RenderTools.DisableCulling();

        MainAPI.Capi.Render.RenderMultiTextureMesh(renderInfo.ModelRef, "tex");

        RenderTools.EnableCulling();

        // Fix shadow sampler???
        MainAPI.Capi.Render.StandardShader.BindTexture2D("tex2dOverlay", 0, 1);
        prog.Uniform("tex2dOverlay", 0);
    }

    public override void OnBeforeRender(ICoreClientAPI capi, ItemStack itemStack, EnumItemRenderTarget target, ref ItemRenderInfo renderinfo)
    {
        if (!ReadStack(0, itemStack, api, out ItemStack? lineStack)) return;

        string lineCode = lineStack?.Collectible.Attributes["lineType"].AsString() ?? "none";

        renderinfo.ModelRef = ObjectCacheUtil.GetOrCreate(capi, $"{Id}-{lineCode}", () =>
        {
            string shapePath = Shape.Base;
            shapePath = shapePath.Replace(":", ":shapes/");
            shapePath += ".json";

            // When getting the shape assumes the shape file is the same as the rod's name.
            Shape shape = capi.Assets.TryGet(shapePath).ToObject<Shape>();

            shape.Textures.Clear();
            foreach (KeyValuePair<string, CompositeTexture> texture in Textures)
            {
                shape.Textures.Add(texture.Key, new AssetLocation(texture.Value.ToString().Split('@')[0]));
            }

            shape.Textures["linen"] = new AssetLocation($"fishing:lines/{lineCode}");
            ShapeTextureSource textureSource = new(capi, shape, "");

            capi.Tesselator.TesselateShape("", shape, out MeshData data, textureSource);
            return capi.Render.UploadMultiTextureMesh(data);
        });
    }
}

public static class ShaderExtensions
{
    public static void Uniform(this ShaderProgramBase shader, Matrix4 matrix, string name)
    {
        GL.UniformMatrix4(shader.uniformLocations[name], false, ref matrix);
    }
}