using System;
using System.Linq;
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
        Structure = new CommandStructure
        {
            Description = HELP,
            Parameters = new CommandParameter[]
            {
                new CommandParameter("Create")
                {
                    Description = "Create a new squad with the next available name."
                },
                new CommandParameter("Join")
                {
                    Description = "Join an existing squad.",
                    Parameters = new CommandParameter[]
                    {
                        new CommandParameter("Name", typeof(string))
                        {
                            Description = "The name or first letter of the squad."
                        }
                    }
                },
                new CommandParameter("Promote")
                {
                    Description = "Give squad leader to one of your squad members.",
                    Parameters = new CommandParameter[]
                    {
                        new CommandParameter("Member", typeof(IPlayer))
                    }
                },
                new CommandParameter("Kick")
                {
                    Description = "Removes a player from your squad",
                    Parameters = new CommandParameter[]
                    {
                        new CommandParameter("Member", typeof(IPlayer))
                    }
                },
                new CommandParameter("Leave")
                {
                    Description = "Leave your current squad. If you are the leader, a new one will be chosen."
                },
                new CommandParameter("Disband")
                {
                    Description = "Leave your current squad and delete it."
                },
                new CommandParameter("Lock")
                {
                    Description = "Limit your squad to only members in your Steam group."
                },
                new CommandParameter("Unlock")
                {
                    Description = "Allow anyone to join your squad."
                }
            }
        };
    }

    public override void Execute(CommandInteraction ctx)
    {
#if DEBUG
        using IDisposable profiler = ProfilingUtils.StartTracking();
#endif
        ctx.AssertRanByPlayer();

        ctx.AssertGamemode<ISquads>();

        if (!UCWarfare.Config.EnableSquads || !SquadManager.Loaded)
            throw ctx.Reply(T.SquadsDisabled);

        ctx.AssertArgs(1, SYNTAX);

        ctx.AssertHelpCheck(0, SYNTAX + " - " + HELP);

        ulong team = ctx.Caller.GetTeam();
        if (team is not 1 and not 2)
            throw ctx.Reply(T.NotOnCaptureTeam);

        if (ctx.MatchParameter(0, "create"))
        {
            ctx.AssertHelpCheck(1, "/squad create (custom names for squads have been removed)");
            if (ctx.Caller.Squad is not null)
                throw ctx.Reply(T.SquadAlreadyInSquad);
            if (SquadManager.Squads.Count(x => x.Team == team) >= SquadManager.ListUI.Squads.Length)
                throw ctx.Reply(T.SquadsTooMany, SquadManager.ListUI.Squads.Length);

            Squad squad = SquadManager.CreateSquad(ctx.Caller, team);
            ctx.Reply(T.SquadCreated, squad);
        }
        else if (ctx.MatchParameter(0, "join"))
        {
            ctx.AssertHelpCheck(1, "/squad join <name> - Join a squad, you can also just put the first letter of the squad name.");

            if (!ctx.TryGetRange(1, out string name))
                throw ctx.SendCorrectUsage("/squad join <name> - Join a squad, you can also just put the first letter of the squad name.");

            if (ctx.Caller.Squad is not null)
                throw ctx.Reply(T.SquadAlreadyInSquad);

            if (!SquadManager.FindSquad(name, team, out Squad squad))
                throw ctx.Reply(T.SquadNotFound, name);

            if (squad.IsLocked && squad.Leader.SteamPlayer.playerID.group.m_SteamID != ctx.Caller.SteamPlayer.playerID.group.m_SteamID)
                throw ctx.Reply(T.SquadLocked, squad);

            if (squad.IsFull())
                throw ctx.Reply(T.SquadFull, squad);

            SquadManager.JoinSquad(ctx.Caller, squad);
            ctx.Defer();
        }
        else if (ctx.MatchParameter(0, "promote"))
        {
            ctx.AssertHelpCheck(1, "/squad promote <member> - Gives the provided player squad leader.");

            ctx.AssertArgs(2, "/squad promote <member> - Gives the provided player squad leader.");

            if (ctx.Caller.Squad is null || ctx.Caller.Squad.Leader.Steam64 != ctx.CallerID)
                throw ctx.Reply(T.SquadNotSquadLeader);

            if (!ctx.TryGet(1, out ulong playerId, out UCPlayer member, ctx.Caller.Squad.Members) || playerId == ctx.CallerID)
                throw ctx.Reply(T.PlayerNotFound);

            SquadManager.PromoteToLeader(ctx.Caller.Squad, member);
            ctx.Defer();
        }
        else if (ctx.MatchParameter(0, "kick"))
        {
            ctx.AssertHelpCheck(1, "/squad kick <member> - Remove the provided player from your squad.");

            ctx.AssertArgs(2, "/squad kick <member> - Remove the provided player from your squad.");

            if (ctx.Caller.Squad is null || ctx.Caller.Squad.Leader.Steam64 != ctx.CallerID)
                throw ctx.Reply(T.SquadNotSquadLeader);

            if (!ctx.TryGet(1, out ulong playerId, out UCPlayer member, ctx.Caller.Squad.Members))
                throw ctx.Reply(T.PlayerNotFound);

            if (playerId == ctx.CallerID)
                throw ctx.Reply(T.PlayerNotFound);

            SquadManager.KickPlayerFromSquad(member, ctx.Caller.Squad);
            ctx.Defer();
        }
        else if (ctx.MatchParameter(0, "leave"))
        {
            ctx.AssertHelpCheck(1, "/squad leave - Leave your current squad.");

            if (ctx.Caller.Squad is null)
                throw ctx.Reply(T.SquadNotInSquad);

            SquadManager.LeaveSquad(ctx.Caller, ctx.Caller.Squad);
            ctx.Defer();
        }
        else if (ctx.MatchParameter(0, "disband"))
        {
            ctx.AssertHelpCheck(1, "/squad disband - Kicks everyone from your squad and deletes it.");

            if (ctx.Caller.Squad is null || ctx.Caller.Squad.Leader.Steam64 != ctx.CallerID)
                throw ctx.Reply(T.SquadNotSquadLeader);

            SquadManager.DisbandSquad(ctx.Caller.Squad);
            ctx.Defer();
        }
        else if (ctx.MatchParameter(0, "lock"))
        {
            ctx.AssertHelpCheck(1, "/squad lock - Lock your squad so only people from your steam group can join.");

            if (ctx.Caller.Squad is null || ctx.Caller.Squad.Leader.Steam64 != ctx.CallerID)
                throw ctx.Reply(T.SquadNotSquadLeader);

            SquadManager.SetLocked(ctx.Caller.Squad, true);
            ctx.Reply(T.SquadLockedSquad);

        }
        else if (ctx.MatchParameter(0, "unlock"))
        {
            ctx.AssertHelpCheck(1, "/squad unlock - Allow anyone to join your squad.");

            if (ctx.Caller.Squad is null || ctx.Caller.Squad.Leader.Steam64 != ctx.CallerID)
                throw ctx.Reply(T.SquadNotSquadLeader);

            SquadManager.SetLocked(ctx.Caller.Squad, false);
            ctx.Reply(T.SquadUnlockedSquad);
        }
        else throw ctx.SendCorrectUsage(SYNTAX);
    }
}