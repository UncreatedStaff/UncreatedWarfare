using DanielWillett.ReflectionTools;
using SDG.Framework.Utilities;
using StackCleaner;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reflection;
using Uncreated.Warfare.Interaction.Commands;
using Uncreated.Warfare.Translations;
using Uncreated.Warfare.Util;
using Debug = System.Diagnostics.Debug;

namespace Uncreated.Warfare.Logging;
public class WarfareLoggerProvider : ILoggerProvider
{
    private readonly ITranslationValueFormatter? _formatter;
    private readonly ConcurrentQueue<LogMessage> _messages;

    internal StackTraceCleaner StackCleaner;

    private static readonly StaticGetter<LogFile> GetDebugLog = Accessor.GenerateStaticGetter<Logs, LogFile>("debugLog", throwOnError: true)!;

    private static readonly Action<string> CallLogInfoIntl =
        (Action<string>)typeof(CommandWindow)
            .GetMethod("internalLogInformation", BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public)!
            .CreateDelegate(typeof(Action<string>), Dedicator.commandWindow);

    private static readonly Action<string> CallLogWarningIntl =
        (Action<string>)typeof(CommandWindow)
            .GetMethod("internalLogWarning", BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public)!
            .CreateDelegate(typeof(Action<string>), Dedicator.commandWindow);

    private static readonly Action<string> CallLogErrorIntl =
        (Action<string>)typeof(CommandWindow)
            .GetMethod("internalLogError", BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public)!
            .CreateDelegate(typeof(Action<string>), Dedicator.commandWindow);

    public WarfareLoggerProvider(ITranslationValueFormatter? formatter)
    {
        _formatter = formatter;
        _messages = new ConcurrentQueue<LogMessage>();

        StackCleanerConfiguration config = new StackCleanerConfiguration
        {
            ColorFormatting = StackColorFormatType.ExtendedANSIColor,
#if NETSTANDARD || NETFRAMEWORK
            Colors = UnityColor32Config.Default,
#else
            Colors = Color32Config.Default,
#endif
            IncludeNamespaces = false,
            IncludeILOffset = true,
            IncludeLineData = true,
            IncludeFileData = true,
            IncludeAssemblyData = false,
            IncludeSourceData = true,
            Locale = CultureInfo.InvariantCulture,
            PutSourceDataOnNewLine = true,
        };

        // add UniTask types to hidden types
        List<Type> hiddenTypes = new List<Type>(config.GetHiddenTypes())
        {
            typeof(UniTask)
        };

        Assembly uniTaskAsm = typeof(UniTask).Assembly;
        Type? type = uniTaskAsm.GetType("Cysharp.Threading.Tasks.EnumeratorAsyncExtensions+EnumeratorPromise", false, false);
        if (type != null)
            hiddenTypes.Add(type);

        type = uniTaskAsm.GetType("Cysharp.Threading.Tasks.CompilerServices.AsyncUniTask`1", false, false);
        if (type != null)
            hiddenTypes.Add(type);

        type = uniTaskAsm.GetType("Cysharp.Threading.Tasks.CompilerServices.AsyncUniTask`2", false, false);
        if (type != null)
            hiddenTypes.Add(type);

        foreach (Type baseType in typeof(UniTask)
                     .GetNestedTypes(BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic)
                     .Where(x => x.Name.IndexOf("promise", StringComparison.OrdinalIgnoreCase) != -1))
        {
            hiddenTypes.Add(baseType);
        }

        config.HiddenTypes = hiddenTypes;

        StackCleaner = new StackTraceCleaner(config);
    }

    /// <summary>
    /// Log something to the console and file.
    /// </summary>
    /// <exception cref="GameThreadException"/>
    public static void WriteToLogRaw(LogLevel logLevel, string text, string? unformattedLog)
    {
        GameThread.AssertCurrent();

        LogIntl(logLevel, text, unformattedLog);
    }

    private static void LogIntl(LogLevel logLevel, string text, string? unformattedLog)
    {
        if (!WarfareModule.IsActive)
        {
            Console.WriteLine(text);
            Debug.WriteLine(text);
            return;
        }

        VanillaCommandListener.IsLogging = true;
        try
        {
            switch (logLevel)
            {
                default:
                    CallLogInfoIntl.Invoke(text);
                    break;

                case LogLevel.Warning:
                    CallLogWarningIntl.Invoke(text);
                    break;

                case LogLevel.Critical:
                case LogLevel.Error:
                    CallLogErrorIntl.Invoke(text);
                    break;
            }
        }
        finally
        {
            VanillaCommandListener.IsLogging = false;
        }

        if (unformattedLog == null)
            return;

        try
        {
            GetDebugLog().writeLine(unformattedLog);
        }
        catch (Exception ex)
        {
            CallLogErrorIntl("Failed to write error: " + ex);
        }
    }

    internal void QueueOutput(LogLevel logLevel, string text, string? unformattedLog)
    {
        if (GameThread.IsCurrent)
        {
            Update();
            LogIntl(logLevel, text, unformattedLog);
            return;
        }

        _messages.Enqueue(new LogMessage
        {
            Level = logLevel,
            Text = text,
            Unformatted = unformattedLog
        });
    }

    private void Update()
    {
        while (_messages.TryDequeue(out LogMessage message))
        {
            LogIntl(message.Level, message.Text, message.Unformatted);
        }
    }

    public ILogger CreateLogger(string categoryName)
    {
        return new WarfareLogger(categoryName, this, _formatter);
    }

    void IDisposable.Dispose()
    {
        TimeUtility.updated -= Update;
        if (GameThread.IsCurrent)
            Update();
        else
        {
            UniTask.Create(async () =>
            {
                await UniTask.SwitchToMainThread();
                Update();
            });
        }
    }

    private struct LogMessage
    {
        public string Text;
        public LogLevel Level;
        public string? Unformatted;
    }
}