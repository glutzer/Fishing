using System;
using Vintagestory.API.MathTools;

namespace Fishing;

/// <summary>
/// Just a utility for lerping things.
/// </summary>
public static class Lerper
{
    public static float LerpTo(this float currentValue, float targetValue, float dt, float smoothTime = 0.25f, float snapValue = 0.01f)
    {
        float t = 1f - MathF.Exp(-dt / smoothTime);

        // Smoothly move toward target.
        currentValue = GameMath.Lerp(currentValue, targetValue, t);

        // Snap when close enough.
        if (MathF.Abs(currentValue - targetValue) < snapValue) currentValue = targetValue;

        return currentValue;
    }
}