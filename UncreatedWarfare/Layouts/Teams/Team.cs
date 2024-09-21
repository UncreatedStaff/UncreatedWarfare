using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using Uncreated.Warfare.Configuration;
using Uncreated.Warfare.Teams;

namespace Uncreated.Warfare.Layouts.Teams;

/// <summary>
/// Represents a team or 'side' in a round.
/// </summary>
public class Team : IEquatable<Team>
{
    public static readonly Team NoTeam = new Team
    {
        Id = 0,
        Faction = new FactionInfo
        {
            Name = "Unaffilated",
            ShortName = "Unaffilated",
            Abbreviation = "UA",
            FactionId = "noteam",
            NameTranslations =
            {
                { string.Empty, "Unaffilated" }
            },
            ShortNameTranslations =
            {
                { string.Empty, "Unaffilated" }
            },
            AbbreviationTranslations =
            {
                { string.Empty, "UA" }
            },
            Color = new Color(0.7058823529f, 0.7058823529f, 0.7058823529f, 1f), // 0xb4b4b4
            KitPrefix = "ua",
            FlagImageURL = "https://i.imgur.com/z0HE5P3.png",
            IsDefaultFaction = true
        },
        GroupId = default,
        Configuration = ConfigurationHelper.EmptySection
    };

    /// <summary>
    /// If this team has a valid <see cref="Id"/> and <see cref="GroupId"/>.
    /// </summary>
    public bool IsValid => Id > 0 && GroupId.m_SteamID > 0;

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

    /// <summary>
    /// Extra configuration about the team from the TeamManager config.
    /// </summary>
    public IConfigurationSection Configuration { get; internal set; }

    [System.Text.Json.Serialization.JsonIgnore, JsonIgnore]
    public List<Team> Opponents { get; } = new List<Team>();

    public override bool Equals(object? obj)
    {
        if (obj is Team otherTeam)
            return Equals(otherTeam);

        if (obj is ulong otherGroup)
            return GroupId.m_SteamID == otherGroup;

        return false;
    }

    public bool IsOpponent(Team other)
    {
        return Opponents.Contains(other);
    }

    public bool IsFriendly(Team other)
    {
        // todo maybe this should change?
        return other.Equals(this);
    }

    public bool Equals(Team? other)
    {
        return other is not null && GroupId.m_SteamID != other.GroupId.m_SteamID;
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

    public override string ToString()
    {
        return Faction.FactionId;
    }

    public static bool operator ==(Team? team1, Team? team2) => team1 is null
        ? team2 is null
        : team2 is not null && team1.GroupId.m_SteamID == team2.GroupId.m_SteamID;
    public static bool operator !=(Team? team1, Team? team2) => !(team1 == team2);
    public static bool operator ==(ulong team1, Team? team2) => team1 == 0 ? team2 is null : team2 is not null && team1 == team2.GroupId.m_SteamID;
    public static bool operator !=(ulong team1, Team? team2) => !(team1 == team2);
    public static bool operator ==(Team? team1, ulong team2) => team2 == 0 ? team1 is null : team1 is not null && team2 == team1.GroupId.m_SteamID;
    public static bool operator !=(Team? team1, ulong team2) => !(team1 == team2);
}