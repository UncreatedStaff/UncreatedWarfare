using System;
using System.Collections.Generic;
using System.Linq;

namespace Uncreated.Warfare.Util.List;

/// <summary>
/// Only selects elements that the predicate returns <see langword="true"/> for.
/// </summary>
public readonly struct TrackingWhereEnumerable<T> : IEnumerable<T>
{
    private readonly object? _list;
    private readonly Func<T, bool> _predicate;

    public TrackingWhereEnumerable(TrackingList<T> list, Func<T, bool> predicate)
    {
        _list = list ?? throw new ArgumentNullException(nameof(list));
        _predicate = predicate;
    }

    public TrackingWhereEnumerable(ReadOnlyTrackingList<T> list, Func<T, bool> predicate)
    {
        _list = list ?? throw new ArgumentNullException(nameof(list));
        _predicate = predicate;
    }

    public bool Contains(T value)
    {
        if (_list is TrackingList<T> writeList)
            return writeList.Contains(value) && _predicate(value);

        return ((ReadOnlyTrackingList<T>)_list!).Contains(value) && _predicate(value);
    }

    public IEnumerator<T> GetEnumerator()
    {
        // theres already an extension method called Where so we need to explicitly use the LINQ one.
        if (_list is TrackingList<T> writeList)
            return Enumerable.Where(writeList, _predicate).GetEnumerator();

        return Enumerable.Where((ReadOnlyTrackingList<T>)_list!, _predicate).GetEnumerator();
    }

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
}