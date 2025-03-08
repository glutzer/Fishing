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
    public T? RollItem<T>(List<T> validItems, float rarityMultiplier) where T : ITierable, IWeightable
    {
        List<T> rolledtiers = RollTier(validItems, rarityMultiplier);
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
    /// </summary>
    public List<T> RollTier<T>(List<T> validItems, float rarityMultiplier) where T : ITierable
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

        int rolledTier = 0;

        for (int i = highestTier; i > 0; i--)
        {
            if (!availableTiers.Contains(i)) continue;
            float chance = MathF.Pow(divisionPerTier, i);
            chance *= rarityMultiplier; // Maybe add DR for rarity here?
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