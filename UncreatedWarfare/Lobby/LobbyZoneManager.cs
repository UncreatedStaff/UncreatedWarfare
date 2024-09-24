using Microsoft.Extensions.Configuration;
using SDG.Framework.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;
using Uncreated.Warfare.Configuration;
using Uncreated.Warfare.Exceptions;
using Uncreated.Warfare.Logging;
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
public class LobbyZoneManager : ILevelHostedService
{
    private FlagInfo[] _teamFlags;
    private readonly ZoneStore _zoneStore;
    private readonly LobbyConfiguration _lobbyConfig;
    private readonly IFactionDataStore _factionDataStore;
    private readonly ILogger<LobbyZoneManager> _logger;
    private ITrackingProximity<WarfarePlayer> _zoneCollider;
    public LobbyZoneManager(ZoneStore zoneStore, LobbyConfiguration lobbyConfig, IFactionDataStore factionDataStore, ILogger<LobbyZoneManager> logger)
    {
        _zoneStore = zoneStore;
        _lobbyConfig = lobbyConfig;
        _factionDataStore = factionDataStore;
        _logger = logger;
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

            flags.Add(new FlagInfo(foundObject, flagId, faction));
        }

        _teamFlags = flags.ToArray();

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

            for (int i = 0; i < _teamFlags.Length; ++i)
            {
                ref FlagInfo flag = ref _teamFlags[i];

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

            // player's position is closest to this flag
            ref FlagInfo closestFlag = ref _teamFlags[closestPosIndex];

            // player is closest to looking at this flag
            ref FlagInfo closestLook = ref _teamFlags[closestLookIndex];

            // within 35 degrees of looking at sign
            float angle = Mathf.Acos(closestLookDot);

            _logger.LogDebug("{0} is closest to {1} and looking at {2}: {3:F2} ({4:F2}).", player, closestFlag.Faction.Name, closestLook.Faction.Name, angle * Mathf.Rad2Deg, closestLookDot);
        }
    }

    private void OnObjectExitedLobby(WarfarePlayer obj)
    {
        _logger.LogInformation("Player left lobby: {0}.", obj);
    }

    private void OnObjectEnteredLobby(WarfarePlayer obj)
    {
        _logger.LogInformation("Player entered lobby: {0}.", obj);
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

    private struct FlagInfo
    {
        public readonly ObjectInfo Object;
        public readonly ushort FlagId;
        public readonly FactionInfo Faction;
        public readonly Vector3 Position;
        public FlagInfo(ObjectInfo obj, ushort flagId, FactionInfo faction)
        {
            Object = obj;
            FlagId = flagId;
            Faction = faction;
            Position = obj.Object.transform.position;
        }
    }
}
