using System.Collections.Generic;
using Uncreated.Warfare.Interaction.Commands;
using Uncreated.Warfare.Players;
using Uncreated.Warfare.Players.Management;

namespace Uncreated.Warfare.Commands;

[Command("psearch"), SubCommandOf(typeof(WarfareDevCommand))]
internal sealed class DebugSearchPlayersCommand : IExecutableCommand
{
    private readonly IPlayerService _playerService;
    public required CommandContext Context { get; init; }

    public DebugSearchPlayersCommand(IPlayerService playerService)
    {
        _playerService = playerService;
    }

    public UniTask ExecuteAsync(CancellationToken token)
    {
        Context.AssertArgs(1);

        string searchTerm = Context.GetRange(0)!;

        List<WarfarePlayer> output = new List<WarfarePlayer>();

        int ct = _playerService.GetOnlinePlayers(searchTerm, output, Context.Culture, PlayerNameType.CharacterName);

        Context.ReplyString($"Found {ct} players. Check console.");

        Context.Logger.LogInformation("Found players: {0}.", ct);
        
        foreach (WarfarePlayer player in output)
        {
            Context.Logger.LogInformation(" - {0}", player);
        }

        return UniTask.CompletedTask;
    }
}