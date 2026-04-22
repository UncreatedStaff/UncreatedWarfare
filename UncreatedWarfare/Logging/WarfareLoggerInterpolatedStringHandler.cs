using System;
using System.Buffers;
using System.Globalization;
using System.Runtime.CompilerServices;
using Uncreated.Warfare.Logging.Formatting;

namespace Uncreated.Warfare.Logging;


[InterpolatedStringHandler]
public struct WarfareTraceLoggerInterpolatedStringHandler
{
    private readonly char[] _buffer;
    private StringParameterList _parameterList;
    private int _bufferIndex;

    public WarfareTraceLoggerInterpolatedStringHandler(int literalLength, int formattedCount, ILogger logger, out bool isEnabled)
    {
        isEnabled = logger.IsEnabled(LogLevel.Trace);
        _buffer = ArrayPool<char>.Shared.Rent(literalLength + WarfareLoggerInterpolatedStringHandlerHelper.CalculateFormatLength(formattedCount));
        _parameterList = StringParameterList.CreateForAdding(formattedCount);
    }

    public void AppendLiteral(string s)
    {
        WarfareLoggerInterpolatedStringHandlerHelper.AppendLiteral(_buffer, ref _bufferIndex, s);
    }

    public void AppendFormatted(object? value)
    {
        WarfareLoggerInterpolatedStringHandlerHelper.AppendArgument(_buffer, ref _bufferIndex, ref _parameterList, value);
    }

    public void AppendFormatted<TValue>(TValue? value, string? format) where TValue : IFormattable
    {
        WarfareLoggerInterpolatedStringHandlerHelper.AppendArgument(_buffer, ref _bufferIndex, ref _parameterList,
            new FormattedValue { Value = value?.ToString(format, CultureInfo.InvariantCulture), Type = typeof(TValue), Alignment = int.MinValue });
    }

    public void AppendFormatted<TValue>(TValue? value, string? format, int alignment) where TValue : IFormattable
    {
        WarfareLoggerInterpolatedStringHandlerHelper.AppendArgument(_buffer, ref _bufferIndex, ref _parameterList,
            new FormattedValue { Value = value?.ToString(format, CultureInfo.InvariantCulture), Type = typeof(TValue), Alignment = alignment });
    }

    public void AppendFormatted<TValue>(TValue? value, int alignment) where TValue : IFormattable
    {
        WarfareLoggerInterpolatedStringHandlerHelper.AppendArgument(_buffer, ref _bufferIndex, ref _parameterList,
            new FormattedValue { Value = value, Type = typeof(TValue), Alignment = alignment });
    }

    public void AppendFormatted(scoped ReadOnlySpan<char> value)
    {
        WarfareLoggerInterpolatedStringHandlerHelper.AppendArgument(_buffer, ref _bufferIndex, ref _parameterList, new string(value));
    }

    public void AppendFormatted(string? value)
    {
        WarfareLoggerInterpolatedStringHandlerHelper.AppendArgument(_buffer, ref _bufferIndex, ref _parameterList, value);
    }

    internal void GetResult(out StringParameterList parameterList, out string literal)
    {
        literal = _bufferIndex == 0 ? string.Empty : new string(_buffer, 0, _bufferIndex);
        parameterList = _parameterList;
        ArrayPool<char>.Shared.Return(_buffer);
    }
}

[InterpolatedStringHandler]
public struct WarfareDebugLoggerInterpolatedStringHandler
{
    private readonly char[] _buffer;
    private StringParameterList _parameterList;
    private int _bufferIndex;

    public WarfareDebugLoggerInterpolatedStringHandler(int literalLength, int formattedCount, ILogger logger, out bool isEnabled)
    {
        isEnabled = logger.IsEnabled(LogLevel.Debug);
        _buffer = ArrayPool<char>.Shared.Rent(literalLength + WarfareLoggerInterpolatedStringHandlerHelper.CalculateFormatLength(formattedCount));
        _parameterList = StringParameterList.CreateForAdding(formattedCount);
    }

    public void AppendLiteral(string s)
    {
        WarfareLoggerInterpolatedStringHandlerHelper.AppendLiteral(_buffer, ref _bufferIndex, s);
    }

    public void AppendFormatted(object? value)
    {
        WarfareLoggerInterpolatedStringHandlerHelper.AppendArgument(_buffer, ref _bufferIndex, ref _parameterList, value);
    }

    public void AppendFormatted<TValue>(TValue? value, string? format) where TValue : IFormattable
    {
        WarfareLoggerInterpolatedStringHandlerHelper.AppendArgument(_buffer, ref _bufferIndex, ref _parameterList,
            new FormattedValue { Value = value?.ToString(format, CultureInfo.InvariantCulture), Type = typeof(TValue), Alignment = int.MinValue });
    }

    public void AppendFormatted<TValue>(TValue? value, string? format, int alignment) where TValue : IFormattable
    {
        WarfareLoggerInterpolatedStringHandlerHelper.AppendArgument(_buffer, ref _bufferIndex, ref _parameterList,
            new FormattedValue { Value = value?.ToString(format, CultureInfo.InvariantCulture), Type = typeof(TValue), Alignment = alignment });
    }

    public void AppendFormatted<TValue>(TValue? value, int alignment) where TValue : IFormattable
    {
        WarfareLoggerInterpolatedStringHandlerHelper.AppendArgument(_buffer, ref _bufferIndex, ref _parameterList,
            new FormattedValue { Value = value, Type = typeof(TValue), Alignment = alignment });
    }

    public void AppendFormatted(scoped ReadOnlySpan<char> value)
    {
        WarfareLoggerInterpolatedStringHandlerHelper.AppendArgument(_buffer, ref _bufferIndex, ref _parameterList, new string(value));
    }

    public void AppendFormatted(string? value)
    {
        WarfareLoggerInterpolatedStringHandlerHelper.AppendArgument(_buffer, ref _bufferIndex, ref _parameterList, value);
    }

    internal void GetResult(out StringParameterList parameterList, out string literal)
    {
        literal = _bufferIndex == 0 ? string.Empty : new string(_buffer, 0, _bufferIndex);
        parameterList = _parameterList;
        ArrayPool<char>.Shared.Return(_buffer);
    }
}

[InterpolatedStringHandler]
public struct WarfareInformationLoggerInterpolatedStringHandler
{
    private readonly char[] _buffer;
    private StringParameterList _parameterList;
    private int _bufferIndex;

    public WarfareInformationLoggerInterpolatedStringHandler(int literalLength, int formattedCount, ILogger logger, out bool isEnabled)
    {
        isEnabled = logger.IsEnabled(LogLevel.Information);
        _buffer = ArrayPool<char>.Shared.Rent(literalLength + WarfareLoggerInterpolatedStringHandlerHelper.CalculateFormatLength(formattedCount));
        _parameterList = StringParameterList.CreateForAdding(formattedCount);
    }

    public void AppendLiteral(string s)
    {
        WarfareLoggerInterpolatedStringHandlerHelper.AppendLiteral(_buffer, ref _bufferIndex, s);
    }

    public void AppendFormatted<TValue>(TValue? value, string? format) where TValue : IFormattable
    {
        WarfareLoggerInterpolatedStringHandlerHelper.AppendArgument(_buffer, ref _bufferIndex, ref _parameterList,
            new FormattedValue { Value = value?.ToString(format, CultureInfo.InvariantCulture), Type = typeof(TValue), Alignment = int.MinValue });
    }

    public void AppendFormatted<TValue>(TValue? value, string? format, int alignment) where TValue : IFormattable
    {
        WarfareLoggerInterpolatedStringHandlerHelper.AppendArgument(_buffer, ref _bufferIndex, ref _parameterList,
            new FormattedValue { Value = value?.ToString(format, CultureInfo.InvariantCulture), Type = typeof(TValue), Alignment = alignment });
    }

    public void AppendFormatted<TValue>(TValue? value, int alignment) where TValue : IFormattable
    {
        WarfareLoggerInterpolatedStringHandlerHelper.AppendArgument(_buffer, ref _bufferIndex, ref _parameterList,
            new FormattedValue { Value = value, Type = typeof(TValue), Alignment = alignment });
    }

    public void AppendFormatted(object? value)
    {
        WarfareLoggerInterpolatedStringHandlerHelper.AppendArgument(_buffer, ref _bufferIndex, ref _parameterList, value);
    }

    public void AppendFormatted(scoped ReadOnlySpan<char> value)
    {
        WarfareLoggerInterpolatedStringHandlerHelper.AppendArgument(_buffer, ref _bufferIndex, ref _parameterList, new string(value));
    }

    public void AppendFormatted(string? value)
    {
        WarfareLoggerInterpolatedStringHandlerHelper.AppendArgument(_buffer, ref _bufferIndex, ref _parameterList, value);
    }

    internal void GetResult(out StringParameterList parameterList, out string literal)
    {
        literal = _bufferIndex == 0 ? string.Empty : new string(_buffer, 0, _bufferIndex);
        parameterList = _parameterList;
        ArrayPool<char>.Shared.Return(_buffer);
    }
}

[InterpolatedStringHandler]
public struct WarfareWarningLoggerInterpolatedStringHandler
{
    private readonly char[] _buffer;
    private StringParameterList _parameterList;
    private int _bufferIndex;

    public WarfareWarningLoggerInterpolatedStringHandler(int literalLength, int formattedCount, ILogger logger, out bool isEnabled)
    {
        isEnabled = logger.IsEnabled(LogLevel.Warning);
        _buffer = ArrayPool<char>.Shared.Rent(literalLength + WarfareLoggerInterpolatedStringHandlerHelper.CalculateFormatLength(formattedCount));
        _parameterList = StringParameterList.CreateForAdding(formattedCount);
    }

    public void AppendLiteral(string s)
    {
        WarfareLoggerInterpolatedStringHandlerHelper.AppendLiteral(_buffer, ref _bufferIndex, s);
    }

    public void AppendFormatted<TValue>(TValue? value, string? format) where TValue : IFormattable
    {
        WarfareLoggerInterpolatedStringHandlerHelper.AppendArgument(_buffer, ref _bufferIndex, ref _parameterList,
            new FormattedValue { Value = value?.ToString(format, CultureInfo.InvariantCulture), Type = typeof(TValue), Alignment = int.MinValue });
    }

    public void AppendFormatted<TValue>(TValue? value, string? format, int alignment) where TValue : IFormattable
    {
        WarfareLoggerInterpolatedStringHandlerHelper.AppendArgument(_buffer, ref _bufferIndex, ref _parameterList,
            new FormattedValue { Value = value?.ToString(format, CultureInfo.InvariantCulture), Type = typeof(TValue), Alignment = alignment });
    }

    public void AppendFormatted<TValue>(TValue? value, int alignment) where TValue : IFormattable
    {
        WarfareLoggerInterpolatedStringHandlerHelper.AppendArgument(_buffer, ref _bufferIndex, ref _parameterList,
            new FormattedValue { Value = value, Type = typeof(TValue), Alignment = alignment });
    }

    public void AppendFormatted(object? value)
    {
        WarfareLoggerInterpolatedStringHandlerHelper.AppendArgument(_buffer, ref _bufferIndex, ref _parameterList, value);
    }

    public void AppendFormatted(scoped ReadOnlySpan<char> value)
    {
        WarfareLoggerInterpolatedStringHandlerHelper.AppendArgument(_buffer, ref _bufferIndex, ref _parameterList, new string(value));
    }

    public void AppendFormatted(string? value)
    {
        WarfareLoggerInterpolatedStringHandlerHelper.AppendArgument(_buffer, ref _bufferIndex, ref _parameterList, value);
    }

    internal void GetResult(out StringParameterList parameterList, out string literal)
    {
        literal = _bufferIndex == 0 ? string.Empty : new string(_buffer, 0, _bufferIndex);
        parameterList = _parameterList;
        ArrayPool<char>.Shared.Return(_buffer);
    }
}

[InterpolatedStringHandler]
public struct WarfareErrorLoggerInterpolatedStringHandler
{
    private readonly char[] _buffer;
    private StringParameterList _parameterList;
    private int _bufferIndex;

    public WarfareErrorLoggerInterpolatedStringHandler(int literalLength, int formattedCount, ILogger logger, out bool isEnabled)
    {
        isEnabled = logger.IsEnabled(LogLevel.Error);
        _buffer = ArrayPool<char>.Shared.Rent(literalLength + WarfareLoggerInterpolatedStringHandlerHelper.CalculateFormatLength(formattedCount));
        _parameterList = StringParameterList.CreateForAdding(formattedCount);
    }

    public void AppendLiteral(string s)
    {
        WarfareLoggerInterpolatedStringHandlerHelper.AppendLiteral(_buffer, ref _bufferIndex, s);
    }

    public void AppendFormatted<TValue>(TValue? value, string? format) where TValue : IFormattable
    {
        WarfareLoggerInterpolatedStringHandlerHelper.AppendArgument(_buffer, ref _bufferIndex, ref _parameterList,
            new FormattedValue { Value = value?.ToString(format, CultureInfo.InvariantCulture), Type = typeof(TValue), Alignment = int.MinValue });
    }

    public void AppendFormatted<TValue>(TValue? value, string? format, int alignment) where TValue : IFormattable
    {
        WarfareLoggerInterpolatedStringHandlerHelper.AppendArgument(_buffer, ref _bufferIndex, ref _parameterList,
            new FormattedValue { Value = value?.ToString(format, CultureInfo.InvariantCulture), Type = typeof(TValue), Alignment = alignment });
    }

    public void AppendFormatted<TValue>(TValue? value, int alignment) where TValue : IFormattable
    {
        WarfareLoggerInterpolatedStringHandlerHelper.AppendArgument(_buffer, ref _bufferIndex, ref _parameterList,
            new FormattedValue { Value = value, Type = typeof(TValue), Alignment = alignment });
    }

    public void AppendFormatted(object? value)
    {
        WarfareLoggerInterpolatedStringHandlerHelper.AppendArgument(_buffer, ref _bufferIndex, ref _parameterList, value);
    }

    public void AppendFormatted(scoped ReadOnlySpan<char> value)
    {
        WarfareLoggerInterpolatedStringHandlerHelper.AppendArgument(_buffer, ref _bufferIndex, ref _parameterList, new string(value));
    }

    public void AppendFormatted(string? value)
    {
        WarfareLoggerInterpolatedStringHandlerHelper.AppendArgument(_buffer, ref _bufferIndex, ref _parameterList, value);
    }

    internal void GetResult(out StringParameterList parameterList, out string literal)
    {
        literal = _bufferIndex == 0 ? string.Empty : new string(_buffer, 0, _bufferIndex);
        parameterList = _parameterList;
        ArrayPool<char>.Shared.Return(_buffer);
    }
}

[InterpolatedStringHandler]
public struct WarfareCriticalLoggerInterpolatedStringHandler
{
    private readonly char[] _buffer;
    private StringParameterList _parameterList;
    private int _bufferIndex;

    public WarfareCriticalLoggerInterpolatedStringHandler(int literalLength, int formattedCount, ILogger logger, out bool isEnabled)
    {
        isEnabled = logger.IsEnabled(LogLevel.Critical);
        _buffer = ArrayPool<char>.Shared.Rent(literalLength + WarfareLoggerInterpolatedStringHandlerHelper.CalculateFormatLength(formattedCount));
        _parameterList = StringParameterList.CreateForAdding(formattedCount);
    }

    public void AppendLiteral(string s)
    {
        WarfareLoggerInterpolatedStringHandlerHelper.AppendLiteral(_buffer, ref _bufferIndex, s);
    }

    public void AppendFormatted<TValue>(TValue? value, string? format) where TValue : IFormattable
    {
        WarfareLoggerInterpolatedStringHandlerHelper.AppendArgument(_buffer, ref _bufferIndex, ref _parameterList,
            new FormattedValue { Value = value?.ToString(format, CultureInfo.InvariantCulture), Type = typeof(TValue), Alignment = int.MinValue });
    }

    public void AppendFormatted<TValue>(TValue? value, string? format, int alignment) where TValue : IFormattable
    {
        WarfareLoggerInterpolatedStringHandlerHelper.AppendArgument(_buffer, ref _bufferIndex, ref _parameterList,
            new FormattedValue { Value = value?.ToString(format, CultureInfo.InvariantCulture), Type = typeof(TValue), Alignment = alignment });
    }

    public void AppendFormatted<TValue>(TValue? value, int alignment) where TValue : IFormattable
    {
        WarfareLoggerInterpolatedStringHandlerHelper.AppendArgument(_buffer, ref _bufferIndex, ref _parameterList,
            new FormattedValue { Value = value, Type = typeof(TValue), Alignment = alignment });
    }

    public void AppendFormatted(object? value)
    {
        WarfareLoggerInterpolatedStringHandlerHelper.AppendArgument(_buffer, ref _bufferIndex, ref _parameterList, value);
    }

    public void AppendFormatted(scoped ReadOnlySpan<char> value)
    {
        WarfareLoggerInterpolatedStringHandlerHelper.AppendArgument(_buffer, ref _bufferIndex, ref _parameterList, new string(value));
    }

    public void AppendFormatted(string? value)
    {
        WarfareLoggerInterpolatedStringHandlerHelper.AppendArgument(_buffer, ref _bufferIndex, ref _parameterList, value);
    }

    internal void GetResult(out StringParameterList parameterList, out string literal)
    {
        literal = _bufferIndex == 0 ? string.Empty : new string(_buffer, 0, _bufferIndex);
        parameterList = _parameterList;
        ArrayPool<char>.Shared.Return(_buffer);
    }
}

[InterpolatedStringHandler]
public struct WarfareLoggerInterpolatedStringHandler
{
    private readonly char[] _buffer;
    private StringParameterList _parameterList;
    private int _bufferIndex;

    public WarfareLoggerInterpolatedStringHandler(int literalLength, int formattedCount, ILogger logger, LogLevel logLevel, out bool isEnabled)
        : this(literalLength, formattedCount)
    {
        isEnabled = logger.IsEnabled(logLevel);
    }

    public WarfareLoggerInterpolatedStringHandler(int literalLength, int formattedCount)
    {
        _buffer = ArrayPool<char>.Shared.Rent(literalLength + WarfareLoggerInterpolatedStringHandlerHelper.CalculateFormatLength(formattedCount));
        _parameterList = StringParameterList.CreateForAdding(formattedCount);
    }

    public void AppendLiteral(string s)
    {
        WarfareLoggerInterpolatedStringHandlerHelper.AppendLiteral(_buffer, ref _bufferIndex, s);
    }

    public void AppendFormatted<TValue>(TValue? value, string? format) where TValue : IFormattable
    {
        WarfareLoggerInterpolatedStringHandlerHelper.AppendArgument(_buffer, ref _bufferIndex, ref _parameterList,
            new FormattedValue { Value = value?.ToString(format, CultureInfo.InvariantCulture), Type = typeof(TValue), Alignment = int.MinValue });
    }

    public void AppendFormatted<TValue>(TValue? value, string? format, int alignment) where TValue : IFormattable
    {
        WarfareLoggerInterpolatedStringHandlerHelper.AppendArgument(_buffer, ref _bufferIndex, ref _parameterList,
            new FormattedValue { Value = value?.ToString(format, CultureInfo.InvariantCulture), Type = typeof(TValue), Alignment = alignment });
    }

    public void AppendFormatted<TValue>(TValue? value, int alignment) where TValue : IFormattable
    {
        WarfareLoggerInterpolatedStringHandlerHelper.AppendArgument(_buffer, ref _bufferIndex, ref _parameterList,
            new FormattedValue { Value = value, Type = typeof(TValue), Alignment = alignment });
    }

    public void AppendFormatted(object? value)
    {
        WarfareLoggerInterpolatedStringHandlerHelper.AppendArgument(_buffer, ref _bufferIndex, ref _parameterList, value);
    }

    public void AppendFormatted(scoped ReadOnlySpan<char> value)
    {
        WarfareLoggerInterpolatedStringHandlerHelper.AppendArgument(_buffer, ref _bufferIndex, ref _parameterList, new string(value));
    }

    public void AppendFormatted(string? value)
    {
        WarfareLoggerInterpolatedStringHandlerHelper.AppendArgument(_buffer, ref _bufferIndex, ref _parameterList, value);
    }

    internal void GetResult(out StringParameterList parameterList, out string literal)
    {
        literal = _bufferIndex == 0 ? string.Empty : new string(_buffer, 0, _bufferIndex);
        parameterList = _parameterList;
        ArrayPool<char>.Shared.Return(_buffer);
    }
}

internal static class WarfareLoggerInterpolatedStringHandlerHelper
{
    internal static void AppendLiteral(char[] buffer, ref int bufferIndex, string s)
    {
        int strLen = s.Length;
        s.CopyTo(0, buffer, bufferIndex, strLen);
        bufferIndex += strLen;
    }

    internal static void AppendArgument(char[] buffer, ref int bufferIndex, ref StringParameterList parameterList, object? value)
    {
        int index = parameterList.Count;
        buffer[bufferIndex] = '{';
        index.TryFormat(buffer.AsSpan(bufferIndex + 1), out int charsWritten, provider: CultureInfo.InvariantCulture);
        bufferIndex += 2 + charsWritten;
        buffer[bufferIndex - 1] = '}';

        parameterList.Add(value);
    }
    internal static int CalculateFormatLength(int formattedCount)
    {
        // count total length of all {n}'s that will be added
        //              "{n}"
        int fmtLength = 3 * formattedCount;
        if (formattedCount > 10)
        {
            int log10 = (int)Math.Log10(formattedCount - 1);
            for (int i = 1; i <= log10; ++i)
            {
                // number of numbers between 10 and 99 basically
                int spanRange = (Math.Min(formattedCount - 1, (int)Math.Pow(10, i + 1) - 1) - (int)Math.Pow(10, i) + 1);
                fmtLength += spanRange * i; // i is digit-count - 1, the first digit is already included in the fmtLength
            }
        }

        return fmtLength;
    }
}

public struct FormattedValue
{
    public object? Value;
    public Type Type;
    public int Alignment;

    /// <inheritdoc />
    public override string ToString()
    {
        return Alignment == int.MinValue ? Value?.ToString()! : string.Format($"{{0,{Alignment}}}", Value);
    }
}