using System;
using System.Text.Json;
using Uncreated.Warfare.Configuration;
using Uncreated.Warfare.Interaction.Commands;

namespace Uncreated.Warfare.Commands;

[Command("dumpcmd"), SubCommandOf(typeof(WarfareDevCommand))]
internal sealed class DebugDumpCommandCommand : IExecutableCommand
{
    private readonly CommandDispatcher _dispatcher;
    public required CommandContext Context { get; init; }

    public DebugDumpCommandCommand(CommandDispatcher dispatcher)
    {
        _dispatcher = dispatcher;
    }

    public UniTask ExecuteAsync(CancellationToken token)
    {
        Context.AssertRanByTerminal();

        if (!Context.TryGet(0, out string? commandName))
        {
            throw Context.SendHelp();
        }

        CommandInfo? foundCommand = _dispatcher.FindCommand(commandName);

        if (foundCommand == null)
        {
            throw Context.ReplyString("Command not found.");
        }

        Console.WriteLine(Environment.NewLine + JsonSerializer.Serialize(foundCommand, ConfigurationSettings.JsonSerializerSettings));
        Context.Defer();
        return UniTask.CompletedTask;
    }
}