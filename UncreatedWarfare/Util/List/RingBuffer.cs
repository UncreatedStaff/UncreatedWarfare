using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;

namespace Uncreated.Warfare.Util.List;

/// <summary>
/// Fixed size first-in-last-out buffer.
/// </summary>
public class RingBuffer<T> : IList<T>, IReadOnlyList<T>
{
    private int _capacity;
    private T?[] _items;
    private int _version;
    private readonly IEqualityComparer<T> _comparer;
    private int _size;
    private int _index;

    /// <summary>
    /// Number of elements that can fit in the buffer.
    /// </summary>
    public int Capacity
    {
        [DebuggerStepThrough]
        get => _capacity;
        set
        {
            if (value == _capacity)
            {
                ++_version;
                return;
            }

            T?[] newItems = new T?[value];
            int newSize = Math.Min(_size, value);
            if (_size < _capacity)
            {
                if (value > _capacity)
                    Array.Copy(_items, newItems, newSize);
                else
                    Array.Copy(_items, Math.Max(_size - value, 0), newItems, 0, value);
            }
            else
            {
                if (value > _capacity)
                {
                    int ind = _capacity - _index;
                    Array.Copy(_items, _index, newItems, 0, ind);
                    if (_index != 0)
                        Array.Copy(_items, 0, newItems, ind, _index);
                }
                else
                {
                    int cutInd = (_index + (_capacity - value)) % _capacity;
                    int copied = cutInd >= _index ? _capacity - cutInd : value;
                    Array.Copy(_items, cutInd, newItems, 0, copied);
                    if (copied < value)
                        Array.Copy(_items, 0, newItems, copied, value - copied);
                }
            }

            _items = newItems;
            _index = newSize % value;
            _size = newSize;
            _capacity = value;
            ++_version;
        }
    }

    public int Count
    {
        [DebuggerStepThrough]
        get => _size;
    }

    bool ICollection<T>.IsReadOnly
    {
        [DebuggerStepThrough]
        get => false;
    }

    [EditorBrowsable(EditorBrowsableState.Advanced)]
    public int Version
    {
        [DebuggerStepThrough]
        get => _version;
    }

    [EditorBrowsable(EditorBrowsableState.Advanced)]
    public int UnderlyingIndex
    {
        [DebuggerStepThrough]
        get => _index;
    }

    [DebuggerStepThrough]
    public RingBuffer(int capacity) : this(capacity, EqualityComparer<T>.Default) { }

    [DebuggerStepThrough]
    public RingBuffer(int capacity, IEqualityComparer<T> equalityComparer)
    {
        _comparer = equalityComparer ?? throw new ArgumentNullException(nameof(equalityComparer));

        if (capacity <= 0)
            throw new ArgumentOutOfRangeException(nameof(capacity));

        _items = new T[capacity];
        _capacity = capacity;
    }

    [EditorBrowsable(EditorBrowsableState.Advanced)]
    public T?[] UnsafeGetUnderlyingArray()
    {
        return _items;
    }

    [DebuggerStepThrough]
    private int GetIndex(int index)
    {
        int startIndex = _size < _capacity ? 0 : _index;
        return (index + startIndex) % _capacity;
    }

    /// <summary>
    /// Add a new value to the buffer.
    /// </summary>
    public void Add(T value)
    {
        _items[_index] = value;
        _index = (_index + 1) % _capacity;
        if (_size < _capacity)
        {
            ++_size;
        }
        ++_version;
    }

    /// <summary>
    /// Remove an item at the specified <paramref name="index"/>.
    /// </summary>
    /// <exception cref="ArgumentOutOfRangeException"/>
    public void RemoveAt(int index)
    {
        if (index < 0 || index >= _size)
            throw new ArgumentOutOfRangeException(nameof(index));

        RemoveAtIntl(GetIndex(index));
    }

    private void RemoveAtIntl(int indexIntl)
    {
        if (_size < _capacity)
        {
            for (int i = indexIntl + 1; i < _size; ++i)
            {
                _items[i - 1] = _items[i];
            }

            --_index;
            --_size;
            _items[_size] = default;
        }
        else if (_index != 0)
        {
            if (_index <= indexIntl)
            {
                for (int i = indexIntl; i > _index; --i)
                {
                    _items[i] = _items[i - 1];
                }
            }
            else
            {
                for (int i = indexIntl; i >= 0; --i)
                {
                    _items[i] = _items[(i - 1 + _capacity) % _capacity];
                }
                for (int i = _capacity - 1; i > _index; --i)
                {
                    _items[i] = _items[i - 1];
                }
            }

            if (_index != _size - 1)
            {
                int rightSide = _capacity - _index - 1;
                if (_index <= rightSide)
                {
                    T?[] items = new T?[_index];
                    Array.Copy(_items, 0, items, 0, _index);
                    Array.Copy(_items, _index + 1, _items, 0, rightSide);
                    Array.Copy(items, 0, _items, rightSide, _index);
                }
                else
                {
                    T?[] items = new T?[rightSide];
                    Array.Copy(_items, _index + 1, items, 0, rightSide);
                    Array.Copy(_items, 0, _items, rightSide, _index);
                    Array.Copy(items, 0, _items, 0, rightSide);
                }
                _items[^1] = default;
            }

            --_size;
            _index = _size;
        }
        else
        {
            for (int i = indexIntl + 1; i < _capacity; ++i)
            {
                _items[i - 1] = _items[i];
            }
        }

        ++_version;
    }

    /// <summary>
    /// Insert an item at the specified <paramref name="index"/>, dropping the oldest element if necessary.
    /// </summary>
    /// <exception cref="ArgumentOutOfRangeException"/>
    public void Insert(int index, T item)
    {
        if (index == _size || index == _size - 1)
        {
            Add(item);
            return;
        }

        if (index > _size || index < 0)
            throw new ArgumentOutOfRangeException(nameof(index));

        if (_size < _capacity)
        {
            int indexIntl = GetIndex(index);
            Array.Copy(_items, indexIntl, _items, indexIntl + 1, _size - indexIntl);
            _items[indexIntl] = item;

            _index = (_index + 1) % _capacity;
            ++_size;
        }
        else if (index == 0)
        {
            _items[_index] = item;
        }
        else
        {
            int indexIntl = GetIndex(index);
            if (indexIntl > _index)
            {
                Array.Copy(_items, _index + 1, _items, _index, indexIntl - _index);
            }
            else
            {
                Array.Copy(_items, _index + 1, _items, _index, _capacity - _index - 1);
                _items[_capacity - 1] = _items[0];
                Array.Copy(_items, 1, _items, 0, indexIntl);
            }

            _items[indexIntl] = item;
        }

        ++_version;
    }

    /// <inheritdoc />
    public bool Remove(T value)
    {
        int indexIntl = IndexOfIntl(value);
        if (indexIntl == -1)
            return false;

        RemoveAtIntl(indexIntl);
        return true;
    }

    /// <inheritdoc />
    public int IndexOf(T value)
    {
        int indexIntl = IndexOfIntl(value);
        if (indexIntl == -1)
            return -1;

        int startIndex = _size < _capacity ? 0 : _index;
        return (indexIntl - startIndex + _capacity) % _capacity;
    }

    public int IndexOfIntl(T value)
    {
        if (_size < _capacity)
        {
            for (int i = 0; i < _size; ++i)
            {
                if (_comparer.Equals(value, _items[i]!))
                    return i;
            }
        }
        else
        {
            for (int i = _index; i < _capacity; ++i)
            {
                if (_comparer.Equals(value, _items[i]!))
                    return i;
            }
            for (int i = 0; i < _index; ++i)
            {
                if (_comparer.Equals(value, _items[i]!))
                    return i;
            }
        }

        return -1;
    }

    /// <inheritdoc />
    public void Clear()
    {
        if (_size > 0)
        {
            _size = 0;
            _index = 0;
            Array.Clear(_items, 0, _capacity);
        }
        ++_version;
    }

    /// <inheritdoc />
    public bool Contains(T value)
    {
        return IndexOfIntl(value) != -1;
    }

    /// <summary>
    /// Copy as many elements as possible from this buffer to an <paramref name="array"/>.
    /// </summary>
    public void CopyTo(T[] array)
    {
        CopyTo(0, array, 0, Math.Min(_size, array.Length));
    }

    /// <summary>
    /// Copy as many elements as possible from this buffer to an <paramref name="array"/>, starting at a given <paramref name="arrayIndex"/>.
    /// </summary>
    public void CopyTo(T[] array, int arrayIndex)
    {
        CopyTo(0, array, 0, Math.Min(_size, array.Length - arrayIndex));
    }

    /// <summary>
    /// Copy as many elements as possible from this buffer to a <paramref name="span"/>.
    /// </summary>
    public void CopyTo(Span<T> span)
    {
        CopyTo(0, span);
    }

    /// <summary>
    /// Copy as many elements as possible from this buffer to a <paramref name="span"/>, skipping the first <paramref name="index"/> elements.
    /// </summary>
    public void CopyTo(int index, Span<T> span)
    {
        if (index >= _size)
        {
            if (index == _size)
                return;

            throw new ArgumentOutOfRangeException(nameof(index));
        }

        if (span.Length == 0)
            return;

        int ct = Math.Min(_size, span.Length);
        if (_size < _capacity)
        {
            _items.AsSpan(index, ct).CopyTo(span!);
        }
        else
        {
            int prevIndex = 0;
            if (_index + index < _capacity)
            {
                int ctToCopy = Math.Min(ct, _capacity - (_index + index));
                _items.AsSpan(_index + index, ctToCopy).CopyTo(span!);
                prevIndex = ctToCopy;
                ct -= ctToCopy;
                if (ct == 0)
                    return;
            }

            index -= _capacity - _index;

            if (_index != 0)
            {
                int stInd = Math.Max(index, 0);
                _items.AsSpan(stInd, Math.Min(ct, _index - stInd)).CopyTo(span[prevIndex..]!);
            }
        }
    }

    /// <summary>
    /// Copy as many elements as possible up to <paramref name="count"/> from this buffer to a <paramref name="array"/>, starting at a given <paramref name="arrayIndex"/>, skipping the first <paramref name="index"/> elements.
    /// </summary>
    public void CopyTo(int index, T[] array, int arrayIndex, int count)
    {
        if (arrayIndex >= array.Length && count > 0)
            throw new ArgumentOutOfRangeException(nameof(arrayIndex));

        if (index >= _size)
        {
            if (index == _size)
                return;

            throw new ArgumentOutOfRangeException(nameof(index));
        }

        if (count == 0)
            return;

        if (arrayIndex + count > array.Length || count < 0)
            throw new ArgumentOutOfRangeException(nameof(count));


        int ct = Math.Min(_size, count);
        if (_size < _capacity)
        {
            Array.Copy(_items, index, array, arrayIndex, ct);
        }
        else
        {
            int prevIndex = 0;
            if (_index + index < _capacity)
            {
                int ctToCopy = Math.Min(ct, _capacity - (_index + index));
                Array.Copy(_items, _index + index, array, arrayIndex, ctToCopy);
                prevIndex = ctToCopy;
                ct -= ctToCopy;
                if (ct == 0)
                    return;
            }

            index -= _capacity - _index;

            if (_index != 0)
            {
                int stInd = Math.Max(index, 0);
                Array.Copy(_items, stInd, array, arrayIndex + prevIndex, Math.Min(ct, _index - stInd));
            }
        }
    }

    /// <summary>
    /// Create a new array with the data on this buffer.
    /// </summary>
    public T[] ToArray()
    {
        T[] arr = new T[_size];

        if (_size < _capacity)
        {
            Array.Copy(_items, arr, _size);
        }
        else
        {
            int ind = _capacity - _index;
            Array.Copy(_items, _index, arr, 0, ind);
            if (_index != 0)
                Array.Copy(_items, 0, arr, ind, _index);
        }

        return arr;
    }

    /// <inheritdoc />
    public T this[int index]
    {
        [DebuggerStepThrough]
        get
        {
            if (index < 0 || index >= _size)
                throw new ArgumentOutOfRangeException(nameof(index));

            return _items[GetIndex(index)]!;
        }

        [DebuggerStepThrough]
        set
        {
            if (index < 0 || index >= _size)
                throw new ArgumentOutOfRangeException(nameof(index));
            _items[GetIndex(index)] = value;
            ++_version;
        }
    }

    /// <summary>
    /// Returns an enumerator that iterates through the collection.
    /// </summary>
    /// <returns>An enumerator that can be used to iterate through the collection.</returns>
    [DebuggerStepThrough]
    public Enumerator GetEnumerator() => new Enumerator(this);

    /// <inheritdoc />
    [DebuggerStepThrough]
    IEnumerator<T> IEnumerable<T>.GetEnumerator() => new Enumerator(this);

    /// <inheritdoc />
    [DebuggerStepThrough]
    IEnumerator IEnumerable.GetEnumerator() => new Enumerator(this);

    /// <summary>
    /// Enumerator implementation for <see cref="RingBuffer{T}"/>.
    /// </summary>
    public struct Enumerator : IEnumerator<T>
    {
        private readonly int _version;
        private readonly RingBuffer<T> _buffer;
        private int _state;
        private int _index;

#nullable disable
        public T Current
        {
            [DebuggerStepThrough]
            get;

            [DebuggerStepThrough]
            private set;
        }
#nullable restore

        internal Enumerator(RingBuffer<T> buffer)
        {
            _buffer = buffer;
            _index = -1;
            _state = -1;
            _version = buffer._version;
        }

        /// <inheritdoc />
        [DebuggerStepThrough]
        public bool MoveNext()
        {
            RingBuffer<T> buffer = _buffer;

            if (buffer._version != _version)
                throw new InvalidOperationException("Collection modified while enumerating.");

            ++_index;
            switch (_state)
            {
                case -1: // uninitialized
                    if (buffer._size < buffer._capacity)
                    {
                        _state = 0;
                        _index = 0;
                        goto case 0;
                    }

                    _index = buffer._index;
                    _state = 1;
                    goto case 1;

                case 0: // not full
                    if (_index >= buffer._size)
                        return false;
                    break;

                case 1: // full first part
                    if (_index >= buffer._capacity)
                    {
                        _index = 0;
                        _state = 2;
                        goto case 2;
                    }
                    break;

                case 2: // full second part
                    if (_index >= buffer._index)
                        return false;
                    break;
            }

            Current = buffer._items[_index]!;
            return true;
        }

        /// <inheritdoc />
        [DebuggerStepThrough]
        public void Reset()
        {
            if (_buffer._version != _version)
                throw new InvalidOperationException("Collection modified while enumerating.");

            _state = -1;
            _index = -1;
            Current = default;
        }

        /// <inheritdoc />
        object? IEnumerator.Current
        {
            [DebuggerStepThrough]
            get => Current;
        }

        /// <inheritdoc />
        [DebuggerStepThrough]
        public void Dispose() { }
    }
}