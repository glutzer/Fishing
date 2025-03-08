using System;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;
using Vintagestory.Client.NoObf;
using Vintagestory.GameContent;

namespace Fishing3;

public class ItemFishFirepitRenderer : IInFirepitRenderer
{
    public double RenderOrder => 0.5;
    public int RenderRange => 20;

    public ICoreClientAPI capi;
    public BlockPos pos;
    private readonly Matrixf ModelMat = new();
    public ItemStack stack;
    public ItemFish fish;

    public ItemFishFirepitRenderer(ICoreClientAPI capi, ItemStack stack, BlockPos pos)
    {
        this.capi = capi;
        this.pos = pos;

        this.stack = stack;
        fish = (ItemFish)stack.Item;
    }

    public void OnRenderFrame(float dt, EnumRenderStage stage)
    {
        if (stage != EnumRenderStage.Opaque) return;

        IRenderAPI rpi = capi.Render;
        Vec3d camPos = capi.World.Player.Entity.CameraPos;

        FishSpecies? species = fish.GetSpecies(stack);
        if (species == null) return;

        IStandardShaderProgram prog = rpi.PreparedStandardShader(pos.X, pos.Y, pos.Z);

        double kg = ItemFish.GetWeight(stack);
        float scale = ItemFish.GetScale(kg);

        prog.DontWarpVertices = 0;
        prog.AddRenderFlags = 0;
        prog.RgbaAmbientIn = rpi.AmbientColor;
        prog.RgbaFogIn = rpi.FogColor;
        prog.FogMinIn = rpi.FogMin;
        prog.FogDensityIn = rpi.FogDensity;
        prog.RgbaTint = ColorUtil.WhiteArgbVec;
        prog.NormalShaded = 1;
        prog.ExtraGodray = 0;
        prog.SsaoAttn = 0;
        prog.AlphaTest = 0.05f;
        prog.OverlayOpacity = 0;

        prog.ModelMatrix = ModelMat
                .Identity()
                .Translate(pos.X - camPos.X + 0.001f, pos.Y - camPos.Y, pos.Z - camPos.Z - 0.001f)
                .Translate(0.5, 1f, 0.5f)
                .Rotate(0, -40 * GameMath.DEG2RAD, 90 * GameMath.DEG2RAD)
                .Scale(scale, scale, scale)
                .Values
            ;

        prog.ViewMatrix = rpi.CameraMatrixOriginf;
        prog.ProjectionMatrix = rpi.CurrentProjectionMatrix;

        MultiTextureMeshRef modelRef = ItemFish.GetModel(capi, species, stack.Attributes.GetBool("smoked"));

        rpi.RenderMultiTextureMesh(modelRef, "tex");

        prog.Stop();
    }

    public void OnCookingComplete()
    {

    }

    public void OnUpdate(float temperature)
    {

    }

    public void Dispose()
    {
        GC.SuppressFinalize(this);
    }
}
