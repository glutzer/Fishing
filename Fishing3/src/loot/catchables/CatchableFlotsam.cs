using System.Collections.Generic;
using System.Linq;
using Vintagestory.API.Common;
using Vintagestory.API.Server;

namespace Fishing3;

public class FlotsamJson
{
    public string[] liquids = new string[] { "water", "saltwater" };
    public string code = null!;
    public double[] tempRange = new double[] { -25, 45 };
    public float weight = 1f;
    public int tier = 0;
    public float kg = 50f;
}

public class WeightedFlotsam : WeightedCatch
{
    public float kg;

    public WeightedFlotsam(Catchable catchable, float weight, int tier, string code, float kg) : base(catchable, weight, tier, code)
    {
        this.kg = kg;
    }
}

[Catchable]
public class CatchableFlotsam : Catchable
{
    public List<FlotsamJson> flotsamList = new();

    public CatchableFlotsam(ICoreServerAPI sapi) : base(sapi)
    {
        List<IAsset> assets = sapi.Assets.GetMany("config/flotsam");
        foreach (IAsset asset in assets)
        {
            FlotsamJson? flotsam = asset.ToObject<FlotsamJson>();
            if (flotsam == null || flotsam.code == null) continue;
            flotsamList.Add(flotsam);
        }
        flotsamList.Sort((a, b) => a.code.CompareTo(b.code));
    }

    public override CaughtInstance Catch(FishingContext context, WeightedCatch weightedCatch, ICoreServerAPI sapi)
    {
        WeightedFlotsam flot = (WeightedFlotsam)weightedCatch;

        CollectibleObject? thing = sapi.World.GetItem(flot.code);
        thing ??= sapi.World.GetBlock(flot.code);

        ItemStack stack = new(thing, 1);
        stack.ResolveBlockOrItem(sapi.World);

        CaughtInstance inst = new(stack, flot.kg, 0, 0);

        return inst;
    }

    public override IEnumerable<WeightedCatch> GetCatches(FishingContext context, ICoreServerAPI sapi)
    {
        string liquid = context.liquid.FirstCodePart();
        return flotsamList
            .Where(x => context.temperature > x.tempRange[0] && context.temperature < x.tempRange[1] && x.liquids.Contains(liquid))
            .Select(x => new WeightedFlotsam(this, x.weight, x.tier, x.code, x.kg));
    }
}