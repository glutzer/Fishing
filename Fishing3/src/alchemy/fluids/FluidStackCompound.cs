using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Vintagestory.API.Common;

namespace Fishing3;

public class FluidStackCompound : FluidStack
{
    public List<FluidStack> containedStacks = [];

    public override int Units
    {
        get => containedStacks.Sum(x => x.Units);
        set
        {
            return; // Can't set units.
        }
    }

    public FluidStackCompound(Fluid fluid) : base(fluid)
    {
    }

    public override bool CanTakeFrom(FluidStack other)
    {
        return true; // Can hold anything.
    }

    public override int TakeFrom(FluidStack other, int maxUnits)
    {
        int initialUnits = maxUnits;

        if (other is FluidStackCompound potionFluid)
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

            if (stack.fluid == other.fluid) goto Finish; // Prevent a new potion stack from being created if this type of fluid already exists in the compound stack.
        }

        // Create a new stack...
        if (maxUnits > 0)
        {
            FluidStack stack = other.fluid.CreateFluidStack();

            if (stack.CanTakeFrom(other))
            {
                maxUnits -= stack.TakeFrom(other, maxUnits);
                containedStacks.Add(stack);

                // Sort on adding a new fluid.
                containedStacks.Sort((a, b) =>
                {
                    return string.Compare(a.fluid.code, b.fluid.code, StringComparison.Ordinal);
                });
            }
        }

    Finish:
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