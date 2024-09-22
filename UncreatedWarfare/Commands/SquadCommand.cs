using Uncreated.Warfare.Interaction.Commands;
using Uncreated.Warfare.Players;

namespace Uncreated.Warfare.Commands;

[Command("squad", "sqaud", "sq")]
public class SquadCommand : IExecutableCommand
{
    private const string Syntax = "/squad <create|join|(un)lock|kick|leave|disband|promote> [parameters...]";
    private const string Help = "Join, create, or manage your squad.";

    /// <inheritdoc />
    public CommandContext Context { get; set; }

    /// <summary>
    /// Get /help metadata about this command.
    /// </summary>
    public static CommandStructure GetHelpMetadata()
    {
        return new CommandStructure
        {
            Description = Help,
            Parameters =
            [
                new CommandParameter("Create")
                {
                    Aliases = [ "new", "start" ],
                    Description = "Create a new squad with the next available name."
                },
                new CommandParameter("Join")
                {
                    Aliases = [ "jion" ],
                    Description = "Join an existing squad.",
                    Parameters =
                    [
                        new CommandParameter("Name", typeof(string))
                        {
                            Description = "The name or first letter of the squad."
                        }
                    ]
                },
                new CommandParameter("Promote")
                {
                    Description = "Give squad leader to one of your squad members.",
                    Parameters =
                    [
                        new CommandParameter("Member", typeof(IPlayer))
                    ]
                },
                new CommandParameter("Kick")
                {
                    Description = "Removes a player from your squad",
                    Parameters =
                    [
                        new CommandParameter("Member", typeof(IPlayer))
                    ]
                },
                new CommandParameter("Leave")
                {
                    Aliases = [ "remove", "disconnect" ],
                    Description = "Leave your current squad. If you are the leader, a new one will be chosen."
                },
                new CommandParameter("Disband")
                {
                    Aliases = [ "delete" ],
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
            ]
        };
    }

    /// <inheritdoc />
    public UniTask ExecuteAsync(CancellationToken token)
    {
#if false
        Context.AssertRanByPlayer();

        Context.AssertGamemode<ISquads>();

        if (!UCWarfare.Config.EnableSquads || !SquadManager.Loaded)
            throw Context.Reply(T.SquadsDisabled);

        Context.AssertArgs(1, Syntax);

        Context.AssertHelpCheck(0, Syntax + " - " + Help);

        ulong team = Context.Player.GetTeam();
        if (team is not 1 and not 2)
            throw Context.Reply(T.NotOnCaptureTeam);

        if (Context.MatchParameter(0, "create", "new", "start"))
        {
            Context.AssertHelpCheck(1, "/squad create (custom names for squads have been removed)");
            if (Context.Player.Squad is not null)
                throw Context.Reply(T.SquadAlreadyInSquad);

            if (SquadManager.MaxSquadsReached(team))
                throw Context.Reply(T.SquadsTooMany, SquadManager.ListUI.Squads.Length);

            if (SquadManager.AreSquadLimited(team, out int requiredTeammatesForMoreSquads))
                throw Context.Reply(T.SquadsTooManyPlayerCount, requiredTeammatesForMoreSquads);

            Squad squad = SquadManager.CreateSquad(Context.Player, team);
            throw Context.Reply(T.SquadCreated, squad);
        }
        
        if (Context.MatchParameter(0, "join", "jion"))
        {
            Context.AssertHelpCheck(1, "/squad join <name> - Join a squad, you can also just put the first letter of the squad name.");

            if (!Context.TryGetRange(1, out string? name))
                throw Context.SendCorrectUsage("/squad join <name> - Join a squad, you can also just put the first letter of the squad name.");

            if (Context.Player.Squad is not null)
                throw Context.Reply(T.SquadAlreadyInSquad);

            if (!SquadManager.FindSquad(name, team, out Squad squad))
                throw Context.Reply(T.SquadNotFound, name);

            if (squad.IsLocked && squad.Leader.SteamPlayer.playerID.group.m_SteamID != Context.Player.SteamPlayer.playerID.group.m_SteamID)
                throw Context.Reply(T.SquadLocked, squad);

            if (squad.IsFull())
                throw Context.Reply(T.SquadFull, squad);

            SquadManager.JoinSquad(Context.Player, squad);
            throw Context.Defer();
        }
        
        if (Context.MatchParameter(0, "promote", "leader"))
        {
            Context.AssertHelpCheck(1, "/squad promote <member> - Gives the provided player squad leader.");

            Context.AssertArgs(2, "/squad promote <member> - Gives the provided player squad leader.");

            if (Context.Player.Squad is null || Context.Player.Squad.Leader.Steam64 != Context.CallerId.m_SteamID)
                throw Context.Reply(T.SquadNotSquadLeader);

            if (!Context.TryGet(1, out ulong playerId, out UCPlayer? member, Context.Player.Squad.Members) || playerId == Context.CallerId.m_SteamID)
                throw Context.Reply(T.PlayerNotFound);

            SquadManager.PromoteToLeader(Context.Player.Squad, member);
            throw Context.Defer();
        }
        
        if (Context.MatchParameter(0, "kick"))
        {
            Context.AssertHelpCheck(1, "/squad kick <member> - Remove the provided player from your squad.");

            Context.AssertArgs(2, "/squad kick <member> - Remove the provided player from your squad.");

            if (Context.Player.Squad is null || Context.Player.Squad.Leader.Steam64 != Context.CallerId.m_SteamID)
                throw Context.Reply(T.SquadNotSquadLeader);

            if (!Context.TryGet(1, out ulong playerId, out UCPlayer? member, Context.Player.Squad.Members))
                throw Context.Reply(T.PlayerNotFound);

            if (playerId == Context.CallerId.m_SteamID)
                throw Context.Reply(T.PlayerNotFound);

            if (!member.IsOnline)
            {
                if (!PlayerSave.TryReadSaveFile(member.Steam64, out PlayerSave save))
                    throw Context.Reply(T.PlayerNotFound);
                
                save.SquadName = string.Empty;
                save.SquadLeader = 0;
                save.SquadLockedId = 0;
                PlayerSave.WriteToSaveFile(save);

                throw Context.Reply(T.SquadPlayerKicked, member);
            }

            SquadManager.KickPlayerFromSquad(member, Context.Player.Squad);
            throw Context.Defer();
        }
        
        if (Context.MatchParameter(0, "leave", "remove", "disconnect"))
        {
            Context.AssertHelpCheck(1, "/squad leave - Leave your current squad.");

            if (Context.Player.Squad is null)
                throw Context.Reply(T.SquadNotInSquad);

            SquadManager.LeaveSquad(Context.Player, Context.Player.Squad);
            throw Context.Defer();
        }
        
        if (Context.MatchParameter(0, "disband", "delete"))
        {
            Context.AssertHelpCheck(1, "/squad disband - Kicks everyone from your squad and deletes it.");

            if (Context.Player.Squad is null || Context.Player.Squad.Leader.Steam64 != Context.CallerId.m_SteamID)
                throw Context.Reply(T.SquadNotSquadLeader);

            SquadManager.DisbandSquad(Context.Player.Squad);
            throw Context.Defer();
        }
        
        if (Context.MatchParameter(0, "lock"))
        {
            Context.AssertHelpCheck(1, "/squad lock - Lock your squad so only people from your steam group can join.");

            if (Context.Player.Squad is null || Context.Player.Squad.Leader.Steam64 != Context.CallerId.m_SteamID)
                throw Context.Reply(T.SquadNotSquadLeader);

            SquadManager.SetLocked(Context.Player.Squad, true);
            throw Context.Reply(T.SquadLockedSquad);

        }
        
        if (Context.MatchParameter(0, "unlock"))
        {
            Context.AssertHelpCheck(1, "/squad unlock - Allow anyone to join your squad.");

            if (Context.Player.Squad is null || Context.Player.Squad.Leader.Steam64 != Context.CallerId.m_SteamID)
                throw Context.Reply(T.SquadNotSquadLeader);

            SquadManager.SetLocked(Context.Player.Squad, false);
            throw Context.Reply(T.SquadUnlockedSquad);
        }
        
        throw Context.SendCorrectUsage(Syntax);
#endif
        return UniTask.CompletedTask;
    }
}