using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using Vintagestory.API.Common.Entities;

namespace Fishing3;

[Effect]
public class EffectIntoxication : AlchemyEffect
{
    [JsonObject(MemberSerialization.OptIn)]
    public class IntoxicationInstance
    {
        [JsonProperty]
        public float duration;

        [JsonProperty]
        public float strength;
    }

    [JsonProperty]
    public readonly List<IntoxicationInstance> intoxicationInstances = [];

    public override float BaseDuration => 60f;

    private Accumulator accumulator = Accumulator.WithInterval(1f).Max(30f);

    public override EffectType Type => EffectType.Duration;

    public override void Initialize()
    {
        intoxicationInstances.Add(new IntoxicationInstance()
        {
            duration = Duration,
            strength = StrengthMultiplier
        });
    }

    public override void OnTick(float dt)
    {
        if (!IsServer) return;

        accumulator.Add(dt);
        while (accumulator.Consume())
        {
            float currentTox = Entity.WatchedAttributes.GetFloat("intoxication");
            float maxStrength = 0f;

            foreach (IntoxicationInstance inst in intoxicationInstances)
            {
                maxStrength = Math.Max(maxStrength, inst.strength);

                currentTox += 0.01f * StrengthMultiplier * accumulator.interval;
                currentTox = Math.Clamp(currentTox, 0f, maxStrength);
            }

            Entity.WatchedAttributes.SetFloat("intoxication", currentTox);
        }
    }

    public override void CollectDataFromFluidStack(FluidStack stack, ApplicationMethod method)
    {
        if (method == ApplicationMethod.Blood)
        {
            StrengthMultiplier *= 2f;
            Duration *= 0.5f;
        }
    }

    public override bool MergeEffects(Effect other)
    {
        if (other is EffectIntoxication toxinEffect)
        {
            intoxicationInstances.AddRange(toxinEffect.intoxicationInstances);
        }

        // Set duration to highest duration of toxin instances.
        float maxDuration = 0f;
        foreach (IntoxicationInstance instance in intoxicationInstances)
        {
            maxDuration = Math.Max(maxDuration, instance.duration);
        }
        Duration = maxDuration;

        return false;
    }
}