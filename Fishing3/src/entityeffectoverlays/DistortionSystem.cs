using MareLib;
using OpenTK.Graphics.OpenGL4;
using OpenTK.Mathematics;
using System;
using System.Collections.Generic;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.Client.NoObf;

namespace Fishing3;

// Convert this to render after post processing later, so bloom is distorted.

[GameSystem(0, EnumAppSide.Client)]
public class DistortionSystem : GameSystem, IRenderer
{
    private FboHandle? distortionFbo;
    private readonly MeshHandle fullscreen = RenderTools.GetFullscreenTriangle();

    private readonly Dictionary<long, Action<float, MareShader>> renderCallbacks = new();
    private readonly Dictionary<long, Action<float, MareShader>> renderCallbacksAnimated = new();
    private long nextInstanceId;

    public DistortionSystem(bool isServer, ICoreAPI api) : base(isServer, api)
    {
        MainAPI.OnWindowResize += (width, height) =>
        {
            distortionFbo?.SetDimensions(width, height);
        };

        MareShaderRegistry.AddShader("fishing:blit", "fishing:blit", "blit");
        MareShaderRegistry.AddShader("fishing:distortion/distortion", "fishing:distortion/distortion", "distortion");
        MareShaderRegistry.AddShader("fishing:distortion/distortionanimated", "fishing:distortion/distortion", "distortionanimated");
    }

    /// <summary>
    /// Registers a renderer for distortion, passes in the default distortion shader.
    /// </summary>
    public long RegisterRenderer(Action<float, MareShader> renderCallback)
    {
        long id = nextInstanceId;
        renderCallbacks.Add(id, renderCallback);
        nextInstanceId++;

        if (renderCallbacks.Count + renderCallbacksAnimated.Count == 1)
        {
            distortionFbo = new FboHandle(MainAPI.RenderWidth, MainAPI.RenderHeight);
            distortionFbo
                .AddAttachment(FramebufferAttachment.ColorAttachment0)
                .AddAttachment(FramebufferAttachment.DepthAttachment);

            MainAPI.Capi.Event.RegisterRenderer(this, EnumRenderStage.AfterFinalComposition);
        }

        return id;
    }

    public void UnregisterRenderer(long instanceId)
    {
        renderCallbacks.Remove(instanceId);

        if (renderCallbacks.Count + renderCallbacksAnimated.Count == 0)
        {
            distortionFbo?.Dispose();
            distortionFbo = null;

            MainAPI.Capi.Event.UnregisterRenderer(this, EnumRenderStage.AfterFinalComposition);
        }
    }

    /// <summary>
    /// Registers a renderer for distortion, passes in the default distortion shader.
    /// </summary>
    public long RegisterAnimatedRenderer(Action<float, MareShader> renderCallback)
    {
        long id = nextInstanceId;
        renderCallbacksAnimated.Add(id, renderCallback);
        nextInstanceId++;

        if (renderCallbacks.Count + renderCallbacksAnimated.Count == 1)
        {
            distortionFbo = new FboHandle(MainAPI.RenderWidth, MainAPI.RenderHeight);
            distortionFbo
                .AddAttachment(FramebufferAttachment.ColorAttachment0)
                .AddAttachment(FramebufferAttachment.DepthAttachment);

            MainAPI.Capi.Event.RegisterRenderer(this, EnumRenderStage.AfterFinalComposition);
        }

        return id;
    }

    public void UnregisterAnimatedRenderer(long instanceId)
    {
        renderCallbacksAnimated.Remove(instanceId);

        if (renderCallbacks.Count + renderCallbacksAnimated.Count == 0)
        {
            distortionFbo?.Dispose();
            distortionFbo = null;

            MainAPI.Capi.Event.UnregisterRenderer(this, EnumRenderStage.AfterFinalComposition);
        }
    }

    public void OnRenderFrame(float dt, EnumRenderStage stage)
    {
        if (distortionFbo == null) return;

        // Setup for both.
        FrameBufferRef primary = RenderTools.GetFramebuffer(EnumFrameBuffer.Primary);
        IShaderProgram current = ShaderProgramBase.CurrentShaderProgram;
        distortionFbo.Bind(FramebufferTarget.Framebuffer);

        // Depth write turned off in block outline.
        RenderTools.EnableDepthWrite();

        GL.ClearColor(new Color4(0, 0, 0, 0));
        GL.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);

        RenderTools.EnableDepthTest();
        RenderTools.EnableCulling();

        // Do regular distortion render.
        MareShader distortion = MareShaderRegistry.Get("distortion");
        distortion.Use();
        distortion.BindTexture(primary.ColorTextureIds[0], "primary"); // 0.
        distortion.BindTexture(primary.DepthTextureId, "depth"); // 1.
        distortion.Uniform("time", TimeUtility.ElapsedClientSeconds());
        distortion.Uniform("resolution", new Vector2(MainAPI.RenderWidth, MainAPI.RenderHeight));
        distortion.Uniform("useTexture", 0);

        foreach (Action<float, MareShader> action in renderCallbacks.Values)
        {
            action(dt, distortion);
        }

        // Do animated distortion render.
        MareShader distortionAnimated = MareShaderRegistry.Get("distortionanimated");
        distortionAnimated.Use();
        distortionAnimated.BindTexture(primary.ColorTextureIds[0], "primary"); // 0.
        distortionAnimated.BindTexture(primary.DepthTextureId, "depth"); // 1.
        distortionAnimated.Uniform("time", TimeUtility.ElapsedClientSeconds());
        distortionAnimated.Uniform("resolution", new Vector2(MainAPI.RenderWidth, MainAPI.RenderHeight));
        distortionAnimated.Uniform("useTexture", 0);
        distortionAnimated.UniformMatrix("playerViewMatrix", MainAPI.Capi.Render.CameraMatrixOriginf);

        foreach (Action<float, MareShader> action in renderCallbacksAnimated.Values)
        {
            action(dt, distortionAnimated);
        }

        // Clean up.
        GL.BindFramebuffer(FramebufferTarget.Framebuffer, primary.FboId);

        // Blit to primary texture.
        MareShader blit = MareShaderRegistry.Get("blit");
        blit.Use();
        blit.BindTexture(distortionFbo[FramebufferAttachment.ColorAttachment0].Handle, "tex2d", 0);
        GL.BindVertexArray(fullscreen.vaoId);
        GL.DrawArrays(PrimitiveType.Triangles, 0, 3);
        GL.BindVertexArray(0);

        // Use old shader.
        current?.Use();
    }

    public void Dispose()
    {
        GC.SuppressFinalize(this);
        fullscreen.Dispose();
        distortionFbo?.Dispose();
    }

    public double RenderOrder => 100;
    public int RenderRange => 0;
}