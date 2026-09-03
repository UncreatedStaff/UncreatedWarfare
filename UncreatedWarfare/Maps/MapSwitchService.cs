using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using System.Net;
using System.Reflection;
using Uncreated.Warfare.Database.Abstractions;
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
    private readonly IGameDataDbContext _dbContext;
    private readonly WarfareModule _module;
    private readonly IPlayerService _playerService;
    private readonly ILogger<MapSwitchService> _logger;

    /// <summary>
    /// Maximum amount of time to wait for all players to disconnect after sending a relay request.
    /// </summary>
    private static readonly TimeSpan WaitForPlayersToRelayTime = TimeSpan.FromSeconds(2);

    public MapSwitchService(MapScheduler mapScheduler, IGameDataDbContext dbContext, WarfareModule module, IPlayerService playerService, ILogger<MapSwitchService> logger)
    {
        _mapScheduler = mapScheduler;
        _dbContext = dbContext;
        _module = module;
        _playerService = playerService;
        _logger = logger;

        if (ConfigDataField == null)
            _logger.LogWarning("Provider._configData not found.");
        if (ModeConfigDataField == null)
            _logger.LogWarning("Provider._modeConfigData not found.");
        if (ModeConfigDataOverridesField == null)
            _logger.LogWarning("Provider._modeConfigDataOverrides not found.");
        if (LoadGameplayConfigMethod == null)
            _logger.LogWarning("Provider.LoadGameplayConfig not found.");
    }

    public async UniTask SwitchMapAsync(MapData map, CancellationToken token = default)
    {
        string currentMapName = Level.info.name;
        MapData? currentMap = await _dbContext.Maps
            .AsNoTracking()
            .Include(x => x.Dependencies)
            .FirstOrDefaultAsync(x => x.DisplayName == currentMapName, token);

        await UniTask.SwitchToMainThread(token);

        LevelInfo? mapInfo = Level.getLevel(map.DisplayName);
        if (mapInfo == null)
        {
            await TryInstallWorkshopAndLoadItems(currentMap, map, token);
            await UniTask.SwitchToMainThread(token);

            mapInfo = Level.getLevel(map.DisplayName);
            if (mapInfo == null)
            {
                throw new ArgumentException($"Map isn't installed: {map.DisplayName}.");
            }
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

    private async Task TryInstallWorkshopAndLoadItems(MapData? currentMap, MapData map, CancellationToken token)
    {
        List<ulong> workshopItemsToInstall = map.Dependencies.Where(x => !x.IsRemoved).Select(x => x.WorkshopId).ToList();
        List<ulong> workshopItemsToRemove = map.Dependencies.Where(x => x.IsRemoved).Select(x => x.WorkshopId).ToList();

        if (currentMap != null)
        {
            if (currentMap.WorkshopId.HasValue)
                workshopItemsToRemove.Add(currentMap.WorkshopId.Value);

            workshopItemsToRemove.AddRange(currentMap.Dependencies.Where(x => !x.IsRemoved).Select(x => x.WorkshopId));
        }

        workshopItemsToRemove.RemoveAll(x => !DedicatedUGC.ugc.Exists(ugc => ugc.publishedFileID.m_PublishedFileId == x));
        workshopItemsToInstall.RemoveAll(x => DedicatedUGC.ugc.Exists(ugc => ugc.publishedFileID.m_PublishedFileId == x));

        workshopItemsToRemove.RemoveAll(workshopItemsToInstall.Contains);

        foreach (ulong item in workshopItemsToRemove)
        {
            int index = DedicatedUGC.ugc.FindIndex(x => x.publishedFileID.m_PublishedFileId == item);
            if (index < 0)
            {
                _logger.LogTrace($"Skipped removing {item}, not installed.");
                continue;
            }

            SteamContent ugc = DedicatedUGC.ugc[index];
            DedicatedUGC.ugc.RemoveAt(index);
        }

        foreach (ulong item in workshopItemsToInstall)
        {
            DedicatedUGC.registerItemInstallation(item);
        }

        DedicatedUGC.beginInstallingItems(false);
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
        _logger.LogInformation($"Switching map to {mapInfo.getLocalizedName()}...");

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
        _logger.LogInformation("Relaying players...");
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
            {
                _logger.LogWarning($"Timed out waiting for some players to disconnect after relay: {Provider.clients.Select(x => x.playerID.playerName)}.");
                break;
            }
        }

        for (int pInd = Provider.clients.Count - 1; pInd >= 0; --pInd)
        {
            SteamPlayer player = Provider.clients[pInd];
            Provider.kick(player.playerID.steamID, "Failed to disconnect via relay. Show this to a dev plz.");
        }

        SaveManager.save();

        _logger.LogDebug("Exiting level...");
        Level.exit();

        while (Level.isExiting)
        {
            await UniTask.NextFrame();
        }

        _logger.LogDebug("Level exited.");

        // re-initialize config in case level has overrides
        ConfigData? config = null;
        if (ConfigDataField != null && ModeConfigDataField != null)
        {
            config = ConfigData.CreateDefault(false);
            ConfigDataField.SetValue(null, config);
        }

        if (ModeConfigDataOverridesField != null && ModeConfigDataOverridesField.GetValue(null) is IDictionary dict)
        {
            dict.Clear();
        }

        if (LoadGameplayConfigMethod != null)
        {
            LoadGameplayConfigMethod.Invoke(null, [ false ]);
        }

        if (config != null && ModeConfigDataField != null)
        {
            ModeConfigDataField.SetValue(null, config.getModeConfig(Provider.mode));
        }

        _logger.LogDebug($"Loading {mapInfo.getLocalizedName()}...");
        Level.load(mapInfo, true);
        Provider.applyLevelModeConfigOverrides();
        
        while (Level.isLoading)
        {
            await UniTask.NextFrame();
        }

        _logger.LogDebug($"{mapInfo.getLocalizedName()} loaded.");
    }

    private static readonly FieldInfo? ConfigDataField = typeof(Provider).GetField("_configData", BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Static);
    private static readonly FieldInfo? ModeConfigDataField = typeof(Provider).GetField("_modeConfigData", BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Static);
    private static readonly FieldInfo? ModeConfigDataOverridesField = typeof(Provider).GetField("_modeConfigDataOverrides", BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Static);

    private static readonly MethodInfo? LoadGameplayConfigMethod =
        typeof(Provider).GetMethod(
            "LoadGameplayConfig",
            BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.Public,
            null,
            CallingConventions.Any,
            [ typeof(bool) ],
            null
        );
}