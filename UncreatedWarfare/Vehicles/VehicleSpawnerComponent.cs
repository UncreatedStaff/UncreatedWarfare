using Microsoft.Extensions.DependencyInjection;
using System;
using System.Linq;
using Uncreated.Warfare.Buildables;
using Uncreated.Warfare.Interaction.Requests;
using Uncreated.Warfare.Layouts.Teams;
using Uncreated.Warfare.Locations;
using Uncreated.Warfare.Players;
using Uncreated.Warfare.Players.Management;
using Uncreated.Warfare.Signs;
using Uncreated.Warfare.Util;
using Uncreated.Warfare.Zones;

namespace Uncreated.Warfare.Vehicles;

#pragma warning disable IDE0051 // unity messages

/// <summary>
/// Handles logic for spawning vehicles.
/// </summary>
public class VehicleSpawnerComponent : MonoBehaviour, IManualOnDestroy, IRequestable<VehicleSpawnInfo>
{
    public const float IdleDistance = 200;
    public const float EnemyIdleDistance = 40;

    private SignInstancer _signInstancer;
    private IPlayerService _playerService;
    private ITeamManager<Team> _teamManager;
    private VehicleService _vehicleService;

    private bool _hasInitialized;
    private float _destroyedTime;
    private float _idleTime;
    private float _lastSignUpdate;
    private float _lastRealtime;
    private ZoneStore _zoneStore;
    private Zone? _lastLocation;
    private Vector2 _lastZoneCheckPos;
    private VehicleSpawnerState _state = VehicleSpawnerState.Uninitialized;
    private bool _isSpawningVehicle;

    public VehicleSpawnInfo SpawnInfo { get; private set; }
    public WarfareVehicleInfo VehicleInfo { get; private set; }

    public VehicleSpawnerState State
    {
        get => _state;
        private set
        {
            if (_state == value)
                return;

            // todo back to auto-property
            WarfareModule.Singleton.GlobalLogger.LogConditional("State updated for {0}. {1} -> {2}.", VehicleInfo.Vehicle.ToDisplayString(), _state, value);
            _state = value;
        }
    }

    public void Init(VehicleSpawnInfo spawnInfo, WarfareVehicleInfo vehicle, IServiceProvider serviceProvider)
    {
        SpawnInfo = spawnInfo;
        VehicleInfo = vehicle;

        _state = VehicleSpawnerState.Uninitialized;
        _signInstancer = serviceProvider.GetRequiredService<SignInstancer>();
        _playerService = serviceProvider.GetRequiredService<IPlayerService>();
        _teamManager = serviceProvider.GetRequiredService<ITeamManager<Team>>();
        _zoneStore = serviceProvider.GetRequiredService<ZoneStore>();
        _vehicleService = serviceProvider.GetRequiredService<VehicleService>();
    }

    /// <summary>
    /// Gets the location displayed on the sign when in use.
    /// </summary>
    public string GetLocation()
    {
        if (_lastLocation != null)
            return _lastLocation.ShortName ?? _lastLocation.Name;
        
        return new GridLocation(transform.position).ToString();
    }

    /// <summary>
    /// Gets the time until respawn shown on the sign.
    /// </summary>
    public TimeSpan GetRespawnDueTime()
    {
        float rt = Time.realtimeSinceStartup;
        return State switch
        {
            VehicleSpawnerState.Destroyed => TimeSpan.FromSeconds(VehicleInfo.RespawnTime.TotalSeconds - (rt - _destroyedTime)),
            VehicleSpawnerState.Idle => TimeSpan.FromSeconds(VehicleInfo.RespawnTime.TotalSeconds - (rt - _idleTime)),
            _ => TimeSpan.Zero
        };
    }

    [UsedImplicitly]
    private void Update()
    {
        Update(Time.realtimeSinceStartup);
    }

    private void Update(float rt)
    {
        if (_isSpawningVehicle)
            return;
     
        bool forceUpdate = false;

        if (State == VehicleSpawnerState.Uninitialized)
        {
            if (!_hasInitialized)
            {
                RespawnVehicle();
                _hasInitialized = true;
            }

            if (SpawnInfo.LinkedVehicle != null && !SpawnInfo.LinkedVehicle.isExploded && !SpawnInfo.LinkedVehicle.isDead && !SpawnInfo.LinkedVehicle.isDrowned)
            {
                State = VehicleSpawnerState.Ready;
                forceUpdate = true;
                _lastLocation = null;
                _idleTime = 0;
                _destroyedTime = 0;
            }
        }
        // Destroyed
        else if (SpawnInfo.LinkedVehicle == null || SpawnInfo.LinkedVehicle.isExploded || SpawnInfo.LinkedVehicle.isDead || SpawnInfo.LinkedVehicle.isDrowned)
        {
            if (State != VehicleSpawnerState.Destroyed)
            {
                State = VehicleSpawnerState.Destroyed;
                forceUpdate = true;
                _lastLocation = null;
                // carry over idle time to destroy time
                _destroyedTime = _idleTime == 0 ? rt : _idleTime;
            }

            CheckRespawn(rt);
            UpdateLinkedSignsTick(rt);
        }
        // Ready
        else if (SpawnInfo.LinkedVehicle.lockedOwner.m_SteamID == 0 && SpawnInfo.LinkedVehicle.isLocked)
        {
            if (State != VehicleSpawnerState.Ready)
            {
                State = VehicleSpawnerState.Ready;
                _lastLocation = null;
                forceUpdate = true;
                _idleTime = 0;
                _destroyedTime = 0;
            }
        }
        // Idle
        else if (IsIdle(SpawnInfo.LinkedVehicle))
        {
            if (State != VehicleSpawnerState.Idle)
            {
                State = VehicleSpawnerState.Idle;
                forceUpdate = true;
                _lastLocation = null;
                _idleTime = rt;
                _destroyedTime = 0;
            }

            CheckRespawn(rt);
            UpdateLinkedSignsTick(rt);
        }
        // Deployed
        else
        {
            if (State != VehicleSpawnerState.Deployed)
            {
                _idleTime = 0;
                _destroyedTime = 0;
                forceUpdate = true;
                State = VehicleSpawnerState.Deployed;
            }

            Vector3 vehiclePos = SpawnInfo.LinkedVehicle.transform.position;

            // every 2.5 meters moved horizontally re-check closest zone to update sign's in-use location
            if (_lastLocation == null || MathUtility.SquaredDistance(in vehiclePos, in _lastZoneCheckPos) > 2.5 * 2.5)
            {
                _lastZoneCheckPos.x = vehiclePos.x;
                _lastZoneCheckPos.y = vehiclePos.z;
                Zone? zone = _zoneStore.FindClosestZone(vehiclePos);
                if (zone != _lastLocation)
                {
                    _lastLocation = zone;
                    forceUpdate = true;
                }
            }
        }

        _lastRealtime = rt;
        if (forceUpdate && _lastSignUpdate != rt)
            UpdateLinkedSigns(rt);
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

    private void CheckRespawn(float rt)
    {
        if (State is not VehicleSpawnerState.Idle and not VehicleSpawnerState.Destroyed)
            return;

        if (rt - (State == VehicleSpawnerState.Idle ? _idleTime : _destroyedTime) >= VehicleInfo.RespawnTime.TotalSeconds)
        {
            RespawnVehicle();
        }
    }

    public void RespawnVehicle()
    {
        // WarfareModule.Singleton.GlobalLogger.LogInformation("Respawning");
        if (SpawnInfo.LinkedVehicle != null && !(SpawnInfo.LinkedVehicle.isExploded || SpawnInfo.LinkedVehicle.isDead))
        {
            VehicleManager.askVehicleDestroy(SpawnInfo.LinkedVehicle);
            SpawnInfo.UnlinkVehicle();
        }

        UniTask.Create(async () =>
        {
            _isSpawningVehicle = true;
            try
            {
                await _vehicleService.SpawnVehicleAsync(SpawnInfo, CancellationToken.None);
            }
            catch (Exception ex)
            {
                WarfareModule.Singleton.GlobalLogger.LogError(ex, "Error respawning vehicle at spawner {0}.", VehicleInfo.Vehicle);
            }
            finally
            {
                _isSpawningVehicle = false;
            }
        });
    }

    private void UpdateLinkedSignsTick(float rt)
    {
        // basically wait until realtime ticks from .997 to 1.014 or whatever, keeps signs in sync with each other
        if (_lastSignUpdate < 1 || _lastRealtime > (int)Math.Floor(rt))
        {
            return;
        }

        UpdateLinkedSigns(rt);
    }

    public void UpdateLinkedSigns()
    {
        GameThread.AssertCurrent();

        _lastSignUpdate = 0;
        float rt = Time.realtimeSinceStartup;
        Update(rt);
        
        if (_lastSignUpdate != rt)
            UpdateLinkedSigns(rt);
    }

    private void UpdateLinkedSigns(float rt)
    {
        // WarfareModule.Singleton.GlobalLogger.LogInformation("Update sign");
        _lastSignUpdate = rt;
        if (Provider.clients.Count == 0)
            return;
        foreach (IBuildable sign in SpawnInfo.SignInstanceIds)
        {
            if (sign.IsStructure || sign.IsDead)
                continue;

            _signInstancer.UpdateSign(sign.GetDrop<BarricadeDrop>());
        }
    }

    void IManualOnDestroy.ManualOnDestroy()
    {
        Destroy(this);
    }

    public static void StartLinkingSign(VehicleSpawnerComponent spawner, WarfarePlayer player)
    {
        GameThread.AssertCurrent();

        VehicleSpawnerLinkComponent comp = player.UnturnedPlayer.gameObject.GetOrAddComponent<VehicleSpawnerLinkComponent>();
        comp.Spawner = spawner;
    }

    public static VehicleSpawnInfo? EndLinkingSign(WarfarePlayer player)
    {
        GameThread.AssertCurrent();

        VehicleSpawnerLinkComponent? comp = player.UnturnedPlayer.GetComponent<VehicleSpawnerLinkComponent>();
        if (comp == null || comp.Spawner == null)
            return null;

        VehicleSpawnInfo info = comp.Spawner.SpawnInfo;
        Destroy(comp.Spawner);
        return info;
    }

    /// <summary>
    /// Tracks the spawner the player is linking a sign to.
    /// </summary>
    // ReSharper disable once ClassNeverInstantiated.Local
    private class VehicleSpawnerLinkComponent : MonoBehaviour
    {
        public VehicleSpawnerComponent? Spawner;

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
    Uninitialized
}
#pragma warning restore IDE0051