using MareLib;
using OpenTK.Mathematics;
using System;
using System.Collections.Generic;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;
using Vintagestory.Client.NoObf;

namespace Fishing3;

public class FluidRenderingInstance
{
    public readonly FluidContainer container;
    public readonly Vector3 cubeScale;
    public readonly Vector3d cubeOffset;
    public long Id { get; set; } = -1;

    public float lerpedFill = 0f;

    public FluidRenderingInstance(FluidContainer container, Vector3 localStart, Vector3 localEnd, BlockPos blockPos)
    {
        this.container = container;

        (localStart, localEnd) = (Vector3.ComponentMin(localEnd, localStart), Vector3.ComponentMax(localEnd, localStart));

        cubeScale = localEnd - localStart;
        cubeOffset.X = blockPos.X + (double)localStart.X;
        cubeOffset.Y = blockPos.Y + (double)localStart.Y;
        cubeOffset.Z = blockPos.Z + (double)localStart.Z;
    }
}

[GameSystem(forSide = EnumAppSide.Client)]
public class FluidBlockRenderingSystem : GameSystem
{
    private long currentId;
    private readonly DummyRenderer renderer;
    private readonly Dictionary<long, FluidRenderingInstance> instances = new();
    private Texture fluidTexture = null!;
    private MeshHandle cubeMesh = null!;

    public static FluidBlockRenderingSystem? Instance { get; private set; }

    private MareShader shader = null!;

    public FluidBlockRenderingSystem(bool isServer, ICoreAPI api) : base(isServer, api)
    {
        renderer = new() { action = Render };
    }

    public override void OnAssetsLoaded()
    {
        fluidTexture = Texture.Create("fishing:textures/solution.png", false, true);
        cubeMesh = CubeMeshUtility.CreateGridAlignedCubeMesh(v =>
        {
            return new StandardVertex(v.position, v.uv, v.normal, Vector4.One);
        });

        Instance = this;
        shader = MareShaderRegistry.AddShader("fishing:liquidoit", "fishing:liquidoit", "liquidoit");
    }

    private void Render(float dt)
    {
        ShaderProgramBase? old = ShaderProgramBase.CurrentShaderProgram;
        old?.Stop();

        shader.Use();
        shader.LightUniforms();
        shader.ShadowUniforms();

        shader.BindTexture(fluidTexture, "tex2d");
        shader.UniformMatrix("offsetViewMatrix", MainAPI.Capi.Render.CameraMatrixOriginf);

        // Later this needs to be integrated into the shader for the original shadow map.

        // Vector3d offset = MainAPI.CameraPosition - new Vector3d(MainAPI.Capi.World.Player.Entity.CameraPos.X, MainAPI.Capi.World.Player.Entity.CameraPos.Y, MainAPI.Capi.World.Player.Entity.CameraPos.Z);

        RenderTools.EnableCulling();

        foreach (FluidRenderingInstance instance in instances.Values)
        {
            if (instance.container.HeldStack == null || instance.container.RoomUsed == 0) // Don't render empty instances.
            {
                instance.lerpedFill = 0f;
                continue;
            }

            int ACTUAL_VALUE = MainAPI.Client.blockAccessor.GetLightRGBsAsInt((int)instance.cubeOffset.X, (int)instance.cubeOffset.Y, (int)instance.cubeOffset.Z);
            Vector4 light = new((ACTUAL_VALUE & 0xFF) / 255f, ((ACTUAL_VALUE >> 8) & 0xFF) / 255f, ((ACTUAL_VALUE >> 16) & 0xFF) / 255f, ((ACTUAL_VALUE >> 24) & 0xFF) / 255f);

            shader.Uniform("rgbaLightIn", light);

            float fillPercent = instance.container.FillPercent;
            instance.lerpedFill = GameMath.Lerp(instance.lerpedFill, fillPercent, dt * 2f);
            if (Math.Abs(instance.lerpedFill - fillPercent) < 0.003) instance.lerpedFill = fillPercent;

            Vector3 scale = instance.cubeScale;
            scale.Y *= instance.lerpedFill;

            Matrix4 mat = Matrix4.CreateScale(scale) * RenderTools.CameraRelativeTranslation(instance.cubeOffset /*+ offset*/);
            shader.Uniform("modelMatrix", mat);
            shader.Uniform("uniformColor", instance.container.HeldStack.fluid.GetColor(instance.container.HeldStack));
            shader.Uniform("glowAmount", (int)(instance.container.HeldStack.fluid.GetGlowLevel(instance.container.HeldStack) * 255));

            Vector3 worldPos = RenderTools.CameraRelativePosition(instance.cubeOffset /*+ offset*/);

            shader.Uniform("cubePosition", worldPos + (scale / 2));
            shader.Uniform("cubeScale", scale);

            RenderTools.RenderMesh(cubeMesh);
        }

        RenderTools.DisableCulling();

        old?.Use();
    }

    public override void OnClose()
    {
        Instance = null;
        fluidTexture.Dispose();
        cubeMesh.Dispose();
    }

    public void RegisterInstance(FluidRenderingInstance instance)
    {
        if (instance.Id != -1) return;

        instance.Id = currentId++;
        instances.Add(instance.Id, instance);

        if (instances.Count == 1) MainAPI.Capi.Event.RegisterRenderer(renderer, EnumRenderStage.OIT);
    }

    public void UnregisterInstance(FluidRenderingInstance instance)
    {
        if (instance.Id == -1) return;

        instances.Remove(instance.Id);
        instance.Id = -1;

        if (instances.Count == 0) MainAPI.Capi.Event.UnregisterRenderer(renderer, EnumRenderStage.OIT);
    }
}