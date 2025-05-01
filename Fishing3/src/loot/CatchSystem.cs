using MareLib;
using OpenTK.Mathematics;
using System;
using System.Collections.Generic;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;

namespace Fishing3;

[GameSystem]
public class CatchSystem : GameSystem
{
    /// <summary>
    /// Catchable singletons for rolling loot.
    /// </summary>
    public readonly List<Catchable> catchables = new();

    // Each tier has half the chance to appear as the last one.
    public TierChooser tierChooser = new(0.1f);

    // Config later.
    public static float BiteTimeMultiplier => 1f;

    public CatchSystem(bool isServer, ICoreAPI api) : base(isServer, api)
    {
    }

    public T GetCatchable<T>() where T : Catchable
    {
        foreach (Catchable catchable in catchables)
        {
            if (catchable is T t) return t;
        }
        throw new InvalidOperationException($"No catchable of type {typeof(T)} found.");
    }

    public override void OnAssetsLoaded()
    {
        if (api.Side == EnumAppSide.Client) return;

        // Sorted list of classes.
        (Type, CatchableAttribute)[] attribs = AttributeUtilities.GetAllAnnotatedClasses<CatchableAttribute>();

        foreach ((Type type, CatchableAttribute _) in attribs)
        {
            Catchable catchable = (Catchable)Activator.CreateInstance(type, MainAPI.Sapi)!;
            catchables.Add(catchable);
        }
    }

    /// <summary>
    /// What the delta should be multiplied by to get the time it takes for a fish to bite.
    /// Rolls a new fishing context.
    /// </summary>
    public static float GetBiteSpeedMultiplier(Vector3d position, Entity? caster)
    {
        FishingContext context = new(MainAPI.Sapi, position, caster);

#if DEBUG
        Console.WriteLine("###");
        Console.WriteLine($"Rarity: {context.RarityMultiplier}");
        Console.WriteLine($"Quantity: {context.QuantityMultiplier}");
        Console.WriteLine($"Temperature: {context.temperature}");
#endif

        return BiteTimeMultiplier * context.QuantityMultiplier;
    }

    /// <summary>
    /// Additional bite speed multiplier from player distance.
    /// </summary>
    public static float GetPlayerDistanceBiteSpeedMultiplier(Vector3d position, Entity caster)
    {
        // 0-1 quantity up to 25m away, then logarithmic.
        float distance = (float)Vector3d.Distance(position, caster.ServerPos.ToVector());
        float distanceMultiplier = DrUtility.CalculateDr(distance, 25f, 1f) / 25f;
        return distanceMultiplier;
    }

    /// <summary>
    /// Try to roll a catch, returns null if nothing is rolled.
    /// Does a large flood fill, do not roll too often.
    /// </summary>
    public CaughtInstance? RollCatch(Vector3d position, Entity? caster)
    {
        // Get data about bobber position.
        FishingContext context = new(MainAPI.Sapi, position, caster);

        // Get potential catches.
        List<WeightedCatch> potentialCatches = new(256);
        foreach (Catchable catchable in catchables)
        {
            potentialCatches.AddRange(catchable.GetCatches(context, MainAPI.Sapi));
        }

#if DEBUG
        tierChooser.PrintChances(potentialCatches, context.RarityMultiplier, x => x.code);
#endif

        // Set tag multipliers (like more sharks, flotsam).
        foreach (WeightedCatch catchable in potentialCatches)
        {
            catchable.SetMultiplier(context.tagMultipliers);
        }

        // Roll one.
        WeightedCatch? rolledCatch = tierChooser.RollItem(potentialCatches, context.RarityMultiplier);

        if (caster?.IsLucky() == true)
        {
            WeightedCatch? luckyCatch = tierChooser.RollItem(potentialCatches, context.RarityMultiplier);
            int originalTier = rolledCatch?.Tier ?? -1;
            if (luckyCatch != null && luckyCatch.Tier > originalTier)
            {
                // Problem: will catch TOO many good things now.
                rolledCatch = luckyCatch;
            }
        }

        if (rolledCatch == null) return null;

#if DEBUG
        Console.WriteLine("### ROLLED CATCH");
        Console.WriteLine($"Code: {rolledCatch.code}, Weight: {rolledCatch.Weight}, Tier: {rolledCatch.Tier}");
#endif

        // Consume bait.
        ConsumeBait(caster);

        // Create it.
        CaughtInstance caughtInstance = rolledCatch.catchable.Catch(context, rolledCatch, MainAPI.Sapi);

        return caughtInstance;
    }

    public void ConsumeBait(Entity? entity)
    {
        if (entity is not EntityPlayer player) return;
        ItemSlot hotbarSlot = player.Player.InventoryManager.ActiveHotbarSlot;
        if (hotbarSlot.Itemstack == null || hotbarSlot.Itemstack.Collectible is not ItemFishingPole) return;
        ItemFishingPole.ReadStack(2, hotbarSlot.Itemstack, api, out ItemStack? baitStack);
        if (baitStack == null) return;
        CollectibleBehaviorBait? collectibleBehaviorBait = baitStack.Collectible.GetBehavior<CollectibleBehaviorBait>();
        if (collectibleBehaviorBait == null) return;

        if (collectibleBehaviorBait.lure)
        {
            ItemFishingPole.DamageStack(2, hotbarSlot, api, 1);
        }
        else if (Random.Shared.NextSingle() < collectibleBehaviorBait.consumeChance)
        {
            baitStack.StackSize--;
            ItemFishingPole.SetStack(2, hotbarSlot.Itemstack, baitStack.StackSize <= 0 ? null : baitStack);
        }
    }
}