namespace Fishing3;

/// <summary>
/// Something that has a default output fluid container.
/// </summary>
public interface IFluidSource
{
    FluidContainer GetSource(int index);
    void MarkContainerDirty();
}