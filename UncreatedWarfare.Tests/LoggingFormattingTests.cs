using Autofac;
using Autofac.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Configuration.Memory;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using Uncreated.Warfare.Logging.Formatting;
using Uncreated.Warfare.Players.Management;
using Uncreated.Warfare.Translations;
using Uncreated.Warfare.Translations.Languages;
using Uncreated.Warfare.Util;

namespace Uncreated.Warfare.Tests;
public class LoggingFormattingTests
{
    private ITranslationValueFormatter _formatter;
    private IContainer _container;

    [SetUp]
    public void Setup()
    {
        ContainerBuilder bldr = new ContainerBuilder();
        bldr.RegisterType<TranslationValueFormatter>()
            .As<ITranslationValueFormatter>()
            .SingleInstance();

        bldr.RegisterInstance(new NullLanguageDataStore())
            .As<ILanguageDataStore>()
            .SingleInstance();

        bldr.RegisterType<NullPlayerService>()
            .As<IPlayerService>()
            .SingleInstance();

        bldr.RegisterInstance(new LoggerFactory([ NullLoggerProvider.Instance ], new LoggerFilterOptions()))
            .As<ILoggerFactory>()
            .OwnedByLifetimeScope();

        bldr.RegisterGeneric(typeof(Logger<>)).As(typeof(ILogger<>));
        bldr.RegisterType(Type.GetType("Microsoft.Extensions.Logging.Logger, Microsoft.Extensions.Logging")!).As<ILogger>();

        bldr.RegisterType<LanguageService>()
            .SingleInstance();

        bldr.RegisterInstance(new ConfigurationBuilder().Add(new MemoryConfigurationSource
        {
            InitialData =
            [
                new KeyValuePair<string, string>("default_language", "en-us"),
                new KeyValuePair<string, string>("logging:terminal_coloring", "ExtendedANSIColor" /*"None"*/),
                new KeyValuePair<string, string>("translations:enum_formatter_type", "Uncreated.Warfare.Translations.ValueFormatters.ToStringEnumValueFormatter`1, Uncreated.Warfare")
            ]
        }).Build()).As<IConfiguration>();

        bldr.RegisterType<TranslationService>()
            .As<ITranslationService>()
            .SingleInstance();

        bldr.Populate([ ]);

        _container = bldr.Build();

        _formatter = _container.Resolve<ITranslationValueFormatter>();
    }

    [Test]
    public void TempTextConverter()
    {
        Console.WriteLine(TerminalColorHelper.WrapMessageWithTerminalColorSequence(ConsoleColor.Red, "CRT", true));
        Console.WriteLine(TerminalColorHelper.WrapMessageWithTerminalColorSequence(ConsoleColor.DarkRed, "ERR", true));
        Console.WriteLine(TerminalColorHelper.WrapMessageWithTerminalColorSequence(ConsoleColor.DarkYellow, "WRN", true));
        Console.WriteLine(TerminalColorHelper.WrapMessageWithTerminalColorSequence(ConsoleColor.DarkCyan, "INF", true));
        Console.WriteLine(TerminalColorHelper.WrapMessageWithTerminalColorSequence(ConsoleColor.Gray, "DBG", true));
        Console.WriteLine(TerminalColorHelper.WrapMessageWithTerminalColorSequence(ConsoleColor.Gray, "TRC", true));

        Console.WriteLine(TerminalColorHelper.WrapMessageWithTerminalColorSequence(ConsoleColor.Yellow, "TRC", false));
        Console.WriteLine(TerminalColorHelper.WrapMessageWithTerminalColorSequence(ConsoleColor.Red, "TRC", false));

        Console.WriteLine(TerminalColorHelper.GetTerminalColorSequence(-2712187, false));
    }

    [Test]
    public void TestEmptyFormat()
    {
        WarfareFormattedLogValues values = new WarfareFormattedLogValues("message", Array.Empty<object>())
        {
            ValueFormatter = _formatter
        };

        string formatted = values.Format(true);

        Assert.That(formatted, Is.EqualTo("message"));
    }

    [Test]
    public void Test1ArgFormat()
    {
        const int arg = 8;

        WarfareFormattedLogValues values = new WarfareFormattedLogValues("7 {0} 9", arg)
        {
            ValueFormatter = _formatter
        };

        string formatted = values.Format(true);

        Assert.That(formatted, Is.EqualTo("7 \u001b[38;2;181;206;168m8\u001b[39m 9"));

        formatted = values.Format(false);

        Assert.That(formatted, Is.EqualTo("7 8 9"));
    }

    [Test]
    public void Test2ArgFormat()
    {
        const int arg1 = 7, arg2 = 8;

        WarfareFormattedLogValues values = new WarfareFormattedLogValues("{0} {1} 9", arg1, arg2)
        {
            ValueFormatter = _formatter
        };

        string formatted = values.Format(true);

        Assert.That(formatted, Is.EqualTo("\u001b[38;2;181;206;168m7\u001b[39m \u001b[38;2;181;206;168m8\u001b[39m 9"));

        formatted = values.Format(false);

        Assert.That(formatted, Is.EqualTo("7 8 9"));
    }

    [Test]
    public void Test3ArgFormat()
    {
        const int arg1 = 7, arg2 = 8, arg3 = 9;

        WarfareFormattedLogValues values = new WarfareFormattedLogValues("{0} {1} {2}", arg1, arg2, arg3)
        {
            ValueFormatter = _formatter
        };

        string formatted = values.Format(true);

        Assert.That(formatted, Is.EqualTo("\u001b[38;2;181;206;168m7\u001b[39m \u001b[38;2;181;206;168m8\u001b[39m \u001b[38;2;181;206;168m9\u001b[39m"));

        formatted = values.Format(false);

        Assert.That(formatted, Is.EqualTo("7 8 9"));
    }

    [Test]
    public void Test4ArgFormat()
    {
        const int arg1 = 7, arg2 = 8, arg3 = 9;
        const string arg4 = ":(";

        WarfareFormattedLogValues values = new WarfareFormattedLogValues("{0} {1} {2} {3}", arg1, arg2, arg3, arg4)
        {
            ValueFormatter = _formatter
        };

        string formatted = values.Format(true);

        Assert.That(formatted, Is.EqualTo("\u001b[38;2;181;206;168m7\u001b[39m \u001b[38;2;181;206;168m8\u001b[39m \u001b[38;2;181;206;168m9\u001b[39m \u001b[38;2;214;157;133m:(\u001b[39m"));

        formatted = values.Format(false);

        Assert.That(formatted, Is.EqualTo("7 8 9 :("));
    }

    [Test]
    public void Test5ArgFormat()
    {
        const int arg2 = 7, arg3 = 8, arg4 = 9;
        const string arg1 = ":)", arg5 = ":(";

        WarfareFormattedLogValues values = new WarfareFormattedLogValues("{0} {1} {2} {3} {4}", [ arg1, arg2, arg3, arg4, arg5 ])
        {
            ValueFormatter = _formatter
        };

        string formatted = values.Format(true);

        Assert.That(formatted, Is.EqualTo("\u001b[38;2;214;157;133m:)\u001b[39m \u001b[38;2;181;206;168m7\u001b[39m \u001b[38;2;181;206;168m8\u001b[39m \u001b[38;2;181;206;168m9\u001b[39m \u001b[38;2;214;157;133m:(\u001b[39m"));

        formatted = values.Format(false);

        Assert.That(formatted, Is.EqualTo(":) 7 8 9 :("));
    }
}
