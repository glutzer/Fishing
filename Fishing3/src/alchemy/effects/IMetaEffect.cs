namespace Fishing3;

public interface IMetaEffect
{
    /// <summary>
    /// Ratio of a meta effect to fluid to apply at full strength.
    /// Linear increase up to 1.
    /// </summary>
    float BaseRatio { get; }

    /// <summary>
    /// Apply a meta effect, with a strength multiplier calculated from the ratio MetaFluid / ReagentFluid, up to the BaseRatio at 1.
    /// </summary>
    void ApplyTo(Effect effect, float ratioStrengthMultiplier);
}