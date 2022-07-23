using SDG.Unturned;
using System;
using System.Linq;
using System.Threading.Tasks;
using Uncreated.Framework;
using Uncreated.Warfare.Commands.CommandSystem;
using Uncreated.Warfare.Gamemodes.Interfaces;
using Uncreated.Warfare.Squads;
using Command = Uncreated.Warfare.Commands.CommandSystem.Command;

namespace Uncreated.Warfare.Commands;
public class SquadCommand : Command
{
    private const string SYNTAX = "/squad <create|join|(un)lock|kick|leave|disband|promote> [parameters...]";
    private const string HELP = "Join, create, or manage your squad.";

    public SquadCommand() : base("squad", EAdminType.MEMBER)
    {
        AddAlias("sqaud");
    }

    public override void Execute(CommandInteraction ctx)
    {
#if DEBUG
        using IDisposable profiler = ProfilingUtils.StartTracking();
#endif
        ctx.AssertRanByPlayer();

        ctx.AssertGamemode<ISquads>();

        if (!UCWarfare.Config.EnableSquads || !SquadManager.Loaded)
            throw ctx.Reply("squads_disabled");

        ctx.AssertArgs(1, SYNTAX);

        ctx.AssertHelpCheck(0, SYNTAX + " - " + HELP);

        ulong team = ctx.Caller.GetTeam();
        if (team is < 1 or > 2)
            throw ctx.Reply("squad_not_in_team");

        if (ctx.MatchParameter(0, "create"))
        {
            ctx.AssertHelpCheck(1, "/squad create (custom names for squads have been removed)");
            if (ctx.Caller.Squad is not null)
                throw ctx.Reply("squad_e_insquad");
            if (SquadManager.Squads.Count(x => x.Team == team) >= SquadManager.ListUI.Squads.Length)
                throw ctx.Reply("squad_too_many");

            Squad squad = SquadManager.CreateSquad(ctx.Caller, team);
            ctx.Reply("squad_created", squad.Name);
        }
        else if (ctx.MatchParameter(0, "join"))
        {
            ctx.AssertHelpCheck(1, "/squad join <name> - Join a squad, you can also just put the first letter of the squad name.");

            if (!ctx.TryGetRange(1, out string name))
                throw ctx.SendCorrectUsage("/squad join <name> - Join a squad, you can also just put the first letter of the squad name.");

            if (ctx.Caller.Squad is not null)
                throw ctx.Reply("squad_e_insquad");
            
            if (!SquadManager.FindSquad(name, team, out Squad squad))
                throw ctx.Reply("squad_e_noexist", name);

            if (squad.IsLocked && squad.Leader.SteamPlayer.playerID.group.m_SteamID != ctx.Caller.SteamPlayer.playerID.group.m_SteamID)
                throw ctx.Reply("squad_e_locked");
            else if (squad.IsFull())
                throw ctx.Reply("squad_e_full");
            else
            {
                SquadManager.JoinSquad(ctx.Caller, squad);
                ctx.Defer();
            }
        }
        else if (ctx.MatchParameter(0, "promote"))
        {
            ctx.AssertHelpCheck(1, "/squad promote <member> - Gives the provided player squad leader.");

            ctx.AssertArgs(2, "/squad promote <member> - Gives the provided player squad leader.");

            if (ctx.Caller.Squad is null || ctx.Caller.Squad.Leader.Steam64 != ctx.CallerID)
                throw ctx.Reply("squad_e_notsquadleader");

            if (!ctx.TryGet(1, out ulong playerId, out UCPlayer member, ctx.Caller.Squad.Members) || playerId == ctx.CallerID)
                throw ctx.Reply("squad_e_playernotfound", ctx.Get(1)!);

            SquadManager.PromoteToLeader(ctx.Caller.Squad, member);
        }
        else if (ctx.MatchParameter(0, "kick"))
        {
            ctx.AssertHelpCheck(1, "/squad kick <member> - Remove the provided player from your squad.");

            ctx.AssertArgs(2, "/squad kick <member> - Remove the provided player from your squad.");

            if (ctx.Caller.Squad is null || ctx.Caller.Squad.Leader.Steam64 != ctx.CallerID)
                throw ctx.Reply("squad_e_notsquadleader");

            if (!ctx.TryGet(1, out ulong playerId, out UCPlayer member, ctx.Caller.Squad.Members))
                throw ctx.Reply("squad_e_playernotfound", ctx.Get(1)!);

            if (playerId == ctx.CallerID)
                throw ctx.Reply("squad_e_playernotfound", ctx.Get(1)!);

            SquadManager.KickPlayerFromSquad(member, ctx.Caller.Squad);
        }
        else if (ctx.MatchParameter(0, "leave"))
        {
            ctx.AssertHelpCheck(1, "/squad leave - Leave your current squad.");

            if (ctx.Caller.Squad is null)
                throw ctx.Reply("squad_e_notinsquad");

            SquadManager.LeaveSquad(ctx.Caller, ctx.Caller.Squad);
        }
        else if (ctx.MatchParameter(0, "disband"))
        {
            ctx.AssertHelpCheck(1, "/squad disband - Kicks everyone from your squad and deletes it.");

            if (ctx.Caller.Squad is null || ctx.Caller.Squad.Leader.Steam64 != ctx.CallerID)
                throw ctx.Reply("squad_e_notsquadleader");

            SquadManager.DisbandSquad(ctx.Caller.Squad);
        }
        else if (ctx.MatchParameter(0, "lock"))
        {
            ctx.AssertHelpCheck(1, "/squad lock - Lock your squad so only people from your steam group can join.");

            if (ctx.Caller.Squad is null || ctx.Caller.Squad.Leader.Steam64 != ctx.CallerID)
                throw ctx.Reply("squad_e_notsquadleader");

            SquadManager.SetLocked(ctx.Caller.Squad, true);
            ctx.Reply("squad_locked");

        }
        else if (ctx.MatchParameter(0, "unlock"))
        {
            ctx.AssertHelpCheck(1, "/squad unlock - Allow anyone to join your squad.");

            if (ctx.Caller.Squad is null || ctx.Caller.Squad.Leader.Steam64 != ctx.CallerID)
                throw ctx.Reply("squad_e_notsquadleader");

            SquadManager.SetLocked(ctx.Caller.Squad, false);
            ctx.Reply("squad_unlocked");
        }
        else throw ctx.SendCorrectUsage(SYNTAX);
    }
}