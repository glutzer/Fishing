using System.Text;
using System.Text.Json.Nodes;

namespace Fishing3;

public abstract class FluidBehavior
{
#pragma warning disable IDE0060 // Remove unused parameter
    protected FluidBehavior(JsonObject data)
#pragma warning restore IDE0060 // Remove unused parameter
    {

    }

    /// <summary>
    /// Called before units are moved between stacks.
    /// Example: to merge stats of a fluid stack.
    /// Only called from the destination type.
    /// </summary>
    public virtual void BeforeFluidAddedToOwnStack(FluidStack sourceStack, FluidStack thisStack, int toMove)
    {

    }

    /// <summary>
    /// Append information about this fluid.
    /// </summary>
    public virtual void GetFluidInfo(StringBuilder builder, FluidStack stack)
    {

    }
}