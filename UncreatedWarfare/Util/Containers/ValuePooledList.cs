using System.Buffers;

namespace Uncreated.Warfare.Util.Containers;

internal struct ValuePooledList<T> : IDisposable
{
    private T[] _array;
    private int _count;

    public int Count => _count;

    public T[] GetUnderlyingArray() => _array;

    public ValuePooledList(int startingCapacity)
    {
        if (startingCapacity < 0)
            throw new ArgumentOutOfRangeException(nameof(startingCapacity));

        _array = startingCapacity == 0 ? Array.Empty<T>() : ArrayPool<T>.Shared.Rent(startingCapacity);
    }

    public ref T this[int index]
    {
        get
        {
            if (index < 0 || index >= _count)
                throw new ArgumentOutOfRangeException(nameof(index), "Index out of bounds of the array.");

            return ref _array[index];
        }
    }

    public ref T Allocate()
    {
        if (_count == _array.Length)
        {
            AllocateSpace(_count + 1);
        }

        int ind = _count;
        ++_count;
        return ref _array[ind];
    }

    public ref T AllocateAt(int index)
    {
        if (index >= _count)
            return ref Allocate();

        index = Math.Max(index, 0);
        if (_count == _array.Length)
        {
            AllocateSpace(_count + 1);
        }

        Array.Copy(_array, index, _array, index + 1, _count - index);
        ++_count;
        return ref _array[index];
    }

    public void RemoveAt(int index)
    {
        CollectionUtility.RemoveFromArray(ref _array, index);
    }

    public T[] ToArray()
    {
        if (_count == 0)
            return Array.Empty<T>();

        T[] newArray = new T[_count];
        Array.Copy(_array, newArray, _count);
        return newArray;
    }

    private void AllocateSpace(int newSize)
    {
        T[] newArray = ArrayPool<T>.Shared.Rent(Math.Max(newSize, (int)Math.Ceiling(_count * 1.5)));
        if (_array.Length > 0)
        {
            T? def = default;
            for (int i = 0; i < _count; ++i)
            {
                newArray[i] = _array[i];
                _array[i] = def!;
            }

            ArrayPool<T>.Shared.Return(_array, clearArray: false);
        }

        _array = newArray;
    }

    public void Dispose()
    {
        if (_array.Length == 0)
            return;

        T? def = default;
        for (int i = 0; i < _count; ++i)
        {
            _array[i] = def!;
        }

        ArrayPool<T>.Shared.Return(_array, clearArray: false);
    }
}