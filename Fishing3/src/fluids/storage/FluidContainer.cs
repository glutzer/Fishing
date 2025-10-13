using System;
using Vintagestory.API.Common;

namespace Fishing;

/// <summary>
/// Holds a fluid stack and has a capacity.
/// </summary>
public class FluidContainer
{
    public int Capacity { get; private set; }
    public int RoomUsed => HeldStack?.Units ?? 0;
    public int RoomLeft => Capacity - RoomUsed;
    public bool Empty => HeldStack == null || HeldStack.Units <= 0;
    public float FillPercent => Capacity == 0 ? 0 : (float)RoomUsed / Capacity;

    public FluidStack? HeldStack { get; private set; }

    public FluidContainer(int capacity)
    {
        Capacity = capacity;
    }

    public void SetCapacity(int capacity)
    {
        Capacity = capacity;
    }

    public void SetStack(FluidStack stack)
    {
        HeldStack = stack;
    }

    /// <summary>
    /// Load a stack from bytes.
    /// </summary>
    public void LoadStack(byte[] bytes, EnumAppSide side)
    {
        if (bytes.Length == 0) return;
        HeldStack = FluidStack.Load(bytes, side);
    }

    /// <summary>
    /// Save the stack to bytes.
    /// </summary>
    public byte[] SaveStack()
    {
        return HeldStack == null ? Array.Empty<byte>() : FluidStack.Save(HeldStack);
    }

    public void EmptyContainer()
    {
        HeldStack = null;
    }

    public virtual bool CanReceiveFluid(FluidStack stack)
    {
        if (HeldStack == null) return true; // Can take anything.

        if (!HeldStack.fluid.EventCanTakeFrom.Invoke((stack, HeldStack))) return false; // Can't receive this stack.

        return true;
    }

    /// <summary>
    /// Does this container have room for this stack?
    /// </summary>
    public virtual bool HasRoomFor(FluidStack stack)
    {
        return RoomLeft - stack.Units >= 0;
    }

    /// <summary>
    /// Tries to move fluids between 2 containers.
    /// Returns amount moved.
    /// </summary>
    public static int MoveFluids(FluidContainer source, FluidContainer destination, int units = int.MaxValue)
    {
        if (source.HeldStack == null || destination.RoomLeft <= 0) return 0;

        if (!destination.CanReceiveFluid(source.HeldStack)) return 0;

        // Incompatible fluids.
        if (destination.HeldStack != null && !destination.HeldStack.CanTakeFrom(source.HeldStack)) return 0;

        destination.HeldStack ??= source.HeldStack.fluid.CreateFluidStack();

        units = Math.Min(units, destination.RoomLeft);

        // Source should have atleast 1 unit now.
        int moved = destination.HeldStack.TakeFrom(source.HeldStack, units);

        source.CheckIfStackEmpty();

        return moved;
    }

    /// <summary>
    /// Tries to move fluids between 2 containers.
    /// Returns amount moved.
    /// </summary>
    public int MoveFluidsTo(FluidContainer destination, int units = int.MaxValue)
    {
        return MoveFluids(this, destination, units);
    }

    /// <summary>
    /// Tries to move fluids from a stack.
    /// Returns amount moved.
    /// </summary>
    public static int MoveFluids(FluidStack sourceStack, FluidContainer destination, int units = int.MaxValue)
    {
        if (destination.RoomLeft <= 0) return 0;

        if (!destination.CanReceiveFluid(sourceStack)) return 0;

        // Incompatible fluids.
        if (destination.HeldStack != null && !destination.HeldStack.CanTakeFrom(sourceStack)) return 0;

        destination.HeldStack ??= sourceStack.fluid.CreateFluidStack();

        units = Math.Min(units, destination.RoomLeft);

        // Source should have atleast 1 unit now.
        int moved = destination.HeldStack.TakeFrom(sourceStack, units);

        return moved;
    }

    /// <summary>
    /// Returns a fluid stack if able to take out atleast 1.
    /// </summary>
    public FluidStack? TakeOut(int amount = int.MaxValue)
    {
        if (HeldStack == null) return null;
        FluidStack newStack = HeldStack.fluid.CreateFluidStack();
        if (!newStack.CanTakeFrom(HeldStack)) return null;

        newStack.TakeFrom(HeldStack, amount);

        CheckIfStackEmpty();

        return newStack.Units <= 0 ? null : newStack;
    }

    /// <summary>
    /// Call when a stack no longer exists.
    /// </summary>
    public void CheckIfStackEmpty()
    {
        if (HeldStack?.Units <= 0) HeldStack = null;
    }

    public virtual FluidContainer Copy(EnumAppSide side)
    {
        byte[] bytes = SaveStack();
        FluidContainer copy = new(Capacity);
        copy.LoadStack(bytes, side);
        return copy;
    }
}