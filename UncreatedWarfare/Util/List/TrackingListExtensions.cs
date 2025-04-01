using System;
using System.Collections.Generic;

namespace Uncreated.Warfare.Util.List;
public static class TrackingListExtensions
{
    /// <summary>
    /// Pass through the hashing capability of <see cref="TrackingList{T}"/> without actually copying the full list.
    /// </summary>
    public static TrackingWhereEnumerable<T> Where<T>(TrackingList<T> list, Func<T, bool> predicate)
    {
        if (list == null)
            throw new ArgumentNullException(nameof(list));

        return new TrackingWhereEnumerable<T>(list, predicate ?? (_ => true));
    }

    /// <summary>
    /// Pass through the hashing capability of <see cref="ReadOnlyTrackingList{T}"/> without actually copying the full list.
    /// </summary>
    public static TrackingWhereEnumerable<T> Where<T>(ReadOnlyTrackingList<T> list, Func<T, bool> predicate)
    {
        if (list == null)
            throw new ArgumentNullException(nameof(list));

        return new TrackingWhereEnumerable<T>(list, predicate ?? (_ => true));
    }

    /// <summary>
    /// Create a <see cref="TrackingList{T}"/> from an existing <paramref name="enumerable"/>.
    /// </summary>
    public static TrackingList<T> ToTrackingList<T>(this IEnumerable<T> enumerable)
    {
        TrackingList<T> list = new TrackingList<T>(enumerable switch
        {
            ICollection c => c.Count,
            ICollection<T> c => c.Count,
            _ => 16
        });

        foreach (T item in enumerable)
        {
            list.Add(item);
        }

        return list;
    }

    /// <summary>
    /// Create a <see cref="ReadOnlyTrackingList{T}"/> from an existing <paramref name="enumerable"/>.
    /// </summary>
    public static ReadOnlyTrackingList<T> ToReadOnlyTrackingList<T>(this IEnumerable<T> enumerable)
    {
        return enumerable.ToTrackingList().AsReadOnly();
    }
}
