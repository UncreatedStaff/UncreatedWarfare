using System;
using System.Collections.Generic;
using Uncreated.Warfare.Events.Models.Flags;
using Uncreated.Warfare.Layouts.Teams;
using Uncreated.Warfare.Players;
using Uncreated.Warfare.Util;
using Uncreated.Warfare.Util.List;
using Uncreated.Warfare.Zones;

namespace Uncreated.Warfare.Layouts.Flags;

public class FlagObjective : IDisposable, IObjective
{
    private readonly HashSet<Team> _previousOwners;

    private readonly List<Team> _discoveredTeams;


    public ZoneRegion Region { get; }
    public Vector3 Center => Region.Primary.Zone.Center;
    public string Name => Region.Name;
    public int Index { get; }
    public ReadOnlyTrackingList<WarfarePlayer> Players => Region.Players;
    public Team Owner => Contest.IsWon ? Contest.Leader : Team.NoTeam;

    /// <inheritdoc />
    public bool IsActive { get; private set; } = true;

    public SingleLeaderContest Contest { get; }
    public bool IsContested { get; private set; }

    public FlagContestState CurrentContestState { get; private set; }
    public FlagObjective(ZoneRegion region, int index) : this(region, index, Team.NoTeam) { }
    public FlagObjective(ZoneRegion region, int index, Team startingOwner)
    {
        _previousOwners = new HashSet<Team> { startingOwner };
        Contest = new SingleLeaderContest(startingOwner, 64);
        Region = region;
        Index = index;

        _discoveredTeams = new List<Team>(2);
        if (startingOwner.IsValid)
        {
            _discoveredTeams.Add(startingOwner);
        }

        Contest.OnPointsChanged += OnPointsChangedIntl;
        Contest.OnWon += OnCapturedIntl;
        Contest.OnRestarted += OnNeutralizedIntl;
        Region.OnPlayerEntered += OnPlayerEnteredIntl;
        Region.OnPlayerExited += OnPlayerExitedIntl;
    }

    /// <summary>
    /// Check if a team has discovered the flag.
    /// </summary>
    public bool IsDiscovered(Team team)
    {
        if (!team.IsValid)
            return false;

        lock (_discoveredTeams)
        {
            for (int i = 0; i < _discoveredTeams.Count; ++i)
            {
                if (_discoveredTeams[i].IsFriendly(team))
                    return true;
            }

            return false;
        }
    }

    /// <summary>
    /// Check if a team has discovered the flag.
    /// </summary>
    public bool IsDiscovered(CSteamID groupId)
    {
        if (groupId.m_SteamID == 0)
            return false;

        lock (_discoveredTeams)
        {
            for (int i = 0; i < _discoveredTeams.Count; ++i)
            {
                if (_discoveredTeams[i].IsFriendly(groupId))
                    return true;
            }

            return false;
        }
    }

    /// <summary>
    /// Add a team to the list of teams who have discovered this flag.
    /// </summary>
    /// <returns><see langword="true"/> if the team is valid and didn't already discover this flag, otherwise <see langword="false"/>.</returns>
    public bool Discover(Team team)
    {
        if (!DiscoverNoRaise(team))
            return false;

        _ = WarfareModule.EventDispatcher.DispatchEventAsync(new FlagDiscovered
        {
            Flag = this,
            Team = team
        });
        return true;
    }

    /// <summary>
    /// Add a team to the list of teams who have discovered this flag, without raising the event.
    /// </summary>
    /// <returns><see langword="true"/> if the team is valid and didn't already discover this flag, otherwise <see langword="false"/>.</returns>
    internal bool DiscoverNoRaise(Team team)
    {
        if (!team.IsValid)
            return false;

        lock (_discoveredTeams)
        {
            if (_discoveredTeams.Contains(team))
                return false;

            _discoveredTeams.Add(team);
        }

        return true;
    }

    // we have an issue where AwardPoints needs to run before this event
    // but also needs the value to be set before that event.
    //  we have to separate the setting and event in some cases
    internal void SetCurrentContestState(in FlagContestState state, bool invokeEvent = true)
    {
        FlagContestState oldState = CurrentContestState;
        CurrentContestState = state;
        if (oldState.Equals(in state) || !invokeEvent)
        {
            return;
        }

        InvokeContestStateChangedEvent(in oldState, in state);
    }

    internal void InvokeContestStateChangedEvent(in FlagContestState oldState, in FlagContestState newState)
    {
        if (oldState.Equals(in newState))
        {
            return;
        }

        _ = WarfareModule.EventDispatcher?.DispatchEventAsync(new FlagContestStateChanged
        {
            Flag = this,
            OldState = oldState,
            NewState = newState
        });
    }

    /// <returns><see langword="true"/> if the specified <see cref="Team"/> has captured this flag at least once, otherwise <see langword="false"/>.</returns>
    public bool IsPastOwner(Team team) => _previousOwners.Contains(team);

    private void OnPointsChangedIntl(int change)
    {
        _ = WarfareModule.EventDispatcher?.DispatchEventAsync(new FlagContestPointsChanged { Flag = this, PointsChange = change });
    }

    private void OnPlayerEnteredIntl(ZoneRegion region, WarfarePlayer player)
    {
        _ = WarfareModule.EventDispatcher?.DispatchEventAsync(new PlayerEnteredFlagRegion { Flag = this, Player = player });
    }

    private void OnPlayerExitedIntl(ZoneRegion region, WarfarePlayer player)
    {
        _ = WarfareModule.EventDispatcher?.DispatchEventAsync(new PlayerExitedFlagRegion { Flag = this, Player = player });
    }

    private void OnCapturedIntl(Team team)
    {
        _previousOwners.Add(team);
        _ = WarfareModule.EventDispatcher?.DispatchEventAsync(new FlagCaptured { Flag = this, Capturer = team });
    }

    private void OnNeutralizedIntl(Team team, Team oldTeam)
    {
        _ = WarfareModule.EventDispatcher?.DispatchEventAsync(new FlagNeutralized { Flag = this, Neutralizer = team, TakenFrom = oldTeam.IsValid ? oldTeam : null });
    }

    public void Dispose()
    {
        Dispose(true);
    }

    protected virtual void Dispose(bool disposing)
    {
        if (!disposing)
            return;

        IsActive = false;
        Region.OnPlayerEntered -= OnPlayerEnteredIntl;
        Region.OnPlayerExited -= OnPlayerExitedIntl;
        // important to dispose the zone region to get rid of colliders and stuff
        Region.Dispose();
    }

    public void MarkContested(bool isContested)
    {
        IsContested = isContested;
    }
    
    public override string ToString()
    {
        return $"FlagObjective: {Name} [" +
               $"Region: {Region}" +
               $", Owner: {Owner}" +
               $", Players Count: {Players.Count}" +
               $", Is Contested: {IsContested}" +
               $", Contest: [{Contest}]]";
    }

    public override bool Equals(object? obj)
    {
        return obj is FlagObjective objective &&
               Name == objective.Name;
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(Name);
    }


    bool ITransformObject.Alive => IsActive;

    Vector3 ITransformObject.Position
    {
        get => Region.Primary.Zone.Spawn;
        set => throw new NotSupportedException();
    }
    Quaternion ITransformObject.Rotation
    {
        get => Quaternion.Euler(0f, Region.Primary.Zone.SpawnYaw, 0f);
        set => throw new NotSupportedException();
    }
    Vector3 ITransformObject.Scale
    {
        get => Vector3.one;
        set => throw new NotSupportedException();
    }
    void ITransformObject.SetPositionAndRotation(Vector3 position, Quaternion rotation)
    {
        throw new NotSupportedException();
    }
}
