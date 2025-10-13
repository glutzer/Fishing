namespace Fishing;

/// <summary>
/// Something that has a default input fluid container.
/// </summary>
public interface IFluidSink
{
    FluidContainer GetSink(int index);
    void MarkContainerDirty();
}