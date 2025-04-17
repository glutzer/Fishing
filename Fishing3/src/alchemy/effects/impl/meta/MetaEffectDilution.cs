using MareLib;
using System;

namespace Fishing3;

[Effect]
public class MetaEffectDilution : AlchemyEffect, IMetaEffect
{
    public float BaseRatio => 1f;

    public void ApplyTo(Effect effect, float ratioStrengthMultiplier)
    {
        if (effect is not AlchemyEffect alchemyEffect) return;

        float multiplier = alchemyEffect.Units / Units;
        multiplier = Math.Clamp(multiplier, 0f, 1f);

        alchemyEffect.StrengthMultiplier *= multiplier;
    }
}