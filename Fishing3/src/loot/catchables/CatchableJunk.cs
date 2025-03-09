using System.Collections.Generic;
using Vintagestory.API.Common;
using Vintagestory.API.Server;

namespace Fishing3;

[Catchable]
public class CatchableJunk : Catchable
{
    public CatchableJunk(ICoreServerAPI sapi) : base(sapi)
    {
    }

    public override CaughtInstance Catch(FishingContext context, WeightedCatch weightedCatch, ICoreServerAPI sapi)
    {
        Item block = sapi.World.GetItem("fishing:line-linen");

        ItemStack stack = new(block, 1);
        stack.ResolveBlockOrItem(sapi.World);

        CaughtInstance inst = new(stack, 30, 10, 30);

        return inst;
    }

    public override IEnumerable<WeightedCatch> GetCatches(FishingContext context, ICoreServerAPI sapi)
    {
        WeightedCatch toCatch = new(this, 100, 0, "chud");
        yield return toCatch;
    }
}