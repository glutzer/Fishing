using MareLib;
using System;
using System.Collections.Generic;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;

namespace Fishing3;

/// <summary>
/// Creates and applies alchemical effects on the server.
/// </summary>
[GameSystem(forSide = EnumAppSide.Server)]
public class AlchemyEffectSystem : GameSystem
{
    public static EffectManager EffectManager { get; private set; } = null!;

    public AlchemyEffectSystem(bool isServer, ICoreAPI api) : base(isServer, api)
    {
    }

    public override void PreInitialize()
    {
        EffectManager = MainAPI.GetGameSystem<EffectManager>(EnumAppSide.Server);
    }

    /// <summary>
    /// Consumes any fluid to apply it to an entity.
    /// Only potions and reagents will do anything.
    /// </summary>
    public static void ApplyFluid(FluidContainer container, int amount, Entity? fromEntity, Entity toEntity)
    {
        if (container.HeldStack == null) return;

        float statMultiplier = fromEntity?.Stats.GetBlended("flaskEffect") ?? 1f;

        if (container.HeldStack is FluidStackPotion)
        {
            ApplyPotion(container, amount, toEntity, statMultiplier);
            return;
        }

        ApplyReagent(container, amount, toEntity, statMultiplier);
    }

    private static void ApplyPotion(FluidContainer container, int amount, Entity toEntity, float statMultiplier)
    {
        // Will be the same as apply reagent, but compiling all effects and getting ratios.
        if (container.TakeOut(amount) is not FluidStackPotion stack) return; // Nothing taken.

        // Aggregate all effects and how many units were used to apply them.
        List<(Effect effect, int units)> createdEffects = new();

        foreach (FluidStack reagentStack in stack.containedStacks)
        {
            if (reagentStack.fluid.GetBehavior<FluidBehaviorReagent>() is not FluidBehaviorReagent reagent || reagentStack.Units <= 0) continue; // Anything with 0 units would break.

            int units = reagentStack.Units;

            foreach (EffectProperties props in reagent.Properties)
            {
                Effect? effect = EffectManager.CreateEffect(props.Type);
                if (effect == null) continue;

                if (effect is AlchemyEffect alchemyEffect)
                {
                    alchemyEffect.StrengthMultiplier *= props.Strength;
                    alchemyEffect.StrengthMultiplier *= statMultiplier; // Also by the player's stat multiplier.
                    alchemyEffect.Duration *= props.Duration;
                    alchemyEffect.Units = units;

                    if (props.Data != null) alchemyEffect.CollectDataFromReagent(props.Data);
                }

                createdEffects.Add((effect, units));
            }
        }

        // Before any initialization, apply meta effects.
        foreach ((Effect effect, int units) in createdEffects)
        {
            if (effect is not IMetaEffect metaEffect) continue;

            foreach ((Effect effect, int units) otherTuple in createdEffects)
            {
                if (otherTuple.effect == effect) continue; // Can't apply to self.

                float metaEffectRatio = Math.Clamp(units / (float)otherTuple.units, 0, 1);

                metaEffect.ApplyTo(otherTuple.effect, metaEffectRatio);
            }
        }

        EntityBehaviorEffects? effectBehavior = toEntity.GetEffectBehavior();
        if (effectBehavior == null) return;

        // Apply every effect.
        foreach ((Effect effect, _) in createdEffects)
        {
            effectBehavior.ApplyEffect(effect);
        }
    }

    private static void ApplyReagent(FluidContainer container, int amount, Entity toEntity, float statMultiplier)
    {
        FluidStack? stack = container.TakeOut(amount);
        if (stack == null) return; // Nothing taken.

        FluidBehaviorReagent? reagent = stack.fluid.GetBehavior<FluidBehaviorReagent>();
        if (reagent == null) return; // May inject any fluid, but won't do anything unless it's a reagent.

        int units = stack.Units;

        List<Effect> createdEffects = new();

        // Multiply initial stats by reagent properties.
        foreach (EffectProperties props in reagent.Properties)
        {
            Effect? effect = EffectManager.CreateEffect(props.Type);
            if (effect == null) continue;

            if (effect is AlchemyEffect alchemyEffect)
            {
                alchemyEffect.StrengthMultiplier *= props.Strength;
                alchemyEffect.StrengthMultiplier *= statMultiplier; // Also by the player's stat multiplier.
                alchemyEffect.Duration *= props.Duration;
                alchemyEffect.Units = units;

                if (props.Data != null) alchemyEffect.CollectDataFromReagent(props.Data);
            }

            createdEffects.Add(effect);
        }

        // Always 1, since single fluid.
        float metaEffectRatio = 1f;

        // Before any initialization, apply meta effects.
        foreach (Effect effect in createdEffects)
        {
            if (effect is not IMetaEffect metaEffect) continue;

            foreach (Effect otherEffect in createdEffects)
            {
                if (otherEffect == effect) continue; // Can't apply to self.

                metaEffect.ApplyTo(otherEffect, metaEffectRatio);
            }
        }

        EntityBehaviorEffects? effectBehavior = toEntity.GetEffectBehavior();
        if (effectBehavior == null) return;

        // Apply every effect.
        foreach (Effect effect in createdEffects)
        {
            effectBehavior.ApplyEffect(effect);
        }
    }

    public override void OnClose()
    {
        EffectManager = null!;
    }
}