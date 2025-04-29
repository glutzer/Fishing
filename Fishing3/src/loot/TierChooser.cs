using System;
using System.Collections.Generic;
using System.Linq;

namespace Fishing3;

public interface ITierable
{
    /// <summary>
    /// Tier of item, starting at 0 and getting rarer.
    /// </summary>
    int Tier { get; }
}

public interface IWeightable
{
    /// <summary>
    /// Weight of item, higher is more common.
    /// </summary>
    float Weight { get; }
}

/// <summary>
/// Takes in a list of ITierable items and removes invalids.
/// </summary>
public class TierChooser
{
    public float baseUpgradeChance;

    /// <summary>
    /// Create a new tier chooser.
    /// Tier chance is multiplied by rarity.
    /// </summary>
    /// <param name="divisionPerTier">Base chance to upgrade a tier. 10% for t1, 1% for t2, 0.1% for t3. -> 20% for t1, 4% for t2, 0.8% for t3. 2x rarity = 8x as many t4.</param>
    public TierChooser(float baseUpgradeChance = 0.1f)
    {
        this.baseUpgradeChance = baseUpgradeChance;
    }

    /// <summary>
    /// Takes tiered and weighted items, rolls one.
    /// If no tiers are rolled, returns null.
    /// </summary>
    public T? RollItem<T>(List<T> validItems, float rarityMultiplier) where T : ITierable, IWeightable
    {
        List<T> rolledtiers = RollTier(validItems, rarityMultiplier);
        return rolledtiers.Count == 0 ? default : RollWeightedList(rolledtiers);
    }

    /// <summary>
    /// Takes a list of weighted items and chooses one.
    /// Must have a count of atleast 1.
    /// </summary>
    public static T RollWeightedList<T>(List<T> weightedItems) where T : IWeightable
    {
        float totalWeight = weightedItems.Sum(item => item.Weight);
        float roll = Random.Shared.NextSingle() * totalWeight;
        foreach (T item in weightedItems)
        {
            roll -= item.Weight;
            if (roll <= 0)
            {
                return item;
            }
        }
        return weightedItems[0];
    }

    /// <summary>
    /// Rolls a tier of item.
    /// Will always return something if items are supplied.
    /// </summary>
    public List<T> RollTier<T>(List<T> validItems, float rarityMultiplier) where T : ITierable
    {
        HashSet<int> availableTiers = new();

        foreach (T item in validItems)
        {
            availableTiers.Add(item.Tier);
        }

        int highestRolledTier = 0;
        float tierUpgradeChance = baseUpgradeChance * rarityMultiplier;

        // Roll up to tier 10.
        for (int i = 1; i <= 10; i++)
        {
            if (Random.Shared.NextSingle() > tierUpgradeChance) break;
            highestRolledTier++;
        }

        // Get the highest tier in available tiers that's equal or lower to the highest rolled tier.
        int highestAvailableTier = availableTiers.Where(tier => tier <= highestRolledTier).DefaultIfEmpty(-1).Max();

        return validItems.Where(item => item.Tier == highestAvailableTier).ToList();
    }

    /// <summary>
    /// Write the chance to roll every item, and the tier it's in.
    /// For debug only.
    /// </summary>
    public void PrintChances<T>(List<T> validItems, float rarityMultiplier, Func<T, string> getIdentifier) where T : ITierable, IWeightable
    {
        Console.WriteLine();
        Console.WriteLine("--- TIER CHANCES ---");

        // Group items by tier.
        IOrderedEnumerable<IGrouping<int, T>> itemsByTier = validItems
            .GroupBy(item => item.Tier)
            .OrderBy(group => group.Key);

        foreach (IGrouping<int, T>? tierGroup in itemsByTier)
        {
            int tier = tierGroup.Key;
            if (tier > 10) continue; // Max tier is 10.

            List<T> items = tierGroup.ToList();

            // Calculate the chance to roll this tier.
            float tierChance = baseUpgradeChance * rarityMultiplier;
            float cumulativeChance = 1f;
            for (int i = 0; i < tier; i++)
            {
                cumulativeChance *= tierChance;
            }
            float chanceToRollTier = cumulativeChance * (1 - tierChance);

            Console.WriteLine($"Tier {tier}: {chanceToRollTier:P2}");

            // Calculate and print the chance for each item in this tier.
            float totalWeight = items.Sum(item => item.Weight);
            foreach (T? item in items)
            {
                float itemChance = item.Weight / totalWeight * chanceToRollTier;
                Console.WriteLine($"  Item: {getIdentifier(item)}, Chance: {itemChance:P2}");
            }
        }
    }
}