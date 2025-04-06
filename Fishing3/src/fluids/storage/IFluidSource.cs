namespace Fishing3;

/// <summary>
/// Something that has a default output fluid container.
/// </summary>
public interface IFluidSource
{
    public FluidContainer GetSource(int index);
    public void MarkContainerDirty();
}