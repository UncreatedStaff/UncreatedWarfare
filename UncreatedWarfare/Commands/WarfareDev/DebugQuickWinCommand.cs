using DanielWillett.ReflectionTools;
using Uncreated.Warfare.Interaction.Commands;
using Uncreated.Warfare.Layouts;
using Uncreated.Warfare.Layouts.Teams;
using Uncreated.Warfare.Players;

namespace Uncreated.Warfare.Commands;

[Command("quickwin", "nextphase"), SubCommandOf(typeof(WarfareDevCommand))]
internal sealed class DebugQuickWinCommand : IExecutableCommand
{
    private readonly Layout _layout;
    public required CommandContext Context { get; init; }

    public DebugQuickWinCommand(Layout layout)
    {
        _layout = layout;
    }

    public async UniTask ExecuteAsync(CancellationToken token)
    {
        Team? winner = (Context.Caller as WarfarePlayer)?.Team;
        if (winner == null || !winner.IsValid || Context.HasArgs(1))
        {
            if (Context.TryGetRange(0, out string? teamLookup))
            {
                winner = _layout.TeamManager.FindTeam(teamLookup);
            }

            if (winner == null || !winner.IsValid)
                throw Context.ReplyString("Winning team not found.");
        }

        _layout.Data[KnownLayoutDataKeys.WinnerTeam] = winner;

        if (_layout.IsActive)
        {
            await _layout.MoveToNextPhase(token);
            Context.ReplyString($"Moved to next phase: {(_layout.ActivePhase?.GetType() is { } t ? Accessor.ExceptionFormatter.Format(t) : "null")}");
        }
        else
        {
            Context.ReplyString("Not active.");
        }
    }
}