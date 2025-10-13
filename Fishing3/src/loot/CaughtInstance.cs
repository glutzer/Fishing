using OpenTK.Mathematics;
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
    public readonly ItemStack? itemStack;
    private float secondsOfStamina;
    public readonly float maxStamina;

    public bool IsFighting { get; private set; } = true;
    private Accumulator accumulator = Accumulator.WithRandomInterval(3f, 10f);

    // Action called on catching, with position.
    public Action<Vector3d>? OnCaught;

    public CaughtInstance(ItemStack? itemStack, float kg = 1f, float speed = 1f, float secondsOfStamina = 1f)
    {
        this.kg = kg;
        this.speed = speed;
        this.itemStack = itemStack;
        this.secondsOfStamina = secondsOfStamina;
        maxStamina = secondsOfStamina;
    }

    public void UpdateStamina(float dt)
    {
        if (maxStamina == 0)
        {
            IsFighting = false;
            return;
        }

        if (IsFighting) secondsOfStamina -= dt;

        accumulator.Add(dt);

        if (accumulator.Progress(dt))
        {
            accumulator.SetRandomInterval(3f, 10f);

            // Minimum stamina.
            float staminaRatio = Math.Max(secondsOfStamina / maxStamina, 0.1f);

            IsFighting = Random.Shared.NextSingle() < staminaRatio;
        }
    }
}