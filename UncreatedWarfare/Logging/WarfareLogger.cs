using System;
using Uncreated.Warfare.Logging.Formatting;
using Uncreated.Warfare.Translations;

namespace Uncreated.Warfare.Logging;
public class WarfareLogger : ILogger
{
    private readonly string _categoryName;
    private readonly ITranslationValueFormatter _formatter;

    public WarfareLogger(string categoryName, ITranslationValueFormatter formatter)
    {
        _categoryName = categoryName;
        _formatter = formatter;
    }

    public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception exception, Func<TState, Exception, string> formatter)
    {
        string fmt;
        if (state is WarfareFormattedLogValues stateValues)
        {
            stateValues.ValueFormatter = _formatter;
            fmt = stateValues.Format(true);
            // todo filelog stateValues.Format(false);
        }
        else
        {
            fmt = formatter(state, exception);
            // todo filelog fmt;
        }

        // todo append log level

        switch (logLevel)
        {
            default:
                CommandWindow.Log(fmt);
                break;

            case LogLevel.Warning:
                CommandWindow.LogWarning(fmt);
                break;

            case LogLevel.Critical:
            case LogLevel.Error:
                CommandWindow.LogError(fmt);
                break;
        }
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
