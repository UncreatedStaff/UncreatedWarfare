using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Uncreated.Warfare.Buildables;
using Uncreated.Warfare.Interaction.Requests;
using Uncreated.Warfare.Layouts;
using Uncreated.Warfare.Layouts.Teams;
using Uncreated.Warfare.Locations;
using Uncreated.Warfare.Players;
using Uncreated.Warfare.Players.Management;
using Uncreated.Warfare.Signs;
using Uncreated.Warfare.Translations;
using Uncreated.Warfare.Translations.ValueFormatters;
using Uncreated.Warfare.Util;
using Uncreated.Warfare.Util.Timing;
using Uncreated.Warfare.Vehicles.Spawners.Delays;
using Uncreated.Warfare.Vehicles.WarfareVehicles;
using Uncreated.Warfare.Zones;

namespace Uncreated.Warfare.Vehicles.Spawners;

public class VehicleSpawner : IRequestable<VehicleSpawner>, IDisposable, ITranslationArgument
{
    public const float IdleDistance = 200;
    public const float EnemyIdleDistance = 40;

    private readonly ILogger _logger;
    private readonly SignInstancer _signInstancer;
    private readonly IPlayerService _playerService;
    private readonly ITeamManager<Team> _teamManager;
    private readonly VehicleService _vehicleService;
    private readonly Layout? _currentLayout;
    private readonly VehicleSpawnerLayoutConfigurer? _vehicleSpawnerSelector;
    private readonly ZoneStore _zoneStore;

    private DateTime _timeDestroyed;
    private DateTime _timeStartedIdle;
    private Zone? _lastLocation;
    private Vector2 _lastZoneCheckPos;
    private VehicleSpawnerState _state = VehicleSpawnerState.AwaitingRespawn;
    private bool _isSpawningVehicle;

    private readonly ILoopTicker _updateTicker;

    public VehicleSpawnInfo SpawnInfo { get; private set; }
    public WarfareVehicleInfo VehicleInfo { get; private set; }
    /// <summary>
    /// A vehicle that has been spawned from this spawn.
    /// </summary>
    public InteractableVehicle? LinkedVehicle { get; private set; }
    public Team Team { get; private set; }
    public IBuildable? Buildable { get; private set; }
    public List<IBuildable> Signs { get; }
    public string ServerSignText => "vbs_" + SpawnInfo.VehicleAsset.Guid.ToString("N", CultureInfo.InvariantCulture);

    public VehicleSpawnerState State
    {
        get => _state;
        private set
        {
            if (_state == value)
                return;

            _logger.LogConditional("Vehicle Spawner {0} state updated. {1} -> {2}.", ToDisplayString(), _state, value);
            _state = value;
        }
    }

    public LayoutRole TeamLayoutRole { get; private set; }

    public VehicleSpawner(VehicleSpawnInfo spawnerInfo, WarfareVehicleInfo vehicleInfo, ILoopTicker updateTicker, IServiceProvider layoutServiceProvider)
    {
        _logger = layoutServiceProvider.GetRequiredService<ILogger<VehicleSpawner>>();
        _signInstancer = layoutServiceProvider.GetRequiredService<SignInstancer>();
        _playerService = layoutServiceProvider.GetRequiredService<IPlayerService>();
        _teamManager = layoutServiceProvider.GetRequiredService<ITeamManager<Team>>();
        _zoneStore = layoutServiceProvider.GetRequiredService<ZoneStore>();
        _vehicleService = layoutServiceProvider.GetRequiredService<VehicleService>();
        _currentLayout = layoutServiceProvider.GetService<Layout>();
        _vehicleSpawnerSelector = layoutServiceProvider.GetService<VehicleSpawnerLayoutConfigurer>();
        _updateTicker = updateTicker;
        updateTicker.OnTick += Update;

        Signs = new List<IBuildable>();

        SpawnInfo = spawnerInfo;
        VehicleInfo = vehicleInfo;

        State = IsEnabledInLayout() ? VehicleSpawnerState.AwaitingRespawn : VehicleSpawnerState.LayoutDisabled;

        // set by ResolveBuildable
        Team = Team.NoTeam;

        ResolveBuildable(spawnerInfo);
        ResolveSigns(spawnerInfo);

        _logger.LogDebug($"Initialized spawner: {SpawnInfo.UniqueName}");
    }
    public void SoftReload(VehicleSpawnInfo newSpawnerInfo, WarfareVehicleInfo newWarfareVehicle)
    {
        SpawnInfo = newSpawnerInfo;
        VehicleInfo = newWarfareVehicle;

        ResolveBuildable(newSpawnerInfo);
        ResolveSigns(newSpawnerInfo);

        UpdateLinkedSigns();
    }
    private void ResolveBuildable(VehicleSpawnInfo spawnerInfo)
    {
        if (spawnerInfo.IsStructure)
        {
            StructureInfo buildableInfo = StructureUtility.FindStructure(spawnerInfo.BuildableInstanceId);
            if (buildableInfo.Drop == null)
            {
                _logger.LogWarning("Missing spawner structure for vehicle spawner '{0}' (Instance ID: {1} Vehicle Asset: {2}. " +
                                   "This spawner may be removed from config, or fixed by editing the Instance ID.",
                    spawnerInfo.UniqueName, spawnerInfo.BuildableInstanceId, spawnerInfo.VehicleAsset);
            }
            else
            {
                Buildable = new BuildableStructure(buildableInfo.Drop);
            }
        }
        else
        {
            BarricadeInfo buildableInfo = BarricadeUtility.FindBarricade(spawnerInfo.BuildableInstanceId);
            if (buildableInfo.Drop == null)
            {
                _logger.LogWarning("Missing spawner barricade for vehicle spawner '{0}' (Instance ID: {1} Vehicle Asset: {2}. " +
                                   "This spawner may be removed from config, or fixed by editing the Instance ID.",
                    spawnerInfo.UniqueName, spawnerInfo.BuildableInstanceId, spawnerInfo.VehicleAsset);
            }
            else
            {
                Buildable = new BuildableBarricade(buildableInfo.Drop);
            }
        }

            
        if (Buildable != null)
        {
            Team = _teamManager.GetTeam(Buildable.Group);

            if (_teamManager is TwoSidedTeamManager ts)
                TeamLayoutRole = ts.GetLayoutRole(Team);
            else
                TeamLayoutRole = LayoutRole.NotApplicable;

        }
        else
        {
            TeamLayoutRole = LayoutRole.NotApplicable;
            Team = Team.NoTeam;
        }
    }
    private void ResolveSigns(VehicleSpawnInfo spawnerInfo)
    {
        Signs.Clear();
        foreach (uint signInstanceId in spawnerInfo.SignInstanceIds)
        {
            BarricadeInfo signInfo = BarricadeUtility.FindBarricade(signInstanceId);
            if (signInfo.Drop == null)
            {
                _logger.LogWarning("Missing sign barricade for linked vehicle spawner '{0}' (Sign Instance ID: {1}). " +
                                   "This sign may be removed from config, or fixed by editing the Instance ID.",
                    spawnerInfo.UniqueName, signInstanceId, spawnerInfo.VehicleAsset);
            }
            else
            {
                Signs.Add(new BuildableBarricade(signInfo.Drop));

                // updates sign instance via the SignTextChanged event
                BarricadeUtility.SetServersideSignText(signInfo.Drop, ServerSignText);
            }
        }
    }

    /// <summary>
    /// Gets the location displayed on the sign when in use.
    /// </summary>
    public string GetLocation()
    {
        if (_lastLocation != null)
            return _lastLocation.ShortName ?? _lastLocation.Name;

        if (Buildable == null)
            return "Unknown";

        return new GridLocation(Buildable.Position).ToString();
    }

    /// <summary>
    /// Gets the time until respawn shown on the sign.
    /// </summary>
    public TimeSpan GetRespawnDueTime()
    {
        TimeSpan timeSpentIdle = DateTime.Now - _timeStartedIdle;
        if (State == VehicleSpawnerState.Idle)
        {
            return VehicleInfo.RespawnTime - timeSpentIdle;
        }
        else if (State == VehicleSpawnerState.Destroyed)
        {
            TimeSpan timeSpentDestroyed = DateTime.Now - _timeDestroyed;
            if (timeSpentIdle >= TimeSpan.Zero)
            {
                if (timeSpentIdle <= timeSpentDestroyed)
                    timeSpentDestroyed = timeSpentDestroyed.Subtract(timeSpentIdle);
                else
                    timeSpentDestroyed = TimeSpan.Zero;
            }

            return VehicleInfo.RespawnTime - timeSpentDestroyed;
        }

        return TimeSpan.Zero;
    }

    private void Update(ILoopTicker ticker, TimeSpan timeSinceStart, TimeSpan deltaTime)
    {
        if (Buildable == null)
            return;

        if (State is VehicleSpawnerState.Disposed or VehicleSpawnerState.Glitched)
            return;

        if (timeSinceStart.Seconds % 10 == 0)
        {
            if (!IsEnabledInLayout())
            {
                State = VehicleSpawnerState.LayoutDisabled;
                UpdateLinkedSigns();
            }
            else if (State == VehicleSpawnerState.LayoutDisabled)
            {
                State = VehicleSpawnerState.AwaitingRespawn;
                UpdateLinkedSigns();
            }
        }

        if (State == VehicleSpawnerState.LayoutDisabled)
            return;

        if (_isSpawningVehicle)
            return;

        if (State == VehicleSpawnerState.AwaitingRespawn)
        {
            if (LinkedVehicle == null)
            {
                RespawnVehicle();
            }

            if (LinkedVehicle != null && !LinkedVehicle.isExploded && !LinkedVehicle.isDead && !LinkedVehicle.isDrowned)
            {
                if (IsDelayed(out _))
                    State = VehicleSpawnerState.LayoutDelayed;
                else
                    State = VehicleSpawnerState.Ready;
                _lastLocation = null;
                _timeStartedIdle = DateTime.MaxValue;
                _timeDestroyed = DateTime.MaxValue;
                UpdateLinkedSigns();
            }
        }
        // Destroyed
        else if (LinkedVehicle == null || LinkedVehicle.isExploded || LinkedVehicle.isDead || LinkedVehicle.isDrowned)
        {
            if (State != VehicleSpawnerState.Destroyed)
            {
                State = VehicleSpawnerState.Destroyed;
                _timeDestroyed = DateTime.Now;
                _timeStartedIdle = DateTime.MaxValue;
                _lastLocation = null;
            }

            CheckRespawn();
            UpdateLinkedSigns();
        }
        // Delayed in Layout configuration
        else if (IsDelayed(out _))
        {
            if (State != VehicleSpawnerState.LayoutDelayed)
            {
                State = VehicleSpawnerState.LayoutDelayed;
            }
            UpdateLinkedSigns();
        }
        // Ready
        else if (LinkedVehicle.lockedOwner.m_SteamID == 0 && LinkedVehicle.isLocked)
        {
            if (State != VehicleSpawnerState.Ready)
            {
                State = VehicleSpawnerState.Ready;
                _lastLocation = null;
                _timeStartedIdle = DateTime.MaxValue;
                _timeDestroyed = DateTime.MaxValue;
                UpdateLinkedSigns();
            }
        }
        // Idle
        else if (IsIdle(LinkedVehicle))
        {
            if (State != VehicleSpawnerState.Idle)
            {
                State = VehicleSpawnerState.Idle;
                _lastLocation = null;
                _timeStartedIdle = DateTime.Now;
                _timeDestroyed = DateTime.MaxValue;
            }

            CheckRespawn();
            UpdateLinkedSigns();
        }
        // Deployed
        else
        {
            if (State != VehicleSpawnerState.Deployed)
            {
                State = VehicleSpawnerState.Deployed;
                UpdateLinkedSigns();
            }

            Vector3 vehiclePos = LinkedVehicle.transform.position;

            // every 2.5 meters moved horizontally re-check closest zone to update sign's in-use location
            if (_lastLocation == null || MathUtility.SquaredDistance(in vehiclePos, in _lastZoneCheckPos) > 2.5 * 2.5)
            {
                _lastZoneCheckPos.x = vehiclePos.x;
                _lastZoneCheckPos.y = vehiclePos.z;
                Zone? zone = _zoneStore.FindClosestZone(vehiclePos);
                if (!ReferenceEquals(zone, _lastLocation))
                {
                    _lastLocation = zone;
                    UpdateLinkedSigns();
                }
            }
        }
    }

    private bool IsIdle(InteractableVehicle vehicle)
    {
        if (vehicle.isDrowned || Provider.clients.Count == 0)
            return true;

        if (vehicle.anySeatsOccupied)
            return false;

        Vector3 pos = vehicle.transform.position;
        Team vehicleTeam = _teamManager.GetTeam(vehicle.lockedGroup);

        return !_playerService.OnlinePlayers.Any(x => x.InRadiusOf(in pos, EnemyIdleDistance) || x.Team.IsFriendly(vehicleTeam) && x.InRadiusOf(in pos, IdleDistance));
    }

    private void CheckRespawn()
    {
        if (State is not VehicleSpawnerState.Idle and not VehicleSpawnerState.Destroyed and not VehicleSpawnerState.LayoutDisabled)
            return;

        if (GetRespawnDueTime() <= TimeSpan.Zero)
            RespawnVehicle();
    }

    public void RespawnVehicle()
    {
        TryDestroyLinkedVehicle();

        UniTask.Create(async () =>
        {
            _isSpawningVehicle = true;
            try
            {
                await _vehicleService.SpawnVehicleAsync(this, CancellationToken.None);
                UpdateLinkedSigns();
            }
            catch (Exception ex)
            {
                State = VehicleSpawnerState.Glitched;
                WarfareModule.Singleton.GlobalLogger.LogError(ex, "Error respawning vehicle at spawner {0}.", VehicleInfo.VehicleAsset);
            }
            finally
            {
                _isSpawningVehicle = false;
            }
        });
    }
    public void TryDestroyLinkedVehicle()
    {
        if (LinkedVehicle != null && !(LinkedVehicle.isExploded || LinkedVehicle.isDead))
        {
            VehicleManager.askVehicleDestroy(LinkedVehicle);
            UnlinkVehicle();
        }
    }

    private void UpdateLinkedSigns()
    {
        if (Provider.clients.Count == 0)
            return;
        foreach (IBuildable sign in Signs)
        {
            if (sign.IsStructure || sign.IsDead)
                continue;

            _signInstancer.UpdateSign(sign.GetDrop<BarricadeDrop>());
        }
    }

    public static void StartLinkingSign(VehicleSpawner spawner, WarfarePlayer player)
    {
        GameThread.AssertCurrent();

        VehicleSpawnerLinkComponent comp = player.UnturnedPlayer.gameObject.GetOrAddComponent<VehicleSpawnerLinkComponent>();
        comp.Spawner = spawner;
    }

    public static VehicleSpawner? EndLinkingSign(WarfarePlayer player)
    {
        GameThread.AssertCurrent();

        VehicleSpawnerLinkComponent? comp = player.UnturnedPlayer.GetComponent<VehicleSpawnerLinkComponent>();
        if (comp == null || comp.Spawner == null)
            return null;

        Object.Destroy(comp);
        return comp.Spawner;
    }
    /// <summary>
    /// Link this spawn to a vehicle.
    /// </summary>
    internal void LinkVehicle(InteractableVehicle vehicle) // todo: replace with WarfareVehicle
    {
        GameThread.AssertCurrent();

        if (vehicle == LinkedVehicle)
            return;

        InteractableVehicle? oldVehicle = LinkedVehicle;
        LinkedVehicle = vehicle;
        if (oldVehicle != null && oldVehicle.TryGetComponent(out WarfareVehicleComponent oldVehicleComponent) && oldVehicleComponent.WarfareVehicle.Spawn == this)
        {
            oldVehicleComponent.WarfareVehicle.UnlinkFromSpawn(this);
        }

        if (vehicle.TryGetComponent(out WarfareVehicleComponent newVehicleComponent))
        {
            newVehicleComponent.WarfareVehicle.LinkToSpawn(this);
        }

        UpdateLinkedSigns();
    }


    /// <summary>
    /// Unlink this spawn from it's <see cref="LinkedVehicle"/>.
    /// </summary>
    internal void UnlinkVehicle()
    {
        GameThread.AssertCurrent();

        InteractableVehicle? oldVehicle = LinkedVehicle;
        if (oldVehicle is null)
            return;
        LinkedVehicle = null;

        UpdateLinkedSigns();

        if (oldVehicle == null || !oldVehicle.TryGetComponent(out WarfareVehicleComponent oldVehicleComponent))
        {
            return;
        }

        if (oldVehicleComponent.WarfareVehicle.Spawn == this)
        {
            oldVehicleComponent.WarfareVehicle.UnlinkFromSpawn(this);
        }
    }
    public bool IsDelayed(out TimeSpan timeLeft)
    {
        timeLeft = GetLayoutDelayTimeLeft();
        return timeLeft > TimeSpan.Zero;
    }
    public TimeSpan GetLayoutDelayTimeLeft()
    {
        if (_vehicleSpawnerSelector == null || _currentLayout == null)
            return TimeSpan.Zero;

        if (!_vehicleSpawnerSelector.TryGetSpawnerConfiguration(SpawnInfo, out VehicleSpawnerLayoutConfiguration? configuration))
            return TimeSpan.Zero;

        if (configuration.Delay == null)
            return TimeSpan.Zero;

        return configuration.Delay.GetTimeLeft(new LayoutDelayContext(_currentLayout, TeamLayoutRole));
    }
    public bool IsEnabledInLayout() => _vehicleSpawnerSelector?.IsEnabledInLayout(SpawnInfo) ?? true;

    public void Dispose()
    {
        _updateTicker.OnTick -= Update;
        TryDestroyLinkedVehicle();
        State = VehicleSpawnerState.Disposed;
        UpdateLinkedSigns();
            
    }
    public string ToDisplayString()
    {
        string buildableType;
        if (Buildable == null)
            buildableType = "Not Found";
        else if (Buildable.IsStructure)
            buildableType = $"Structure: {Buildable.InstanceId}";
        else
            buildableType = $"Barricade: {Buildable.InstanceId}";

        return $"'{SpawnInfo.UniqueName}' [{buildableType} - {VehicleInfo.VehicleAsset.ToDisplayString()}]";
    }

    /// <inheritdoc />
    public override string ToString()
    {
        return ToDisplayString();
    }

    /// <inheritdoc />
    public string Translate(ITranslationValueFormatter formatter, in ValueFormatParameters parameters)
    {
        return formatter.Colorize(VehicleInfo.VehicleAsset.Asset?.FriendlyName ?? VehicleInfo.VehicleAsset.Guid.ToString("N"), Team.Faction.Color, parameters.Options);
    }

    /// <summary>
    /// Tracks the spawner the player is linking a sign to.
    /// </summary>
    // ReSharper disable once ClassNeverInstantiated.Local
    private class VehicleSpawnerLinkComponent : MonoBehaviour
    {
        public VehicleSpawner? Spawner;

        [UsedImplicitly]
        private IEnumerator Start()
        {
            yield return new WaitForSeconds(30f);
            Destroy(this);
            Spawner = null;
        }
    }
}

public enum VehicleSpawnerState
{
    Destroyed,
    Deployed,
    Idle,
    Ready,
    LayoutDelayed,
    AwaitingRespawn,
    LayoutDisabled,
    Disposed,

    /// <summary>
    /// The vanilla vehicle couldn't spawn for some reason. This is deadlocked until next game.
    /// </summary>
    Glitched
}