using MareLib;
using System;
using Vintagestory.API.Common;

namespace Fishing3;

/// <summary>
/// Something that has been caught and given to the bobber.
/// </summary>
public class CaughtInstance
{
    public readonly float kg;
    public readonly float speed;
    public readonly ItemStack itemStack;
    private float secondsOfStamina;
    public readonly float maxStamina;

    public bool IsFighting { get; private set; } = true;
    private Accumulator accumulator = Accumulator.WithRandomInterval(1f, 5f);

    public CaughtInstance(ItemStack itemStack, float kg = 1f, float speed = 1f, float secondsOfStamina = 1f)
    {
        this.kg = kg;
        this.speed = speed;
        this.itemStack = itemStack;
        this.secondsOfStamina = secondsOfStamina;
        maxStamina = secondsOfStamina;
    }

    public void UpdateStamina(float dt)
    {
        if (IsFighting) secondsOfStamina -= dt;

        accumulator.Add(dt);

        if (accumulator.ConsumeAll())
        {
            accumulator.SetRandomInterval(1f, 5f);

            float staminaRatio = secondsOfStamina / maxStamina;

            IsFighting = Random.Shared.NextSingle() < staminaRatio;
        }
    }
}