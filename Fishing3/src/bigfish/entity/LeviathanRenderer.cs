using System;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.MathTools;
using Vintagestory.GameContent;

namespace Fishing;

public class LeviathanRenderer : EntityShapeRenderer
{
    public LeviathanRenderer(Entity entity, ICoreClientAPI api) : base(entity, api)
    {
    }

    public override void DoRender3DOpaque(float dt, bool isShadowPass)
    {
        if (isSpectator) return;

        NewModelMatrix(entity, dt, isShadowPass);
        Vec3d camPos = capi.World.Player.Entity.CameraPos;
        OriginPos.Set((float)(entity.Pos.X - camPos.X), (float)(entity.Pos.InternalY - camPos.Y), (float)(entity.Pos.Z - camPos.Z));

        if (isShadowPass)
        {
            DoRender3DAfterOIT(dt, true);
        }
    }

    public void NewModelMatrix(Entity entity, float dt, bool isShadowPass)
    {
        Mat4f.Identity(ModelMat);

        EntityPlayer entityPlayer = capi.World.Player.Entity;
        Mat4f.Translate(ModelMat, ModelMat, (float)(entity.Pos.X - entityPlayer.CameraPos.X), (float)(entity.Pos.InternalY - entityPlayer.CameraPos.Y), (float)(entity.Pos.Z - entityPlayer.CameraPos.Z));

        float rotX = entity.Properties.Client.Shape?.rotateX ?? 0;
        float rotZ = entity.Properties.Client.Shape?.rotateZ ?? 0;

        double[] quat = Quaterniond.Create();

        //float yaw = entity.Pos.Yaw + ((rotY + 90) * GameMath.DEG2RAD);
        float yaw = entity.Pos.Yaw;

        Quaterniond.RotateY(quat, quat, yaw);
        Quaterniond.RotateX(quat, quat, entity.Pos.Pitch + (rotX * GameMath.DEG2RAD));
        Quaterniond.RotateZ(quat, quat, entity.Pos.Roll + (rotZ * GameMath.DEG2RAD));

        Quaterniond.RotateY(quat, quat, yangle);
        Quaterniond.RotateX(quat, quat, xangle);
        Quaterniond.RotateZ(quat, quat, zangle);

        float[] qf = new float[quat.Length];
        for (int i = 0; i < quat.Length; i++) qf[i] = (float)quat[i];
        Mat4f.Mul(ModelMat, ModelMat, Mat4f.FromQuat(Mat4f.Create(), qf));

        float scale = entity.Properties.Client.Size;
        Mat4f.Scale(ModelMat, ModelMat, new float[] { scale, scale, scale });

        // Center entity at 0.5f.
        Mat4f.Translate(ModelMat, ModelMat, -0.5f, 0, -0.5f);
    }

    public override void DoRender3DOpaqueBatched(float dt, bool isShadowPass)
    {
        if (isSpectator || meshRefOpaque == null) return;

        IShaderProgram currentActiveShader = capi.Render.CurrentActiveShader;
        if (isShadowPass)
        {
            Mat4f.Mul(tmpMvMat, capi.Render.CurrentModelviewMatrix, ModelMat);
            currentActiveShader.UniformMatrix("modelViewMatrix", tmpMvMat);
        }
        else
        {
            frostAlpha += (targetFrostAlpha - frostAlpha) * dt / 6f;
            float value = (float)Math.Round(GameMath.Clamp(frostAlpha, 0f, 1f), 4);

            // Get max light for every corner of the hitbox.
            for (int i = 0; i < 8; i++)
            {
                bool addX = (i & 1) != 0;
                bool addY = (i & 2) != 0;
                bool addZ = (i & 4) != 0;

                float x = (int)(entity.Pos.X + entity.CollisionBox.X1);
                float y = (int)(entity.Pos.InternalY + entity.CollisionBox.Y1);
                float z = (int)(entity.Pos.Z + entity.CollisionBox.Z1);

                if (addX)
                {
                    x += entity.CollisionBox.XSize;
                }

                if (addY)
                {
                    y += entity.CollisionBox.YSize;
                }

                if (addZ)
                {
                    z += entity.CollisionBox.ZSize;
                }

                Vec4f lightRgbs = capi.World.BlockAccessor.GetLightRGBs((int)x, (int)y, (int)z);
                lightrgbs.R = Math.Max(lightrgbs.R, lightRgbs.R);
                lightrgbs.G = Math.Max(lightrgbs.G, lightRgbs.G);
                lightrgbs.B = Math.Max(lightrgbs.B, lightRgbs.B);
                lightrgbs.A = Math.Max(lightrgbs.A, lightRgbs.A);
            }

            currentActiveShader.Uniform("rgbaLightIn", lightrgbs);

            currentActiveShader.Uniform("extraGlow", entity.Properties.Client.GlowLevel);
            currentActiveShader.UniformMatrix("modelMatrix", ModelMat);
            currentActiveShader.UniformMatrix("viewMatrix", capi.Render.CurrentModelviewMatrix);
            currentActiveShader.Uniform("addRenderFlags", AddRenderFlags);
            currentActiveShader.Uniform("windWaveIntensity", (float)WindWaveIntensity);
            currentActiveShader.Uniform("entityId", (int)entity.EntityId);
            currentActiveShader.Uniform("glitchFlicker", glitchFlicker ? 1 : 0);
            currentActiveShader.Uniform("frostAlpha", value);
            currentActiveShader.Uniform("waterWaveCounter", capi.Render.ShaderUniforms.WaterWaveCounter);
            color.R = ((entity.RenderColor >> 16) & 0xFF) / 255f;
            color.G = ((entity.RenderColor >> 8) & 0xFF) / 255f;
            color.B = (entity.RenderColor & 0xFF) / 255f;
            color.A = ((entity.RenderColor >> 24) & 0xFF) / 255f;
            currentActiveShader.Uniform("renderColor", color);
            double stability = entity.WatchedAttributes.GetDouble("temporalStability", 1.0);
            double playerStability = capi.World.Player.Entity.WatchedAttributes.GetDouble("temporalStability", 1.0);
            double minStability = Math.Min(stability, playerStability);
            float glitchValue = (float)(glitchAffected ? Math.Max(0.0, 1.0 - (2.5 * minStability)) : 0.0);
            currentActiveShader.Uniform("glitchEffectStrength", glitchValue);
        }

        currentActiveShader.UBOs["Animation"].Update(entity.AnimManager.Animator.Matrices, 0, entity.AnimManager.Animator.MaxJointId * 16 * 4);
        capi.Render.RenderMultiTextureMesh(meshRefOpaque, "entityTex");
    }
}