using System;
using System.Text;
using System.Text.Json.Nodes;

namespace Fishing3;

public class FluidBehaviorReagent : FluidBehavior
{
    public FluidBehaviorReagent(JsonObject data) : base(data)
    {
    }

    public override void GetFluidInfo(StringBuilder builder, FluidStack stack)
    {
        builder.AppendLine("Reagent");
        float purity = stack.Attributes.GetFloat("purity", 0f);
        builder.AppendLine($"Purity: {MathF.Round(purity, 2)}");
    }

    public override void BeforeFluidAddedToOwnStack(FluidStack sourceStack, FluidStack thisStack, int toMove)
    {
        // Dilute purities.
        if (toMove <= 0) return;

        float sourcePurity = sourceStack.Attributes.GetFloat("purity", 0f);
        float destinationPurity = thisStack.Attributes.GetFloat("purity", 0f);

        float ratio = toMove / (float)(toMove + thisStack.Units);

        float newPurity = (sourcePurity * ratio) + (destinationPurity * (1f - ratio));

        thisStack.Attributes.SetFloat("purity", newPurity);
    }
}