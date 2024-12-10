using Uncreated.Warfare.Interaction.Commands;
using Uncreated.Warfare.Players;
using Uncreated.Warfare.Sessions;

namespace Uncreated.Warfare.Commands;

[Command("newsession", "ns"), SubCommandOf(typeof(WarfareDevCommand))]
internal sealed class DebugNewSessionCommand : IExecutableCommand
{
    private readonly SessionManager _sessionManager;
    public required CommandContext Context { get; init; }

    public DebugNewSessionCommand(SessionManager sessionManager)
    {
        _sessionManager = sessionManager;
    }

    public async UniTask ExecuteAsync(CancellationToken token)
    {
        if (Context.Caller is WarfarePlayer pl && Context.MatchParameter(0, "addevent", "ae"))
        {
            Interlocked.Increment(ref pl.CurrentSession.EventCount);
            throw Context.ReplyString("Incremented event count for session.");
        }

        if (Context.Caller.IsTerminal && Context.ArgumentCount == 0)
        {
            await _sessionManager.StartNewSessionForAllPlayers(false, token);
        }
        else if (Context.TryGet(0, out _, out WarfarePlayer? onlinePlayer, remainder: true))
        {
            if (onlinePlayer == null)
                throw Context.SendPlayerNotFound();

            await _sessionManager.StartNewSession(onlinePlayer, false, token);
        }
        else
        {
            Context.AssertRanByPlayer();

            await _sessionManager.StartNewSession(Context.Player, false, token);
        }

        Context.ReplyString("Started new session.");
    }
}