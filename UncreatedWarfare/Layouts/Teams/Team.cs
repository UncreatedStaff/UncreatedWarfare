using Steamworks;
using System.Collections.Generic;
using Uncreated.Warfare.Models.Factions;
using Uncreated.Warfare.Teams;

namespace Uncreated.Warfare.Layouts.Teams;

/// <summary>
/// Represents a team or 'side' in a round.
/// </summary>
public class Team
{
    /// <summary>
    /// Unique ID for the team.
    /// </summary>
    public required int Id { get; init; }

    /// <summary>
    /// Information about the faction for this team.
    /// </summary>
    public required FactionInfo Faction { get; init; }

    /// <summary>
    /// Id of the in-game group used for this team.
    /// </summary>
    public CSteamID GroupId { get; init; }

    public List<Team> Opponents { get; private set; } = new List<Team>();

    public static bool operator ==(Team team1, Team team2) => team1.GroupId == team2.GroupId;
    public static bool operator !=(Team team1, Team team2) => team1.GroupId != team2.GroupId;
    public static bool operator ==(ulong team1, Team team2) => team1 == team2.GroupId.m_SteamID;
    public static bool operator !=(ulong team1, Team team2) => team1 != team2.GroupId.m_SteamID;
    public static bool operator ==(Team team1, ulong team2) => team1.GroupId.m_SteamID == team2;
    public static bool operator !=(Team team1, ulong team2) => team1.GroupId.m_SteamID != team2;
    public static readonly Team NoTeam = new Team
    {
        Id = 0,
        Faction = new FactionInfo(),
        GroupId = new CSteamID()    
    };

    public override bool Equals(object? obj)
    {
        if (obj is Team otherTeam)
            return GroupId == otherTeam.GroupId;

        if (obj is ulong otherGroup)
            return GroupId.m_SteamID == otherGroup;

        return false;
    }

    public override int GetHashCode()
    {
        return GroupId.GetHashCode();
    }
    public static void DeclareEnemies(params Team[] teams)
    {
        foreach (Team team in teams)
        {
            foreach (Team other in teams)
            {
                if (team == other || team == NoTeam || other == NoTeam)
                    continue;

                if (team.Opponents.Contains(other))
                    continue;

                team.Opponents.Add(other);
            }
        }
    }
}