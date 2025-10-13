namespace Fishing;

[Effect]
public class MetaEffectExtender : AlchemyEffect, IMetaEffect
{
    public float BaseRatio => 1f;

    public void ApplyTo(Effect effect, float ratioStrengthMultiplier)
    {
        float multiplier = 1f + (StrengthMultiplier * ratioStrengthMultiplier);
        effect.Duration *= multiplier;
    }
}