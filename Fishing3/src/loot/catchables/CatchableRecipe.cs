using MareLib;
using System.Collections.Generic;
using Vintagestory.API.Common;
using Vintagestory.API.Server;

namespace Fishing3;

[Catchable]
public class CatchableRecipe : Catchable
{
    public AlchemyRecipeRegistry alchemyRecipeRegistry;

    public CatchableRecipe(ICoreServerAPI sapi) : base(sapi)
    {
        alchemyRecipeRegistry = MainAPI.GetGameSystem<AlchemyRecipeRegistry>(sapi.Side);
    }

    public override CaughtInstance Catch(FishingContext context, WeightedCatch weightedCatch, ICoreServerAPI sapi)
    {
        ItemStack parchment = alchemyRecipeRegistry.GenerateRandomParchment();
        return new CaughtInstance(parchment, 2f, 0f, 0f);
    }

    public override IEnumerable<WeightedCatch> GetCatches(FishingContext context, ICoreServerAPI sapi)
    {
        // What should the weight of catching one of these be? Maybe change depending on context, or yield nothing?
        yield return new WeightedCatch(this, 1f, 0, "recipe");
    }
}