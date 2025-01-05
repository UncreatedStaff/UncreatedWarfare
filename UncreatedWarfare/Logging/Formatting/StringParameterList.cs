using System;

namespace Uncreated.Warfare.Logging.Formatting;

/// <summary>
/// Efficiently keeps up with a list of parameters.
/// </summary>
internal readonly struct StringParameterList
{
    public readonly object? Parameter1;
    public readonly object? Parameter2;
    public readonly object? Parameter3;
    public readonly object? Parameter4;
    private readonly bool _isArray;

    public readonly int Count;

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
