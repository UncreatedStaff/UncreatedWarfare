using System;
using System.Collections.Generic;

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

    public Enumerator GetEnumerator()
    {
        return new Enumerator(_list is TrackingList<T> tl ? tl.GetEnumerator() : ((ReadOnlyTrackingList<T>?)_list!).GetEnumerator(), _predicate);
    }

    IEnumerator<T> IEnumerable<T>.GetEnumerator() => GetEnumerator();
    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

    public struct Enumerator : IEnumerator<T>
    {
        private readonly Func<T, bool> _predicate;
        private List<T>.Enumerator _enumerator;
        public Enumerator(List<T>.Enumerator enumerator, Func<T, bool> predicate)
        {
            _enumerator = enumerator;
            _predicate = predicate;
        }

        public bool MoveNext()
        {
            while (_enumerator.MoveNext())
            {
                if (_predicate(_enumerator.Current))
                    return true;
            }

            return false;
        }

        public void Reset()
        {
            IEnumerator enumerator = _enumerator;
            enumerator.Reset();
            _enumerator = (List<T>.Enumerator)enumerator;
        }

        public T Current => _enumerator.Current;
        object? IEnumerator.Current => _enumerator.Current;
        public void Dispose()
        {
            _enumerator.Dispose();
        }
    }
}