using StackCleaner;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Runtime.CompilerServices;
using Uncreated.Warfare.Logging.Formatting;
using Uncreated.Warfare.Translations;
using Uncreated.Warfare.Translations.Util;
using Uncreated.Warfare.Util;

namespace Uncreated.Warfare.Logging;
public class WarfareLogger : ILogger
{
    private static readonly string[] LogLevelsRaw = [ "TRC", "DBG", "INF", "WRN", "ERR", "CRT" ];
    private static readonly string[] LogLevelsANSI = [ "\e[47mTRC\e[49m", "\e[47mDBG\e[49m", "\e[46mINF\e[49m", "\e[43mWRN\e[49m", "\e[41mERR\e[49m", "\e[101mCRT\e[49m" ];
    private static readonly string[] LogLevelsExtendedANSI = LogLevelsANSI;

    private readonly string _categoryName;
    private readonly WarfareLoggerProvider _loggerProvider;
    private readonly ITranslationValueFormatter? _formatter;

    private List<Scope>? _scopeHierarchy;

    public WarfareLogger(string categoryName, WarfareLoggerProvider loggerProvider, ITranslationValueFormatter? formatter)
    {
        if (categoryName.StartsWith("Uncreated.Warfare", StringComparison.Ordinal))
        {
            ReadOnlySpan<char> categorySection = categoryName.AsSpan(categoryName.Length > 19 && categoryName[18] == '.' ? 19 : 18);
            int firstDot = categorySection.IndexOf('.');
            int lastDot = categorySection.LastIndexOf('.');
            if (firstDot == -1 || firstDot == lastDot)
                _categoryName = new string(categorySection);
            else
            {
                _categoryName = categorySection.Slice(0, firstDot).Concat(categorySection.Slice(lastDot, categorySection.Length - lastDot));
            }
        }
        else
        {
            _categoryName = categoryName;
        }

        _loggerProvider = loggerProvider;
        _formatter = formatter;
    }

    public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
    {
        DateTime timeStamp = DateTime.UtcNow;

        string formattedText, unformattedText;
        if (typeof(TState) == typeof(WarfareFormattedLogValues))
        {
            ref WarfareFormattedLogValues stateValues = ref Unsafe.As<TState, WarfareFormattedLogValues>(ref state);
            stateValues.ValueFormatter = _formatter;
            formattedText = stateValues.Format(true);
            unformattedText = stateValues.Format(false);
        }
        else
        {
            formattedText = unformattedText = formatter(state, exception);
        }

        if (logLevel is < LogLevel.Trace or > LogLevel.Critical)
            logLevel = LogLevel.Information;

        formattedText = CreateString(_formatter, _categoryName, _scopeHierarchy, this, formattedText, timeStamp, logLevel, false);
        unformattedText = CreateString(_formatter, _categoryName, _scopeHierarchy, this, unformattedText, timeStamp, logLevel, true);

        if (exception != null)
        {
            formattedText += Environment.NewLine + ExceptionFormatter.FormatException(exception, _loggerProvider.StackCleaner);
            unformattedText += Environment.NewLine + exception;
        }

        _loggerProvider.QueueOutput(logLevel, formattedText, unformattedText);
    }

    [Pure]
    public bool IsEnabled(LogLevel logLevel)
    {
        return true;
    }

    [MustUseReturnValue("Must be disposed after use to cancel the scope.")]
    public IDisposable BeginScope<TState>(TState state) where TState : notnull
    {
        return new Scope<TState>(state, this);
    }

    internal static string CreateString(ITranslationValueFormatter? formatter, string categoryName, List<Scope>? scopeHierarchy, object? locker, string message, DateTime timestamp, LogLevel logLevel, bool forFileLog, string? logLevelText = null, bool writeMessageColor = true, bool excludeCategory = false)
    {
        message ??= string.Empty;

        StackColorFormatType coloring = forFileLog || formatter == null ? StackColorFormatType.None : formatter.TranslationService.TerminalColoring;

        string[] array = coloring switch
        {
            StackColorFormatType.ExtendedANSIColor => LogLevelsExtendedANSI,
            StackColorFormatType.ANSIColor => LogLevelsANSI,
            _ => LogLevelsRaw
        };

        logLevelText ??= array[(int)logLevel];

        int dateTimeLength = forFileLog ? 19 : 8;
        int logLevelLength = logLevelText.Length;
        if (forFileLog)
            logLevelLength += 2;

        int length = 4 + message.Length + dateTimeLength + logLevelLength;

        if (!excludeCategory)
        {
            length += 3 + categoryName.Length;
        }

        if (coloring != StackColorFormatType.None)
        {
            length += TerminalColorHelper.GetTerminalColorSequenceLength(ConsoleColor.Black, false);
            length += TerminalColorHelper.GetTerminalColorSequenceLength(ConsoleColor.DarkGray, false);
            if (logLevel is not LogLevel.Debug and not LogLevel.Trace && writeMessageColor)
                length += TerminalColorHelper.GetTerminalColorSequenceLength(ConsoleColor.DarkCyan, false);
        }

        bool lockTaken = false;
        if (scopeHierarchy != null)
        {
            Monitor.Enter(locker, ref lockTaken);
            if (scopeHierarchy is { Count: > 0 })
            {
                length += 5;

                foreach (Scope scope in scopeHierarchy)
                    length += scope.Format(forFileLog).Length;

                length += (scopeHierarchy.Count - 1) * 3 /* ", " */;

                if (coloring != StackColorFormatType.None && writeMessageColor) length += scopeHierarchy.Count * 5;
            }
        }

        try
        {
            CreateLogStringState state = default;
            state.LogLevelText = logLevelText;
            state.ColorType = coloring;
            state.ForFileLog = forFileLog;
            state.LogLevel = logLevel;
            state.Timestamp = timestamp;
            state.Message = message;
            state.WriteMessageColor = writeMessageColor;
            state.ExcludeCategory = excludeCategory;
            state.CategoryName = categoryName;
            state.ScopeHierarchy = scopeHierarchy;

            return string.Create(length, state, CreateLog);
        }
        finally
        {
            if (lockTaken)
                Monitor.Exit(locker);
        }
    }

    private static void CreateLog(Span<char> span, CreateLogStringState state)
    {
        int index;
        if (state.ForFileLog)
        {
            span[0] = '['; // length = 19
            state.Timestamp.TryFormat(span[1..], out _, "yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture);
            span[20] = ']';
            span[21] = ' ';
            span[22] = '[';
            state.LogLevelText.AsSpan().CopyTo(span[23..]);
            index = 23 + state.LogLevelText.Length;
            span[index++] = ']';
            span[index++] = ' ';
        }
        else
        {
            index = 0;
            if (state.ColorType != StackColorFormatType.None) index = TerminalColorHelper.WriteTerminalColorSequence(span, ConsoleColor.Black, false);

            state.LogLevelText.AsSpan().CopyTo(span[index..]);
            index += state.LogLevelText.Length;

            if (state.ColorType != StackColorFormatType.None) index += TerminalColorHelper.WriteTerminalColorSequence(span[index..], ConsoleColor.DarkGray, false);

            span[index++] = ' ';
            span[index++] = '['; // length = 8
            state.Timestamp.TryFormat(span[index..], out _, "mm:ss.ff", CultureInfo.InvariantCulture);
            index += 8;
            span[index++] = ']';
            span[index++] = ' ';
        }

        if (!state.ExcludeCategory)
        {
            span[index++] = '[';
            state.CategoryName.AsSpan().CopyTo(span[index..]);
            index += state.CategoryName.Length;
            span[index++] = ']';
            span[index++] = ' ';
        }

        if (state.ScopeHierarchy is { Count: > 0 })
        {
            span[index++] = '|';
            span[index++] = ' ';

            bool any = false;
            foreach (Scope scope in state.ScopeHierarchy)
            {
                if (!any)
                    any = true;
                else
                {
                    if (state.ColorType != StackColorFormatType.None)
                        index += TerminalColorHelper.WriteTerminalColorSequence(span[index..], ConsoleColor.DarkGray, false);
                    span[index++] = ' ';
                    span[index++] = '/';
                    span[index++] = ' ';
                }
                string fmt = scope.Format(state.ForFileLog);
                fmt.AsSpan().CopyTo(span[index..]);
                index += fmt.Length;
            }

            if (state.ColorType != StackColorFormatType.None)
                index += TerminalColorHelper.WriteTerminalColorSequence(span[index..], ConsoleColor.DarkGray, false);
            span[index++] = ' ';
            span[index++] = '|';

            span[index++] = ' ';
        }

        if (state.WriteMessageColor && state.ColorType != StackColorFormatType.None)
        {
            ConsoleColor textColor = state.LogLevel switch
            {
                LogLevel.Information => ConsoleColor.DarkCyan,
                LogLevel.Warning => ConsoleColor.Yellow,
                LogLevel.Critical or LogLevel.Error => ConsoleColor.Red,
                _ => ConsoleColor.DarkGray
            };

            if (textColor != ConsoleColor.DarkGray)
            {
                index += TerminalColorHelper.WriteTerminalColorSequence(span[index..], textColor);
            }

            Span<char> message = span[index..];
            state.Message.AsSpan().CopyTo(message);

            // replace resets with the correct color
            while (true)
            {
                int nextIndex = ((ReadOnlySpan<char>)message).IndexOf(TerminalColorHelper.ForegroundResetSequence, StringComparison.Ordinal);
                if (nextIndex == -1) break;

                TerminalColorHelper.WriteTerminalColorSequence(message[nextIndex..], textColor);
                message = message.Slice(nextIndex + 5);
            }
        }
        else
        {
            state.Message.AsSpan().CopyTo(span[index..]);
        }
    }

    private struct CreateLogStringState
    {
        public string LogLevelText;
        public DateTime Timestamp;
        public StackColorFormatType ColorType;
        public LogLevel LogLevel;
        public bool ForFileLog;
        public string Message;
        public bool ExcludeCategory;
        public bool WriteMessageColor;
        public List<Scope>? ScopeHierarchy;
        public string CategoryName;
    }

    internal abstract class Scope : IDisposable
    {
        protected readonly WarfareLogger Logger;
        protected Scope(WarfareLogger logger)
        {
            Logger = logger;

            lock (Logger)
                (Logger._scopeHierarchy ??= new List<Scope>(1)).Add(this);
        }

        public abstract string Format(bool forFileLog);

        public void Dispose()
        {
            lock (Logger)
            {
                List<Scope>? hierarchy = Logger._scopeHierarchy;
                if (hierarchy == null)
                    return;

                hierarchy.Remove(this);
                if (hierarchy.Count == 0)
                    Logger._scopeHierarchy = null;
            }
        }
    }

    private class Scope<TState> : Scope
    {
        // ReSharper disable InconsistentlySynchronizedField
        private string? _stateCacheColored;
        private string? _stateCacheUncolored;
        private StackColorFormatType _cacheColorType;
        public TState State { get; }
        public Scope(TState state, WarfareLogger logger) : base(logger)
        {
            State = state;
        }

        public override string Format(bool forFileLog)
        {
            ITranslationValueFormatter? formatter = Logger._formatter;
            if (formatter == null)
                return State?.ToString() ?? string.Empty;

            if (forFileLog && _stateCacheUncolored != null)
                return _stateCacheUncolored;

            StackColorFormatType color = forFileLog
                ? StackColorFormatType.None
                : formatter.TranslationService.TerminalColoring;

            if (!forFileLog && _stateCacheColored != null && _cacheColorType == color)
                return _stateCacheColored;

            ArgumentFormat fmt = default;
            ValueFormatParameters parameters = new ValueFormatParameters(
                -1,
                CultureInfo.InvariantCulture,
                formatter.LanguageService.GetDefaultLanguage(),
                TimeZoneInfo.Utc,
                color is StackColorFormatType.ExtendedANSIColor or StackColorFormatType.ANSIColor
                    ? TranslationOptions.ForTerminal
                    : TranslationOptions.NoRichText,
                in fmt,
                null,
                null,
                null,
                1
            );

            if (forFileLog)
                return _stateCacheUncolored ??= formatter.Format(State, in parameters);

            string format = formatter.Format(State, in parameters);

            if (color != StackColorFormatType.None)
            {
                WarfareFormattedLogValues.TryDecideColor(State, out int argb, out int prefixSize, out _, color);
                if (prefixSize != 0)
                {
                    _cacheColorType = color;
                    return _stateCacheColored = TerminalColorHelper.WrapMessageWithTerminalColorSequence(argb, format, false);
                }
            }

            _cacheColorType = color;
            return _stateCacheColored = format;
        }
        // ReSharper restore InconsistentlySynchronizedField
    }
}
