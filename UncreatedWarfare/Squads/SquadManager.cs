using System;
using System.Linq;
using Uncreated.Warfare.Events.Models;
using Uncreated.Warfare.Events.Models.Players;
using Uncreated.Warfare.Events.Models.Squads;
using Uncreated.Warfare.Layouts.Teams;
using Uncreated.Warfare.Players;
using Uncreated.Warfare.Players.Extensions;
using Uncreated.Warfare.Players.Management;
using Uncreated.Warfare.Util;
using Uncreated.Warfare.Util.List;

namespace Uncreated.Warfare.Squads;

public class SquadManager : 
    IEventListener<PlayerTeamChanged>,
    IEventListener<PlayerJoined>
{
    public const int MaxSquadCount = 8;

    private readonly ILogger<SquadManager> _logger;
    private readonly IPlayerService _playerService;
    private readonly TrackingList<Squad> _squads;

    /// <summary>
    /// List of all active Squads in the current game.
    /// </summary>
    public ReadOnlyTrackingList<Squad> Squads { get; }

    public SquadManager(ILogger<SquadManager> logger, IPlayerService playerService)
    {
        _logger = logger;
        _playerService = playerService;
        _squads = new TrackingList<Squad>(16);
        Squads = new ReadOnlyTrackingList<Squad>(_squads);
    }

    /// <summary>
    /// Gets an unused squad name using the NATO phonetic alphabet.
    /// </summary>
    /// <remarks>Used for automatically creating squads if you request a squadleader kit.</remarks>
    public string GetUniqueSquadName(Team playerTeam)
    {
        for (char letter = 'A'; letter <= 'Z'; ++letter)
        {
            string name = NATOPhoneticAlphabetHelper.GetProperCase(letter);
            if (Squads.Any(x => x.Team == playerTeam && x.Name.Equals(name, StringComparison.InvariantCultureIgnoreCase)))
            {
                continue;
            }

            return name;
        }

        // this shouldn't really ever happen
        return Guid.NewGuid().ToString("N");
    }

    public bool AreSquadLimited(Team team, out int requiredTeammatesForMoreSquads)
    {
        int squadsCount = Squads.Count(x => x.Team == team);

        float friendlyCount = _playerService.OnlinePlayers.Count(p => p.Team == team);

        // ReSharper disable once PossibleLossOfFraction
        int maxSquads = Mathf.CeilToInt((friendlyCount + (Squad.MaxMembers / 2)) / Squad.MaxMembers);

        if (maxSquads >= MaxSquadCount)
        {
            requiredTeammatesForMoreSquads = 0;
            return true;
        }

        requiredTeammatesForMoreSquads = Squad.MaxMembers * maxSquads - (Squad.MaxMembers / 2) + 1;

        return squadsCount >= maxSquads;
    }

    public Squad CreateSquad(WarfarePlayer squadLeader, string squadName)
    {
        GameThread.AssertCurrent();

        Squad squad = new Squad(squadLeader.Team, squadName, GetSquadIdentificationNumber(squadLeader.Team), this);
        _squads.Add(squad);
        squad.AddMemberWithoutNotify(squadLeader);

        UniTask.Create(async () =>
        {
            // note: SquadCreated event must run before SquadMemberJoined
            await WarfareModule.EventDispatcher.DispatchEventAsync(new SquadCreated
            {
                Squad = squad,
                Player = squadLeader
            });
            await WarfareModule.EventDispatcher.DispatchEventAsync(new SquadMemberJoined
            {
                Squad = squad,
                IsNewSquad = true,
                Player = squadLeader
            });
        });
        
        _logger.LogDebug("Created new Squad: " + squad);
        return squad;
    }
    private byte GetSquadIdentificationNumber(Team team)
    {
        checked
        {
            for (byte id = 1; ; ++id)
            {
                if (Squads.Any(s => s.Team == team && s.TeamIdentificationNumber == id))
                    continue;

                return id;
            }
        }
    }

    public bool DisbandSquad(Squad squad)
    {
        GameThread.AssertCurrent();

        Squad? existing = _squads.FindAndRemove(s => s == squad);
        if (existing == null)
            return false;

        existing.DisbandMembers();

        _ = WarfareModule.EventDispatcher.DispatchEventAsync(new SquadDisbanded { Squad = squad });
        _logger.LogDebug("Disbanded squad: " + squad);
        return true;
    }

    public void HandleEvent(PlayerTeamChanged e, IServiceProvider serviceProvider)
    {
        if (!e.Player.IsInSquad())
            return;
        
        e.Player.GetSquad()!.RemoveMember(e.Player);
    }

    public void HandleEvent(PlayerJoined e, IServiceProvider serviceProvider)
    {
        if (e.IsNewPlayer)
            return;

        Squad? previouslyJoinedSquad = Squads.FirstOrDefault(s => s.Team == e.Player.Team && s.TeamIdentificationNumber == e.SaveData.SquadTeamIdentificationNumber);
        if (previouslyJoinedSquad == null)
            return;
        
        previouslyJoinedSquad.TryAddMember(e.Player);
    }
}