using System;
using System.Text.Json.Nodes;

namespace Fishing3;

public class EffectProperties
{
    public required string Type { get; set; }
    public float Strength { get; set; } = 1f;
    public float Duration { get; set; } = 1f;
    public JsonObject? Data { get; set; }
}

[FluidBehavior]
public class FluidBehaviorReagent : FluidBehavior
{
    public EffectProperties[] Properties { get; set; }

    public FluidBehaviorReagent(JsonObject data) : base(data)
    {
        Properties = data.Get<EffectProperties[]>("Effects") ?? Array.Empty<EffectProperties>();
    }

    public override void RegisterEvents(Fluid fluid)
    {
        fluid.EventBeforeFluidAddedToOwnStack.Register(args =>
        {
            // Dilute purities.
            if (args.toMove <= 0) return;

            float sourcePurity = args.sourceStack.Attributes.GetFloat("purity", 0f);
            float destinationPurity = args.thisStack.Attributes.GetFloat("purity", 0f);

            float ratio = args.toMove / (float)(args.toMove + args.thisStack.Units);

            float newPurity = (sourcePurity * ratio) + (destinationPurity * (1f - ratio));

            args.thisStack.Attributes.SetFloat("purity", newPurity);
        });

        fluid.EventGetFluidInfo.Register(args =>
        {
            float purity = args.thisStack.Attributes.GetFloat("purity", 0f);
            args.builder.AppendLine($"<font color=\"#777777\">Reagent</font>, {MathF.Round(purity, 2)} purity");
        });
    }
}