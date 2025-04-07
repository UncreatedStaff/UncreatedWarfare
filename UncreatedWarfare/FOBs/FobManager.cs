using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;
using Uncreated.Framework.UI;
using Uncreated.Warfare.Buildables;
using Uncreated.Warfare.Configuration;
using Uncreated.Warfare.Events.Models.Fobs;
using Uncreated.Warfare.FOBs;
using Uncreated.Warfare.FOBs.Construction;
using Uncreated.Warfare.FOBs.Entities;
using Uncreated.Warfare.FOBs.SupplyCrates.Throwable.Vehicle;
using Uncreated.Warfare.Interaction;
using Uncreated.Warfare.Kits.Whitelists;
using Uncreated.Warfare.Layouts.Teams;
using Uncreated.Warfare.Locations;
using Uncreated.Warfare.Services;
using Uncreated.Warfare.Translations;
using Uncreated.Warfare.Util;
using Uncreated.Warfare.Util.List;
using Uncreated.Warfare.Zones;

namespace Uncreated.Warfare.Fobs;

public partial class FobManager : IWhitelistExceptionProvider, ILayoutHostedService, IDisposable
{
    internal const float EmplacementSpawnOffset = 3f;

    private readonly FobTranslations _translations;
    private readonly AssetConfiguration _assetConfiguration;
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<FobManager> _logger;
    private readonly TrackingList<IFobEntity> _entities;
    private readonly TrackingList<IFob> _fobs;
    private readonly ChatService _chatService;
    private readonly ITeamManager<Team> _teamManager;
    private readonly ZoneStore _zoneStore;

    public FobConfiguration Configuration { get; }

    /// <summary>
    /// Items placed by players that aren't linked to a specific FOB.
    /// </summary>
    public IReadOnlyList<IFobEntity> Entities { get; }

    /// <summary>
    /// List of all FOBs in the world.
    /// </summary>
    public IReadOnlyList<IFob> Fobs { get; }

    public FobManager(IServiceProvider serviceProvider, ILogger<FobManager> logger)
    {
        Configuration = serviceProvider.GetRequiredService<FobConfiguration>();
        _translations = serviceProvider.GetRequiredService<TranslationInjection<FobTranslations>>().Value;
        _assetConfiguration = serviceProvider.GetRequiredService<AssetConfiguration>();
        _chatService = serviceProvider.GetRequiredService<ChatService>();
        _teamManager = serviceProvider.GetRequiredService<ITeamManager<Team>>();
        _zoneStore = serviceProvider.GetRequiredService<ZoneStore>();
        _serviceProvider = serviceProvider;
        _logger = logger;
        _fobs = new TrackingList<IFob>(24);
        _entities = new TrackingList<IFobEntity>(32);
        Entities = _entities.AsReadOnly();

        Fobs = new ReadOnlyTrackingList<IFob>(_fobs);

        Configuration.OnChange += OnConfigUpdated;
    }

    /// <inheritdoc />
    UniTask ILayoutHostedService.StartAsync(CancellationToken token)
    {
        // registers all existing buildables inside main bases
        foreach (BarricadeInfo barricade in BarricadeUtility.EnumerateNonPlantedBarricades())
        {
            TryRegisterExistingBuildable(new BuildableBarricade(barricade.Drop));
        }

        foreach (StructureInfo structure in StructureUtility.EnumerateStructures())
        {
            TryRegisterExistingBuildable(new BuildableStructure(structure.Drop));
        }

        return UniTask.CompletedTask;

        void TryRegisterExistingBuildable(IBuildable buildable)
        {
            if (_entities.Any(x => x is IBuildableFobEntity f && f.Buildable.Equals(buildable)))
                return;

            // get team from which zones the barricade is in
            Zone? mainBase = _zoneStore.EnumerateInsideZones(buildable.Position, ZoneType.MainBase).FirstOrDefault();
            if (mainBase == null)
                return;

            Team? team = _teamManager.FindTeam(mainBase.Faction);
            if (team == null)
                return;

            TryRegisterEntity(buildable, team, _serviceProvider);
        }
    }

    /// <inheritdoc />
    UniTask ILayoutHostedService.StopAsync(CancellationToken token)
    {
        for (int i = _fobs.Count - 1; i >= 0; --i)
        {
            DeregisterFob(_fobs[i]);
        }

        _fobs.Clear();

        for (int i = _entities.Count - 1; i >= 0; --i)
        {
            DeregisterFobEntity(_entities[i]);
        }

        _entities.Clear();

        return UniTask.CompletedTask;
    }

    public BunkerFob RegisterBunkerFob(IBuildable fobBuildable)
    {
        GridLocation griddy = new GridLocation(fobBuildable.Position);
        string fobName = $"{NATOPhoneticAlphabetHelper.GetProperCase(griddy.LetterX)}-{griddy.Y + 1}";

        BunkerFob fob = new BunkerFob(_serviceProvider, fobName, fobBuildable);
        RegisterFob(fob);
        return fob;
    }
    public bool DeregisterFob(IFob fob)
    {
        IFob? existing = _fobs.FindAndRemove(f => f == fob);
        if (existing == null)
            return false;

        if (existing is IDisposable disposable)
        {
            try
            {
                disposable.Dispose();
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, $"Error disposing FOB: {fob}.");
            }
        }

        _logger.LogDebug("Deregistered FOB: " + fob);
        _ = WarfareModule.EventDispatcher.DispatchEventAsync(new FobDeregistered { Fob = fob });
        return true;
    }
    public IFob RegisterFob(IFob fob)
    {
        _fobs.Add(fob);
        _logger.LogDebug("Registered new FOB: " + fob);
        _ = WarfareModule.EventDispatcher.DispatchEventAsync(new FobRegistered { Fob = fob });
        return fob;
    }
    public void RegisterFobEntity(IFobEntity entity)
    {
        if (_entities.Contains(entity))
        {
            _logger.LogWarning($"FOB Entity registered twice: {entity}");
            return;
        }
        _entities.Add(entity);
        _logger.LogDebug($"Registered new FOB Entity: {entity}");

    }
    public bool DeregisterFobEntity(IFobEntity entity)
    {
        IFobEntity? existing = _entities.FindAndRemove(f => f == entity);
        if (existing == null)
            return false;

        if (existing is IDisposable disposable)
        {
            try
            {
                disposable.Dispose();
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, $"Error disposing FOB Entity: {entity}.");
            }
        }

        _logger.LogDebug($"Deregistered FOB Entity: {entity}");
        return true;
    }
    public TBuildableFobType? FindBuildableFob<TBuildableFobType>(IBuildable matchingBuildable) where TBuildableFobType : IBuildableFob
    {
        return _fobs.OfType<TBuildableFobType>().FirstOrDefault(f => f.Buildable.Equals(matchingBuildable));
    }
    public ResourceFob? FindNearestResourceFob(Team team, Vector3 position)
    {
        return _fobs.OfType<ResourceFob>().FirstOrDefault(f =>
            f.Team == team &&
            MathUtility.WithinRange(position, f.Position, f.EffectiveRadius)
        );
    }
    public BunkerFob? FindNearestBunkerFob(Team team, Vector3 position, bool includeUnbuilt = true)
    {
        return _fobs.OfType<BunkerFob>().FirstOrDefault(f =>
            f.Team == team &&
            MathUtility.WithinRange(position, f.Position, f.EffectiveRadius) &&
            (includeUnbuilt || f.IsBuilt)
        );
    }
    public BunkerFob? FindNearestBunkerFob(CSteamID teamGroup, Vector3 position, bool includeUnbuilt = true)
    {
        return _fobs.OfType<BunkerFob>().FirstOrDefault(f =>
            f.Team.GroupId == teamGroup
            && MathUtility.WithinRange(position, f.Position, f.EffectiveRadius)
            && (includeUnbuilt || f.IsBuilt)
        );
    }
    public IEnumerable<BunkerFob> FriendlyBunkerFobs(Team team, bool includeUnbuilt = true)
    {
        return _fobs.OfType<BunkerFob>().Where(f =>
            f.Team == team && (includeUnbuilt || f.IsBuilt)
        );
    }
    public TEntity? GetBuildableFobEntity<TEntity>(IBuildable buildable) where TEntity : IBuildableFobEntity
    {
        return _entities.OfType<TEntity>().FirstOrDefault(f =>
            f.Buildable.Equals(buildable)
        );
    }
    public EmplacementEntity? GetEmplacementFobEntity(InteractableVehicle emplacementVehicle)
    {
        return _entities.OfType<EmplacementEntity>().FirstOrDefault(f =>
            f.Vehicle.Vehicle.instanceID == emplacementVehicle.instanceID
        );
    }

    /// <inheritdoc />
    ValueTask<int> IWhitelistExceptionProvider.GetWhitelistAmount(IAssetContainer assetContainer)
    {
        foreach (ShovelableInfo shovelable in Configuration.Shovelables)
        {
            if (shovelable.Foundation.MatchAsset(assetContainer))
                return new ValueTask<int>(-1);
        }
        foreach (ThrownVehicleCrateInfo vehicleCrate in Configuration.ThrowableVehicleSupplyCrates)
        {
            if (vehicleCrate.ThrowableItemAsset.MatchAsset(assetContainer))
                return new ValueTask<int>(-1);
        }

        return new ValueTask<int>(0);
    }

    private void OnConfigUpdated(IConfiguration obj)
    {
        foreach (IFob fob in Fobs)
        {
            fob.UpdateConfiguration(Configuration);
        }

        foreach (IFobEntity entity in Entities)
        {
            entity.UpdateConfiguration(Configuration);
        }
    }

    public void Dispose()
    {
        Configuration.OnChange -= OnConfigUpdated;
    }
}