using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System;
using System.Linq;
using Uncreated.Warfare.Configuration;
using Uncreated.Warfare.Exceptions;
using Uncreated.Warfare.Layouts;
using Uncreated.Warfare.Layouts.Teams;
using Uncreated.Warfare.Players;
using Uncreated.Warfare.Util;
using Uncreated.Warfare.Util.List;
using Uncreated.Warfare.Zones;

namespace Uncreated.Warfare.FreeTeamDeathmatch;

/// <summary>
/// <see cref="ITeamManager{TTeam}"/> implementation for FreeTDM.
/// </summary>
internal sealed class FtdmDualSidedTeamManager : TwoSidedTeamManager
{
    private readonly ILogger<FtdmDualSidedTeamManager> _logger;

#nullable disable
    private Layout _layout;
    private WarfareModule _warfareModule;
#nullable restore

    /// <summary>
    /// The location used for the game.
    /// </summary>
    public FtdmLocation Location { get; private set; }

    /// <summary>
    /// Stores spawns for each team.
    /// </summary>
    /// <remarks>Safe to assume all teams have an entry in this dictionary.</remarks>
    public IReadOnlyDictionary<Team, FtdmLocationSpawn> Spawns { get; private set; }

    /// <inheritdoc />
    public FtdmDualSidedTeamManager(ILogger<FtdmDualSidedTeamManager> logger) : base(logger)
    {
        _logger = logger;
        Spawns = new LinearDictionary<Team, FtdmLocationSpawn>();
        Location = new FtdmLocation();
        SpawnAtWarRoom = false;
    }

    /// <inheritdoc />
    public override Vector4? GetSpawnPointWhenRespawningAtMain(IPlayer player, Team team, ZoneStore globalZoneStore)
    {
        if (!Spawns.TryGetValue(team, out FtdmLocationSpawn spawnLocation))
        {
            return null;
        }

        Vector3 location = spawnLocation.Spawns[RandomUtility.GetIndex(spawnLocation.Spawns)];

        Zone? zone = globalZoneStore.SearchZone(spawnLocation.Zone);

        return new Vector4(location.x, location.y, location.z, zone?.SpawnYaw ?? 0);
    }

    /// <inheritdoc />
    public override async UniTask InitializeAsync(IServiceProvider serviceProvider, CancellationToken token = default)
    {
        _layout = serviceProvider.GetRequiredService<Layout>();
        _warfareModule = serviceProvider.GetRequiredService<WarfareModule>();

        await base.InitializeAsync(serviceProvider, token);

        string fileName = _layout.LayoutInfo.FilePath;
        string? chosenLocationName = null;
        List<FtdmLocation>? locations = null;
        if (Configuration != null)
        {
            if (Configuration["Import"] is { } importLoc)
            {
                string absolutePath = _layout.LayoutInfo.ResolveRelativePath(importLoc);
                fileName = absolutePath;
                IConfigurationBuilder builder = new ConfigurationBuilder();
                ConfigurationHelper.AddJsonOrYamlFile(builder, _warfareModule.FileProvider, absolutePath, reloadOnChange: false);

                IConfigurationRoot importedConfig = builder.Build();
                locations = importedConfig.GetSection("Locations").Get<List<FtdmLocation>>();
                if (importedConfig is IDisposable disp)
                    disp.Dispose();
            }

            locations ??= Configuration.GetSection("Locations").Get<List<FtdmLocation>>();

            if (Configuration["Location"] is { Length: > 0 } locationName)
                chosenLocationName = locationName;
        }

        if (locations == null
            || locations.Any(
                x => x.PlayArea == null
                     || x.Spawns is not { Length: > 0 }
                     || x.Spawns.Any(s => s.Spawns is not { Length: > 0 }
                                          || s.Team == null
                                          || s.Zone == null)
                     )
            )
        {
            throw new GameConfigurationException("Spawns not configured correctly. Some value is null or not included.", fileName);
        }

        int spawnIndex = -1;
        if (chosenLocationName != null)
            spawnIndex = locations.FindIndex(x => string.Equals(x.PlayArea, chosenLocationName, StringComparison.Ordinal));
        
        if (spawnIndex < 0)
            spawnIndex = RandomUtility.GetIndex(locations);
        
        FtdmLocation location = locations[spawnIndex];

        LinearDictionary<Team, FtdmLocationSpawn> spawns = new LinearDictionary<Team, FtdmLocationSpawn>(AllTeams.Count);
        foreach (FtdmLocationSpawn spawn in location.Spawns)
        {
            Team? team = FindTeam(spawn.Team);
            if (team == null)
            {
                _logger.LogWarning($"Unknown team {spawn.Team} in spawn for location {location.PlayArea}.");
                continue;
            }

            if (!spawns.TryAdd(team, spawn))
            {
                _logger.LogWarning($"Duplicate team {spawn.Team} in spawn for location {location.PlayArea}.");
            }
        }

        if (spawns.Count != AllTeams.Count)
        {
            throw new GameConfigurationException($"Missing teams in spawns for location {location.PlayArea}.", fileName);
        }

        Location = location;
        Spawns = spawns;
        _logger.LogInformation($"FTDM location: {location.PlayArea}.");
    }
}

#nullable disable
public class FtdmLocation
{
    /// <summary>
    /// Descriptors of locations where teams can spawn.
    /// </summary>
    public FtdmLocationSpawn[] Spawns { get; set; }

    /// <summary>
    /// The zone layout out the entire play area.
    /// </summary>
    public string PlayArea { get; set; }
}

public class FtdmLocationSpawn
{
    /// <summary>
    /// Team identifier being configured.
    /// </summary>
    public string Team { get; set; }

    /// <summary>
    /// Name of the zone surrounding the whole spawn, enemies can not enter this area.
    /// </summary>
    public string Zone { get; set; }
    
    /// <summary>
    /// The locations where players can spawn.
    /// </summary>
    public Vector3[] Spawns { get; set; }
}
#nullable restore