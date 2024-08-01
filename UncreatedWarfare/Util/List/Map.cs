using System;
using System.Collections.Generic;

namespace Uncreated.Warfare.Util.List;

/// <summary>
/// Two-way dictionary.
/// </summary>
public class Map<T1, T2> : IDictionary<T1, T2>
{
    private readonly Dictionary<T1, T2> _forward;
    private readonly Dictionary<T2, T1> _reverse;
    public int Count => _forward.Count;
    public Map() : this(0, null, null) { }
    public Map(int capacity) : this(capacity, null, null) { }
    public Map(int capacity, IEqualityComparer<T1>? comparer1) : this(capacity, comparer1, null) { }
    public Map(int capacity, IEqualityComparer<T2>? comparer2) : this(capacity, null, comparer2) { }
    public Map(int capacity, IEqualityComparer<T1>? comparer1, IEqualityComparer<T2>? comparer2)
    {
        _forward = new Dictionary<T1, T2>(capacity, comparer1);
        _reverse = new Dictionary<T2, T1>(capacity, comparer2);
    }

    /// <exception cref="ArgumentException">Either key is already in the map.</exception>
    public void Add(T1 item1, T2 item2)
    {
        if (_forward.ContainsKey(item1))
            throw new ArgumentException("Adding duplicate item (T1).", nameof(item1));
        if (_reverse.ContainsKey(item2))
            throw new ArgumentException("Adding duplicate item (T2).", nameof(item2));
        _forward.Add(item1, item2);
        _reverse.Add(item2, item1);
    }

    public void SetOrAdd(T1 item1, T2 item2)
    {
        if (_forward.ContainsKey(item1))
            RemoveT1(item1);
        if (_reverse.ContainsKey(item2))
            RemoveT2(item2);
        _forward.Add(item1, item2);
        _reverse.Add(item2, item1);
    }

    public void Clear()
    {
        _forward.Clear();
        _reverse.Clear();
    }

    public bool Contains(T1 key) => ContainsT1(key);
    public bool Contains(T2 key) => ContainsT2(key);
    public bool Contains(T1 item1, T2 item2) => ContainsT1(item1) || ContainsT2(item2);

    public bool Remove(T1 key) => RemoveT1(key);
    public bool Remove(T2 key) => RemoveT2(key);

    public bool TryGetValue(T1 key, out T2 value) => _forward.TryGetValue(key, out value);
    public bool TryGetValue(T2 key, out T1 value) => _reverse.TryGetValue(key, out value);

    // These are in case the two types are the same.

    public bool ContainsT1(T1 key) => _forward.ContainsKey(key);
    public bool ContainsT2(T2 key) => _reverse.ContainsKey(key);
    public bool RemoveT1(T1 key)
    {
        if (!_forward.ContainsKey(key))
            return false;
        T2 value = _forward[key];
        _forward.Remove(key);
        _reverse.Remove(value);
        return true;
    }
    public bool RemoveT2(T2 key)
    {
        if (!_reverse.ContainsKey(key))
            return false;
        T1 value = _reverse[key];
        _reverse.Remove(key);
        _forward.Remove(value);
        return true;
    }
    public bool TryGetValueT1(T1 key, out T2 value) => _forward.TryGetValue(key, out value);
    public bool TryGetValueT2(T2 key, out T1 value) => _reverse.TryGetValue(key, out value);


    public T2 this[T1 key]
    {
        get => _forward[key];
        set
        {
            _forward[key] = value;
            _reverse[value] = key;
        }
    }
    public T1 this[T2 key]
    {
        get => _reverse[key];
        set
        {
            _reverse[key] = value;
            _forward[value] = key;
        }
    }

    IEnumerator<KeyValuePair<T1, T2>> IEnumerable<KeyValuePair<T1, T2>>.GetEnumerator() => _forward.GetEnumerator();
    IEnumerator IEnumerable.GetEnumerator() => _forward.GetEnumerator();
    void ICollection<KeyValuePair<T1, T2>>.Add(KeyValuePair<T1, T2> item) => Add(item.Key, item.Value);
    bool ICollection<KeyValuePair<T1, T2>>.IsReadOnly => false;
    bool ICollection<KeyValuePair<T1, T2>>.Contains(KeyValuePair<T1, T2> item) => ContainsT1(item.Key) || ContainsT2(item.Value);
    void ICollection<KeyValuePair<T1, T2>>.CopyTo(KeyValuePair<T1, T2>[] array, int arrayIndex) => (_forward as ICollection<KeyValuePair<T1, T2>>).CopyTo(array, arrayIndex);
    bool ICollection<KeyValuePair<T1, T2>>.Remove(KeyValuePair<T1, T2> item) => RemoveT1(item.Key);
    bool IDictionary<T1, T2>.ContainsKey(T1 key) => ContainsT1(key);
    bool IDictionary<T1, T2>.Remove(T1 key) => RemoveT1(key);
    bool IDictionary<T1, T2>.TryGetValue(T1 key, out T2 value) => TryGetValueT1(key, out value);

    ICollection<T1> IDictionary<T1, T2>.Keys => _forward.Keys;
    ICollection<T2> IDictionary<T1, T2>.Values => _forward.Values;
}
