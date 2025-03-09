using System;
using System.Collections.Generic;
using System.Text;
using Vintagestory.API.Common;
using Vintagestory.API.Datastructures;

namespace Fishing3;

public class CollectibleBehaviorBait : CollectibleBehavior
{
    // Chance for this bait to be consumed.
    public float consumeChance = 1f;

    // Instead has a durability, won't use chance.
    public bool lure;

    // Weight multiplier for catches with tag.
    public Dictionary<string, float> tagMultipliers = new();

    public CollectibleBehaviorBait(CollectibleObject collObj) : base(collObj)
    {
    }

    public override void Initialize(JsonObject properties)
    {
        base.Initialize(properties);

        if (properties["consumeChance"].Exists)
        {
            consumeChance = properties["consumeChance"].AsFloat(1f);
        }

        if (properties["lure"].Exists)
        {
            lure = properties["lure"].AsBool(false);
        }

        if (properties["tagMultipliers"].Exists)
        {
            tagMultipliers = properties["tagMultipliers"].AsObject<Dictionary<string, float>>();
        }
    }

    /// <summary>
    /// Try to consume from a stack, return if stack still exists.
    /// </summary>
    public bool ConsumeBait(ItemStack stack)
    {
        if (lure) // Remove 1 durability.
        {
            int currentDurability = stack.Attributes.GetInt("durability", stack.Collectible.Durability);
            currentDurability -= 1;
            stack.Attributes.SetInt("durability", currentDurability);
            return currentDurability > 0;
        }
        else
        {
            float consumeRoll = Random.Shared.NextSingle();
            if (consumeRoll < consumeChance)
            {
                stack.StackSize--;
                return stack.StackSize > 0;
            }
            return true;
        }
    }

    public override void GetHeldItemInfo(ItemSlot inSlot, StringBuilder builder, IWorldAccessor world, bool withDebugInfo)
    {
        if (!lure)
        {
            builder.AppendLine($"Bait consumption chance: {consumeChance * 100}%");
        }
        else
        {
            builder.AppendLine($"Bait lure");
        }
        //builder.AppendLine($"Attractiveness: {biteChanceMultiplier}x");

        // Add tag multipliers to the tooltip
        foreach (KeyValuePair<string, float> tagMultiplier in tagMultipliers)
        {
            if (tagMultiplier.Value > 1f)
            {
                builder.AppendLine($"Attracts {tagMultiplier.Key}");
            }
            else if (tagMultiplier.Value < 1f)
            {
                builder.AppendLine($"Repels {tagMultiplier.Key}");
            }
        }
    }
}