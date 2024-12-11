using Uncreated.Warfare.Interaction.Commands;
using Uncreated.Warfare.Players;
using Uncreated.Warfare.Players.Management;

namespace Uncreated.Warfare.Commands;

[Command("clearcooldowns", "cc"), SubCommandOf(typeof(WarfareDevCommand))]
internal sealed class DebugClearCooldownsCommand : IExecutableCommand
{
    private readonly CooldownManager _cooldownManager;
    private readonly IPlayerService _playerService;
    public required CommandContext Context { get; init; }

    public DebugClearCooldownsCommand(CooldownManager cooldownManager, IPlayerService playerService)
    {
        _cooldownManager = cooldownManager;
        _playerService = playerService;
    }

    public UniTask ExecuteAsync(CancellationToken token)
    {
        if (Context.Caller.IsTerminal && Context.ArgumentCount == 0)
        {
            foreach (WarfarePlayer pl in _playerService.OnlinePlayers)
                _cooldownManager.RemoveCooldown(pl);
        }
        else if (Context.TryGet(0, out _, out WarfarePlayer? onlinePlayer, remainder: true))
        {
            if (onlinePlayer == null)
                throw Context.SendPlayerNotFound();

            _cooldownManager.RemoveCooldown(onlinePlayer);
        }
        else
        {
            Context.AssertRanByPlayer();

            _cooldownManager.RemoveCooldown(Context.Player);
        }

        Context.ReplyString("Cleared any cooldowns.");
        return UniTask.CompletedTask;
    }
}