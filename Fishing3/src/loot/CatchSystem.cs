using Cairo;
using MareLib;
using OpenTK.Mathematics;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.MathTools;

namespace Fishing3;

[GameSystem]
public class CatchSystem : GameSystem
{
    /// <summary>
    /// Catchable singletons for rolling loot.
    /// </summary>
    public readonly List<Catchable> catchables = new();

    // Each tier has half the chance to appear as the last one.
    public TierChooser tierChooser = new(0.5f);

    public CatchSystem(bool isServer, ICoreAPI api) : base(isServer, api)
    {
    }

    public override void OnAssetsLoaded()
    {
        // Sorted list of classes.
        (Type, CatchableAttribute)[] attribs = AttributeUtilities.GetAllAnnotatedClasses<CatchableAttribute>();

        foreach ((Type type, CatchableAttribute _) in attribs)
        {
            Catchable catchable = (Catchable)Activator.CreateInstance(type, MainAPI.Sapi)!;
            catchables.Add(catchable);
        }
    }

    /// <summary>
    /// Try to roll a catch, returns null if nothing is rolled.
    /// Does a large flood fill, do not roll too often.
    /// </summary>
    public CaughtInstance? RollCatch(Vector3d position, Entity caster)
    {
        // Get data about bobber position.
        FishingContext context = new(MainAPI.Sapi, new BlockPos((int)position.X, (int)position.Y, (int)position.Z));

        // Get potential catches.
        List<WeightedCatch> potentialCatches = new(256);
        foreach (Catchable catchable in catchables)
        {
            potentialCatches.AddRange(catchable.GetCatches(context, MainAPI.Sapi));
        }

        // Squared increase, then logarithmic dropoff based on fluid volume. TODO: include different values for different fluids.
        float rarityMultiplier = DRUtility.CalculateDR(context.volume, 2000f, 1.5f) / 2000f; //~1.45 rarity multiplier at max. 
        if (rarityMultiplier < 1f) rarityMultiplier *= rarityMultiplier;

        // 30% more rarity in stormy weathers.
        rarityMultiplier *= 1f + context.precipitation * 0.3f;

        // Multiplier from classes or effects.
        rarityMultiplier *= caster.Stats.GetBlended("fishRarity");

        // Linear multiplier up to 25 distance, then logarithmic.
        float distance = (float)Vector3d.Distance(position, caster.ServerPos.ToVector());
        float distanceMultiplier = DRUtility.CalculateDR(distance, 25f, 1f) / 25f;
        rarityMultiplier *= distanceMultiplier;

        context.rarityMultiplier = rarityMultiplier;

        // Roll one.
        WeightedCatch? rolledCatch = tierChooser.RollItem(potentialCatches, rarityMultiplier, -1);
        if (rolledCatch == null) return null;

        // Create it.
        CaughtInstance caughtInstance = rolledCatch.catchable.Catch(context, rolledCatch, MainAPI.Sapi);

        return caughtInstance;
    }
}