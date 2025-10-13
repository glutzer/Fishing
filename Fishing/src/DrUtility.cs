using System;

namespace Fishing;

public static class DrUtility
{
    /// <summary>
    /// rate = DR rate. Higher decreases faster, lower decreases slower.
    /// Linear until baseline then drops off.
    /// 1 rate -> 1 at 1, 1.3 at 2, 1.47 at 3.
    /// 0.5 rate -> 1 at 1, 1.6 at 2, 1.95 at 3.
    /// 0.33 rate -> 1 at 1, 1.9 at 2, 2.42 at 3.
    /// 0.25 rate -> 1 at 1, 2.2 at 2, 2.9 at 3, 3.4 at 4.
    /// 2 rate -> 1 at 1, 1.15 at 2, 1.24 at 3.
    /// 
    /// These are calculated from the value / baseLine, then multiplied back.
    /// https://www.desmos.com/calculator/n840ul1tst
    /// </summary>
    public static float CalculateDr(float value, float baseLine, float rate)
    {
        if (value <= baseLine) return value;

        float ratio = value / baseLine;
        float power = 1 / rate;
        power = MathF.Pow(ratio, power);
        power = MathF.Log(power, 10);
        power += 1;
        return power * baseLine;
    }

    /// <summary>
    /// Takes a value with DR applied, gets the reverse of it.
    /// </summary>
    public static float ReverseDr(float value, float baseLine, float rate)
    {
        return value < baseLine ? value : baseLine * MathF.Exp(rate * ((value / baseLine) - 1));
    }
}