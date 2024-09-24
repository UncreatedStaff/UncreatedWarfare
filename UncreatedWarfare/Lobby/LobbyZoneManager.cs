using Microsoft.Extensions.Configuration;
using SDG.Framework.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;
using Uncreated.Warfare.Configuration;
using Uncreated.Warfare.Events.Models;
using Uncreated.Warfare.Events.Models.Objects;
using Uncreated.Warfare.Exceptions;
using Uncreated.Warfare.Layouts;
using Uncreated.Warfare.Layouts.Teams;
using Uncreated.Warfare.Players;
using Uncreated.Warfare.Proximity;
using Uncreated.Warfare.Services;
using Uncreated.Warfare.Teams;
using Uncreated.Warfare.Util;
using Uncreated.Warfare.Zones;

namespace Uncreated.Warfare.Lobby;

/// <summary>
/// Handles visual effects in the lobby.
/// </summary>
public class LobbyZoneManager : ILevelHostedService, IEventListener<QuestObjectInteracted>, ILayoutStartingListener
{
    private const short FlagJoining = -1;
    private const short FlagFull = 0;
    private const short FlagOpen = 1;

    private readonly ZoneStore _zoneStore;
    private readonly LobbyConfiguration _lobbyConfig;
    private readonly IFactionDataStore _factionDataStore;
    private readonly ILogger<LobbyZoneManager> _logger;
    private readonly WarfareModule _module;
    private ITrackingProximity<WarfarePlayer> _zoneCollider;

    private readonly ITeamSelectorBehavior _behavior;

    internal FlagInfo[] TeamFlags;
    
    public LobbyZoneManager(ZoneStore zoneStore, LobbyConfiguration lobbyConfig, IFactionDataStore factionDataStore, ILogger<LobbyZoneManager> logger, WarfareModule module, ITeamSelectorBehavior behavior)
    {
        _zoneStore = zoneStore;
        _lobbyConfig = lobbyConfig;
        _factionDataStore = factionDataStore;
        _logger = logger;
        _module = module;
        _behavior = behavior;
    }

    public UniTask LoadLevelAsync(CancellationToken token)
    {
        // find team flag objects
        List<FlagInfo> flags = new List<FlagInfo>();
        foreach (IConfigurationSection flagInfo in _lobbyConfig.GetSection("Flags").GetChildren())
        {
            string? teamStr = flagInfo["Team"];
            IAssetLink<ObjectAsset> asset = flagInfo.GetAssetLink<ObjectAsset>("Object");
            ushort flagId = flagInfo.GetValue<ushort>("Flag");

            FactionInfo? faction = _factionDataStore.FindFaction(teamStr);
            if (faction == null)
                throw new GameConfigurationException("Invalid faction \"" + teamStr + "\"", _lobbyConfig.FilePath);

            ITeamManager<Team> teamManager = _module.GetActiveLayout().TeamManager;

            ObjectInfo foundObject = default;
            foreach (ObjectInfo obj in LevelObjectUtility.EnumerateObjects())
            {
                if (!asset.MatchAsset(obj.Object.asset))
                    continue;

                if (foundObject.HasValue)
                {
                    throw new GameConfigurationException($"Multiple {asset} objects in the map, unable to choose one for the lobby flag", _lobbyConfig.FilePath);
                }

                foundObject = obj;
            }

            if (!foundObject.HasValue)
            {
                throw new GameConfigurationException($"No {asset} objects in the map, unable to find a lobby flag", _lobbyConfig.FilePath);
            }

            Team? team = teamManager.AllTeams.FirstOrDefault(x => x.Faction.Equals(faction));

            if (team == null)
            {
                throw new GameConfigurationException($"No team registered with the faction {faction.Name}", _lobbyConfig.FilePath);
            }

            flags.Add(new FlagInfo(flags.Count, foundObject, flagId, team));
        }

        TeamFlags = flags.ToArray();

        TeamInfo[] behaviorTeamInfo = new TeamInfo[TeamFlags.Length];
        for (int i = 0; i < behaviorTeamInfo.Length; ++i)
        {
            ref TeamInfo info = ref behaviorTeamInfo[i];
            info = new TeamInfo(TeamFlags[i].Team);
        }

        _behavior.Teams = behaviorTeamInfo;

        // find zone
        string lobbyZoneName = _lobbyConfig["Zone"];

        Zone? zone = _zoneStore.Zones.FirstOrDefault(x => x.Name.Equals(lobbyZoneName, StringComparison.Ordinal));

        if (zone == null)
            throw new GameConfigurationException("Lobby zone not found: \"" + lobbyZoneName + "\"", _lobbyConfig.FilePath);

        _zoneCollider = _zoneStore.CreateColliderForZone(zone);

        _zoneCollider.OnObjectEntered += OnObjectEnteredLobby;
        _zoneCollider.OnObjectExited += OnObjectExitedLobby;

        TimeUtility.physicsUpdated += OnFixedUpdate;

        return UniTask.CompletedTask;
    }

    public void StartJoiningTeam(WarfarePlayer player, int teamIndex)
    {
        PlayerLobbyComponent component = player.Component<PlayerLobbyComponent>();
        if (teamIndex < 0)
        {
            if (!component.IsJoining)
                return;

            // cancel joining team
            component.StartJoiningTeam(-1);
            UpdateAllFlags(player);
            return;
        }

        if (component.IsJoining)
            component.StartJoiningTeam(-1);

        ref FlagInfo flag = ref TeamFlags[teamIndex];
        component.StartJoiningTeam(teamIndex);
        if (!player.UnturnedPlayer.quests.getFlag(flag.FlagId, out short v) || v != FlagJoining)
        {
            player.UnturnedPlayer.quests.sendSetFlag(flag.FlagId, FlagJoining);
        }

        for (int i = 0; i < TeamFlags.Length; ++i)
        {
            if (i == teamIndex)
                continue;

            ref FlagInfo flag2 = ref TeamFlags[i];
            player.UnturnedPlayer.quests.sendSetFlag(flag2.FlagId, FlagFull);
        }
    }

    private void UpdateAllFlags(WarfarePlayer player)
    {
        PlayerLobbyComponent component = player.Component<PlayerLobbyComponent>();
        if (component.IsJoining)
        {
            for (int i = 0; i < TeamFlags.Length; ++i)
            {
                ref FlagInfo flag = ref TeamFlags[i];
                player.UnturnedPlayer.quests.sendSetFlag(flag.FlagId, component.JoiningTeam.Index == i ? FlagJoining : FlagFull);
            }
        }
        else
        {
            for (int i = 0; i < TeamFlags.Length; ++i)
            {
                ref FlagInfo flag = ref TeamFlags[i];
                player.UnturnedPlayer.quests.sendSetFlag(flag.FlagId, _behavior.CanJoinTeam(i, -1) ? FlagOpen : FlagFull);
                // todo put barricade in front of player for a frame to refresh the highlight color
            }
        }
    }

    private void OnFixedUpdate()
    {
        foreach (WarfarePlayer player in _zoneCollider.ActiveObjects)
        {
            if (!player.IsOnline)
                continue;

            int closestLookIndex = -1;
            float closestLookDot = 0;
            int closestPosIndex = -1;
            float closestDistSqr = 0;
            Transform pos = player.UnturnedPlayer.look.aim;
            Vector3 playerPos = pos.position;
            Vector3 playerLookVector = pos.forward;

            for (int i = 0; i < TeamFlags.Length; ++i)
            {
                ref FlagInfo flag = ref TeamFlags[i];

                Vector3 lookVector = flag.Position - playerPos;
                
                float dot = Vector3.Dot(lookVector.normalized, playerLookVector);
                if (closestLookIndex == -1 || closestLookDot < dot)
                {
                    closestLookDot = dot;
                    closestLookIndex = i;
                }

                float distSqr = lookVector.sqrMagnitude;
                if (closestPosIndex == -1 || closestDistSqr > distSqr)
                {
                    closestDistSqr = distSqr;
                    closestPosIndex = i;
                }
            }

            // within 35 degrees of looking at sign
            float angle = Mathf.Acos(closestLookDot);
            if (angle > 35)
                closestLookIndex = -1;

            player.Component<PlayerLobbyComponent>().UpdatePositionalData(closestLookIndex, closestPosIndex);
        }
    }

    private void OnObjectExitedLobby(WarfarePlayer player)
    {
        player.Component<PlayerLobbyComponent>().EnterLobby();
    }

    private void OnObjectEnteredLobby(WarfarePlayer player)
    {
        player.Component<PlayerLobbyComponent>().ExitLobby();
    }

    UniTask IHostedService.StartAsync(CancellationToken token)
    {
        return UniTask.CompletedTask;
    }

    UniTask IHostedService.StopAsync(CancellationToken token)
    {
        TimeUtility.physicsUpdated -= OnFixedUpdate;

        if (_zoneCollider != null)
        {
            _zoneCollider.OnObjectEntered -= OnObjectEnteredLobby;
            _zoneCollider.OnObjectExited -= OnObjectExitedLobby;

            if (_zoneCollider is IDisposable d)
                d.Dispose();

            _zoneCollider = null!;
        }

        return UniTask.CompletedTask;
    }

    void IEventListener<QuestObjectInteracted>.HandleEvent(QuestObjectInteracted e, IServiceProvider serviceProvider)
    {
        for (int i = 0; i < TeamFlags.Length; ++i)
        {
            ref FlagInfo flag = ref TeamFlags[i];
            if (flag.Object.Index != e.ObjectIndex || !flag.Object.Coord.Equals(e.RegionPosition))
                continue;

            StartJoiningTeam(e.Player, i);
            break;
        }
    }

    UniTask ILayoutStartingListener.HandleLayoutStartingAsync(Layout layout, CancellationToken token)
    {
        // update 'Team' objects for the new session
        ITeamManager<Team> teamManager = _module.GetActiveLayout().TeamManager;
        for (int i = 0; i < TeamFlags.Length; ++i)
        {
            ref FlagInfo flag = ref TeamFlags[i];
            FactionInfo faction = flag.Team.Faction;

            Team team = teamManager.AllTeams.First(x => x.Faction.Equals(faction));
            flag.Team = team;

            ref TeamInfo behaviorTeam = ref _behavior.Teams[i];
            behaviorTeam.Team = team;
        }

        _behavior.UpdateTeams();

        return UniTask.CompletedTask;
    }

    public struct FlagInfo
    {
        public readonly ObjectInfo Object;
        public readonly ushort FlagId;
        public readonly Vector3 Position;
        public readonly int Index;
        public Team Team;
        public FlagInfo(int index, ObjectInfo obj, ushort flagId, Team team)
        {
            Index = index;
            Object = obj;
            FlagId = flagId;
            Position = obj.Object.transform.position;
            Team = team;
        }
    }
}