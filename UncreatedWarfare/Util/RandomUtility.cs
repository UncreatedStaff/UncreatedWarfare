using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Security;
using UnityEngine.PlayerLoop;
using Random = System.Random;

namespace Uncreated.Warfare.Util;

/// <summary>
/// Thread-safe wrapper utilities for <see cref="Random"/>.
/// </summary>
public static class RandomUtility
{
    [ThreadStatic]
    private static Random? _random;

    private static bool _unityLoaded;
    private static bool _unityLoadedSet;

    private static Random GetNonGameThreadRandom() => _random ??= new Random((int)DateTime.UtcNow.Ticks);

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static void SetUnityLoaded()
    {
        try
        {
            _ = UnityEngine.Random.value;
            _unityLoaded = true;
        }
        catch (SecurityException)
        {
            _unityLoaded = false;
        }
        catch (UnityException)
        {
            _unityLoaded = true;
        }

        Interlocked.MemoryBarrier();
        _unityLoadedSet = true;
    }

    /// <summary>
    /// Thread-safe function to get a random integer within the range [0, <see cref="int.MaxValue"/>).
    /// </summary>
    public static int GetInteger()
    {
        if (!_unityLoadedSet)
            SetUnityLoaded();

        return _unityLoaded && GameThread.IsCurrent
            ? UnityEngine.Random.Range(0, int.MaxValue)
            : GetNonGameThreadRandom().Next();
    }

    /// <summary>
    /// Thread-safe function to get a random integer within the range [0, <paramref name="upperExclusive"/>).
    /// </summary>
    public static int GetInteger(int upperExclusive)
    {
        if (!_unityLoadedSet)
            SetUnityLoaded();

        if (upperExclusive == 1)
            return 0;

        return _unityLoaded && GameThread.IsCurrent
            ? UnityEngine.Random.Range(0, upperExclusive)
            : GetNonGameThreadRandom().Next(upperExclusive);
    }

    /// <summary>
    /// Thread-safe function to get a random integer within the range [<paramref name="lowerInclusive"/>, <paramref name="upperExclusive"/>).
    /// </summary>
    public static int GetInteger(int lowerInclusive, int upperExclusive)
    {
        if (!_unityLoadedSet)
            SetUnityLoaded();

        if (lowerInclusive == upperExclusive - 1)
            return lowerInclusive;

        return _unityLoaded && GameThread.IsCurrent
            ? UnityEngine.Random.Range(lowerInclusive, upperExclusive)
            : GetNonGameThreadRandom().Next(lowerInclusive, upperExclusive);
    }

    /// <summary>
    /// Thread-safe function to get a random single-precision decimal within the range [0, 1].
    /// </summary>
    public static float GetFloat()
    {
        if (!_unityLoadedSet)
            SetUnityLoaded();

        return _unityLoaded && GameThread.IsCurrent
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
        if (!_unityLoadedSet)
            SetUnityLoaded();

        return _unityLoaded && GameThread.IsCurrent
            ? UnityEngine.Random.Range(lowerInclusive, upperInclusive)
            : (float)GetNonGameThreadRandom().NextDouble() * (upperInclusive - lowerInclusive) + lowerInclusive;
    }

    /// <summary>
    /// Thread-safe function to get a random double-precision decimal within the range [0, 1].
    /// </summary>
    public static double GetDouble()
    {
        if (!_unityLoadedSet)
            SetUnityLoaded();

        return _unityLoaded && GameThread.IsCurrent
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
        return GetDouble() * (upperInclusive - lowerInclusive) + lowerInclusive;
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
            if (pick < totalWeight)
                return i;
        }
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
            if (pick < totalWeight)
                return i;
        }

        return list.Count - 1;
    }

    /// <summary>
    /// Thread-safe function to get a set number of random values with no repeats from <paramref name="list"/>. If more than are in list are requested, less will be returned.
    /// </summary>
    public static T[] GetRandomValues<T>(IReadOnlyList<T> list, Func<T, float> weightSelector, int amount)
    {
        if (list == null || list.Count == 0 || amount == 0)
        {
            return Array.Empty<T>();
        }

        if (amount < 0)
            amount = list.Count;

        if (amount == 1)
        {
            return [ list[GetIndex(list, weightSelector)] ];
        }

        List<T> copy = list.ToList();

        float totalWeight = 0;
        for (int i = 0; i < list.Count; ++i)
        {
            totalWeight += weightSelector(list[i]);
        }

        amount = Math.Min(amount, list.Count);
        
        T[] results = new T[amount];

        for (int c = 0; c < amount; ++c)
        {
            float pick = GetFloat(0, totalWeight);
            float weightProgress = 0;
            int index = list.Count - 1;
            for (int i = 0; i < copy.Count; ++i)
            {
                weightProgress += weightSelector(copy[i]);
                if (pick >= weightProgress)
                    continue;

                index = i;
                break;
            }

            T result = copy[index];
            results[c] = result;
            copy.RemoveAt(index);

            totalWeight -= weightSelector(result);
        }

        return results;
    }

    /// <summary>
    /// Thread-safe function to get a set number of random values with no repeats from <paramref name="list"/>. If more than are in list are requested, less will be returned.
    /// </summary>
    public static T[] GetRandomValues<T>(IReadOnlyList<T> list, Func<T, double> weightSelector, int amount)
    {
        if (list == null || list.Count == 0 || amount == 0)
        {
            return Array.Empty<T>();
        }

        if (amount < 0)
            amount = list.Count;

        if (amount == 1)
        {
            return [ list[GetIndex(list, weightSelector)] ];
        }

        List<T> copy = list.ToList();

        double totalWeight = 0;
        for (int i = 0; i < list.Count; ++i)
        {
            totalWeight += weightSelector(list[i]);
        }

        amount = Math.Min(amount, list.Count);
        
        T[] results = new T[amount];

        for (int c = 0; c < amount; ++c)
        {
            double pick = GetDouble(0, totalWeight);
            double weightProgress = 0;
            int index = list.Count - 1;
            for (int i = 0; i < copy.Count; ++i)
            {
                weightProgress += weightSelector(copy[i]);
                if (pick >= weightProgress)
                    continue;

                index = i;
                break;
            }

            T result = copy[index];
            results[c] = result;
            copy.RemoveAt(index);

            totalWeight -= weightSelector(result);
        }

        return results;
    }
}
