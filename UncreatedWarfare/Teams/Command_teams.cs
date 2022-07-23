using System;
using Uncreated.Framework;
using Uncreated.Warfare.Commands.CommandSystem;
using Uncreated.Warfare.Gamemodes;
using Uncreated.Warfare.Gamemodes.Interfaces;
using Command = Uncreated.Warfare.Commands.CommandSystem.Command;

namespace Uncreated.Warfare.Commands;
public class TeamsCommand : Command
{
    private const string SYNTAX = "/teams";
    private const string HELP = "Switch teams without rejoining the server.";

    public TeamsCommand() : base("teams", EAdminType.MEMBER) { }

    public override void Execute(CommandInteraction ctx)
    {
#if DEBUG
        using IDisposable profiler = ProfilingUtils.StartTracking();
#endif
        ctx.AssertHelpCheck(0, SYNTAX + " - " + HELP);

        ctx.AssertRanByPlayer();

        ctx.AssertGamemode(out ITeams teamgm);
        if (Data.Is(out IImplementsLeaderboard<BasePlayerStats, BaseStatTracker<BasePlayerStats>> il) && il.isScreenUp)
            throw ctx.SendUnknownError();

        if (!teamgm.UseTeamSelector || teamgm.TeamSelector is null)
            throw ctx.SendGamemodeError();

        if (!ctx.Caller.OnDuty() && CooldownManager.HasCooldown(ctx.Caller, ECooldownType.CHANGE_TEAMS, out Cooldown cooldown))
        {
            ctx.Reply("teams_e_cooldown", cooldown.ToString());
            return;
        }
        ulong team = ctx.Caller.GetTeam();
        if ((team is 1 or 2) && !ctx.Caller.Player.IsInMain())
        {
            ctx.Reply("teams_e_notinmain");
            return;
        }
        teamgm.TeamSelector!.JoinSelectionMenu(ctx.Caller);
        ctx.Defer();
    }
}
