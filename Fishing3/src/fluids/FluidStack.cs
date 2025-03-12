using MareLib;
using System;
using System.IO;
using System.Text;
using Vintagestory.API.Common;

namespace Fishing3;

/// <summary>
/// Base fluid stack class.
/// Acts as a container.
/// </summary>
public class FluidStack
{
    protected int units = 0;
    public readonly Fluid fluid;

    /// <summary>
    /// Stack volume, limited by container.
    /// </summary>
    public virtual int Units
    {
        get => units;
        set => units = value;
    }

    public FluidStack(Fluid fluid)
    {
        this.fluid = fluid;
    }

    /// <summary>
    /// Can these stacks merge?
    /// For example with a solution stack: check if the properties of fluids match (like blood type or teleportation tag).
    /// </summary>
    public virtual bool CanTakeFrom(FluidStack other)
    {
        return other.fluid == fluid;
    }

    /// <summary>
    /// Tries to take from other stack, returns amount taken.
    /// </summary>
    public virtual int TakeFrom(FluidStack other, int maxUnits)
    {
        maxUnits = Math.Min(other.units, maxUnits);
        other.units -= maxUnits;
        units += maxUnits;
        return maxUnits;
    }

    /// <summary>
    /// Append misc information about this fluid.
    /// Stack size and name is already covered by container items.
    /// Example: if a potion fluid has a special property, like blood type, append it here to make it evident why it can't merge.
    /// </summary>
    public virtual void GetFluidInfo(StringBuilder builder)
    {
        builder.AppendLine($"{units} units of {fluid.code}");
    }

    public virtual void ToBytes(BinaryWriter writer)
    {
        writer.Write(units);
    }

    public virtual void FromBytes(BinaryReader reader, EnumAppSide side)
    {
        units = reader.ReadInt32();
    }

    public static byte[] Save(FluidStack stack)
    {
        using MemoryStream stream = new();
        using BinaryWriter writer = new(stream);
        writer.Write(stack.fluid.code);
        stack.ToBytes(writer);
        return stream.ToArray();
    }

    public static FluidStack? Load(byte[] data, EnumAppSide side)
    {
        try
        {
            using MemoryStream stream = new(data);
            using BinaryReader reader = new(stream);
            string code = reader.ReadString();
            Fluid fluid = MainAPI.GetGameSystem<FluidRegistry>(side).GetFluid(code);
            FluidStack stack = fluid.CreateFluidStack();
            stack.FromBytes(reader, side);
            return stack;
        }
        catch
        {
            return null; // Unable to load stack.
        }
    }

    public static void Save(FluidStack stack, BinaryWriter writer)
    {
        writer.Write(stack.fluid.code);
        stack.ToBytes(writer);
    }

    public static FluidStack? Load(BinaryReader reader, EnumAppSide side)
    {
        try
        {
            string code = reader.ReadString();
            Fluid fluid = MainAPI.GetGameSystem<FluidRegistry>(side).GetFluid(code);
            FluidStack stack = fluid.CreateFluidStack();
            stack.FromBytes(reader, side);
            return stack;
        }
        catch
        {
            return null; // Unable to load stack.
        }
    }
}