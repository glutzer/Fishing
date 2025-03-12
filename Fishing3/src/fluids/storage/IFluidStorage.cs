namespace Fishing3;

/// <summary>
/// Anything that can hold fluids, including items.
/// </summary>
public interface IFluidStorage
{
    public FluidContainer Container { get; }
}