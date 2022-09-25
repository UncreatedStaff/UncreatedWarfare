using System;
using Uncreated.Framework;
using Uncreated.Warfare.Commands.CommandSystem;
using Uncreated.Warfare.Gamemodes.Interfaces;
using Uncreated.Warfare.Squads;
using Command = Uncreated.Warfare.Commands.CommandSystem.Command;

namespace Uncreated.Warfare.Commands;
public class RallyCommand : Command
{
    private const string SYNTAX = "/rally";
    private const string HELP = "Deploys you to a rallypoint.";

    public RallyCommand() : base("rally", EAdminType.MEMBER) { }

    public override void Execute(CommandInteraction ctx)
    {
#if DEBUG
        using IDisposable profiler = ProfilingUtils.StartTracking();
#endif

        ctx.AssertGamemode<ISquads>();

        ctx.AssertRanByPlayer();

        ctx.AssertHelpCheck(0, SYNTAX + " - " + HELP);

        if (!UCWarfare.Config.EnableSquads)
        {
            ctx.Reply(T.SquadsDisabled);
            return;
        }

        if (ctx.Caller.Squad is null)
            throw ctx.Reply(T.RallyNotInSquad);

        if (!RallyManager.HasRally(ctx.Caller.Squad, out RallyPoint rallypoint) || !rallypoint.IsActive)
            throw ctx.Reply(ctx.Caller.IsSquadLeader() ? T.RallyNotActiveSL : T.RallyNotActive);

        if (ctx.MatchParameter(0, "cancel", "c", "abort"))
        {
            if (rallypoint.AwaitingPlayers.RemoveAll(p => p.Steam64 == ctx.CallerID) > 0)
            {
                rallypoint.ShowUIForPlayer(ctx.Caller);
                ctx.Reply(T.RallyAbort);
            }
            else throw ctx.Reply(T.RallyNotQueued);
        }
        else if (ctx.HasArgsExact(0))
        {
            if (rallypoint.timer <= 0)
                rallypoint.TeleportPlayer(ctx.Caller);
            else if (!rallypoint.AwaitingPlayers.Exists(p => p.Steam64 == ctx.CallerID))
            {
                rallypoint.AwaitingPlayers.Add(ctx.Caller);
                rallypoint.UpdateUIForAwaitingPlayers();
                ctx.Reply(T.RallyWait, rallypoint.timer);
            }
            else throw ctx.Reply(T.RallyAlreadyQueued);
        }
        else throw ctx.SendCorrectUsage(SYNTAX);
    }
}
