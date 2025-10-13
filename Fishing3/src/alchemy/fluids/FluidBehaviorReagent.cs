using System;
using System.Text;
using System.Text.Json.Nodes;

namespace Fishing3;

public class EffectProperties
{
    public required string Type { get; set; }
    public float Strength { get; set; } = 1f;
    public float Duration { get; set; } = 1f;
    public JsonObject? Data { get; set; }
    public Effect? ReferenceEffect { get; set; }
}

public interface IEffectInfoProvider
{
    /// <summary>
    /// Gets info from reagent behavior.
    /// Only CollectDataFromReagent is called at this time, if it's an AlchemyEffect.
    /// The strength from the properties is also applied.
    /// Purity may be obtained from the stack, from FluidBehaviorReagent.GetPurityMultiplier().
    /// </summary>
    void GetInfo(StringBuilder builder, FluidStack stack);
}

[FluidBehavior]
public class FluidBehaviorReagent : FluidBehavior
{
    public EffectProperties[] Properties { get; set; }

    public FluidBehaviorReagent(JsonObject data) : base(data)
    {
        Properties = data.Get<EffectProperties[]>("Effects") ?? Array.Empty<EffectProperties>();
    }

    public static float GetPurityMultiplier(FluidStack stack)
    {
        return stack.Attributes.GetFloat("purity", 0f) + 1f;
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

            foreach (EffectProperties props in Properties)
            {
                if (props.ReferenceEffect is IEffectInfoProvider infoProvider)
                {
                    infoProvider.GetInfo(args.builder, args.thisStack);
                }
            }
        });

        foreach (EffectProperties props in Properties)
        {
            props.ReferenceEffect = MainAPI.GetGameSystem<EffectManager>(fluid.api.Side).CreateEffect(props.Type);

            if (props.ReferenceEffect is AlchemyEffect alch && props.Data != null)
            {
                alch.CollectDataFromReagent(props.Data);
                alch.StrengthMultiplier *= props.Strength;
            }
        }
    }
}