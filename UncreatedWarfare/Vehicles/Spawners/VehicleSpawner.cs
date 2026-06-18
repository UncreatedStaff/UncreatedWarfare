using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Immutable;
using System.Globalization;
using System.Linq;
using Uncreated.Warfare.Buildables;
using Uncreated.Warfare.Configuration;
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
// ReSharper disable InconsistentlySynchronizedField

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
    private readonly VehicleSpawnerService _vehicleSpawnerService;
    private readonly VehicleSpawnerLayoutConfigurer? _vehicleSpawnerSelector;
    private readonly ZoneStore _zoneStore;
    private readonly IServiceProvider _serviceProvider;
    private VehicleSpawnerInfo _spawnInfo;

    private DateTime _timeDestroyed;
    private DateTime _timeStartedIdle;
    private Zone? _lastLocation;
    private Vector2 _lastZoneCheckPos;
    private VehicleSpawnerState _state;
    private bool _isSpawningVehicle;

    private readonly ILoopTicker _updateTicker;

    public Layout? Layout { get; }
    public VehicleSpawnerInfo SpawnInfo => _spawnInfo;
    public WarfareVehicleInfo VehicleInfo { get; private set; }
    /// <summary>
    /// A vehicle that has been spawned from this spawn.
    /// </summary>
    public InteractableVehicle? LinkedVehicle { get; private set; }
    public Team Team { get; }
    public IBuildable? Buildable { get; private set; }
    public ImmutableArray<IBuildable> Signs { get; private set; }
    public DateTime RequestTime { get; private set; }
    public string ServerSignText => "vbs_" + SpawnInfo.VehicleId.ToString("N", CultureInfo.InvariantCulture);

    public VehicleSpawnerState State
    {
        get => _state;
        private set
        {
            if (_state == value)
                return;

            if (_state is not VehicleSpawnerState.Deployed and not VehicleSpawnerState.Idle)
            {
                RequestTime = default;
            }

            _logger.LogConditional("Vehicle Spawner {0} state updated. {1} -> {2}.", ToDisplayString(), _state, value);
            _state = value;
        }
    }

    public LayoutRole TeamLayoutRole { get; }

    /// <exception cref="GameThreadException"/>
    public VehicleSpawner(
        VehicleSpawnerInfo spawnerInfo,
        ILoopTicker updateTicker,
        IServiceProvider layoutServiceProvider)
    {
        GameThread.AssertCurrent();

        _logger = layoutServiceProvider.GetRequiredService<ILogger<VehicleSpawner>>();
        _signInstancer = layoutServiceProvider.GetRequiredService<SignInstancer>();
        _playerService = layoutServiceProvider.GetRequiredService<IPlayerService>();
        _teamManager = layoutServiceProvider.GetRequiredService<ITeamManager<Team>>();
        _zoneStore = layoutServiceProvider.GetRequiredService<ZoneStore>();
        _vehicleService = layoutServiceProvider.GetRequiredService<VehicleService>();
        _vehicleSpawnerService = layoutServiceProvider.GetRequiredService<VehicleSpawnerService>();
        Layout = layoutServiceProvider.GetService<Layout>();
        _vehicleSpawnerSelector = layoutServiceProvider.GetService<VehicleSpawnerLayoutConfigurer>();
        _serviceProvider = layoutServiceProvider;
        _updateTicker = updateTicker;
        updateTicker.OnTick += Update;

        _spawnInfo = spawnerInfo;
        LoadVehicleInfo();

        _state = IsEnabledInLayout() ? VehicleSpawnerState.AwaitingRespawn : VehicleSpawnerState.LayoutDisabled;

        Team? team = _teamManager.FindTeam(_spawnInfo.Faction);
        if (team == null)
        {
            _logger.LogWarning($"Unknown team with faction {_spawnInfo.Faction}.");
            team = Team.NoTeam;
        }

        Team = team;

        if (_teamManager is TwoSidedTeamManager ts)
            TeamLayoutRole = ts.GetLayoutRole(Team);
        else
            TeamLayoutRole = LayoutRole.NotApplicable;

        ResolveBuildable();
        ResolveSigns();

        _logger.LogTrace($"Initialized spawner: {SpawnInfo.Id}");
    }

    /// <summary>
    /// Update the spawn data for this spawner.
    /// </summary>
    /// <exception cref="ArgumentNullException"/>
    /// <exception cref="AssetNotFoundException">Vehicle asset not found.</exception>
    public void UpdateSpawnInfo(VehicleSpawnerInfo newInfo)
    {
        GameThread.AssertCurrent();

        if (newInfo == null)
            throw new ArgumentNullException(nameof(newInfo));

        if (!VehicleInfo.VehicleAsset.MatchGuid(newInfo.VehicleId))
        {
            LoadVehicleInfo();
        }

        _spawnInfo = newInfo;

        ResolveBuildable();
        ResolveSigns();

        UpdateLinkedSignsOnMainThread();
    }

    [MemberNotNull(nameof(VehicleInfo))]
    private void LoadVehicleInfo()
    {
        VehicleAsset? vehicleAsset = Assets.find<VehicleAsset>(SpawnInfo.VehicleId);
        if (vehicleAsset == null)
        {
            throw new AssetNotFoundException(
                AssetLink.Create<VehicleAsset>(SpawnInfo.VehicleId),
                nameof(VehicleSpawnerInfo.VehicleId)
            );
        }

        WarfareVehicleInfo? info = _vehicleService.Info.GetVehicleInfo(vehicleAsset);
        if (info == null)
        {
            _logger.LogWarning($"Vehicle info not found for vehicle {vehicleAsset}, falling back to default data.");
            info = WarfareVehicleInfo.CreateDefault(vehicleAsset);
        }

        if (VehicleInfo != null && !VehicleInfo.VehicleAsset.MatchGuid(SpawnInfo.VehicleId))
        {
            _logger.LogWarning($"Updated vehicle of spawner {SpawnInfo.Id}: {VehicleInfo.VehicleAsset} -> {info.VehicleAsset}.");
        }

        VehicleInfo = info;
    }

    private void ResolveBuildable()
    {
        IBuildable? buildable = null;

        AssetConfiguration config = _serviceProvider.GetRequiredService<AssetConfiguration>();
        IAssetLink<ItemPlaceableAsset> bay = config.GetAssetLink<ItemPlaceableAsset>(
            "Buildables:Gameplay:VehicleSpawner"
        );

        if (!bay.TryGetAsset(out ItemPlaceableAsset? asset))
        {
            _logger.LogError("Vehicle bay not found in configuration.");
            return;
        }


        lock (_vehicleSpawnerService.SpawnerBuildableMap)
        {
            Buildable = null;

            if (_vehicleSpawnerService.SpawnerBuildableMap.TryGetValue(SpawnInfo.Id, out VehicleSpawnerService.SpawnerBuildables buildables))
            {
                if (!buildables.Bay.TryGetBuildable(out buildable))
                {
                    _logger.LogWarning($"Missing cached buildable {buildables.Bay} for vehicle spawner {SpawnInfo.Id}.");
                }
            }

            Vector3 spawnPos = SpawnInfo.Position;
            Vector3 spawnRot = new Vector3(SpawnInfo.RotationX, SpawnInfo.RotationY, SpawnInfo.RotationZ);
            Quaternion offsetSpawnRot = Quaternion.Euler(spawnRot) * BarricadeUtility.DefaultBarricadeRotation;
            if (buildable is not { IsAlive: true })
            {
                buildable = BuildableExtensions.DropBuildable(asset, spawnPos, offsetSpawnRot, CSteamID.Nil, Team.GroupId);
                _logger.LogInformation($"Placed missing buildable for spawner {SpawnInfo.Id}.");
            }
            else if (!bay.MatchAsset(buildable.Asset))
            {
                _logger.LogInformation($"Replaced buildable for spawner {SpawnInfo.Id} because the asset changed. Update: {buildable.Asset} -> {bay}.");
                buildable.Destroy();
                buildable = BuildableExtensions.DropBuildable(asset, spawnPos, offsetSpawnRot, CSteamID.Nil, Team.GroupId);
            }
            else
            {
                Vector3 pos = buildable.Position;
                Vector3 euler = (buildable.Rotation * BarricadeUtility.InverseDefaultBarricadeRotation).eulerAngles;
                if (buildable.Group != Team.GroupId)
                {
                    _logger.LogInformation($"Updated out-of-date group ID of spawner {SpawnInfo.Id}. Update: {buildable.Group} -> {Team.GroupId}.");
                    buildable.SetOwnerOrGroup(_serviceProvider, group: Team.GroupId);
                }

                if (!pos.IsNearlyEqual(spawnPos, 0.1f) || !MathUtility.IsRotationNearlyEqual(spawnRot, euler, 1.5f))
                {
                    _logger.LogInformation(
                        $"Updated out-of-date transform of spawner {SpawnInfo.Id}. Update: {pos} -> {spawnPos}, {euler} -> {spawnRot}."
                    );
                    buildable.SetPositionAndRotation(spawnPos, offsetSpawnRot);
                }
            }

            buildables.Bay = new BuildableDescriptor(buildable);

            _vehicleSpawnerService.SpawnerBuildableMap[SpawnInfo.Id] = buildables;
            _vehicleSpawnerService.SpawnerBuildableMapIsDirty = true;
        }

        Buildable = buildable;
    }
    private void ResolveSigns()
    {
        AssetConfiguration config = _serviceProvider.GetRequiredService<AssetConfiguration>();
        IAssetLink<ItemBarricadeAsset> bay = config.GetAssetLink<ItemBarricadeAsset>(
            "Buildables:Gameplay:VehicleSpawnerSign"
        );

        if (!bay.TryGetAsset(out ItemBarricadeAsset? asset))
        {
            _logger.LogError("Vehicle bay sign not found in configuration.");
            Signs = ImmutableArray<IBuildable>.Empty;
            return;
        }

        lock (_vehicleSpawnerService.SpawnerBuildableMap)
        {
            BuildableDescriptor[] descriptors;
            if (_vehicleSpawnerService.SpawnerBuildableMap.TryGetValue(SpawnInfo.Id, out VehicleSpawnerService.SpawnerBuildables buildables))
                descriptors = buildables.Signs ?? Array.Empty<BuildableDescriptor>();
            else
            {
                descriptors = Array.Empty<BuildableDescriptor>();
                if (Buildable != null)
                    buildables.Bay = new BuildableDescriptor(Buildable);
            }

            BuildableDescriptor[] outDescriptors = descriptors;
            if (outDescriptors.Length < SpawnInfo.Signs.Length)
            {
                outDescriptors = new BuildableDescriptor[SpawnInfo.Signs.Length];
                for (int i = 0; i < descriptors.Length; ++i)
                    outDescriptors[i] = descriptors[i];
            }

            IBuildable[] signs = new IBuildable[SpawnInfo.Signs.Length];

            Span<char> expectedSignText = stackalloc char[36];
            expectedSignText[0] = 'v';
            expectedSignText[1] = 'b';
            expectedSignText[2] = 's';
            expectedSignText[3] = '_';

            for (int i = 0; i < SpawnInfo.Signs.Length; i++)
            {
                VehicleSpawnerSignInfo sign = SpawnInfo.Signs[i];

                IBuildable? buildable = null;
                if (i < descriptors.Length)
                {
                    BuildableDescriptor desc = descriptors[i];
                    if (desc.IsStructure || !desc.TryGetBuildable(out buildable))
                    {
                        _logger.LogWarning($"Missing cached sign {desc} for vehicle spawner {SpawnInfo.Id} sign {i}.");
                    }
                }

                Vector3 spawnPos = sign.Position;
                Vector3 spawnRot = new Vector3(sign.RotationX, sign.RotationY, sign.RotationZ);
                Quaternion offsetSpawnRot = Quaternion.Euler(spawnRot) * BarricadeUtility.DefaultBarricadeRotation;
                bool justPlaced = false;
                if (buildable is not { IsAlive: true })
                {
                    justPlaced = true;
                    buildable = BuildableExtensions.DropBuildable(asset, spawnPos, offsetSpawnRot, CSteamID.Nil, Team.GroupId);
                    _logger.LogInformation($"Placed missing buildable for spawner {SpawnInfo.Id} sign {i}.");
                }
                else if (!bay.MatchAsset(buildable.Asset))
                {
                    justPlaced = true;
                    _logger.LogInformation($"Replaced buildable for spawner {SpawnInfo.Id} sign {i} because the asset changed. Update: {buildable.Asset} -> {bay}.");
                    buildable.Destroy();
                    buildable = BuildableExtensions.DropBuildable(asset, spawnPos, offsetSpawnRot, CSteamID.Nil, Team.GroupId);
                }
                else
                {
                    Vector3 pos = buildable.Position;
                    Vector3 euler = (buildable.Rotation * BarricadeUtility.InverseDefaultBarricadeRotation).eulerAngles;
                    if (buildable.Group != Team.GroupId)
                    {
                        _logger.LogInformation($"Updated out-of-date group ID of spawner {SpawnInfo.Id} sign {i}. Update: {buildable.Group} -> {Team.GroupId}.");
                        buildable.SetOwnerOrGroup(_serviceProvider, group: Team.GroupId);
                    }

                    if (!pos.IsNearlyEqual(spawnPos, 0.1f) || !MathUtility.IsRotationNearlyEqual(spawnRot, euler, 1.5f))
                    {
                        _logger.LogInformation($"Updated out-of-date transform of spawner {SpawnInfo.Id} sign {i}. Update: {pos} -> {spawnPos}, {euler} -> {spawnRot}.");
                        buildable.SetPositionAndRotation(spawnPos, offsetSpawnRot);
                    }

                    SpawnInfo.VehicleId.TryFormat(expectedSignText.Slice(4), out _, "N");
                }

                BarricadeDrop drop = buildable.GetDrop<BarricadeDrop>();
                if (drop.interactable is InteractableSign signInfo && !expectedSignText.Equals(signInfo.text, StringComparison.Ordinal))
                {
                    if (!justPlaced)
                    {
                        _logger.LogInformation($"Updated out-of-date sign text of spawner {SpawnInfo.Id} sign {i}. Update: {signInfo.text} -> {expectedSignText}.");
                    }
                    BarricadeUtility.SetServersideSignText(drop, expectedSignText);
                    _signInstancer.UpdateSign(drop);
                }

                outDescriptors[i] = new BuildableDescriptor(buildable);
                signs[i] = buildable;
            }

            buildables.Signs = outDescriptors;
            _vehicleSpawnerService.SpawnerBuildableMap[SpawnInfo.Id] = buildables;
            _vehicleSpawnerService.SpawnerBuildableMapIsDirty = true;

            for (int i = outDescriptors.Length; i < descriptors.Length; ++i)
            {
                if (!descriptors[i].TryGetBuildable(out IBuildable? buildable))
                    continue;

                buildable.Destroy();
                _logger.LogInformation($"Removed extra vehicle sign {i} on spawner {SpawnInfo.Id}.");
            }

            Signs = ImmutableArray.Create(signs);
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
        if (Buildable == null || _isSpawningVehicle)
            return;

        if (State is VehicleSpawnerState.Disposed or VehicleSpawnerState.Glitched)
            return;

        if (!IsEnabledInLayout())
        {
            State = VehicleSpawnerState.LayoutDisabled;
            UpdateLinkedSignsOnMainThread();
        }
        else if (State == VehicleSpawnerState.LayoutDisabled)
        {
            State = VehicleSpawnerState.AwaitingRespawn;
            UpdateLinkedSignsOnMainThread();
        }

        if (State == VehicleSpawnerState.LayoutDisabled)
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
                UpdateLinkedSignsOnMainThread();
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
            UpdateLinkedSignsOnMainThread();
        }
        // Delayed in Layout configuration
        else if (IsDelayed(out _))
        {
            if (State != VehicleSpawnerState.LayoutDelayed)
            {
                State = VehicleSpawnerState.LayoutDelayed;
            }
            _lastLocation = null;
            _timeStartedIdle = DateTime.MaxValue;
            _timeDestroyed = DateTime.MaxValue;
            UpdateLinkedSignsOnMainThread();
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
                UpdateLinkedSignsOnMainThread();
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
            UpdateLinkedSignsOnMainThread();
        }
        // Deployed
        else
        {
            bool update = false;
            if (State != VehicleSpawnerState.Deployed)
            {
                State = VehicleSpawnerState.Deployed;
                update = true;
            }

            Vector3 vehiclePos = LinkedVehicle.transform.position;

            // every 2.5 meters moved horizontally re-check closest zone to update sign's in-use location
            if (update || _lastLocation == null || MathUtility.SquaredDistance(in vehiclePos, in _lastZoneCheckPos) > 2.5 * 2.5)
            {
                _lastZoneCheckPos.x = vehiclePos.x;
                _lastZoneCheckPos.y = vehiclePos.z;
                Zone? zone = _zoneStore.GetClosestZone(vehiclePos, null, null, isForLocation: true);
                if (!ReferenceEquals(zone, _lastLocation))
                {
                    _lastLocation = zone;
                    update = true;
                }
            }

            if (update)
            {
                UpdateLinkedSignsOnMainThread();
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
                await UniTask.SwitchToMainThread();
                UpdateLinkedSignsOnMainThread();
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

    /// <remarks>Thread-safe</remarks>
    public void TryDestroyLinkedVehicle()
    {
        if (GameThread.IsCurrent)
        {
            TryDestroyLinkedVehicleOnMainThread();
        }
        else
        {
            UniTask.Create(async () =>
            {
                await UniTask.SwitchToMainThread();
                TryDestroyLinkedVehicleOnMainThread();
            });
        }
    }

    private void TryDestroyLinkedVehicleOnMainThread()
    {
        if (LinkedVehicle == null || LinkedVehicle.isExploded || LinkedVehicle.isDead)
            return;

        VehicleManager.askVehicleDestroy(LinkedVehicle);
        UnlinkVehicle();
    }

    /// <remarks>Thread-safe</remarks>
    private void UpdateLinkedSigns()
    {
        if (GameThread.IsCurrent)
        {
            UpdateLinkedSignsOnMainThread();
        }
        else
        {
            UniTask.Create(async () =>
            {
                await UniTask.SwitchToMainThread();
                UpdateLinkedSignsOnMainThread();
            });
        }
    }

    private void UpdateLinkedSignsOnMainThread()
    {
        if (Provider.clients.Count == 0)
            return;
        foreach (IBuildable sign in Signs)
        {
            if (sign.IsStructure || !sign.Alive)
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
    internal void LinkVehicle(WarfareVehicle vehicle)
    {
        GameThread.AssertCurrent();

        if (vehicle.Vehicle == LinkedVehicle)
            return;

        InteractableVehicle? oldVehicle = LinkedVehicle;
        LinkedVehicle = vehicle.Vehicle;
        if (oldVehicle != null && oldVehicle.TryGetComponent(out WarfareVehicleComponent oldVehicleComponent) && oldVehicleComponent.WarfareVehicle.Spawn == this)
        {
            oldVehicleComponent.WarfareVehicle.UnlinkFromSpawn(this);
        }

        if (vehicle.Vehicle.TryGetComponent(out WarfareVehicleComponent newVehicleComponent))
        {
            newVehicleComponent.WarfareVehicle.LinkToSpawn(this);
        }

        UpdateLinkedSignsOnMainThread();
    }


    /// <summary>
    /// Unlink this spawn from it's <see cref="LinkedVehicle"/>.
    /// </summary>
    internal void UnlinkVehicle(bool holdSignLink = false)
    {
        GameThread.AssertCurrent();

        InteractableVehicle? oldVehicle = LinkedVehicle;
        if (oldVehicle is null)
            return;
        LinkedVehicle = null;

        if (!holdSignLink)
            UpdateLinkedSignsOnMainThread();

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
    internal void NotifyRequsted()
    {
        RequestTime = DateTime.UtcNow;
    }
    public TimeSpan GetLayoutDelayTimeLeft()
    {
        if (_vehicleSpawnerSelector == null || Layout == null)
            return TimeSpan.Zero;

        if (!_vehicleSpawnerSelector.TryGetSpawnerConfiguration(SpawnInfo, out VehicleSpawnerLayoutConfiguration? configuration))
            return TimeSpan.Zero;

        if (configuration.Delay == null)
            return TimeSpan.Zero;

        return configuration.Delay.GetTimeLeft(new LayoutDelayContext(Layout, TeamLayoutRole));
    }
    public bool IsEnabledInLayout()
    {
        return _vehicleSpawnerSelector?.IsEnabledInLayout(SpawnInfo) ?? true;
    }

    public void Dispose()
    {
        _updateTicker.OnTick -= Update;
        TryDestroyLinkedVehicle();
        State = VehicleSpawnerState.Disposed;
        //UpdateLinkedSigns();
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

        return $"'{SpawnInfo.Id}' [{buildableType} - {VehicleInfo.VehicleAsset.ToDisplayString()}]";
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