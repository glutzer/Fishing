using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Vintagestory.API.Common;

namespace Fishing3;

public class FluidStackPotion : FluidStack
{
    public List<FluidStack> containedStacks = new();

    public override int Units
    {
        get => containedStacks.Sum(x => x.Units);
        set
        {
            return; // Can't set units.
        }
    }

    public FluidStackPotion(Fluid fluid) : base(fluid)
    {
    }

    public override bool CanTakeFrom(FluidStack other)
    {
        return other is FluidStackPotion || other.fluid.HasBehavior<FluidBehaviorReagent>();
    }

    public override int TakeFrom(FluidStack other, int maxUnits)
    {
        int initialUnits = maxUnits;

        if (other is FluidStackPotion potionFluid)
        {
            int units = potionFluid.Units;
            int unitsToMove = maxUnits;

            foreach (FluidStack containedStack in potionFluid.containedStacks)
            {
                float ratio = containedStack.Units / (float)units;
                int toMove = (int)Math.Ceiling(unitsToMove * ratio);
                if (toMove > maxUnits) toMove = maxUnits;

                if (CanTakeFrom(containedStack))
                {
                    maxUnits -= TakeFrom(containedStack, toMove);
                }
            }

            int amountTaken = initialUnits - maxUnits;
            potionFluid.OnTakenFrom(amountTaken);

            return initialUnits - maxUnits;
        }

        // Not a potion fluid, just a normal fluid.

        foreach (FluidStack stack in containedStacks)
        {
            if (stack.CanTakeFrom(other))
            {
                maxUnits -= stack.TakeFrom(other, maxUnits);
            }
        }

        // Was able to take the entire amount needed into an existing stack.
        if (other.Units == 0) return initialUnits - maxUnits;

        // Create a new stack...
        if (maxUnits > 0)
        {
            FluidStack stack = other.fluid.CreateFluidStack();

            if (stack.CanTakeFrom(other))
            {
                maxUnits -= stack.TakeFrom(other, maxUnits);
                containedStacks.Add(stack);
            }
        }

        return initialUnits - maxUnits;
    }

    public override void OnTakenFrom(int units)
    {
        // Remove empty stacks.
        if (units == 0) return;

        containedStacks.RemoveAll(x => x.Units == 0);
    }

    public override void ToBytes(BinaryWriter writer)
    {
        base.ToBytes(writer);

        writer.Write(containedStacks.Count);

        foreach (FluidStack containedStack in containedStacks)
        {
            Save(containedStack, writer);
        }
    }

    public override void FromBytes(BinaryReader reader, EnumAppSide side)
    {
        containedStacks.Clear();

        base.FromBytes(reader, side);

        int fluidCount = reader.ReadInt32();

        for (int i = 0; i < fluidCount; i++)
        {
            FluidStack? containedStack = Load(reader, side);
            if (containedStack == null) continue;
            containedStacks.Add(containedStack);
        }
    }

    public override void GetFluidInfo(StringBuilder builder)
    {
        foreach (FluidStack stack in containedStacks)
        {
            stack.GetFluidInfo(builder);
        }
    }
}