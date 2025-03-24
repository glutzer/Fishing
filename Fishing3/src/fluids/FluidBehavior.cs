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
    /// Register events when this behavior is created.
    /// </summary>
    public virtual void RegisterEvents(Fluid fluid)
    {

    }
}