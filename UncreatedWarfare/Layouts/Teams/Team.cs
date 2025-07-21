using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics.CodeAnalysis;
using Uncreated.Warfare.Configuration;
using Uncreated.Warfare.Players;
using Uncreated.Warfare.Teams;
using Uncreated.Warfare.Util;

namespace Uncreated.Warfare.Layouts.Teams;

/// <summary>
/// Represents a team or 'side' in a round.
/// </summary>
public class Team : IEquatable<Team>
{
    private List<Team>? _opponents;

    // used for no-alloc Select statements in OnlinePlayersOnTeam
    internal Func<WarfarePlayer, bool> PlayerSelector;

    private bool PlayerSelectorIntl(WarfarePlayer player) => player.Team.GroupId.m_SteamID == GroupId.m_SteamID;

    public static readonly Team NoTeam = new Team
    {
        Id = 0,
        Faction = FactionInfo.NoFaction,
        GroupId = default,
        Configuration = ConfigurationHelper.EmptySection,
        _opponents = new List<Team>(0)
    };

    public Team()
    {
        PlayerSelector = PlayerSelectorIntl;
    }

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
    public IConfigurationSection Configuration
    {
        get;
        internal set => field = value ?? ConfigurationHelper.EmptySection;
    } = ConfigurationHelper.EmptySection;

    /// <summary>
    /// List of all opponent teams.
    /// </summary>
    [System.Text.Json.Serialization.JsonIgnore, JsonIgnore]
    [field: MaybeNull, AllowNull]
    public IReadOnlyCollection<Team> Opponents
    {
        get
        {
            if (field != null)
                return field;

            if (_opponents == null)
            {
                Interlocked.CompareExchange(ref _opponents, new List<Team>(0), null);
            }

            return field ??= new ReadOnlyCollection<Team>(_opponents);
        }
    }

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
        return !ReferenceEquals(this, other) && _opponents != null && _opponents.Contains(other);
    }

    public virtual bool IsOpponent(CSteamID otherGroupId)
    {
        if (otherGroupId.m_SteamID == GroupId.m_SteamID || _opponents == null)
        {
            return false;
        }

        for (int i = 0; i < _opponents.Count; ++i)
        {
            if (_opponents[i].GroupId.m_SteamID == otherGroupId.m_SteamID)
                return true;
        }

        return false;
    }

    public virtual bool IsFriendly(Team other)
    {
        return other.Equals(this);
    }

    public virtual bool IsFriendly(CSteamID otherGroupId)
    {
        return otherGroupId.m_SteamID == GroupId.m_SteamID;
    }

    public virtual bool Equals(Team? other)
    {
        return other is not null && GroupId.m_SteamID == other.GroupId.m_SteamID;
    }

    public override int GetHashCode()
    {
        return unchecked( (int)GroupId.GetAccountID().m_AccountID );
    }

    public static void DeclareEnemies(params IReadOnlyCollection<Team> teams)
    {
        GameThread.AssertCurrent();

        foreach (Team team in teams)
        {
            if (team._opponents == null)
            {
                Interlocked.CompareExchange(ref team._opponents, new List<Team>(teams.Count - 1), null);
            }

            team._opponents.Capacity = teams.Count - 1;
        }

        foreach (Team team in teams)
        {
            if (team == NoTeam)
                continue;

            foreach (Team other in teams)
            {
                if (team == other || other == NoTeam || team._opponents == null || team._opponents.Contains(other))
                    continue;

                team._opponents.Add(other);
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