using OpenTK.Mathematics;
using System;
using System.Collections.Generic;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;
using Vintagestory.Client.NoObf;

namespace Fishing;

public class HeatPipeInstance
{
    public GridPos position;
    public float celsius;
    public MeshHandle? mesh;

    public HeatPipeInstance(GridPos pos, float celsius)
    {
        position = pos;
        this.celsius = celsius;
    }

    public void ChangeTemperature(float celsius)
    {
        this.celsius += celsius;
        // Clamp between 15 and 1000.
        this.celsius = Math.Clamp(this.celsius, 15f, 1000f);
    }
}

/// <summary>
/// Heat pipe blocks register to this.
/// On the client it handles rendering, on the server it handles spreading.
/// </summary>
[GameSystem]
public class HeatPipeSystem : GameSystem, IRenderer
{
    public double RenderOrder => 0.55;
    public int RenderRange => 0;

    private readonly Dictionary<GridPos, HeatPipeInstance> activePipes = [];
    private long listenerId;

    private Texture? pipeTexture;

    public HeatPipeSystem(bool isServer, ICoreAPI api) : base(isServer, api)
    {
    }

    public override void OnStart()
    {
        if (!isServer)
        {
            pipeTexture = Texture.Create("fishing:textures/heatpipe.png", true, true);
            //pipeTexture = Texture.Create("game:textures/block/metal/sheet/cupronickel1.png", true, true);
            NuttyShaderRegistry.AddShader("marelib:opaque", "fishing:heatpipe", "heatpipe");
        }
    }

    private void ForEachAdjacentPipe(Action<HeatPipeInstance> action, GridPos pos)
    {
        pos.Y++;

        for (int i = 0; i < 3; i++)
        {
            BlockFaces.ForEachPerpendicularFace(EnumBlockFacing.Up, face =>
            {
                GridPos offset = BlockFaces.GetFaceOffset(face) + pos;
                if (activePipes.TryGetValue(offset, out HeatPipeInstance? value))
                {
                    action(value);
                }
            });
            pos.Y--;
        }
    }

    private void ServerTick(int tick)
    {
        if (tick % 20 != 0) return;

        foreach (HeatPipeInstance instance in activePipes.Values)
        {
            if (instance.celsius > 15)
            {
                instance.ChangeTemperature(-0.4f);
                MainAPI.Sapi.World.BlockAccessor.MarkBlockEntityDirty(new BlockPos(instance.position.X, instance.position.Y, instance.position.Z));
            }

            if (instance.celsius < 15)
            {
                instance.celsius = 15;
                MainAPI.Sapi.World.BlockAccessor.MarkBlockEntityDirty(new BlockPos(instance.position.X, instance.position.Y, instance.position.Z));
            }
        }

        foreach (HeatPipeInstance instance in activePipes.Values)
        {
            ForEachAdjacentPipe(adja =>
            {
                float difference = adja.celsius - instance.celsius;
                difference -= 10f; // Up to 10 degrees of transfer.
                if (difference <= 0.001f) return; // Actual floating point inaccuracy problem.

                if (difference > 1f) difference *= 0.5f;

                difference *= 0.5f;

                adja.ChangeTemperature(-difference);
                instance.ChangeTemperature(difference);

                // Mark both dirty.
                MainAPI.Sapi.World.BlockAccessor.MarkBlockEntityDirty(new BlockPos(instance.position.X, instance.position.Y, instance.position.Z));
                MainAPI.Sapi.World.BlockAccessor.MarkBlockEntityDirty(new BlockPos(adja.position.X, adja.position.Y, adja.position.Z));
            }, instance.position);
        }
    }

    /// <summary>
    /// Registers and tessellates a pipe.
    /// </summary>
    public void RegisterPipe(HeatPipeInstance instance)
    {
        if (activePipes.Count == 0)
        {
            if (!isServer)
            {
                MainAPI.Capi.Event.RegisterRenderer(this, EnumRenderStage.Opaque);
            }
            else
            {
                listenerId = TickSystem.Server!.RegisterTicker(ServerTick);
            }
        }

        if (activePipes.TryGetValue(instance.position, out HeatPipeInstance? value))
        {
            Console.WriteLine($"Pipe already registered at {instance.position} on {api.Side}, disposing.");
            value.mesh?.Dispose();
        }

        activePipes[instance.position] = instance;

        if (!isServer)
        {
            ForEachAdjacentPipe(RetessellateInstance, instance.position);
            RetessellateInstance(instance);
        }
    }

    /// <summary>
    /// Unregisters and disposes a pipe.
    /// </summary>
    public void UnregisterPipe(HeatPipeInstance instance)
    {
        if (activePipes.TryGetValue(instance.position, out HeatPipeInstance? value))
        {
            value.mesh?.Dispose();
            activePipes.Remove(instance.position);
        }
        else
        {
            return; // Nothing to unregister.
        }

        if (!isServer)
        {
            ForEachAdjacentPipe(RetessellateInstance, instance.position);
        }

        if (activePipes.Count == 0)
        {
            if (!isServer)
            {
                MainAPI.Capi?.Event.UnregisterRenderer(this, EnumRenderStage.Opaque);
            }
            else
            {
                TickSystem.Server?.UnregisterTicker(listenerId);
            }
        }
    }

    public void OnRenderFrame(float dt, EnumRenderStage stage)
    {
        if (pipeTexture == null) return;

        ShaderProgramBase? currentShader = ShaderProgramBase.CurrentShaderProgram;
        currentShader?.Stop();

        NuttyShader pipeShader = NuttyShaderRegistry.Get("heatpipe");
        pipeShader.Use();

        pipeShader.LightUniforms();
        pipeShader.ShadowUniforms();

        pipeShader.BindTexture(pipeTexture, "tex2d");

        pipeShader.UniformMatrix("offsetViewMatrix", MainAPI.Capi.Render.CameraMatrixOriginf);

        Vector3d offset = MainAPI.CameraPosition - new Vector3d(MainAPI.Capi.World.Player.Entity.CameraPos.X, MainAPI.Capi.World.Player.Entity.CameraPos.Y, MainAPI.Capi.World.Player.Entity.CameraPos.Z);

        foreach (HeatPipeInstance instance in activePipes.Values)
        {
            if (instance.mesh == null) continue;

            pipeShader.Uniform("temperature", instance.celsius);
            pipeShader.Uniform("modelMatrix", RenderTools.CameraRelativeTranslation(instance.position.X + offset.X, instance.position.Y + offset.Y, instance.position.Z + offset.Z));

            int ACTUAL_VALUE = MainAPI.Client.blockAccessor.GetLightRGBsAsInt(instance.position.X, instance.position.Y, instance.position.Z);
            Vector4 light = new((ACTUAL_VALUE & 0xFF) / 255f, ((ACTUAL_VALUE >> 8) & 0xFF) / 255f, ((ACTUAL_VALUE >> 16) & 0xFF) / 255f, ((ACTUAL_VALUE >> 24) & 0xFF) / 255f);
            pipeShader.Uniform("rgbaLightIn", light);

            RenderTools.RenderMesh(instance.mesh);
        }

        currentShader?.Use();
    }

    /// <summary>
    /// Retessellates an instance based on context of registered instances.
    /// </summary>
    public void RetessellateInstance(HeatPipeInstance instance)
    {
        instance.mesh?.Dispose();

        MeshInfo<StandardVertex> data = new(6, 6);

        // Tessellate center.
        CubeMeshUtility.AddRangeCubeData(data, v =>
        {
            v = TessellatorTools.SetUvsBasedOnPosition(v);
            return new StandardVertex(v.position, v.uv, v.normal, Vector4.One);
        }, new Vector3(0.4f, 0f, 0.4f), new Vector3(0.6f, 0.15f, 0.6f));

        for (int i = 0; i < 4; i++)
        {
            GridPos offsetPos = BlockFaces.GetFaceOffset((EnumBlockFacing)i) + instance.position;
            offsetPos.Y++;

            bool tessellateDown = false;
            bool valid = false;

            for (int y = 0; y < 3; y++)
            {
                if (!activePipes.ContainsKey(offsetPos))
                {
                    offsetPos.Y--;
                    continue;
                }

                if (y == 2) tessellateDown = true;
                valid = true;
                break;
            }

            if (!valid) continue;

            GridPos offset = BlockFaces.GetFaceOffset((EnumBlockFacing)i);
            Vector3 offsetVector = new(offset.X, offset.Y, offset.Z);
            Vector3 perp = Vector3.Cross(Vector3.UnitY, offsetVector);

            Vector3 start = new Vector3(0.5f, 0f, 0.5f) + (offsetVector * 0.1f) - (perp * 0.1f);
            Vector3 end = new Vector3(0.5f, 0.15f, 0.5f) + (offsetVector * 0.5f) + (perp * 0.1f);

            Vector3 min = Vector3.ComponentMin(start, end);
            Vector3 max = Vector3.ComponentMax(start, end);

            CubeMeshUtility.AddRangeCubeData(data, v =>
            {
                v = TessellatorTools.SetUvsBasedOnPosition(v);
                return new StandardVertex(v.position, v.uv, v.normal, Vector4.One);
            }, min, max);

            if (tessellateDown)
            {
                start = new Vector3(0.5f, 0f, 0.5f) + (offsetVector * 0.5f) - (perp * 0.1f);
                end = start + (offsetVector * 0.15f) + (perp * 0.2f);
                end.Y -= 0.85f;
                start.Y += 0.15f;

                min = Vector3.ComponentMin(start, end);
                max = Vector3.ComponentMax(start, end);

                CubeMeshUtility.AddRangeCubeData(data, v =>
                {
                    v = TessellatorTools.SetUvsBasedOnPosition(v);
                    return new StandardVertex(v.position, v.uv, v.normal, Vector4.One);
                }, min, max);
            }
        }

        instance.mesh = data.Upload();
    }

    public void Dispose()
    {
        GC.SuppressFinalize(this);
    }
}