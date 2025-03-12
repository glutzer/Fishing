using System;
using System.Collections.Generic;
using System.Linq;

namespace Fishing3;

public interface ITierable
{
    /// <summary>
    /// Tier of item, starting at 0 and getting rarer.
    /// </summary>
    public int Tier { get; }
}

public interface IWeightable
{
    /// <summary>
    /// Weight of item, higher is more common.
    /// </summary>
    public float Weight { get; }
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

    /// <summary>
    /// Takes tiered and weighted items, rolls one.
    /// If no tiers are rolled, returns null.
    /// </summary>
    public T? RollItem<T>(List<T> validItems, float rarityMultiplier, int minTier = -1) where T : ITierable, IWeightable
    {
        List<T> rolledtiers = RollTier(validItems, rarityMultiplier, minTier);
        if (rolledtiers.Count == 0) return default;
        return RollWeightedList(rolledtiers);
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
    /// Removes all items from the list except the highest rolled tier.
    /// Must have a count of atleast 1.
    /// If NOTHING rolls min tier will be chosen (-1 for no default).
    /// </summary>
    public List<T> RollTier<T>(List<T> validItems, float rarityMultiplier, int minTier = -1) where T : ITierable
    {
        int highestTier = 0;
        HashSet<int> availableTiers = new();

        foreach (T item in validItems)
        {
            if (item.Tier > highestTier)
            {
                highestTier = item.Tier;
            }
            availableTiers.Add(item.Tier);
        }

        int rolledTier = minTier;

        for (int i = highestTier; i > minTier; i--)
        {
            if (!availableTiers.Contains(i)) continue;

            float chance = MathF.Pow(divisionPerTier, i);
            chance *= rarityMultiplier;

            // Base line is 1 / i + 1, then drop-off is decreased with higher tiers.
            // Below 1 rarity, nothing may roll, or literal junk (-1 tier) may be chosen.
            chance = DRUtility.CalculateDR(chance, 1 / (i + 1), 1f - (i * 0.1f));

            float rarityRoll = Random.Shared.NextSingle();
            if (rarityRoll <= chance)
            {
                rolledTier = i;
                break;
            }
        }

        return validItems.Where(item => item.Tier == rolledTier).ToList();
    }
}