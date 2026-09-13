using Microsoft.EntityFrameworkCore;
using SDG.Provider;
using System;
using System.IO;
using System.Linq;
using System.Net;
using System.Reflection;
using Uncreated.Warfare.Database.Abstractions;
using Uncreated.Warfare.Events.Models;
using Uncreated.Warfare.Events.Models.Players;
using Uncreated.Warfare.Layouts;
using Uncreated.Warfare.Models.Seasons;
using Uncreated.Warfare.Networking;
using Uncreated.Warfare.Patches;
using Uncreated.Warfare.Players;
using Uncreated.Warfare.Players.Extensions;
using Uncreated.Warfare.Players.Management;
using Uncreated.Warfare.Players.UI;
using Uncreated.Warfare.Util;
using UnityEngine.SceneManagement;

namespace Uncreated.Warfare.Maps;

/// <summary>
/// Responsible for switching the map without restarting the server.
/// </summary>
public class MapSwitchService : IEventListener<PlayerJoined>
{
    private readonly MapScheduler _mapScheduler;
    private readonly IGameDataDbContext _dbContext;
    private readonly WarfareModule _module;
    private readonly IPlayerService _playerService;
    private readonly ILogger<MapSwitchService> _logger;
    private readonly LayoutFactory _layoutFactory;

    public MapData? SwitchingToMap => _switchingToMap;

    private DateTime? _estimatedCompletedTime;

    internal const float WorkshopCacheInvalidateSeconds = 60;
    internal const float WorkshopCachePaddingSeconds = 2;

    /// <summary>
    /// Maximum amount of time to wait for all players to disconnect after sending a relay request.
    /// </summary>
    private static readonly TimeSpan WaitForPlayersToRelayTime = TimeSpan.FromSeconds(2);

    public MapSwitchService(
        MapScheduler mapScheduler,
        IGameDataDbContext dbContext,
        WarfareModule module,
        IPlayerService playerService,
        LayoutFactory layoutFactory,
        ILogger<MapSwitchService> logger)
    {
        _mapScheduler = mapScheduler;
        _dbContext = dbContext;
        _module = module;
        _playerService = playerService;
        _layoutFactory = layoutFactory;
        _logger = logger;

        if (ConfigDataField == null)
            _logger.LogWarning("Provider._configData not found.");
        if (ModeConfigDataField == null)
            _logger.LogWarning("Provider._modeConfigData not found.");
        if (ModeConfigDataOverridesField == null)
            _logger.LogWarning("Provider._modeConfigDataOverrides not found.");
        if (LoadGameplayConfigMethod == null)
            _logger.LogWarning("Provider.LoadGameplayConfig not found.");
        if (LevelOnSceneLoaded == null)
            _logger.LogWarning("Level.onSceneLoaded not found.");
        if (ProviderResetChannels == null)
            _logger.LogWarning("Provider.resetChannels not found.");
    }

    /// <summary>
    /// Calculates the estimated time that it will take to switch levels from the current time.
    /// </summary>
    public TimeSpan? GetEstimatedTimeRemaining()
    {
        DateTime? estCompletedTime = _estimatedCompletedTime;
        if (!estCompletedTime.HasValue)
            return null;

        TimeSpan remaining = estCompletedTime.Value - DateTime.UtcNow;
        return remaining.Ticks >= 0 ? remaining : null;
    }

    /// <summary>
    /// Switches the server's map to another map given it's data configured in the 'maps' table in the database.
    /// </summary>
    /// <exception cref="ArgumentException">The map isn't installed, even after a workshop update.</exception>
    public async UniTask SwitchMapAsync(MapData map, MapSwitchParameters parameters, CancellationToken token = default)
    {
        TimeSpan extraTimeToComplete = TimeSpan.FromSeconds(0.5d);

        _estimatedCompletedTime = null;

        _switchingToMap = map;
        if (parameters.MinimumWaitTime > TimeSpan.FromSeconds(5))
        {
            _estimatedCompletedTime = DateTime.UtcNow.Add(parameters.MinimumWaitTime + extraTimeToComplete);
        }
        try
        {
            string currentMapName = Level.info.name;
            MapData? currentMap = await _dbContext.Maps
                .AsNoTracking()
                .Include(x => x.Dependencies)
                .OrderBy(x => x.Id)
                .FirstOrDefaultAsync(x => x.DisplayName == currentMapName, token);

            using IDisposable? hideHud = _module.ServiceProvider.ResolveOptional<HudManager>()?.HideHud();

            // list of assets and bundles to unload
            UnloadInfo unloadInfo = await TryInstallWorkshopAndLoadItems(currentMap, map, token);

            await UniTask.SwitchToMainThread(CancellationToken.None);

            LevelInfo? mapInfo = Level.getLevel(map.DisplayName);
            if (mapInfo == null)
            {
                throw new ArgumentException($"Map isn't installed: {map.DisplayName}.");
            }

            await _playerService.TakePlayerConnectionLock(CancellationToken.None);
            try
            {
                await UniTask.SwitchToMainThread();

                // needs a very large queue to accept that many players at once
                byte oldQueueSize = Provider.queueSize;
                byte newQueueSize = Math.Max(oldQueueSize, (byte)Math.Min(byte.MaxValue, _module.TrueMaxPlayers + (_originalQueueSize == 0 ? Provider.queueSize : _originalQueueSize)));
                Provider.queueSize = newQueueSize;
                _logger.LogTrace($"Updated queue size to {newQueueSize} from {oldQueueSize}.");

                if (_originalQueueSize == 0)
                    _originalQueueSize = oldQueueSize;

                // so workshop queries send the new map name
                _logger.LogInformation($"Switching map to {mapInfo.getLocalizedName()}...");

                SteamGameServer.SetMapName(mapInfo.name);   // steam server listing
                Provider.map = map.DisplayName;             // workshop files query

                foreach (WarfarePlayer player in _playerService.OnlinePlayers)
                {
                    player.FreezeMovement();
                }

                if (_playerService.OnlinePlayers.Count > 0)
                {
                    // wait at least 60 seconds since the most recent workshop query so the client-side workshop item cache becomes outdated
                    TimeSpan timeSinceLastQuery = GetWorkshopFilesLastSentRecorder.GetTimeSinceOnlinePlayerQueried(_playerService);
                    TimeSpan delay = TimeSpan.FromSeconds(WorkshopCacheInvalidateSeconds) - timeSinceLastQuery;
                    if (delay < parameters.MinimumWaitTime)
                        delay = parameters.MinimumWaitTime;

                    if (delay.Ticks > 0)
                    {
                        _logger.LogInformation($"Waiting {delay} to invalidate the client workshop cache.");
                        _estimatedCompletedTime = DateTime.UtcNow.Add(delay + extraTimeToComplete);
                        await UniTask.Delay(delay, cancellationToken: CancellationToken.None);
                        await UniTask.SwitchToMainThread();
                    }
                }
                else if (parameters.MinimumWaitTime > TimeSpan.Zero)
                {
                    _logger.LogInformation($"Waiting {parameters.MinimumWaitTime} for minimum wait time.");
                    _estimatedCompletedTime = DateTime.UtcNow.Add(parameters.MinimumWaitTime + extraTimeToComplete);
                    await UniTask.Delay(parameters.MinimumWaitTime, cancellationToken: CancellationToken.None);
                    await UniTask.SwitchToMainThread();
                }

                _estimatedCompletedTime ??= DateTime.UtcNow.Add(extraTimeToComplete);

                await _layoutFactory.UnloadLevel(CancellationToken.None);

                await SwitchMapIntl(mapInfo, unloadInfo, map);

                await UniTask.SwitchToMainThread();

                // reject any players that tried to join mid-rotate
                RejectPending();
            }
            finally
            {
                _estimatedCompletedTime = null;
                _playerService.ReleasePlayerConnectionLock();
            }
        }
        finally
        {
            Interlocked.CompareExchange(ref _switchingToMap, null, map);
        }
    }

    private int _hasInstalledEvent;
    private readonly List<TaskCompletionSource<object?>> _installationWaiters = new List<TaskCompletionSource<object?>>(1);

    private async Task<UnloadInfo> TryInstallWorkshopAndLoadItems(MapData? currentMap, MapData map, CancellationToken token)
    {
        token.ThrowIfCancellationRequested();

        List<ulong> workshopItemsToInstall = map.Dependencies.Where(x => !x.IsRemoved).Select(x => x.WorkshopId).ToList();
        
        if (map.WorkshopId.HasValue)
            workshopItemsToInstall.Insert(0, map.WorkshopId.Value);

        List<ulong> workshopItemsToRemove = map.Dependencies.Where(x => x.IsRemoved).Select(x => x.WorkshopId).ToList();

        if (currentMap != null)
        {
            if (currentMap.WorkshopId.HasValue)
                workshopItemsToRemove.Add(currentMap.WorkshopId.Value);

            workshopItemsToRemove.AddRange(currentMap.Dependencies.Where(x => !x.IsRemoved).Select(x => x.WorkshopId));
        }

        workshopItemsToRemove.RemoveAll(workshopItemsToInstall.Contains);
        workshopItemsToRemove.RemoveAll(x => !DedicatedUGC.ugc.Exists(ugc => ugc.publishedFileID.m_PublishedFileId == x));

        workshopItemsToInstall.RemoveAll(x => DedicatedUGC.ugc.Exists(ugc => ugc.publishedFileID.m_PublishedFileId == x));

        await UniTask.SwitchToMainThread(token);

        UnloadInfo unloadInfo = new UnloadInfo(this)
        {
            BundlesToUnload = new List<MasterBundleConfig>(4),
            OriginsToUnload = new List<AssetOrigin>(4)
        };

        bool anyWorkshopChanges = false;
        foreach (ulong item in workshopItemsToRemove)
        {
            int index = DedicatedUGC.ugc.FindIndex(x => x.publishedFileID.m_PublishedFileId == item);
            if (index < 0)
            {
                _logger.LogTrace($"Skipped removing {item}, not installed.");
                continue;
            }

            SteamContent ugc = DedicatedUGC.ugc[index];
            anyWorkshopChanges |= WorkshopUtility.RemoveModIdFromServerMenu(new PublishedFileId_t(item), advertise: false);
            DedicatedUGC.ugc.RemoveAt(index);
            DedicatedUGC.itemsQueried.Remove(item); // required to allow redownloads of the same mod twice (Gulf -> Yellowknife -> Gulf)
            _logger.LogTrace($"Removed UGC item: {ugc.publishedFileID.m_PublishedFileId}.");

            AssetOrigin origin = TempSteamworksWorkshop.FindOrAddOrigin(item);
            unloadInfo.OriginsToUnload.Add(origin);
            _logger.LogTrace($" + Discovered {origin.GetAssets().Count} asset(s) in {origin.name}.");

            string bundlePath = ugc.path;
            if (ugc.type == ESteamUGCType.MAP && WorkshopTool.findMapBundlesPath(ugc.path, out string mapBundlePath))
            {
                bundlePath = mapBundlePath;
            }

            if (Path.AltDirectorySeparatorChar != Path.DirectorySeparatorChar)
            {
                bundlePath = bundlePath.Replace(Path.AltDirectorySeparatorChar, Path.DirectorySeparatorChar);
            }

            for (int i = 0; i < Assets.allMasterBundles.Count; ++i)
            {
                MasterBundleConfig masterBundle = Assets.allMasterBundles[i];

                char s;
                // not within mod folder
                if (!masterBundle.directoryPath.StartsWith(bundlePath, StringComparison.Ordinal)
                    || (masterBundle.directoryPath.Length > bundlePath.Length &&
                        (s = masterBundle.directoryPath[bundlePath.Length]) != Path.DirectorySeparatorChar &&
                        s != Path.AltDirectorySeparatorChar))
                {
                    continue;
                }

                _logger.LogTrace($" + Discovered {masterBundle.assetBundleName} in {origin.name}.");
                unloadInfo.BundlesToUnload.Add(masterBundle);
            }
        }

        foreach (ulong item in workshopItemsToInstall)
        {
            DedicatedUGC.registerItemInstallation(item);
            anyWorkshopChanges |= WorkshopUtility.AddModIdToServerMenu(new PublishedFileId_t(item), advertise: false);
            _logger.LogTrace($"Registered UGC item: {item}.");
        }
        
        if (anyWorkshopChanges)
        {
            WorkshopUtility.UpdateGameServerAdvertisement();
        }

        TaskCompletionSource<object?> waitForInstalled = new TaskCompletionSource<object?>(state: null);
        if (Interlocked.Increment(ref _hasInstalledEvent) == 1)
        {
            DedicatedUGC.installed += OnFinishedInstalling;
        }

        await using CancellationTokenRegistration reg = token.Register(() =>
        {
            waitForInstalled.TrySetCanceled(token);
        });

        lock (_installationWaiters)
        {
            _installationWaiters.Add(waitForInstalled);
        }

        try
        {
            _logger.LogTrace("Installing new UGC items...");
            DedicatedUGC.beginInstallingItems(false);

            await waitForInstalled.Task;
            await UniTask.SwitchToMainThread();
        }
        finally
        {
            lock (_installationWaiters)
            {
                _installationWaiters.Remove(waitForInstalled);
            }

            if (Interlocked.Decrement(ref _hasInstalledEvent) == 0)
            {
                DedicatedUGC.installed -= OnFinishedInstalling;
            }
        }

        _logger.LogTrace("Done installing new UGC items for map change.");
        return unloadInfo;
    }

    private void OnFinishedInstalling()
    {
        lock (_installationWaiters)
        {
            foreach (TaskCompletionSource<object?> tcs in _installationWaiters.ToArray())
            {
                tcs.TrySetResult(null);
            }
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

    private class UnloadInfo(MapSwitchService mapSwitchService)
    {
        public required List<AssetOrigin> OriginsToUnload;
        public required List<MasterBundleConfig> BundlesToUnload;

        public async UniTask ApplyUnload(CancellationToken token = default)
        {
            await UniTask.SwitchToMainThread(token);

            foreach (AssetOrigin origin in OriginsToUnload)
            {
                Assets.assetOrigins.Remove(origin);

                IReadOnlyList<Asset> assetList = origin.GetAssets();
                foreach (Asset asset in assetList)
                {
                    Assets.AssetMapping mapping = Assets.currentAssetMapping;

                    EAssetType category = asset.assetCategory;
                    if (category != EAssetType.NONE && asset.id != 0)
                    {
                        ((IDictionary<ushort, Asset>)mapping.legacyAssetsTable[category]).Remove(new KeyValuePair<ushort, Asset>(asset.id, asset));
                    }

                    ((IDictionary<Guid, Asset>)mapping.assetDictionary).Remove(new KeyValuePair<Guid, Asset>(asset.GUID, asset));
                    mapping.assetList.Remove(asset);
                }

                mapSwitchService._logger.LogTrace($"Unloaded {assetList.Count} asset(s) from {origin.name}.");
            }

            foreach (MasterBundleConfig config in BundlesToUnload)
            {
                Assets.allMasterBundles.Remove(config);
                config.unload();
                mapSwitchService._logger.LogTrace($"Unloaded {config.assetBundleName}.");
            }

            await Resources.UnloadUnusedAssets();

            GC.Collect(GC.MaxGeneration, GCCollectionMode.Forced, true, true);
        }
    }

    private byte _originalQueueSize;
    private DateTime _lastRelayAll;
    private MapData? _switchingToMap;

    private async UniTask SwitchMapIntl(LevelInfo mapInfo, UnloadInfo? unloadInfo, MapData map)
    {
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

        _lastRelayAll = DateTime.UtcNow;
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

        // kick players that failed to relay somehow
        for (int pInd = Provider.clients.Count - 1; pInd >= 0; --pInd)
        {
            SteamPlayer player = Provider.clients[pInd];
            Provider.kick(player.playerID.steamID, "Failed to disconnect via relay. Show this to a dev plz.");
        }

        SaveManager.save();

        _logger.LogDebug("Exiting level...");
        Level.exit();

        // unity requires that at least one scene is always loaded, so load an empty scene so it's happy.
        Scene tempScene = SceneManager.CreateScene("Uncreated.EmptyTempScene");
        try
        {
            // unload game scene to destroy everything
            Scene gameScene = SceneManager.GetSceneByBuildIndex(Level.BUILD_INDEX_GAME);
            await TryUnloadScene(gameScene, errorLog: "Failed to unload game scene. We'll see what happens ig.");
            await UniTask.SwitchToMainThread();

            // update MapScheduler.Current
            _mapScheduler.NotifyMapSwitched(map);

            // invoke onSceneLoaded(Level.BUILD_INDEX_MENU) to trigger the 'on exit' events
            if (LevelOnSceneLoaded != null)
            {
                Scene scene = SceneManager.GetSceneByBuildIndex(Level.BUILD_INDEX_MENU);
                object? instance = LevelOnSceneLoaded.IsStatic ? null : Level.instance;
                _logger.LogTrace("Invoking onSceneLoaded(MENU)...");
                LevelOnSceneLoaded.Invoke(instance, [ scene, LoadSceneMode.Single ]);
            }
            else
            {
                _logger.LogWarning("Level.onSceneLoaded not found.");
            }

            _logger.LogDebug("Unloaded game scene.");

            // clears the NetIdRegistry, among other things
            if (ProviderResetChannels != null)
            {
                ProviderResetChannels.Invoke(null, Array.Empty<object>());
            }
            else
            {
                _logger.LogWarning("Provider.resetChannels not found.");
                NetIdRegistry.Clear();
            }

            // unload assets and bundles defined in previous map
            if (unloadInfo != null)
            {
                _logger.LogTrace("Unloading last map's assets...");
                await unloadInfo.ApplyUnload();
                await UniTask.SwitchToMainThread();
            }

            _logger.LogDebug("Level exited.");

            // re-initialize config in case level has config overrides
            ReloadConfigData();

            _logger.LogTrace("Applying new assets...");

            // adds new assets to the current asset mapping
            Assets.ApplyServerAssetMapping(mapInfo, DedicatedUGC.ugc.Select(x => x.publishedFileID).ToList());

            // add new assets to physics material table
            PhysicsMaterialNetTable.ServerPopulateTable();

            // re-calculate hashes for all masterbundles, since the new masterbundles won't have a hash
            Assets.initializeMasterBundleValidation();

            _logger.LogDebug($"Loading {mapInfo.getLocalizedName()}...");
            Level.load(mapInfo, true);
            Provider.applyLevelModeConfigOverrides();

            while (!Level.isLoaded)
            {
                await UniTask.NextFrame();
            }
        }
        finally
        {
            await UniTask.SwitchToMainThread();
            await TryUnloadScene(tempScene);
        }

        _logger.LogDebug($"{mapInfo.getLocalizedName()} loaded.");
    }

    private static void ReloadConfigData()
    {
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
            LoadGameplayConfigMethod.Invoke(null, [false]);
        }

        if (config != null && ModeConfigDataField != null)
        {
            ModeConfigDataField.SetValue(null, config.getModeConfig(Provider.mode));
        }
    }

    private async UniTask TryUnloadScene(Scene scene, string? errorLog = null)
    {
        if (!scene.IsValid())
            return;

        try
        {
            AsyncOperation? op = SceneManager.UnloadSceneAsync(scene);
            if (op != null)
                await op;
            else if (errorLog != null)
                _logger.LogWarning(errorLog);
        }
        catch (ArgumentException) { }
    }

    void IEventListener<PlayerJoined>.HandleEvent(PlayerJoined e, IServiceProvider serviceProvider)
    {
        if (_originalQueueSize == 0)
            return;

        // revert queue size back to original size once the pending array is empty enough
        // wait 30 seconds to give players time to actually start joining
        if (Provider.pending.Count > 0 || (DateTime.UtcNow - _lastRelayAll).TotalSeconds < 5)
            return;

        Provider.queueSize = _originalQueueSize;
        _logger.LogTrace($"Reverted queue size to {_originalQueueSize}.");
        _originalQueueSize = 0;
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
    private static readonly MethodInfo? LevelOnSceneLoaded =
        typeof(Level).GetMethod(
            "onSceneLoaded",
            BindingFlags.Static | BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public,
            null,
            CallingConventions.Any,
            [typeof(Scene), typeof(LoadSceneMode)],
            null
        );

    private static readonly MethodInfo? ProviderResetChannels =
        typeof(Provider).GetMethod(
            "resetChannels",
            BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.Public,
            null,
            CallingConventions.Any,
            Type.EmptyTypes,
            null
        );
}

/// <summary>
/// Options for switching maps.
/// </summary>
/// <param name="MinimumWaitTime">Ensures that map switching waits at least this long to stop.</param>
public sealed record MapSwitchParameters(TimeSpan MinimumWaitTime)
{
    /// <summary>
    /// Default map switching parameters.
    /// </summary>
    public static MapSwitchParameters Default { get; } = new MapSwitchParameters(MinimumWaitTime: TimeSpan.Zero);
}