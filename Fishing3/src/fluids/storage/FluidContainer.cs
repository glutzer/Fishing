using System;

namespace Fishing3;

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

    public void EmptyContainer()
    {
        HeldStack = null;
    }

    public virtual bool CanReceiveFluid(FluidStack stack)
    {
        return true;
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
    public static int MoveFluids(FluidStack heldStack, FluidContainer destination, int units = int.MaxValue)
    {
        if (destination.RoomLeft <= 0) return 0;

        if (!destination.CanReceiveFluid(heldStack)) return 0;

        // Incompatible fluids.
        if (destination.HeldStack != null && !destination.HeldStack.CanTakeFrom(heldStack)) return 0;

        destination.HeldStack ??= heldStack.fluid.CreateFluidStack();

        units = Math.Min(units, destination.RoomLeft);

        // Source should have atleast 1 unit now.
        int moved = destination.HeldStack.TakeFrom(heldStack, units);

        return moved;
    }

    /// <summary>
    /// Call when a stack no longer exists.
    /// </summary>
    public void CheckIfStackEmpty()
    {
        if (HeldStack?.Units <= 0) HeldStack = null;
    }
}