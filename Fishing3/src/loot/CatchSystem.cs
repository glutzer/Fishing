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
    public TierChooser tierChooser = new(0.5f);

    private const float BASE_BITE_TIME = 40f;

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
    /// On the server, get time for a bite.
    /// </summary>
    public static float GetTimeToBite(Vector3d position, Entity? caster)
    {
        FishingContext context = new(MainAPI.Sapi, position, caster);

#if DEBUG
        Console.WriteLine("###");
        Console.WriteLine($"Rarity: {context.RarityMultiplier}");
        Console.WriteLine($"Quantity: {context.QuantityMultiplier}");
        Console.WriteLine($"Temperature: {context.temperature}");
#endif

        float time = BASE_BITE_TIME / Math.Clamp(context.QuantityMultiplier, 0.1f, 2f);

        // 0.5-1.5x multiplier on time.
        // Max time: 600 seconds. Min time: 10 seconds.
        time *= 0.5f + Random.Shared.NextSingle();

        return time;
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

        // Set tag multipliers (like more sharks, flotsam).
        foreach (WeightedCatch catchable in potentialCatches)
        {
            catchable.SetMultiplier(context.tagMultipliers);
        }

        // Roll one.
        WeightedCatch? rolledCatch = tierChooser.RollItem(potentialCatches, context.RarityMultiplier, -1);
        if (rolledCatch == null) return null;

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