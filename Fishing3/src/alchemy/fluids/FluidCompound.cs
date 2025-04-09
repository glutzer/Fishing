using OpenTK.Mathematics;
using System;
using System.Collections.Generic;
using Vintagestory.API.Common;

namespace Fishing3;

/// <summary>
/// A fluid with many others, for things like potions or separating.
/// </summary>
[Fluid]
public class FluidCompound : Fluid
{
    protected override Type StackType => typeof(FluidStackCompound);

    public FluidCompound(FluidJson fluidJson, int id, ICoreAPI api) : base(fluidJson, id, api)
    {
    }

    public override void RegisterEvents()
    {
        //base.RegisterEvents();
        // Don't register base take from event, this can take from anything.

        // Check every fluid this one consists of.
        EventCheckFluid.Register(args =>
        {
            if (args.container.HeldStack is not FluidStackCompound compoundStack) return;

            // Create a temporary container for checking.
            FluidContainer container = new(args.container.Capacity);
            List<FluidStack>? newStacks = null;

            for (int i = 0; i < compoundStack.containedStacks.Count; i++)
            {
                FluidStack stack = compoundStack.containedStacks[i];

                container.SetStack(stack);

                stack.fluid.EventCheckFluid.Invoke((container, args.containingObject, args.api));

                // If the stack in the container has changed, remove at this index, and subtract i so it will be at the next index.
                if (stack != container.HeldStack) // If the stack was modified.
                {
                    compoundStack.containedStacks.RemoveAt(i);
                    i--;
                    if (container.HeldStack != null)
                    {
                        newStacks ??= new List<FluidStack>();
                        newStacks.Add(container.HeldStack);
                    }
                }
            }

            // Move every new stack back into this container.
            if (newStacks != null)
            {
                foreach (FluidStack newStack in newStacks)
                {
                    FluidContainer.MoveFluids(newStack, args.container);
                }
            }
        });
    }

    public override Vector4 GetColor(FluidStack fluidStack)
    {
        if (fluidStack is not FluidStackCompound potionFluidStack) return color;

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
        if (fluidStack is not FluidStackCompound potionFluidStack) return glowLevel;

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