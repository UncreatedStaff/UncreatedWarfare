using DanielWillett.ReflectionTools;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace Uncreated.Warfare.Util.List;

/// <summary>
/// An implementation of <see cref="IDictionary{Tkey,TValue}"/> that uses equality operators to identify keys instead of hash codes, can be faster and have less of a memory impact in smaller collections.
/// </summary>
/// <typeparam name="TKey">Unique key in the dictionary.</typeparam>
/// <typeparam name="TValue">Non-uniuqe value in the dictionary.</typeparam>
public class LinearDictionary<TKey, TValue> : IDictionary<TKey, TValue>, IReadOnlyDictionary<TKey, TValue>, IDictionary
{
    private readonly bool _readOnly;
    private readonly IEqualityComparer<TKey> _comparer;
    private KeyValuePair<TKey, TValue>[] _items;
    private int _version;
    private int _size;

    /// <summary>
    /// The number of elements this dictionary can store before the underlying storage has to be resized.
    /// </summary>
    public int Capacity
    {
        get => _items.Length;
        set
        {
            AssertNotReadOnly();
            if (_size > value)
                throw new ArgumentOutOfRangeException(nameof(value), "Capacity can not be less than Count.");
            
            if (_items.Length == value)
                return;

            KeyValuePair<TKey, TValue>[] @new = new KeyValuePair<TKey, TValue>[value];
            Array.Copy(_items, 0, @new, 0, _size);
            _items = @new;
        }
    }

    /// <inheritdoc cref="IDictionary{TKey,TValue}.this" />
    public TValue this[TKey key]
    {
        get
        {
            int index = IndexOf(key);
            if (index < 0)
                throw new KeyNotFoundException($"Key not present in dictionary: {key}.");
            return _items[index].Value;
        }
        set
        {
            AssertNotReadOnly();
            int index = IndexOf(key);
            if (index < 0)
            {
                Append(key, value);
            }
            else
            {
                _items[index] = new KeyValuePair<TKey, TValue>(key, value);
                ++_version;
            }
        }
    }

    /// <inheritdoc />
    public void CopyTo(Array array, int index)
    {
        if (array == null)
            throw new ArgumentNullException(nameof(array));

        int length = array.Length - index;

        if (length < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(index));
        }

        length = Math.Min(length, _size);

        for (int i = 0; i < length; ++i)
        {
            array.SetValue(_items[i], i + index);
        }
    }

    /// <inheritdoc cref="IDictionary{TKey,TValue}.Count" />
    public int Count => _size;

    /// <inheritdoc cref="ICollection{T}" />
    public bool IsReadOnly => _readOnly;

    /// <inheritdoc cref="ICollection{T}.Keys" />
    public KeyCollection Keys => new KeyCollection(this);

    /// <inheritdoc cref="ICollection{T}.Keys" />
    public ValueCollection Values => new ValueCollection(this);

    public LinearDictionary(IEqualityComparer<TKey>? comparer, params KeyValuePair<TKey, TValue>[] underlyingItems) : this(comparer, false, underlyingItems) { }
    public LinearDictionary(params KeyValuePair<TKey, TValue>[] underlyingItems) : this(null, false, underlyingItems) { }

    public LinearDictionary(bool isReadOnly, params KeyValuePair<TKey, TValue>[] underlyingItems) : this(null, isReadOnly, underlyingItems) { }
    public LinearDictionary(IEqualityComparer<TKey>? comparer, bool isReadOnly, params KeyValuePair<TKey, TValue>[] underlyingItems)
    {
        _items = underlyingItems;
        _size = underlyingItems.Length;
        _readOnly = isReadOnly;
        _comparer = comparer ?? EqualityComparer<TKey>.Default;
    }

    public LinearDictionary(int capacity = 0) : this(null, capacity) { }
    public LinearDictionary(IEqualityComparer<TKey>? comparer, int capacity = 0)
    {
        if (capacity < 0)
            throw new ArgumentOutOfRangeException(nameof(capacity));

        _comparer = comparer ?? EqualityComparer<TKey>.Default;
        _items = capacity == 0 ? Array.Empty<KeyValuePair<TKey, TValue>>() : new KeyValuePair<TKey, TValue>[capacity];
    }

    private void AssertNotReadOnly()
    {
        if (_readOnly)
            throw new InvalidOperationException("This collection is read-only.");
    }

    /// <inheritdoc cref="IEnumerable{T}.GetEnumerator" />
    public Enumerator GetEnumerator()
    {
        return new Enumerator(this);
    }

    /// <inheritdoc />
    public void Add(TKey key, TValue value)
    {
        AssertNotReadOnly();
        int index = IndexOf(key);
        if (index >= 0)
            throw new ArgumentException($"Key already present in dictionary: {key}.");

        Append(key, value);
    }

    private void Append(TKey key, TValue value)
    {
        if (_size >= _items.Length)
        {
            int newSize = Math.Max(_items.Length * 2, 4);
            KeyValuePair<TKey, TValue>[] @new = new KeyValuePair<TKey, TValue>[newSize];
            Array.Copy(_items, 0, @new, 0, _size);
            _items = @new;
        }

        _items[_size] = new KeyValuePair<TKey, TValue>(key, value);
        ++_size;
        ++_version;
    }

    /// <inheritdoc cref="IDictionary{TKey,TValue}.ContainsKey" />
    public bool ContainsKey(TKey key)
    {
        return IndexOf(key) >= 0;
    }

    /// <inheritdoc />
    public bool Remove(TKey key)
    {
        AssertNotReadOnly();
        int index = IndexOf(key);
        if (index < 0)
            return false;

        RemoveAt(index);
        return true;
    }

#pragma warning disable CS8767
    /// <inheritdoc cref="IDictionary{TKey,TValue}.TryGetValue" />
    public bool TryGetValue(TKey key, [MaybeNullWhen(false)] out TValue value)
    {
        int length = _size;
        for (int i = 0; i < length; ++i)
        {
            ref KeyValuePair<TKey, TValue> item = ref _items[i];
            if (!_comparer.Equals(item.Key, key))
                continue;

            value = item.Value;
            return true;
        }

        value = default;
        return false;
    }
#pragma warning restore CS8767

    /// <summary>
    /// Remove an item by it's key and output the old value.
    /// </summary>
    /// <returns><see langword="true"/> if the object is found and removed, otherwise <see langword="false"/>.</returns>
    public bool TryRemove(TKey key, [MaybeNullWhen(false)] out TValue value)
    {
        AssertNotReadOnly();
        int length = _size;
        for (int i = 0; i < length; ++i)
        {
            ref KeyValuePair<TKey, TValue> item = ref _items[i];
            if (!_comparer.Equals(item.Key, key))
                continue;

            value = item.Value;
            RemoveAt(i);
            return true;
        }

        value = default;
        return false;
    }

    /// <summary>
    /// Add an item if it's key doesn't already exist.
    /// </summary>
    /// <returns><see langword="true"/> if the object is not found and added, otherwise <see langword="false"/>.</returns>
    public bool GetOrAdd(TKey key, [MaybeNullWhen(true)] out TValue existingValue, Func<TValue> factory)
    {
        AssertNotReadOnly();
        int length = _size;
        for (int i = 0; i < length; ++i)
        {
            ref KeyValuePair<TKey, TValue> item = ref _items[i];
            if (!_comparer.Equals(item.Key, key))
                continue;

            existingValue = item.Value;
            return false;
        }

        Append(key, factory());
        existingValue = default;
        return true;
    }

    /// <summary>
    /// Add an item if it's key doesn't already exist.
    /// </summary>
    /// <returns><see langword="true"/> if the object is not found and added, otherwise <see langword="false"/>.</returns>
    public bool GetOrAdd(TKey key, [MaybeNullWhen(true)] out TValue existingValue, Lazy<TValue> value)
    {
        AssertNotReadOnly();
        int length = _size;
        for (int i = 0; i < length; ++i)
        {
            ref KeyValuePair<TKey, TValue> item = ref _items[i];
            if (!EqualityComparer<TKey>.Default.Equals(item.Key, key))
                continue;

            existingValue = item.Value;
            return false;
        }

        Append(key, value.Value);
        existingValue = default;
        return true;
    }

    /// <summary>
    /// Add an item if it's key doesn't already exist.
    /// </summary>
    /// <returns><see langword="true"/> if the object is not found and added, otherwise <see langword="false"/>.</returns>
    public bool GetOrAdd<TState>(TKey key, [MaybeNullWhen(true)] out TValue existingValue, in TState state, Func<TState, TValue> value)
    {
        AssertNotReadOnly();
        int length = _size;
        for (int i = 0; i < length; ++i)
        {
            ref KeyValuePair<TKey, TValue> item = ref _items[i];
            if (!_comparer.Equals(item.Key, key))
                continue;

            existingValue = item.Value;
            return false;
        }

        Append(key, value(state));
        existingValue = default;
        return true;
    }

    /// <inheritdoc cref="ICollection{T}.Clear" />
    public void Clear()
    {
        AssertNotReadOnly();
        _size = 0;
        Array.Clear(_items, 0, _size);
        ++_version;
    }

    /// <inheritdoc />
    public void CopyTo(KeyValuePair<TKey, TValue>[] array, int arrayIndex)
    {
        if (array == null)
            throw new ArgumentNullException(nameof(array));

        int length = array.Length - arrayIndex;

        if (length < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(arrayIndex));
        }

        length = Math.Min(length, _size);

        for (int i = 0; i < length; ++i)
        {
            array[i + arrayIndex] = _items[i];
        }
    }

    private int IndexOf(TKey key)
    {
        int length = _size;
        for (int i = 0; i < length; ++i)
        {
            if (_comparer.Equals(_items[i].Key, key))
            {
                return i;
            }
        }

        return -1;
    }

    private void RemoveAt(int index)
    {
        if (index >= _size)
            throw new ArgumentOutOfRangeException(nameof(index));

        --_size;

        if (index == _size)
        {
            _items[index] = default;
        }
        else
        {
            ref KeyValuePair<TKey, TValue> element = ref _items[index];
            ref KeyValuePair<TKey, TValue> swap = ref _items[_size];
            element = swap;
            swap = default;
        }

        ++_version;
    }

    /// <summary>
    /// Gets a key that passes a <paramref name="predicate"/>.
    /// </summary>
    public bool TryGetKey(Func<TValue, bool> predicate, [MaybeNullWhen(false)] out TKey key)
    {
        for (int i = 0; i < _items.Length; ++i)
        {
            ref KeyValuePair<TKey, TValue> kvp = ref _items[i];
            if (predicate(kvp.Value))
            {
                key = kvp.Key;
                return true;
            }
        }

        key = default;
        return false;
    }

    /// <summary>
    /// Gets a key that has a value equal to <paramref name="value"/>.
    /// </summary>
    public bool TryGetKey(TValue value, [MaybeNullWhen(false)] out TKey key)
    {
        IEqualityComparer<TValue> equalityComparer = EqualityComparer<TValue>.Default;
        for (int i = 0; i < _items.Length; ++i)
        {
            ref KeyValuePair<TKey, TValue> kvp = ref _items[i];
            if (equalityComparer.Equals(value, kvp.Value))
            {
                key = kvp.Key;
                return true;
            }
        }

        key = default;
        return false;
    }

    /// <inheritdoc />
    bool ICollection.IsSynchronized => false;

    /// <inheritdoc />
    object ICollection.SyncRoot => null!;

    /// <inheritdoc />
    bool IDictionary.IsFixedSize => false;

    /// <inheritdoc />
    object? IDictionary.this[object key]
    {
        get => this[CastKey(key)];
        set => this[CastKey(key)] = CastValue(value);
    }

    /// <inheritdoc />
    IEnumerator<KeyValuePair<TKey, TValue>> IEnumerable<KeyValuePair<TKey, TValue>>.GetEnumerator()
    {
        return new Enumerator(this);
    }

    /// <inheritdoc />
    IEnumerator IEnumerable.GetEnumerator()
    {
        return new Enumerator(this);
    }

    /// <inheritdoc />
    bool IDictionary.Contains(object key)
    {
        if (key is not TKey kval)
        {
            if (key == null && !typeof(TKey).IsValueType)
                kval = default!;
            throw new ArgumentException($"Key must be of type {Accessor.ExceptionFormatter.Format(typeof(TKey))}.", nameof(key));
        }

        return ContainsKey(kval);
    }

    /// <inheritdoc />
    IDictionaryEnumerator IDictionary.GetEnumerator()
    {
        return new Enumerator(this);
    }

    private static TKey CastKey(object? key)
    {
        if (key is TKey kval)
            return kval;

        if (key != null || typeof(TKey).IsValueType)
            throw new ArgumentException($"Key must be of type {Accessor.ExceptionFormatter.Format(typeof(TKey))}.", nameof(key));

        return default!;
    }

    private static TValue CastValue(object? value)
    {
        if (value is TValue vval)
            return vval;

        if (value != null || typeof(TValue).IsValueType)
            throw new ArgumentException($"Value must be of type {Accessor.ExceptionFormatter.Format(typeof(TValue))}.", nameof(value));

        return default!;
    }

    /// <inheritdoc />
    void IDictionary.Remove(object key)
    {
        Remove(CastKey(key));
    }

    /// <inheritdoc />
    void IDictionary.Add(object key, object value)
    {
        Add(CastKey(key), CastValue(value));
    }

    /// <inheritdoc />
    void ICollection<KeyValuePair<TKey, TValue>>.Add(KeyValuePair<TKey, TValue> item)
    {
        Add(item.Key, item.Value);
    }

    /// <inheritdoc />
    bool ICollection<KeyValuePair<TKey, TValue>>.Contains(KeyValuePair<TKey, TValue> item)
    {
        int index = IndexOf(item.Key);
        return index >= 0 && EqualityComparer<TValue>.Default.Equals(_items[index].Value, item.Value);
    }

    /// <inheritdoc />
    bool ICollection<KeyValuePair<TKey, TValue>>.Remove(KeyValuePair<TKey, TValue> item)
    {
        AssertNotReadOnly();
        int index = IndexOf(item.Key);
        if (index < 0 || !EqualityComparer<TValue>.Default.Equals(_items[index].Value, item.Value))
            return false;

        RemoveAt(index);
        return true;
    }

    /// <inheritdoc />
    ICollection<TKey> IDictionary<TKey, TValue>.Keys => new KeyCollection(this);

    /// <inheritdoc />
    ICollection<TValue> IDictionary<TKey, TValue>.Values => new ValueCollection(this);

    /// <inheritdoc />
    IEnumerable<TKey> IReadOnlyDictionary<TKey, TValue>.Keys => new KeyCollection(this);

    /// <inheritdoc />
    IEnumerable<TValue> IReadOnlyDictionary<TKey, TValue>.Values => new ValueCollection(this);

    /// <inheritdoc />
    ICollection IDictionary.Values => new ValueCollection(this);

    /// <inheritdoc />
    ICollection IDictionary.Keys => new KeyCollection(this);

    /// <summary>
    /// KeyValuePair enumerator for <see cref="LinearDictionary{TKey,TValue}"/>.
    /// </summary>
    public struct Enumerator : IEnumerator<KeyValuePair<TKey, TValue>>, IDictionaryEnumerator
    {
        private readonly int _version;
        private readonly LinearDictionary<TKey, TValue> _dictionary;
        private int _index;

        /// <inheritdoc />
        public KeyValuePair<TKey, TValue> Current => _dictionary._items[_index];

        public Enumerator(LinearDictionary<TKey, TValue> dictionary)
        {
            _dictionary = dictionary;
            _version = _dictionary._version;
            _index = -1;
        }

        /// <inheritdoc />
        public bool MoveNext()
        {
            if (_version != _dictionary._version)
            {
                throw new InvalidOperationException("Collection modified.");
            }

            ++_index;
            return _index < _dictionary._size;
        }

        /// <inheritdoc />
        public void Reset()
        {
            _index = -1;
        }

        /// <inheritdoc />
        object IEnumerator.Current => _dictionary._items[_index];

        /// <inheritdoc />
        public void Dispose() { }

        /// <inheritdoc />
        public DictionaryEntry Entry
        {
            get
            {
                ref KeyValuePair<TKey, TValue> kvp = ref _dictionary._items[_index];

                return new DictionaryEntry(kvp.Key!, kvp.Value);
            }
        }

        /// <inheritdoc />
        public object? Key => _dictionary._items[_index].Key;

        /// <inheritdoc />
        public object? Value => _dictionary._items[_index].Value;
    }

    /// <summary>
    /// Key enumerator for <see cref="LinearDictionary{TKey,TValue}"/>.
    /// </summary>
    public struct KeyEnumerator : IEnumerator<TKey>
    {
        private readonly int _version;
        private readonly LinearDictionary<TKey, TValue> _dictionary;
        private int _index;

        /// <inheritdoc />
        public TKey Current => _dictionary._items[_index].Key;

        public KeyEnumerator(LinearDictionary<TKey, TValue> dictionary)
        {
            _dictionary = dictionary;
            _version = _dictionary._version;
            _index = -1;
        }

        /// <inheritdoc />
        public bool MoveNext()
        {
            if (_version != _dictionary._version)
            {
                throw new InvalidOperationException("Collection modified.");
            }

            ++_index;
            return _index < _dictionary._size;
        }

        /// <inheritdoc />
        public void Reset()
        {
            _index = -1;
        }

        /// <inheritdoc />
        object? IEnumerator.Current => _dictionary._items[_index].Key;

        /// <inheritdoc />
        public void Dispose() { }
    }

    /// <summary>
    /// Value enumerator for <see cref="LinearDictionary{TKey,TValue}"/>.
    /// </summary>
    public struct ValueEnumerator : IEnumerator<TValue>
    {
        private readonly int _version;
        private readonly LinearDictionary<TKey, TValue> _dictionary;
        private int _index;

        /// <inheritdoc />
        public TValue Current => _dictionary._items[_index].Value;

        public ValueEnumerator(LinearDictionary<TKey, TValue> dictionary)
        {
            _dictionary = dictionary;
            _version = _dictionary._version;
            _index = -1;
        }

        /// <inheritdoc />
        public bool MoveNext()
        {
            if (_version != _dictionary._version)
            {
                throw new InvalidOperationException("Collection modified.");
            }

            ++_index;
            return _index < _dictionary._size;
        }

        /// <inheritdoc />
        public void Reset()
        {
            _index = -1;
        }

        /// <inheritdoc />
        object? IEnumerator.Current => _dictionary._items[_index].Value;

        /// <inheritdoc />
        public void Dispose() { }
    }

    /// <summary>
    /// Key collection for <see cref="LinearDictionary{TKey,TValue}"/>.
    /// </summary>
    public readonly struct KeyCollection : ICollection<TKey>, IReadOnlyCollection<TKey>, ICollection
    {
        private readonly LinearDictionary<TKey, TValue> _dictionary;

        public KeyCollection(LinearDictionary<TKey, TValue> dictionary)
        {
            _dictionary = dictionary;
        }

        /// <inheritdoc cref="IEnumerable{T}.GetEnumerator" />
        public KeyEnumerator GetEnumerator()
        {
            return new KeyEnumerator(_dictionary);
        }

        /// <exception cref="NotSupportedException"></exception>
        public void Add(TKey item)
        {
            _dictionary.AssertNotReadOnly();
            throw new NotSupportedException();
        }

        /// <summary>
        /// Remove all items from the corresponding dictionary.
        /// </summary>
        public void Clear()
        {
            _dictionary.Clear();
        }

        /// <inheritdoc />
        public bool Contains(TKey item)
        {
            return _dictionary.ContainsKey(item);
        }

        /// <inheritdoc />
        public void CopyTo(TKey[] array, int arrayIndex)
        {
            if (array == null)
                throw new ArgumentNullException(nameof(array));

            int length = array.Length - arrayIndex;

            if (length < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(arrayIndex));
            }

            length = Math.Min(length, _dictionary._size);

            for (int i = 0; i < length; ++i)
            {
                array[i + arrayIndex] = _dictionary._items[i].Key;
            }
        }

        /// <inheritdoc />
        public bool Remove(TKey item)
        {
            return _dictionary.Remove(item);
        }

        /// <inheritdoc />
        public void CopyTo(Array array, int index)
        {
            if (array == null)
                throw new ArgumentNullException(nameof(array));

            int length = array.Length - index;

            if (length < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(index));
            }

            length = Math.Min(length, _dictionary._size);

            for (int i = 0; i < length; ++i)
            {
                array.SetValue(_dictionary._items[i].Key, i + index);
            }
        }

        /// <inheritdoc cref="ICollection{T}.Count" />
        public int Count => _dictionary._size;

        /// <inheritdoc />
        public bool IsReadOnly => _dictionary._readOnly;

        /// <inheritdoc />
        IEnumerator<TKey> IEnumerable<TKey>.GetEnumerator()
        {
            return new KeyEnumerator(_dictionary);
        }

        /// <inheritdoc />
        IEnumerator IEnumerable.GetEnumerator()
        {
            return new KeyEnumerator(_dictionary);
        }

        /// <inheritdoc />
        bool ICollection.IsSynchronized => false;

        /// <inheritdoc />
        object ICollection.SyncRoot => null!;
    }

    /// <summary>
    /// Value collection for <see cref="LinearDictionary{TKey,TValue}"/>.
    /// </summary>
    public readonly struct ValueCollection : ICollection<TValue>, IReadOnlyCollection<TValue>, ICollection
    {
        private readonly LinearDictionary<TKey, TValue> _dictionary;

        public ValueCollection(LinearDictionary<TKey, TValue> dictionary)
        {
            _dictionary = dictionary;
        }

        /// <inheritdoc cref="IEnumerable{T}.GetEnumerator" />
        public ValueEnumerator GetEnumerator()
        {
            return new ValueEnumerator(_dictionary);
        }

        /// <exception cref="NotSupportedException"></exception>
        public void Add(TValue item)
        {
            _dictionary.AssertNotReadOnly();
            throw new NotSupportedException();
        }

        /// <summary>
        /// Remove all items from the corresponding dictionary.
        /// </summary>
        public void Clear()
        {
            _dictionary.Clear();
        }

        /// <inheritdoc />
        public bool Contains(TValue item)
        {
            int length = _dictionary._size;
            for (int i = 0; i < length; ++i)
            {
                if (EqualityComparer<TValue>.Default.Equals(_dictionary._items[i].Value, item))
                {
                    return true;
                }
            }

            return false;
        }

        /// <inheritdoc />
        public void CopyTo(TValue[] array, int arrayIndex)
        {
            if (array == null)
                throw new ArgumentNullException(nameof(array));

            int length = array.Length - arrayIndex;

            if (length < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(arrayIndex));
            }

            length = Math.Min(length, _dictionary._size);

            for (int i = 0; i < length; ++i)
            {
                array[i + arrayIndex] = _dictionary._items[i].Value;
            }
        }

        /// <inheritdoc />
        public bool Remove(TValue item)
        {
            _dictionary.AssertNotReadOnly();
            int length = _dictionary._size;
            for (int i = 0; i < length; ++i)
            {
                if (EqualityComparer<TValue>.Default.Equals(_dictionary._items[i].Value, item))
                {
                    _dictionary.RemoveAt(i);
                    return true;
                }
            }

            return false;
        }

        /// <inheritdoc />
        public void CopyTo(Array array, int index)
        {
            if (array == null)
                throw new ArgumentNullException(nameof(array));

            int length = array.Length - index;

            if (length < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(index));
            }

            length = Math.Min(length, _dictionary._size);

            for (int i = 0; i < length; ++i)
            {
                array.SetValue(_dictionary._items[i].Value, i + index);
            }
        }

        /// <inheritdoc cref="ICollection{T}.Count" />
        public int Count => _dictionary._size;

        /// <inheritdoc />
        public bool IsReadOnly => _dictionary._readOnly;

        /// <inheritdoc />
        IEnumerator<TValue> IEnumerable<TValue>.GetEnumerator()
        {
            return new ValueEnumerator(_dictionary);
        }

        /// <inheritdoc />
        IEnumerator IEnumerable.GetEnumerator()
        {
            return new ValueEnumerator(_dictionary);
        }

        /// <inheritdoc />
        bool ICollection.IsSynchronized => false;

        /// <inheritdoc />
        object ICollection.SyncRoot => null!;
    }
}