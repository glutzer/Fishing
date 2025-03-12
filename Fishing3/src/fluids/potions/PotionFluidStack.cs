using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Vintagestory.API.Common;

namespace Fishing3;

public class PotionFluidStack : FluidStack
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

    public PotionFluidStack(Fluid fluid) : base(fluid)
    {
    }

    public override bool CanTakeFrom(FluidStack other)
    {
        return other is not PotionFluidStack;
    }

    public override int TakeFrom(FluidStack other, int maxUnits)
    {
        int initialUnits = maxUnits;

        foreach (FluidStack stack in containedStacks)
        {
            if (stack.CanTakeFrom(other))
            {
                maxUnits -= stack.TakeFrom(other, maxUnits);
            }
        }

        if (other.Units == 0) return initialUnits - maxUnits;

        if (maxUnits > 0)
        {
            FluidStack newStack = other.fluid.CreateFluidStack();

            if (newStack.CanTakeFrom(other))
            {
                maxUnits -= newStack.TakeFrom(other, maxUnits);
                containedStacks.Add(newStack);
            }
        }

        return initialUnits - maxUnits;
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