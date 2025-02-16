using System;

namespace Uncreated.Warfare.Logging.Formatting;

/// <summary>
/// Efficiently keeps up with a list of parameters.
/// </summary>
internal struct StringParameterList
{
    public object? Parameter1;
    public object? Parameter2;
    public object? Parameter3;
    public object? Parameter4;
    private readonly bool _isArray;

    public int Count;

    public StringParameterList(object?[] args)
    {
        _isArray = true;
        Parameter1 = args;
        Count = args.Length;
    }

    public StringParameterList(object?[] args, int ct)
    {
        _isArray = true;
        Parameter1 = args;
        Count = ct;
    }

    public StringParameterList(object? arg1)
    {
        if (arg1 is object?[] args)
        {
            _isArray = true;
            Count = args.Length;
        }
        else
        {
            Count = 1;
        }

        Parameter1 = arg1;
    }

    public StringParameterList(object? arg1, object? arg2)
    {
        Parameter1 = arg1;
        Parameter2 = arg2;
        Count = 2;
    }
    
    public StringParameterList(object? arg1, object? arg2, object? arg3)
    {
        Parameter1 = arg1;
        Parameter2 = arg2;
        Parameter3 = arg3;
        Count = 3;
    }
    
    public StringParameterList(object? arg1, object? arg2, object? arg3, object? arg4)
    {
        Parameter1 = arg1;
        Parameter2 = arg2;
        Parameter3 = arg3;
        Parameter4 = arg4;
        Count = 4;
    }

    public static StringParameterList CreateForAdding(int capacity)
    {
        return capacity < 5
            ? default
            : new StringParameterList(new object?[capacity], 0);
    }

    public void Add(object? value)
    {
        if (_isArray)
        {
            object?[] arr = (object?[])Parameter1!;
            arr[Count] = value;
        }
        else
        {
            switch (Count)
            {
                case 0:
                    Parameter1 = value;
                    break;
                case 1:
                    Parameter2 = value;
                    break;
                case 2:
                    Parameter3 = value;
                    break;
                case 3:
                    Parameter4 = value;
                    break;
            }
        }

        ++Count;
    }

    public object?[] ToArray()
    {
        if (_isArray) return (object?[])Parameter1!;
        return Count switch
        {
            0 => Array.Empty<object>(),
            1 => [ Parameter1 ],
            2 => [ Parameter1, Parameter2 ],
            3 => [ Parameter1, Parameter2, Parameter3 ],
            _ => [ Parameter1, Parameter2, Parameter3, Parameter4 ]
        };
    }

    public object this[int index]
    {
        get
        {
            if (index < 0)
                return OutOfRange.Value;

            if (_isArray)
            {
                object?[] arr = (object?[]?)Parameter1!;
                if (index >= Count)
                    return OutOfRange.Value;

                return arr[index] ?? DBNull.Value;
            }

            return index switch
            {
                0 => Parameter1,
                1 => Parameter2,
                2 => Parameter3,
                3 => Parameter4,
                _ => OutOfRange.Value
            } ?? DBNull.Value;
        }
    }
}
