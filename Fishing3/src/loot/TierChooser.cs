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
    public float divisionPerTier;

    /// <summary>
    /// Create a new tier chooser.
    /// Tier chance is multiplied by rarity.
    /// </summary>
    /// <param name="divisionPerTier">With 0.5f division, 100% chance, 50% chance, 25% chance.</param>
    public TierChooser(float divisionPerTier = 0.5f)
    {
        this.divisionPerTier = divisionPerTier;
    }

    public void PrintTierChances<T>(List<T> items, float rarityMultiplier, bool luckyRoll) where T : ITierable, IWeightable
    {
        float totalWeight = 0;
        HashSet<int> availableTiers = new();
        foreach (T item in items)
        {
            if (availableTiers.Add(item.Tier))
            {

            }
        }
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

        float totalWeight = 0;
        List<(int tier, float weight)> tierWeights = new();

        foreach (int tier in availableTiers)
        {
            float chance = MathF.Pow(divisionPerTier, tier);
            chance *= rarityMultiplier;

            // Base line is 1 / i + 1, then drop-off is decreased with higher tiers.
            // Below 1 rarity, nothing may roll, or literal junk (-1 tier) may be chosen.
            chance = DRUtility.CalculateDR(chance, 1f / (tier + 1), 0.7f);

            totalWeight += chance;
            tierWeights.Add((tier, chance));
        }

        float roll = Random.Shared.NextSingle() * totalWeight;
        int chosenTier = -1;
        foreach ((int tier, float weight) in tierWeights)
        {
            roll -= weight;
            if (roll <= 0)
            {
                chosenTier = tier;
                break;
            }
        }

        return validItems.Where(item => item.Tier == chosenTier).ToList();
    }
}