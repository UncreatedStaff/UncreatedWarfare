using Uncreated.Warfare.Events.Models.Squads;
using Uncreated.Warfare.Players;
using Uncreated.Warfare.Util.List;

namespace Uncreated.Warfare.Squads;

public class SquadManager
{
    private readonly ILogger<SquadManager> _logger;
    private readonly TrackingList<Squad> _squads;

    /// <summary>
    /// List of all active Squads in the current game.
    /// </summary>
    public ReadOnlyTrackingList<Squad> Squads { get; }

    public SquadManager(ILogger<SquadManager> logger)
    {
        _logger = logger;
        _squads = new TrackingList<Squad>(16);
        Squads = new ReadOnlyTrackingList<Squad>(_squads);
    }

    public Squad CreateSquad(WarfarePlayer squadLeader, string squadName)
    {
        Squad squad = new Squad(squadLeader.Team, squadName, this);
        // moved this so SquadCreated runs before SquadMemberJoined
        _ = WarfareModule.EventDispatcher.DispatchEventAsync(new SquadCreated { Squad = squad, Player = squadLeader });
        squad.AddMember(squadLeader);
        _squads.Add(squad);
        _logger.LogDebug("Created new Squad: " + squad);
        return squad;
    }

    public bool DisbandSquad(Squad squad)
    {
        Squad? existing = _squads.FindAndRemove(s => s == squad);
        if (existing == null)
            return false;

        existing.DisbandMembers();

        _ = WarfareModule.EventDispatcher.DispatchEventAsync(new SquadDisbanded { Squad = squad });
        _logger.LogDebug("Disbanded squad: " + squad);
        return true;
    }
}
