using DanielWillett.ReflectionTools;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Runtime.CompilerServices;

namespace Uncreated.Warfare.Util;
public static class CollectionUtility
{
    /// <summary>
    /// Adds an element to an <paramref name="array"/> and returns the new array.
    /// </summary>
    [MustUseReturnValue]
    public static TElement[] AddToArray<TElement>(TElement[]? array, TElement value)
    {
        AddToArray(ref array, value);
        return array!;
    }

    /// <summary>
    /// Inserts an element into an <paramref name="array"/> and returns the new array.
    /// </summary>
    /// <exception cref="ArgumentOutOfRangeException"/>
    [MustUseReturnValue]
    public static TElement[] AddToArray<TElement>(TElement[]? array, TElement value, int index)
    {
        AddToArray(ref array, value, index);
        return array!;
    }

    /// <summary>
    /// Removes an element from an index in the array.
    /// </summary>
    /// <exception cref="ArgumentNullException"/>
    /// <exception cref="ArgumentOutOfRangeException"/>
    [MustUseReturnValue]
    public static TElement[] RemoveFromArray<TElement>(TElement[] array, int index)
    {
        RemoveFromArray(ref array, index);
        return array;
    }

    /// <summary>
    /// Adds an element to an <paramref name="array"/>.
    /// </summary>
#nullable disable
    public static void AddToArray<TElement>(ref TElement[] array, TElement value)
#nullable restore
    {
        if (array == null || array.Length == 0)
        {
            array = [ value ];
            return;
        }

        int oldLength = array.Length;

        TElement[] newArray = new TElement[oldLength + 1];

        if (typeof(TElement).IsPrimitive)
        {
            Buffer.BlockCopy(array, 0, newArray, 0, Unsafe.SizeOf<TElement>() * oldLength);
        }
        else
        {
            Array.Copy(array, newArray, oldLength);
        }

        newArray[oldLength] = value;
        array = newArray;
    }

    /// <summary>
    /// Inserts an element into an <paramref name="array"/>.
    /// </summary>
    /// <exception cref="ArgumentOutOfRangeException"/>
#nullable disable
    public static void AddToArray<TElement>(ref TElement[] array, TElement value, int index)
#nullable restore
    {
        if (array == null || array.Length == 0)
        {
            if (index != 0)
                throw new ArgumentOutOfRangeException(nameof(index));
            array = [ value ];
            return;
        }

        int oldLength = array.Length;

        if (index < 0 || index > oldLength)
            throw new ArgumentOutOfRangeException(nameof(index));

        TElement[] newArray = new TElement[oldLength + 1];

        if (typeof(TElement).IsPrimitive)
        {
            if (index != 0)
                Buffer.BlockCopy(array, 0, newArray, 0, Unsafe.SizeOf<TElement>() * index);
            if (index != oldLength)
                Buffer.BlockCopy(array, Unsafe.SizeOf<TElement>() * index, newArray, Unsafe.SizeOf<TElement>() * (index + 1), Unsafe.SizeOf<TElement>() * (oldLength - index));
        }
        else
        {
            if (index != 0)
                Array.Copy(array, 0, newArray, 0, index);
            if (index != oldLength)
                Array.Copy(array, index, newArray, index + 1, oldLength - index);
        }

        newArray[index] = value;
        array = newArray;
    }

    /// <summary>
    /// Removes an element from an index in the array.
    /// </summary>
    /// <exception cref="ArgumentNullException"/>
    /// <exception cref="ArgumentOutOfRangeException"/>
    public static void RemoveFromArray<TElement>(ref TElement[] array, int index)
    {
        if (array == null)
            throw new ArgumentNullException(nameof(array));

        if (index < 0 || index >= array.Length)
            throw new ArgumentOutOfRangeException(nameof(index), "Index out of bounds of the array.");

        int oldLength = array.Length;
        TElement[] newArray = new TElement[oldLength - 1];

        if (typeof(TElement).IsPrimitive)
        {
            if (index != 0)
                Buffer.BlockCopy(array, 0, newArray, 0, Unsafe.SizeOf<TElement>() * index);
            if (index != oldLength - 1)
                Buffer.BlockCopy(array, Unsafe.SizeOf<TElement>() * (index + 1), newArray, Unsafe.SizeOf<TElement>() * index, Unsafe.SizeOf<TElement>() * (oldLength - index - 1));
        }
        else
        {
            if (index != 0)
                Array.Copy(array, 0, newArray, 0, index);
            if (index != oldLength - 1)
                Array.Copy(array, index + 1, newArray, index, oldLength - index - 1);
        }
    }

    /// <summary>
    /// Compares two byte arrays.
    /// </summary>
    public static bool CompareBytes(byte[]? arr1, byte[]? arr2)
    {
        if (ReferenceEquals(arr1, arr2))
            return true;

        return arr1 != null && arr2 != null && arr1.Length == arr2.Length && arr1.AsSpan().SequenceEqual(arr2);
    }

    /// <summary>
    /// Check if a collection is <see langword="null"/> or if it has no elements.
    /// </summary>
    public static bool IsNullOrEmpty<T>(this ICollection<T>? collection)
    {
        return collection is not { Count: > 0 };
    }

    /// <summary>
    /// Search through a list for the first index of an object matching searched text.
    /// </summary>
    /// <param name="equalsOnly">Text must match exactly to be returned.</param>
    public static int StringIndexOf<T>(IReadOnlyList<T> collection, Func<T, string?> selector, ReadOnlySpan<char> input, bool equalsOnly = false)
    {
        if (input.Length == 0)
            return -1;

        for (int i = 0; i < collection.Count; ++i)
        {
            if (input.Equals(selector(collection[i]), StringComparison.InvariantCultureIgnoreCase))
                return i;
        }

        if (equalsOnly)
            return -1;
        
        for (int i = 0; i < collection.Count; ++i)
        {
            string? n = selector(collection[i]);
            if (n != null && n.AsSpan().IndexOf(input, StringComparison.InvariantCultureIgnoreCase) != -1)
                return i;
        }

        int amt = input.Count(' ') + 1;
        Span<Range> splitAlloc = stackalloc Range[amt];

        splitAlloc = splitAlloc[input.Split(splitAlloc, ' ', trimEachEntry: true, options: StringSplitOptions.RemoveEmptyEntries)..];
        if (splitAlloc.Length == 0)
            return -1;

        for (int i = 0; i < collection.Count; ++i)
        {
            string? name = selector(collection[i]);
            if (name == null)
                continue;

            bool all = true;
            for (int j = 0; j < splitAlloc.Length; ++j)
            {
                ReadOnlySpan<char> word = input[splitAlloc[j]];

                if (name.AsSpan().IndexOf(word, StringComparison.InvariantCultureIgnoreCase) != -1)
                    continue;

                all = false;
                break;
            }

            if (all)
                return i;
        }

        return -1;
    }

    /// <summary>
    /// Search through a list for the first object matching searched text.
    /// </summary>
    /// <param name="equalsOnly">Text must match exactly to be returned.</param>
    public static T? StringFind<T>(IEnumerable<T> set, Func<T, string?> selector, ReadOnlySpan<char> input, bool equalsOnly = false)
    {
        IReadOnlyList<T> list = (set as IReadOnlyList<T>) ?? set.ToList();
        int index = StringIndexOf(list, selector, input, equalsOnly);
        return index < 0 ? default : list[index];
    }

    /// <summary>
    /// Search through a list for all objects matching searched text.
    /// </summary>
    /// <param name="equalsOnly">Text must match exactly to be returned.</param>
    /// <returns>Number of results.</returns>
    public static int StringSearch<T>(IEnumerable<T> set, IList<T> results, Func<T, string?> selector, ReadOnlySpan<char> input, bool equalsOnly = false)
    {
        if (input.Length == 0)
            return 0;

        IReadOnlyList<T> collection = (set as IReadOnlyList<T>) ?? set.ToList();

        for (int i = 0; i < collection.Count; ++i)
        {
            T obj = collection[i];
            if (input.Equals(selector(obj), StringComparison.InvariantCultureIgnoreCase))
                results.Add(obj);
        }

        if (equalsOnly)
            return -1;

        for (int i = 0; i < collection.Count; ++i)
        {
            T obj = collection[i];
            string? n = selector(obj);
            if (n != null && n.AsSpan().IndexOf(input, StringComparison.InvariantCultureIgnoreCase) != -1)
                results.Add(obj);
        }

        int amt = input.Count(' ') + 1;
        Span<Range> splitAlloc = stackalloc Range[amt];

        splitAlloc = splitAlloc[input.Split(splitAlloc, ' ', trimEachEntry: true, options: StringSplitOptions.RemoveEmptyEntries)..];
        if (splitAlloc.Length == 0)
            return -1;

        for (int i = 0; i < collection.Count; ++i)
        {
            T obj = collection[i];
            string? name = selector(obj);
            if (name == null)
                continue;

            bool all = true;
            for (int j = 0; j < splitAlloc.Length; ++j)
            {
                ReadOnlySpan<char> word = input[splitAlloc[j]];

                if (name.AsSpan().IndexOf(word, StringComparison.InvariantCultureIgnoreCase) != -1)
                    continue;

                all = false;
                break;
            }

            if (all)
                results.Add(obj);
        }

        return -1;
    }

    /// <summary>
    /// Creates a copy of a byte array.
    /// </summary>
    public static byte[] CloneBytes(this byte[] source, int index = 0, int length = -1)
    {
        if (source == null)
            return null!;
        if (source.Length == 0)
            return Array.Empty<byte>();
        if (index >= source.Length)
            index = source.Length - 1;
        if (length < 0 || length + index > source.Length)
            length = source.Length - index;
        if (length == 0)
            return Array.Empty<byte>();

        byte[] result = new byte[length];
        Buffer.BlockCopy(source, index, result, 0, length);
        return result;
    }

    /// <summary>
    /// Creates a deep copy of an array as an array, using <see cref="ICloneable"/> to clone all elements.
    /// </summary>
    public static T[] CloneArray<T>(T[] source, int index = 0, int length = -1) where T : ICloneable
    {
        if (source == null)
            return null!;
        if (source.Length == 0)
            return Array.Empty<T>();
        if (index >= source.Length)
            index = source.Length - 1;
        if (length < 0 || length + index > source.Length)
            length = source.Length - index;
        if (length == 0)
            return Array.Empty<T>();
        T[] result = new T[length];
        for (int i = 0; i < length; ++i)
            result[i] = (T)source[i + index].Clone();
        return result;
    }

    /// <summary>
    /// Creates a shallow copy of a list as an array, using <see cref="ICloneable"/> to clone all elements.
    /// </summary>
    public static T[] CloneReadOnlyList<T>(IReadOnlyList<T> source, int index = 0, int length = -1) where T : ICloneable
    {
        if (source == null)
            return null!;
        if (source.Count == 0)
            return Array.Empty<T>();
        if (index >= source.Count)
            index = source.Count - 1;
        if (length < 0 || length + index > source.Count)
            length = source.Count - index;
        if (length == 0)
            return Array.Empty<T>();
        T[] result = new T[length];
        for (int i = 0; i < length; ++i)
            result[i] = (T)source[i + index].Clone();
        return result;
    }

    /// <summary>
    /// Creates a shallow copy of a list as an array, using <see cref="ICloneable"/> to clone all elements.
    /// </summary>
    public static T[] CloneList<T>(IList<T> source, int index = 0, int length = -1) where T : ICloneable
    {
        if (source == null)
            return null!;
        if (source.Count == 0)
            return Array.Empty<T>();
        if (index >= source.Count)
            index = source.Count - 1;
        if (length < 0 || length + index > source.Count)
            length = source.Count - index;
        if (length == 0)
            return Array.Empty<T>();
        T[] result = new T[length];
        for (int i = 0; i < length; ++i)
            result[i] = (T)source[i + index].Clone();
        return result;
    }

    /// <summary>
    /// Creates a shallow copy of a list as an array.
    /// </summary>
    public static T[] CloneStructArray<T>(T[] source, int index = 0, int length = -1) where T : struct
    {
        if (source == null)
            return null!;
        if (source.Length == 0)
            return Array.Empty<T>();
        if (index >= source.Length)
            index = source.Length - 1;
        if (length < 0 || length + index > source.Length)
            length = source.Length - index;
        if (length == 0)
            return Array.Empty<T>();
        T[] result = new T[length];
        Array.Copy(source, index, result, 0, length);
        return result;
    }

    /// <summary>
    /// Creates a shallow copy of a list as an array.
    /// </summary>
    public static T[] CloneReadOnlyStructList<T>(IReadOnlyList<T> source, int index = 0, int length = -1) where T : struct
    {
        if (source == null)
            return null!;
        if (source.Count == 0)
            return Array.Empty<T>();
        if (index >= source.Count)
            index = source.Count - 1;
        if (length < 0 || length + index > source.Count)
            length = source.Count - index;
        if (length == 0)
            return Array.Empty<T>();
        T[] result = new T[length];
        if (source is List<T> list)
            list.CopyTo(index, result, 0, length);
        else
        {
            for (int i = 0; i < source.Count; ++i)
                result[i] = source[i];
        }
        return result;
    }

    /// <summary>
    /// Creates a shallow copy of a list as an array.
    /// </summary>
    public static T[] CloneStructList<T>(IList<T> source, int index = 0, int length = -1) where T : struct
    {
        if (source == null)
            return null!;
        if (source.Count == 0)
            return Array.Empty<T>();
        if (index >= source.Count)
            index = source.Count - 1;
        if (length < 0 || length + index > source.Count)
            length = source.Count - index;
        if (length == 0)
            return Array.Empty<T>();
        T[] result = new T[length];
        if (source is List<T> list)
            list.CopyTo(index, result, 0, length);
        else
        {
            for (int i = 0; i < source.Count; ++i)
                result[i] = source[i];
        }
        return result;
    }

    /// <summary>
    /// Find a value from an array with the index of each element in the predicate.
    /// </summary>
    public static TElement? FindIndexed<TElement>(this TElement[] array, Func<TElement, int, bool> predicate)
    {
        for (int i = 0; i < array.Length; ++i)
        {
            if (predicate(array[i], i))
                return array[i];
        }

        return default;
    }

    /// <summary>
    /// Convert to an array if it isn't already.
    /// </summary>
    public static TElement[] ToArrayFast<TElement>(this IEnumerable<TElement> enumerable, bool copy = false)
    {
        if (!copy && enumerable is TElement[] array)
            return array;

        if (enumerable is List<TElement> list)
        {
            if (!copy && list.Count == list.Capacity && Accessor.TryGetUnderlyingArray(list, out TElement[] underlying))
                return underlying;

            return list.ToArray();
        }

        return enumerable.ToArray();
    }

    /// <summary>
    /// Extract a span from a list.
    /// </summary>
    public static ReadOnlySpan<TElement> AsSpan<TElement>(this List<TElement> list)
    {
        return list.GetUnderlyingArrayOrCopy().AsSpan(0, list.Count);
    }

    /// <summary>
    /// Updates the value in a <see cref="ConcurrentDictionary{TKey,TValue}"/> and returns the old value.
    /// </summary>
    /// <returns>Whether or not the value was already in the dictionary.</returns>
    public static bool AddOrUpdate<TKey, TValue>(
        this ConcurrentDictionary<TKey, TValue> dict,
        TKey key,
        TValue newValue,
        [MaybeNullWhen(false)] out TValue oldValue)
    {
        if (dict == null)
            throw new ArgumentNullException(nameof(dict));

        AddOrUpdater<TValue> updater = new AddOrUpdater<TValue>(newValue);

        dict.AddOrUpdate(
            key,
            static (_, updater) =>
            {
                updater.OldValue = default;
                updater.HadValue = false;
                return updater.NewValue;
            },
            static (_, oldValue, updater) =>
            {
                updater.OldValue = oldValue;
                updater.HadValue = true;
                return updater.NewValue;
            },
            updater
        );

        oldValue = updater.OldValue;
        return updater.HadValue;
    }

    private class AddOrUpdater<TValue>(TValue value)
    {
        public readonly TValue NewValue = value;

        public TValue? OldValue;
        public bool HadValue;
    }
}

public class DistanceComparer<TValue> : IComparer<TValue>
{
    private readonly Vector3 _position;
    private readonly Func<TValue, Vector3> _getPosition;
    private readonly bool _horizontalDistanceOnly;
    private readonly bool _reverse;

    public DistanceComparer(Vector3 position, Func<TValue, Vector3> getPosition, bool horizontalDistanceOnly, bool reverse)
    {
        _position = position;
        _getPosition = getPosition;
        _horizontalDistanceOnly = horizontalDistanceOnly;
        _reverse = reverse;
    }

    /// <inheritdoc />
    public int Compare(TValue a, TValue b)
    {
        if (ReferenceEquals(a, b))
            return 0;

        Vector3 pA = _getPosition(a);
        Vector3 pB = _getPosition(b);
        float aDist = MathUtility.SquaredDistance(in pA, in _position, _horizontalDistanceOnly);
        float bDist = MathUtility.SquaredDistance(in pB, in _position, _horizontalDistanceOnly);
        return _reverse ? bDist.CompareTo(aDist) : aDist.CompareTo(bDist);
    }
}

public class LookAtComparer<TValue> : IComparer<TValue>
{
    private readonly Vector3 _lookOrigin;
    private readonly Func<TValue, Vector3> _getLookDirection;
    private readonly bool _reverse;

    public LookAtComparer(Vector3 lookOrigin, Func<TValue, Vector3> getLookDirection, bool reverse)
    {
        _lookOrigin = lookOrigin;
        _getLookDirection = getLookDirection;
        _reverse = reverse;
    }

    /// <inheritdoc />
    public int Compare(TValue a, TValue b)
    {
        if (ReferenceEquals(a, b))
            return 0;

        Vector3 dA = _getLookDirection(a);
        Vector3 dB = _getLookDirection(b);
        float aAngle = Vector3.Angle(dA, _lookOrigin);
        float bAngle = Vector3.Angle(dB, _lookOrigin);
        return _reverse ? bAngle.CompareTo(aAngle) : aAngle.CompareTo(bAngle);
    }
}