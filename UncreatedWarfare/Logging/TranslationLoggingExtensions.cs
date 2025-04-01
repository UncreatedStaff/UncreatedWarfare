using System;
using Uncreated.Warfare.Translations;

namespace Uncreated.Warfare.Logging;

/// <summary>
/// Extensions that allow for logging translations directly to a <see cref="ILogger"/>.
/// </summary>
/// <remarks>Colors will be translated properly to virtual terminal sequences.</remarks>
public static class TranslationLoggingExtensions
{
    #region 0-arg
    /// <summary>
    /// Log a 0-arg translation using default language and settings as a DEBUG log.
    /// </summary>
    /// <exception cref="ArgumentException">The translation has arguments.</exception>
    public static void LogDebug(this ILogger logger, Translation translation)
    {
        logger.LogDebug(translation.Translate(forTerminal: true));
    }

    /// <summary>
    /// Log a 0-arg translation using default language and settings as a DEBUG log.
    /// </summary>
    /// <exception cref="ArgumentException">The translation has arguments.</exception>
    public static void LogDebug(this ILogger logger, Exception exception, Translation translation)
    {
        logger.LogDebug(exception, translation.Translate(forTerminal: true));
    }

    /// <summary>
    /// Log a 0-arg translation using default language and settings as a DEBUG log.
    /// </summary>
    /// <exception cref="ArgumentException">The translation has arguments.</exception>
    public static void LogDebug(this ILogger logger, EventId eventId, Translation translation)
    {
        logger.LogDebug(eventId, translation.Translate(forTerminal: true));
    }

    /// <summary>
    /// Log a 0-arg translation using default language and settings as a DEBUG log.
    /// </summary>
    /// <exception cref="ArgumentException">The translation has arguments.</exception>
    public static void LogDebug(this ILogger logger, EventId eventId, Exception exception, Translation translation)
    {
        logger.LogDebug(eventId, exception, translation.Translate(forTerminal: true));
    }

    /// <summary>
    /// Log a 0-arg translation using default language and settings as a TRACE log.
    /// </summary>
    /// <exception cref="ArgumentException">The translation has arguments.</exception>
    public static void LogTrace(this ILogger logger, Translation translation)
    {
        logger.LogTrace(translation.Translate(forTerminal: true));
    }

    /// <summary>
    /// Log a 0-arg translation using default language and settings as a TRACE log.
    /// </summary>
    /// <exception cref="ArgumentException">The translation has arguments.</exception>
    public static void LogTrace(this ILogger logger, Exception exception, Translation translation)
    {
        logger.LogTrace(exception, translation.Translate(forTerminal: true));
    }

    /// <summary>
    /// Log a 0-arg translation using default language and settings as a TRACE log.
    /// </summary>
    /// <exception cref="ArgumentException">The translation has arguments.</exception>
    public static void LogTrace(this ILogger logger, EventId eventId, Translation translation)
    {
        logger.LogTrace(eventId, translation.Translate(forTerminal: true));
    }

    /// <summary>
    /// Log a 0-arg translation using default language and settings as a TRACE log.
    /// </summary>
    /// <exception cref="ArgumentException">The translation has arguments.</exception>
    public static void LogTrace(this ILogger logger, EventId eventId, Exception exception, Translation translation)
    {
        logger.LogTrace(eventId, exception, translation.Translate(forTerminal: true));
    }

    /// <summary>
    /// Log a 0-arg translation using default language and settings as an INFORMATION log.
    /// </summary>
    /// <exception cref="ArgumentException">The translation has arguments.</exception>
    public static void LogInformation(this ILogger logger, Translation translation)
    {
        logger.LogInformation(translation.Translate(forTerminal: true));
    }

    /// <summary>
    /// Log a 0-arg translation using default language and settings as an INFORMATION log.
    /// </summary>
    /// <exception cref="ArgumentException">The translation has arguments.</exception>
    public static void LogInformation(this ILogger logger, Exception exception, Translation translation)
    {
        logger.LogInformation(exception, translation.Translate(forTerminal: true));
    }

    /// <summary>
    /// Log a 0-arg translation using default language and settings as an INFORMATION log.
    /// </summary>
    /// <exception cref="ArgumentException">The translation has arguments.</exception>
    public static void LogInformation(this ILogger logger, EventId eventId, Translation translation)
    {
        logger.LogInformation(eventId, translation.Translate(forTerminal: true));
    }

    /// <summary>
    /// Log a 0-arg translation using default language and settings as an INFORMATION log.
    /// </summary>
    /// <exception cref="ArgumentException">The translation has arguments.</exception>
    public static void LogInformation(this ILogger logger, EventId eventId, Exception exception, Translation translation)
    {
        logger.LogInformation(eventId, exception, translation.Translate(forTerminal: true));
    }

    /// <summary>
    /// Log a 0-arg translation using default language and settings as a WARNING log.
    /// </summary>
    /// <exception cref="ArgumentException">The translation has arguments.</exception>
    public static void LogWarning(this ILogger logger, Translation translation)
    {
        logger.LogWarning(translation.Translate(forTerminal: true));
    }

    /// <summary>
    /// Log a 0-arg translation using default language and settings as a WARNING log.
    /// </summary>
    /// <exception cref="ArgumentException">The translation has arguments.</exception>
    public static void LogWarning(this ILogger logger, Exception exception, Translation translation)
    {
        logger.LogWarning(exception, translation.Translate(forTerminal: true));
    }

    /// <summary>
    /// Log a 0-arg translation using default language and settings as a WARNING log.
    /// </summary>
    /// <exception cref="ArgumentException">The translation has arguments.</exception>
    public static void LogWarning(this ILogger logger, EventId eventId, Translation translation)
    {
        logger.LogWarning(eventId, translation.Translate(forTerminal: true));
    }

    /// <summary>
    /// Log a 0-arg translation using default language and settings as a WARNING log.
    /// </summary>
    /// <exception cref="ArgumentException">The translation has arguments.</exception>
    public static void LogWarning(this ILogger logger, EventId eventId, Exception exception, Translation translation)
    {
        logger.LogWarning(eventId, exception, translation.Translate(forTerminal: true));
    }

    /// <summary>
    /// Log a 0-arg translation using default language and settings as an ERROR log.
    /// </summary>
    /// <exception cref="ArgumentException">The translation has arguments.</exception>
    public static void LogError(this ILogger logger, Translation translation)
    {
        logger.LogError(translation.Translate(forTerminal: true));
    }

    /// <summary>
    /// Log a 0-arg translation using default language and settings as an ERROR log.
    /// </summary>
    /// <exception cref="ArgumentException">The translation has arguments.</exception>
    public static void LogError(this ILogger logger, Exception exception, Translation translation)
    {
        logger.LogError(exception, translation.Translate(forTerminal: true));
    }

    /// <summary>
    /// Log a 0-arg translation using default language and settings as an ERROR log.
    /// </summary>
    /// <exception cref="ArgumentException">The translation has arguments.</exception>
    public static void LogError(this ILogger logger, EventId eventId, Translation translation)
    {
        logger.LogError(eventId, translation.Translate(forTerminal: true));
    }

    /// <summary>
    /// Log a 0-arg translation using default language and settings as an ERROR log.
    /// </summary>
    /// <exception cref="ArgumentException">The translation has arguments.</exception>
    public static void LogError(this ILogger logger, EventId eventId, Exception exception, Translation translation)
    {
        logger.LogError(eventId, exception, translation.Translate(forTerminal: true));
    }

    /// <summary>
    /// Log a 0-arg translation using default language and settings as a CRITICAL log.
    /// </summary>
    /// <exception cref="ArgumentException">The translation has arguments.</exception>
    public static void LogCritical(this ILogger logger, Translation translation)
    {
        logger.LogCritical(translation.Translate(forTerminal: true));
    }

    /// <summary>
    /// Log a 0-arg translation using default language and settings as a CRITICAL log.
    /// </summary>
    /// <exception cref="ArgumentException">The translation has arguments.</exception>
    public static void LogCritical(this ILogger logger, Exception exception, Translation translation)
    {
        logger.LogCritical(exception, translation.Translate(forTerminal: true));
    }

    /// <summary>
    /// Log a 0-arg translation using default language and settings as a CRITICAL log.
    /// </summary>
    /// <exception cref="ArgumentException">The translation has arguments.</exception>
    public static void LogCritical(this ILogger logger, EventId eventId, Translation translation)
    {
        logger.LogCritical(eventId, translation.Translate(forTerminal: true));
    }

    /// <summary>
    /// Log a 0-arg translation using default language and settings as a CRITICAL log.
    /// </summary>
    /// <exception cref="ArgumentException">The translation has arguments.</exception>
    public static void LogCritical(this ILogger logger, EventId eventId, Exception exception, Translation translation)
    {
        logger.LogCritical(eventId, exception, translation.Translate(forTerminal: true));
    }

    /// <summary>
    /// Log a 0-arg translation using default language and settings.
    /// </summary>
    /// <exception cref="ArgumentException">The translation has arguments.</exception>
    public static void Log(this ILogger logger, LogLevel logLevel, Translation translation)
    {
        logger.Log(logLevel, translation.Translate(forTerminal: true));
    }

    /// <summary>
    /// Log a 0-arg translation using default language and settings.
    /// </summary>
    /// <exception cref="ArgumentException">The translation has arguments.</exception>
    public static void Log(this ILogger logger, LogLevel logLevel, Exception exception, Translation translation)
    {
        logger.Log(logLevel, exception, translation.Translate(forTerminal: true));
    }

    /// <summary>
    /// Log a 0-arg translation using default language and settings.
    /// </summary>
    /// <exception cref="ArgumentException">The translation has arguments.</exception>
    public static void Log(this ILogger logger, LogLevel logLevel, EventId eventId, Translation translation)
    {
        logger.Log(logLevel, eventId, translation.Translate(forTerminal: true));
    }

    /// <summary>
    /// Log a 0-arg translation using default language and settings.
    /// </summary>
    /// <exception cref="ArgumentException">The translation has arguments.</exception>
    public static void Log(this ILogger logger, LogLevel logLevel, EventId eventId, Exception exception, Translation translation)
    {
        logger.Log(logLevel, eventId, exception, translation.Translate(forTerminal: true));
    }
    #endregion

    #region 1-arg
    /// <summary>
    /// Log a 1-arg translation using default language and settings as a DEBUG log.
    /// </summary>
    public static void LogDebug<T0>(this ILogger logger, Translation<T0> translation, T0 arg0)
    {
        logger.LogDebug(translation.Translate(arg0, forTerminal: true));
    }

    /// <summary>
    /// Log a 1-arg translation using default language and settings as a DEBUG log.
    /// </summary>
    public static void LogDebug<T0>(this ILogger logger, Exception exception, Translation<T0> translation, T0 arg0)
    {
        logger.LogDebug(exception, translation.Translate(arg0, forTerminal: true));
    }

    /// <summary>
    /// Log a 1-arg translation using default language and settings as a DEBUG log.
    /// </summary>
    public static void LogDebug<T0>(this ILogger logger, EventId eventId, Translation<T0> translation, T0 arg0)
    {
        logger.LogDebug(eventId, translation.Translate(arg0, forTerminal: true));
    }

    /// <summary>
    /// Log a 1-arg translation using default language and settings as a DEBUG log.
    /// </summary>
    public static void LogDebug<T0>(this ILogger logger, EventId eventId, Exception exception, Translation<T0> translation, T0 arg0)
    {
        logger.LogDebug(eventId, exception, translation.Translate(arg0, forTerminal: true));
    }

    /// <summary>
    /// Log a 1-arg translation using default language and settings as a TRACE log.
    /// </summary>
    public static void LogTrace<T0>(this ILogger logger, Translation<T0> translation, T0 arg0)
    {
        logger.LogTrace(translation.Translate(arg0, forTerminal: true));
    }

    /// <summary>
    /// Log a 1-arg translation using default language and settings as a TRACE log.
    /// </summary>
    public static void LogTrace<T0>(this ILogger logger, Exception exception, Translation<T0> translation, T0 arg0)
    {
        logger.LogTrace(exception, translation.Translate(arg0, forTerminal: true));
    }

    /// <summary>
    /// Log a 1-arg translation using default language and settings as a TRACE log.
    /// </summary>
    public static void LogTrace<T0>(this ILogger logger, EventId eventId, Translation<T0> translation, T0 arg0)
    {
        logger.LogTrace(eventId, translation.Translate(arg0, forTerminal: true));
    }

    /// <summary>
    /// Log a 1-arg translation using default language and settings as a TRACE log.
    /// </summary>
    public static void LogTrace<T0>(this ILogger logger, EventId eventId, Exception exception, Translation<T0> translation, T0 arg0)
    {
        logger.LogTrace(eventId, exception, translation.Translate(arg0, forTerminal: true));
    }

    /// <summary>
    /// Log a 1-arg translation using default language and settings as an INFORMATION log.
    /// </summary>
    public static void LogInformation<T0>(this ILogger logger, Translation<T0> translation, T0 arg0)
    {
        logger.LogInformation(translation.Translate(arg0, forTerminal: true));
    }

    /// <summary>
    /// Log a 1-arg translation using default language and settings as an INFORMATION log.
    /// </summary>
    public static void LogInformation<T0>(this ILogger logger, Exception exception, Translation<T0> translation, T0 arg0)
    {
        logger.LogInformation(exception, translation.Translate(arg0, forTerminal: true));
    }

    /// <summary>
    /// Log a 1-arg translation using default language and settings as an INFORMATION log.
    /// </summary>
    public static void LogInformation<T0>(this ILogger logger, EventId eventId, Translation<T0> translation, T0 arg0)
    {
        logger.LogInformation(eventId, translation.Translate(arg0, forTerminal: true));
    }

    /// <summary>
    /// Log a 1-arg translation using default language and settings as an INFORMATION log.
    /// </summary>
    public static void LogInformation<T0>(this ILogger logger, EventId eventId, Exception exception, Translation<T0> translation, T0 arg0)
    {
        logger.LogInformation(eventId, exception, translation.Translate(arg0, forTerminal: true));
    }

    /// <summary>
    /// Log a 1-arg translation using default language and settings as a WARNING log.
    /// </summary>
    public static void LogWarning<T0>(this ILogger logger, Translation<T0> translation, T0 arg0)
    {
        logger.LogWarning(translation.Translate(arg0, forTerminal: true));
    }

    /// <summary>
    /// Log a 1-arg translation using default language and settings as a WARNING log.
    /// </summary>
    public static void LogWarning<T0>(this ILogger logger, Exception exception, Translation<T0> translation, T0 arg0)
    {
        logger.LogWarning(exception, translation.Translate(arg0, forTerminal: true));
    }

    /// <summary>
    /// Log a 1-arg translation using default language and settings as a WARNING log.
    /// </summary>
    public static void LogWarning<T0>(this ILogger logger, EventId eventId, Translation<T0> translation, T0 arg0)
    {
        logger.LogWarning(eventId, translation.Translate(arg0, forTerminal: true));
    }

    /// <summary>
    /// Log a 1-arg translation using default language and settings as a WARNING log.
    /// </summary>
    public static void LogWarning<T0>(this ILogger logger, EventId eventId, Exception exception, Translation<T0> translation, T0 arg0)
    {
        logger.LogWarning(eventId, exception, translation.Translate(arg0, forTerminal: true));
    }

    /// <summary>
    /// Log a 1-arg translation using default language and settings as an ERROR log.
    /// </summary>
    public static void LogError<T0>(this ILogger logger, Translation<T0> translation, T0 arg0)
    {
        logger.LogError(translation.Translate(arg0, forTerminal: true));
    }

    /// <summary>
    /// Log a 1-arg translation using default language and settings as an ERROR log.
    /// </summary>
    public static void LogError<T0>(this ILogger logger, Exception exception, Translation<T0> translation, T0 arg0)
    {
        logger.LogError(exception, translation.Translate(arg0, forTerminal: true));
    }

    /// <summary>
    /// Log a 1-arg translation using default language and settings as an ERROR log.
    /// </summary>
    public static void LogError<T0>(this ILogger logger, EventId eventId, Translation<T0> translation, T0 arg0)
    {
        logger.LogError(eventId, translation.Translate(arg0, forTerminal: true));
    }

    /// <summary>
    /// Log a 1-arg translation using default language and settings as an ERROR log.
    /// </summary>
    public static void LogError<T0>(this ILogger logger, EventId eventId, Exception exception, Translation<T0> translation, T0 arg0)
    {
        logger.LogError(eventId, exception, translation.Translate(arg0, forTerminal: true));
    }

    /// <summary>
    /// Log a 1-arg translation using default language and settings as a CRITICAL log.
    /// </summary>
    public static void LogCritical<T0>(this ILogger logger, Translation<T0> translation, T0 arg0)
    {
        logger.LogCritical(translation.Translate(arg0, forTerminal: true));
    }

    /// <summary>
    /// Log a 1-arg translation using default language and settings as a CRITICAL log.
    /// </summary>
    public static void LogCritical<T0>(this ILogger logger, Exception exception, Translation<T0> translation, T0 arg0)
    {
        logger.LogCritical(exception, translation.Translate(arg0, forTerminal: true));
    }

    /// <summary>
    /// Log a 1-arg translation using default language and settings as a CRITICAL log.
    /// </summary>
    public static void LogCritical<T0>(this ILogger logger, EventId eventId, Translation<T0> translation, T0 arg0)
    {
        logger.LogCritical(eventId, translation.Translate(arg0, forTerminal: true));
    }

    /// <summary>
    /// Log a 1-arg translation using default language and settings as a CRITICAL log.
    /// </summary>
    public static void LogCritical<T0>(this ILogger logger, EventId eventId, Exception exception, Translation<T0> translation, T0 arg0)
    {
        logger.LogCritical(eventId, exception, translation.Translate(arg0, forTerminal: true));
    }

    /// <summary>
    /// Log a 1-arg translation using default language and settings.
    /// </summary>
    public static void Log<T0>(this ILogger logger, LogLevel logLevel, Translation<T0> translation, T0 arg0)
    {
        logger.Log(logLevel, translation.Translate(arg0, forTerminal: true));
    }

    /// <summary>
    /// Log a 1-arg translation using default language and settings.
    /// </summary>
    public static void Log<T0>(this ILogger logger, LogLevel logLevel, Exception exception, Translation<T0> translation, T0 arg0)
    {
        logger.Log(logLevel, exception, translation.Translate(arg0, forTerminal: true));
    }

    /// <summary>
    /// Log a 1-arg translation using default language and settings.
    /// </summary>
    public static void Log<T0>(this ILogger logger, LogLevel logLevel, EventId eventId, Translation<T0> translation, T0 arg0)
    {
        logger.Log(logLevel, eventId, translation.Translate(arg0, forTerminal: true));
    }

    /// <summary>
    /// Log a 1-arg translation using default language and settings.
    /// </summary>
    public static void Log<T0>(this ILogger logger, LogLevel logLevel, EventId eventId, Exception exception, Translation<T0> translation, T0 arg0)
    {
        logger.Log(logLevel, eventId, exception, translation.Translate(arg0, forTerminal: true));
    }
    #endregion

    #region 2-arg

    /// <summary>
    /// Log a 2-arg translation using default language and settings as a DEBUG log.
    /// </summary>
    public static void LogDebug<T0, T1>(this ILogger logger, Translation<T0, T1> translation, T0 arg0, T1 arg1)
    {
        logger.LogDebug(translation.Translate(arg0, arg1, forTerminal: true));
    }

    /// <summary>
    /// Log a 2-arg translation using default language and settings as a DEBUG log.
    /// </summary>
    public static void LogDebug<T0, T1>(this ILogger logger, Exception exception, Translation<T0, T1> translation, T0 arg0, T1 arg1)
    {
        logger.LogDebug(exception, translation.Translate(arg0, arg1, forTerminal: true));
    }

    /// <summary>
    /// Log a 2-arg translation using default language and settings as a DEBUG log.
    /// </summary>
    public static void LogDebug<T0, T1>(this ILogger logger, EventId eventId, Translation<T0, T1> translation, T0 arg0, T1 arg1)
    {
        logger.LogDebug(eventId, translation.Translate(arg0, arg1, forTerminal: true));
    }

    /// <summary>
    /// Log a 2-arg translation using default language and settings as a DEBUG log.
    /// </summary>
    public static void LogDebug<T0, T1>(this ILogger logger, EventId eventId, Exception exception, Translation<T0, T1> translation, T0 arg0, T1 arg1)
    {
        logger.LogDebug(eventId, exception, translation.Translate(arg0, arg1, forTerminal: true));
    }

    /// <summary>
    /// Log a 2-arg translation using default language and settings as a TRACE log.
    /// </summary>
    public static void LogTrace<T0, T1>(this ILogger logger, Translation<T0, T1> translation, T0 arg0, T1 arg1)
    {
        logger.LogTrace(translation.Translate(arg0, arg1, forTerminal: true));
    }

    /// <summary>
    /// Log a 2-arg translation using default language and settings as a TRACE log.
    /// </summary>
    public static void LogTrace<T0, T1>(this ILogger logger, Exception exception, Translation<T0, T1> translation, T0 arg0, T1 arg1)
    {
        logger.LogTrace(exception, translation.Translate(arg0, arg1, forTerminal: true));
    }

    /// <summary>
    /// Log a 2-arg translation using default language and settings as a TRACE log.
    /// </summary>
    public static void LogTrace<T0, T1>(this ILogger logger, EventId eventId, Translation<T0, T1> translation, T0 arg0, T1 arg1)
    {
        logger.LogTrace(eventId, translation.Translate(arg0, arg1, forTerminal: true));
    }

    /// <summary>
    /// Log a 2-arg translation using default language and settings as a TRACE log.
    /// </summary>
    public static void LogTrace<T0, T1>(this ILogger logger, EventId eventId, Exception exception, Translation<T0, T1> translation, T0 arg0, T1 arg1)
    {
        logger.LogTrace(eventId, exception, translation.Translate(arg0, arg1, forTerminal: true));
    }

    /// <summary>
    /// Log a 2-arg translation using default language and settings as an INFORMATION log.
    /// </summary>
    public static void LogInformation<T0, T1>(this ILogger logger, Translation<T0, T1> translation, T0 arg0, T1 arg1)
    {
        logger.LogInformation(translation.Translate(arg0, arg1, forTerminal: true));
    }

    /// <summary>
    /// Log a 2-arg translation using default language and settings as an INFORMATION log.
    /// </summary>
    public static void LogInformation<T0, T1>(this ILogger logger, Exception exception, Translation<T0, T1> translation, T0 arg0, T1 arg1)
    {
        logger.LogInformation(exception, translation.Translate(arg0, arg1, forTerminal: true));
    }

    /// <summary>
    /// Log a 2-arg translation using default language and settings as an INFORMATION log.
    /// </summary>
    public static void LogInformation<T0, T1>(this ILogger logger, EventId eventId, Translation<T0, T1> translation, T0 arg0, T1 arg1)
    {
        logger.LogInformation(eventId, translation.Translate(arg0, arg1, forTerminal: true));
    }

    /// <summary>
    /// Log a 2-arg translation using default language and settings as an INFORMATION log.
    /// </summary>
    public static void LogInformation<T0, T1>(this ILogger logger, EventId eventId, Exception exception, Translation<T0, T1> translation, T0 arg0, T1 arg1)
    {
        logger.LogInformation(eventId, exception, translation.Translate(arg0, arg1, forTerminal: true));
    }

    /// <summary>
    /// Log a 2-arg translation using default language and settings as a WARNING log.
    /// </summary>
    public static void LogWarning<T0, T1>(this ILogger logger, Translation<T0, T1> translation, T0 arg0, T1 arg1)
    {
        logger.LogWarning(translation.Translate(arg0, arg1, forTerminal: true));
    }

    /// <summary>
    /// Log a 2-arg translation using default language and settings as a WARNING log.
    /// </summary>
    public static void LogWarning<T0, T1>(this ILogger logger, Exception exception, Translation<T0, T1> translation, T0 arg0, T1 arg1)
    {
        logger.LogWarning(exception, translation.Translate(arg0, arg1, forTerminal: true));
    }

    /// <summary>
    /// Log a 2-arg translation using default language and settings as a WARNING log.
    /// </summary>
    public static void LogWarning<T0, T1>(this ILogger logger, EventId eventId, Translation<T0, T1> translation, T0 arg0, T1 arg1)
    {
        logger.LogWarning(eventId, translation.Translate(arg0, arg1, forTerminal: true));
    }

    /// <summary>
    /// Log a 2-arg translation using default language and settings as a WARNING log.
    /// </summary>
    public static void LogWarning<T0, T1>(this ILogger logger, EventId eventId, Exception exception, Translation<T0, T1> translation, T0 arg0, T1 arg1)
    {
        logger.LogWarning(eventId, exception, translation.Translate(arg0, arg1, forTerminal: true));
    }

    /// <summary>
    /// Log a 2-arg translation using default language and settings as an ERROR log.
    /// </summary>
    public static void LogError<T0, T1>(this ILogger logger, Translation<T0, T1> translation, T0 arg0, T1 arg1)
    {
        logger.LogError(translation.Translate(arg0, arg1, forTerminal: true));
    }

    /// <summary>
    /// Log a 2-arg translation using default language and settings as an ERROR log.
    /// </summary>
    public static void LogError<T0, T1>(this ILogger logger, Exception exception, Translation<T0, T1> translation, T0 arg0, T1 arg1)
    {
        logger.LogError(exception, translation.Translate(arg0, arg1, forTerminal: true));
    }

    /// <summary>
    /// Log a 2-arg translation using default language and settings as an ERROR log.
    /// </summary>
    public static void LogError<T0, T1>(this ILogger logger, EventId eventId, Translation<T0, T1> translation, T0 arg0, T1 arg1)
    {
        logger.LogError(eventId, translation.Translate(arg0, arg1, forTerminal: true));
    }

    /// <summary>
    /// Log a 2-arg translation using default language and settings as an ERROR log.
    /// </summary>
    public static void LogError<T0, T1>(this ILogger logger, EventId eventId, Exception exception, Translation<T0, T1> translation, T0 arg0, T1 arg1)
    {
        logger.LogError(eventId, exception, translation.Translate(arg0, arg1, forTerminal: true));
    }

    /// <summary>
    /// Log a 2-arg translation using default language and settings as a CRITICAL log.
    /// </summary>
    public static void LogCritical<T0, T1>(this ILogger logger, Translation<T0, T1> translation, T0 arg0, T1 arg1)
    {
        logger.LogCritical(translation.Translate(arg0, arg1, forTerminal: true));
    }

    /// <summary>
    /// Log a 2-arg translation using default language and settings as a CRITICAL log.
    /// </summary>
    public static void LogCritical<T0, T1>(this ILogger logger, Exception exception, Translation<T0, T1> translation, T0 arg0, T1 arg1)
    {
        logger.LogCritical(exception, translation.Translate(arg0, arg1, forTerminal: true));
    }

    /// <summary>
    /// Log a 2-arg translation using default language and settings as a CRITICAL log.
    /// </summary>
    public static void LogCritical<T0, T1>(this ILogger logger, EventId eventId, Translation<T0, T1> translation, T0 arg0, T1 arg1)
    {
        logger.LogCritical(eventId, translation.Translate(arg0, arg1, forTerminal: true));
    }

    /// <summary>
    /// Log a 2-arg translation using default language and settings as a CRITICAL log.
    /// </summary>
    public static void LogCritical<T0, T1>(this ILogger logger, EventId eventId, Exception exception, Translation<T0, T1> translation, T0 arg0, T1 arg1)
    {
        logger.LogCritical(eventId, exception, translation.Translate(arg0, arg1, forTerminal: true));
    }

    /// <summary>
    /// Log a 2-arg translation using default language and settings.
    /// </summary>
    public static void Log<T0, T1>(this ILogger logger, LogLevel logLevel, Translation<T0, T1> translation, T0 arg0, T1 arg1)
    {
        logger.Log(logLevel, translation.Translate(arg0, arg1, forTerminal: true));
    }

    /// <summary>
    /// Log a 2-arg translation using default language and settings.
    /// </summary>
    public static void Log<T0, T1>(this ILogger logger, LogLevel logLevel, Exception exception, Translation<T0, T1> translation, T0 arg0, T1 arg1)
    {
        logger.Log(logLevel, exception, translation.Translate(arg0, arg1, forTerminal: true));
    }

    /// <summary>
    /// Log a 2-arg translation using default language and settings.
    /// </summary>
    public static void Log<T0, T1>(this ILogger logger, LogLevel logLevel, EventId eventId, Translation<T0, T1> translation, T0 arg0, T1 arg1)
    {
        logger.Log(logLevel, eventId, translation.Translate(arg0, arg1, forTerminal: true));
    }

    /// <summary>
    /// Log a 2-arg translation using default language and settings.
    /// </summary>
    public static void Log<T0, T1>(this ILogger logger, LogLevel logLevel, EventId eventId, Exception exception, Translation<T0, T1> translation, T0 arg0, T1 arg1)
    {
        logger.Log(logLevel, eventId, exception, translation.Translate(arg0, arg1, forTerminal: true));
    }
    #endregion

    #region 3-arg
    /// <summary>
    /// Log a 3-arg translation using default language and settings as a DEBUG log.
    /// </summary>
    public static void LogDebug<T0, T1, T2>(this ILogger logger, Translation<T0, T1, T2> translation, T0 arg0, T1 arg1, T2 arg2)
    {
        logger.LogDebug(translation.Translate(arg0, arg1, arg2, forTerminal: true));
    }

    /// <summary>
    /// Log a 3-arg translation using default language and settings as a DEBUG log.
    /// </summary>
    public static void LogDebug<T0, T1, T2>(this ILogger logger, Exception exception, Translation<T0, T1, T2> translation, T0 arg0, T1 arg1, T2 arg2)
    {
        logger.LogDebug(exception, translation.Translate(arg0, arg1, arg2, forTerminal: true));
    }

    /// <summary>
    /// Log a 3-arg translation using default language and settings as a DEBUG log.
    /// </summary>
    public static void LogDebug<T0, T1, T2>(this ILogger logger, EventId eventId, Translation<T0, T1, T2> translation, T0 arg0, T1 arg1, T2 arg2)
    {
        logger.LogDebug(eventId, translation.Translate(arg0, arg1, arg2, forTerminal: true));
    }

    /// <summary>
    /// Log a 3-arg translation using default language and settings as a DEBUG log.
    /// </summary>
    public static void LogDebug<T0, T1, T2>(this ILogger logger, EventId eventId, Exception exception, Translation<T0, T1, T2> translation, T0 arg0, T1 arg1, T2 arg2)
    {
        logger.LogDebug(eventId, exception, translation.Translate(arg0, arg1, arg2, forTerminal: true));
    }

    /// <summary>
    /// Log a 3-arg translation using default language and settings as an INFORMATION log.
    /// </summary>
    public static void LogTrace<T0, T1, T2>(this ILogger logger, Translation<T0, T1, T2> translation, T0 arg0, T1 arg1, T2 arg2)
    {
        logger.LogTrace(translation.Translate(arg0, arg1, arg2, forTerminal: true));
    }

    /// <summary>
    /// Log a 3-arg translation using default language and settings as an INFORMATION log.
    /// </summary>
    public static void LogTrace<T0, T1, T2>(this ILogger logger, Exception exception, Translation<T0, T1, T2> translation, T0 arg0, T1 arg1, T2 arg2)
    {
        logger.LogTrace(exception, translation.Translate(arg0, arg1, arg2, forTerminal: true));
    }

    /// <summary>
    /// Log a 3-arg translation using default language and settings as an INFORMATION log.
    /// </summary>
    public static void LogTrace<T0, T1, T2>(this ILogger logger, EventId eventId, Translation<T0, T1, T2> translation, T0 arg0, T1 arg1, T2 arg2)
    {
        logger.LogTrace(eventId, translation.Translate(arg0, arg1, arg2, forTerminal: true));
    }

    /// <summary>
    /// Log a 3-arg translation using default language and settings as an INFORMATION log.
    /// </summary>
    public static void LogTrace<T0, T1, T2>(this ILogger logger, EventId eventId, Exception exception, Translation<T0, T1, T2> translation, T0 arg0, T1 arg1, T2 arg2)
    {
        logger.LogTrace(eventId, exception, translation.Translate(arg0, arg1, arg2, forTerminal: true));
    }

    /// <summary>
    /// Log a 3-arg translation using default language and settings as an INFORMATION log.
    /// </summary>
    public static void LogInformation<T0, T1, T2>(this ILogger logger, Translation<T0, T1, T2> translation, T0 arg0, T1 arg1, T2 arg2)
    {
        logger.LogInformation(translation.Translate(arg0, arg1, arg2, forTerminal: true));
    }

    /// <summary>
    /// Log a 3-arg translation using default language and settings as an INFORMATION log.
    /// </summary>
    public static void LogInformation<T0, T1, T2>(this ILogger logger, Exception exception, Translation<T0, T1, T2> translation, T0 arg0, T1 arg1, T2 arg2)
    {
        logger.LogInformation(exception, translation.Translate(arg0, arg1, arg2, forTerminal: true));
    }

    /// <summary>
    /// Log a 3-arg translation using default language and settings as an INFORMATION log.
    /// </summary>
    public static void LogInformation<T0, T1, T2>(this ILogger logger, EventId eventId, Translation<T0, T1, T2> translation, T0 arg0, T1 arg1, T2 arg2)
    {
        logger.LogInformation(eventId, translation.Translate(arg0, arg1, arg2, forTerminal: true));
    }

    /// <summary>
    /// Log a 3-arg translation using default language and settings as an INFORMATION log.
    /// </summary>
    public static void LogInformation<T0, T1, T2>(this ILogger logger, EventId eventId, Exception exception, Translation<T0, T1, T2> translation, T0 arg0, T1 arg1, T2 arg2)
    {
        logger.LogInformation(eventId, exception, translation.Translate(arg0, arg1, arg2, forTerminal: true));
    }

    /// <summary>
    /// Log a 3-arg translation using default language and settings as a WARNING log.
    /// </summary>
    public static void LogWarning<T0, T1, T2>(this ILogger logger, Translation<T0, T1, T2> translation, T0 arg0, T1 arg1, T2 arg2)
    {
        logger.LogWarning(translation.Translate(arg0, arg1, arg2, forTerminal: true));
    }

    /// <summary>
    /// Log a 3-arg translation using default language and settings as a WARNING log.
    /// </summary>
    public static void LogWarning<T0, T1, T2>(this ILogger logger, Exception exception, Translation<T0, T1, T2> translation, T0 arg0, T1 arg1, T2 arg2)
    {
        logger.LogWarning(exception, translation.Translate(arg0, arg1, arg2, forTerminal: true));
    }

    /// <summary>
    /// Log a 3-arg translation using default language and settings as a WARNING log.
    /// </summary>
    public static void LogWarning<T0, T1, T2>(this ILogger logger, EventId eventId, Translation<T0, T1, T2> translation, T0 arg0, T1 arg1, T2 arg2)
    {
        logger.LogWarning(eventId, translation.Translate(arg0, arg1, arg2, forTerminal: true));
    }

    /// <summary>
    /// Log a 3-arg translation using default language and settings as a WARNING log.
    /// </summary>
    public static void LogWarning<T0, T1, T2>(this ILogger logger, EventId eventId, Exception exception, Translation<T0, T1, T2> translation, T0 arg0, T1 arg1, T2 arg2)
    {
        logger.LogWarning(eventId, exception, translation.Translate(arg0, arg1, arg2, forTerminal: true));
    }

    /// <summary>
    /// Log a 3-arg translation using default language and settings as an ERROR log.
    /// </summary>
    public static void LogError<T0, T1, T2>(this ILogger logger, Translation<T0, T1, T2> translation, T0 arg0, T1 arg1, T2 arg2)
    {
        logger.LogError(translation.Translate(arg0, arg1, arg2, forTerminal: true));
    }

    /// <summary>
    /// Log a 3-arg translation using default language and settings as an ERROR log.
    /// </summary>
    public static void LogError<T0, T1, T2>(this ILogger logger, Exception exception, Translation<T0, T1, T2> translation, T0 arg0, T1 arg1, T2 arg2)
    {
        logger.LogError(exception, translation.Translate(arg0, arg1, arg2, forTerminal: true));
    }

    /// <summary>
    /// Log a 3-arg translation using default language and settings as an ERROR log.
    /// </summary>
    public static void LogError<T0, T1, T2>(this ILogger logger, EventId eventId, Translation<T0, T1, T2> translation, T0 arg0, T1 arg1, T2 arg2)
    {
        logger.LogError(eventId, translation.Translate(arg0, arg1, arg2, forTerminal: true));
    }

    /// <summary>
    /// Log a 3-arg translation using default language and settings as an ERROR log.
    /// </summary>
    public static void LogError<T0, T1, T2>(this ILogger logger, EventId eventId, Exception exception, Translation<T0, T1, T2> translation, T0 arg0, T1 arg1, T2 arg2)
    {
        logger.LogError(eventId, exception, translation.Translate(arg0, arg1, arg2, forTerminal: true));
    }

    /// <summary>
    /// Log a 3-arg translation using default language and settings as a CRITICAL log.
    /// </summary>
    public static void LogCritical<T0, T1, T2>(this ILogger logger, Translation<T0, T1, T2> translation, T0 arg0, T1 arg1, T2 arg2)
    {
        logger.LogCritical(translation.Translate(arg0, arg1, arg2, forTerminal: true));
    }

    /// <summary>
    /// Log a 3-arg translation using default language and settings as a CRITICAL log.
    /// </summary>
    public static void LogCritical<T0, T1, T2>(this ILogger logger, Exception exception, Translation<T0, T1, T2> translation, T0 arg0, T1 arg1, T2 arg2)
    {
        logger.LogCritical(exception, translation.Translate(arg0, arg1, arg2, forTerminal: true));
    }

    /// <summary>
    /// Log a 3-arg translation using default language and settings as a CRITICAL log.
    /// </summary>
    public static void LogCritical<T0, T1, T2>(this ILogger logger, EventId eventId, Translation<T0, T1, T2> translation, T0 arg0, T1 arg1, T2 arg2)
    {
        logger.LogCritical(eventId, translation.Translate(arg0, arg1, arg2, forTerminal: true));
    }

    /// <summary>
    /// Log a 3-arg translation using default language and settings as a CRITICAL log.
    /// </summary>
    public static void LogCritical<T0, T1, T2>(this ILogger logger, EventId eventId, Exception exception, Translation<T0, T1, T2> translation, T0 arg0, T1 arg1, T2 arg2)
    {
        logger.LogCritical(eventId, exception, translation.Translate(arg0, arg1, arg2, forTerminal: true));
    }

    /// <summary>
    /// Log a 3-arg translation using default language and settings.
    /// </summary>
    public static void Log<T0, T1, T2>(this ILogger logger, LogLevel logLevel, Translation<T0, T1, T2> translation, T0 arg0, T1 arg1, T2 arg2)
    {
        logger.Log(logLevel, translation.Translate(arg0, arg1, arg2, forTerminal: true));
    }

    /// <summary>
    /// Log a 3-arg translation using default language and settings.
    /// </summary>
    public static void Log<T0, T1, T2>(this ILogger logger, LogLevel logLevel, Exception exception, Translation<T0, T1, T2> translation, T0 arg0, T1 arg1, T2 arg2)
    {
        logger.Log(logLevel, exception, translation.Translate(arg0, arg1, arg2, forTerminal: true));
    }

    /// <summary>
    /// Log a 3-arg translation using default language and settings.
    /// </summary>
    public static void Log<T0, T1, T2>(this ILogger logger, LogLevel logLevel, EventId eventId, Translation<T0, T1, T2> translation, T0 arg0, T1 arg1, T2 arg2)
    {
        logger.Log(logLevel, eventId, translation.Translate(arg0, arg1, arg2, forTerminal: true));
    }

    /// <summary>
    /// Log a 3-arg translation using default language and settings.
    /// </summary>
    public static void Log<T0, T1, T2>(this ILogger logger, LogLevel logLevel, EventId eventId, Exception exception, Translation<T0, T1, T2> translation, T0 arg0, T1 arg1, T2 arg2)
    {
        logger.Log(logLevel, eventId, exception, translation.Translate(arg0, arg1, arg2, forTerminal: true));
    }
    #endregion

    #region 4-arg
    /// <summary>
    /// Log a 4-arg translation using default language and settings as a DEBUG log.
    /// </summary>
    public static void LogDebug<T0, T1, T2, T3>(this ILogger logger, Translation<T0, T1, T2, T3> translation, T0 arg0, T1 arg1, T2 arg2, T3 arg3)
    {
        logger.LogDebug(translation.Translate(arg0, arg1, arg2, arg3, forTerminal: true));
    }

    /// <summary>
    /// Log a 4-arg translation using default language and settings as a DEBUG log.
    /// </summary>
    public static void LogDebug<T0, T1, T2, T3>(this ILogger logger, Exception exception, Translation<T0, T1, T2, T3> translation, T0 arg0, T1 arg1, T2 arg2, T3 arg3)
    {
        logger.LogDebug(exception, translation.Translate(arg0, arg1, arg2, arg3, forTerminal: true));
    }

    /// <summary>
    /// Log a 4-arg translation using default language and settings as a DEBUG log.
    /// </summary>
    public static void LogDebug<T0, T1, T2, T3>(this ILogger logger, EventId eventId, Translation<T0, T1, T2, T3> translation, T0 arg0, T1 arg1, T2 arg2, T3 arg3)
    {
        logger.LogDebug(eventId, translation.Translate(arg0, arg1, arg2, arg3, forTerminal: true));
    }

    /// <summary>
    /// Log a 4-arg translation using default language and settings as a DEBUG log.
    /// </summary>
    public static void LogDebug<T0, T1, T2, T3>(this ILogger logger, EventId eventId, Exception exception, Translation<T0, T1, T2, T3> translation, T0 arg0, T1 arg1, T2 arg2, T3 arg3)
    {
        logger.LogDebug(eventId, exception, translation.Translate(arg0, arg1, arg2, arg3, forTerminal: true));
    }

    /// <summary>
    /// Log a 4-arg translation using default language and settings as an INFORMATION log.
    /// </summary>
    public static void LogTrace<T0, T1, T2, T3>(this ILogger logger, Translation<T0, T1, T2, T3> translation, T0 arg0, T1 arg1, T2 arg2, T3 arg3)
    {
        logger.LogTrace(translation.Translate(arg0, arg1, arg2, arg3, forTerminal: true));
    }

    /// <summary>
    /// Log a 4-arg translation using default language and settings as an INFORMATION log.
    /// </summary>
    public static void LogTrace<T0, T1, T2, T3>(this ILogger logger, Exception exception, Translation<T0, T1, T2, T3> translation, T0 arg0, T1 arg1, T2 arg2, T3 arg3)
    {
        logger.LogTrace(exception, translation.Translate(arg0, arg1, arg2, arg3, forTerminal: true));
    }

    /// <summary>
    /// Log a 4-arg translation using default language and settings as an INFORMATION log.
    /// </summary>
    public static void LogTrace<T0, T1, T2, T3>(this ILogger logger, EventId eventId, Translation<T0, T1, T2, T3> translation, T0 arg0, T1 arg1, T2 arg2, T3 arg3)
    {
        logger.LogTrace(eventId, translation.Translate(arg0, arg1, arg2, arg3, forTerminal: true));
    }

    /// <summary>
    /// Log a 4-arg translation using default language and settings as an INFORMATION log.
    /// </summary>
    public static void LogTrace<T0, T1, T2, T3>(this ILogger logger, EventId eventId, Exception exception, Translation<T0, T1, T2, T3> translation, T0 arg0, T1 arg1, T2 arg2, T3 arg3)
    {
        logger.LogTrace(eventId, exception, translation.Translate(arg0, arg1, arg2, arg3, forTerminal: true));
    }

    /// <summary>
    /// Log a 4-arg translation using default language and settings as an INFORMATION log.
    /// </summary>
    public static void LogInformation<T0, T1, T2, T3>(this ILogger logger, Translation<T0, T1, T2, T3> translation, T0 arg0, T1 arg1, T2 arg2, T3 arg3)
    {
        logger.LogInformation(translation.Translate(arg0, arg1, arg2, arg3, forTerminal: true));
    }

    /// <summary>
    /// Log a 4-arg translation using default language and settings as an INFORMATION log.
    /// </summary>
    public static void LogInformation<T0, T1, T2, T3>(this ILogger logger, Exception exception, Translation<T0, T1, T2, T3> translation, T0 arg0, T1 arg1, T2 arg2, T3 arg3)
    {
        logger.LogInformation(exception, translation.Translate(arg0, arg1, arg2, arg3, forTerminal: true));
    }

    /// <summary>
    /// Log a 4-arg translation using default language and settings as an INFORMATION log.
    /// </summary>
    public static void LogInformation<T0, T1, T2, T3>(this ILogger logger, EventId eventId, Translation<T0, T1, T2, T3> translation, T0 arg0, T1 arg1, T2 arg2, T3 arg3)
    {
        logger.LogInformation(eventId, translation.Translate(arg0, arg1, arg2, arg3, forTerminal: true));
    }

    /// <summary>
    /// Log a 4-arg translation using default language and settings as an INFORMATION log.
    /// </summary>
    public static void LogInformation<T0, T1, T2, T3>(this ILogger logger, EventId eventId, Exception exception, Translation<T0, T1, T2, T3> translation, T0 arg0, T1 arg1, T2 arg2, T3 arg3)
    {
        logger.LogInformation(eventId, exception, translation.Translate(arg0, arg1, arg2, arg3, forTerminal: true));
    }

    /// <summary>
    /// Log a 4-arg translation using default language and settings as a WARNING log.
    /// </summary>
    public static void LogWarning<T0, T1, T2, T3>(this ILogger logger, Translation<T0, T1, T2, T3> translation, T0 arg0, T1 arg1, T2 arg2, T3 arg3)
    {
        logger.LogWarning(translation.Translate(arg0, arg1, arg2, arg3, forTerminal: true));
    }

    /// <summary>
    /// Log a 4-arg translation using default language and settings as a WARNING log.
    /// </summary>
    public static void LogWarning<T0, T1, T2, T3>(this ILogger logger, Exception exception, Translation<T0, T1, T2, T3> translation, T0 arg0, T1 arg1, T2 arg2, T3 arg3)
    {
        logger.LogWarning(exception, translation.Translate(arg0, arg1, arg2, arg3, forTerminal: true));
    }

    /// <summary>
    /// Log a 4-arg translation using default language and settings as a WARNING log.
    /// </summary>
    public static void LogWarning<T0, T1, T2, T3>(this ILogger logger, EventId eventId, Translation<T0, T1, T2, T3> translation, T0 arg0, T1 arg1, T2 arg2, T3 arg3)
    {
        logger.LogWarning(eventId, translation.Translate(arg0, arg1, arg2, arg3, forTerminal: true));
    }

    /// <summary>
    /// Log a 4-arg translation using default language and settings as a WARNING log.
    /// </summary>
    public static void LogWarning<T0, T1, T2, T3>(this ILogger logger, EventId eventId, Exception exception, Translation<T0, T1, T2, T3> translation, T0 arg0, T1 arg1, T2 arg2, T3 arg3)
    {
        logger.LogWarning(eventId, exception, translation.Translate(arg0, arg1, arg2, arg3, forTerminal: true));
    }

    /// <summary>
    /// Log a 4-arg translation using default language and settings as an ERROR log.
    /// </summary>
    public static void LogError<T0, T1, T2, T3>(this ILogger logger, Translation<T0, T1, T2, T3> translation, T0 arg0, T1 arg1, T2 arg2, T3 arg3)
    {
        logger.LogError(translation.Translate(arg0, arg1, arg2, arg3, forTerminal: true));
    }

    /// <summary>
    /// Log a 4-arg translation using default language and settings as an ERROR log.
    /// </summary>
    public static void LogError<T0, T1, T2, T3>(this ILogger logger, Exception exception, Translation<T0, T1, T2, T3> translation, T0 arg0, T1 arg1, T2 arg2, T3 arg3)
    {
        logger.LogError(exception, translation.Translate(arg0, arg1, arg2, arg3, forTerminal: true));
    }

    /// <summary>
    /// Log a 4-arg translation using default language and settings as an ERROR log.
    /// </summary>
    public static void LogError<T0, T1, T2, T3>(this ILogger logger, EventId eventId, Translation<T0, T1, T2, T3> translation, T0 arg0, T1 arg1, T2 arg2, T3 arg3)
    {
        logger.LogError(eventId, translation.Translate(arg0, arg1, arg2, arg3, forTerminal: true));
    }

    /// <summary>
    /// Log a 4-arg translation using default language and settings as an ERROR log.
    /// </summary>
    public static void LogError<T0, T1, T2, T3>(this ILogger logger, EventId eventId, Exception exception, Translation<T0, T1, T2, T3> translation, T0 arg0, T1 arg1, T2 arg2, T3 arg3)
    {
        logger.LogError(eventId, exception, translation.Translate(arg0, arg1, arg2, arg3, forTerminal: true));
    }

    /// <summary>
    /// Log a 4-arg translation using default language and settings as a CRITICAL log.
    /// </summary>
    public static void LogCritical<T0, T1, T2, T3>(this ILogger logger, Translation<T0, T1, T2, T3> translation, T0 arg0, T1 arg1, T2 arg2, T3 arg3)
    {
        logger.LogCritical(translation.Translate(arg0, arg1, arg2, arg3, forTerminal: true));
    }

    /// <summary>
    /// Log a 4-arg translation using default language and settings as a CRITICAL log.
    /// </summary>
    public static void LogCritical<T0, T1, T2, T3>(this ILogger logger, Exception exception, Translation<T0, T1, T2, T3> translation, T0 arg0, T1 arg1, T2 arg2, T3 arg3)
    {
        logger.LogCritical(exception, translation.Translate(arg0, arg1, arg2, arg3, forTerminal: true));
    }

    /// <summary>
    /// Log a 4-arg translation using default language and settings as a CRITICAL log.
    /// </summary>
    public static void LogCritical<T0, T1, T2, T3>(this ILogger logger, EventId eventId, Translation<T0, T1, T2, T3> translation, T0 arg0, T1 arg1, T2 arg2, T3 arg3)
    {
        logger.LogCritical(eventId, translation.Translate(arg0, arg1, arg2, arg3, forTerminal: true));
    }

    /// <summary>
    /// Log a 4-arg translation using default language and settings as a CRITICAL log.
    /// </summary>
    public static void LogCritical<T0, T1, T2, T3>(this ILogger logger, EventId eventId, Exception exception, Translation<T0, T1, T2, T3> translation, T0 arg0, T1 arg1, T2 arg2, T3 arg3)
    {
        logger.LogCritical(eventId, exception, translation.Translate(arg0, arg1, arg2, arg3, forTerminal: true));
    }

    /// <summary>
    /// Log a 4-arg translation using default language and settings.
    /// </summary>
    public static void Log<T0, T1, T2, T3>(this ILogger logger, LogLevel logLevel, Translation<T0, T1, T2, T3> translation, T0 arg0, T1 arg1, T2 arg2, T3 arg3)
    {
        logger.Log(logLevel, translation.Translate(arg0, arg1, arg2, arg3, forTerminal: true));
    }

    /// <summary>
    /// Log a 4-arg translation using default language and settings.
    /// </summary>
    public static void Log<T0, T1, T2, T3>(this ILogger logger, LogLevel logLevel, Exception exception, Translation<T0, T1, T2, T3> translation, T0 arg0, T1 arg1, T2 arg2, T3 arg3)
    {
        logger.Log(logLevel, exception, translation.Translate(arg0, arg1, arg2, arg3, forTerminal: true));
    }

    /// <summary>
    /// Log a 4-arg translation using default language and settings.
    /// </summary>
    public static void Log<T0, T1, T2, T3>(this ILogger logger, LogLevel logLevel, EventId eventId, Translation<T0, T1, T2, T3> translation, T0 arg0, T1 arg1, T2 arg2, T3 arg3)
    {
        logger.Log(logLevel, eventId, translation.Translate(arg0, arg1, arg2, arg3, forTerminal: true));
    }

    /// <summary>
    /// Log a 4-arg translation using default language and settings.
    /// </summary>
    public static void Log<T0, T1, T2, T3>(this ILogger logger, LogLevel logLevel, EventId eventId, Exception exception, Translation<T0, T1, T2, T3> translation, T0 arg0, T1 arg1, T2 arg2, T3 arg3)
    {
        logger.Log(logLevel, eventId, exception, translation.Translate(arg0, arg1, arg2, arg3, forTerminal: true));
    }
    #endregion

    #region 5-arg
    /// <summary>
    /// Log a 5-arg translation using default language and settings as a DEBUG log.
    /// </summary>
    public static void LogDebug<T0, T1, T2, T3, T4>(this ILogger logger, Translation<T0, T1, T2, T3, T4> translation, T0 arg0, T1 arg1, T2 arg2, T3 arg3, T4 arg4)
    {
        logger.LogDebug(translation.Translate(arg0, arg1, arg2, arg3, arg4, forTerminal: true));
    }

    /// <summary>
    /// Log a 5-arg translation using default language and settings as a DEBUG log.
    /// </summary>
    public static void LogDebug<T0, T1, T2, T3, T4>(this ILogger logger, Exception exception, Translation<T0, T1, T2, T3, T4> translation, T0 arg0, T1 arg1, T2 arg2, T3 arg3, T4 arg4)
    {
        logger.LogDebug(exception, translation.Translate(arg0, arg1, arg2, arg3, arg4, forTerminal: true));
    }

    /// <summary>
    /// Log a 5-arg translation using default language and settings as a DEBUG log.
    /// </summary>
    public static void LogDebug<T0, T1, T2, T3, T4>(this ILogger logger, EventId eventId, Translation<T0, T1, T2, T3, T4> translation, T0 arg0, T1 arg1, T2 arg2, T3 arg3, T4 arg4)
    {
        logger.LogDebug(eventId, translation.Translate(arg0, arg1, arg2, arg3, arg4, forTerminal: true));
    }

    /// <summary>
    /// Log a 5-arg translation using default language and settings as a DEBUG log.
    /// </summary>
    public static void LogDebug<T0, T1, T2, T3, T4>(this ILogger logger, EventId eventId, Exception exception, Translation<T0, T1, T2, T3, T4> translation, T0 arg0, T1 arg1, T2 arg2, T3 arg3, T4 arg4)
    {
        logger.LogDebug(eventId, exception, translation.Translate(arg0, arg1, arg2, arg3, arg4, forTerminal: true));
    }

    /// <summary>
    /// Log a 5-arg translation using default language and settings as an INFORMATION log.
    /// </summary>
    public static void LogTrace<T0, T1, T2, T3, T4>(this ILogger logger, Translation<T0, T1, T2, T3, T4> translation, T0 arg0, T1 arg1, T2 arg2, T3 arg3, T4 arg4)
    {
        logger.LogTrace(translation.Translate(arg0, arg1, arg2, arg3, arg4, forTerminal: true));
    }

    /// <summary>
    /// Log a 5-arg translation using default language and settings as an INFORMATION log.
    /// </summary>
    public static void LogTrace<T0, T1, T2, T3, T4>(this ILogger logger, Exception exception, Translation<T0, T1, T2, T3, T4> translation, T0 arg0, T1 arg1, T2 arg2, T3 arg3, T4 arg4)
    {
        logger.LogTrace(exception, translation.Translate(arg0, arg1, arg2, arg3, arg4, forTerminal: true));
    }

    /// <summary>
    /// Log a 5-arg translation using default language and settings as an INFORMATION log.
    /// </summary>
    public static void LogTrace<T0, T1, T2, T3, T4>(this ILogger logger, EventId eventId, Translation<T0, T1, T2, T3, T4> translation, T0 arg0, T1 arg1, T2 arg2, T3 arg3, T4 arg4)
    {
        logger.LogTrace(eventId, translation.Translate(arg0, arg1, arg2, arg3, arg4, forTerminal: true));
    }

    /// <summary>
    /// Log a 5-arg translation using default language and settings as an INFORMATION log.
    /// </summary>
    public static void LogTrace<T0, T1, T2, T3, T4>(this ILogger logger, EventId eventId, Exception exception, Translation<T0, T1, T2, T3, T4> translation, T0 arg0, T1 arg1, T2 arg2, T3 arg3, T4 arg4)
    {
        logger.LogTrace(eventId, exception, translation.Translate(arg0, arg1, arg2, arg3, arg4, forTerminal: true));
    }

    /// <summary>
    /// Log a 5-arg translation using default language and settings as an INFORMATION log.
    /// </summary>
    public static void LogInformation<T0, T1, T2, T3, T4>(this ILogger logger, Translation<T0, T1, T2, T3, T4> translation, T0 arg0, T1 arg1, T2 arg2, T3 arg3, T4 arg4)
    {
        logger.LogInformation(translation.Translate(arg0, arg1, arg2, arg3, arg4, forTerminal: true));
    }

    /// <summary>
    /// Log a 5-arg translation using default language and settings as an INFORMATION log.
    /// </summary>
    public static void LogInformation<T0, T1, T2, T3, T4>(this ILogger logger, Exception exception, Translation<T0, T1, T2, T3, T4> translation, T0 arg0, T1 arg1, T2 arg2, T3 arg3, T4 arg4)
    {
        logger.LogInformation(exception, translation.Translate(arg0, arg1, arg2, arg3, arg4, forTerminal: true));
    }

    /// <summary>
    /// Log a 5-arg translation using default language and settings as an INFORMATION log.
    /// </summary>
    public static void LogInformation<T0, T1, T2, T3, T4>(this ILogger logger, EventId eventId, Translation<T0, T1, T2, T3, T4> translation, T0 arg0, T1 arg1, T2 arg2, T3 arg3, T4 arg4)
    {
        logger.LogInformation(eventId, translation.Translate(arg0, arg1, arg2, arg3, arg4, forTerminal: true));
    }

    /// <summary>
    /// Log a 5-arg translation using default language and settings as an INFORMATION log.
    /// </summary>
    public static void LogInformation<T0, T1, T2, T3, T4>(this ILogger logger, EventId eventId, Exception exception, Translation<T0, T1, T2, T3, T4> translation, T0 arg0, T1 arg1, T2 arg2, T3 arg3, T4 arg4)
    {
        logger.LogInformation(eventId, exception, translation.Translate(arg0, arg1, arg2, arg3, arg4, forTerminal: true));
    }

    /// <summary>
    /// Log a 5-arg translation using default language and settings as a WARNING log.
    /// </summary>
    public static void LogWarning<T0, T1, T2, T3, T4>(this ILogger logger, Translation<T0, T1, T2, T3, T4> translation, T0 arg0, T1 arg1, T2 arg2, T3 arg3, T4 arg4)
    {
        logger.LogWarning(translation.Translate(arg0, arg1, arg2, arg3, arg4, forTerminal: true));
    }

    /// <summary>
    /// Log a 5-arg translation using default language and settings as a WARNING log.
    /// </summary>
    public static void LogWarning<T0, T1, T2, T3, T4>(this ILogger logger, Exception exception, Translation<T0, T1, T2, T3, T4> translation, T0 arg0, T1 arg1, T2 arg2, T3 arg3, T4 arg4)
    {
        logger.LogWarning(exception, translation.Translate(arg0, arg1, arg2, arg3, arg4, forTerminal: true));
    }

    /// <summary>
    /// Log a 5-arg translation using default language and settings as a WARNING log.
    /// </summary>
    public static void LogWarning<T0, T1, T2, T3, T4>(this ILogger logger, EventId eventId, Translation<T0, T1, T2, T3, T4> translation, T0 arg0, T1 arg1, T2 arg2, T3 arg3, T4 arg4)
    {
        logger.LogWarning(eventId, translation.Translate(arg0, arg1, arg2, arg3, arg4, forTerminal: true));
    }

    /// <summary>
    /// Log a 5-arg translation using default language and settings as a WARNING log.
    /// </summary>
    public static void LogWarning<T0, T1, T2, T3, T4>(this ILogger logger, EventId eventId, Exception exception, Translation<T0, T1, T2, T3, T4> translation, T0 arg0, T1 arg1, T2 arg2, T3 arg3, T4 arg4)
    {
        logger.LogWarning(eventId, exception, translation.Translate(arg0, arg1, arg2, arg3, arg4, forTerminal: true));
    }

    /// <summary>
    /// Log a 5-arg translation using default language and settings as an ERROR log.
    /// </summary>
    public static void LogError<T0, T1, T2, T3, T4>(this ILogger logger, Translation<T0, T1, T2, T3, T4> translation, T0 arg0, T1 arg1, T2 arg2, T3 arg3, T4 arg4)
    {
        logger.LogError(translation.Translate(arg0, arg1, arg2, arg3, arg4, forTerminal: true));
    }

    /// <summary>
    /// Log a 5-arg translation using default language and settings as an ERROR log.
    /// </summary>
    public static void LogError<T0, T1, T2, T3, T4>(this ILogger logger, Exception exception, Translation<T0, T1, T2, T3, T4> translation, T0 arg0, T1 arg1, T2 arg2, T3 arg3, T4 arg4)
    {
        logger.LogError(exception, translation.Translate(arg0, arg1, arg2, arg3, arg4, forTerminal: true));
    }

    /// <summary>
    /// Log a 5-arg translation using default language and settings as an ERROR log.
    /// </summary>
    public static void LogError<T0, T1, T2, T3, T4>(this ILogger logger, EventId eventId, Translation<T0, T1, T2, T3, T4> translation, T0 arg0, T1 arg1, T2 arg2, T3 arg3, T4 arg4)
    {
        logger.LogError(eventId, translation.Translate(arg0, arg1, arg2, arg3, arg4, forTerminal: true));
    }

    /// <summary>
    /// Log a 5-arg translation using default language and settings as an ERROR log.
    /// </summary>
    public static void LogError<T0, T1, T2, T3, T4>(this ILogger logger, EventId eventId, Exception exception, Translation<T0, T1, T2, T3, T4> translation, T0 arg0, T1 arg1, T2 arg2, T3 arg3, T4 arg4)
    {
        logger.LogError(eventId, exception, translation.Translate(arg0, arg1, arg2, arg3, arg4, forTerminal: true));
    }

    /// <summary>
    /// Log a 5-arg translation using default language and settings as a CRITICAL log.
    /// </summary>
    public static void LogCritical<T0, T1, T2, T3, T4>(this ILogger logger, Translation<T0, T1, T2, T3, T4> translation, T0 arg0, T1 arg1, T2 arg2, T3 arg3, T4 arg4)
    {
        logger.LogCritical(translation.Translate(arg0, arg1, arg2, arg3, arg4, forTerminal: true));
    }

    /// <summary>
    /// Log a 5-arg translation using default language and settings as a CRITICAL log.
    /// </summary>
    public static void LogCritical<T0, T1, T2, T3, T4>(this ILogger logger, Exception exception, Translation<T0, T1, T2, T3, T4> translation, T0 arg0, T1 arg1, T2 arg2, T3 arg3, T4 arg4)
    {
        logger.LogCritical(exception, translation.Translate(arg0, arg1, arg2, arg3, arg4, forTerminal: true));
    }

    /// <summary>
    /// Log a 5-arg translation using default language and settings as a CRITICAL log.
    /// </summary>
    public static void LogCritical<T0, T1, T2, T3, T4>(this ILogger logger, EventId eventId, Translation<T0, T1, T2, T3, T4> translation, T0 arg0, T1 arg1, T2 arg2, T3 arg3, T4 arg4)
    {
        logger.LogCritical(eventId, translation.Translate(arg0, arg1, arg2, arg3, arg4, forTerminal: true));
    }

    /// <summary>
    /// Log a 5-arg translation using default language and settings as a CRITICAL log.
    /// </summary>
    public static void LogCritical<T0, T1, T2, T3, T4>(this ILogger logger, EventId eventId, Exception exception, Translation<T0, T1, T2, T3, T4> translation, T0 arg0, T1 arg1, T2 arg2, T3 arg3, T4 arg4)
    {
        logger.LogCritical(eventId, exception, translation.Translate(arg0, arg1, arg2, arg3, arg4, forTerminal: true));
    }

    /// <summary>
    /// Log a 5-arg translation using default language and settings.
    /// </summary>
    public static void Log<T0, T1, T2, T3, T4>(this ILogger logger, LogLevel logLevel, Translation<T0, T1, T2, T3, T4> translation, T0 arg0, T1 arg1, T2 arg2, T3 arg3, T4 arg4)
    {
        logger.Log(logLevel, translation.Translate(arg0, arg1, arg2, arg3, arg4, forTerminal: true));
    }

    /// <summary>
    /// Log a 5-arg translation using default language and settings.
    /// </summary>
    public static void Log<T0, T1, T2, T3, T4>(this ILogger logger, LogLevel logLevel, Exception exception, Translation<T0, T1, T2, T3, T4> translation, T0 arg0, T1 arg1, T2 arg2, T3 arg3, T4 arg4)
    {
        logger.Log(logLevel, exception, translation.Translate(arg0, arg1, arg2, arg3, arg4, forTerminal: true));
    }

    /// <summary>
    /// Log a 5-arg translation using default language and settings.
    /// </summary>
    public static void Log<T0, T1, T2, T3, T4>(this ILogger logger, LogLevel logLevel, EventId eventId, Translation<T0, T1, T2, T3, T4> translation, T0 arg0, T1 arg1, T2 arg2, T3 arg3, T4 arg4)
    {
        logger.Log(logLevel, eventId, translation.Translate(arg0, arg1, arg2, arg3, arg4, forTerminal: true));
    }

    /// <summary>
    /// Log a 5-arg translation using default language and settings.
    /// </summary>
    public static void Log<T0, T1, T2, T3, T4>(this ILogger logger, LogLevel logLevel, EventId eventId, Exception exception, Translation<T0, T1, T2, T3, T4> translation, T0 arg0, T1 arg1, T2 arg2, T3 arg3, T4 arg4)
    {
        logger.Log(logLevel, eventId, exception, translation.Translate(arg0, arg1, arg2, arg3, arg4, forTerminal: true));
    }
    #endregion

    #region 6-arg
    /// <summary>
    /// Log a 6-arg translation using default language and settings as a DEBUG log.
    /// </summary>
    public static void LogDebug<T0, T1, T2, T3, T4, T5>(this ILogger logger, Translation<T0, T1, T2, T3, T4, T5> translation, T0 arg0, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5)
    {
        logger.LogDebug(translation.Translate(arg0, arg1, arg2, arg3, arg4, arg5, forTerminal: true));
    }

    /// <summary>
    /// Log a 6-arg translation using default language and settings as a DEBUG log.
    /// </summary>
    public static void LogDebug<T0, T1, T2, T3, T4, T5>(this ILogger logger, Exception exception, Translation<T0, T1, T2, T3, T4, T5> translation, T0 arg0, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5)
    {
        logger.LogDebug(exception, translation.Translate(arg0, arg1, arg2, arg3, arg4, arg5, forTerminal: true));
    }

    /// <summary>
    /// Log a 6-arg translation using default language and settings as a DEBUG log.
    /// </summary>
    public static void LogDebug<T0, T1, T2, T3, T4, T5>(this ILogger logger, EventId eventId, Translation<T0, T1, T2, T3, T4, T5> translation, T0 arg0, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5)
    {
        logger.LogDebug(eventId, translation.Translate(arg0, arg1, arg2, arg3, arg4, arg5, forTerminal: true));
    }

    /// <summary>
    /// Log a 6-arg translation using default language and settings as a DEBUG log.
    /// </summary>
    public static void LogDebug<T0, T1, T2, T3, T4, T5>(this ILogger logger, EventId eventId, Exception exception, Translation<T0, T1, T2, T3, T4, T5> translation, T0 arg0, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5)
    {
        logger.LogDebug(eventId, exception, translation.Translate(arg0, arg1, arg2, arg3, arg4, arg5, forTerminal: true));
    }

    /// <summary>
    /// Log a 6-arg translation using default language and settings as an INFORMATION log.
    /// </summary>
    public static void LogTrace<T0, T1, T2, T3, T4, T5>(this ILogger logger, Translation<T0, T1, T2, T3, T4, T5> translation, T0 arg0, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5)
    {
        logger.LogTrace(translation.Translate(arg0, arg1, arg2, arg3, arg4, arg5, forTerminal: true));
    }

    /// <summary>
    /// Log a 6-arg translation using default language and settings as an INFORMATION log.
    /// </summary>
    public static void LogTrace<T0, T1, T2, T3, T4, T5>(this ILogger logger, Exception exception, Translation<T0, T1, T2, T3, T4, T5> translation, T0 arg0, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5)
    {
        logger.LogTrace(exception, translation.Translate(arg0, arg1, arg2, arg3, arg4, arg5, forTerminal: true));
    }

    /// <summary>
    /// Log a 6-arg translation using default language and settings as an INFORMATION log.
    /// </summary>
    public static void LogTrace<T0, T1, T2, T3, T4, T5>(this ILogger logger, EventId eventId, Translation<T0, T1, T2, T3, T4, T5> translation, T0 arg0, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5)
    {
        logger.LogTrace(eventId, translation.Translate(arg0, arg1, arg2, arg3, arg4, arg5, forTerminal: true));
    }

    /// <summary>
    /// Log a 6-arg translation using default language and settings as an INFORMATION log.
    /// </summary>
    public static void LogTrace<T0, T1, T2, T3, T4, T5>(this ILogger logger, EventId eventId, Exception exception, Translation<T0, T1, T2, T3, T4, T5> translation, T0 arg0, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5)
    {
        logger.LogTrace(eventId, exception, translation.Translate(arg0, arg1, arg2, arg3, arg4, arg5, forTerminal: true));
    }

    /// <summary>
    /// Log a 6-arg translation using default language and settings as an INFORMATION log.
    /// </summary>
    public static void LogInformation<T0, T1, T2, T3, T4, T5>(this ILogger logger, Translation<T0, T1, T2, T3, T4, T5> translation, T0 arg0, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5)
    {
        logger.LogInformation(translation.Translate(arg0, arg1, arg2, arg3, arg4, arg5, forTerminal: true));
    }

    /// <summary>
    /// Log a 6-arg translation using default language and settings as an INFORMATION log.
    /// </summary>
    public static void LogInformation<T0, T1, T2, T3, T4, T5>(this ILogger logger, Exception exception, Translation<T0, T1, T2, T3, T4, T5> translation, T0 arg0, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5)
    {
        logger.LogInformation(exception, translation.Translate(arg0, arg1, arg2, arg3, arg4, arg5, forTerminal: true));
    }

    /// <summary>
    /// Log a 6-arg translation using default language and settings as an INFORMATION log.
    /// </summary>
    public static void LogInformation<T0, T1, T2, T3, T4, T5>(this ILogger logger, EventId eventId, Translation<T0, T1, T2, T3, T4, T5> translation, T0 arg0, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5)
    {
        logger.LogInformation(eventId, translation.Translate(arg0, arg1, arg2, arg3, arg4, arg5, forTerminal: true));
    }

    /// <summary>
    /// Log a 6-arg translation using default language and settings as an INFORMATION log.
    /// </summary>
    public static void LogInformation<T0, T1, T2, T3, T4, T5>(this ILogger logger, EventId eventId, Exception exception, Translation<T0, T1, T2, T3, T4, T5> translation, T0 arg0, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5)
    {
        logger.LogInformation(eventId, exception, translation.Translate(arg0, arg1, arg2, arg3, arg4, arg5, forTerminal: true));
    }

    /// <summary>
    /// Log a 6-arg translation using default language and settings as a WARNING log.
    /// </summary>
    public static void LogWarning<T0, T1, T2, T3, T4, T5>(this ILogger logger, Translation<T0, T1, T2, T3, T4, T5> translation, T0 arg0, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5)
    {
        logger.LogWarning(translation.Translate(arg0, arg1, arg2, arg3, arg4, arg5, forTerminal: true));
    }

    /// <summary>
    /// Log a 6-arg translation using default language and settings as a WARNING log.
    /// </summary>
    public static void LogWarning<T0, T1, T2, T3, T4, T5>(this ILogger logger, Exception exception, Translation<T0, T1, T2, T3, T4, T5> translation, T0 arg0, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5)
    {
        logger.LogWarning(exception, translation.Translate(arg0, arg1, arg2, arg3, arg4, arg5, forTerminal: true));
    }

    /// <summary>
    /// Log a 6-arg translation using default language and settings as a WARNING log.
    /// </summary>
    public static void LogWarning<T0, T1, T2, T3, T4, T5>(this ILogger logger, EventId eventId, Translation<T0, T1, T2, T3, T4, T5> translation, T0 arg0, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5)
    {
        logger.LogWarning(eventId, translation.Translate(arg0, arg1, arg2, arg3, arg4, arg5, forTerminal: true));
    }

    /// <summary>
    /// Log a 6-arg translation using default language and settings as a WARNING log.
    /// </summary>
    public static void LogWarning<T0, T1, T2, T3, T4, T5>(this ILogger logger, EventId eventId, Exception exception, Translation<T0, T1, T2, T3, T4, T5> translation, T0 arg0, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5)
    {
        logger.LogWarning(eventId, exception, translation.Translate(arg0, arg1, arg2, arg3, arg4, arg5, forTerminal: true));
    }

    /// <summary>
    /// Log a 6-arg translation using default language and settings as an ERROR log.
    /// </summary>
    public static void LogError<T0, T1, T2, T3, T4, T5>(this ILogger logger, Translation<T0, T1, T2, T3, T4, T5> translation, T0 arg0, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5)
    {
        logger.LogError(translation.Translate(arg0, arg1, arg2, arg3, arg4, arg5, forTerminal: true));
    }

    /// <summary>
    /// Log a 6-arg translation using default language and settings as an ERROR log.
    /// </summary>
    public static void LogError<T0, T1, T2, T3, T4, T5>(this ILogger logger, Exception exception, Translation<T0, T1, T2, T3, T4, T5> translation, T0 arg0, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5)
    {
        logger.LogError(exception, translation.Translate(arg0, arg1, arg2, arg3, arg4, arg5, forTerminal: true));
    }

    /// <summary>
    /// Log a 6-arg translation using default language and settings as an ERROR log.
    /// </summary>
    public static void LogError<T0, T1, T2, T3, T4, T5>(this ILogger logger, EventId eventId, Translation<T0, T1, T2, T3, T4, T5> translation, T0 arg0, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5)
    {
        logger.LogError(eventId, translation.Translate(arg0, arg1, arg2, arg3, arg4, arg5, forTerminal: true));
    }

    /// <summary>
    /// Log a 6-arg translation using default language and settings as an ERROR log.
    /// </summary>
    public static void LogError<T0, T1, T2, T3, T4, T5>(this ILogger logger, EventId eventId, Exception exception, Translation<T0, T1, T2, T3, T4, T5> translation, T0 arg0, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5)
    {
        logger.LogError(eventId, exception, translation.Translate(arg0, arg1, arg2, arg3, arg4, arg5, forTerminal: true));
    }

    /// <summary>
    /// Log a 6-arg translation using default language and settings as a CRITICAL log.
    /// </summary>
    public static void LogCritical<T0, T1, T2, T3, T4, T5>(this ILogger logger, Translation<T0, T1, T2, T3, T4, T5> translation, T0 arg0, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5)
    {
        logger.LogCritical(translation.Translate(arg0, arg1, arg2, arg3, arg4, arg5, forTerminal: true));
    }

    /// <summary>
    /// Log a 6-arg translation using default language and settings as a CRITICAL log.
    /// </summary>
    public static void LogCritical<T0, T1, T2, T3, T4, T5>(this ILogger logger, Exception exception, Translation<T0, T1, T2, T3, T4, T5> translation, T0 arg0, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5)
    {
        logger.LogCritical(exception, translation.Translate(arg0, arg1, arg2, arg3, arg4, arg5, forTerminal: true));
    }

    /// <summary>
    /// Log a 6-arg translation using default language and settings as a CRITICAL log.
    /// </summary>
    public static void LogCritical<T0, T1, T2, T3, T4, T5>(this ILogger logger, EventId eventId, Translation<T0, T1, T2, T3, T4, T5> translation, T0 arg0, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5)
    {
        logger.LogCritical(eventId, translation.Translate(arg0, arg1, arg2, arg3, arg4, arg5, forTerminal: true));
    }

    /// <summary>
    /// Log a 6-arg translation using default language and settings as a CRITICAL log.
    /// </summary>
    public static void LogCritical<T0, T1, T2, T3, T4, T5>(this ILogger logger, EventId eventId, Exception exception, Translation<T0, T1, T2, T3, T4, T5> translation, T0 arg0, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5)
    {
        logger.LogCritical(eventId, exception, translation.Translate(arg0, arg1, arg2, arg3, arg4, arg5, forTerminal: true));
    }

    /// <summary>
    /// Log a 6-arg translation using default language and settings.
    /// </summary>
    public static void Log<T0, T1, T2, T3, T4, T5>(this ILogger logger, LogLevel logLevel, Translation<T0, T1, T2, T3, T4, T5> translation, T0 arg0, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5)
    {
        logger.Log(logLevel, translation.Translate(arg0, arg1, arg2, arg3, arg4, arg5, forTerminal: true));
    }

    /// <summary>
    /// Log a 6-arg translation using default language and settings.
    /// </summary>
    public static void Log<T0, T1, T2, T3, T4, T5>(this ILogger logger, LogLevel logLevel, Exception exception, Translation<T0, T1, T2, T3, T4, T5> translation, T0 arg0, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5)
    {
        logger.Log(logLevel, exception, translation.Translate(arg0, arg1, arg2, arg3, arg4, arg5, forTerminal: true));
    }

    /// <summary>
    /// Log a 6-arg translation using default language and settings.
    /// </summary>
    public static void Log<T0, T1, T2, T3, T4, T5>(this ILogger logger, LogLevel logLevel, EventId eventId, Translation<T0, T1, T2, T3, T4, T5> translation, T0 arg0, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5)
    {
        logger.Log(logLevel, eventId, translation.Translate(arg0, arg1, arg2, arg3, arg4, arg5, forTerminal: true));
    }

    /// <summary>
    /// Log a 6-arg translation using default language and settings.
    /// </summary>
    public static void Log<T0, T1, T2, T3, T4, T5>(this ILogger logger, LogLevel logLevel, EventId eventId, Exception exception, Translation<T0, T1, T2, T3, T4, T5> translation, T0 arg0, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5)
    {
        logger.Log(logLevel, eventId, exception, translation.Translate(arg0, arg1, arg2, arg3, arg4, arg5, forTerminal: true));
    }
    #endregion

    #region 7-arg
    /// <summary>
    /// Log a 7-arg translation using default language and settings as a DEBUG log.
    /// </summary>
    public static void LogDebug<T0, T1, T2, T3, T4, T5, T6>(this ILogger logger, Translation<T0, T1, T2, T3, T4, T5, T6> translation, T0 arg0, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6)
    {
        logger.LogDebug(translation.Translate(arg0, arg1, arg2, arg3, arg4, arg5, arg6, forTerminal: true));
    }

    /// <summary>
    /// Log a 7-arg translation using default language and settings as a DEBUG log.
    /// </summary>
    public static void LogDebug<T0, T1, T2, T3, T4, T5, T6>(this ILogger logger, Exception exception, Translation<T0, T1, T2, T3, T4, T5, T6> translation, T0 arg0, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6)
    {
        logger.LogDebug(exception, translation.Translate(arg0, arg1, arg2, arg3, arg4, arg5, arg6, forTerminal: true));
    }

    /// <summary>
    /// Log a 7-arg translation using default language and settings as a DEBUG log.
    /// </summary>
    public static void LogDebug<T0, T1, T2, T3, T4, T5, T6>(this ILogger logger, EventId eventId, Translation<T0, T1, T2, T3, T4, T5, T6> translation, T0 arg0, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6)
    {
        logger.LogDebug(eventId, translation.Translate(arg0, arg1, arg2, arg3, arg4, arg5, arg6, forTerminal: true));
    }

    /// <summary>
    /// Log a 7-arg translation using default language and settings as a DEBUG log.
    /// </summary>
    public static void LogDebug<T0, T1, T2, T3, T4, T5, T6>(this ILogger logger, EventId eventId, Exception exception, Translation<T0, T1, T2, T3, T4, T5, T6> translation, T0 arg0, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6)
    {
        logger.LogDebug(eventId, exception, translation.Translate(arg0, arg1, arg2, arg3, arg4, arg5, arg6, forTerminal: true));
    }

    /// <summary>
    /// Log a 7-arg translation using default language and settings as an INFORMATION log.
    /// </summary>
    public static void LogTrace<T0, T1, T2, T3, T4, T5, T6>(this ILogger logger, Translation<T0, T1, T2, T3, T4, T5, T6> translation, T0 arg0, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6)
    {
        logger.LogTrace(translation.Translate(arg0, arg1, arg2, arg3, arg4, arg5, arg6, forTerminal: true));
    }

    /// <summary>
    /// Log a 7-arg translation using default language and settings as an INFORMATION log.
    /// </summary>
    public static void LogTrace<T0, T1, T2, T3, T4, T5, T6>(this ILogger logger, Exception exception, Translation<T0, T1, T2, T3, T4, T5, T6> translation, T0 arg0, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6)
    {
        logger.LogTrace(exception, translation.Translate(arg0, arg1, arg2, arg3, arg4, arg5, arg6, forTerminal: true));
    }

    /// <summary>
    /// Log a 7-arg translation using default language and settings as an INFORMATION log.
    /// </summary>
    public static void LogTrace<T0, T1, T2, T3, T4, T5, T6>(this ILogger logger, EventId eventId, Translation<T0, T1, T2, T3, T4, T5, T6> translation, T0 arg0, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6)
    {
        logger.LogTrace(eventId, translation.Translate(arg0, arg1, arg2, arg3, arg4, arg5, arg6, forTerminal: true));
    }

    /// <summary>
    /// Log a 7-arg translation using default language and settings as an INFORMATION log.
    /// </summary>
    public static void LogTrace<T0, T1, T2, T3, T4, T5, T6>(this ILogger logger, EventId eventId, Exception exception, Translation<T0, T1, T2, T3, T4, T5, T6> translation, T0 arg0, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6)
    {
        logger.LogTrace(eventId, exception, translation.Translate(arg0, arg1, arg2, arg3, arg4, arg5, arg6, forTerminal: true));
    }

    /// <summary>
    /// Log a 7-arg translation using default language and settings as an INFORMATION log.
    /// </summary>
    public static void LogInformation<T0, T1, T2, T3, T4, T5, T6>(this ILogger logger, Translation<T0, T1, T2, T3, T4, T5, T6> translation, T0 arg0, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6)
    {
        logger.LogInformation(translation.Translate(arg0, arg1, arg2, arg3, arg4, arg5, arg6, forTerminal: true));
    }

    /// <summary>
    /// Log a 7-arg translation using default language and settings as an INFORMATION log.
    /// </summary>
    public static void LogInformation<T0, T1, T2, T3, T4, T5, T6>(this ILogger logger, Exception exception, Translation<T0, T1, T2, T3, T4, T5, T6> translation, T0 arg0, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6)
    {
        logger.LogInformation(exception, translation.Translate(arg0, arg1, arg2, arg3, arg4, arg5, arg6, forTerminal: true));
    }

    /// <summary>
    /// Log a 7-arg translation using default language and settings as an INFORMATION log.
    /// </summary>
    public static void LogInformation<T0, T1, T2, T3, T4, T5, T6>(this ILogger logger, EventId eventId, Translation<T0, T1, T2, T3, T4, T5, T6> translation, T0 arg0, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6)
    {
        logger.LogInformation(eventId, translation.Translate(arg0, arg1, arg2, arg3, arg4, arg5, arg6, forTerminal: true));
    }

    /// <summary>
    /// Log a 7-arg translation using default language and settings as an INFORMATION log.
    /// </summary>
    public static void LogInformation<T0, T1, T2, T3, T4, T5, T6>(this ILogger logger, EventId eventId, Exception exception, Translation<T0, T1, T2, T3, T4, T5, T6> translation, T0 arg0, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6)
    {
        logger.LogInformation(eventId, exception, translation.Translate(arg0, arg1, arg2, arg3, arg4, arg5, arg6, forTerminal: true));
    }

    /// <summary>
    /// Log a 7-arg translation using default language and settings as a WARNING log.
    /// </summary>
    public static void LogWarning<T0, T1, T2, T3, T4, T5, T6>(this ILogger logger, Translation<T0, T1, T2, T3, T4, T5, T6> translation, T0 arg0, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6)
    {
        logger.LogWarning(translation.Translate(arg0, arg1, arg2, arg3, arg4, arg5, arg6, forTerminal: true));
    }

    /// <summary>
    /// Log a 7-arg translation using default language and settings as a WARNING log.
    /// </summary>
    public static void LogWarning<T0, T1, T2, T3, T4, T5, T6>(this ILogger logger, Exception exception, Translation<T0, T1, T2, T3, T4, T5, T6> translation, T0 arg0, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6)
    {
        logger.LogWarning(exception, translation.Translate(arg0, arg1, arg2, arg3, arg4, arg5, arg6, forTerminal: true));
    }

    /// <summary>
    /// Log a 7-arg translation using default language and settings as a WARNING log.
    /// </summary>
    public static void LogWarning<T0, T1, T2, T3, T4, T5, T6>(this ILogger logger, EventId eventId, Translation<T0, T1, T2, T3, T4, T5, T6> translation, T0 arg0, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6)
    {
        logger.LogWarning(eventId, translation.Translate(arg0, arg1, arg2, arg3, arg4, arg5, arg6, forTerminal: true));
    }

    /// <summary>
    /// Log a 7-arg translation using default language and settings as a WARNING log.
    /// </summary>
    public static void LogWarning<T0, T1, T2, T3, T4, T5, T6>(this ILogger logger, EventId eventId, Exception exception, Translation<T0, T1, T2, T3, T4, T5, T6> translation, T0 arg0, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6)
    {
        logger.LogWarning(eventId, exception, translation.Translate(arg0, arg1, arg2, arg3, arg4, arg5, arg6, forTerminal: true));
    }

    /// <summary>
    /// Log a 7-arg translation using default language and settings as an ERROR log.
    /// </summary>
    public static void LogError<T0, T1, T2, T3, T4, T5, T6>(this ILogger logger, Translation<T0, T1, T2, T3, T4, T5, T6> translation, T0 arg0, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6)
    {
        logger.LogError(translation.Translate(arg0, arg1, arg2, arg3, arg4, arg5, arg6, forTerminal: true));
    }

    /// <summary>
    /// Log a 7-arg translation using default language and settings as an ERROR log.
    /// </summary>
    public static void LogError<T0, T1, T2, T3, T4, T5, T6>(this ILogger logger, Exception exception, Translation<T0, T1, T2, T3, T4, T5, T6> translation, T0 arg0, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6)
    {
        logger.LogError(exception, translation.Translate(arg0, arg1, arg2, arg3, arg4, arg5, arg6, forTerminal: true));
    }

    /// <summary>
    /// Log a 7-arg translation using default language and settings as an ERROR log.
    /// </summary>
    public static void LogError<T0, T1, T2, T3, T4, T5, T6>(this ILogger logger, EventId eventId, Translation<T0, T1, T2, T3, T4, T5, T6> translation, T0 arg0, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6)
    {
        logger.LogError(eventId, translation.Translate(arg0, arg1, arg2, arg3, arg4, arg5, arg6, forTerminal: true));
    }

    /// <summary>
    /// Log a 7-arg translation using default language and settings as an ERROR log.
    /// </summary>
    public static void LogError<T0, T1, T2, T3, T4, T5, T6>(this ILogger logger, EventId eventId, Exception exception, Translation<T0, T1, T2, T3, T4, T5, T6> translation, T0 arg0, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6)
    {
        logger.LogError(eventId, exception, translation.Translate(arg0, arg1, arg2, arg3, arg4, arg5, arg6, forTerminal: true));
    }

    /// <summary>
    /// Log a 7-arg translation using default language and settings as a CRITICAL log.
    /// </summary>
    public static void LogCritical<T0, T1, T2, T3, T4, T5, T6>(this ILogger logger, Translation<T0, T1, T2, T3, T4, T5, T6> translation, T0 arg0, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6)
    {
        logger.LogCritical(translation.Translate(arg0, arg1, arg2, arg3, arg4, arg5, arg6, forTerminal: true));
    }

    /// <summary>
    /// Log a 7-arg translation using default language and settings as a CRITICAL log.
    /// </summary>
    public static void LogCritical<T0, T1, T2, T3, T4, T5, T6>(this ILogger logger, Exception exception, Translation<T0, T1, T2, T3, T4, T5, T6> translation, T0 arg0, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6)
    {
        logger.LogCritical(exception, translation.Translate(arg0, arg1, arg2, arg3, arg4, arg5, arg6, forTerminal: true));
    }

    /// <summary>
    /// Log a 7-arg translation using default language and settings as a CRITICAL log.
    /// </summary>
    public static void LogCritical<T0, T1, T2, T3, T4, T5, T6>(this ILogger logger, EventId eventId, Translation<T0, T1, T2, T3, T4, T5, T6> translation, T0 arg0, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6)
    {
        logger.LogCritical(eventId, translation.Translate(arg0, arg1, arg2, arg3, arg4, arg5, arg6, forTerminal: true));
    }

    /// <summary>
    /// Log a 7-arg translation using default language and settings as a CRITICAL log.
    /// </summary>
    public static void LogCritical<T0, T1, T2, T3, T4, T5, T6>(this ILogger logger, EventId eventId, Exception exception, Translation<T0, T1, T2, T3, T4, T5, T6> translation, T0 arg0, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6)
    {
        logger.LogCritical(eventId, exception, translation.Translate(arg0, arg1, arg2, arg3, arg4, arg5, arg6, forTerminal: true));
    }

    /// <summary>
    /// Log a 7-arg translation using default language and settings.
    /// </summary>
    public static void Log<T0, T1, T2, T3, T4, T5, T6>(this ILogger logger, LogLevel logLevel, Translation<T0, T1, T2, T3, T4, T5, T6> translation, T0 arg0, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6)
    {
        logger.Log(logLevel, translation.Translate(arg0, arg1, arg2, arg3, arg4, arg5, arg6, forTerminal: true));
    }

    /// <summary>
    /// Log a 7-arg translation using default language and settings.
    /// </summary>
    public static void Log<T0, T1, T2, T3, T4, T5, T6>(this ILogger logger, LogLevel logLevel, Exception exception, Translation<T0, T1, T2, T3, T4, T5, T6> translation, T0 arg0, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6)
    {
        logger.Log(logLevel, exception, translation.Translate(arg0, arg1, arg2, arg3, arg4, arg5, arg6, forTerminal: true));
    }

    /// <summary>
    /// Log a 7-arg translation using default language and settings.
    /// </summary>
    public static void Log<T0, T1, T2, T3, T4, T5, T6>(this ILogger logger, LogLevel logLevel, EventId eventId, Translation<T0, T1, T2, T3, T4, T5, T6> translation, T0 arg0, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6)
    {
        logger.Log(logLevel, eventId, translation.Translate(arg0, arg1, arg2, arg3, arg4, arg5, arg6, forTerminal: true));
    }

    /// <summary>
    /// Log a 7-arg translation using default language and settings.
    /// </summary>
    public static void Log<T0, T1, T2, T3, T4, T5, T6>(this ILogger logger, LogLevel logLevel, EventId eventId, Exception exception, Translation<T0, T1, T2, T3, T4, T5, T6> translation, T0 arg0, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6)
    {
        logger.Log(logLevel, eventId, exception, translation.Translate(arg0, arg1, arg2, arg3, arg4, arg5, arg6, forTerminal: true));
    }
    #endregion

    #region 8-arg
    /// <summary>
    /// Log a 8-arg translation using default language and settings as a DEBUG log.
    /// </summary>
    public static void LogDebug<T0, T1, T2, T3, T4, T5, T6, T7>(this ILogger logger, Translation<T0, T1, T2, T3, T4, T5, T6, T7> translation, T0 arg0, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6, T7 arg7)
    {
        logger.LogDebug(translation.Translate(arg0, arg1, arg2, arg3, arg4, arg5, arg6, arg7, forTerminal: true));
    }

    /// <summary>
    /// Log a 8-arg translation using default language and settings as a DEBUG log.
    /// </summary>
    public static void LogDebug<T0, T1, T2, T3, T4, T5, T6, T7>(this ILogger logger, Exception exception, Translation<T0, T1, T2, T3, T4, T5, T6, T7> translation, T0 arg0, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6, T7 arg7)
    {
        logger.LogDebug(exception, translation.Translate(arg0, arg1, arg2, arg3, arg4, arg5, arg6, arg7, forTerminal: true));
    }

    /// <summary>
    /// Log a 8-arg translation using default language and settings as a DEBUG log.
    /// </summary>
    public static void LogDebug<T0, T1, T2, T3, T4, T5, T6, T7>(this ILogger logger, EventId eventId, Translation<T0, T1, T2, T3, T4, T5, T6, T7> translation, T0 arg0, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6, T7 arg7)
    {
        logger.LogDebug(eventId, translation.Translate(arg0, arg1, arg2, arg3, arg4, arg5, arg6, arg7, forTerminal: true));
    }

    /// <summary>
    /// Log a 8-arg translation using default language and settings as a DEBUG log.
    /// </summary>
    public static void LogDebug<T0, T1, T2, T3, T4, T5, T6, T7>(this ILogger logger, EventId eventId, Exception exception, Translation<T0, T1, T2, T3, T4, T5, T6, T7> translation, T0 arg0, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6, T7 arg7)
    {
        logger.LogDebug(eventId, exception, translation.Translate(arg0, arg1, arg2, arg3, arg4, arg5, arg6, arg7, forTerminal: true));
    }

    /// <summary>
    /// Log a 8-arg translation using default language and settings as an INFORMATION log.
    /// </summary>
    public static void LogTrace<T0, T1, T2, T3, T4, T5, T6, T7>(this ILogger logger, Translation<T0, T1, T2, T3, T4, T5, T6, T7> translation, T0 arg0, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6, T7 arg7)
    {
        logger.LogTrace(translation.Translate(arg0, arg1, arg2, arg3, arg4, arg5, arg6, arg7, forTerminal: true));
    }

    /// <summary>
    /// Log a 8-arg translation using default language and settings as an INFORMATION log.
    /// </summary>
    public static void LogTrace<T0, T1, T2, T3, T4, T5, T6, T7>(this ILogger logger, Exception exception, Translation<T0, T1, T2, T3, T4, T5, T6, T7> translation, T0 arg0, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6, T7 arg7)
    {
        logger.LogTrace(exception, translation.Translate(arg0, arg1, arg2, arg3, arg4, arg5, arg6, arg7, forTerminal: true));
    }

    /// <summary>
    /// Log a 8-arg translation using default language and settings as an INFORMATION log.
    /// </summary>
    public static void LogTrace<T0, T1, T2, T3, T4, T5, T6, T7>(this ILogger logger, EventId eventId, Translation<T0, T1, T2, T3, T4, T5, T6, T7> translation, T0 arg0, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6, T7 arg7)
    {
        logger.LogTrace(eventId, translation.Translate(arg0, arg1, arg2, arg3, arg4, arg5, arg6, arg7, forTerminal: true));
    }

    /// <summary>
    /// Log a 8-arg translation using default language and settings as an INFORMATION log.
    /// </summary>
    public static void LogTrace<T0, T1, T2, T3, T4, T5, T6, T7>(this ILogger logger, EventId eventId, Exception exception, Translation<T0, T1, T2, T3, T4, T5, T6, T7> translation, T0 arg0, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6, T7 arg7)
    {
        logger.LogTrace(eventId, exception, translation.Translate(arg0, arg1, arg2, arg3, arg4, arg5, arg6, arg7, forTerminal: true));
    }

    /// <summary>
    /// Log a 8-arg translation using default language and settings as an INFORMATION log.
    /// </summary>
    public static void LogInformation<T0, T1, T2, T3, T4, T5, T6, T7>(this ILogger logger, Translation<T0, T1, T2, T3, T4, T5, T6, T7> translation, T0 arg0, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6, T7 arg7)
    {
        logger.LogInformation(translation.Translate(arg0, arg1, arg2, arg3, arg4, arg5, arg6, arg7, forTerminal: true));
    }

    /// <summary>
    /// Log a 8-arg translation using default language and settings as an INFORMATION log.
    /// </summary>
    public static void LogInformation<T0, T1, T2, T3, T4, T5, T6, T7>(this ILogger logger, Exception exception, Translation<T0, T1, T2, T3, T4, T5, T6, T7> translation, T0 arg0, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6, T7 arg7)
    {
        logger.LogInformation(exception, translation.Translate(arg0, arg1, arg2, arg3, arg4, arg5, arg6, arg7, forTerminal: true));
    }

    /// <summary>
    /// Log a 8-arg translation using default language and settings as an INFORMATION log.
    /// </summary>
    public static void LogInformation<T0, T1, T2, T3, T4, T5, T6, T7>(this ILogger logger, EventId eventId, Translation<T0, T1, T2, T3, T4, T5, T6, T7> translation, T0 arg0, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6, T7 arg7)
    {
        logger.LogInformation(eventId, translation.Translate(arg0, arg1, arg2, arg3, arg4, arg5, arg6, arg7, forTerminal: true));
    }

    /// <summary>
    /// Log a 8-arg translation using default language and settings as an INFORMATION log.
    /// </summary>
    public static void LogInformation<T0, T1, T2, T3, T4, T5, T6, T7>(this ILogger logger, EventId eventId, Exception exception, Translation<T0, T1, T2, T3, T4, T5, T6, T7> translation, T0 arg0, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6, T7 arg7)
    {
        logger.LogInformation(eventId, exception, translation.Translate(arg0, arg1, arg2, arg3, arg4, arg5, arg6, arg7, forTerminal: true));
    }

    /// <summary>
    /// Log a 8-arg translation using default language and settings as a WARNING log.
    /// </summary>
    public static void LogWarning<T0, T1, T2, T3, T4, T5, T6, T7>(this ILogger logger, Translation<T0, T1, T2, T3, T4, T5, T6, T7> translation, T0 arg0, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6, T7 arg7)
    {
        logger.LogWarning(translation.Translate(arg0, arg1, arg2, arg3, arg4, arg5, arg6, arg7, forTerminal: true));
    }

    /// <summary>
    /// Log a 8-arg translation using default language and settings as a WARNING log.
    /// </summary>
    public static void LogWarning<T0, T1, T2, T3, T4, T5, T6, T7>(this ILogger logger, Exception exception, Translation<T0, T1, T2, T3, T4, T5, T6, T7> translation, T0 arg0, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6, T7 arg7)
    {
        logger.LogWarning(exception, translation.Translate(arg0, arg1, arg2, arg3, arg4, arg5, arg6, arg7, forTerminal: true));
    }

    /// <summary>
    /// Log a 8-arg translation using default language and settings as a WARNING log.
    /// </summary>
    public static void LogWarning<T0, T1, T2, T3, T4, T5, T6, T7>(this ILogger logger, EventId eventId, Translation<T0, T1, T2, T3, T4, T5, T6, T7> translation, T0 arg0, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6, T7 arg7)
    {
        logger.LogWarning(eventId, translation.Translate(arg0, arg1, arg2, arg3, arg4, arg5, arg6, arg7, forTerminal: true));
    }

    /// <summary>
    /// Log a 8-arg translation using default language and settings as a WARNING log.
    /// </summary>
    public static void LogWarning<T0, T1, T2, T3, T4, T5, T6, T7>(this ILogger logger, EventId eventId, Exception exception, Translation<T0, T1, T2, T3, T4, T5, T6, T7> translation, T0 arg0, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6, T7 arg7)
    {
        logger.LogWarning(eventId, exception, translation.Translate(arg0, arg1, arg2, arg3, arg4, arg5, arg6, arg7, forTerminal: true));
    }

    /// <summary>
    /// Log a 8-arg translation using default language and settings as an ERROR log.
    /// </summary>
    public static void LogError<T0, T1, T2, T3, T4, T5, T6, T7>(this ILogger logger, Translation<T0, T1, T2, T3, T4, T5, T6, T7> translation, T0 arg0, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6, T7 arg7)
    {
        logger.LogError(translation.Translate(arg0, arg1, arg2, arg3, arg4, arg5, arg6, arg7, forTerminal: true));
    }

    /// <summary>
    /// Log a 8-arg translation using default language and settings as an ERROR log.
    /// </summary>
    public static void LogError<T0, T1, T2, T3, T4, T5, T6, T7>(this ILogger logger, Exception exception, Translation<T0, T1, T2, T3, T4, T5, T6, T7> translation, T0 arg0, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6, T7 arg7)
    {
        logger.LogError(exception, translation.Translate(arg0, arg1, arg2, arg3, arg4, arg5, arg6, arg7, forTerminal: true));
    }

    /// <summary>
    /// Log a 8-arg translation using default language and settings as an ERROR log.
    /// </summary>
    public static void LogError<T0, T1, T2, T3, T4, T5, T6, T7>(this ILogger logger, EventId eventId, Translation<T0, T1, T2, T3, T4, T5, T6, T7> translation, T0 arg0, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6, T7 arg7)
    {
        logger.LogError(eventId, translation.Translate(arg0, arg1, arg2, arg3, arg4, arg5, arg6, arg7, forTerminal: true));
    }

    /// <summary>
    /// Log a 8-arg translation using default language and settings as an ERROR log.
    /// </summary>
    public static void LogError<T0, T1, T2, T3, T4, T5, T6, T7>(this ILogger logger, EventId eventId, Exception exception, Translation<T0, T1, T2, T3, T4, T5, T6, T7> translation, T0 arg0, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6, T7 arg7)
    {
        logger.LogError(eventId, exception, translation.Translate(arg0, arg1, arg2, arg3, arg4, arg5, arg6, arg7, forTerminal: true));
    }

    /// <summary>
    /// Log a 8-arg translation using default language and settings as a CRITICAL log.
    /// </summary>
    public static void LogCritical<T0, T1, T2, T3, T4, T5, T6, T7>(this ILogger logger, Translation<T0, T1, T2, T3, T4, T5, T6, T7> translation, T0 arg0, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6, T7 arg7)
    {
        logger.LogCritical(translation.Translate(arg0, arg1, arg2, arg3, arg4, arg5, arg6, arg7, forTerminal: true));
    }

    /// <summary>
    /// Log a 8-arg translation using default language and settings as a CRITICAL log.
    /// </summary>
    public static void LogCritical<T0, T1, T2, T3, T4, T5, T6, T7>(this ILogger logger, Exception exception, Translation<T0, T1, T2, T3, T4, T5, T6, T7> translation, T0 arg0, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6, T7 arg7)
    {
        logger.LogCritical(exception, translation.Translate(arg0, arg1, arg2, arg3, arg4, arg5, arg6, arg7, forTerminal: true));
    }

    /// <summary>
    /// Log a 8-arg translation using default language and settings as a CRITICAL log.
    /// </summary>
    public static void LogCritical<T0, T1, T2, T3, T4, T5, T6, T7>(this ILogger logger, EventId eventId, Translation<T0, T1, T2, T3, T4, T5, T6, T7> translation, T0 arg0, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6, T7 arg7)
    {
        logger.LogCritical(eventId, translation.Translate(arg0, arg1, arg2, arg3, arg4, arg5, arg6, arg7, forTerminal: true));
    }

    /// <summary>
    /// Log a 8-arg translation using default language and settings as a CRITICAL log.
    /// </summary>
    public static void LogCritical<T0, T1, T2, T3, T4, T5, T6, T7>(this ILogger logger, EventId eventId, Exception exception, Translation<T0, T1, T2, T3, T4, T5, T6, T7> translation, T0 arg0, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6, T7 arg7)
    {
        logger.LogCritical(eventId, exception, translation.Translate(arg0, arg1, arg2, arg3, arg4, arg5, arg6, arg7, forTerminal: true));
    }

    /// <summary>
    /// Log a 8-arg translation using default language and settings.
    /// </summary>
    public static void Log<T0, T1, T2, T3, T4, T5, T6, T7>(this ILogger logger, LogLevel logLevel, Translation<T0, T1, T2, T3, T4, T5, T6, T7> translation, T0 arg0, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6, T7 arg7)
    {
        logger.Log(logLevel, translation.Translate(arg0, arg1, arg2, arg3, arg4, arg5, arg6, arg7, forTerminal: true));
    }

    /// <summary>
    /// Log a 8-arg translation using default language and settings.
    /// </summary>
    public static void Log<T0, T1, T2, T3, T4, T5, T6, T7>(this ILogger logger, LogLevel logLevel, Exception exception, Translation<T0, T1, T2, T3, T4, T5, T6, T7> translation, T0 arg0, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6, T7 arg7)
    {
        logger.Log(logLevel, exception, translation.Translate(arg0, arg1, arg2, arg3, arg4, arg5, arg6, arg7, forTerminal: true));
    }

    /// <summary>
    /// Log a 8-arg translation using default language and settings.
    /// </summary>
    public static void Log<T0, T1, T2, T3, T4, T5, T6, T7>(this ILogger logger, LogLevel logLevel, EventId eventId, Translation<T0, T1, T2, T3, T4, T5, T6, T7> translation, T0 arg0, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6, T7 arg7)
    {
        logger.Log(logLevel, eventId, translation.Translate(arg0, arg1, arg2, arg3, arg4, arg5, arg6, arg7, forTerminal: true));
    }

    /// <summary>
    /// Log a 8-arg translation using default language and settings.
    /// </summary>
    public static void Log<T0, T1, T2, T3, T4, T5, T6, T7>(this ILogger logger, LogLevel logLevel, EventId eventId, Exception exception, Translation<T0, T1, T2, T3, T4, T5, T6, T7> translation, T0 arg0, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6, T7 arg7)
    {
        logger.Log(logLevel, eventId, exception, translation.Translate(arg0, arg1, arg2, arg3, arg4, arg5, arg6, arg7, forTerminal: true));
    }
    #endregion

    #region 9-arg
    /// <summary>
    /// Log a 9-arg translation using default language and settings as a DEBUG log.
    /// </summary>
    public static void LogDebug<T0, T1, T2, T3, T4, T5, T6, T7, T8>(this ILogger logger, Translation<T0, T1, T2, T3, T4, T5, T6, T7, T8> translation, T0 arg0, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6, T7 arg7, T8 arg8)
    {
        logger.LogDebug(translation.Translate(arg0, arg1, arg2, arg3, arg4, arg5, arg6, arg7, arg8, forTerminal: true));
    }

    /// <summary>
    /// Log a 9-arg translation using default language and settings as a DEBUG log.
    /// </summary>
    public static void LogDebug<T0, T1, T2, T3, T4, T5, T6, T7, T8>(this ILogger logger, Exception exception, Translation<T0, T1, T2, T3, T4, T5, T6, T7, T8> translation, T0 arg0, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6, T7 arg7, T8 arg8)
    {
        logger.LogDebug(exception, translation.Translate(arg0, arg1, arg2, arg3, arg4, arg5, arg6, arg7, arg8, forTerminal: true));
    }

    /// <summary>
    /// Log a 9-arg translation using default language and settings as a DEBUG log.
    /// </summary>
    public static void LogDebug<T0, T1, T2, T3, T4, T5, T6, T7, T8>(this ILogger logger, EventId eventId, Translation<T0, T1, T2, T3, T4, T5, T6, T7, T8> translation, T0 arg0, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6, T7 arg7, T8 arg8)
    {
        logger.LogDebug(eventId, translation.Translate(arg0, arg1, arg2, arg3, arg4, arg5, arg6, arg7, arg8, forTerminal: true));
    }

    /// <summary>
    /// Log a 9-arg translation using default language and settings as a DEBUG log.
    /// </summary>
    public static void LogDebug<T0, T1, T2, T3, T4, T5, T6, T7, T8>(this ILogger logger, EventId eventId, Exception exception, Translation<T0, T1, T2, T3, T4, T5, T6, T7, T8> translation, T0 arg0, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6, T7 arg7, T8 arg8)
    {
        logger.LogDebug(eventId, exception, translation.Translate(arg0, arg1, arg2, arg3, arg4, arg5, arg6, arg7, arg8, forTerminal: true));
    }

    /// <summary>
    /// Log a 9-arg translation using default language and settings as a TRACE log.
    /// </summary>
    public static void LogTrace<T0, T1, T2, T3, T4, T5, T6, T7, T8>(this ILogger logger, Translation<T0, T1, T2, T3, T4, T5, T6, T7, T8> translation, T0 arg0, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6, T7 arg7, T8 arg8)
    {
        logger.LogTrace(translation.Translate(arg0, arg1, arg2, arg3, arg4, arg5, arg6, arg7, arg8, forTerminal: true));
    }

    /// <summary>
    /// Log a 9-arg translation using default language and settings as a TRACE log.
    /// </summary>
    public static void LogTrace<T0, T1, T2, T3, T4, T5, T6, T7, T8>(this ILogger logger, Exception exception, Translation<T0, T1, T2, T3, T4, T5, T6, T7, T8> translation, T0 arg0, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6, T7 arg7, T8 arg8)
    {
        logger.LogTrace(exception, translation.Translate(arg0, arg1, arg2, arg3, arg4, arg5, arg6, arg7, arg8, forTerminal: true));
    }

    /// <summary>
    /// Log a 9-arg translation using default language and settings as a TRACE log.
    /// </summary>
    public static void LogTrace<T0, T1, T2, T3, T4, T5, T6, T7, T8>(this ILogger logger, EventId eventId, Translation<T0, T1, T2, T3, T4, T5, T6, T7, T8> translation, T0 arg0, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6, T7 arg7, T8 arg8)
    {
        logger.LogTrace(eventId, translation.Translate(arg0, arg1, arg2, arg3, arg4, arg5, arg6, arg7, arg8, forTerminal: true));
    }

    /// <summary>
    /// Log a 9-arg translation using default language and settings as a TRACE log.
    /// </summary>
    public static void LogTrace<T0, T1, T2, T3, T4, T5, T6, T7, T8>(this ILogger logger, EventId eventId, Exception exception, Translation<T0, T1, T2, T3, T4, T5, T6, T7, T8> translation, T0 arg0, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6, T7 arg7, T8 arg8)
    {
        logger.LogTrace(eventId, exception, translation.Translate(arg0, arg1, arg2, arg3, arg4, arg5, arg6, arg7, arg8, forTerminal: true));
    }

    /// <summary>
    /// Log a 9-arg translation using default language and settings as an INFORMATION log.
    /// </summary>
    public static void LogInformation<T0, T1, T2, T3, T4, T5, T6, T7, T8>(this ILogger logger, Translation<T0, T1, T2, T3, T4, T5, T6, T7, T8> translation, T0 arg0, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6, T7 arg7, T8 arg8)
    {
        logger.LogInformation(translation.Translate(arg0, arg1, arg2, arg3, arg4, arg5, arg6, arg7, arg8, forTerminal: true));
    }

    /// <summary>
    /// Log a 9-arg translation using default language and settings as an INFORMATION log.
    /// </summary>
    public static void LogInformation<T0, T1, T2, T3, T4, T5, T6, T7, T8>(this ILogger logger, Exception exception, Translation<T0, T1, T2, T3, T4, T5, T6, T7, T8> translation, T0 arg0, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6, T7 arg7, T8 arg8)
    {
        logger.LogInformation(exception, translation.Translate(arg0, arg1, arg2, arg3, arg4, arg5, arg6, arg7, arg8, forTerminal: true));
    }

    /// <summary>
    /// Log a 9-arg translation using default language and settings as an INFORMATION log.
    /// </summary>
    public static void LogInformation<T0, T1, T2, T3, T4, T5, T6, T7, T8>(this ILogger logger, EventId eventId, Translation<T0, T1, T2, T3, T4, T5, T6, T7, T8> translation, T0 arg0, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6, T7 arg7, T8 arg8)
    {
        logger.LogInformation(eventId, translation.Translate(arg0, arg1, arg2, arg3, arg4, arg5, arg6, arg7, arg8, forTerminal: true));
    }

    /// <summary>
    /// Log a 9-arg translation using default language and settings as an INFORMATION log.
    /// </summary>
    public static void LogInformation<T0, T1, T2, T3, T4, T5, T6, T7, T8>(this ILogger logger, EventId eventId, Exception exception, Translation<T0, T1, T2, T3, T4, T5, T6, T7, T8> translation, T0 arg0, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6, T7 arg7, T8 arg8)
    {
        logger.LogInformation(eventId, exception, translation.Translate(arg0, arg1, arg2, arg3, arg4, arg5, arg6, arg7, arg8, forTerminal: true));
    }

    /// <summary>
    /// Log a 9-arg translation using default language and settings as a WARNING log.
    /// </summary>
    public static void LogWarning<T0, T1, T2, T3, T4, T5, T6, T7, T8>(this ILogger logger, Translation<T0, T1, T2, T3, T4, T5, T6, T7, T8> translation, T0 arg0, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6, T7 arg7, T8 arg8)
    {
        logger.LogWarning(translation.Translate(arg0, arg1, arg2, arg3, arg4, arg5, arg6, arg7, arg8, forTerminal: true));
    }

    /// <summary>
    /// Log a 9-arg translation using default language and settings as a WARNING log.
    /// </summary>
    public static void LogWarning<T0, T1, T2, T3, T4, T5, T6, T7, T8>(this ILogger logger, Exception exception, Translation<T0, T1, T2, T3, T4, T5, T6, T7, T8> translation, T0 arg0, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6, T7 arg7, T8 arg8)
    {
        logger.LogWarning(exception, translation.Translate(arg0, arg1, arg2, arg3, arg4, arg5, arg6, arg7, arg8, forTerminal: true));
    }

    /// <summary>
    /// Log a 9-arg translation using default language and settings as a WARNING log.
    /// </summary>
    public static void LogWarning<T0, T1, T2, T3, T4, T5, T6, T7, T8>(this ILogger logger, EventId eventId, Translation<T0, T1, T2, T3, T4, T5, T6, T7, T8> translation, T0 arg0, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6, T7 arg7, T8 arg8)
    {
        logger.LogWarning(eventId, translation.Translate(arg0, arg1, arg2, arg3, arg4, arg5, arg6, arg7, arg8, forTerminal: true));
    }

    /// <summary>
    /// Log a 9-arg translation using default language and settings as a WARNING log.
    /// </summary>
    public static void LogWarning<T0, T1, T2, T3, T4, T5, T6, T7, T8>(this ILogger logger, EventId eventId, Exception exception, Translation<T0, T1, T2, T3, T4, T5, T6, T7, T8> translation, T0 arg0, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6, T7 arg7, T8 arg8)
    {
        logger.LogWarning(eventId, exception, translation.Translate(arg0, arg1, arg2, arg3, arg4, arg5, arg6, arg7, arg8, forTerminal: true));
    }

    /// <summary>
    /// Log a 9-arg translation using default language and settings as an ERROR log.
    /// </summary>
    public static void LogError<T0, T1, T2, T3, T4, T5, T6, T7, T8>(this ILogger logger, Translation<T0, T1, T2, T3, T4, T5, T6, T7, T8> translation, T0 arg0, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6, T7 arg7, T8 arg8)
    {
        logger.LogError(translation.Translate(arg0, arg1, arg2, arg3, arg4, arg5, arg6, arg7, arg8, forTerminal: true));
    }

    /// <summary>
    /// Log a 9-arg translation using default language and settings as an ERROR log.
    /// </summary>
    public static void LogError<T0, T1, T2, T3, T4, T5, T6, T7, T8>(this ILogger logger, Exception exception, Translation<T0, T1, T2, T3, T4, T5, T6, T7, T8> translation, T0 arg0, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6, T7 arg7, T8 arg8)
    {
        logger.LogError(exception, translation.Translate(arg0, arg1, arg2, arg3, arg4, arg5, arg6, arg7, arg8, forTerminal: true));
    }

    /// <summary>
    /// Log a 9-arg translation using default language and settings as an ERROR log.
    /// </summary>
    public static void LogError<T0, T1, T2, T3, T4, T5, T6, T7, T8>(this ILogger logger, EventId eventId, Translation<T0, T1, T2, T3, T4, T5, T6, T7, T8> translation, T0 arg0, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6, T7 arg7, T8 arg8)
    {
        logger.LogError(eventId, translation.Translate(arg0, arg1, arg2, arg3, arg4, arg5, arg6, arg7, arg8, forTerminal: true));
    }

    /// <summary>
    /// Log a 9-arg translation using default language and settings as an ERROR log.
    /// </summary>
    public static void LogError<T0, T1, T2, T3, T4, T5, T6, T7, T8>(this ILogger logger, EventId eventId, Exception exception, Translation<T0, T1, T2, T3, T4, T5, T6, T7, T8> translation, T0 arg0, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6, T7 arg7, T8 arg8)
    {
        logger.LogError(eventId, exception, translation.Translate(arg0, arg1, arg2, arg3, arg4, arg5, arg6, arg7, arg8, forTerminal: true));
    }

    /// <summary>
    /// Log a 9-arg translation using default language and settings as a CRITICAL log.
    /// </summary>
    public static void LogCritical<T0, T1, T2, T3, T4, T5, T6, T7, T8>(this ILogger logger, Translation<T0, T1, T2, T3, T4, T5, T6, T7, T8> translation, T0 arg0, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6, T7 arg7, T8 arg8)
    {
        logger.LogCritical(translation.Translate(arg0, arg1, arg2, arg3, arg4, arg5, arg6, arg7, arg8, forTerminal: true));
    }

    /// <summary>
    /// Log a 9-arg translation using default language and settings as a CRITICAL log.
    /// </summary>
    public static void LogCritical<T0, T1, T2, T3, T4, T5, T6, T7, T8>(this ILogger logger, Exception exception, Translation<T0, T1, T2, T3, T4, T5, T6, T7, T8> translation, T0 arg0, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6, T7 arg7, T8 arg8)
    {
        logger.LogCritical(exception, translation.Translate(arg0, arg1, arg2, arg3, arg4, arg5, arg6, arg7, arg8, forTerminal: true));
    }

    /// <summary>
    /// Log a 9-arg translation using default language and settings as a CRITICAL log.
    /// </summary>
    public static void LogCritical<T0, T1, T2, T3, T4, T5, T6, T7, T8>(this ILogger logger, EventId eventId, Translation<T0, T1, T2, T3, T4, T5, T6, T7, T8> translation, T0 arg0, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6, T7 arg7, T8 arg8)
    {
        logger.LogCritical(eventId, translation.Translate(arg0, arg1, arg2, arg3, arg4, arg5, arg6, arg7, arg8, forTerminal: true));
    }

    /// <summary>
    /// Log a 9-arg translation using default language and settings as a CRITICAL log.
    /// </summary>
    public static void LogCritical<T0, T1, T2, T3, T4, T5, T6, T7, T8>(this ILogger logger, EventId eventId, Exception exception, Translation<T0, T1, T2, T3, T4, T5, T6, T7, T8> translation, T0 arg0, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6, T7 arg7, T8 arg8)
    {
        logger.LogCritical(eventId, exception, translation.Translate(arg0, arg1, arg2, arg3, arg4, arg5, arg6, arg7, arg8, forTerminal: true));
    }

    /// <summary>
    /// Log a 9-arg translation using default language and settings.
    /// </summary>
    public static void Log<T0, T1, T2, T3, T4, T5, T6, T7, T8>(this ILogger logger, LogLevel logLevel, Translation<T0, T1, T2, T3, T4, T5, T6, T7, T8> translation, T0 arg0, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6, T7 arg7, T8 arg8)
    {
        logger.Log(logLevel, translation.Translate(arg0, arg1, arg2, arg3, arg4, arg5, arg6, arg7, arg8, forTerminal: true));
    }

    /// <summary>
    /// Log a 9-arg translation using default language and settings.
    /// </summary>
    public static void Log<T0, T1, T2, T3, T4, T5, T6, T7, T8>(this ILogger logger, LogLevel logLevel, Exception exception, Translation<T0, T1, T2, T3, T4, T5, T6, T7, T8> translation, T0 arg0, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6, T7 arg7, T8 arg8)
    {
        logger.Log(logLevel, exception, translation.Translate(arg0, arg1, arg2, arg3, arg4, arg5, arg6, arg7, arg8, forTerminal: true));
    }

    /// <summary>
    /// Log a 9-arg translation using default language and settings.
    /// </summary>
    public static void Log<T0, T1, T2, T3, T4, T5, T6, T7, T8>(this ILogger logger, LogLevel logLevel, EventId eventId, Translation<T0, T1, T2, T3, T4, T5, T6, T7, T8> translation, T0 arg0, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6, T7 arg7, T8 arg8)
    {
        logger.Log(logLevel, eventId, translation.Translate(arg0, arg1, arg2, arg3, arg4, arg5, arg6, arg7, arg8, forTerminal: true));
    }

    /// <summary>
    /// Log a 9-arg translation using default language and settings.
    /// </summary>
    public static void Log<T0, T1, T2, T3, T4, T5, T6, T7, T8>(this ILogger logger, LogLevel logLevel, EventId eventId, Exception exception, Translation<T0, T1, T2, T3, T4, T5, T6, T7, T8> translation, T0 arg0, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6, T7 arg7, T8 arg8)
    {
        logger.Log(logLevel, eventId, exception, translation.Translate(arg0, arg1, arg2, arg3, arg4, arg5, arg6, arg7, arg8, forTerminal: true));
    }
    #endregion

    #region 10-arg
    /// <summary>
    /// Log a 10-arg translation using default language and settings as a DEBUG log.
    /// </summary>
    public static void LogDebug<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9>(this ILogger logger, Translation<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9> translation, T0 arg0, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6, T7 arg7, T8 arg8, T9 arg9)
    {
        logger.LogDebug(translation.Translate(arg0, arg1, arg2, arg3, arg4, arg5, arg6, arg7, arg8, arg9, forTerminal: true));
    }

    /// <summary>
    /// Log a 10-arg translation using default language and settings as a DEBUG log.
    /// </summary>
    public static void LogDebug<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9>(this ILogger logger, Exception exception, Translation<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9> translation, T0 arg0, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6, T7 arg7, T8 arg8, T9 arg9)
    {
        logger.LogDebug(exception, translation.Translate(arg0, arg1, arg2, arg3, arg4, arg5, arg6, arg7, arg8, arg9, forTerminal: true));
    }

    /// <summary>
    /// Log a 10-arg translation using default language and settings as a DEBUG log.
    /// </summary>
    public static void LogDebug<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9>(this ILogger logger, EventId eventId, Translation<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9> translation, T0 arg0, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6, T7 arg7, T8 arg8, T9 arg9)
    {
        logger.LogDebug(eventId, translation.Translate(arg0, arg1, arg2, arg3, arg4, arg5, arg6, arg7, arg8, arg9, forTerminal: true));
    }

    /// <summary>
    /// Log a 10-arg translation using default language and settings as a DEBUG log.
    /// </summary>
    public static void LogDebug<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9>(this ILogger logger, EventId eventId, Exception exception, Translation<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9> translation, T0 arg0, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6, T7 arg7, T8 arg8, T9 arg9)
    {
        logger.LogDebug(eventId, exception, translation.Translate(arg0, arg1, arg2, arg3, arg4, arg5, arg6, arg7, arg8, arg9, forTerminal: true));
    }

    /// <summary>
    /// Log a 10-arg translation using default language and settings as a TRACE log.
    /// </summary>
    public static void LogTrace<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9>(this ILogger logger, Translation<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9> translation, T0 arg0, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6, T7 arg7, T8 arg8, T9 arg9)
    {
        logger.LogTrace(translation.Translate(arg0, arg1, arg2, arg3, arg4, arg5, arg6, arg7, arg8, arg9, forTerminal: true));
    }

    /// <summary>
    /// Log a 10-arg translation using default language and settings as a TRACE log.
    /// </summary>
    public static void LogTrace<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9>(this ILogger logger, Exception exception, Translation<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9> translation, T0 arg0, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6, T7 arg7, T8 arg8, T9 arg9)
    {
        logger.LogTrace(exception, translation.Translate(arg0, arg1, arg2, arg3, arg4, arg5, arg6, arg7, arg8, arg9, forTerminal: true));
    }

    /// <summary>
    /// Log a 10-arg translation using default language and settings as a TRACE log.
    /// </summary>
    public static void LogTrace<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9>(this ILogger logger, EventId eventId, Translation<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9> translation, T0 arg0, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6, T7 arg7, T8 arg8, T9 arg9)
    {
        logger.LogTrace(eventId, translation.Translate(arg0, arg1, arg2, arg3, arg4, arg5, arg6, arg7, arg8, arg9, forTerminal: true));
    }

    /// <summary>
    /// Log a 10-arg translation using default language and settings as a TRACE log.
    /// </summary>
    public static void LogTrace<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9>(this ILogger logger, EventId eventId, Exception exception, Translation<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9> translation, T0 arg0, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6, T7 arg7, T8 arg8, T9 arg9)
    {
        logger.LogTrace(eventId, exception, translation.Translate(arg0, arg1, arg2, arg3, arg4, arg5, arg6, arg7, arg8, arg9, forTerminal: true));
    }

    /// <summary>
    /// Log a 10-arg translation using default language and settings as an INFORMATION log.
    /// </summary>
    public static void LogInformation<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9>(this ILogger logger, Translation<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9> translation, T0 arg0, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6, T7 arg7, T8 arg8, T9 arg9)
    {
        logger.LogInformation(translation.Translate(arg0, arg1, arg2, arg3, arg4, arg5, arg6, arg7, arg8, arg9, forTerminal: true));
    }

    /// <summary>
    /// Log a 10-arg translation using default language and settings as an INFORMATION log.
    /// </summary>
    public static void LogInformation<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9>(this ILogger logger, Exception exception, Translation<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9> translation, T0 arg0, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6, T7 arg7, T8 arg8, T9 arg9)
    {
        logger.LogInformation(exception, translation.Translate(arg0, arg1, arg2, arg3, arg4, arg5, arg6, arg7, arg8, arg9, forTerminal: true));
    }

    /// <summary>
    /// Log a 10-arg translation using default language and settings as an INFORMATION log.
    /// </summary>
    public static void LogInformation<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9>(this ILogger logger, EventId eventId, Translation<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9> translation, T0 arg0, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6, T7 arg7, T8 arg8, T9 arg9)
    {
        logger.LogInformation(eventId, translation.Translate(arg0, arg1, arg2, arg3, arg4, arg5, arg6, arg7, arg8, arg9, forTerminal: true));
    }

    /// <summary>
    /// Log a 10-arg translation using default language and settings as an INFORMATION log.
    /// </summary>
    public static void LogInformation<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9>(this ILogger logger, EventId eventId, Exception exception, Translation<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9> translation, T0 arg0, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6, T7 arg7, T8 arg8, T9 arg9)
    {
        logger.LogInformation(eventId, exception, translation.Translate(arg0, arg1, arg2, arg3, arg4, arg5, arg6, arg7, arg8, arg9, forTerminal: true));
    }

    /// <summary>
    /// Log a 10-arg translation using default language and settings as a WARNING log.
    /// </summary>
    public static void LogWarning<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9>(this ILogger logger, Translation<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9> translation, T0 arg0, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6, T7 arg7, T8 arg8, T9 arg9)
    {
        logger.LogWarning(translation.Translate(arg0, arg1, arg2, arg3, arg4, arg5, arg6, arg7, arg8, arg9, forTerminal: true));
    }

    /// <summary>
    /// Log a 10-arg translation using default language and settings as a WARNING log.
    /// </summary>
    public static void LogWarning<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9>(this ILogger logger, Exception exception, Translation<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9> translation, T0 arg0, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6, T7 arg7, T8 arg8, T9 arg9)
    {
        logger.LogWarning(exception, translation.Translate(arg0, arg1, arg2, arg3, arg4, arg5, arg6, arg7, arg8, arg9, forTerminal: true));
    }

    /// <summary>
    /// Log a 10-arg translation using default language and settings as a WARNING log.
    /// </summary>
    public static void LogWarning<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9>(this ILogger logger, EventId eventId, Translation<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9> translation, T0 arg0, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6, T7 arg7, T8 arg8, T9 arg9)
    {
        logger.LogWarning(eventId, translation.Translate(arg0, arg1, arg2, arg3, arg4, arg5, arg6, arg7, arg8, arg9, forTerminal: true));
    }

    /// <summary>
    /// Log a 10-arg translation using default language and settings as a WARNING log.
    /// </summary>
    public static void LogWarning<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9>(this ILogger logger, EventId eventId, Exception exception, Translation<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9> translation, T0 arg0, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6, T7 arg7, T8 arg8, T9 arg9)
    {
        logger.LogWarning(eventId, exception, translation.Translate(arg0, arg1, arg2, arg3, arg4, arg5, arg6, arg7, arg8, arg9, forTerminal: true));
    }

    /// <summary>
    /// Log a 10-arg translation using default language and settings as an ERROR log.
    /// </summary>
    public static void LogError<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9>(this ILogger logger, Translation<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9> translation, T0 arg0, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6, T7 arg7, T8 arg8, T9 arg9)
    {
        logger.LogError(translation.Translate(arg0, arg1, arg2, arg3, arg4, arg5, arg6, arg7, arg8, arg9, forTerminal: true));
    }

    /// <summary>
    /// Log a 10-arg translation using default language and settings as an ERROR log.
    /// </summary>
    public static void LogError<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9>(this ILogger logger, Exception exception, Translation<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9> translation, T0 arg0, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6, T7 arg7, T8 arg8, T9 arg9)
    {
        logger.LogError(exception, translation.Translate(arg0, arg1, arg2, arg3, arg4, arg5, arg6, arg7, arg8, arg9, forTerminal: true));
    }

    /// <summary>
    /// Log a 10-arg translation using default language and settings as an ERROR log.
    /// </summary>
    public static void LogError<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9>(this ILogger logger, EventId eventId, Translation<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9> translation, T0 arg0, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6, T7 arg7, T8 arg8, T9 arg9)
    {
        logger.LogError(eventId, translation.Translate(arg0, arg1, arg2, arg3, arg4, arg5, arg6, arg7, arg8, arg9, forTerminal: true));
    }

    /// <summary>
    /// Log a 10-arg translation using default language and settings as an ERROR log.
    /// </summary>
    public static void LogError<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9>(this ILogger logger, EventId eventId, Exception exception, Translation<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9> translation, T0 arg0, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6, T7 arg7, T8 arg8, T9 arg9)
    {
        logger.LogError(eventId, exception, translation.Translate(arg0, arg1, arg2, arg3, arg4, arg5, arg6, arg7, arg8, arg9, forTerminal: true));
    }

    /// <summary>
    /// Log a 10-arg translation using default language and settings as a CRITICAL log.
    /// </summary>
    public static void LogCritical<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9>(this ILogger logger, Translation<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9> translation, T0 arg0, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6, T7 arg7, T8 arg8, T9 arg9)
    {
        logger.LogCritical(translation.Translate(arg0, arg1, arg2, arg3, arg4, arg5, arg6, arg7, arg8, arg9, forTerminal: true));
    }

    /// <summary>
    /// Log a 10-arg translation using default language and settings as a CRITICAL log.
    /// </summary>
    public static void LogCritical<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9>(this ILogger logger, Exception exception, Translation<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9> translation, T0 arg0, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6, T7 arg7, T8 arg8, T9 arg9)
    {
        logger.LogCritical(exception, translation.Translate(arg0, arg1, arg2, arg3, arg4, arg5, arg6, arg7, arg8, arg9, forTerminal: true));
    }

    /// <summary>
    /// Log a 10-arg translation using default language and settings as a CRITICAL log.
    /// </summary>
    public static void LogCritical<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9>(this ILogger logger, EventId eventId, Translation<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9> translation, T0 arg0, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6, T7 arg7, T8 arg8, T9 arg9)
    {
        logger.LogCritical(eventId, translation.Translate(arg0, arg1, arg2, arg3, arg4, arg5, arg6, arg7, arg8, arg9, forTerminal: true));
    }

    /// <summary>
    /// Log a 10-arg translation using default language and settings as a CRITICAL log.
    /// </summary>
    public static void LogCritical<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9>(this ILogger logger, EventId eventId, Exception exception, Translation<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9> translation, T0 arg0, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6, T7 arg7, T8 arg8, T9 arg9)
    {
        logger.LogCritical(eventId, exception, translation.Translate(arg0, arg1, arg2, arg3, arg4, arg5, arg6, arg7, arg8, arg9, forTerminal: true));
    }

    /// <summary>
    /// Log a 10-arg translation using default language and settings.
    /// </summary>
    public static void Log<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9>(this ILogger logger, LogLevel logLevel, Translation<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9> translation, T0 arg0, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6, T7 arg7, T8 arg8, T9 arg9)
    {
        logger.Log(logLevel, translation.Translate(arg0, arg1, arg2, arg3, arg4, arg5, arg6, arg7, arg8, arg9, forTerminal: true));
    }

    /// <summary>
    /// Log a 10-arg translation using default language and settings.
    /// </summary>
    public static void Log<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9>(this ILogger logger, LogLevel logLevel, Exception exception, Translation<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9> translation, T0 arg0, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6, T7 arg7, T8 arg8, T9 arg9)
    {
        logger.Log(logLevel, exception, translation.Translate(arg0, arg1, arg2, arg3, arg4, arg5, arg6, arg7, arg8, arg9, forTerminal: true));
    }

    /// <summary>
    /// Log a 10-arg translation using default language and settings.
    /// </summary>
    public static void Log<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9>(this ILogger logger, LogLevel logLevel, EventId eventId, Translation<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9> translation, T0 arg0, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6, T7 arg7, T8 arg8, T9 arg9)
    {
        logger.Log(logLevel, eventId, translation.Translate(arg0, arg1, arg2, arg3, arg4, arg5, arg6, arg7, arg8, arg9, forTerminal: true));
    }

    /// <summary>
    /// Log a 10-arg translation using default language and settings.
    /// </summary>
    public static void Log<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9>(this ILogger logger, LogLevel logLevel, EventId eventId, Exception exception, Translation<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9> translation, T0 arg0, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6, T7 arg7, T8 arg8, T9 arg9)
    {
        logger.Log(logLevel, eventId, exception, translation.Translate(arg0, arg1, arg2, arg3, arg4, arg5, arg6, arg7, arg8, arg9, forTerminal: true));
    }
    #endregion
}
