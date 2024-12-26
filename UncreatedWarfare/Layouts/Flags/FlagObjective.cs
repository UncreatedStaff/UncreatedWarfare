using System;
using System.Collections.Generic;
using Uncreated.Warfare.Events.Models.Flags;
using Uncreated.Warfare.Layouts.Teams;
using Uncreated.Warfare.Players;
using Uncreated.Warfare.Util.List;
using Uncreated.Warfare.Zones;

namespace Uncreated.Warfare.Layouts.Flags;
public class FlagObjective : IDisposable
{
    private readonly HashSet<Team> _previousOwners;
    public ZoneRegion Region { get; }
    public Vector3 Center => Region.Primary.Zone.Center;
    public string Name => Region.Name;
    public ReadOnlyTrackingList<WarfarePlayer> Players => Region.Players;
    public Team Owner => Contest.IsWon ? Contest.Leader : Team.NoTeam;
    public SingleLeaderContest Contest { get; }
    public bool IsContested { get; private set; }

    public FlagContestState CurrentContestState { get; private set; }
    public FlagObjective(ZoneRegion region) : this(region, Team.NoTeam) { }
    public FlagObjective(ZoneRegion region, Team startingOwner)
    {
        _previousOwners = new HashSet<Team> { startingOwner };
        Contest = new SingleLeaderContest(startingOwner, 64);
        Region = region;
        Contest.OnPointsChanged += OnPointsChangedIntl;
        Contest.OnWon += OnCapturedIntl;
        Contest.OnRestarted += OnNeutralizedIntl;
        Region.OnPlayerEntered += OnPlayerEnteredIntl;
        Region.OnPlayerExited += OnPlayerExitedIntl;
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

    private void OnNeutralizedIntl(Team team)
    {
        _ = WarfareModule.EventDispatcher?.DispatchEventAsync(new FlagNeutralized { Flag = this, Neutralizer = team });
    }

    public void Dispose()
    {
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
}
