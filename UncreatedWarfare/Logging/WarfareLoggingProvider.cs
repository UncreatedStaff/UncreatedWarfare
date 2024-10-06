using System;
using Uncreated.Warfare.Translations;

namespace Uncreated.Warfare.Logging;
public class WarfareLoggingProvider : ILoggerProvider
{
    private readonly ITranslationValueFormatter _formatter;
    public WarfareLoggingProvider(ITranslationValueFormatter formatter)
    {
        _formatter = formatter;
    }

    public ILogger CreateLogger(string categoryName)
    {
        return new WarfareLogger(categoryName, _formatter);
    }
    void IDisposable.Dispose() { }
}
