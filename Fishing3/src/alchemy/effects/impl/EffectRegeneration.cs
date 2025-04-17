using MareLib;
using Vintagestory.GameContent;

namespace Fishing3;

[Effect]
public class EffectRegeneration : AlchemyEffect
{
    private Accumulator accumulator = Accumulator.WithInterval(1f).Max(30f);

    // 1 HP per 5 seconds at base strength.
    public const float STRENGTH_RATIO = 0.2f;

    // 100 units at base strength = 6 hp.
    public override float BaseDuration => 30f;

    public override EffectType Type => EffectType.Duration;

    public override void OnTick(float dt)
    {
        if (!IsServer) return;

        EntityBehaviorHealth? health = Entity.GetBehavior<EntityBehaviorHealth>();
        if (health == null) return;

        accumulator.Add(dt);
        while (accumulator.Consume())
        {
            health.Health += StrengthMultiplier * STRENGTH_RATIO * accumulator.interval;
        }
    }

    public override bool MergeEffects(Effect other)
    {
        EffectRegeneration regen = (EffectRegeneration)other;

        float thisPower = StrengthMultiplier * Duration;
        float otherPower = regen.StrengthMultiplier * regen.Duration;

        if (otherPower > thisPower)
        {
            StrengthMultiplier = regen.StrengthMultiplier;
            Duration = regen.Duration;
        }

        return false;
    }
}