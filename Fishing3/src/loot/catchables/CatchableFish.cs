using System.Collections.Generic;
using Vintagestory.API.Server;

namespace Fishing3;

[Catchable]
public class CatchableFish : Catchable
{
    public CatchableFish(ICoreServerAPI sapi) : base(sapi)
    {
    }

    public override CaughtInstance Catch(FishingContext context, WeightedCatch weightedCatch, ICoreServerAPI sapi)
    {
        return null;
    }

    public override IEnumerable<WeightedCatch> GetCatches(FishingContext context, ICoreServerAPI sapi)
    {
        yield break;
    }
}