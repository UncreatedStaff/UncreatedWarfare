using DanielWillett.ReflectionTools;
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
using Uncreated.Warfare.Models.Seasons;

namespace Uncreated.Warfare.Maps;

public class MapScheduler : IAsyncEventListener<ServerWorkshopLoading>
{
    private readonly IConfiguration _configuration;
    private readonly ILogger<MapScheduler> _logger;
    private readonly WarfareModule _module;

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

    [EventListener(Priority = 10)]
    async UniTask IAsyncEventListener<ServerWorkshopLoading>.HandleEventAsync(ServerWorkshopLoading e, IServiceProvider serviceProvider, CancellationToken token)
    {
        await using AsyncServiceScope scope = serviceProvider.CreateAsyncScope();

        ISeasonsDbContext dbContext = scope.ServiceProvider.GetRequiredService<ISeasonsDbContext>();

        IQueryable<MapData> maps = dbContext.Maps.Include(x => x.Dependencies).AsNoTracking();

        string? specificMapName = _configuration["map"];
        
        MapData? map;
        if (specificMapName != null)
        {
            map = await maps.FirstOrDefaultAsync(x => EF.Functions.Like(x.DisplayName, specificMapName), token);
            if (map == null)
            {
                _logger.LogError($"Map not found: {specificMapName}");
                await _module.ShutdownAsync("Map not found.", token);
                return;
            }

            HasSelectedMap = true;
            Current = map;
            ApplyMapSetting(e, dbContext);
            return;
        }

        string nextMapTempConfigFile = Path.Combine(_module.HomeDirectory, "Maps", "Next Map.json");

        map = null;
        if (File.Exists(nextMapTempConfigFile))
        {
            string fl = File.ReadAllText(nextMapTempConfigFile);
            map = await maps.Where(x => EF.Functions.Like(x.DisplayName, fl)).FirstOrDefaultAsync(token);
            if (map == null)
            {
                _logger.LogError($"Next map not found: {specificMapName}");
                await _module.ShutdownAsync("Next map not found.", token);
                return;
            }
        }
        
        if (map == null)
        {
            map = await maps.OrderBy(x => WarfareEFFunctions.Random()).FirstOrDefaultAsync(token);
            if (map == null)
            {
                _logger.LogError($"No maps configured: {specificMapName}");
                await _module.ShutdownAsync("Map not found.", token);
                return;
            }
        }


        HasSelectedMap = true;
        Current = map;
        ApplyMapSetting(e, dbContext);
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
            }
            else
            {
                e.Items.Add(fileId);
            }
        }

        Provider.map = Current.DisplayName;

        // note: i dont think this is really needed. it corrupts the 304930 .acf file and probably isn't worth fixing

        // delete unused mods
        // string baseGamePath = Path.GetFullPath(Path.Combine(Application.dataPath, ".."));
        // string expectedModPath = Path.Combine(baseGamePath, "Servers", Provider.serverID, "Workshop", "Steam", "content", Provider.APP_ID.m_AppId.ToString(CultureInfo.InvariantCulture));
        // 
        // DirectoryInfo expectedModFolder = new DirectoryInfo(expectedModPath);
        // if (!expectedModFolder.Exists)
        //     return;

        // todo:
        /*
        List<ulong> allMapIds = await dbContext.Maps
            .Where(x => x.WorkshopId.HasValue)
            .Select(x => x.WorkshopId!.Value)
            .ToListAsync();*/

        // foreach (DirectoryInfo modFolder in expectedModFolder.EnumerateDirectories("*", SearchOption.TopDirectoryOnly))
        // {
        //     if (!ulong.TryParse(modFolder.Name, NumberStyles.Number, CultureInfo.InvariantCulture, out ulong mod))
        //     {
        //         continue;
        //     }
        // 
        //     PublishedFileId_t modId = new PublishedFileId_t(mod);
        //     if (e.Items.Contains(modId) /* || _maps.Any(x => x.WorkshopId == mod || x.RequiredDependencies != null && Array.IndexOf(x.RequiredDependencies, mod) >= 0) */)
        //     {
        //         continue;
        //     }
        // 
        //     string displayPath = Path.GetRelativePath(baseGamePath, modFolder.FullName);
        //     try
        //     {
        //         modFolder.Delete(true);
        //         _logger.LogInformation("Deleted unused mod folder {0} from workshop directory: \"{1}\".", mod, displayPath);
        //     }
        //     catch (Exception ex)
        //     {
        //         _logger.LogWarning(ex, "Unable to delete unused mod folder {0} from workshop directory: \"{1}\".", mod, displayPath);
        //     }
        // }
    }
}
