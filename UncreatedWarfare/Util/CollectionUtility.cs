using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;

namespace Uncreated.Warfare.Util;
public static class CollectionUtility
{
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

        int amt = input.Count(' ');
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
        return list[index];
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

        int amt = input.Count(' ');
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
    /// Adds an element to a position in the array.
    /// </summary>
    public static T[] AddToArray<T>([NotNullIfNotNull(nameof(array))] T[]? array, T value, int index = -1)
    {
        AddToArray(ref array, value, index);
        return array!;
    }

    /// <summary>
    /// Adds an element to a position in the array.
    /// </summary>
    public static void AddToArray<T>([NotNullIfNotNull(nameof(array))] ref T[]? array, T value, int index = -1)
    {
        if (array == null || array.Length == 0)
        {
            array = [ value ];
            return;
        }
        if (index < 0)
            index = array.Length;
        T[] old = array;
        array = new T[old.Length + 1];
        if (index != 0)
            Array.Copy(old, array, index);
        if (index != old.Length)
            Array.Copy(old, index, array, index + 1, old.Length - index);
        array[index] = value;
    }

    /// <summary>
    /// Removes an element from an index in the array.
    /// </summary>
    /// <exception cref="ArgumentNullException"/>
    /// <exception cref="ArgumentOutOfRangeException"/>
    public static T[] RemoveFromArray<T>(T[] array, int index)
    {
        RemoveFromArray(ref array, index);
        return array;
    }

    /// <summary>
    /// Removes an element from an index in the array.
    /// </summary>
    /// <exception cref="ArgumentNullException"/>
    /// <exception cref="ArgumentOutOfRangeException"/>
    public static void RemoveFromArray<T>(ref T[] array, int index)
    {
        if (array == null)
            throw new ArgumentNullException(nameof(array));
        
        if (index < 0 || index >= array.Length)
            throw new ArgumentOutOfRangeException(nameof(index), "Index out of bounds of the array.");

        T[] old = array;
        array = new T[old.Length - 1];
        if (index != 0)
            Array.Copy(old, 0, array, 0, index);
        if (index != array.Length)
            Array.Copy(old, index + 1, array, index, array.Length - index);
    }
}
