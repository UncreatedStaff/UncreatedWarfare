using System;
using System.Diagnostics;
using Uncreated.Warfare;
using Uncreated.Warfare.Logging.Formatting;

// ReSharper disable once CheckNamespace
namespace Uncreated;

// this class is mostly copied and expanded from
// https://github.com/dotnet/extensions/blob/v3.1.0/src/Logging/Logging.Abstractions/src/LoggerExtensions.cs
//  - Microsoft.Extensions.Logging.Abstractions v3.1.0 source
public static class WarfareLoggingExtensions
{
    //------------------------------------------DEBUG------------------------------------------//

    /// <summary>
    /// Formats and writes a debug log message.
    /// </summary>
    /// <param name="logger">The <see cref="ILogger"/> to write to.</param>
    /// <param name="eventId">The event id associated with the log.</param>
    /// <param name="exception">The exception to log.</param>
    /// <param name="message">Format string of the log message in message template format. Example: <code>"User {0} logged in from {1}"</code></param>
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
    /// <param name="message">Format string of the log message in message template format. Example: <code>"User {0} logged in from {1}"</code></param>
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
    /// <param name="message">Format string of the log message in message template format. Example: <code>"User {0} logged in from {1}"</code></param>
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
    /// <param name="message">Format string of the log message in message template format. Example: <code>"User {0} logged in from {1}"</code></param>
    /// <param name="args">An object array that contains zero or more objects to format.</param>
    /// <example>logger.LogDebug("Processing request from {Address}", address)</example>
    [StringFormatMethod(nameof(message))]
    public static void LogDebug(this ILogger logger, string message, params object?[]? args)
    {
        logger.Log(LogLevel.Debug, message, args);
    }

    /// <summary>
    /// Formats and writes a debug log message.
    /// </summary>
    /// <param name="logger">The <see cref="ILogger"/> to write to.</param>
    /// <param name="eventId">The event id associated with the log.</param>
    /// <param name="exception">The exception to log.</param>
    /// <param name="message">Format string of the log message in message template format. Example: <code>"User {0} logged in from {1}"</code></param>
    /// <example>logger.LogDebug(0, exception, "Error while processing request from {Address}", address)</example>
    [StringFormatMethod(nameof(message))]
    public static void LogDebug(this ILogger logger, EventId eventId, Exception? exception, string message)
    {
        logger.Log(LogLevel.Debug, eventId, exception, message, Array.Empty<object>());
    }

    /// <summary>
    /// Formats and writes a debug log message.
    /// </summary>
    /// <param name="logger">The <see cref="ILogger"/> to write to.</param>
    /// <param name="eventId">The event id associated with the log.</param>
    /// <param name="message">Format string of the log message in message template format. Example: <code>"User {0} logged in from {1}"</code></param>
    /// <example>logger.LogDebug(0, "Processing request from {Address}", address)</example>
    [StringFormatMethod(nameof(message))]
    public static void LogDebug(this ILogger logger, EventId eventId, string message)
    {
        logger.Log(LogLevel.Debug, eventId, message, Array.Empty<object>());
    }

    /// <summary>
    /// Formats and writes a debug log message.
    /// </summary>
    /// <param name="logger">The <see cref="ILogger"/> to write to.</param>
    /// <param name="exception">The exception to log.</param>
    /// <param name="message">Format string of the log message in message template format. Example: <code>"User {0} logged in from {1}"</code></param>
    /// <example>logger.LogDebug(exception, "Error while processing request from {Address}", address)</example>
    [StringFormatMethod(nameof(message))]
    public static void LogDebug(this ILogger logger, Exception? exception, string message)
    {
        logger.Log(LogLevel.Debug, exception, message, Array.Empty<object>());
    }

    /// <summary>
    /// Formats and writes a debug log message.
    /// </summary>
    /// <param name="logger">The <see cref="ILogger"/> to write to.</param>
    /// <param name="message">Format string of the log message in message template format. Example: <code>"User {0} logged in from {1}"</code></param>
    /// <example>logger.LogDebug("Processing request from {Address}", address)</example>
    [StringFormatMethod(nameof(message))]
    public static void LogDebug(this ILogger logger, string message)
    {
        logger.Log(LogLevel.Debug, message, Array.Empty<object>());
    }

    /// <summary>
    /// Formats and writes a debug log message.
    /// </summary>
    /// <param name="logger">The <see cref="ILogger"/> to write to.</param>
    /// <param name="eventId">The event id associated with the log.</param>
    /// <param name="exception">The exception to log.</param>
    /// <param name="message">Format string of the log message in message template format. Example: <code>"User {0} logged in from {1}"</code></param>
    /// <example>logger.LogDebug(0, exception, "Error while processing request from {Address}", address)</example>
    [StringFormatMethod(nameof(message))]
    public static void LogDebug(this ILogger logger, EventId eventId, Exception? exception, string message, object? arg1)
    {
        logger.Log(LogLevel.Debug, eventId, exception, message, arg1);
    }

    /// <summary>
    /// Formats and writes a debug log message.
    /// </summary>
    /// <param name="logger">The <see cref="ILogger"/> to write to.</param>
    /// <param name="eventId">The event id associated with the log.</param>
    /// <param name="message">Format string of the log message in message template format. Example: <code>"User {0} logged in from {1}"</code></param>
    /// <example>logger.LogDebug(0, "Processing request from {Address}", address)</example>
    [StringFormatMethod(nameof(message))]
    public static void LogDebug(this ILogger logger, EventId eventId, string message, object? arg1)
    {
        logger.Log(LogLevel.Debug, eventId, message, arg1);
    }

    /// <summary>
    /// Formats and writes a debug log message.
    /// </summary>
    /// <param name="logger">The <see cref="ILogger"/> to write to.</param>
    /// <param name="exception">The exception to log.</param>
    /// <param name="message">Format string of the log message in message template format. Example: <code>"User {0} logged in from {1}"</code></param>
    /// <example>logger.LogDebug(exception, "Error while processing request from {Address}", address)</example>
    [StringFormatMethod(nameof(message))]
    public static void LogDebug(this ILogger logger, Exception? exception, string message, object? arg1)
    {
        logger.Log(LogLevel.Debug, exception, message, arg1);
    }

    /// <summary>
    /// Formats and writes a debug log message.
    /// </summary>
    /// <param name="logger">The <see cref="ILogger"/> to write to.</param>
    /// <param name="message">Format string of the log message in message template format. Example: <code>"User {0} logged in from {1}"</code></param>
    /// <example>logger.LogDebug("Processing request from {Address}", address)</example>
    [StringFormatMethod(nameof(message))]
    public static void LogDebug(this ILogger logger, string message, object? arg1)
    {
        logger.Log(LogLevel.Debug, message, arg1);
    }

    /// <summary>
    /// Formats and writes a debug log message.
    /// </summary>
    /// <param name="logger">The <see cref="ILogger"/> to write to.</param>
    /// <param name="eventId">The event id associated with the log.</param>
    /// <param name="exception">The exception to log.</param>
    /// <param name="message">Format string of the log message in message template format. Example: <code>"User {0} logged in from {1}"</code></param>
    /// <example>logger.LogDebug(0, exception, "Error while processing request from {Address}", address)</example>
    [StringFormatMethod(nameof(message))]
    public static void LogDebug(this ILogger logger, EventId eventId, Exception? exception, string message, object? arg1, object? arg2)
    {
        logger.Log(LogLevel.Debug, eventId, exception, message, arg1, arg2);
    }

    /// <summary>
    /// Formats and writes a debug log message.
    /// </summary>
    /// <param name="logger">The <see cref="ILogger"/> to write to.</param>
    /// <param name="eventId">The event id associated with the log.</param>
    /// <param name="message">Format string of the log message in message template format. Example: <code>"User {0} logged in from {1}"</code></param>
    /// <example>logger.LogDebug(0, "Processing request from {Address}", address)</example>
    [StringFormatMethod(nameof(message))]
    public static void LogDebug(this ILogger logger, EventId eventId, string message, object? arg1, object? arg2)
    {
        logger.Log(LogLevel.Debug, eventId, message, arg1, arg2);
    }

    /// <summary>
    /// Formats and writes a debug log message.
    /// </summary>
    /// <param name="logger">The <see cref="ILogger"/> to write to.</param>
    /// <param name="exception">The exception to log.</param>
    /// <param name="message">Format string of the log message in message template format. Example: <code>"User {0} logged in from {1}"</code></param>
    /// <example>logger.LogDebug(exception, "Error while processing request from {Address}", address)</example>
    [StringFormatMethod(nameof(message))]
    public static void LogDebug(this ILogger logger, Exception? exception, string message, object? arg1, object? arg2)
    {
        logger.Log(LogLevel.Debug, exception, message, arg1, arg2);
    }

    /// <summary>
    /// Formats and writes a debug log message.
    /// </summary>
    /// <param name="logger">The <see cref="ILogger"/> to write to.</param>
    /// <param name="message">Format string of the log message in message template format. Example: <code>"User {0} logged in from {1}"</code></param>
    /// <example>logger.LogDebug("Processing request from {Address}", address)</example>
    [StringFormatMethod(nameof(message))]
    public static void LogDebug(this ILogger logger, string message, object? arg1, object? arg2)
    {
        logger.Log(LogLevel.Debug, message, arg1, arg2);
    }

    /// <summary>
    /// Formats and writes a debug log message.
    /// </summary>
    /// <param name="logger">The <see cref="ILogger"/> to write to.</param>
    /// <param name="eventId">The event id associated with the log.</param>
    /// <param name="exception">The exception to log.</param>
    /// <param name="message">Format string of the log message in message template format. Example: <code>"User {0} logged in from {1}"</code></param>
    /// <example>logger.LogDebug(0, exception, "Error while processing request from {Address}", address)</example>
    [StringFormatMethod(nameof(message))]
    public static void LogDebug(this ILogger logger, EventId eventId, Exception? exception, string message, object? arg1, object? arg2, object? arg3)
    {
        logger.Log(LogLevel.Debug, eventId, exception, message, arg1, arg2, arg3);
    }

    /// <summary>
    /// Formats and writes a debug log message.
    /// </summary>
    /// <param name="logger">The <see cref="ILogger"/> to write to.</param>
    /// <param name="eventId">The event id associated with the log.</param>
    /// <param name="message">Format string of the log message in message template format. Example: <code>"User {0} logged in from {1}"</code></param>
    /// <example>logger.LogDebug(0, "Processing request from {Address}", address)</example>
    [StringFormatMethod(nameof(message))]
    public static void LogDebug(this ILogger logger, EventId eventId, string message, object? arg1, object? arg2, object? arg3)
    {
        logger.Log(LogLevel.Debug, eventId, message, arg1, arg2, arg3);
    }

    /// <summary>
    /// Formats and writes a debug log message.
    /// </summary>
    /// <param name="logger">The <see cref="ILogger"/> to write to.</param>
    /// <param name="exception">The exception to log.</param>
    /// <param name="message">Format string of the log message in message template format. Example: <code>"User {0} logged in from {1}"</code></param>
    /// <example>logger.LogDebug(exception, "Error while processing request from {Address}", address)</example>
    [StringFormatMethod(nameof(message))]
    public static void LogDebug(this ILogger logger, Exception? exception, string message, object? arg1, object? arg2, object? arg3)
    {
        logger.Log(LogLevel.Debug, exception, message, arg1, arg2, arg3);
    }

    /// <summary>
    /// Formats and writes a debug log message.
    /// </summary>
    /// <param name="logger">The <see cref="ILogger"/> to write to.</param>
    /// <param name="message">Format string of the log message in message template format. Example: <code>"User {0} logged in from {1}"</code></param>
    /// <example>logger.LogDebug("Processing request from {Address}", address)</example>
    [StringFormatMethod(nameof(message))]
    public static void LogDebug(this ILogger logger, string message, object? arg1, object? arg2, object? arg3)
    {
        logger.Log(LogLevel.Debug, message, arg1, arg2, arg3);
    }

    /// <summary>
    /// Formats and writes a debug log message.
    /// </summary>
    /// <param name="logger">The <see cref="ILogger"/> to write to.</param>
    /// <param name="eventId">The event id associated with the log.</param>
    /// <param name="exception">The exception to log.</param>
    /// <param name="message">Format string of the log message in message template format. Example: <code>"User {0} logged in from {1}"</code></param>
    /// <example>logger.LogDebug(0, exception, "Error while processing request from {Address}", address)</example>
    [StringFormatMethod(nameof(message))]
    public static void LogDebug(this ILogger logger, EventId eventId, Exception? exception, string message, object? arg1, object? arg2, object? arg3, object? arg4)
    {
        logger.Log(LogLevel.Debug, eventId, exception, message, arg1, arg2, arg3, arg4);
    }

    /// <summary>
    /// Formats and writes a debug log message.
    /// </summary>
    /// <param name="logger">The <see cref="ILogger"/> to write to.</param>
    /// <param name="eventId">The event id associated with the log.</param>
    /// <param name="message">Format string of the log message in message template format. Example: <code>"User {0} logged in from {1}"</code></param>
    /// <example>logger.LogDebug(0, "Processing request from {Address}", address)</example>
    [StringFormatMethod(nameof(message))]
    public static void LogDebug(this ILogger logger, EventId eventId, string message, object? arg1, object? arg2, object? arg3, object? arg4)
    {
        logger.Log(LogLevel.Debug, eventId, message, arg1, arg2, arg3, arg4);
    }

    /// <summary>
    /// Formats and writes a debug log message.
    /// </summary>
    /// <param name="logger">The <see cref="ILogger"/> to write to.</param>
    /// <param name="exception">The exception to log.</param>
    /// <param name="message">Format string of the log message in message template format. Example: <code>"User {0} logged in from {1}"</code></param>
    /// <example>logger.LogDebug(exception, "Error while processing request from {Address}", address)</example>
    [StringFormatMethod(nameof(message))]
    public static void LogDebug(this ILogger logger, Exception? exception, string message, object? arg1, object? arg2, object? arg3, object? arg4)
    {
        logger.Log(LogLevel.Debug, exception, message, arg1, arg2, arg3, arg4);
    }

    /// <summary>
    /// Formats and writes a debug log message.
    /// </summary>
    /// <param name="logger">The <see cref="ILogger"/> to write to.</param>
    /// <param name="message">Format string of the log message in message template format. Example: <code>"User {0} logged in from {1}"</code></param>
    /// <example>logger.LogDebug("Processing request from {Address}", address)</example>
    [StringFormatMethod(nameof(message))]
    public static void LogDebug(this ILogger logger, string message, object? arg1, object? arg2, object? arg3, object? arg4)
    {
        logger.Log(LogLevel.Debug, message, arg1, arg2, arg3, arg4);
    }

    //------------------------------------------DEBUG------------------------------------------//
    // conditional debug

    /// <summary>
    /// Formats and writes a debug log message.
    /// </summary>
    /// <param name="logger">The <see cref="ILogger"/> to write to.</param>
    /// <param name="eventId">The event id associated with the log.</param>
    /// <param name="exception">The exception to log.</param>
    /// <param name="message">Format string of the log message in message template format. Example: <code>"User {0} logged in from {1}"</code></param>
    /// <param name="args">An object array that contains zero or more objects to format.</param>
    /// <example>logger.LogDebug(0, exception, "Error while processing request from {Address}", address)</example>
    [StringFormatMethod(nameof(message))]
    [Conditional("DEBUG")]
    public static void LogConditional(this ILogger logger, EventId eventId, Exception? exception, string message, params object?[]? args)
    {
        logger.Log(LogLevel.Debug, eventId, exception, message, args);
    }

    /// <summary>
    /// Formats and writes a debug log message.
    /// </summary>
    /// <param name="logger">The <see cref="ILogger"/> to write to.</param>
    /// <param name="eventId">The event id associated with the log.</param>
    /// <param name="message">Format string of the log message in message template format. Example: <code>"User {0} logged in from {1}"</code></param>
    /// <param name="args">An object array that contains zero or more objects to format.</param>
    /// <example>logger.LogConditional(0, "Processing request from {Address}", address)</example>
    [StringFormatMethod(nameof(message))]
    [Conditional("DEBUG")]
    public static void LogConditional(this ILogger logger, EventId eventId, string message, params object?[]? args)
    {
        logger.Log(LogLevel.Debug, eventId, message, args);
    }

    /// <summary>
    /// Formats and writes a debug log message.
    /// </summary>
    /// <param name="logger">The <see cref="ILogger"/> to write to.</param>
    /// <param name="exception">The exception to log.</param>
    /// <param name="message">Format string of the log message in message template format. Example: <code>"User {0} logged in from {1}"</code></param>
    /// <param name="args">An object array that contains zero or more objects to format.</param>
    /// <example>logger.LogConditional(exception, "Error while processing request from {Address}", address)</example>
    [StringFormatMethod(nameof(message))]
    [Conditional("DEBUG")]
    public static void LogConditional(this ILogger logger, Exception? exception, string message, params object?[]? args)
    {
        logger.Log(LogLevel.Debug, exception, message, args);
    }

    /// <summary>
    /// Formats and writes a debug log message.
    /// </summary>
    /// <param name="logger">The <see cref="ILogger"/> to write to.</param>
    /// <param name="message">Format string of the log message in message template format. Example: <code>"User {0} logged in from {1}"</code></param>
    /// <param name="args">An object array that contains zero or more objects to format.</param>
    /// <example>logger.LogConditional("Processing request from {Address}", address)</example>
    [StringFormatMethod(nameof(message))]
    [Conditional("DEBUG")]
    public static void LogConditional(this ILogger logger, string message, params object?[]? args)
    {
        logger.Log(LogLevel.Debug, message, args);
    }

    /// <summary>
    /// Formats and writes a debug log message.
    /// </summary>
    /// <param name="logger">The <see cref="ILogger"/> to write to.</param>
    /// <param name="eventId">The event id associated with the log.</param>
    /// <param name="exception">The exception to log.</param>
    /// <param name="message">Format string of the log message in message template format. Example: <code>"User {0} logged in from {1}"</code></param>
    /// <example>logger.LogConditional(0, exception, "Error while processing request from {Address}", address)</example>
    [StringFormatMethod(nameof(message))]
    [Conditional("DEBUG")]
    public static void LogConditional(this ILogger logger, EventId eventId, Exception? exception, string message)
    {
        logger.Log(LogLevel.Debug, eventId, exception, message, Array.Empty<object>());
    }

    /// <summary>
    /// Formats and writes a debug log message.
    /// </summary>
    /// <param name="logger">The <see cref="ILogger"/> to write to.</param>
    /// <param name="eventId">The event id associated with the log.</param>
    /// <param name="message">Format string of the log message in message template format. Example: <code>"User {0} logged in from {1}"</code></param>
    /// <example>logger.LogConditional(0, "Processing request from {Address}", address)</example>
    [StringFormatMethod(nameof(message))]
    [Conditional("DEBUG")]
    public static void LogConditional(this ILogger logger, EventId eventId, string message)
    {
        logger.Log(LogLevel.Debug, eventId, message, Array.Empty<object>());
    }

    /// <summary>
    /// Formats and writes a debug log message.
    /// </summary>
    /// <param name="logger">The <see cref="ILogger"/> to write to.</param>
    /// <param name="exception">The exception to log.</param>
    /// <param name="message">Format string of the log message in message template format. Example: <code>"User {0} logged in from {1}"</code></param>
    /// <example>logger.LogConditional(exception, "Error while processing request from {Address}", address)</example>
    [StringFormatMethod(nameof(message))]
    [Conditional("DEBUG")]
    public static void LogConditional(this ILogger logger, Exception? exception, string message)
    {
        logger.Log(LogLevel.Debug, exception, message, Array.Empty<object>());
    }

    /// <summary>
    /// Formats and writes a debug log message.
    /// </summary>
    /// <param name="logger">The <see cref="ILogger"/> to write to.</param>
    /// <param name="message">Format string of the log message in message template format. Example: <code>"User {0} logged in from {1}"</code></param>
    /// <example>logger.LogConditional("Processing request from {Address}", address)</example>
    [StringFormatMethod(nameof(message))]
    [Conditional("DEBUG")]
    public static void LogConditional(this ILogger logger, string message)
    {
        logger.Log(LogLevel.Debug, message, Array.Empty<object>());
    }

    /// <summary>
    /// Formats and writes a debug log message.
    /// </summary>
    /// <param name="logger">The <see cref="ILogger"/> to write to.</param>
    /// <param name="eventId">The event id associated with the log.</param>
    /// <param name="exception">The exception to log.</param>
    /// <param name="message">Format string of the log message in message template format. Example: <code>"User {0} logged in from {1}"</code></param>
    /// <example>logger.LogConditional(0, exception, "Error while processing request from {Address}", address)</example>
    [StringFormatMethod(nameof(message))]
    [Conditional("DEBUG")]
    public static void LogConditional(this ILogger logger, EventId eventId, Exception? exception, string message, object? arg1)
    {
        logger.Log(LogLevel.Debug, eventId, exception, message, arg1);
    }

    /// <summary>
    /// Formats and writes a debug log message.
    /// </summary>
    /// <param name="logger">The <see cref="ILogger"/> to write to.</param>
    /// <param name="eventId">The event id associated with the log.</param>
    /// <param name="message">Format string of the log message in message template format. Example: <code>"User {0} logged in from {1}"</code></param>
    /// <example>logger.LogConditional(0, "Processing request from {Address}", address)</example>
    [StringFormatMethod(nameof(message))]
    [Conditional("DEBUG")]
    public static void LogConditional(this ILogger logger, EventId eventId, string message, object? arg1)
    {
        logger.Log(LogLevel.Debug, eventId, message, arg1);
    }

    /// <summary>
    /// Formats and writes a debug log message.
    /// </summary>
    /// <param name="logger">The <see cref="ILogger"/> to write to.</param>
    /// <param name="exception">The exception to log.</param>
    /// <param name="message">Format string of the log message in message template format. Example: <code>"User {0} logged in from {1}"</code></param>
    /// <example>logger.LogConditional(exception, "Error while processing request from {Address}", address)</example>
    [StringFormatMethod(nameof(message))]
    [Conditional("DEBUG")]
    public static void LogConditional(this ILogger logger, Exception? exception, string message, object? arg1)
    {
        logger.Log(LogLevel.Debug, exception, message, arg1);
    }

    /// <summary>
    /// Formats and writes a debug log message.
    /// </summary>
    /// <param name="logger">The <see cref="ILogger"/> to write to.</param>
    /// <param name="message">Format string of the log message in message template format. Example: <code>"User {0} logged in from {1}"</code></param>
    /// <example>logger.LogConditional("Processing request from {Address}", address)</example>
    [StringFormatMethod(nameof(message))]
    [Conditional("DEBUG")]
    public static void LogConditional(this ILogger logger, string message, object? arg1)
    {
        logger.Log(LogLevel.Debug, message, arg1);
    }

    /// <summary>
    /// Formats and writes a debug log message.
    /// </summary>
    /// <param name="logger">The <see cref="ILogger"/> to write to.</param>
    /// <param name="eventId">The event id associated with the log.</param>
    /// <param name="exception">The exception to log.</param>
    /// <param name="message">Format string of the log message in message template format. Example: <code>"User {0} logged in from {1}"</code></param>
    /// <example>logger.LogConditional(0, exception, "Error while processing request from {Address}", address)</example>
    [StringFormatMethod(nameof(message))]
    [Conditional("DEBUG")]
    public static void LogConditional(this ILogger logger, EventId eventId, Exception? exception, string message, object? arg1, object? arg2)
    {
        logger.Log(LogLevel.Debug, eventId, exception, message, arg1, arg2);
    }

    /// <summary>
    /// Formats and writes a debug log message.
    /// </summary>
    /// <param name="logger">The <see cref="ILogger"/> to write to.</param>
    /// <param name="eventId">The event id associated with the log.</param>
    /// <param name="message">Format string of the log message in message template format. Example: <code>"User {0} logged in from {1}"</code></param>
    /// <example>logger.LogConditional(0, "Processing request from {Address}", address)</example>
    [StringFormatMethod(nameof(message))]
    [Conditional("DEBUG")]
    public static void LogConditional(this ILogger logger, EventId eventId, string message, object? arg1, object? arg2)
    {
        logger.Log(LogLevel.Debug, eventId, message, arg1, arg2);
    }

    /// <summary>
    /// Formats and writes a debug log message.
    /// </summary>
    /// <param name="logger">The <see cref="ILogger"/> to write to.</param>
    /// <param name="exception">The exception to log.</param>
    /// <param name="message">Format string of the log message in message template format. Example: <code>"User {0} logged in from {1}"</code></param>
    /// <example>logger.LogConditional(exception, "Error while processing request from {Address}", address)</example>
    [StringFormatMethod(nameof(message))]
    [Conditional("DEBUG")]
    public static void LogConditional(this ILogger logger, Exception? exception, string message, object? arg1, object? arg2)
    {
        logger.Log(LogLevel.Debug, exception, message, arg1, arg2);
    }

    /// <summary>
    /// Formats and writes a debug log message.
    /// </summary>
    /// <param name="logger">The <see cref="ILogger"/> to write to.</param>
    /// <param name="message">Format string of the log message in message template format. Example: <code>"User {0} logged in from {1}"</code></param>
    /// <example>logger.LogConditional("Processing request from {Address}", address)</example>
    [StringFormatMethod(nameof(message))]
    [Conditional("DEBUG")]
    public static void LogConditional(this ILogger logger, string message, object? arg1, object? arg2)
    {
        logger.Log(LogLevel.Debug, message, arg1, arg2);
    }

    /// <summary>
    /// Formats and writes a debug log message.
    /// </summary>
    /// <param name="logger">The <see cref="ILogger"/> to write to.</param>
    /// <param name="eventId">The event id associated with the log.</param>
    /// <param name="exception">The exception to log.</param>
    /// <param name="message">Format string of the log message in message template format. Example: <code>"User {0} logged in from {1}"</code></param>
    /// <example>logger.LogConditional(0, exception, "Error while processing request from {Address}", address)</example>
    [StringFormatMethod(nameof(message))]
    [Conditional("DEBUG")]
    public static void LogConditional(this ILogger logger, EventId eventId, Exception? exception, string message, object? arg1, object? arg2, object? arg3)
    {
        logger.Log(LogLevel.Debug, eventId, exception, message, arg1, arg2, arg3);
    }

    /// <summary>
    /// Formats and writes a debug log message.
    /// </summary>
    /// <param name="logger">The <see cref="ILogger"/> to write to.</param>
    /// <param name="eventId">The event id associated with the log.</param>
    /// <param name="message">Format string of the log message in message template format. Example: <code>"User {0} logged in from {1}"</code></param>
    /// <example>logger.LogConditional(0, "Processing request from {Address}", address)</example>
    [StringFormatMethod(nameof(message))]
    [Conditional("DEBUG")]
    public static void LogConditional(this ILogger logger, EventId eventId, string message, object? arg1, object? arg2, object? arg3)
    {
        logger.Log(LogLevel.Debug, eventId, message, arg1, arg2, arg3);
    }

    /// <summary>
    /// Formats and writes a debug log message.
    /// </summary>
    /// <param name="logger">The <see cref="ILogger"/> to write to.</param>
    /// <param name="exception">The exception to log.</param>
    /// <param name="message">Format string of the log message in message template format. Example: <code>"User {0} logged in from {1}"</code></param>
    /// <example>logger.LogConditional(exception, "Error while processing request from {Address}", address)</example>
    [StringFormatMethod(nameof(message))]
    [Conditional("DEBUG")]
    public static void LogConditional(this ILogger logger, Exception? exception, string message, object? arg1, object? arg2, object? arg3)
    {
        logger.Log(LogLevel.Debug, exception, message, arg1, arg2, arg3);
    }

    /// <summary>
    /// Formats and writes a debug log message.
    /// </summary>
    /// <param name="logger">The <see cref="ILogger"/> to write to.</param>
    /// <param name="message">Format string of the log message in message template format. Example: <code>"User {0} logged in from {1}"</code></param>
    /// <example>logger.LogConditional("Processing request from {Address}", address)</example>
    [StringFormatMethod(nameof(message))]
    [Conditional("DEBUG")]
    public static void LogConditional(this ILogger logger, string message, object? arg1, object? arg2, object? arg3)
    {
        logger.Log(LogLevel.Debug, message, arg1, arg2, arg3);
    }

    /// <summary>
    /// Formats and writes a debug log message.
    /// </summary>
    /// <param name="logger">The <see cref="ILogger"/> to write to.</param>
    /// <param name="eventId">The event id associated with the log.</param>
    /// <param name="exception">The exception to log.</param>
    /// <param name="message">Format string of the log message in message template format. Example: <code>"User {0} logged in from {1}"</code></param>
    /// <example>logger.LogConditional(0, exception, "Error while processing request from {Address}", address)</example>
    [StringFormatMethod(nameof(message))]
    [Conditional("DEBUG")]
    public static void LogConditional(this ILogger logger, EventId eventId, Exception? exception, string message, object? arg1, object? arg2, object? arg3, object? arg4)
    {
        logger.Log(LogLevel.Debug, eventId, exception, message, arg1, arg2, arg3, arg4);
    }

    /// <summary>
    /// Formats and writes a debug log message.
    /// </summary>
    /// <param name="logger">The <see cref="ILogger"/> to write to.</param>
    /// <param name="eventId">The event id associated with the log.</param>
    /// <param name="message">Format string of the log message in message template format. Example: <code>"User {0} logged in from {1}"</code></param>
    /// <example>logger.LogConditional(0, "Processing request from {Address}", address)</example>
    [StringFormatMethod(nameof(message))]
    [Conditional("DEBUG")]
    public static void LogConditional(this ILogger logger, EventId eventId, string message, object? arg1, object? arg2, object? arg3, object? arg4)
    {
        logger.Log(LogLevel.Debug, eventId, message, arg1, arg2, arg3, arg4);
    }

    /// <summary>
    /// Formats and writes a debug log message.
    /// </summary>
    /// <param name="logger">The <see cref="ILogger"/> to write to.</param>
    /// <param name="exception">The exception to log.</param>
    /// <param name="message">Format string of the log message in message template format. Example: <code>"User {0} logged in from {1}"</code></param>
    /// <example>logger.LogConditional(exception, "Error while processing request from {Address}", address)</example>
    [StringFormatMethod(nameof(message))]
    [Conditional("DEBUG")]
    public static void LogConditional(this ILogger logger, Exception? exception, string message, object? arg1, object? arg2, object? arg3, object? arg4)
    {
        logger.Log(LogLevel.Debug, exception, message, arg1, arg2, arg3, arg4);
    }

    /// <summary>
    /// Formats and writes a debug log message.
    /// </summary>
    /// <param name="logger">The <see cref="ILogger"/> to write to.</param>
    /// <param name="message">Format string of the log message in message template format. Example: <code>"User {0} logged in from {1}"</code></param>
    /// <example>logger.LogConditional("Processing request from {Address}", address)</example>
    [StringFormatMethod(nameof(message))]
    [Conditional("DEBUG")]
    public static void LogConditional(this ILogger logger, string message, object? arg1, object? arg2, object? arg3, object? arg4)
    {
        logger.Log(LogLevel.Debug, message, arg1, arg2, arg3, arg4);
    }

    //------------------------------------------TRACE------------------------------------------//

    /// <summary>
    /// Formats and writes a trace log message.
    /// </summary>
    /// <param name="logger">The <see cref="ILogger"/> to write to.</param>
    /// <param name="eventId">The event id associated with the log.</param>
    /// <param name="exception">The exception to log.</param>
    /// <param name="message">Format string of the log message in message template format. Example: <code>"User {0} logged in from {1}"</code></param>
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
    /// <param name="message">Format string of the log message in message template format. Example: <code>"User {0} logged in from {1}"</code></param>
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
    /// <param name="message">Format string of the log message in message template format. Example: <code>"User {0} logged in from {1}"</code></param>
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
    /// <param name="message">Format string of the log message in message template format. Example: <code>"User {0} logged in from {1}"</code></param>
    /// <param name="args">An object array that contains zero or more objects to format.</param>
    /// <example>logger.LogTrace("Processing request from {Address}", address)</example>
    [StringFormatMethod(nameof(message))]
    public static void LogTrace(this ILogger logger, string message, params object?[]? args)
    {
        logger.Log(LogLevel.Trace, message, args);
    }

    /// <summary>
    /// Formats and writes a trace log message.
    /// </summary>
    /// <param name="logger">The <see cref="ILogger"/> to write to.</param>
    /// <param name="eventId">The event id associated with the log.</param>
    /// <param name="exception">The exception to log.</param>
    /// <param name="message">Format string of the log message in message template format. Example: <code>"User {0} logged in from {1}"</code></param>
    /// <example>logger.LogTrace(0, exception, "Error while processing request from {Address}", address)</example>
    [StringFormatMethod(nameof(message))]
    public static void LogTrace(this ILogger logger, EventId eventId, Exception? exception, string message)
    {
        logger.Log(LogLevel.Trace, eventId, exception, message, Array.Empty<object>());
    }

    /// <summary>
    /// Formats and writes a trace log message.
    /// </summary>
    /// <param name="logger">The <see cref="ILogger"/> to write to.</param>
    /// <param name="eventId">The event id associated with the log.</param>
    /// <param name="message">Format string of the log message in message template format. Example: <code>"User {0} logged in from {1}"</code></param>
    /// <example>logger.LogTrace(0, "Processing request from {Address}", address)</example>
    [StringFormatMethod(nameof(message))]
    public static void LogTrace(this ILogger logger, EventId eventId, string message)
    {
        logger.Log(LogLevel.Trace, eventId, message, Array.Empty<object>());
    }

    /// <summary>
    /// Formats and writes a trace log message.
    /// </summary>
    /// <param name="logger">The <see cref="ILogger"/> to write to.</param>
    /// <param name="exception">The exception to log.</param>
    /// <param name="message">Format string of the log message in message template format. Example: <code>"User {0} logged in from {1}"</code></param>
    /// <example>logger.LogTrace(exception, "Error while processing request from {Address}", address)</example>
    [StringFormatMethod(nameof(message))]
    public static void LogTrace(this ILogger logger, Exception? exception, string message)
    {
        logger.Log(LogLevel.Trace, exception, message, Array.Empty<object>());
    }

    /// <summary>
    /// Formats and writes a trace log message.
    /// </summary>
    /// <param name="logger">The <see cref="ILogger"/> to write to.</param>
    /// <param name="message">Format string of the log message in message template format. Example: <code>"User {0} logged in from {1}"</code></param>
    /// <example>logger.LogTrace("Processing request from {Address}", address)</example>
    [StringFormatMethod(nameof(message))]
    public static void LogTrace(this ILogger logger, string message)
    {
        logger.Log(LogLevel.Trace, message, Array.Empty<object>());
    }

    /// <summary>
    /// Formats and writes a trace log message.
    /// </summary>
    /// <param name="logger">The <see cref="ILogger"/> to write to.</param>
    /// <param name="eventId">The event id associated with the log.</param>
    /// <param name="exception">The exception to log.</param>
    /// <param name="message">Format string of the log message in message template format. Example: <code>"User {0} logged in from {1}"</code></param>
    /// <example>logger.LogTrace(0, exception, "Error while processing request from {Address}", address)</example>
    [StringFormatMethod(nameof(message))]
    public static void LogTrace(this ILogger logger, EventId eventId, Exception? exception, string message, object? arg1)
    {
        logger.Log(LogLevel.Trace, eventId, exception, message, arg1);
    }

    /// <summary>
    /// Formats and writes a trace log message.
    /// </summary>
    /// <param name="logger">The <see cref="ILogger"/> to write to.</param>
    /// <param name="eventId">The event id associated with the log.</param>
    /// <param name="message">Format string of the log message in message template format. Example: <code>"User {0} logged in from {1}"</code></param>
    /// <example>logger.LogTrace(0, "Processing request from {Address}", address)</example>
    [StringFormatMethod(nameof(message))]
    public static void LogTrace(this ILogger logger, EventId eventId, string message, object? arg1)
    {
        logger.Log(LogLevel.Trace, eventId, message, arg1);
    }

    /// <summary>
    /// Formats and writes a trace log message.
    /// </summary>
    /// <param name="logger">The <see cref="ILogger"/> to write to.</param>
    /// <param name="exception">The exception to log.</param>
    /// <param name="message">Format string of the log message in message template format. Example: <code>"User {0} logged in from {1}"</code></param>
    /// <example>logger.LogTrace(exception, "Error while processing request from {Address}", address)</example>
    [StringFormatMethod(nameof(message))]
    public static void LogTrace(this ILogger logger, Exception? exception, string message, object? arg1)
    {
        logger.Log(LogLevel.Trace, exception, message, arg1);
    }

    /// <summary>
    /// Formats and writes a trace log message.
    /// </summary>
    /// <param name="logger">The <see cref="ILogger"/> to write to.</param>
    /// <param name="message">Format string of the log message in message template format. Example: <code>"User {0} logged in from {1}"</code></param>
    /// <example>logger.LogTrace("Processing request from {Address}", address)</example>
    [StringFormatMethod(nameof(message))]
    public static void LogTrace(this ILogger logger, string message, object? arg1)
    {
        logger.Log(LogLevel.Trace, message, arg1);
    }

    /// <summary>
    /// Formats and writes a trace log message.
    /// </summary>
    /// <param name="logger">The <see cref="ILogger"/> to write to.</param>
    /// <param name="eventId">The event id associated with the log.</param>
    /// <param name="exception">The exception to log.</param>
    /// <param name="message">Format string of the log message in message template format. Example: <code>"User {0} logged in from {1}"</code></param>
    /// <example>logger.LogTrace(0, exception, "Error while processing request from {Address}", address)</example>
    [StringFormatMethod(nameof(message))]
    public static void LogTrace(this ILogger logger, EventId eventId, Exception? exception, string message, object? arg1, object? arg2)
    {
        logger.Log(LogLevel.Trace, eventId, exception, message, arg1, arg2);
    }

    /// <summary>
    /// Formats and writes a trace log message.
    /// </summary>
    /// <param name="logger">The <see cref="ILogger"/> to write to.</param>
    /// <param name="eventId">The event id associated with the log.</param>
    /// <param name="message">Format string of the log message in message template format. Example: <code>"User {0} logged in from {1}"</code></param>
    /// <example>logger.LogTrace(0, "Processing request from {Address}", address)</example>
    [StringFormatMethod(nameof(message))]
    public static void LogTrace(this ILogger logger, EventId eventId, string message, object? arg1, object? arg2)
    {
        logger.Log(LogLevel.Trace, eventId, message, arg1, arg2);
    }

    /// <summary>
    /// Formats and writes a trace log message.
    /// </summary>
    /// <param name="logger">The <see cref="ILogger"/> to write to.</param>
    /// <param name="exception">The exception to log.</param>
    /// <param name="message">Format string of the log message in message template format. Example: <code>"User {0} logged in from {1}"</code></param>
    /// <example>logger.LogTrace(exception, "Error while processing request from {Address}", address)</example>
    [StringFormatMethod(nameof(message))]
    public static void LogTrace(this ILogger logger, Exception? exception, string message, object? arg1, object? arg2)
    {
        logger.Log(LogLevel.Trace, exception, message, arg1, arg2);
    }

    /// <summary>
    /// Formats and writes a trace log message.
    /// </summary>
    /// <param name="logger">The <see cref="ILogger"/> to write to.</param>
    /// <param name="message">Format string of the log message in message template format. Example: <code>"User {0} logged in from {1}"</code></param>
    /// <example>logger.LogTrace("Processing request from {Address}", address)</example>
    [StringFormatMethod(nameof(message))]
    public static void LogTrace(this ILogger logger, string message, object? arg1, object? arg2)
    {
        logger.Log(LogLevel.Trace, message, arg1, arg2);
    }

    /// <summary>
    /// Formats and writes a trace log message.
    /// </summary>
    /// <param name="logger">The <see cref="ILogger"/> to write to.</param>
    /// <param name="eventId">The event id associated with the log.</param>
    /// <param name="exception">The exception to log.</param>
    /// <param name="message">Format string of the log message in message template format. Example: <code>"User {0} logged in from {1}"</code></param>
    /// <example>logger.LogTrace(0, exception, "Error while processing request from {Address}", address)</example>
    [StringFormatMethod(nameof(message))]
    public static void LogTrace(this ILogger logger, EventId eventId, Exception? exception, string message, object? arg1, object? arg2, object? arg3)
    {
        logger.Log(LogLevel.Trace, eventId, exception, message, arg1, arg2, arg3);
    }

    /// <summary>
    /// Formats and writes a trace log message.
    /// </summary>
    /// <param name="logger">The <see cref="ILogger"/> to write to.</param>
    /// <param name="eventId">The event id associated with the log.</param>
    /// <param name="message">Format string of the log message in message template format. Example: <code>"User {0} logged in from {1}"</code></param>
    /// <example>logger.LogTrace(0, "Processing request from {Address}", address)</example>
    [StringFormatMethod(nameof(message))]
    public static void LogTrace(this ILogger logger, EventId eventId, string message, object? arg1, object? arg2, object? arg3)
    {
        logger.Log(LogLevel.Trace, eventId, message, arg1, arg2, arg3);
    }

    /// <summary>
    /// Formats and writes a trace log message.
    /// </summary>
    /// <param name="logger">The <see cref="ILogger"/> to write to.</param>
    /// <param name="exception">The exception to log.</param>
    /// <param name="message">Format string of the log message in message template format. Example: <code>"User {0} logged in from {1}"</code></param>
    /// <example>logger.LogTrace(exception, "Error while processing request from {Address}", address)</example>
    [StringFormatMethod(nameof(message))]
    public static void LogTrace(this ILogger logger, Exception? exception, string message, object? arg1, object? arg2, object? arg3)
    {
        logger.Log(LogLevel.Trace, exception, message, arg1, arg2, arg3);
    }

    /// <summary>
    /// Formats and writes a trace log message.
    /// </summary>
    /// <param name="logger">The <see cref="ILogger"/> to write to.</param>
    /// <param name="message">Format string of the log message in message template format. Example: <code>"User {0} logged in from {1}"</code></param>
    /// <example>logger.LogTrace("Processing request from {Address}", address)</example>
    [StringFormatMethod(nameof(message))]
    public static void LogTrace(this ILogger logger, string message, object? arg1, object? arg2, object? arg3)
    {
        logger.Log(LogLevel.Trace, message, arg1, arg2, arg3);
    }

    /// <summary>
    /// Formats and writes a trace log message.
    /// </summary>
    /// <param name="logger">The <see cref="ILogger"/> to write to.</param>
    /// <param name="eventId">The event id associated with the log.</param>
    /// <param name="exception">The exception to log.</param>
    /// <param name="message">Format string of the log message in message template format. Example: <code>"User {0} logged in from {1}"</code></param>
    /// <example>logger.LogTrace(0, exception, "Error while processing request from {Address}", address)</example>
    [StringFormatMethod(nameof(message))]
    public static void LogTrace(this ILogger logger, EventId eventId, Exception? exception, string message, object? arg1, object? arg2, object? arg3, object? arg4)
    {
        logger.Log(LogLevel.Trace, eventId, exception, message, arg1, arg2, arg3, arg4);
    }

    /// <summary>
    /// Formats and writes a trace log message.
    /// </summary>
    /// <param name="logger">The <see cref="ILogger"/> to write to.</param>
    /// <param name="eventId">The event id associated with the log.</param>
    /// <param name="message">Format string of the log message in message template format. Example: <code>"User {0} logged in from {1}"</code></param>
    /// <example>logger.LogTrace(0, "Processing request from {Address}", address)</example>
    [StringFormatMethod(nameof(message))]
    public static void LogTrace(this ILogger logger, EventId eventId, string message, object? arg1, object? arg2, object? arg3, object? arg4)
    {
        logger.Log(LogLevel.Trace, eventId, message, arg1, arg2, arg3, arg4);
    }

    /// <summary>
    /// Formats and writes a trace log message.
    /// </summary>
    /// <param name="logger">The <see cref="ILogger"/> to write to.</param>
    /// <param name="exception">The exception to log.</param>
    /// <param name="message">Format string of the log message in message template format. Example: <code>"User {0} logged in from {1}"</code></param>
    /// <example>logger.LogTrace(exception, "Error while processing request from {Address}", address)</example>
    [StringFormatMethod(nameof(message))]
    public static void LogTrace(this ILogger logger, Exception? exception, string message, object? arg1, object? arg2, object? arg3, object? arg4)
    {
        logger.Log(LogLevel.Trace, exception, message, arg1, arg2, arg3, arg4);
    }

    /// <summary>
    /// Formats and writes a trace log message.
    /// </summary>
    /// <param name="logger">The <see cref="ILogger"/> to write to.</param>
    /// <param name="message">Format string of the log message in message template format. Example: <code>"User {0} logged in from {1}"</code></param>
    /// <example>logger.LogTrace("Processing request from {Address}", address)</example>
    [StringFormatMethod(nameof(message))]
    public static void LogTrace(this ILogger logger, string message, object? arg1, object? arg2, object? arg3, object? arg4)
    {
        logger.Log(LogLevel.Trace, message, arg1, arg2, arg3, arg4);
    }

    //------------------------------------------INFORMATION------------------------------------------//

    /// <summary>
    /// Formats and writes an informational log message.
    /// </summary>
    /// <param name="logger">The <see cref="ILogger"/> to write to.</param>
    /// <param name="eventId">The event id associated with the log.</param>
    /// <param name="exception">The exception to log.</param>
    /// <param name="message">Format string of the log message in message template format. Example: <code>"User {0} logged in from {1}"</code></param>
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
    /// <param name="message">Format string of the log message in message template format. Example: <code>"User {0} logged in from {1}"</code></param>
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
    /// <param name="message">Format string of the log message in message template format. Example: <code>"User {0} logged in from {1}"</code></param>
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
    /// <param name="message">Format string of the log message in message template format. Example: <code>"User {0} logged in from {1}"</code></param>
    /// <param name="args">An object array that contains zero or more objects to format.</param>
    /// <example>logger.LogInformation("Processing request from {Address}", address)</example>
    [StringFormatMethod(nameof(message))]
    public static void LogInformation(this ILogger logger, string message, params object?[]? args)
    {
        logger.Log(LogLevel.Information, message, args);
    }

    /// <summary>
    /// Formats and writes an informational log message.
    /// </summary>
    /// <param name="logger">The <see cref="ILogger"/> to write to.</param>
    /// <param name="eventId">The event id associated with the log.</param>
    /// <param name="exception">The exception to log.</param>
    /// <param name="message">Format string of the log message in message template format. Example: <code>"User {0} logged in from {1}"</code></param>
    /// <example>logger.LogInformation(0, exception, "Error while processing request from {Address}", address)</example>
    [StringFormatMethod(nameof(message))]
    public static void LogInformation(this ILogger logger, EventId eventId, Exception? exception, string message)
    {
        logger.Log(LogLevel.Information, eventId, exception, message, Array.Empty<object>());
    }

    /// <summary>
    /// Formats and writes an informational log message.
    /// </summary>
    /// <param name="logger">The <see cref="ILogger"/> to write to.</param>
    /// <param name="eventId">The event id associated with the log.</param>
    /// <param name="message">Format string of the log message in message template format. Example: <code>"User {0} logged in from {1}"</code></param>
    /// <example>logger.LogInformation(0, "Processing request from {Address}", address)</example>
    [StringFormatMethod(nameof(message))]
    public static void LogInformation(this ILogger logger, EventId eventId, string message)
    {
        logger.Log(LogLevel.Information, eventId, message, Array.Empty<object>());
    }

    /// <summary>
    /// Formats and writes an informational log message.
    /// </summary>
    /// <param name="logger">The <see cref="ILogger"/> to write to.</param>
    /// <param name="exception">The exception to log.</param>
    /// <param name="message">Format string of the log message in message template format. Example: <code>"User {0} logged in from {1}"</code></param>
    /// <example>logger.LogInformation(exception, "Error while processing request from {Address}", address)</example>
    [StringFormatMethod(nameof(message))]
    public static void LogInformation(this ILogger logger, Exception? exception, string message)
    {
        logger.Log(LogLevel.Information, exception, message, Array.Empty<object>());
    }

    /// <summary>
    /// Formats and writes an informational log message.
    /// </summary>
    /// <param name="logger">The <see cref="ILogger"/> to write to.</param>
    /// <param name="message">Format string of the log message in message template format. Example: <code>"User {0} logged in from {1}"</code></param>
    /// <example>logger.LogInformation("Processing request from {Address}", address)</example>
    [StringFormatMethod(nameof(message))]
    public static void LogInformation(this ILogger logger, string message)
    {
        logger.Log(LogLevel.Information, message, Array.Empty<object>());
    }

    /// <summary>
    /// Formats and writes an informational log message.
    /// </summary>
    /// <param name="logger">The <see cref="ILogger"/> to write to.</param>
    /// <param name="eventId">The event id associated with the log.</param>
    /// <param name="exception">The exception to log.</param>
    /// <param name="message">Format string of the log message in message template format. Example: <code>"User {0} logged in from {1}"</code></param>
    /// <example>logger.LogInformation(0, exception, "Error while processing request from {Address}", address)</example>
    [StringFormatMethod(nameof(message))]
    public static void LogInformation(this ILogger logger, EventId eventId, Exception? exception, string message, object? arg1)
    {
        logger.Log(LogLevel.Information, eventId, exception, message, arg1);
    }

    /// <summary>
    /// Formats and writes an informational log message.
    /// </summary>
    /// <param name="logger">The <see cref="ILogger"/> to write to.</param>
    /// <param name="eventId">The event id associated with the log.</param>
    /// <param name="message">Format string of the log message in message template format. Example: <code>"User {0} logged in from {1}"</code></param>
    /// <example>logger.LogInformation(0, "Processing request from {Address}", address)</example>
    [StringFormatMethod(nameof(message))]
    public static void LogInformation(this ILogger logger, EventId eventId, string message, object? arg1)
    {
        logger.Log(LogLevel.Information, eventId, message, arg1);
    }

    /// <summary>
    /// Formats and writes an informational log message.
    /// </summary>
    /// <param name="logger">The <see cref="ILogger"/> to write to.</param>
    /// <param name="exception">The exception to log.</param>
    /// <param name="message">Format string of the log message in message template format. Example: <code>"User {0} logged in from {1}"</code></param>
    /// <example>logger.LogInformation(exception, "Error while processing request from {Address}", address)</example>
    [StringFormatMethod(nameof(message))]
    public static void LogInformation(this ILogger logger, Exception? exception, string message, object? arg1)
    {
        logger.Log(LogLevel.Information, exception, message, arg1);
    }

    /// <summary>
    /// Formats and writes an informational log message.
    /// </summary>
    /// <param name="logger">The <see cref="ILogger"/> to write to.</param>
    /// <param name="message">Format string of the log message in message template format. Example: <code>"User {0} logged in from {1}"</code></param>
    /// <example>logger.LogInformation("Processing request from {Address}", address)</example>
    [StringFormatMethod(nameof(message))]
    public static void LogInformation(this ILogger logger, string message, object? arg1)
    {
        logger.Log(LogLevel.Information, message, arg1);
    }

    /// <summary>
    /// Formats and writes an informational log message.
    /// </summary>
    /// <param name="logger">The <see cref="ILogger"/> to write to.</param>
    /// <param name="eventId">The event id associated with the log.</param>
    /// <param name="exception">The exception to log.</param>
    /// <param name="message">Format string of the log message in message template format. Example: <code>"User {0} logged in from {1}"</code></param>
    /// <example>logger.LogInformation(0, exception, "Error while processing request from {Address}", address)</example>
    [StringFormatMethod(nameof(message))]
    public static void LogInformation(this ILogger logger, EventId eventId, Exception? exception, string message, object? arg1, object? arg2)
    {
        logger.Log(LogLevel.Information, eventId, exception, message, arg1, arg2);
    }

    /// <summary>
    /// Formats and writes an informational log message.
    /// </summary>
    /// <param name="logger">The <see cref="ILogger"/> to write to.</param>
    /// <param name="eventId">The event id associated with the log.</param>
    /// <param name="message">Format string of the log message in message template format. Example: <code>"User {0} logged in from {1}"</code></param>
    /// <example>logger.LogInformation(0, "Processing request from {Address}", address)</example>
    [StringFormatMethod(nameof(message))]
    public static void LogInformation(this ILogger logger, EventId eventId, string message, object? arg1, object? arg2)
    {
        logger.Log(LogLevel.Information, eventId, message, arg1, arg2);
    }

    /// <summary>
    /// Formats and writes an informational log message.
    /// </summary>
    /// <param name="logger">The <see cref="ILogger"/> to write to.</param>
    /// <param name="exception">The exception to log.</param>
    /// <param name="message">Format string of the log message in message template format. Example: <code>"User {0} logged in from {1}"</code></param>
    /// <example>logger.LogInformation(exception, "Error while processing request from {Address}", address)</example>
    [StringFormatMethod(nameof(message))]
    public static void LogInformation(this ILogger logger, Exception? exception, string message, object? arg1, object? arg2)
    {
        logger.Log(LogLevel.Information, exception, message, arg1, arg2);
    }

    /// <summary>
    /// Formats and writes an informational log message.
    /// </summary>
    /// <param name="logger">The <see cref="ILogger"/> to write to.</param>
    /// <param name="message">Format string of the log message in message template format. Example: <code>"User {0} logged in from {1}"</code></param>
    /// <example>logger.LogInformation("Processing request from {Address}", address)</example>
    [StringFormatMethod(nameof(message))]
    public static void LogInformation(this ILogger logger, string message, object? arg1, object? arg2)
    {
        logger.Log(LogLevel.Information, message, arg1, arg2);
    }

    /// <summary>
    /// Formats and writes an informational log message.
    /// </summary>
    /// <param name="logger">The <see cref="ILogger"/> to write to.</param>
    /// <param name="eventId">The event id associated with the log.</param>
    /// <param name="exception">The exception to log.</param>
    /// <param name="message">Format string of the log message in message template format. Example: <code>"User {0} logged in from {1}"</code></param>
    /// <example>logger.LogInformation(0, exception, "Error while processing request from {Address}", address)</example>
    [StringFormatMethod(nameof(message))]
    public static void LogInformation(this ILogger logger, EventId eventId, Exception? exception, string message, object? arg1, object? arg2, object? arg3)
    {
        logger.Log(LogLevel.Information, eventId, exception, message, arg1, arg2, arg3);
    }

    /// <summary>
    /// Formats and writes an informational log message.
    /// </summary>
    /// <param name="logger">The <see cref="ILogger"/> to write to.</param>
    /// <param name="eventId">The event id associated with the log.</param>
    /// <param name="message">Format string of the log message in message template format. Example: <code>"User {0} logged in from {1}"</code></param>
    /// <example>logger.LogInformation(0, "Processing request from {Address}", address)</example>
    [StringFormatMethod(nameof(message))]
    public static void LogInformation(this ILogger logger, EventId eventId, string message, object? arg1, object? arg2, object? arg3)
    {
        logger.Log(LogLevel.Information, eventId, message, arg1, arg2, arg3);
    }

    /// <summary>
    /// Formats and writes an informational log message.
    /// </summary>
    /// <param name="logger">The <see cref="ILogger"/> to write to.</param>
    /// <param name="exception">The exception to log.</param>
    /// <param name="message">Format string of the log message in message template format. Example: <code>"User {0} logged in from {1}"</code></param>
    /// <example>logger.LogInformation(exception, "Error while processing request from {Address}", address)</example>
    [StringFormatMethod(nameof(message))]
    public static void LogInformation(this ILogger logger, Exception? exception, string message, object? arg1, object? arg2, object? arg3)
    {
        logger.Log(LogLevel.Information, exception, message, arg1, arg2, arg3);
    }

    /// <summary>
    /// Formats and writes an informational log message.
    /// </summary>
    /// <param name="logger">The <see cref="ILogger"/> to write to.</param>
    /// <param name="message">Format string of the log message in message template format. Example: <code>"User {0} logged in from {1}"</code></param>
    /// <example>logger.LogInformation("Processing request from {Address}", address)</example>
    [StringFormatMethod(nameof(message))]
    public static void LogInformation(this ILogger logger, string message, object? arg1, object? arg2, object? arg3)
    {
        logger.Log(LogLevel.Information, message, arg1, arg2, arg3);
    }

    /// <summary>
    /// Formats and writes an informational log message.
    /// </summary>
    /// <param name="logger">The <see cref="ILogger"/> to write to.</param>
    /// <param name="eventId">The event id associated with the log.</param>
    /// <param name="exception">The exception to log.</param>
    /// <param name="message">Format string of the log message in message template format. Example: <code>"User {0} logged in from {1}"</code></param>
    /// <example>logger.LogInformation(0, exception, "Error while processing request from {Address}", address)</example>
    [StringFormatMethod(nameof(message))]
    public static void LogInformation(this ILogger logger, EventId eventId, Exception? exception, string message, object? arg1, object? arg2, object? arg3, object? arg4)
    {
        logger.Log(LogLevel.Information, eventId, exception, message, arg1, arg2, arg3, arg4);
    }

    /// <summary>
    /// Formats and writes an informational log message.
    /// </summary>
    /// <param name="logger">The <see cref="ILogger"/> to write to.</param>
    /// <param name="eventId">The event id associated with the log.</param>
    /// <param name="message">Format string of the log message in message template format. Example: <code>"User {0} logged in from {1}"</code></param>
    /// <example>logger.LogInformation(0, "Processing request from {Address}", address)</example>
    [StringFormatMethod(nameof(message))]
    public static void LogInformation(this ILogger logger, EventId eventId, string message, object? arg1, object? arg2, object? arg3, object? arg4)
    {
        logger.Log(LogLevel.Information, eventId, message, arg1, arg2, arg3, arg4);
    }

    /// <summary>
    /// Formats and writes an informational log message.
    /// </summary>
    /// <param name="logger">The <see cref="ILogger"/> to write to.</param>
    /// <param name="exception">The exception to log.</param>
    /// <param name="message">Format string of the log message in message template format. Example: <code>"User {0} logged in from {1}"</code></param>
    /// <example>logger.LogInformation(exception, "Error while processing request from {Address}", address)</example>
    [StringFormatMethod(nameof(message))]
    public static void LogInformation(this ILogger logger, Exception? exception, string message, object? arg1, object? arg2, object? arg3, object? arg4)
    {
        logger.Log(LogLevel.Information, exception, message, arg1, arg2, arg3, arg4);
    }

    /// <summary>
    /// Formats and writes an informational log message.
    /// </summary>
    /// <param name="logger">The <see cref="ILogger"/> to write to.</param>
    /// <param name="message">Format string of the log message in message template format. Example: <code>"User {0} logged in from {1}"</code></param>
    /// <example>logger.LogInformation("Processing request from {Address}", address)</example>
    [StringFormatMethod(nameof(message))]
    public static void LogInformation(this ILogger logger, string message, object? arg1, object? arg2, object? arg3, object? arg4)
    {
        logger.Log(LogLevel.Information, message, arg1, arg2, arg3, arg4);
    }

    //------------------------------------------WARNING------------------------------------------//

    /// <summary>
    /// Formats and writes a warning log message.
    /// </summary>
    /// <param name="logger">The <see cref="ILogger"/> to write to.</param>
    /// <param name="eventId">The event id associated with the log.</param>
    /// <param name="exception">The exception to log.</param>
    /// <param name="message">Format string of the log message in message template format. Example: <code>"User {0} logged in from {1}"</code></param>
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
    /// <param name="message">Format string of the log message in message template format. Example: <code>"User {0} logged in from {1}"</code></param>
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
    /// <param name="message">Format string of the log message in message template format. Example: <code>"User {0} logged in from {1}"</code></param>
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
    /// <param name="message">Format string of the log message in message template format. Example: <code>"User {0} logged in from {1}"</code></param>
    /// <param name="args">An object array that contains zero or more objects to format.</param>
    /// <example>logger.LogWarning("Processing request from {Address}", address)</example>
    [StringFormatMethod(nameof(message))]
    public static void LogWarning(this ILogger logger, string message, params object?[]? args)
    {
        logger.Log(LogLevel.Warning, message, args);
    }

    /// <summary>
    /// Formats and writes a warning log message.
    /// </summary>
    /// <param name="logger">The <see cref="ILogger"/> to write to.</param>
    /// <param name="eventId">The event id associated with the log.</param>
    /// <param name="exception">The exception to log.</param>
    /// <param name="message">Format string of the log message in message template format. Example: <code>"User {0} logged in from {1}"</code></param>
    /// <example>logger.LogWarning(0, exception, "Error while processing request from {Address}", address)</example>
    [StringFormatMethod(nameof(message))]
    public static void LogWarning(this ILogger logger, EventId eventId, Exception? exception, string message)
    {
        logger.Log(LogLevel.Warning, eventId, exception, message, Array.Empty<object>());
    }

    /// <summary>
    /// Formats and writes a warning log message.
    /// </summary>
    /// <param name="logger">The <see cref="ILogger"/> to write to.</param>
    /// <param name="eventId">The event id associated with the log.</param>
    /// <param name="message">Format string of the log message in message template format. Example: <code>"User {0} logged in from {1}"</code></param>
    /// <example>logger.LogWarning(0, "Processing request from {Address}", address)</example>
    [StringFormatMethod(nameof(message))]
    public static void LogWarning(this ILogger logger, EventId eventId, string message)
    {
        logger.Log(LogLevel.Warning, eventId, message, Array.Empty<object>());
    }

    /// <summary>
    /// Formats and writes a warning log message.
    /// </summary>
    /// <param name="logger">The <see cref="ILogger"/> to write to.</param>
    /// <param name="exception">The exception to log.</param>
    /// <param name="message">Format string of the log message in message template format. Example: <code>"User {0} logged in from {1}"</code></param>
    /// <example>logger.LogWarning(exception, "Error while processing request from {Address}", address)</example>
    [StringFormatMethod(nameof(message))]
    public static void LogWarning(this ILogger logger, Exception? exception, string message)
    {
        logger.Log(LogLevel.Warning, exception, message, Array.Empty<object>());
    }

    /// <summary>
    /// Formats and writes a warning log message.
    /// </summary>
    /// <param name="logger">The <see cref="ILogger"/> to write to.</param>
    /// <param name="message">Format string of the log message in message template format. Example: <code>"User {0} logged in from {1}"</code></param>
    /// <example>logger.LogWarning("Processing request from {Address}", address)</example>
    [StringFormatMethod(nameof(message))]
    public static void LogWarning(this ILogger logger, string message)
    {
        logger.Log(LogLevel.Warning, message, Array.Empty<object>());
    }

    /// <summary>
    /// Formats and writes a warning log message.
    /// </summary>
    /// <param name="logger">The <see cref="ILogger"/> to write to.</param>
    /// <param name="eventId">The event id associated with the log.</param>
    /// <param name="exception">The exception to log.</param>
    /// <param name="message">Format string of the log message in message template format. Example: <code>"User {0} logged in from {1}"</code></param>
    /// <example>logger.LogWarning(0, exception, "Error while processing request from {Address}", address)</example>
    [StringFormatMethod(nameof(message))]
    public static void LogWarning(this ILogger logger, EventId eventId, Exception? exception, string message, object? arg1)
    {
        logger.Log(LogLevel.Warning, eventId, exception, message, arg1);
    }

    /// <summary>
    /// Formats and writes a warning log message.
    /// </summary>
    /// <param name="logger">The <see cref="ILogger"/> to write to.</param>
    /// <param name="eventId">The event id associated with the log.</param>
    /// <param name="message">Format string of the log message in message template format. Example: <code>"User {0} logged in from {1}"</code></param>
    /// <example>logger.LogWarning(0, "Processing request from {Address}", address)</example>
    [StringFormatMethod(nameof(message))]
    public static void LogWarning(this ILogger logger, EventId eventId, string message, object? arg1)
    {
        logger.Log(LogLevel.Warning, eventId, message, arg1);
    }

    /// <summary>
    /// Formats and writes a warning log message.
    /// </summary>
    /// <param name="logger">The <see cref="ILogger"/> to write to.</param>
    /// <param name="exception">The exception to log.</param>
    /// <param name="message">Format string of the log message in message template format. Example: <code>"User {0} logged in from {1}"</code></param>
    /// <example>logger.LogWarning(exception, "Error while processing request from {Address}", address)</example>
    [StringFormatMethod(nameof(message))]
    public static void LogWarning(this ILogger logger, Exception? exception, string message, object? arg1)
    {
        logger.Log(LogLevel.Warning, exception, message, arg1);
    }

    /// <summary>
    /// Formats and writes a warning log message.
    /// </summary>
    /// <param name="logger">The <see cref="ILogger"/> to write to.</param>
    /// <param name="message">Format string of the log message in message template format. Example: <code>"User {0} logged in from {1}"</code></param>
    /// <example>logger.LogWarning("Processing request from {Address}", address)</example>
    [StringFormatMethod(nameof(message))]
    public static void LogWarning(this ILogger logger, string message, object? arg1)
    {
        logger.Log(LogLevel.Warning, message, arg1);
    }

    /// <summary>
    /// Formats and writes a warning log message.
    /// </summary>
    /// <param name="logger">The <see cref="ILogger"/> to write to.</param>
    /// <param name="eventId">The event id associated with the log.</param>
    /// <param name="exception">The exception to log.</param>
    /// <param name="message">Format string of the log message in message template format. Example: <code>"User {0} logged in from {1}"</code></param>
    /// <example>logger.LogWarning(0, exception, "Error while processing request from {Address}", address)</example>
    [StringFormatMethod(nameof(message))]
    public static void LogWarning(this ILogger logger, EventId eventId, Exception? exception, string message, object? arg1, object? arg2)
    {
        logger.Log(LogLevel.Warning, eventId, exception, message, arg1, arg2);
    }

    /// <summary>
    /// Formats and writes a warning log message.
    /// </summary>
    /// <param name="logger">The <see cref="ILogger"/> to write to.</param>
    /// <param name="eventId">The event id associated with the log.</param>
    /// <param name="message">Format string of the log message in message template format. Example: <code>"User {0} logged in from {1}"</code></param>
    /// <example>logger.LogWarning(0, "Processing request from {Address}", address)</example>
    [StringFormatMethod(nameof(message))]
    public static void LogWarning(this ILogger logger, EventId eventId, string message, object? arg1, object? arg2)
    {
        logger.Log(LogLevel.Warning, eventId, message, arg1, arg2);
    }

    /// <summary>
    /// Formats and writes a warning log message.
    /// </summary>
    /// <param name="logger">The <see cref="ILogger"/> to write to.</param>
    /// <param name="exception">The exception to log.</param>
    /// <param name="message">Format string of the log message in message template format. Example: <code>"User {0} logged in from {1}"</code></param>
    /// <example>logger.LogWarning(exception, "Error while processing request from {Address}", address)</example>
    [StringFormatMethod(nameof(message))]
    public static void LogWarning(this ILogger logger, Exception? exception, string message, object? arg1, object? arg2)
    {
        logger.Log(LogLevel.Warning, exception, message, arg1, arg2);
    }

    /// <summary>
    /// Formats and writes a warning log message.
    /// </summary>
    /// <param name="logger">The <see cref="ILogger"/> to write to.</param>
    /// <param name="message">Format string of the log message in message template format. Example: <code>"User {0} logged in from {1}"</code></param>
    /// <example>logger.LogWarning("Processing request from {Address}", address)</example>
    [StringFormatMethod(nameof(message))]
    public static void LogWarning(this ILogger logger, string message, object? arg1, object? arg2)
    {
        logger.Log(LogLevel.Warning, message, arg1, arg2);
    }

    /// <summary>
    /// Formats and writes a warning log message.
    /// </summary>
    /// <param name="logger">The <see cref="ILogger"/> to write to.</param>
    /// <param name="eventId">The event id associated with the log.</param>
    /// <param name="exception">The exception to log.</param>
    /// <param name="message">Format string of the log message in message template format. Example: <code>"User {0} logged in from {1}"</code></param>
    /// <example>logger.LogWarning(0, exception, "Error while processing request from {Address}", address)</example>
    [StringFormatMethod(nameof(message))]
    public static void LogWarning(this ILogger logger, EventId eventId, Exception? exception, string message, object? arg1, object? arg2, object? arg3)
    {
        logger.Log(LogLevel.Warning, eventId, exception, message, arg1, arg2, arg3);
    }

    /// <summary>
    /// Formats and writes a warning log message.
    /// </summary>
    /// <param name="logger">The <see cref="ILogger"/> to write to.</param>
    /// <param name="eventId">The event id associated with the log.</param>
    /// <param name="message">Format string of the log message in message template format. Example: <code>"User {0} logged in from {1}"</code></param>
    /// <example>logger.LogWarning(0, "Processing request from {Address}", address)</example>
    [StringFormatMethod(nameof(message))]
    public static void LogWarning(this ILogger logger, EventId eventId, string message, object? arg1, object? arg2, object? arg3)
    {
        logger.Log(LogLevel.Warning, eventId, message, arg1, arg2, arg3);
    }

    /// <summary>
    /// Formats and writes a warning log message.
    /// </summary>
    /// <param name="logger">The <see cref="ILogger"/> to write to.</param>
    /// <param name="exception">The exception to log.</param>
    /// <param name="message">Format string of the log message in message template format. Example: <code>"User {0} logged in from {1}"</code></param>
    /// <example>logger.LogWarning(exception, "Error while processing request from {Address}", address)</example>
    [StringFormatMethod(nameof(message))]
    public static void LogWarning(this ILogger logger, Exception? exception, string message, object? arg1, object? arg2, object? arg3)
    {
        logger.Log(LogLevel.Warning, exception, message, arg1, arg2, arg3);
    }

    /// <summary>
    /// Formats and writes a warning log message.
    /// </summary>
    /// <param name="logger">The <see cref="ILogger"/> to write to.</param>
    /// <param name="message">Format string of the log message in message template format. Example: <code>"User {0} logged in from {1}"</code></param>
    /// <example>logger.LogWarning("Processing request from {Address}", address)</example>
    [StringFormatMethod(nameof(message))]
    public static void LogWarning(this ILogger logger, string message, object? arg1, object? arg2, object? arg3)
    {
        logger.Log(LogLevel.Warning, message, arg1, arg2, arg3);
    }

    /// <summary>
    /// Formats and writes a warning log message.
    /// </summary>
    /// <param name="logger">The <see cref="ILogger"/> to write to.</param>
    /// <param name="eventId">The event id associated with the log.</param>
    /// <param name="exception">The exception to log.</param>
    /// <param name="message">Format string of the log message in message template format. Example: <code>"User {0} logged in from {1}"</code></param>
    /// <example>logger.LogWarning(0, exception, "Error while processing request from {Address}", address)</example>
    [StringFormatMethod(nameof(message))]
    public static void LogWarning(this ILogger logger, EventId eventId, Exception? exception, string message, object? arg1, object? arg2, object? arg3, object? arg4)
    {
        logger.Log(LogLevel.Warning, eventId, exception, message, arg1, arg2, arg3, arg4);
    }

    /// <summary>
    /// Formats and writes a warning log message.
    /// </summary>
    /// <param name="logger">The <see cref="ILogger"/> to write to.</param>
    /// <param name="eventId">The event id associated with the log.</param>
    /// <param name="message">Format string of the log message in message template format. Example: <code>"User {0} logged in from {1}"</code></param>
    /// <example>logger.LogWarning(0, "Processing request from {Address}", address)</example>
    [StringFormatMethod(nameof(message))]
    public static void LogWarning(this ILogger logger, EventId eventId, string message, object? arg1, object? arg2, object? arg3, object? arg4)
    {
        logger.Log(LogLevel.Warning, eventId, message, arg1, arg2, arg3, arg4);
    }

    /// <summary>
    /// Formats and writes a warning log message.
    /// </summary>
    /// <param name="logger">The <see cref="ILogger"/> to write to.</param>
    /// <param name="exception">The exception to log.</param>
    /// <param name="message">Format string of the log message in message template format. Example: <code>"User {0} logged in from {1}"</code></param>
    /// <example>logger.LogWarning(exception, "Error while processing request from {Address}", address)</example>
    [StringFormatMethod(nameof(message))]
    public static void LogWarning(this ILogger logger, Exception? exception, string message, object? arg1, object? arg2, object? arg3, object? arg4)
    {
        logger.Log(LogLevel.Warning, exception, message, arg1, arg2, arg3, arg4);
    }

    /// <summary>
    /// Formats and writes a warning log message.
    /// </summary>
    /// <param name="logger">The <see cref="ILogger"/> to write to.</param>
    /// <param name="message">Format string of the log message in message template format. Example: <code>"User {0} logged in from {1}"</code></param>
    /// <example>logger.LogWarning("Processing request from {Address}", address)</example>
    [StringFormatMethod(nameof(message))]
    public static void LogWarning(this ILogger logger, string message, object? arg1, object? arg2, object? arg3, object? arg4)
    {
        logger.Log(LogLevel.Warning, message, arg1, arg2, arg3, arg4);
    }

    //------------------------------------------ERROR------------------------------------------//

    /// <summary>
    /// Formats and writes an error log message.
    /// </summary>
    /// <param name="logger">The <see cref="ILogger"/> to write to.</param>
    /// <param name="eventId">The event id associated with the log.</param>
    /// <param name="exception">The exception to log.</param>
    /// <param name="message">Format string of the log message in message template format. Example: <code>"User {0} logged in from {1}"</code></param>
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
    /// <param name="message">Format string of the log message in message template format. Example: <code>"User {0} logged in from {1}"</code></param>
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
    /// <param name="message">Format string of the log message in message template format. Example: <code>"User {0} logged in from {1}"</code></param>
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
    /// <param name="message">Format string of the log message in message template format. Example: <code>"User {0} logged in from {1}"</code></param>
    /// <param name="args">An object array that contains zero or more objects to format.</param>
    /// <example>logger.LogError("Processing request from {Address}", address)</example>
    [StringFormatMethod(nameof(message))]
    public static void LogError(this ILogger logger, string message, params object?[]? args)
    {
        logger.Log(LogLevel.Error, message, args);
    }

    /// <summary>
    /// Formats and writes an error log message.
    /// </summary>
    /// <param name="logger">The <see cref="ILogger"/> to write to.</param>
    /// <param name="eventId">The event id associated with the log.</param>
    /// <param name="exception">The exception to log.</param>
    /// <param name="message">Format string of the log message in message template format. Example: <code>"User {0} logged in from {1}"</code></param>
    /// <example>logger.LogError(0, exception, "Error while processing request from {Address}", address)</example>
    [StringFormatMethod(nameof(message))]
    public static void LogError(this ILogger logger, EventId eventId, Exception? exception, string message)
    {
        logger.Log(LogLevel.Error, eventId, exception, message, Array.Empty<object>());
    }

    /// <summary>
    /// Formats and writes an error log message.
    /// </summary>
    /// <param name="logger">The <see cref="ILogger"/> to write to.</param>
    /// <param name="eventId">The event id associated with the log.</param>
    /// <param name="message">Format string of the log message in message template format. Example: <code>"User {0} logged in from {1}"</code></param>
    /// <example>logger.LogError(0, "Processing request from {Address}", address)</example>
    [StringFormatMethod(nameof(message))]
    public static void LogError(this ILogger logger, EventId eventId, string message)
    {
        logger.Log(LogLevel.Error, eventId, message, Array.Empty<object>());
    }

    /// <summary>
    /// Formats and writes an error log message.
    /// </summary>
    /// <param name="logger">The <see cref="ILogger"/> to write to.</param>
    /// <param name="exception">The exception to log.</param>
    /// <param name="message">Format string of the log message in message template format. Example: <code>"User {0} logged in from {1}"</code></param>
    /// <example>logger.LogError(exception, "Error while processing request from {Address}", address)</example>
    [StringFormatMethod(nameof(message))]
    public static void LogError(this ILogger logger, Exception? exception, string message)
    {
        logger.Log(LogLevel.Error, exception, message, Array.Empty<object>());
    }

    /// <summary>
    /// Formats and writes an error log message.
    /// </summary>
    /// <param name="logger">The <see cref="ILogger"/> to write to.</param>
    /// <param name="message">Format string of the log message in message template format. Example: <code>"User {0} logged in from {1}"</code></param>
    /// <example>logger.LogError("Processing request from {Address}", address)</example>
    [StringFormatMethod(nameof(message))]
    public static void LogError(this ILogger logger, string message)
    {
        logger.Log(LogLevel.Error, message, Array.Empty<object>());
    }

    /// <summary>
    /// Formats and writes an error log message.
    /// </summary>
    /// <param name="logger">The <see cref="ILogger"/> to write to.</param>
    /// <param name="eventId">The event id associated with the log.</param>
    /// <param name="exception">The exception to log.</param>
    /// <param name="message">Format string of the log message in message template format. Example: <code>"User {0} logged in from {1}"</code></param>
    /// <example>logger.LogError(0, exception, "Error while processing request from {Address}", address)</example>
    [StringFormatMethod(nameof(message))]
    public static void LogError(this ILogger logger, EventId eventId, Exception? exception, string message, object? arg1)
    {
        logger.Log(LogLevel.Error, eventId, exception, message, arg1);
    }

    /// <summary>
    /// Formats and writes an error log message.
    /// </summary>
    /// <param name="logger">The <see cref="ILogger"/> to write to.</param>
    /// <param name="eventId">The event id associated with the log.</param>
    /// <param name="message">Format string of the log message in message template format. Example: <code>"User {0} logged in from {1}"</code></param>
    /// <example>logger.LogError(0, "Processing request from {Address}", address)</example>
    [StringFormatMethod(nameof(message))]
    public static void LogError(this ILogger logger, EventId eventId, string message, object? arg1)
    {
        logger.Log(LogLevel.Error, eventId, message, arg1);
    }

    /// <summary>
    /// Formats and writes an error log message.
    /// </summary>
    /// <param name="logger">The <see cref="ILogger"/> to write to.</param>
    /// <param name="exception">The exception to log.</param>
    /// <param name="message">Format string of the log message in message template format. Example: <code>"User {0} logged in from {1}"</code></param>
    /// <example>logger.LogError(exception, "Error while processing request from {Address}", address)</example>
    [StringFormatMethod(nameof(message))]
    public static void LogError(this ILogger logger, Exception? exception, string message, object? arg1)
    {
        logger.Log(LogLevel.Error, exception, message, arg1);
    }

    /// <summary>
    /// Formats and writes an error log message.
    /// </summary>
    /// <param name="logger">The <see cref="ILogger"/> to write to.</param>
    /// <param name="message">Format string of the log message in message template format. Example: <code>"User {0} logged in from {1}"</code></param>
    /// <example>logger.LogError("Processing request from {Address}", address)</example>
    [StringFormatMethod(nameof(message))]
    public static void LogError(this ILogger logger, string message, object? arg1)
    {
        logger.Log(LogLevel.Error, message, arg1);
    }

    /// <summary>
    /// Formats and writes an error log message.
    /// </summary>
    /// <param name="logger">The <see cref="ILogger"/> to write to.</param>
    /// <param name="eventId">The event id associated with the log.</param>
    /// <param name="exception">The exception to log.</param>
    /// <param name="message">Format string of the log message in message template format. Example: <code>"User {0} logged in from {1}"</code></param>
    /// <example>logger.LogError(0, exception, "Error while processing request from {Address}", address)</example>
    [StringFormatMethod(nameof(message))]
    public static void LogError(this ILogger logger, EventId eventId, Exception? exception, string message, object? arg1, object? arg2)
    {
        logger.Log(LogLevel.Error, eventId, exception, message, arg1, arg2);
    }

    /// <summary>
    /// Formats and writes an error log message.
    /// </summary>
    /// <param name="logger">The <see cref="ILogger"/> to write to.</param>
    /// <param name="eventId">The event id associated with the log.</param>
    /// <param name="message">Format string of the log message in message template format. Example: <code>"User {0} logged in from {1}"</code></param>
    /// <example>logger.LogError(0, "Processing request from {Address}", address)</example>
    [StringFormatMethod(nameof(message))]
    public static void LogError(this ILogger logger, EventId eventId, string message, object? arg1, object? arg2)
    {
        logger.Log(LogLevel.Error, eventId, message, arg1, arg2);
    }

    /// <summary>
    /// Formats and writes an error log message.
    /// </summary>
    /// <param name="logger">The <see cref="ILogger"/> to write to.</param>
    /// <param name="exception">The exception to log.</param>
    /// <param name="message">Format string of the log message in message template format. Example: <code>"User {0} logged in from {1}"</code></param>
    /// <example>logger.LogError(exception, "Error while processing request from {Address}", address)</example>
    [StringFormatMethod(nameof(message))]
    public static void LogError(this ILogger logger, Exception? exception, string message, object? arg1, object? arg2)
    {
        logger.Log(LogLevel.Error, exception, message, arg1, arg2);
    }

    /// <summary>
    /// Formats and writes an error log message.
    /// </summary>
    /// <param name="logger">The <see cref="ILogger"/> to write to.</param>
    /// <param name="message">Format string of the log message in message template format. Example: <code>"User {0} logged in from {1}"</code></param>
    /// <example>logger.LogError("Processing request from {Address}", address)</example>
    [StringFormatMethod(nameof(message))]
    public static void LogError(this ILogger logger, string message, object? arg1, object? arg2)
    {
        logger.Log(LogLevel.Error, message, arg1, arg2);
    }

    /// <summary>
    /// Formats and writes an error log message.
    /// </summary>
    /// <param name="logger">The <see cref="ILogger"/> to write to.</param>
    /// <param name="eventId">The event id associated with the log.</param>
    /// <param name="exception">The exception to log.</param>
    /// <param name="message">Format string of the log message in message template format. Example: <code>"User {0} logged in from {1}"</code></param>
    /// <example>logger.LogError(0, exception, "Error while processing request from {Address}", address)</example>
    [StringFormatMethod(nameof(message))]
    public static void LogError(this ILogger logger, EventId eventId, Exception? exception, string message, object? arg1, object? arg2, object? arg3)
    {
        logger.Log(LogLevel.Error, eventId, exception, message, arg1, arg2, arg3);
    }

    /// <summary>
    /// Formats and writes an error log message.
    /// </summary>
    /// <param name="logger">The <see cref="ILogger"/> to write to.</param>
    /// <param name="eventId">The event id associated with the log.</param>
    /// <param name="message">Format string of the log message in message template format. Example: <code>"User {0} logged in from {1}"</code></param>
    /// <example>logger.LogError(0, "Processing request from {Address}", address)</example>
    [StringFormatMethod(nameof(message))]
    public static void LogError(this ILogger logger, EventId eventId, string message, object? arg1, object? arg2, object? arg3)
    {
        logger.Log(LogLevel.Error, eventId, message, arg1, arg2, arg3);
    }

    /// <summary>
    /// Formats and writes an error log message.
    /// </summary>
    /// <param name="logger">The <see cref="ILogger"/> to write to.</param>
    /// <param name="exception">The exception to log.</param>
    /// <param name="message">Format string of the log message in message template format. Example: <code>"User {0} logged in from {1}"</code></param>
    /// <example>logger.LogError(exception, "Error while processing request from {Address}", address)</example>
    [StringFormatMethod(nameof(message))]
    public static void LogError(this ILogger logger, Exception? exception, string message, object? arg1, object? arg2, object? arg3)
    {
        logger.Log(LogLevel.Error, exception, message, arg1, arg2, arg3);
    }

    /// <summary>
    /// Formats and writes an error log message.
    /// </summary>
    /// <param name="logger">The <see cref="ILogger"/> to write to.</param>
    /// <param name="message">Format string of the log message in message template format. Example: <code>"User {0} logged in from {1}"</code></param>
    /// <example>logger.LogError("Processing request from {Address}", address)</example>
    [StringFormatMethod(nameof(message))]
    public static void LogError(this ILogger logger, string message, object? arg1, object? arg2, object? arg3)
    {
        logger.Log(LogLevel.Error, message, arg1, arg2, arg3);
    }

    /// <summary>
    /// Formats and writes an error log message.
    /// </summary>
    /// <param name="logger">The <see cref="ILogger"/> to write to.</param>
    /// <param name="eventId">The event id associated with the log.</param>
    /// <param name="exception">The exception to log.</param>
    /// <param name="message">Format string of the log message in message template format. Example: <code>"User {0} logged in from {1}"</code></param>
    /// <example>logger.LogError(0, exception, "Error while processing request from {Address}", address)</example>
    [StringFormatMethod(nameof(message))]
    public static void LogError(this ILogger logger, EventId eventId, Exception? exception, string message, object? arg1, object? arg2, object? arg3, object? arg4)
    {
        logger.Log(LogLevel.Error, eventId, exception, message, arg1, arg2, arg3, arg4);
    }

    /// <summary>
    /// Formats and writes an error log message.
    /// </summary>
    /// <param name="logger">The <see cref="ILogger"/> to write to.</param>
    /// <param name="eventId">The event id associated with the log.</param>
    /// <param name="message">Format string of the log message in message template format. Example: <code>"User {0} logged in from {1}"</code></param>
    /// <example>logger.LogError(0, "Processing request from {Address}", address)</example>
    [StringFormatMethod(nameof(message))]
    public static void LogError(this ILogger logger, EventId eventId, string message, object? arg1, object? arg2, object? arg3, object? arg4)
    {
        logger.Log(LogLevel.Error, eventId, message, arg1, arg2, arg3, arg4);
    }

    /// <summary>
    /// Formats and writes an error log message.
    /// </summary>
    /// <param name="logger">The <see cref="ILogger"/> to write to.</param>
    /// <param name="exception">The exception to log.</param>
    /// <param name="message">Format string of the log message in message template format. Example: <code>"User {0} logged in from {1}"</code></param>
    /// <example>logger.LogError(exception, "Error while processing request from {Address}", address)</example>
    [StringFormatMethod(nameof(message))]
    public static void LogError(this ILogger logger, Exception? exception, string message, object? arg1, object? arg2, object? arg3, object? arg4)
    {
        logger.Log(LogLevel.Error, exception, message, arg1, arg2, arg3, arg4);
    }

    /// <summary>
    /// Formats and writes an error log message.
    /// </summary>
    /// <param name="logger">The <see cref="ILogger"/> to write to.</param>
    /// <param name="message">Format string of the log message in message template format. Example: <code>"User {0} logged in from {1}"</code></param>
    /// <example>logger.LogError("Processing request from {Address}", address)</example>
    [StringFormatMethod(nameof(message))]
    public static void LogError(this ILogger logger, string message, object? arg1, object? arg2, object? arg3, object? arg4)
    {
        logger.Log(LogLevel.Error, message, arg1, arg2, arg3, arg4);
    }

    //------------------------------------------CRITICAL------------------------------------------//

    /// <summary>
    /// Formats and writes a critical log message.
    /// </summary>
    /// <param name="logger">The <see cref="ILogger"/> to write to.</param>
    /// <param name="eventId">The event id associated with the log.</param>
    /// <param name="exception">The exception to log.</param>
    /// <param name="message">Format string of the log message in message template format. Example: <code>"User {0} logged in from {1}"</code></param>
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
    /// <param name="message">Format string of the log message in message template format. Example: <code>"User {0} logged in from {1}"</code></param>
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
    /// <param name="message">Format string of the log message in message template format. Example: <code>"User {0} logged in from {1}"</code></param>
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
    /// <param name="message">Format string of the log message in message template format. Example: <code>"User {0} logged in from {1}"</code></param>
    /// <param name="args">An object array that contains zero or more objects to format.</param>
    /// <example>logger.LogCritical("Processing request from {Address}", address)</example>
    [StringFormatMethod(nameof(message))]
    public static void LogCritical(this ILogger logger, string message, params object?[]? args)
    {
        logger.Log(LogLevel.Critical, message, args);
    }

    /// <summary>
    /// Formats and writes a critical log message.
    /// </summary>
    /// <param name="logger">The <see cref="ILogger"/> to write to.</param>
    /// <param name="eventId">The event id associated with the log.</param>
    /// <param name="exception">The exception to log.</param>
    /// <param name="message">Format string of the log message in message template format. Example: <code>"User {0} logged in from {1}"</code></param>
    /// <example>logger.LogCritical(0, exception, "Error while processing request from {Address}", address)</example>
    [StringFormatMethod(nameof(message))]
    public static void LogCritical(this ILogger logger, EventId eventId, Exception? exception, string message)
    {
        logger.Log(LogLevel.Critical, eventId, exception, message, Array.Empty<object>());
    }

    /// <summary>
    /// Formats and writes a critical log message.
    /// </summary>
    /// <param name="logger">The <see cref="ILogger"/> to write to.</param>
    /// <param name="eventId">The event id associated with the log.</param>
    /// <param name="message">Format string of the log message in message template format. Example: <code>"User {0} logged in from {1}"</code></param>
    /// <example>logger.LogCritical(0, "Processing request from {Address}", address)</example>
    [StringFormatMethod(nameof(message))]
    public static void LogCritical(this ILogger logger, EventId eventId, string message)
    {
        logger.Log(LogLevel.Critical, eventId, message, Array.Empty<object>());
    }

    /// <summary>
    /// Formats and writes a critical log message.
    /// </summary>
    /// <param name="logger">The <see cref="ILogger"/> to write to.</param>
    /// <param name="exception">The exception to log.</param>
    /// <param name="message">Format string of the log message in message template format. Example: <code>"User {0} logged in from {1}"</code></param>
    /// <example>logger.LogCritical(exception, "Error while processing request from {Address}", address)</example>
    [StringFormatMethod(nameof(message))]
    public static void LogCritical(this ILogger logger, Exception? exception, string message)
    {
        logger.Log(LogLevel.Critical, exception, message, Array.Empty<object>());
    }

    /// <summary>
    /// Formats and writes a critical log message.
    /// </summary>
    /// <param name="logger">The <see cref="ILogger"/> to write to.</param>
    /// <param name="message">Format string of the log message in message template format. Example: <code>"User {0} logged in from {1}"</code></param>
    /// <example>logger.LogCritical("Processing request from {Address}", address)</example>
    [StringFormatMethod(nameof(message))]
    public static void LogCritical(this ILogger logger, string message)
    {
        logger.Log(LogLevel.Critical, message, Array.Empty<object>());
    }

    /// <summary>
    /// Formats and writes a critical log message.
    /// </summary>
    /// <param name="logger">The <see cref="ILogger"/> to write to.</param>
    /// <param name="eventId">The event id associated with the log.</param>
    /// <param name="exception">The exception to log.</param>
    /// <param name="message">Format string of the log message in message template format. Example: <code>"User {0} logged in from {1}"</code></param>
    /// <example>logger.LogCritical(0, exception, "Error while processing request from {Address}", address)</example>
    [StringFormatMethod(nameof(message))]
    public static void LogCritical(this ILogger logger, EventId eventId, Exception? exception, string message, object? arg1)
    {
        logger.Log(LogLevel.Critical, eventId, exception, message, arg1);
    }

    /// <summary>
    /// Formats and writes a critical log message.
    /// </summary>
    /// <param name="logger">The <see cref="ILogger"/> to write to.</param>
    /// <param name="eventId">The event id associated with the log.</param>
    /// <param name="message">Format string of the log message in message template format. Example: <code>"User {0} logged in from {1}"</code></param>
    /// <example>logger.LogCritical(0, "Processing request from {Address}", address)</example>
    [StringFormatMethod(nameof(message))]
    public static void LogCritical(this ILogger logger, EventId eventId, string message, object? arg1)
    {
        logger.Log(LogLevel.Critical, eventId, message, arg1);
    }

    /// <summary>
    /// Formats and writes a critical log message.
    /// </summary>
    /// <param name="logger">The <see cref="ILogger"/> to write to.</param>
    /// <param name="exception">The exception to log.</param>
    /// <param name="message">Format string of the log message in message template format. Example: <code>"User {0} logged in from {1}"</code></param>
    /// <example>logger.LogCritical(exception, "Error while processing request from {Address}", address)</example>
    [StringFormatMethod(nameof(message))]
    public static void LogCritical(this ILogger logger, Exception? exception, string message, object? arg1)
    {
        logger.Log(LogLevel.Critical, exception, message, arg1);
    }

    /// <summary>
    /// Formats and writes a critical log message.
    /// </summary>
    /// <param name="logger">The <see cref="ILogger"/> to write to.</param>
    /// <param name="message">Format string of the log message in message template format. Example: <code>"User {0} logged in from {1}"</code></param>
    /// <example>logger.LogCritical("Processing request from {Address}", address)</example>
    [StringFormatMethod(nameof(message))]
    public static void LogCritical(this ILogger logger, string message, object? arg1)
    {
        logger.Log(LogLevel.Critical, message, arg1);
    }

    /// <summary>
    /// Formats and writes a critical log message.
    /// </summary>
    /// <param name="logger">The <see cref="ILogger"/> to write to.</param>
    /// <param name="eventId">The event id associated with the log.</param>
    /// <param name="exception">The exception to log.</param>
    /// <param name="message">Format string of the log message in message template format. Example: <code>"User {0} logged in from {1}"</code></param>
    /// <example>logger.LogCritical(0, exception, "Error while processing request from {Address}", address)</example>
    [StringFormatMethod(nameof(message))]
    public static void LogCritical(this ILogger logger, EventId eventId, Exception? exception, string message, object? arg1, object? arg2)
    {
        logger.Log(LogLevel.Critical, eventId, exception, message, arg1, arg2);
    }

    /// <summary>
    /// Formats and writes a critical log message.
    /// </summary>
    /// <param name="logger">The <see cref="ILogger"/> to write to.</param>
    /// <param name="eventId">The event id associated with the log.</param>
    /// <param name="message">Format string of the log message in message template format. Example: <code>"User {0} logged in from {1}"</code></param>
    /// <example>logger.LogCritical(0, "Processing request from {Address}", address)</example>
    [StringFormatMethod(nameof(message))]
    public static void LogCritical(this ILogger logger, EventId eventId, string message, object? arg1, object? arg2)
    {
        logger.Log(LogLevel.Critical, eventId, message, arg1, arg2);
    }

    /// <summary>
    /// Formats and writes a critical log message.
    /// </summary>
    /// <param name="logger">The <see cref="ILogger"/> to write to.</param>
    /// <param name="exception">The exception to log.</param>
    /// <param name="message">Format string of the log message in message template format. Example: <code>"User {0} logged in from {1}"</code></param>
    /// <example>logger.LogCritical(exception, "Error while processing request from {Address}", address)</example>
    [StringFormatMethod(nameof(message))]
    public static void LogCritical(this ILogger logger, Exception? exception, string message, object? arg1, object? arg2)
    {
        logger.Log(LogLevel.Critical, exception, message, arg1, arg2);
    }

    /// <summary>
    /// Formats and writes a critical log message.
    /// </summary>
    /// <param name="logger">The <see cref="ILogger"/> to write to.</param>
    /// <param name="message">Format string of the log message in message template format. Example: <code>"User {0} logged in from {1}"</code></param>
    /// <example>logger.LogCritical("Processing request from {Address}", address)</example>
    [StringFormatMethod(nameof(message))]
    public static void LogCritical(this ILogger logger, string message, object? arg1, object? arg2)
    {
        logger.Log(LogLevel.Critical, message, arg1, arg2);
    }

    /// <summary>
    /// Formats and writes a critical log message.
    /// </summary>
    /// <param name="logger">The <see cref="ILogger"/> to write to.</param>
    /// <param name="eventId">The event id associated with the log.</param>
    /// <param name="exception">The exception to log.</param>
    /// <param name="message">Format string of the log message in message template format. Example: <code>"User {0} logged in from {1}"</code></param>
    /// <example>logger.LogCritical(0, exception, "Error while processing request from {Address}", address)</example>
    [StringFormatMethod(nameof(message))]
    public static void LogCritical(this ILogger logger, EventId eventId, Exception? exception, string message, object? arg1, object? arg2, object? arg3)
    {
        logger.Log(LogLevel.Critical, eventId, exception, message, arg1, arg2, arg3);
    }

    /// <summary>
    /// Formats and writes a critical log message.
    /// </summary>
    /// <param name="logger">The <see cref="ILogger"/> to write to.</param>
    /// <param name="eventId">The event id associated with the log.</param>
    /// <param name="message">Format string of the log message in message template format. Example: <code>"User {0} logged in from {1}"</code></param>
    /// <example>logger.LogCritical(0, "Processing request from {Address}", address)</example>
    [StringFormatMethod(nameof(message))]
    public static void LogCritical(this ILogger logger, EventId eventId, string message, object? arg1, object? arg2, object? arg3)
    {
        logger.Log(LogLevel.Critical, eventId, message, arg1, arg2, arg3);
    }

    /// <summary>
    /// Formats and writes a critical log message.
    /// </summary>
    /// <param name="logger">The <see cref="ILogger"/> to write to.</param>
    /// <param name="exception">The exception to log.</param>
    /// <param name="message">Format string of the log message in message template format. Example: <code>"User {0} logged in from {1}"</code></param>
    /// <example>logger.LogCritical(exception, "Error while processing request from {Address}", address)</example>
    [StringFormatMethod(nameof(message))]
    public static void LogCritical(this ILogger logger, Exception? exception, string message, object? arg1, object? arg2, object? arg3)
    {
        logger.Log(LogLevel.Critical, exception, message, arg1, arg2, arg3);
    }

    /// <summary>
    /// Formats and writes a critical log message.
    /// </summary>
    /// <param name="logger">The <see cref="ILogger"/> to write to.</param>
    /// <param name="message">Format string of the log message in message template format. Example: <code>"User {0} logged in from {1}"</code></param>
    /// <example>logger.LogCritical("Processing request from {Address}", address)</example>
    [StringFormatMethod(nameof(message))]
    public static void LogCritical(this ILogger logger, string message, object? arg1, object? arg2, object? arg3)
    {
        logger.Log(LogLevel.Critical, message, arg1, arg2, arg3);
    }

    /// <summary>
    /// Formats and writes a critical log message.
    /// </summary>
    /// <param name="logger">The <see cref="ILogger"/> to write to.</param>
    /// <param name="eventId">The event id associated with the log.</param>
    /// <param name="exception">The exception to log.</param>
    /// <param name="message">Format string of the log message in message template format. Example: <code>"User {0} logged in from {1}"</code></param>
    /// <example>logger.LogCritical(0, exception, "Error while processing request from {Address}", address)</example>
    [StringFormatMethod(nameof(message))]
    public static void LogCritical(this ILogger logger, EventId eventId, Exception? exception, string message, object? arg1, object? arg2, object? arg3, object? arg4)
    {
        logger.Log(LogLevel.Critical, eventId, exception, message, arg1, arg2, arg3, arg4);
    }

    /// <summary>
    /// Formats and writes a critical log message.
    /// </summary>
    /// <param name="logger">The <see cref="ILogger"/> to write to.</param>
    /// <param name="eventId">The event id associated with the log.</param>
    /// <param name="message">Format string of the log message in message template format. Example: <code>"User {0} logged in from {1}"</code></param>
    /// <example>logger.LogCritical(0, "Processing request from {Address}", address)</example>
    [StringFormatMethod(nameof(message))]
    public static void LogCritical(this ILogger logger, EventId eventId, string message, object? arg1, object? arg2, object? arg3, object? arg4)
    {
        logger.Log(LogLevel.Critical, eventId, message, arg1, arg2, arg3, arg4);
    }

    /// <summary>
    /// Formats and writes a critical log message.
    /// </summary>
    /// <param name="logger">The <see cref="ILogger"/> to write to.</param>
    /// <param name="exception">The exception to log.</param>
    /// <param name="message">Format string of the log message in message template format. Example: <code>"User {0} logged in from {1}"</code></param>
    /// <example>logger.LogCritical(exception, "Error while processing request from {Address}", address)</example>
    [StringFormatMethod(nameof(message))]
    public static void LogCritical(this ILogger logger, Exception? exception, string message, object? arg1, object? arg2, object? arg3, object? arg4)
    {
        logger.Log(LogLevel.Critical, exception, message, arg1, arg2, arg3, arg4);
    }

    /// <summary>
    /// Formats and writes a critical log message.
    /// </summary>
    /// <param name="logger">The <see cref="ILogger"/> to write to.</param>
    /// <param name="message">Format string of the log message in message template format. Example: <code>"User {0} logged in from {1}"</code></param>
    /// <example>logger.LogCritical("Processing request from {Address}", address)</example>
    [StringFormatMethod(nameof(message))]
    public static void LogCritical(this ILogger logger, string message, object? arg1, object? arg2, object? arg3, object? arg4)
    {
        logger.Log(LogLevel.Critical, message, arg1, arg2, arg3, arg4);
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

        args ??= Array.Empty<object?>();
        if (WarfareModule.IsActive)
        {
            logger.Log(logLevel, eventId, new WarfareFormattedLogValues(message, args), exception, MessageFormatter);
        }
        else
        {
            LoggerExtensions.Log(logger, logLevel, eventId, exception, message, args);
        }
    }

    /// <summary>
    /// Formats and writes a log message at the specified log level.
    /// </summary>
    /// <param name="logger">The <see cref="ILogger"/> to write to.</param>
    /// <param name="logLevel">Entry will be written on this level.</param>
    /// <param name="message">Format string of the log message.</param>
    [StringFormatMethod(nameof(message))]
    public static void Log(this ILogger logger, LogLevel logLevel, string message)
    {
        logger.Log(logLevel, 0, null, message, Array.Empty<object>());
    }

    /// <summary>
    /// Formats and writes a log message at the specified log level.
    /// </summary>
    /// <param name="logger">The <see cref="ILogger"/> to write to.</param>
    /// <param name="logLevel">Entry will be written on this level.</param>
    /// <param name="eventId">The event id associated with the log.</param>
    /// <param name="message">Format string of the log message.</param>
    [StringFormatMethod(nameof(message))]
    public static void Log(this ILogger logger, LogLevel logLevel, EventId eventId, string message)
    {
        logger.Log(logLevel, eventId, null, message, Array.Empty<object>());
    }

    /// <summary>
    /// Formats and writes a log message at the specified log level.
    /// </summary>
    /// <param name="logger">The <see cref="ILogger"/> to write to.</param>
    /// <param name="logLevel">Entry will be written on this level.</param>
    /// <param name="exception">The exception to log.</param>
    /// <param name="message">Format string of the log message.</param>
    [StringFormatMethod(nameof(message))]
    public static void Log(this ILogger logger, LogLevel logLevel, Exception? exception, string message)
    {
        logger.Log(logLevel, 0, exception, message, Array.Empty<object>());
    }

    /// <summary>
    /// Formats and writes a log message at the specified log level.
    /// </summary>
    /// <param name="logger">The <see cref="ILogger"/> to write to.</param>
    /// <param name="logLevel">Entry will be written on this level.</param>
    /// <param name="eventId">The event id associated with the log.</param>
    /// <param name="exception">The exception to log.</param>
    /// <param name="message">Format string of the log message.</param>
    [StringFormatMethod(nameof(message))]
    public static void Log(this ILogger logger, LogLevel logLevel, EventId eventId, Exception? exception, string message)
    {
        if (logger == null)
        {
            throw new ArgumentNullException(nameof(logger));
        }

        if (WarfareModule.IsActive)
        {
            logger.Log(logLevel, eventId, new WarfareFormattedLogValues(message, Array.Empty<object>()), exception, MessageFormatter);
        }
        else
        {
            LoggerExtensions.Log(logger, logLevel, eventId, exception, message, Array.Empty<object>());
        }
    }

    /// <summary>
    /// Formats and writes a log message at the specified log level.
    /// </summary>
    /// <param name="logger">The <see cref="ILogger"/> to write to.</param>
    /// <param name="logLevel">Entry will be written on this level.</param>
    /// <param name="message">Format string of the log message.</param>
    [StringFormatMethod(nameof(message))]
    public static void Log(this ILogger logger, LogLevel logLevel, string message, object? arg1)
    {
        logger.Log(logLevel, 0, null, message, arg1);
    }

    /// <summary>
    /// Formats and writes a log message at the specified log level.
    /// </summary>
    /// <param name="logger">The <see cref="ILogger"/> to write to.</param>
    /// <param name="logLevel">Entry will be written on this level.</param>
    /// <param name="eventId">The event id associated with the log.</param>
    /// <param name="message">Format string of the log message.</param>
    [StringFormatMethod(nameof(message))]
    public static void Log(this ILogger logger, LogLevel logLevel, EventId eventId, string message, object? arg1)
    {
        logger.Log(logLevel, eventId, null, message, arg1);
    }

    /// <summary>
    /// Formats and writes a log message at the specified log level.
    /// </summary>
    /// <param name="logger">The <see cref="ILogger"/> to write to.</param>
    /// <param name="logLevel">Entry will be written on this level.</param>
    /// <param name="exception">The exception to log.</param>
    /// <param name="message">Format string of the log message.</param>
    [StringFormatMethod(nameof(message))]
    public static void Log(this ILogger logger, LogLevel logLevel, Exception? exception, string message, object? arg1)
    {
        logger.Log(logLevel, 0, exception, message, arg1);
    }

    /// <summary>
    /// Formats and writes a log message at the specified log level.
    /// </summary>
    /// <param name="logger">The <see cref="ILogger"/> to write to.</param>
    /// <param name="logLevel">Entry will be written on this level.</param>
    /// <param name="eventId">The event id associated with the log.</param>
    /// <param name="exception">The exception to log.</param>
    /// <param name="message">Format string of the log message.</param>
    [StringFormatMethod(nameof(message))]
    public static void Log(this ILogger logger, LogLevel logLevel, EventId eventId, Exception? exception, string message, object? arg1)
    {
        if (logger == null)
        {
            throw new ArgumentNullException(nameof(logger));
        }

        if (WarfareModule.IsActive)
        {
            logger.Log(logLevel, eventId, new WarfareFormattedLogValues(message, arg1), exception, MessageFormatter);
        }
        else
        {
            LoggerExtensions.Log(logger, logLevel, eventId, exception, message, [ arg1 ]);
        }
    }

    /// <summary>
    /// Formats and writes a log message at the specified log level.
    /// </summary>
    /// <param name="logger">The <see cref="ILogger"/> to write to.</param>
    /// <param name="logLevel">Entry will be written on this level.</param>
    /// <param name="message">Format string of the log message.</param>
    [StringFormatMethod(nameof(message))]
    public static void Log(this ILogger logger, LogLevel logLevel, string message, object? arg1, object? arg2)
    {
        logger.Log(logLevel, 0, null, message, arg1, arg2);
    }

    /// <summary>
    /// Formats and writes a log message at the specified log level.
    /// </summary>
    /// <param name="logger">The <see cref="ILogger"/> to write to.</param>
    /// <param name="logLevel">Entry will be written on this level.</param>
    /// <param name="eventId">The event id associated with the log.</param>
    /// <param name="message">Format string of the log message.</param>
    [StringFormatMethod(nameof(message))]
    public static void Log(this ILogger logger, LogLevel logLevel, EventId eventId, string message, object? arg1, object? arg2)
    {
        logger.Log(logLevel, eventId, null, message, arg1, arg2);
    }

    /// <summary>
    /// Formats and writes a log message at the specified log level.
    /// </summary>
    /// <param name="logger">The <see cref="ILogger"/> to write to.</param>
    /// <param name="logLevel">Entry will be written on this level.</param>
    /// <param name="exception">The exception to log.</param>
    /// <param name="message">Format string of the log message.</param>
    [StringFormatMethod(nameof(message))]
    public static void Log(this ILogger logger, LogLevel logLevel, Exception? exception, string message, object? arg1, object? arg2)
    {
        logger.Log(logLevel, 0, exception, message, arg1, arg2);
    }

    /// <summary>
    /// Formats and writes a log message at the specified log level.
    /// </summary>
    /// <param name="logger">The <see cref="ILogger"/> to write to.</param>
    /// <param name="logLevel">Entry will be written on this level.</param>
    /// <param name="eventId">The event id associated with the log.</param>
    /// <param name="exception">The exception to log.</param>
    /// <param name="message">Format string of the log message.</param>
    [StringFormatMethod(nameof(message))]
    public static void Log(this ILogger logger, LogLevel logLevel, EventId eventId, Exception? exception, string message, object? arg1, object? arg2)
    {
        if (logger == null)
        {
            throw new ArgumentNullException(nameof(logger));
        }

        if (WarfareModule.IsActive)
        {
            logger.Log(logLevel, eventId, new WarfareFormattedLogValues(message, arg1, arg2), exception, MessageFormatter);
        }
        else
        {
            LoggerExtensions.Log(logger, logLevel, eventId, exception, message, [ arg1, arg2 ]);
        }
    }

    /// <summary>
    /// Formats and writes a log message at the specified log level.
    /// </summary>
    /// <param name="logger">The <see cref="ILogger"/> to write to.</param>
    /// <param name="logLevel">Entry will be written on this level.</param>
    /// <param name="message">Format string of the log message.</param>
    [StringFormatMethod(nameof(message))]
    public static void Log(this ILogger logger, LogLevel logLevel, string message, object? arg1, object? arg2, object? arg3)
    {
        logger.Log(logLevel, 0, null, message, arg1, arg2, arg3);
    }

    /// <summary>
    /// Formats and writes a log message at the specified log level.
    /// </summary>
    /// <param name="logger">The <see cref="ILogger"/> to write to.</param>
    /// <param name="logLevel">Entry will be written on this level.</param>
    /// <param name="eventId">The event id associated with the log.</param>
    /// <param name="message">Format string of the log message.</param>
    [StringFormatMethod(nameof(message))]
    public static void Log(this ILogger logger, LogLevel logLevel, EventId eventId, string message, object? arg1, object? arg2, object? arg3)
    {
        logger.Log(logLevel, eventId, null, message, arg1, arg2, arg3);
    }

    /// <summary>
    /// Formats and writes a log message at the specified log level.
    /// </summary>
    /// <param name="logger">The <see cref="ILogger"/> to write to.</param>
    /// <param name="logLevel">Entry will be written on this level.</param>
    /// <param name="exception">The exception to log.</param>
    /// <param name="message">Format string of the log message.</param>
    [StringFormatMethod(nameof(message))]
    public static void Log(this ILogger logger, LogLevel logLevel, Exception? exception, string message, object? arg1, object? arg2, object? arg3)
    {
        logger.Log(logLevel, 0, exception, message, arg1, arg2, arg3);
    }

    /// <summary>
    /// Formats and writes a log message at the specified log level.
    /// </summary>
    /// <param name="logger">The <see cref="ILogger"/> to write to.</param>
    /// <param name="logLevel">Entry will be written on this level.</param>
    /// <param name="eventId">The event id associated with the log.</param>
    /// <param name="exception">The exception to log.</param>
    /// <param name="message">Format string of the log message.</param>
    [StringFormatMethod(nameof(message))]
    public static void Log(this ILogger logger, LogLevel logLevel, EventId eventId, Exception? exception, string message, object? arg1, object? arg2, object? arg3)
    {
        if (logger == null)
        {
            throw new ArgumentNullException(nameof(logger));
        }

        if (WarfareModule.IsActive)
        {
            logger.Log(logLevel, eventId, new WarfareFormattedLogValues(message, arg1, arg2, arg3), exception, MessageFormatter);
        }
        else
        {
            LoggerExtensions.Log(logger, logLevel, eventId, exception, message, [ arg1, arg2, arg3 ]);
        }
    }

    /// <summary>
    /// Formats and writes a log message at the specified log level.
    /// </summary>
    /// <param name="logger">The <see cref="ILogger"/> to write to.</param>
    /// <param name="logLevel">Entry will be written on this level.</param>
    /// <param name="message">Format string of the log message.</param>
    [StringFormatMethod(nameof(message))]
    public static void Log(this ILogger logger, LogLevel logLevel, string message, object? arg1, object? arg2, object? arg3, object? arg4)
    {
        logger.Log(logLevel, 0, null, message, arg1, arg2, arg3, arg4);
    }

    /// <summary>
    /// Formats and writes a log message at the specified log level.
    /// </summary>
    /// <param name="logger">The <see cref="ILogger"/> to write to.</param>
    /// <param name="logLevel">Entry will be written on this level.</param>
    /// <param name="eventId">The event id associated with the log.</param>
    /// <param name="message">Format string of the log message.</param>
    [StringFormatMethod(nameof(message))]
    public static void Log(this ILogger logger, LogLevel logLevel, EventId eventId, string message, object? arg1, object? arg2, object? arg3, object? arg4)
    {
        logger.Log(logLevel, eventId, null, message, arg1, arg2, arg3, arg4);
    }

    /// <summary>
    /// Formats and writes a log message at the specified log level.
    /// </summary>
    /// <param name="logger">The <see cref="ILogger"/> to write to.</param>
    /// <param name="logLevel">Entry will be written on this level.</param>
    /// <param name="exception">The exception to log.</param>
    /// <param name="message">Format string of the log message.</param>
    [StringFormatMethod(nameof(message))]
    public static void Log(this ILogger logger, LogLevel logLevel, Exception? exception, string message, object? arg1, object? arg2, object? arg3, object? arg4)
    {
        logger.Log(logLevel, 0, exception, message, arg1, arg2, arg3, arg4);
    }

    /// <summary>
    /// Formats and writes a log message at the specified log level.
    /// </summary>
    /// <param name="logger">The <see cref="ILogger"/> to write to.</param>
    /// <param name="logLevel">Entry will be written on this level.</param>
    /// <param name="eventId">The event id associated with the log.</param>
    /// <param name="exception">The exception to log.</param>
    /// <param name="message">Format string of the log message.</param>
    [StringFormatMethod(nameof(message))]
    public static void Log(this ILogger logger, LogLevel logLevel, EventId eventId, Exception? exception, string message, object? arg1, object? arg2, object? arg3, object? arg4)
    {
        if (logger == null)
        {
            throw new ArgumentNullException(nameof(logger));
        }


        if (WarfareModule.IsActive)
        {
            logger.Log(logLevel, eventId, new WarfareFormattedLogValues(message, arg1, arg2, arg3, arg4), exception, MessageFormatter);
        }
        else
        {
            LoggerExtensions.Log(logger, logLevel, eventId, exception, message, [ arg1, arg2, arg3, arg4 ]);
        }
    }

    private static readonly Func<WarfareFormattedLogValues, Exception?, string> MessageFormatter = MessageFormatterMtd;
    private static string MessageFormatterMtd(WarfareFormattedLogValues state, Exception? error)
    {
        return state.ToString();
    }
}