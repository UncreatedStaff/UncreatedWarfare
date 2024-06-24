using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading;

namespace Uncreated.Warfare.Util;

/// <summary>
/// Thread-safe wrapper utilities for <see cref="Random"/>.
/// </summary>
public static class RandomUtility
{
    [ThreadStatic]
    private static Random? _random;

    private static int _randomSeed;
    private static Random GetNonGameThreadRandom() => _random ??= new Random(Interlocked.Increment(ref _randomSeed));

    /// <summary>
    /// Thread-safe function to get a random integer within the range [0, <see cref="int.MaxValue"/>).
    /// </summary>
    public static int GetInteger()
    {
        return Environment.CurrentManagedThreadId == WarfareModule.GameThreadId
            ? UnityEngine.Random.Range(0, int.MaxValue)
            : GetNonGameThreadRandom().Next();
    }

    /// <summary>
    /// Thread-safe function to get a random integer within the range [0, <paramref name="upperExclusive"/>).
    /// </summary>
    public static int GetInteger(int upperExclusive)
    {
        if (upperExclusive == 1)
            return 0;

        return Environment.CurrentManagedThreadId == WarfareModule.GameThreadId
            ? UnityEngine.Random.Range(0, upperExclusive)
            : GetNonGameThreadRandom().Next(upperExclusive);
    }

    /// <summary>
    /// Thread-safe function to get a random integer within the range [<paramref name="lowerInclusive"/>, <paramref name="upperExclusive"/>).
    /// </summary>
    public static int GetInteger(int lowerInclusive, int upperExclusive)
    {
        if (lowerInclusive == upperExclusive - 1)
            return lowerInclusive;

        return Environment.CurrentManagedThreadId == WarfareModule.GameThreadId
            ? UnityEngine.Random.Range(lowerInclusive, upperExclusive)
            : GetNonGameThreadRandom().Next(lowerInclusive, upperExclusive);
    }

    /// <summary>
    /// Thread-safe function to get a random single-precision decimal within the range [0, 1].
    /// </summary>
    public static float GetFloat()
    {
        return Environment.CurrentManagedThreadId == WarfareModule.GameThreadId
            ? UnityEngine.Random.value
            : (float)GetNonGameThreadRandom().NextDouble();
    }

    /// <summary>
    /// Get a random true or false boolean.
    /// </summary>
    public static bool GetBoolean() => GetFloat() >= 0.5f;

    /// <summary>
    /// Thread-safe function to get a random single-precision decimal within the range [0, <paramref name="upperInclusive"/>].
    /// </summary>
    public static float GetFloat(float upperInclusive) => GetFloat(0, upperInclusive);

    /// <summary>
    /// Thread-safe function to get a random single-precision decimal within the range [<paramref name="lowerInclusive"/>, <paramref name="upperInclusive"/>].
    /// </summary>
    public static float GetFloat(float lowerInclusive, float upperInclusive)
    {
        return Environment.CurrentManagedThreadId == WarfareModule.GameThreadId
            ? UnityEngine.Random.Range(lowerInclusive, upperInclusive)
            : (float)GetNonGameThreadRandom().NextDouble() * (upperInclusive - lowerInclusive) + lowerInclusive;
    }

    /// <summary>
    /// Thread-safe function to get a random double-precision decimal within the range [0, 1].
    /// </summary>
    public static double GetDouble()
    {
        return Environment.CurrentManagedThreadId == WarfareModule.GameThreadId
            ? UnityEngine.Random.value
            : (float)GetNonGameThreadRandom().NextDouble();
    }

    /// <summary>
    /// Thread-safe function to get a random double-precision decimal within the range [0, <paramref name="upperInclusive"/>].
    /// </summary>
    public static double GetDouble(double upperInclusive) => GetDouble(0, upperInclusive);

    /// <summary>
    /// Thread-safe function to get a random double-precision decimal within the range [<paramref name="lowerInclusive"/>, <paramref name="upperInclusive"/>].
    /// </summary>
    public static double GetDouble(double lowerInclusive, double upperInclusive)
    {
        return GetNonGameThreadRandom().NextDouble() * (upperInclusive - lowerInclusive) + lowerInclusive;
    }

    /// <summary>
    /// Thread-safe function to get a random index within a <paramref name="list"/>.
    /// </summary>
    /// <exception cref="ArgumentException"><paramref name="list"/> is empty.</exception>
    public static int GetIndex(ICollection list)
    {
        if (list.Count == 0)
            throw new ArgumentException("List is empty", nameof(list));

        return GetInteger(0, list.Count);
    }

    /// <summary>
    /// Thread-safe function to get a random index within a <paramref name="list"/>.
    /// </summary>
    /// <exception cref="ArgumentException"><paramref name="list"/> is empty.</exception>
    public static int GetIndex<T>(ICollection<T> list)
    {
        if (list.Count == 0)
            throw new ArgumentException("List is empty", nameof(list));

        return GetInteger(0, list.Count);
    }

    /// <summary>
    /// Thread-safe function to get a random index within a <paramref name="list"/>.
    /// </summary>
    /// <exception cref="ArgumentException"><paramref name="list"/> is empty.</exception>
    public static int GetIndex<T>(IReadOnlyCollection<T> list)
    {
        if (list.Count == 0)
            throw new ArgumentException("List is empty", nameof(list));

        return GetInteger(0, list.Count);
    }

    /// <summary>
    /// Thread-safe function to get a random index within a <paramref name="list"/> given a weight for each element.
    /// </summary>
    /// <exception cref="ArgumentException"><paramref name="list"/> is empty.</exception>
    public static int GetIndex<T>(IReadOnlyList<T> list, Func<T, float> weightSelector)
    {
        if (list.Count == 0)
            throw new ArgumentException("List is empty", nameof(list));

        float totalWeight = 0;
        for (int i = 0; i < list.Count; ++i)
        {
            totalWeight += weightSelector(list[i]);
        }

        float pick = GetFloat(0, totalWeight);
        totalWeight = 0;
        for (int i = 0; i < list.Count; ++i)
        {
            totalWeight += weightSelector(list[i]);
            if (totalWeight > pick)
                return i;
        }

        // should never happen
        return list.Count - 1;
    }

    /// <summary>
    /// Thread-safe function to get a random index within a <paramref name="list"/> given a weight for each element.
    /// </summary>
    /// <exception cref="ArgumentException"><paramref name="list"/> is empty.</exception>
    public static int GetIndex<T>(IReadOnlyList<T> list, Func<T, double> weightSelector)
    {
        if (list.Count == 0)
            throw new ArgumentException("List is empty", nameof(list));

        double totalWeight = 0;
        for (int i = 0; i < list.Count; ++i)
        {
            totalWeight += weightSelector(list[i]);
        }

        double pick = GetDouble(0, totalWeight);
        totalWeight = 0;
        for (int i = 0; i < list.Count; ++i)
        {
            totalWeight += weightSelector(list[i]);
            if (totalWeight > pick)
                return i;
        }

        // should never happen
        return list.Count - 1;
    }
}
