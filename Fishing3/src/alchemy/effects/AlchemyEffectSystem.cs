using MareLib;
using System;
using System.Collections.Generic;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;

namespace Fishing3;

public enum ApplicationMethod
{
    Consume,
    Blood,
    Skin
}

/// <summary>
/// Creates and applies alchemical effects on the server.
/// </summary>
[GameSystem]
public class AlchemyEffectSystem : GameSystem
{
    public static EffectManager Server { get; private set; } = null!;

    private HudEffects? hudEffects;

    public AlchemyEffectSystem(bool isServer, ICoreAPI api) : base(isServer, api)
    {
    }

    public override void PreInitialize()
    {
        if (!isServer) return;
        Server = MainAPI.GetGameSystem<EffectManager>(EnumAppSide.Server);
    }

    public override void OnStart()
    {
        if (isServer) return;

        // Init HUD.
        hudEffects = new HudEffects();

        MainAPI.GetGameSystem<EffectManager>(api.Side).OnPlayerEffectCountChanged += args =>
        {
            if (!args.player.IsSelf()) return;

            if (args.currentCount <= 0)
            {
                hudEffects?.TryClose();
            }
            else if (args.previousCount == 0)
            {
                hudEffects?.TryOpen();
            }
        };
    }

    /// <summary>
    /// Consumes any fluid to apply it to an entity.
    /// Only potions and reagents will do anything.
    /// Only call this on the server.
    /// </summary>
    public static void ApplyFluid(FluidContainer container, int amount, Entity? fromEntity, Entity toEntity, ApplicationMethod method, float auxMultiplier = 1f)
    {
        if (container.HeldStack == null) return;

        float statMultiplier = fromEntity?.Stats.GetBlended("flaskEffect") ?? 1f;
        statMultiplier *= auxMultiplier;

        if (container.HeldStack is FluidStackCompound)
        {
            ApplyPotion(container, amount, toEntity, statMultiplier, method);
            return;
        }

        ApplyReagent(container, amount, toEntity, statMultiplier, method);
    }

    private static void ApplyPotion(FluidContainer container, int amount, Entity toEntity, float statMultiplier, ApplicationMethod method)
    {
        // Will be the same as apply reagent, but compiling all effects and getting ratios.
        if (container.TakeOut(amount) is not FluidStackCompound stack) return; // Nothing taken.

        // Aggregate all effects and how many units were used to apply them.
        List<(Effect effect, int units)> createdEffects = new();

        foreach (FluidStack reagentStack in stack.containedStacks)
        {
            if (reagentStack.fluid.GetBehavior<FluidBehaviorReagent>() is not FluidBehaviorReagent reagent || reagentStack.Units <= 0) continue;

            int units = reagentStack.Units;
            float purityMultiplier = FluidBehaviorReagent.GetPurityMultiplier(reagentStack);

            // The base duration for an effect is reached at 100 units? Linear scaling.
            float durationMultiplier = units / 100f;

            foreach (EffectProperties props in reagent.Properties)
            {
                Effect? effect = Server.CreateEffect(props.Type);
                if (effect == null) continue;

                // For all effects?
                effect.Duration *= durationMultiplier;
                effect.Duration *= props.Duration;

                if (effect is AlchemyEffect alchemyEffect)
                {
                    if (units < alchemyEffect.MinimumVolume) continue; // Not enough units to apply this effect.

                    alchemyEffect.StrengthMultiplier *= props.Strength;
                    alchemyEffect.StrengthMultiplier *= statMultiplier; // Also by the player's stat multiplier.
                    alchemyEffect.StrengthMultiplier *= purityMultiplier; // Also by the reagent's purity.

                    alchemyEffect.Units = units;

                    if (props.Data != null) alchemyEffect.CollectDataFromReagent(props.Data);
                    alchemyEffect.CollectDataFromFluidStack(reagentStack, method);
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

                float metaEffectRatio = units / (float)otherTuple.units;
                float strengthMulti = Math.Clamp(metaEffectRatio / metaEffect.BaseRatio, 0f, 1f);

                metaEffect.ApplyTo(otherTuple.effect, strengthMulti);
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

    private static void ApplyReagent(FluidContainer container, int amount, Entity toEntity, float statMultiplier, ApplicationMethod method)
    {
        FluidStack? stack = container.TakeOut(amount);
        if (stack == null) return; // Nothing taken.

        FluidBehaviorReagent? reagent = stack.fluid.GetBehavior<FluidBehaviorReagent>();
        if (reagent == null) return; // May inject any fluid, but won't do anything unless it's a reagent.

        int units = stack.Units;
        float purityMultiplier = FluidBehaviorReagent.GetPurityMultiplier(stack);

        // The base duration for an effect is reached at 100 units? Linear scaling.
        float durationMultiplier = units / 100f;

        List<Effect> createdEffects = new();

        // Multiply initial stats by reagent properties.
        foreach (EffectProperties props in reagent.Properties)
        {
            Effect? effect = Server.CreateEffect(props.Type);
            if (effect == null) continue;

            effect.Duration *= durationMultiplier;
            effect.Duration *= props.Duration;

            if (effect is AlchemyEffect alchemyEffect)
            {
                alchemyEffect.StrengthMultiplier *= props.Strength;
                alchemyEffect.StrengthMultiplier *= statMultiplier; // Also by the player's stat multiplier.
                alchemyEffect.StrengthMultiplier *= purityMultiplier; // Also by the reagent's purity.

                alchemyEffect.Units = units;

                if (props.Data != null) alchemyEffect.CollectDataFromReagent(props.Data);
                alchemyEffect.CollectDataFromFluidStack(stack, method);
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
        Server = null!;
    }
}