using System;
using System.Net;
using Uncreated.Warfare.Models.Seasons;
using Uncreated.Warfare.Networking;
using Uncreated.Warfare.Players.Management;
using Uncreated.Warfare.Util;

namespace Uncreated.Warfare.Maps;

/// <summary>
/// Responsible for switching the map without restarting the server.
/// </summary>
public class MapSwitchService
{
    private readonly MapScheduler _mapScheduler;
    private readonly WarfareModule _module;
    private readonly IPlayerService _playerService;

    /// <summary>
    /// Maximum amount of time to wait for all players to disconnect after sending a relay request.
    /// </summary>
    private static readonly TimeSpan WaitForPlayersToRelayTime = TimeSpan.FromSeconds(2);

    public MapSwitchService(MapScheduler mapScheduler, WarfareModule module, IPlayerService playerService)
    {
        _mapScheduler = mapScheduler;
        _module = module;
        _playerService = playerService;
    }

    public async UniTask SwitchMapAsync(MapData map, CancellationToken token = default)
    {
        await UniTask.SwitchToMainThread(token);
        
        LevelInfo? mapInfo = Level.getLevel(map.DisplayName);
        if (mapInfo == null)
        {
            throw new ArgumentException($"Map isn't installed: {map.DisplayName}.");
        }

        await _playerService.TakePlayerConnectionLock(token);
        try
        {
            await UniTask.SwitchToMainThread(token);
            token.ThrowIfCancellationRequested();

            await SwitchMapIntl(mapInfo, map);

            await UniTask.SwitchToMainThread();

            // reject any players that tried to join mid-rotate
            RejectPending();
        }
        finally
        {
            _playerService.ReleasePlayerConnectionLock();
        }
    }

    private static void RejectPending()
    {
        for (int pInd = Provider.pending.Count - 1; pInd >= 0; --pInd)
        {
            SteamPending pending = Provider.pending[pInd];
            Provider.reject(
                pending.playerID.steamID,
                ESteamRejection.PLUGIN,
                "Map is rotating, please refresh and try rejoining."
            );
        }
    }

    private async UniTask SwitchMapIntl(LevelInfo mapInfo, MapData map)
    {
        // advertise the map's mod on the server menu and remove the old map
        ulong modId = map.WorkshopId ?? 0;
        bool changedMods = false;
        if (modId == 0 || (changedMods = WorkshopUtility.AddModIdToServerMenu(new PublishedFileId_t(modId), advertise: false)))
        {
            ulong oldModId = _mapScheduler.Current?.WorkshopId ?? 0;
            if (oldModId != 0)
            {
                WorkshopUtility.RemoveModIdFromServerMenu(new PublishedFileId_t(oldModId));
            }
            else if (changedMods)
            {
                WorkshopUtility.UpdateGameServerAdvertisement();
            }
        }

        SteamGameServer.SetMapName(mapInfo.name);

        // reject any players that are trying to join
        RejectPending();

        Action<Player> sendRelayToServer;
        const bool shouldShowMenu = false;

        if (_module.CanUseConnectionCode)
        {
            CSteamID connectionCode = SteamGameServer.GetSteamID();
            sendRelayToServer = p => p.sendRelayToServer(connectionCode, string.Empty, shouldShowMenu);
        }
        else
        {
            IPAddress sysNetIp = SteamGameServer.GetPublicIP()
                                                .ToIPAddress()
                                                .MapToIPv4();

            uint packed = IPv4Range.Pack(sysNetIp);
            sendRelayToServer = p => p.sendRelayToServer(packed, Provider.port, string.Empty, shouldShowMenu);
        }

        foreach (SteamPlayer player in Provider.clients)
        {
            sendRelayToServer(player.player);
        }

        // ask all players to rejoin the server
        DateTime startWaitingTime = DateTime.UtcNow;
        while (Provider.clients.Count > 0)
        {
            await UniTask.NextFrame();

            if (DateTime.UtcNow - startWaitingTime > WaitForPlayersToRelayTime)
                break;
        }

        for (int pInd = Provider.clients.Count - 1; pInd >= 0; --pInd)
        {
            SteamPlayer player = Provider.clients[pInd];
            Provider.kick(player.playerID.steamID, "Failed to disconnect via relay. Show this to a dev plz.");
        }

        // todo Level.exit();
    }
}