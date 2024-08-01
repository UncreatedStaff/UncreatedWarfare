using System;
using System.Runtime.CompilerServices;

namespace Uncreated.Warfare.Util;
public static class MathUtility
{
    /// <summary>
    /// Rounds a number to the nearest <paramref name="round"/>. 
    /// </summary>
    /// <param name="value">Value to actually round.</param>
    /// <param name="round">Nearest number to round to.</param>
    /// <param name="min">Absolute minimum allowed value. Also acts as the base value to increment by <paramref name="round"/> from.</param>
    /// <param name="max">Absolute maximum allowed value.</param>
    public static int RoundNumber(int value, int round, int min, int max)
    {
        if (round <= 1)
            return value;

        int val2 = value - min;
        int mod = val2 % round;
        if (mod == 0)
            return value;

        if (mod > round / 2f)
        {
            int rounded = value + (round - mod);
            if (rounded <= max)
                return rounded;
        }

        return value - mod;
    }

    /// <summary>
    /// Rounds a number to the nearest <paramref name="round"/>. Negative numbers  
    /// </summary>
    /// <param name="value">Value to actually round.</param>
    /// <param name="round">Nearest number to round to.</param>
    /// <param name="min">Absolute minimum allowed value. Also acts as the base value to increment by <paramref name="round"/> from.</param>
    /// <param name="max">Absolute maximum allowed value.</param>
    public static double RoundNumber(double value, int round, double min, double max)
    {
        if (round == 0)
            return value;

        if (round > 0)
        {
            double relativeValue = value - min;
            double step = relativeValue % round;
            if (step == 0)
                return value;

            if (step <= round / 2f)
                return value - step;

            double rounded = value + (round - step);
            if (rounded <= max)
                return rounded;

            return value - step;
        }
        else
        {
            double roundCalculated = -round;
            int ct = -1;
            while (roundCalculated >= 1d)
            {
                roundCalculated /= 10d;
                ++ct;
            }

            if (ct > 0)
            {
                roundCalculated *= Math.Pow(10, -ct);
            }

            double relativeValue = value - min;
            double step = relativeValue % roundCalculated;
            if (step == 0)
                return value;

            if (step <= roundCalculated / 2f)
                return value - step;

            double rounded = value + (roundCalculated - step);
            if (rounded <= max)
                return rounded;

            return value - step;
        }
    }

    /// <summary>
    /// Find square distance and optionally ignore the Y axis.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static float SquaredDistance(in Vector3 pos1, in Vector3 pos2, bool horizontalDistanceOnly)
    {
        float dx = pos1.x - pos2.x,
              dz = pos1.z - pos2.z;

        if (horizontalDistanceOnly)
        {
            return dx * dx + dz * dz;
        }

        float dy = pos1.y - pos2.y;
        return dx * dx + dy * dy + dz * dz;
    }
}
