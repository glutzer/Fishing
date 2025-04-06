namespace Fishing3;

/// <summary>
/// Something that has a default input fluid container.
/// </summary>
public interface IFluidSink
{
    public FluidContainer GetSink(int index);
    public void MarkContainerDirty();
}