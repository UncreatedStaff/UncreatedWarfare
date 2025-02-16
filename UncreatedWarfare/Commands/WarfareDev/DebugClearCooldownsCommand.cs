using Uncreated.Warfare.Interaction.Commands;
using Uncreated.Warfare.Players;
using Uncreated.Warfare.Players.Cooldowns;
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

    public async UniTask ExecuteAsync(CancellationToken token)
    {
        if (Context.Caller.IsTerminal && Context.ArgumentCount == 0)
        {
            foreach (WarfarePlayer pl in _playerService.OnlinePlayers)
                _cooldownManager.RemoveCooldown(pl);

            throw Context.ReplyString("Cleared cooldowns for all players.");
        }

        (CSteamID? steam64, WarfarePlayer? onlinePlayer) = await Context.TryGetPlayer(0, remainder: true).ConfigureAwait(false);
        
        if (steam64.HasValue || Context.HasArgs(1))
        {
            if (onlinePlayer == null)
                throw Context.SendPlayerNotFound();

            _cooldownManager.RemoveCooldown(onlinePlayer);
            Context.ReplyString($"Cleared cooldowns for {onlinePlayer.Names.GetDisplayNameOrCharacterName()}.");
        }
        else
        {
            Context.AssertRanByPlayer();

            _cooldownManager.RemoveCooldown(Context.Player);
            Context.ReplyString("Cleared your cooldowns.");
        }
    }
}