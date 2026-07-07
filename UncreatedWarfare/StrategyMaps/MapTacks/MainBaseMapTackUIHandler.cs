using System;
using System.Linq;
using Uncreated.Warfare.Events.Models;
using Uncreated.Warfare.Events.Models.Zones;
using Uncreated.Warfare.FOBs.SupplyCrates;
using Uncreated.Warfare.Layouts.Teams;
using Uncreated.Warfare.Players;
using Uncreated.Warfare.Proximity;
using Uncreated.Warfare.Translations;
using Uncreated.Warfare.Vehicles.Spawners;
using Uncreated.Warfare.Zones;

namespace Uncreated.Warfare.StrategyMaps.MapTacks;

internal class MainBaseMapTackUIHandler : IMapTackUIHandler, IDisposable,
    IEventListener<PlayerEnteredZone>,
    IEventListener<PlayerExitedZone>
{
    private readonly Zone _mainBase;
    private readonly Team _team;
    private readonly VehicleSpawnerService _spawnerService;
    private readonly ZoneStore _zoneStore;
    private readonly MapTackInfoUITranslations _translations;

    private float _lastVehicleCount;

    private bool _isDisposed;

    private readonly int[] _vehicleCounts;
    private static readonly int[] VehicleCountsBuffer = new int[MapTackVehicleType.Count - 1];

    public event VehicleUpdated? OnVehicleUpdated;

    event SuppliesUpdated? IMapTackUIHandler.OnSuppliesUpdated { add { } remove { } }
    event HealthUpdated? IMapTackUIHandler.OnHealthUpdated { add { } remove { } }
    event AttributesUpdated? IMapTackUIHandler.OnAttributesUpdated { add { } remove { } }

    public MainBaseMapTackUIHandler(Zone mainBase, Team team, VehicleSpawnerService spawnerService, TranslationInjection<MapTackInfoUITranslations> translations, ZoneStore zoneStore)
    {
        _mainBase = mainBase;
        _team = team;
        _spawnerService = spawnerService;
        _zoneStore = zoneStore;
        _translations = translations.Value;

        _vehicleCounts = new int[MapTackVehicleType.Count - 1];

        foreach (VehicleSpawner spawner in spawnerService.Spawners)
        {
            if (spawner.Team.IsFriendly(_team))
            {
                spawner.VehicleStateUpdated += OnVehicleSpawnerStateUpdated;
            }
        }
    }

    private void OnVehicleSpawnerStateUpdated(VehicleSpawner spawner, VehicleSpawnerState state)
    {
        if (state is not (
                VehicleSpawnerState.Ready
                or VehicleSpawnerState.Deployed
                or VehicleSpawnerState.LayoutDisabled
                or VehicleSpawnerState.LayoutDelayed
                or VehicleSpawnerState.Glitched
            ))
        {
            return;
        }

        MapTackVehicleType type = MapTackVehicleType.FromVehicleType(spawner.VehicleInfo.Type);
        UpdateVehicleCounts(true, type);
    }

    private void UpdateVehicleCounts(bool notify, MapTackVehicleType type = MapTackVehicleType.Other)
    {
        int oldValue = 0;
        if (type == MapTackVehicleType.Other)
        {
            if (notify)
                Buffer.BlockCopy(_vehicleCounts, 0, VehicleCountsBuffer, 0, sizeof(int) * VehicleCountsBuffer.Length);
            Array.Clear(_vehicleCounts, 0, _vehicleCounts.Length);
        }
        else
        {
            oldValue = _vehicleCounts[(int)type - 1];
            _vehicleCounts[(int)type - 1] = 0;
        }

        if (type is MapTackVehicleType.Infantry or MapTackVehicleType.Other && _zoneStore.IsGlobal)
        {
            foreach (ZoneProximity prox in _zoneStore.ProximityZones.Where(x => string.Equals(x.Zone.Name, _mainBase.Name, StringComparison.Ordinal)))
            {
                if (prox.Proximity is not ITrackingProximity<WarfarePlayer> trackingProximity)
                    continue;

                Vector3 center = prox.Zone.Center;
                Vector2 center2 = new Vector2(center.x, center.z);

                if (!prox.Zone.IsPrimary && _zoneStore.IsInsideZone(center2, ZoneType.WarRoom, _team.Faction))
                    continue;

                int playerCount = trackingProximity.ActiveObjects.Count;

                _vehicleCounts[(int)MapTackVehicleType.Infantry - 1] += playerCount;
            }
        }

        if (type != MapTackVehicleType.Infantry)
        {
            foreach (VehicleSpawner spawner in _spawnerService.Spawners)
            {
                if (!spawner.Team.IsFriendly(_team))
                    continue;

                MapTackVehicleType t = MapTackVehicleType.FromVehicleType(spawner.VehicleInfo.Type);
                if (type != MapTackVehicleType.Other && t != type)
                    continue;

                if (spawner.State == VehicleSpawnerState.Ready)
                {
                    ++_vehicleCounts[(int)t - 1];
                }
            }
        }

        if (!notify)
            return;

        if (type != MapTackVehicleType.Other)
        {
            int now = _vehicleCounts[(int)type - 1];
            if (oldValue != now)
                OnVehicleUpdated?.Invoke(type, now);
            return;
        }

        for (MapTackVehicleType t = (MapTackVehicleType)MapTackVehicleType.Count - 1; t >= MapTackVehicleType.Infantry; --t)
        {
            int old = VehicleCountsBuffer[(int)t - 1];
            int now = _vehicleCounts[(int)t - 1];
            if (old != now)
            {
                OnVehicleUpdated?.Invoke(t, now);
            }
        }
    }

    public string GetTitle(in LanguageSet languageSet)
    {
        return _translations.MainBaseTitle.Translate(_team.Faction, in languageSet);
    }

    public string? GetLocation(in LanguageSet languageSet)
    {
        return null;
    }

    /// <inheritdoc />
    public int? GetSupplyCount(SupplyType type)
    {
        return null;
    }

    /// <inheritdoc />
    public double? GetHealth()
    {
        return null;
    }

    /// <inheritdoc />
    public MapTackAttributes GetAttributes()
    {
        return 0;
    }

    /// <inheritdoc />
    public void CountVehicles(IList<KeyValuePair<MapTackVehicleType, int>> vehicleCounts)
    {
        if (_lastVehicleCount == 0 || Time.realtimeSinceStartup - _lastVehicleCount >= 30f)
        {
            UpdateVehicleCounts(notify: false);
        }

        for (MapTackVehicleType type = (MapTackVehicleType)MapTackVehicleType.Count - 1; type >= MapTackVehicleType.Infantry; --type)
        {
            int ct = _vehicleCounts[(int)type - 1];
            if (ct > 0)
                vehicleCounts.Add(new KeyValuePair<MapTackVehicleType, int>(type, ct));
        }
    }

    /// <inheritdoc />
    public void Dispose()
    {
        foreach (VehicleSpawner spawner in _spawnerService.Spawners)
        {
            if (spawner.Team.IsFriendly(_team))
            {
                spawner.VehicleStateUpdated -= OnVehicleSpawnerStateUpdated;
            }
        }

        _isDisposed = true;
    }

    void IEventListener<PlayerEnteredZone>.HandleEvent(PlayerEnteredZone e, IServiceProvider serviceProvider)
    {
        // if the map tack is removed for some reason this could theoretically still be called by the event dispatched
        if (_isDisposed) return;
        
        if (e.Zone.Type != ZoneType.MainBase)
            return;

        UpdateVehicleCounts(true, MapTackVehicleType.Infantry);
    }

    void IEventListener<PlayerExitedZone>.HandleEvent(PlayerExitedZone e, IServiceProvider serviceProvider)
    {
        if (_isDisposed) return;

        if (e.Zone.Type != ZoneType.MainBase)
            return;

        UpdateVehicleCounts(true, MapTackVehicleType.Infantry);
    }
}