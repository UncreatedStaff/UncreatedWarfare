using StackCleaner;
using System;
using System.Globalization;
using Uncreated.Warfare.Logging.Formatting;
using Uncreated.Warfare.Translations;
using Uncreated.Warfare.Util;

namespace Uncreated.Warfare.Logging;
public class WarfareLogger : ILogger
{
    private static readonly string[] LogLevelsRaw = [ "TRC", "DBG", "INF", "WRN", "ERR", "CRT" ];
    private static readonly string[] LogLevelsANSI = [ "\u001b[47mTRC\u001b[49m", "\u001b[47mDBG\u001b[49m", "\u001b[46mINF\u001b[49m", "\u001b[43mWRN\u001b[49m", "\u001b[41mERR\u001b[49m", "\u001b[101mCRT\u001b[49m" ];
    private static readonly string[] LogLevelsExtendedANSI = LogLevelsANSI; // todo maybe add full rgb colors idk

    private readonly string _categoryName;
    private readonly WarfareLoggerProvider _loggerProvider;
    private readonly ITranslationValueFormatter? _formatter;

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

        _loggerProvider = loggerProvider;
        _formatter = formatter;
    }

    public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception exception, Func<TState, Exception, string> formatter)
    {
        DateTime timeStamp = DateTime.Now;

        string formattedText, unformattedText;
        if (state is WarfareFormattedLogValues stateValues)
        {
            stateValues.ValueFormatter = _formatter;
            formattedText = stateValues.Format(true);
            unformattedText = stateValues.Format(false);
        }
        else
        {
            formattedText = unformattedText = formatter(state, exception);
        }

        StackColorFormatType coloring = _formatter == null ? StackColorFormatType.None : _formatter.TranslationService.TerminalColoring;

        string[] array = coloring switch
        {
            StackColorFormatType.ExtendedANSIColor => LogLevelsExtendedANSI,
            StackColorFormatType.ANSIColor => LogLevelsANSI,
            _ => LogLevelsRaw
        };

        string timestampFmt = timeStamp.ToString("mm:ss.ff", CultureInfo.InvariantCulture);

        string logLevelText = array[logLevel is >= 0 and <= LogLevel.Critical ? (int)logLevel : (int)LogLevel.Information];

        if (coloring is StackColorFormatType.ExtendedANSIColor or StackColorFormatType.ANSIColor)
        {
            switch (logLevel)
            {
                case LogLevel.Information:
                    formattedText = "\u001b[36m" + formattedText.Replace(TerminalColorHelper.ForegroundResetSequence, "\u001b[36m");
                    break;

                case LogLevel.Warning:
                    formattedText = "\u001b[93m" + formattedText.Replace(TerminalColorHelper.ForegroundResetSequence, "\u001b[93m");
                    break;

                case LogLevel.Critical:
                case LogLevel.Error:
                    formattedText = "\u001b[91m" + formattedText.Replace(TerminalColorHelper.ForegroundResetSequence, "\u001b[91m");
                    break;

                case LogLevel.Trace:
                case LogLevel.Debug:
                    formattedText = formattedText.Replace(TerminalColorHelper.ForegroundResetSequence, "\u001b[90m");
                    break;
            }
        }

        formattedText = coloring switch
        {
            StackColorFormatType.ANSIColor or StackColorFormatType.ExtendedANSIColor => $"\u001b[90m\u001b[30m{logLevelText}\u001b[90m [{timestampFmt}] [{_categoryName}\u001b[90m] {formattedText}",
            _ => $"{logLevelText} [{timestampFmt}] [{_categoryName}] {formattedText}"
        };

        logLevelText = LogLevelsRaw[logLevel is >= 0 and <= LogLevel.Critical ? (int)logLevel : (int)LogLevel.Information];
        unformattedText = $"[{timeStamp.ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture)}] [{logLevelText}] [{_categoryName}] {unformattedText}";

        if (exception != null)
        {
            formattedText += Environment.NewLine + ExceptionFormatter.FormatException(exception, _loggerProvider.StackCleaner);
            unformattedText += Environment.NewLine + exception;
        }

        _loggerProvider.QueueOutput(logLevel, formattedText, unformattedText);
    }

    public bool IsEnabled(LogLevel logLevel)
    {
        return true;
    }

    public IDisposable BeginScope<TState>(TState state)
    {
        throw new NotSupportedException();
    }
}
