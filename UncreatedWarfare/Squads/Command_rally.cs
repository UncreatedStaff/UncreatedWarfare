using SDG.Unturned;
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

        if (ctx.Caller.Squad is null || !ctx.Caller.IsSquadLeader())
            throw ctx.Reply(T.RallyNotSquadleader);

        var rallypoint = ctx.Caller.Squad.RallyPoint;

        if (rallypoint == null || !rallypoint.IsActive)
            throw ctx.Reply(T.RallyNotActiveSL);

        if (ctx.MatchParameter(0, "cancel", "c", "abort"))
        {
            if (!ctx.Caller.IsSquadLeader())
                throw ctx.Reply(T.RallyNoCancelPerm);

            if (rallypoint.IsDeploying)
            {
                rallypoint.AwaitingPlayers.Clear();
                rallypoint.ShowUIForPlayer(ctx.Caller);
                ctx.Reply(T.RallyCancel);
            }
            else throw ctx.Reply(T.RallyNoCancel);
        }
        else if (ctx.MatchParameter(0, "deny", "d"))
        {
            if (rallypoint.IsDeploying)
            {
                rallypoint.AwaitingPlayers.RemoveAll(p => p.Steam64 == ctx.CallerID);
                rallypoint.ShowUIForPlayer(ctx.Caller);
                ctx.Reply(T.RallyCancel);
            }
            else throw ctx.Reply(T.RallyNoDeny);
        }
        else if (ctx.HasArgsExact(0))
        {
            if (!rallypoint.IsDeploying)
            {
                rallypoint.StartDeployment();
                ctx.Reply(T.RallyWait, rallypoint.SecondsLeft);
            }
            else throw ctx.Reply(T.RallyAlreadyDeploying);
        }
        else throw ctx.SendCorrectUsage(SYNTAX);
    }
}
