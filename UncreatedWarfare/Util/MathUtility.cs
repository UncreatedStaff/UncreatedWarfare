using SDG.NetPak;
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
    /// Rounds a number to the nearest <paramref name="round"/>.
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
    public static float SquaredDistance(in Vector3 pos1, in Vector3 pos2)
    {
        float dx = pos1.x - pos2.x,
              dz = pos1.z - pos2.z;

        float dy = pos1.y - pos2.y;
        return dx * dx + dy * dy + dz * dz;
    }

    /// <summary>
    /// Find square distance and optionally ignore the Y axis.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static float SquaredDistance(Vector3 pos1, in Vector3 pos2)
    {
        float dx = pos1.x - pos2.x,
              dz = pos1.z - pos2.z;

        float dy = pos1.y - pos2.y;
        return dx * dx + dy * dy + dz * dz;
    }

    /// <summary>
    /// Find square distance and optionally ignore the Y axis.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static float SquaredDistance(in Vector3 pos1, Vector3 pos2)
    {
        float dx = pos1.x - pos2.x,
              dz = pos1.z - pos2.z;

        float dy = pos1.y - pos2.y;
        return dx * dx + dy * dy + dz * dz;
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

    /// <summary>
    /// Find square distance between two 2D points.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static float SquaredDistance(in Vector2 pos1, in Vector2 pos2)
    {
        float dx = pos1.x - pos2.x,
              dz = pos1.y - pos2.y;

        return dx * dx + dz * dz;
    }

    /// <summary>
    /// Find square distance between two 2D points.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static float SquaredDistance(in Vector2 pos1, in Vector3 pos2)
    {
        float dx = pos1.x - pos2.x,
              dz = pos1.y - pos2.z;

        return dx * dx + dz * dz;
    }
    
    /// <summary>
    /// Find square distance between two 2D points.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static float SquaredDistance(in Vector3 pos1, in Vector2 pos2)
    {
        float dx = pos1.x - pos2.x,
              dz = pos1.z - pos2.y;

        return dx * dx + dz * dz;
    }

    /// <summary>
    /// Returns true if <paramref name="pos1"/> and <paramref name="pos2"/> are less than <paramref name="range"/> units away from each other, otherwise false. Compares using square magnitude for speed.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool WithinRange(in Vector3 pos1, in Vector3 pos2, float range) => SquaredDistance(in pos1, in pos2, false) <= range * range;

    /// <summary>
    /// Returns true if <paramref name="pos1"/> and <paramref name="pos2"/> are less than <paramref name="range"/> units away from each other, otherwise false. Compares using square magnitude for speed.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool WithinRange2D(in Vector3 pos1, in Vector3 pos2, float range) => SquaredDistance(in pos1, in pos2, true) <= range * range;

    /// <summary>
    /// Returns true if <paramref name="pos1"/> and <paramref name="pos2"/> are less than <paramref name="range"/> units away from each other, otherwise false. Compares using square magnitude for speed.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool WithinRange(Vector3 pos1, Vector3 pos2, float range) => SquaredDistance(in pos1, in pos2, false) <= range * range;

    /// <summary>
    /// Returns true if <paramref name="pos1"/> and <paramref name="pos2"/> are less than <paramref name="range"/> units away from each other, otherwise false. Compares using square magnitude for speed.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool WithinRange2D(Vector3 pos1, Vector3 pos2, float range) => SquaredDistance(in pos1, in pos2, true) <= range * range;

    /// <summary>
    /// Counts the number of digits in a number, not counting the negative sign.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static int CountDigits(int value)
    {
        return value != 0 ? 1 + (int)Math.Log10(Math.Abs(value)) : 1;
    }
    
    /// <summary>
    /// Counts the number of digits in an unsigned number.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static int CountDigits(uint value)
    {
        return value != 0 ? 1 + (int)Math.Log10(value) : 1;
    }

    public static bool IsRotationNearlyEqual(Vector3 r1, Vector3 r2, float tolerance)
    {
        return IsAngleNearlyEqual(r1.x, r2.x, tolerance) && IsAngleNearlyEqual(r1.y, r2.y, tolerance) && IsAngleNearlyEqual(r1.z, r2.z, tolerance);
    }

    public static bool IsAngleNearlyEqual(float a1, float a2, float tolerance)
    {
        a1 %= 360f;
        a2 %= 360f;

        float a1Dx360 = Math.Min(360f - a1, a1);
        float a2Dx360 = Math.Min(360f - a2, a2);
        if (a1Dx360 + a2Dx360 <= tolerance)
        {
            return true;
        }

        return Math.Abs(a1 - a2) <= tolerance;
    }

    public static void CompressVector3(ref Vector3 position, int intBitCount = 13, int fracBitCount = 7)
    {
        // UnityNetPakWriterEx.WriteClampedVector3
        // UnityNetPakReaderEx.ReadClampedVector3

        CompressFloat(ref position.x, intBitCount, fracBitCount);
        CompressFloat(ref position.y, intBitCount, fracBitCount);
        CompressFloat(ref position.z, intBitCount, fracBitCount);
    }

    public static void CompressYawOrQuaternion(ref Quaternion quaternion, int yawBitCount = 9, int quaternionBitsPerComponent = 9)
    {
        // UnityNetPakWriterEx.WriteSpecialYawOrQuaternion
        // UnityNetPakReaderEx.ReadSpecialYawOrQuaternion

        Vector3 z = quaternion * Vector3.forward;
        bool isOnlyRotatedAroundYAxis = z.y > 0.9999f;
        if (!isOnlyRotatedAroundYAxis)
        {
            CompressQuaternion(ref quaternion);
            return;
        }

        Vector3 y = quaternion * Vector3.up;
        Vector2 direction = new Vector2(-y.z, -y.x).normalized;
        float yaw = Mathf.Atan2(direction.y, direction.x);
        CompressRadians(ref yaw, yawBitCount);
        quaternion = Quaternion.Euler(-90.0f, yaw * Mathf.Rad2Deg, 0.0f);
    }

    private static void CompressRadians(ref float radians, int bitCount = 8)
    {
        // UnityNetPakWriterEx.WriteRadians
        // UnityNetPakReaderEx.ReadRadians

        const float tau = Mathf.PI * 2.0f;
        uint maxValue = 1u << bitCount;

        float remainder = (radians % tau + tau) % tau;
        uint quantizedValue = (uint)(remainder / tau * maxValue);
        radians = quantizedValue / (float)maxValue * tau;
    }

    public static void CompressQuaternion(ref Quaternion quaternion, int bitsPerComponent = 9)
    {
        // UnityNetPakWriterEx.WriteQuaternion
        // UnityNetPakReaderEx.ReadQuaternion

        uint largestComponentIndex = 0;
        float largestComponentValue;
        float largestComponentSign;

        if (quaternion.x < 0.0f)
        {
            largestComponentValue = -quaternion.x;
            largestComponentSign = -1.0f;
        }
        else
        {
            largestComponentValue = quaternion.x;
            largestComponentSign = 1.0f;
        }

        for (uint componentIndex = 1; componentIndex < 4; ++componentIndex)
        {
            float componentValue = quaternion[(int)componentIndex];
            if (componentValue < 0.0f)
            {
                componentValue = -componentValue;
                if (componentValue <= largestComponentValue)
                    continue;

                largestComponentIndex = componentIndex;
                largestComponentValue = componentValue;
                largestComponentSign = -1.0f;
            }
            else if (componentValue > largestComponentValue)
            {
                largestComponentIndex = componentIndex;
                largestComponentValue = componentValue;
                largestComponentSign = +1.0f;
            }
        }

        float value0;
        float value1;
        float value2;
        switch (largestComponentIndex)
        {
            case 0:
                value0 = quaternion.y;
                value1 = quaternion.z;
                value2 = quaternion.w;
                break;

            case 1:
                value0 = quaternion.x;
                value1 = quaternion.z;
                value2 = quaternion.w;
                break;

            case 2:
                value0 = quaternion.x;
                value1 = quaternion.y;
                value2 = quaternion.w;
                break;

            default: // case 3:
                value0 = quaternion.x;
                value1 = quaternion.y;
                value2 = quaternion.z;
                break;
        }

        float v0 = value0 * largestComponentSign * NetPakConst.SQRT_OF_TWO;
        float v1 = value1 * largestComponentSign * NetPakConst.SQRT_OF_TWO;
        float v2 = value2 * largestComponentSign * NetPakConst.SQRT_OF_TWO;

        CompressSignedNormalizedFloat(ref v0, bitsPerComponent);
        CompressSignedNormalizedFloat(ref v1, bitsPerComponent);
        CompressSignedNormalizedFloat(ref v2, bitsPerComponent);

        v0 *= NetPakConst.INV_SQRT_OF_TWO;
        v1 *= NetPakConst.INV_SQRT_OF_TWO;
        v2 *= NetPakConst.INV_SQRT_OF_TWO;

        largestComponentValue = Mathf.Sqrt(1.0f - (v0 * v0 + v1 * v1 + v2 * v2));
        quaternion = largestComponentIndex switch
        {
            0 => new Quaternion(largestComponentValue, v0, v1, v2),
            1 => new Quaternion(v0, largestComponentValue, v1, v2),
            2 => new Quaternion(v0, v1, largestComponentValue, v2),
            _ => new Quaternion(v0, v1, v2, largestComponentValue)
        };
    }

    private static void CompressFloat(ref float value, int intBitCount, int fracBitCount)
    {
        // SystemNetPakWriterEx.WriteClampedFloat
        // SystemNetPakReaderEx.ReadClampedFloat

        int absMinValue = 1 << (intBitCount - 1);
        uint maxFracValue = 1u << fracBitCount;
        uint whole, frac;
        if (value < -absMinValue)
        {
            whole = 0u;
            frac = 0u;
        }
        else if (value >= absMinValue)
        {
            whole = Bitmask(intBitCount);
            frac = Bitmask(fracBitCount);
        }
        else if (Mathf.Abs(value) < 0.0001f)
        {
            whole = (uint)absMinValue;
            frac = 0;
        }
        else
        {
            int intValue = Mathf.FloorToInt(value);
            whole = (uint)(value + absMinValue);
            float fracValue = value - intValue;
            frac = (uint)(fracValue * maxFracValue);
        }

        value = (int)whole - absMinValue + frac / (float)maxFracValue;
    }

    private static void CompressSignedNormalizedFloat(ref float value, int bitCount)
    {
        // SystemNetPakWriterEx.WriteSignedNormalizedFloat
        // SystemNetPakReaderEx.ReadSignedNormalizedFloat

        uint maxValuePlusOne = 1u << (bitCount - 1);
        uint maxValue = maxValuePlusOne - 1;

        uint quantizedValue;
        if (value >= 0f)
        {
            quantizedValue = (uint)(value * maxValue + 0.5f);
        }
        else
        {
            quantizedValue = (uint)(-value * maxValue + 0.5f);
            quantizedValue |= maxValuePlusOne;
        }

        if ((quantizedValue & maxValuePlusOne) == maxValuePlusOne)
        {
            value = -((quantizedValue & maxValue) / (float)maxValue);
        }
        else
        {
            value = quantizedValue / (float)maxValue;
        }
    }

    private static uint Bitmask(int amount)
    {
        if (amount == 32) return unchecked( (uint)-1 );
        return (1u << amount) - 1;
    }
}
