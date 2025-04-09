using MareLib;
using OpenTK.Mathematics;
using System;
using System.Collections.Generic;

namespace Fishing3;

public struct ProcessingParticleInstance
{
    public float maxLife;
    public float lifeLeft;
    public Vector4 color;
    public Vector2 screenPosition;
    public float velocity;
}

public class WidgetProcessingIndicator : Widget
{
    private readonly Queue<ProcessingParticleInstance> particleQueue = new();
    private readonly Func<bool> isProcessing;

    public override int SortPriority => 1;

    private Accumulator accum = Accumulator.WithRandomInterval(0.3f, 1f);

    private readonly NineSliceTexture background;
    private readonly Texture blank;

    public WidgetProcessingIndicator(Widget? parent, Func<bool> isProcessing) : base(parent)
    {
        this.isProcessing = isProcessing;
        background = GuiThemes.Button;
        blank = GuiThemes.Blank;

        accum.Max(2f);
    }

    public void SpawnParticle()
    {
        ProcessingParticleInstance inst = new()
        {
            color = Vector4.One * (0.5f + (Random.Shared.NextSingle() * 0.5f)),
            lifeLeft = 1.5f + (Random.Shared.NextSingle() * 1.5f),
            screenPosition = new(X + (Width * Random.Shared.NextSingle()), Y + (Height * Random.Shared.NextSingle())),
            velocity = 50f + (Random.Shared.NextSingle() * 100f)
        };

        inst.maxLife = inst.lifeLeft;

        inst.color.W = 0.5f;

        particleQueue.Enqueue(inst);
    }

    public override void OnRender(float dt, MareShader shader)
    {
        bool processing = isProcessing();

        if (accum.Progress(dt * 3) && processing)
        {
            SpawnParticle();
        }

        shader.Uniform("color", processing ? new Vector4(0.9f, 0.5f, 0.3f, 0.6f) : new Vector4(0.4f, 0.4f, 0.4f, 0.6f));
        RenderTools.RenderNineSlice(background, shader, X, Y, Width, Height); // Background.

        int queueCount = particleQueue.Count;
        shader.BindTexture(blank, "tex2d");

        for (int i = 0; i < queueCount; i++)
        {
            ProcessingParticleInstance particle = particleQueue.Dequeue();
            particle.lifeLeft -= dt;
            if (particle.lifeLeft <= 0) continue;

            particle.screenPosition.Y -= particle.velocity * dt;

            float fade = particle.lifeLeft / particle.maxLife;

            Vector4 color = particle.color;
            color.W *= fade;
            shader.Uniform("color", color);

            // Render particle.
            RenderTools.RenderQuad(shader, particle.screenPosition.X - 10, particle.screenPosition.Y - 10, 20, 20);

            // Re-enqueue the particle with updated values.
            particleQueue.Enqueue(particle);
        }

        shader.Uniform("color", Vector4.One);
    }
}