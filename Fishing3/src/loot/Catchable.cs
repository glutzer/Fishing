using MareLib;
using System.Collections.Generic;
using Vintagestory.API.Server;

namespace Fishing3;

/// <summary>
/// Attribute for any Catchable to be registered.
/// </summary>
public class CatchableAttribute : ClassAttribute
{

}

/// <summary>
/// Abstract class for something that can be caught from fishing.
/// Only on the server.
/// </summary>
public abstract class Catchable
{
    protected Catchable()
    {

    }

    /// <summary>
    /// Return all things that can be caught given the conditions.
    /// These may be cached if necessary.
    /// May return nothing.
    /// </summary>
    public abstract IEnumerable<WeightedCatch> GetCatches(FishingContext context, ICoreServerAPI sapi);

    /// <summary>
    /// Once a weighted catch has been chosen, return a *new* caught instance.
    /// </summary>
    public abstract CaughtInstance Catch(FishingContext context, WeightedCatch weightedCatch, ICoreServerAPI sapi);

    // This is really all that's needed, maybe add data about it lower down.
}