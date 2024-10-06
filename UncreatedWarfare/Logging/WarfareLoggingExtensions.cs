﻿using System;
using Uncreated.Warfare.Logging.Formatting;

// ReSharper disable once CheckNamespace
namespace Uncreated.Warfare;
#if false
// this class is mostly copied from https://github.com/dotnet/extensions/blob/v3.1.0/src/Logging/Logging.Abstractions/src/LoggerExtensions.cs
public static class WarfareLoggingExtensions
{
    //------------------------------------------DEBUG------------------------------------------//

    /// <summary>
    /// Formats and writes a debug log message.
    /// </summary>
    /// <param name="logger">The <see cref="ILogger"/> to write to.</param>
    /// <param name="eventId">The event id associated with the log.</param>
    /// <param name="exception">The exception to log.</param>
    /// <param name="message">Format string of the log message in message template format. Example: <code>"User {User} logged in from {Address}"</code></param>
    /// <param name="args">An object array that contains zero or more objects to format.</param>
    /// <example>logger.LogDebug(0, exception, "Error while processing request from {Address}", address)</example>
    [StringFormatMethod(nameof(message))]
    public static void LogDebug(this ILogger logger, EventId eventId, Exception? exception, string message, params object?[]? args)
    {
        logger.Log(LogLevel.Debug, eventId, exception, message, args);
    }

    /// <summary>
    /// Formats and writes a debug log message.
    /// </summary>
    /// <param name="logger">The <see cref="ILogger"/> to write to.</param>
    /// <param name="eventId">The event id associated with the log.</param>
    /// <param name="message">Format string of the log message in message template format. Example: <code>"User {User} logged in from {Address}"</code></param>
    /// <param name="args">An object array that contains zero or more objects to format.</param>
    /// <example>logger.LogDebug(0, "Processing request from {Address}", address)</example>
    [StringFormatMethod(nameof(message))]
    public static void LogDebug(this ILogger logger, EventId eventId, string message, params object?[]? args)
    {
        logger.Log(LogLevel.Debug, eventId, message, args);
    }

    /// <summary>
    /// Formats and writes a debug log message.
    /// </summary>
    /// <param name="logger">The <see cref="ILogger"/> to write to.</param>
    /// <param name="exception">The exception to log.</param>
    /// <param name="message">Format string of the log message in message template format. Example: <code>"User {User} logged in from {Address}"</code></param>
    /// <param name="args">An object array that contains zero or more objects to format.</param>
    /// <example>logger.LogDebug(exception, "Error while processing request from {Address}", address)</example>
    [StringFormatMethod(nameof(message))]
    public static void LogDebug(this ILogger logger, Exception? exception, string message, params object?[]? args)
    {
        logger.Log(LogLevel.Debug, exception, message, args);
    }

    /// <summary>
    /// Formats and writes a debug log message.
    /// </summary>
    /// <param name="logger">The <see cref="ILogger"/> to write to.</param>
    /// <param name="message">Format string of the log message in message template format. Example: <code>"User {User} logged in from {Address}"</code></param>
    /// <param name="args">An object array that contains zero or more objects to format.</param>
    /// <example>logger.LogDebug("Processing request from {Address}", address)</example>
    [StringFormatMethod(nameof(message))]
    public static void LogDebug(this ILogger logger, string message, params object?[]? args)
    {
        logger.Log(LogLevel.Debug, message, args);
    }

    //------------------------------------------TRACE------------------------------------------//

    /// <summary>
    /// Formats and writes a trace log message.
    /// </summary>
    /// <param name="logger">The <see cref="ILogger"/> to write to.</param>
    /// <param name="eventId">The event id associated with the log.</param>
    /// <param name="exception">The exception to log.</param>
    /// <param name="message">Format string of the log message in message template format. Example: <code>"User {User} logged in from {Address}"</code></param>
    /// <param name="args">An object array that contains zero or more objects to format.</param>
    /// <example>logger.LogTrace(0, exception, "Error while processing request from {Address}", address)</example>
    [StringFormatMethod(nameof(message))]
    public static void LogTrace(this ILogger logger, EventId eventId, Exception? exception, string message, params object?[]? args)
    {
        logger.Log(LogLevel.Trace, eventId, exception, message, args);
    }

    /// <summary>
    /// Formats and writes a trace log message.
    /// </summary>
    /// <param name="logger">The <see cref="ILogger"/> to write to.</param>
    /// <param name="eventId">The event id associated with the log.</param>
    /// <param name="message">Format string of the log message in message template format. Example: <code>"User {User} logged in from {Address}"</code></param>
    /// <param name="args">An object array that contains zero or more objects to format.</param>
    /// <example>logger.LogTrace(0, "Processing request from {Address}", address)</example>
    [StringFormatMethod(nameof(message))]
    public static void LogTrace(this ILogger logger, EventId eventId, string message, params object?[]? args)
    {
        logger.Log(LogLevel.Trace, eventId, message, args);
    }

    /// <summary>
    /// Formats and writes a trace log message.
    /// </summary>
    /// <param name="logger">The <see cref="ILogger"/> to write to.</param>
    /// <param name="exception">The exception to log.</param>
    /// <param name="message">Format string of the log message in message template format. Example: <code>"User {User} logged in from {Address}"</code></param>
    /// <param name="args">An object array that contains zero or more objects to format.</param>
    /// <example>logger.LogTrace(exception, "Error while processing request from {Address}", address)</example>
    [StringFormatMethod(nameof(message))]
    public static void LogTrace(this ILogger logger, Exception? exception, string message, params object?[]? args)
    {
        logger.Log(LogLevel.Trace, exception, message, args);
    }

    /// <summary>
    /// Formats and writes a trace log message.
    /// </summary>
    /// <param name="logger">The <see cref="ILogger"/> to write to.</param>
    /// <param name="message">Format string of the log message in message template format. Example: <code>"User {User} logged in from {Address}"</code></param>
    /// <param name="args">An object array that contains zero or more objects to format.</param>
    /// <example>logger.LogTrace("Processing request from {Address}", address)</example>
    [StringFormatMethod(nameof(message))]
    public static void LogTrace(this ILogger logger, string message, params object?[]? args)
    {
        logger.Log(LogLevel.Trace, message, args);
    }

    //------------------------------------------INFORMATION------------------------------------------//

    /// <summary>
    /// Formats and writes an informational log message.
    /// </summary>
    /// <param name="logger">The <see cref="ILogger"/> to write to.</param>
    /// <param name="eventId">The event id associated with the log.</param>
    /// <param name="exception">The exception to log.</param>
    /// <param name="message">Format string of the log message in message template format. Example: <code>"User {User} logged in from {Address}"</code></param>
    /// <param name="args">An object array that contains zero or more objects to format.</param>
    /// <example>logger.LogInformation(0, exception, "Error while processing request from {Address}", address)</example>
    [StringFormatMethod(nameof(message))]
    public static void LogInformation(this ILogger logger, EventId eventId, Exception? exception, string message, params object?[]? args)
    {
        logger.Log(LogLevel.Information, eventId, exception, message, args);
    }

    /// <summary>
    /// Formats and writes an informational log message.
    /// </summary>
    /// <param name="logger">The <see cref="ILogger"/> to write to.</param>
    /// <param name="eventId">The event id associated with the log.</param>
    /// <param name="message">Format string of the log message in message template format. Example: <code>"User {User} logged in from {Address}"</code></param>
    /// <param name="args">An object array that contains zero or more objects to format.</param>
    /// <example>logger.LogInformation(0, "Processing request from {Address}", address)</example>
    [StringFormatMethod(nameof(message))]
    public static void LogInformation(this ILogger logger, EventId eventId, string message, params object?[]? args)
    {
        logger.Log(LogLevel.Information, eventId, message, args);
    }

    /// <summary>
    /// Formats and writes an informational log message.
    /// </summary>
    /// <param name="logger">The <see cref="ILogger"/> to write to.</param>
    /// <param name="exception">The exception to log.</param>
    /// <param name="message">Format string of the log message in message template format. Example: <code>"User {User} logged in from {Address}"</code></param>
    /// <param name="args">An object array that contains zero or more objects to format.</param>
    /// <example>logger.LogInformation(exception, "Error while processing request from {Address}", address)</example>
    [StringFormatMethod(nameof(message))]
    public static void LogInformation(this ILogger logger, Exception? exception, string message, params object?[]? args)
    {
        logger.Log(LogLevel.Information, exception, message, args);
    }

    /// <summary>
    /// Formats and writes an informational log message.
    /// </summary>
    /// <param name="logger">The <see cref="ILogger"/> to write to.</param>
    /// <param name="message">Format string of the log message in message template format. Example: <code>"User {User} logged in from {Address}"</code></param>
    /// <param name="args">An object array that contains zero or more objects to format.</param>
    /// <example>logger.LogInformation("Processing request from {Address}", address)</example>
    [StringFormatMethod(nameof(message))]
    public static void LogInformation(this ILogger logger, string message, params object?[]? args)
    {
        logger.Log(LogLevel.Information, message, args);
    }

    //------------------------------------------WARNING------------------------------------------//

    /// <summary>
    /// Formats and writes a warning log message.
    /// </summary>
    /// <param name="logger">The <see cref="ILogger"/> to write to.</param>
    /// <param name="eventId">The event id associated with the log.</param>
    /// <param name="exception">The exception to log.</param>
    /// <param name="message">Format string of the log message in message template format. Example: <code>"User {User} logged in from {Address}"</code></param>
    /// <param name="args">An object array that contains zero or more objects to format.</param>
    /// <example>logger.LogWarning(0, exception, "Error while processing request from {Address}", address)</example>
    [StringFormatMethod(nameof(message))]
    public static void LogWarning(this ILogger logger, EventId eventId, Exception? exception, string message, params object?[]? args)
    {
        logger.Log(LogLevel.Warning, eventId, exception, message, args);
    }

    /// <summary>
    /// Formats and writes a warning log message.
    /// </summary>
    /// <param name="logger">The <see cref="ILogger"/> to write to.</param>
    /// <param name="eventId">The event id associated with the log.</param>
    /// <param name="message">Format string of the log message in message template format. Example: <code>"User {User} logged in from {Address}"</code></param>
    /// <param name="args">An object array that contains zero or more objects to format.</param>
    /// <example>logger.LogWarning(0, "Processing request from {Address}", address)</example>
    [StringFormatMethod(nameof(message))]
    public static void LogWarning(this ILogger logger, EventId eventId, string message, params object?[]? args)
    {
        logger.Log(LogLevel.Warning, eventId, message, args);
    }

    /// <summary>
    /// Formats and writes a warning log message.
    /// </summary>
    /// <param name="logger">The <see cref="ILogger"/> to write to.</param>
    /// <param name="exception">The exception to log.</param>
    /// <param name="message">Format string of the log message in message template format. Example: <code>"User {User} logged in from {Address}"</code></param>
    /// <param name="args">An object array that contains zero or more objects to format.</param>
    /// <example>logger.LogWarning(exception, "Error while processing request from {Address}", address)</example>
    [StringFormatMethod(nameof(message))]
    public static void LogWarning(this ILogger logger, Exception? exception, string message, params object?[]? args)
    {
        logger.Log(LogLevel.Warning, exception, message, args);
    }

    /// <summary>
    /// Formats and writes a warning log message.
    /// </summary>
    /// <param name="logger">The <see cref="ILogger"/> to write to.</param>
    /// <param name="message">Format string of the log message in message template format. Example: <code>"User {User} logged in from {Address}"</code></param>
    /// <param name="args">An object array that contains zero or more objects to format.</param>
    /// <example>logger.LogWarning("Processing request from {Address}", address)</example>
    [StringFormatMethod(nameof(message))]
    public static void LogWarning(this ILogger logger, string message, params object?[]? args)
    {
        logger.Log(LogLevel.Warning, message, args);
    }

    //------------------------------------------ERROR------------------------------------------//

    /// <summary>
    /// Formats and writes an error log message.
    /// </summary>
    /// <param name="logger">The <see cref="ILogger"/> to write to.</param>
    /// <param name="eventId">The event id associated with the log.</param>
    /// <param name="exception">The exception to log.</param>
    /// <param name="message">Format string of the log message in message template format. Example: <code>"User {User} logged in from {Address}"</code></param>
    /// <param name="args">An object array that contains zero or more objects to format.</param>
    /// <example>logger.LogError(0, exception, "Error while processing request from {Address}", address)</example>
    [StringFormatMethod(nameof(message))]
    public static void LogError(this ILogger logger, EventId eventId, Exception? exception, string message, params object?[]? args)
    {
        logger.Log(LogLevel.Error, eventId, exception, message, args);
    }

    /// <summary>
    /// Formats and writes an error log message.
    /// </summary>
    /// <param name="logger">The <see cref="ILogger"/> to write to.</param>
    /// <param name="eventId">The event id associated with the log.</param>
    /// <param name="message">Format string of the log message in message template format. Example: <code>"User {User} logged in from {Address}"</code></param>
    /// <param name="args">An object array that contains zero or more objects to format.</param>
    /// <example>logger.LogError(0, "Processing request from {Address}", address)</example>
    [StringFormatMethod(nameof(message))]
    public static void LogError(this ILogger logger, EventId eventId, string message, params object?[]? args)
    {
        logger.Log(LogLevel.Error, eventId, message, args);
    }

    /// <summary>
    /// Formats and writes an error log message.
    /// </summary>
    /// <param name="logger">The <see cref="ILogger"/> to write to.</param>
    /// <param name="exception">The exception to log.</param>
    /// <param name="message">Format string of the log message in message template format. Example: <code>"User {User} logged in from {Address}"</code></param>
    /// <param name="args">An object array that contains zero or more objects to format.</param>
    /// <example>logger.LogError(exception, "Error while processing request from {Address}", address)</example>
    [StringFormatMethod(nameof(message))]
    public static void LogError(this ILogger logger, Exception? exception, string message, params object?[]? args)
    {
        logger.Log(LogLevel.Error, exception, message, args);
    }

    /// <summary>
    /// Formats and writes an error log message.
    /// </summary>
    /// <param name="logger">The <see cref="ILogger"/> to write to.</param>
    /// <param name="message">Format string of the log message in message template format. Example: <code>"User {User} logged in from {Address}"</code></param>
    /// <param name="args">An object array that contains zero or more objects to format.</param>
    /// <example>logger.LogError("Processing request from {Address}", address)</example>
    [StringFormatMethod(nameof(message))]
    public static void LogError(this ILogger logger, string message, params object?[]? args)
    {
        logger.Log(LogLevel.Error, message, args);
    }

    //------------------------------------------CRITICAL------------------------------------------//

    /// <summary>
    /// Formats and writes a critical log message.
    /// </summary>
    /// <param name="logger">The <see cref="ILogger"/> to write to.</param>
    /// <param name="eventId">The event id associated with the log.</param>
    /// <param name="exception">The exception to log.</param>
    /// <param name="message">Format string of the log message in message template format. Example: <code>"User {User} logged in from {Address}"</code></param>
    /// <param name="args">An object array that contains zero or more objects to format.</param>
    /// <example>logger.LogCritical(0, exception, "Error while processing request from {Address}", address)</example>
    [StringFormatMethod(nameof(message))]
    public static void LogCritical(this ILogger logger, EventId eventId, Exception? exception, string message, params object?[]? args)
    {
        logger.Log(LogLevel.Critical, eventId, exception, message, args);
    }

    /// <summary>
    /// Formats and writes a critical log message.
    /// </summary>
    /// <param name="logger">The <see cref="ILogger"/> to write to.</param>
    /// <param name="eventId">The event id associated with the log.</param>
    /// <param name="message">Format string of the log message in message template format. Example: <code>"User {User} logged in from {Address}"</code></param>
    /// <param name="args">An object array that contains zero or more objects to format.</param>
    /// <example>logger.LogCritical(0, "Processing request from {Address}", address)</example>
    [StringFormatMethod(nameof(message))]
    public static void LogCritical(this ILogger logger, EventId eventId, string message, params object?[]? args)
    {
        logger.Log(LogLevel.Critical, eventId, message, args);
    }

    /// <summary>
    /// Formats and writes a critical log message.
    /// </summary>
    /// <param name="logger">The <see cref="ILogger"/> to write to.</param>
    /// <param name="exception">The exception to log.</param>
    /// <param name="message">Format string of the log message in message template format. Example: <code>"User {User} logged in from {Address}"</code></param>
    /// <param name="args">An object array that contains zero or more objects to format.</param>
    /// <example>logger.LogCritical(exception, "Error while processing request from {Address}", address)</example>
    [StringFormatMethod(nameof(message))]
    public static void LogCritical(this ILogger logger, Exception? exception, string message, params object?[]? args)
    {
        logger.Log(LogLevel.Critical, exception, message, args);
    }

    /// <summary>
    /// Formats and writes a critical log message.
    /// </summary>
    /// <param name="logger">The <see cref="ILogger"/> to write to.</param>
    /// <param name="message">Format string of the log message in message template format. Example: <code>"User {User} logged in from {Address}"</code></param>
    /// <param name="args">An object array that contains zero or more objects to format.</param>
    /// <example>logger.LogCritical("Processing request from {Address}", address)</example>
    [StringFormatMethod(nameof(message))]
    public static void LogCritical(this ILogger logger, string message, params object?[]? args)
    {
        logger.Log(LogLevel.Critical, message, args);
    }

    /// <summary>
    /// Formats and writes a log message at the specified log level.
    /// </summary>
    /// <param name="logger">The <see cref="ILogger"/> to write to.</param>
    /// <param name="logLevel">Entry will be written on this level.</param>
    /// <param name="message">Format string of the log message.</param>
    /// <param name="args">An object array that contains zero or more objects to format.</param>
    [StringFormatMethod(nameof(message))]
    public static void Log(this ILogger logger, LogLevel logLevel, string message, params object?[]? args)
    {
        logger.Log(logLevel, 0, null, message, args);
    }

    /// <summary>
    /// Formats and writes a log message at the specified log level.
    /// </summary>
    /// <param name="logger">The <see cref="ILogger"/> to write to.</param>
    /// <param name="logLevel">Entry will be written on this level.</param>
    /// <param name="eventId">The event id associated with the log.</param>
    /// <param name="message">Format string of the log message.</param>
    /// <param name="args">An object array that contains zero or more objects to format.</param>
    [StringFormatMethod(nameof(message))]
    public static void Log(this ILogger logger, LogLevel logLevel, EventId eventId, string message, params object?[]? args)
    {
        logger.Log(logLevel, eventId, null, message, args);
    }

    /// <summary>
    /// Formats and writes a log message at the specified log level.
    /// </summary>
    /// <param name="logger">The <see cref="ILogger"/> to write to.</param>
    /// <param name="logLevel">Entry will be written on this level.</param>
    /// <param name="exception">The exception to log.</param>
    /// <param name="message">Format string of the log message.</param>
    /// <param name="args">An object array that contains zero or more objects to format.</param>
    [StringFormatMethod(nameof(message))]
    public static void Log(this ILogger logger, LogLevel logLevel, Exception? exception, string message, params object?[]? args)
    {
        logger.Log(logLevel, 0, exception, message, args);
    }

    /// <summary>
    /// Formats and writes a log message at the specified log level.
    /// </summary>
    /// <param name="logger">The <see cref="ILogger"/> to write to.</param>
    /// <param name="logLevel">Entry will be written on this level.</param>
    /// <param name="eventId">The event id associated with the log.</param>
    /// <param name="exception">The exception to log.</param>
    /// <param name="message">Format string of the log message.</param>
    /// <param name="args">An object array that contains zero or more objects to format.</param>
    [StringFormatMethod(nameof(message))]
    public static void Log(this ILogger logger, LogLevel logLevel, EventId eventId, Exception? exception, string message, params object?[]? args)
    {
        if (logger == null)
        {
            throw new ArgumentNullException(nameof(logger));
        }

        logger.Log(logLevel, eventId, new WarfareFormattedLogValues(message, args ?? Array.Empty<object?>()), exception, MessageFormatter);
    }

    private static readonly Func<WarfareFormattedLogValues, Exception, string> MessageFormatter = MessageFormatterMtd;
    private static string MessageFormatterMtd(WarfareFormattedLogValues state, Exception error)
    {
        return state.ToString();
    }
}
#endif