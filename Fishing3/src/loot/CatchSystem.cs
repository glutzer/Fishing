using MareLib;
using OpenTK.Mathematics;
using System;
using System.Collections.Generic;
using Vintagestory.API.Common;
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
            Catchable catchable = (Catchable)Activator.CreateInstance(type)!;
            catchables.Add(catchable);
        }
    }

    /// <summary>
    /// Try to roll a catch, returns null if nothing is rolled.
    /// </summary>
    public CaughtInstance? RollCatch(Vector3d position)
    {
        // Get data about bobber position.
        FishingContext context = new(MainAPI.Sapi, new BlockPos((int)position.X, (int)position.Y, (int)position.Z));

        // Get potential catches.
        List<WeightedCatch> potentialCatches = new(256);
        foreach (Catchable catchable in catchables)
        {
            potentialCatches.AddRange(catchable.GetCatches(context, MainAPI.Sapi));
        }

        // Roll one.
        WeightedCatch? rolledCatch = tierChooser.RollItem(potentialCatches, 1f);
        if (rolledCatch == null) return null;

        // Create it.
        CaughtInstance caughtInstance = rolledCatch.catchable.Catch(context, rolledCatch, MainAPI.Sapi);

        return caughtInstance;
    }
}