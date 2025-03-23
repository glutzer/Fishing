using OpenTK.Mathematics;
using System;
using Vintagestory.API.Common;

namespace Fishing3;

[Fluid]
public class FluidPotion : Fluid
{
    protected override Type StackType => typeof(FluidStackPotion);

    public FluidPotion(FluidJson fluidJson, int id, ICoreAPI api) : base(fluidJson, id, api)
    {
    }

    public override Vector4 GetColor(FluidStack fluidStack)
    {
        if (fluidStack is not FluidStackPotion potionFluidStack) return color;

        Vector4 outColor = default;
        float weight = 0;

        foreach (FluidStack stack in potionFluidStack.containedStacks)
        {
            int units = stack.Units;
            outColor += stack.fluid.GetColor(stack) * units;
            weight += 1f * units;
        }

        return outColor / weight;
    }

    public override float GetGlowLevel(FluidStack fluidStack)
    {
        if (fluidStack is not FluidStackPotion potionFluidStack) return glowLevel;

        float outGlow = 0;
        float weight = 0;

        foreach (FluidStack stack in potionFluidStack.containedStacks)
        {
            int units = stack.Units;
            outGlow += stack.fluid.GetGlowLevel(stack) * units;
            weight += 1f * units;
        }

        return outGlow / weight;
    }
}