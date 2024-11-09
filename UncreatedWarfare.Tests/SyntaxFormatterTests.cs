using Autofac;
using Microsoft.Extensions.Logging;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Threading;
using System.Threading.Tasks;
using Uncreated.Warfare.Interaction.Commands;
using Uncreated.Warfare.Interaction.Commands.Syntax;

namespace Uncreated.Warfare.Tests;
public class SyntaxFormatterTests
{
    private List<CommandInfo> _commands;
    [SetUp]
    public void SetupCommands()
    {
        LoggingFormattingTests logging = new LoggingFormattingTests();
        logging.Setup();

        _commands = CommandDispatcher.DiscoverAssemblyCommands(logging.Container.Resolve<ILogger>(), null);
    }

    [Test]
    public async Task TestKitCommand()
    {
        CommandInfo command = _commands.Find(x => x.CommandName == "kit");

        ISyntaxWriter syntaxWriter = new PlainTextSyntaxWriter(CultureInfo.InvariantCulture);
        CommandSyntaxFormatter formatter = new CommandSyntaxFormatter(syntaxWriter, null, null);

        CommandSyntaxFormatter.SyntaxStringInfo syntaxInfo = await formatter.GetSyntaxString(command, [ "give" ], null, null, null, CancellationToken.None);

        Console.WriteLine(syntaxInfo.Syntax);

        string desc = formatter.GetRichDescription(command, syntaxInfo.TargetParameter, syntaxInfo.TargetFlag, syntaxWriter, null);

        Console.WriteLine(desc ?? "NULL");
    }
}
