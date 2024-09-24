using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace Uncreated.Warfare.Util.List;
public class TrackingList<T> : IList<T>
{
    private readonly HashSet<T> _set;
    private readonly List<T> _list;

    public int Count => _list.Count;

    public bool IsReadOnly => false;

    public T this[int index] { get => _list[index]; set => _list[index] = value; }

    public TrackingList()
    {
        _list = new List<T>();
        _set = new HashSet<T>();
    }
    
    public TrackingList(int capacity)
    {
        _list = new List<T>(capacity);
        _set = new HashSet<T>(capacity);
    }

    public List<T>.Enumerator GetEnumerator() => _list.GetEnumerator();
    IEnumerator<T> IEnumerable<T>.GetEnumerator() => _list.GetEnumerator();

    IEnumerator IEnumerable.GetEnumerator() => _list.GetEnumerator();

    public int IndexOf(T item) => _list.IndexOf(item);

    public void Insert(int index, T item)
    {
        _list.Insert(index, item);
        _set.Add(item);
    }

    public bool Remove(T item)
    {
        _set.Remove(item);
        return _list.Remove(item);
    }

    public void RemoveAt(int index)
    {
        if (index >= Count)
            throw new IndexOutOfRangeException();

        T item = _list[index];
        _list.RemoveAt(index);
        _set.Remove(item);
    }

    public void Add(T item)
    {
        if (_set.Contains(item))
            throw new ArgumentException("Item already exists in list");

        _list.Add(item);
        _set.Add(item);
    }

    public void Clear()
    {
        _list.Clear();
        _set.Clear();
    }

    public bool Contains(T item) => _set.Contains(item);

    public void CopyTo(T[] array, int arrayIndex)
    {
        _list.CopyTo(array, arrayIndex);
    }

    // extra list stuff
    public bool AddIfNotExists(T item)
    {
        if (_set.Add(item))
        {
            _list.Add(item);
            return true;
        }

        return false;
    }
    public void RemoveAll(Predicate<T> predicate)
    {
        _list.RemoveAll(predicate);
        _set.RemoveWhere(predicate);
    }

    public bool TryGet(int index, [MaybeNullWhen(false)] out T item)
    {
        item = default;
        if (index < Count)
        {
            item = _list[index];
            return true;
        }
        return false;
    }
    public ReadOnlyTrackingList<T> AsReadOnly() => new ReadOnlyTrackingList<T>(this);
}

public class ReadOnlyTrackingList<T> : IReadOnlyList<T>
{
    private readonly TrackingList<T> _list;

    public int Count => _list.Count;

    public T this[int index] { get => _list[index]; set => _list[index] = value; }

    public ReadOnlyTrackingList(TrackingList<T> list)
    {
        _list = list;
    }

    public List<T>.Enumerator GetEnumerator() => _list.GetEnumerator();

    IEnumerator<T> IEnumerable<T>.GetEnumerator() => ((IEnumerable<T>)_list).GetEnumerator();

    IEnumerator IEnumerable.GetEnumerator() => ((IEnumerable)_list).GetEnumerator();

    public int IndexOf(T item) => _list.IndexOf(item);

    public bool Contains(T item) => _list.Contains(item);

    public void CopyTo(T[] array, int arrayIndex) => _list.CopyTo(array, arrayIndex);

    public bool TryGet(int index, out T? item) => _list.TryGet(index, out item);
}