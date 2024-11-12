using System.Linq;
using Uncreated.Warfare.Squads;

namespace Uncreated.Warfare.Players.Extensions;
public static class PlayerSquadExtensions
{
    /// <summary>
    /// Gets the player's current squad.
    /// </summary>
    public static Squad? GetSquad(this WarfarePlayer player)
    {
        return player.Component<SquadPlayerComponent>().Squad;
    }

    /// <summary>
    /// Gets if a player is currently in a squad.
    /// </summary>
    public static bool IsInSquad(this WarfarePlayer player)
    {
        return player.Component<SquadPlayerComponent>().Squad != null;
    }

    /// <summary>
    /// Gets if a player is currently the leader of their squad.
    /// </summary>
    public static bool IsSquadLeader(this WarfarePlayer player)
    {
        Squad? squad = player.Component<SquadPlayerComponent>().Squad;
        return squad != null && squad.Leader.Equals(player);
    }

    /// <summary>
    /// Gets if a player is currently a member in a squad but not a leader.
    /// </summary>
    public static bool IsNonLeaderSquadMember(this WarfarePlayer player)
    {
        Squad? squad = player.Component<SquadPlayerComponent>().Squad;
        return squad != null && !squad.Leader.Equals(player);
    }

    /// <summary>
    /// Gets if a player is currently in a squad which can no longer fit any more members.
    /// </summary>
    public static bool IsInFullSquad(this WarfarePlayer player)
    {
        Squad? squad = player.Component<SquadPlayerComponent>().Squad;
        return squad is { IsFull: true };
    }

    /// <summary>
    /// Gets if a player is currently in a squad with another player.
    /// </summary>
    public static bool IsInSquadWith(this WarfarePlayer player, IPlayer other)
    {
        Squad? squad = player.Component<SquadPlayerComponent>().Squad;
        if (squad == null)
            return false;

        if (other is WarfarePlayer pl)
            return pl.Component<SquadPlayerComponent>().Squad == squad;
        
        foreach (WarfarePlayer member in squad.Members)
        {
            if (member.Equals(other))
                return true;
        }

        return false;
    }
}