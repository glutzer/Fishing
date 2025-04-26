using MareLib;
using System;
using System.Collections.Generic;
using System.Linq;
using Vintagestory.API.Common;
using Vintagestory.API.Server;

namespace Fishing3;

[Catchable]
public class CatchableFish : Catchable
{
    public readonly FishSpeciesSystem fishSpeciesSystem;

    public CatchableFish(ICoreServerAPI sapi) : base(sapi)
    {
        fishSpeciesSystem = MainAPI.GetGameSystem<FishSpeciesSystem>(sapi.Side);
    }

    public override CaughtInstance Catch(FishingContext context, WeightedCatch weightedCatch, ICoreServerAPI sapi)
    {
        if (!fishSpeciesSystem.TryGetSpecies(weightedCatch.code, out FishSpecies? species)) throw new Exception($"Supplied a code {weightedCatch.code} for a fish, but was not found when catching");

        double kg = species.baseKg;

        // Random multiplier, 1-3 weighted towards 1.
        float sizeMultiplier = 1f + (MathF.Pow(Random.Shared.NextSingle(), 3) * 2);

        // Cubed increase of size as temperature lowers. 45% larger at 0 temperature. (May roll up to 6x size here).
        float northernSizeMultiplier = Math.Clamp(60f - (context.temperature + 20f), 0f, 60f) / 60f;
        sizeMultiplier *= 1f + MathF.Pow(northernSizeMultiplier, 3);

        // Logarithmic rarity scaling.
        float rarityMultiplier = DRUtility.CalculateDR(context.RarityMultiplier, 1f, 1f);
        sizeMultiplier *= rarityMultiplier;

        kg *= sizeMultiplier;
        kg = Math.Round(kg, 2);

        float speed = 3f + (Random.Shared.NextSingle() * 3f);
        float secondsOfStamina = 20f + (Random.Shared.NextSingle() * 20f);

        ItemStack stack = species.CreateStack(sapi, kg);
        CaughtInstance instance = new(stack, (float)kg, speed, secondsOfStamina);

        // Add fish sound?
        //instance.OnCaught += vector =>
        //{
        //    sapi.World.PlaySoundAt();
        //};

        return instance;
    }

    public override IEnumerable<WeightedCatch> GetCatches(FishingContext context, ICoreServerAPI sapi)
    {
        string liquid = context.liquid.FirstCodePart();

        return fishSpeciesSystem.SpeciesAlphabetical
            .Where(x =>
            {
                if (x.tempRange.X > context.temperature || x.tempRange.Y < context.temperature) return false;
                if (!x.liquids.Contains(liquid)) return false;
                if (x.riverOnly && !context.isRiver) return false;

                return true;
            })
            .Select(x => new WeightedCatch(this, x.weight, x.tier, x.code).WithTag("fish"));
    }
}