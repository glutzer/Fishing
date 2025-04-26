using System;

namespace Fishing3;

public static class DRUtility
{
    /// <summary>
    /// rate = DR rate. Higher decreases faster, lower decreases slower.
    /// Linear until base line then drops off.
    /// 1 rate -> 1 at 1, 1.3 at 2, 1.47 at 3.
    /// 0.5 rate -> 1 at 1, 1.6 at 2, 1.95 at 3.
    /// 0.33 rate -> 1 at 1, 1.9 at 2, 2.42 at 3.
    /// 0.25 rate -> 1 at 1, 2.2 at 2, 2.9 at 3, 3.4 at 4.
    /// 2 rate -> 1 at 1, 1.15 at 2, 1.24 at 3.
    /// 
    /// These are calculated from the value / baseLine, then multiplied back.
    /// </summary>
    public static float CalculateDR(float value, float baseLine, float rate)
    {
        if (value < baseLine)
        {
            return value;
        }

        float ratio = value / baseLine;
        float y = 1 + MathF.Log(MathF.Pow(ratio, 1 / rate));
        return y * baseLine;
    }

    /// <summary>
    /// Takes a value with DR applied, gets the reverse of it.
    /// </summary>
    public static float ReverseDR(float value, float baseLine, float rate)
    {
        return value < baseLine ? value : baseLine * MathF.Exp(rate * ((value / baseLine) - 1));
    }
}