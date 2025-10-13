using System;
using System.Collections.Generic;
using System.Text;
using Vintagestory.API.Common;
using Vintagestory.API.Datastructures;

namespace Fishing;

public class CollectibleBehaviorBait : CollectibleBehavior
{
    // Chance for this bait to be consumed.
    public float consumeChance = 1f;

    // Instead has a durability, won't use chance.
    public bool Lure { get; private set; }

    public float extraRarity = 0f;
    public float extraQuantity = 0f;

    public float rarityMultiplier = 1f;
    public float quantityMultiplier = 1f;

    // Weight multiplier for catches with tag.
    public Dictionary<string, float> tagMultipliers = [];

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
            Lure = properties["lure"].AsBool(false);
        }

        if (properties["tagMultipliers"].Exists)
        {
            tagMultipliers = properties["tagMultipliers"].AsObject<Dictionary<string, float>>();
        }

        if (properties["extraRarity"].Exists)
        {
            extraRarity = properties["extraRarity"].AsFloat(0f);
        }

        if (properties["extraQuantity"].Exists)
        {
            extraQuantity = properties["extraQuantity"].AsFloat(0f);
        }

        if (properties["rarityMultiplier"].Exists)
        {
            rarityMultiplier = properties["rarityMultiplier"].AsFloat(1f);
        }

        if (properties["quantityMultiplier"].Exists)
        {
            quantityMultiplier = properties["quantityMultiplier"].AsFloat(1f);
        }
    }

    /// <summary>
    /// Try to consume from a stack, return if stack still exists.
    /// </summary>
    public bool ConsumeBait(ItemStack stack)
    {
        if (Lure) // Remove 1 durability.
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
        if (!Lure)
        {
            builder.AppendLine($"Bait consumption chance: {consumeChance * 100}%");
        }
        else
        {
            builder.AppendLine($"Bait lure");
        }

        if (extraRarity != 0)
        {
            builder.AppendLine(extraRarity > 0 ? $"+{extraRarity} rarity" : $"-{-extraRarity} rarity");
        }

        if (extraQuantity != 0)
        {
            builder.AppendLine(extraQuantity > 0 ? $"+{extraQuantity} quantity" : $"-{-extraQuantity} quantity");
        }

        if (rarityMultiplier != 1f)
        {
            builder.AppendLine($"x{rarityMultiplier} rarity");
        }

        if (quantityMultiplier != 1f)
        {
            builder.AppendLine($"x{quantityMultiplier} quantity");
        }

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