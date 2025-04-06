using MareLib;
using Newtonsoft.Json;
using System.Text.Json.Nodes;

namespace Fishing3;

/// <summary>
/// Effect spawned from a reagent.
/// </summary>
public abstract class AlchemyEffect : Effect, IStrength
{
    /// <summary>
    /// Minimum units for this effect to be valid.
    /// </summary>
    public virtual int MinimumVolume => 1;

    /// <summary>
    /// Arbitrary value for the effect to multiply itself by.
    /// Increased by many things.
    /// </summary>
    [JsonProperty]
    public float StrengthMultiplier { get; set; } = 1f;

    /// <summary>
    /// How many units were used to apply this effect.
    /// Set by alchemy system and used to calculate strength of instant effects like healing.
    /// </summary>
    [JsonProperty]
    public int Units { get; set; }

    /// <summary>
    /// Collect data from the reagent which applied this effect on the server.
    /// </summary>
    public virtual void CollectDataFromFluidStack(FluidStack stack)
    {

    }

    /// <summary>
    /// Collect data from the reagent's Data property, if supplied.
    /// </summary>
    public virtual void CollectDataFromReagent(JsonObject data)
    {

    }
}