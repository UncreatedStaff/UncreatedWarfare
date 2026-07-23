using System;
using System.Globalization;
using Uncreated.Warfare.Buildables;
using Uncreated.Warfare.Configuration;
using Uncreated.Warfare.FOBs.SupplyCrates;
using Uncreated.Warfare.Players;
using Uncreated.Warfare.Util;

namespace Uncreated.Warfare.StrategyMaps.MapTacks;

public class MapTack : IDisposable, ITransformObject
{
    private List<WarfarePlayer>? _uiUpdateListeners;

    private readonly StrategyMapManager _strategyMapManager;
    private readonly bool _leaveUiHandlerOpen;
    private bool _hasShowInAreaEvents;

    public IAssetLink<ItemPlaceableAsset> MarkerAsset { get; }
    public IBuildable Marker { get; private set; }

    public IMapTackUIHandler? UIHandler { get; private set; }

    public virtual Vector3 FeatureWorldPosition { get; }

    public StrategyMap Map { get; }

    public MapTack(StrategyMapManager manager, StrategyMap map, IAssetLink<ItemPlaceableAsset> markerAsset, Vector3 featureWorldPosition, IMapTackUIHandler? uiHandler = null, bool leaveUiHandlerOpen = false)
    {
        _strategyMapManager = manager;
        _leaveUiHandlerOpen = leaveUiHandlerOpen;
        Map = map;
        Marker = null!;
        MarkerAsset = markerAsset;
        FeatureWorldPosition = featureWorldPosition;
        UIHandler = uiHandler;
        if (uiHandler is not { ShouldShowInArea: true })
            return;

        uiHandler.OnPlayerEntered += HandlePlayerEntered;
        uiHandler.OnPlayerExited += HandlePlayerExited;
        _hasShowInAreaEvents = true;
        foreach (WarfarePlayer player in uiHandler.Players)
        {
            HandlePlayerEntered(player);
        }
    }

    public virtual void DropMarker(Vector3 worldCoordinatesOnTable, Quaternion rotation)
    {
        if (Marker != null)
            throw new InvalidOperationException("Map tack's marker has already been dropped. Map tack markers should not be dropped more than once.");

        Marker = BuildableExtensions.DropBuildable(MarkerAsset.GetAssetOrFail(), worldCoordinatesOnTable, rotation);
    }

    public void Dispose()
    {
        UnsubscribeUIListeners();
        WarfareModule.Singleton.GlobalLogger.LogConditional($"Map tack {MarkerAsset} destroyed.");
        _strategyMapManager.UI?.HandleTackDestroyed(this);
        Marker.Destroy();
        if (!_leaveUiHandlerOpen && UIHandler is IDisposable disp)
            disp.Dispose();

        if (!_hasShowInAreaEvents)
            return;

        _hasShowInAreaEvents = false;
        UIHandler!.OnPlayerEntered -= HandlePlayerEntered;
        UIHandler!.OnPlayerExited -= HandlePlayerExited;
        foreach (WarfarePlayer player in UIHandler.Players)
        {
            HandlePlayerExited(player);
        }
    }

    public Vector3 Position
    {
        get => Marker.Position;
        set => Marker.Position = value;
    }

    public Quaternion Rotation
    {
        get => Marker.Rotation;
        set => Marker.Rotation = value;
    }

    public Vector3 Scale
    {
        get => Vector3.one;
        set => throw new NotSupportedException();
    }

    public void SetPositionAndRotation(Vector3 position, Quaternion rotation)
    {
        Marker.SetPositionAndRotation(position, rotation);
    }

    public bool Alive => Marker.IsAlive;

    private void SubscribeUIListeners()
    {
        IMapTackUIHandler? uiHandler = UIHandler;
        if (uiHandler == null)
            return;

        uiHandler.OnVehicleUpdated += VehicleUpdated;
        uiHandler.OnAttributesUpdated += AttributesUpdated;
        uiHandler.OnHealthUpdated += HealthUpdated;
        uiHandler.OnSuppliesUpdated += SuppliesUpdated;
    }

    private void UnsubscribeUIListeners()
    {
        IMapTackUIHandler? uiHandler = UIHandler;
        if (uiHandler == null)
            return;

        uiHandler.OnVehicleUpdated -= VehicleUpdated;
        uiHandler.OnAttributesUpdated -= AttributesUpdated;
        uiHandler.OnHealthUpdated -= HealthUpdated;
        uiHandler.OnSuppliesUpdated -= SuppliesUpdated;
    }

    private void HandlePlayerEntered(WarfarePlayer player)
    {
        WarfareModule.Singleton.GlobalLogger.LogConditional($"Player entered {MarkerAsset}: {player}.");
        _strategyMapManager.UI?.HandlePlayerEntered(this, player);
    }

    private void HandlePlayerExited(WarfarePlayer player)
    {
        WarfareModule.Singleton.GlobalLogger.LogConditional($"Player exited {MarkerAsset}: {player}.");
        _strategyMapManager.UI?.HandlePlayerExited(this, player);
    }

    private void VehicleUpdated(MapTackVehicleType type, int amount)
    {
        WarfareModule.Singleton.GlobalLogger.LogConditional($"Vehicle updated in {MarkerAsset}: {type}={amount}.");
        _strategyMapManager.UI?.HandleVehicleUpdated(this, type, amount);
    }

    private void SuppliesUpdated(SupplyType type, int amount)
    {
        WarfareModule.Singleton.GlobalLogger.LogConditional($"Supply updated in {MarkerAsset}: {type}={amount}.");
        _strategyMapManager.UI?.HandleSuppliesUpdated(this, type, amount);
    }

    private void HealthUpdated(double? health)
    {
        WarfareModule.Singleton.GlobalLogger.LogConditional($"Health updated in {MarkerAsset}: {(health.HasValue ? health.Value.ToString(CultureInfo.InvariantCulture) : "HIDE HEALTH")}.");
        _strategyMapManager.UI?.HandleHealthUpdated(this, health);
    }

    private void AttributesUpdated(MapTackAttributes attributes)
    {
        WarfareModule.Singleton.GlobalLogger.LogConditional($"Attributes updated in {MarkerAsset}: {attributes}.");
        _strategyMapManager.UI?.HandleAttributesUpdated(this, attributes);
    }

    internal List<WarfarePlayer>.Enumerator EnumerateWatchers()
    {
        GameThread.AssertCurrent();

        _uiUpdateListeners ??= new List<WarfarePlayer>(4);
        return _uiUpdateListeners.GetEnumerator();
    }

    internal void AddUIUpdateListener(WarfarePlayer player)
    {
        GameThread.AssertCurrent();

        _uiUpdateListeners ??= new List<WarfarePlayer>(4);
        if (_uiUpdateListeners.Contains(player))
            return;

        _uiUpdateListeners.Add(player);
        if (_uiUpdateListeners.Count == 1)
        {
            SubscribeUIListeners();
        }
    }

    internal void RemoveUIUpdateListener(WarfarePlayer player)
    {
        GameThread.AssertCurrent();

        if (_uiUpdateListeners == null)
        {
            return;
        }

        int c = _uiUpdateListeners.RemoveAll(x => x.Equals(player));
        if (c > 0 && _uiUpdateListeners.Count == 0)
        {
            UnsubscribeUIListeners();
        }
    }
}
