using System.Collections.Generic;

namespace Fishing3;

public class WeightedCatch : ITierable, IWeightable
{
    private readonly int tier;
    private readonly float weight;

    public readonly Catchable catchable;
    public readonly string code;

    /// <summary>
    /// Return a weighted catch to be chosen by the fishing loot system.
    /// </summary>
    /// <param name="catchable">The catchable that created this.</param>
    /// <param name="weight">The weight of this being chosen at the tier.</param>
    /// <param name="tier">The tier this will be chosen at. Higher is rarer, chance halved every tier.</param>
    /// <param name="code">The code passed back into the ICatchable when caught.</param>
    public WeightedCatch(Catchable catchable, float weight, int tier, string code)
    {
        this.catchable = catchable;
        this.weight = weight;
        this.tier = tier;
        this.code = code;
    }

    /// <summary>
    /// Set a multiplier for tags, present in the fishing context.
    /// </summary>
    private float multiplier = 1f;
    public readonly HashSet<string> tags = new();

    public void SetMultiplier(Dictionary<string, float> tagMultipliers)
    {
        multiplier = 1f;

        foreach (string tag in tags)
        {
            if (tagMultipliers.TryGetValue(tag, out float multiplier))
            {
                this.multiplier *= multiplier;
            }
        }
    }

    public WeightedCatch WithTag(string tag)
    {
        tags.Add(tag);
        return this;
    }

    public int Tier => tier;
    public float Weight => weight * multiplier;
}