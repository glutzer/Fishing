using MareLib;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using Vintagestory.API.Common;
using Vintagestory.GameContent;

namespace Fishing3;

[Effect]
public class EffectToxin : AlchemyEffect
{
    [JsonObject(MemberSerialization.OptIn)]
    public class ToxinInstance
    {
        [JsonProperty]
        public float maxDuration;

        [JsonProperty]
        public float timeActive;

        [JsonProperty]
        public float strength;

        /// <summary>
        /// Strength of the toxin increases over time, starting at nothing.
        /// </summary>
        public float StrengthOverTime => strength * Math.Clamp(timeActive / maxDuration, 0f, 1f);
    }

    [JsonProperty]
    public readonly List<ToxinInstance> toxinInstances = new();

    public override EffectType Type => EffectType.Duration;
    public override float BaseDuration => 50f; // Serves as a multiplier instead.

    private Accumulator accumulator = Accumulator.WithInterval(3f).Max(30f);

    public const float DAMAGE_PER_STRENGTH_CONSTANT = 0.5f;

    public override void Initialize()
    {
        toxinInstances.Add(new ToxinInstance()
        {
            maxDuration = Duration,
            strength = StrengthMultiplier
        });
    }

    public override void OnTick(float dt)
    {
        if (!IsServer) return;

        EntityBehaviorHealth? health = Entity.GetBehavior<EntityBehaviorHealth>();
        if (health == null) return;

        accumulator.Add(dt);
        while (accumulator.Consume())
        {
            foreach (ToxinInstance instance in toxinInstances)
            {
                instance.timeActive += accumulator.interval;
                health.Health -= instance.StrengthOverTime * DAMAGE_PER_STRENGTH_CONSTANT * accumulator.interval;
            }
        }

        if (health.Health < 0) health.Health = 0;

        toxinInstances.RemoveAll(x => x.timeActive > x.maxDuration);

        if (health.Health <= 0 && Entity is EntityAgent agent)
        {
            Entity.ReceiveDamage(new DamageSource()
            {
                Source = EnumDamageSource.Suicide,
                Type = EnumDamageType.Poison
            }, 1f);
        }
    }

    public override void OnDurationExpired()
    {
        EntityBehaviorHealth? health = Entity.GetBehavior<EntityBehaviorHealth>();
        if (health == null) return;

        foreach (ToxinInstance instance in toxinInstances)
        {
            instance.timeActive += accumulator.interval;
            health.Health -= instance.StrengthOverTime * DAMAGE_PER_STRENGTH_CONSTANT;
        }
    }

    public override bool MergeEffects(Effect other)
    {
        if (other is EffectToxin toxinEffect)
        {
            toxinInstances.AddRange(toxinEffect.toxinInstances);
        }

        // Set duration to highest duration of toxin instances.
        float maxDuration = 0f;
        foreach (ToxinInstance instance in toxinInstances)
        {
            maxDuration = Math.Max(maxDuration, instance.maxDuration);
        }
        Duration = maxDuration;

        return false;
    }

    public override void CollectDataFromFluidStack(FluidStack stack, ApplicationMethod method)
    {
        if (method == ApplicationMethod.Blood)
        {
            StrengthMultiplier *= 2f;
        }
    }
}