namespace Fishing;

[Effect]
public class MetaEffectAmplifier : AlchemyEffect, IMetaEffect
{
    public float BaseRatio => 1f;

    public void ApplyTo(Effect effect, float ratioStrengthMultiplier)
    {
        if (effect is not IStrength strength) return;

        float multiplier = 1f + (StrengthMultiplier * ratioStrengthMultiplier);
        strength.StrengthMultiplier *= multiplier;
    }
}