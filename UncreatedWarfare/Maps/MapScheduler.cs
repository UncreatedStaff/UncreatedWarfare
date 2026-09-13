using DanielWillett.ModularRpcs.Annotations;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Globalization;
using System.IO;
using System.Linq;
using Uncreated.Warfare.Database;
using Uncreated.Warfare.Database.Abstractions;
using Uncreated.Warfare.Events;
using Uncreated.Warfare.Events.Models;
using Uncreated.Warfare.Events.Models.Server;
using Uncreated.Warfare.Interaction;
using Uncreated.Warfare.Models.Seasons;
using Uncreated.Warfare.Translations;
using Uncreated.Warfare.Translations.Collections;
using Uncreated.Warfare.Util.Timing;

namespace Uncreated.Warfare.Maps;

[GenerateRpcSource]
public partial class MapScheduler : IAsyncEventListener<ServerWorkshopLoading>
{
    private const string NextMapFileName = "Queued Map.txt";

    private readonly IConfiguration _configuration;
    private readonly ILogger<MapScheduler> _logger;
    private readonly WarfareModule _module;

    private IDisposable? _notifyMapChangeLoopTicker;
    private MapData? _forcedNextMap;

    /// <summary>
    /// Whether or not a map has been selected yet.
    /// </summary>
    [MemberNotNullWhen(true, nameof(Current))]
    public bool HasSelectedMap { get; set; }

    /// <summary>
    /// The current map.
    /// </summary>
    /// <remarks>Not tracked by EF.</remarks>
    public MapData? Current { get; private set; }

    public MapScheduler(IConfiguration configuration, ILogger<MapScheduler> logger, WarfareModule module)
    {
        _logger = logger;
        _module = module;
        _configuration = configuration;
    }

    /// <summary>
    /// Sets <paramref name="map"/> as the next map to be played.
    /// </summary>
    public void ScheduleMap(MapData map)
    {
        if (map.Dependencies == null)
            throw new NotIncludedException("MapData.Dependencies.");

        lock (this)
        {
            _forcedNextMap = map;
            string dir = Path.Combine(_module.HomeDirectory, "Cache");
            string path = Path.Combine(dir, NextMapFileName);

            Directory.CreateDirectory(dir);
            File.WriteAllText(path, map.Id.ToString(CultureInfo.InvariantCulture));
        }

        StartScheduledMapBroadcastLoopTicker(map);
    }

    /// <summary>
    /// Attempts to get the next map that should be loaded. Called at the end of games.
    /// </summary>
    public bool TryGetPendingMap([NotNullWhen(true)] out MapData? map)
    {
        lock (this)
        {
            if (_forcedNextMap != null)
            {
                map = _forcedNextMap;
                ClearScheduledMapIntl();
                return true;
            }
        }

        map = null;
        return false;
    }

    /// <summary>
    /// Removes the last map enqueued by <see cref="ScheduleMap"/>.
    /// </summary>
    public void ClearScheduledMap()
    {
        lock (this)
        {
            ClearScheduledMapIntl();
        }
    }

    private void ClearScheduledMapIntl()
    {
        _forcedNextMap = null;

        _notifyMapChangeLoopTicker?.Dispose();
        _notifyMapChangeLoopTicker = null;

        string path = Path.Combine(_module.HomeDirectory, "Cache", NextMapFileName);
        try
        {
            File.Delete(path);
        }
        catch (IOException ex)
        {
            if (ex is not FileNotFoundException and not DirectoryNotFoundException)
                _logger.LogWarning(ex, $"Failed to remove {NextMapFileName}.");
        }
    }

    [EventListener(Priority = 10)]
    async UniTask IAsyncEventListener<ServerWorkshopLoading>.HandleEventAsync(ServerWorkshopLoading e, IServiceProvider serviceProvider, CancellationToken token)
    {
        int startupMapId = -1;
        lock (this)
        {
            string path = Path.Combine(_module.HomeDirectory, "Cache", NextMapFileName);
            try
            {
                string mapId = File.ReadAllText(path);
                if (int.TryParse(mapId, NumberStyles.Any, CultureInfo.InvariantCulture, out int id))
                {
                    startupMapId = id;
                }

                File.Delete(path);
            }
            catch (IOException ex)
            {
                if (ex is not FileNotFoundException and not DirectoryNotFoundException)
                    _logger.LogWarning(ex, $"Failed to read {NextMapFileName}.");
            }
        }

        await using AsyncServiceScope scope = serviceProvider.CreateAsyncScope();

        ISeasonsDbContext dbContext = scope.ServiceProvider.GetRequiredService<ISeasonsDbContext>();

        IQueryable<MapData> maps = dbContext.Maps.Include(x => x.Dependencies).AsNoTracking();
        
        MapData? map = null;

        // map from EnqueueMap(MapData)
        if (startupMapId >= 0)
        {
            map = await maps.FirstOrDefaultAsync(x => x.Id == startupMapId, token);
            if (map == null)
            {
                _logger.LogError($"Enqueued map not found: {startupMapId}.");
            }
        }

        // map from config
        string? specificMapName = _configuration["map"];
        if (map == null && !string.IsNullOrEmpty(specificMapName))
        {
            map = await maps.FirstOrDefaultAsync(x => EF.Functions.Like(x.DisplayName, specificMapName), token);
            if (map == null)
            {
                _logger.LogError($"Map not found: {specificMapName}");
                await _module.ShutdownAsync("Map not found.", token);
                return;
            }
        }

        // fallback pick a random map
        if (map == null)
        {
            map = await maps.OrderBy(x => WarfareEFFunctions.Random()).FirstOrDefaultAsync(token);
            if (map == null)
                _logger.LogWarning("Failed to select a random map.");
        }

        if (map == null)
        {
            _logger.LogError("No maps configured.");
            await _module.ShutdownAsync("No maps configured.", token);
            return;
        }

        HasSelectedMap = true;
        Current = map;
        ApplyMapSetting(e, dbContext);
    }

    internal void NotifyMapSwitched(MapData mapData)
    {
        Current = mapData;
    }

    internal void ApplyMapSetting(ServerWorkshopLoading e, ISeasonsDbContext dbContext)
    {
        _logger.LogInformation($"Map selected: {Current!.DisplayName}");
        if (Current!.WorkshopId.HasValue)
        {
            e.Items.Add(new PublishedFileId_t(Current.WorkshopId.Value));
        }

        foreach (MapWorkshopDependency dependency in Current.Dependencies)
        {
            PublishedFileId_t fileId = new PublishedFileId_t(dependency.WorkshopId);
            if (dependency.IsRemoved)
            {
                e.IgnoredChildren.Add(fileId);
                e.Items.Remove(fileId);
            }
            else
            {
                e.Items.Add(fileId);
            }
        }

        Provider.map = Current.DisplayName;
    }

    [RpcReceive]
    private async Task<bool> ReceiveScheduleMap(IServiceProvider serviceProvider, int mapId)
    {
        MapData? map;
        await using (AsyncServiceScope scope = serviceProvider.CreateAsyncScope())
        {
            ISeasonsDbContext dbContext = scope.ServiceProvider.GetRequiredService<ISeasonsDbContext>();

            map = await dbContext.Maps.Include(x => x.Dependencies).FirstOrDefaultAsync(x => x.Id == mapId);
            if (map == null)
            {
                return false;
            }

            ScheduleMap(map);
        }

        return true;
    }

    private void StartScheduledMapBroadcastLoopTicker(MapData? map)
    {
        lock (this)
        {
            if (map != _forcedNextMap)
                return;

            _notifyMapChangeLoopTicker?.Dispose();

            ILoopTickerFactory? loopTickerFactory = _module.ServiceProvider.ResolveOptional<ILoopTickerFactory>();
            if (loopTickerFactory == null)
                return;

            _notifyMapChangeLoopTicker = loopTickerFactory.CreateTicker(
                TimeSpan.FromMinutes(5),
                invokeImmediately: true,
                map,
                queueOnGameThread: true,
                (ticker, _, _) =>
                {
                    ChatService? chatService = _module.ServiceProvider.ResolveOptional<ChatService>();
                    CommonTranslations? translations = _module.ServiceProvider.ResolveOptional<TranslationInjection<CommonTranslations>>()?.Value;

                    if (translations != null && chatService != null)
                    {
                        chatService.Broadcast(translations.MapScheduledBroadcast, ticker.State!.DisplayName);
                    }
                });
        }
    }
}