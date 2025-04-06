using MareLib;
using OpenTK.Mathematics;
using System;
using System.IO;
using System.Text;
using Vintagestory.API.Common;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;

namespace Fishing3;

/// <summary>
/// Base fluid stack class.
/// Acts as a container.
/// </summary>
public class FluidStack
{
    protected int units = 0;
    public readonly Fluid fluid;

    public TreeAttribute Attributes { get; } = new();

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

    public virtual bool CanTakeFrom(FluidStack other)
    {
        return fluid.EventCanTakeFrom.Invoke((other, this));
    }

    /// <summary>
    /// Tries to take from other stack, returns amount taken.
    /// Must call OnTakenFrom from the other stack after transfer completes.
    /// </summary>
    public virtual int TakeFrom(FluidStack other, int maxUnits)
    {
        maxUnits = Math.Min(other.units, maxUnits);

        fluid.EventBeforeFluidAddedToOwnStack.Invoke((other, this, maxUnits));

        // Add and subtract units.
        other.units -= maxUnits;
        units += maxUnits;

        other.OnTakenFrom(maxUnits);
        return maxUnits;
    }

    /// <summary>
    /// Called when this stack is taken from by TakeFrom.
    /// Called regardless of amount taken, may have 0 units.
    /// </summary>
    public virtual void OnTakenFrom(int units)
    {

    }

    /// <summary>
    /// Append misc information about this fluid.
    /// Example: if a potion fluid has a special property, like blood type, append it here to make it evident why it can't merge.
    /// </summary>
    public virtual void GetFluidInfo(StringBuilder builder)
    {
        Vector4 fluidColor = fluid.GetColor(this);
        string hex = ColorUtil.Doubles2Hex(new double[] { fluidColor.X, fluidColor.Y, fluidColor.Z });
        builder.AppendLine($"{units}mL of <font color=\"{hex}\">{fluid.GetName(this)}</font>");

        fluid.EventGetFluidInfo.Invoke((builder, this));
    }

    public virtual void ToBytes(BinaryWriter writer)
    {
        writer.Write(units);
        Attributes.ToBytes(writer);
    }

    public virtual void FromBytes(BinaryReader reader, EnumAppSide side)
    {
        units = reader.ReadInt32();
        Attributes.FromBytes(reader);
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