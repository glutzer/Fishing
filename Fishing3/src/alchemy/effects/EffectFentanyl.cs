using MareLib;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.GameContent;

namespace Fishing3;

[Effect]
public class EffectFentanyl : AlchemyEffect
{
    public override float BaseDuration => 30f;
    public override EffectType Type => EffectType.Duration;

    public override void OnLoaded()
    {
        if (!IsServer) return;

        // 10% increased movement speed at baseline.
        Entity.Stats.Set("walkspeed", "fent", StrengthMultiplier * 0.1f, true);
        EffectBehavior.onDamaging += DamageToPregnantAnimals;
    }

    public void DamageToPregnantAnimals(ref float damage, DamageSource source, Entity toEntity)
    {
        EntityBehaviorMultiply? bh = toEntity.GetBehavior<EntityBehaviorMultiply>();
        if (bh == null) return;

        if (bh.IsPregnant)
        {
            damage *= 2f * StrengthMultiplier;
        }
    }

    public override void OnTick(float dt)
    {
        if (!IsServer) return;

        EntityBehaviorBreathe? behavior = Entity.GetBehavior<EntityBehaviorBreathe>();
        if (behavior == null) return;

        behavior.Oxygen -= 50f * StrengthMultiplier;
        behavior.HasAir = false;
    }

    public override void OnUnloaded()
    {
        if (!IsServer) return;

        Entity.Stats.Remove("walkspeed", "fent");
        EffectBehavior.onDamaging -= DamageToPregnantAnimals;
    }
}