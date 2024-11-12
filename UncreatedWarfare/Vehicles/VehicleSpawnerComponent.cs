using Microsoft.Extensions.DependencyInjection;
using System;
using System.Linq;
using Uncreated.Warfare.Buildables;
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
public class VehicleSpawnerComponent : MonoBehaviour, IManualOnDestroy
{
    public const float IdleDistance = 200;
    public const float EnemyIdleDistance = 20;

    private SignInstancer _signInstancer;
    private IPlayerService _playerService;
    private ITeamManager<Team> _teamManager;
    private VehicleService _vehicleService;

    private float _destroyedTime;
    private float _idleTime;
    private float _lastSignUpdate;
    private float _lastRealtime;
    private ZoneStore _zoneStore;
    private Zone? _lastLocation;
    private Vector2 _lastZoneCheckPos;

    public VehicleSpawnInfo SpawnInfo { get; private set; }
    public WarfareVehicleInfo VehicleInfo { get; private set; }
    public VehicleSpawnerState State { get; private set; }

    public void Init(VehicleSpawnInfo spawnInfo, WarfareVehicleInfo vehicle, IServiceProvider serviceProvider)
    {
        SpawnInfo = spawnInfo;
        VehicleInfo = vehicle;

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
            VehicleSpawnerState.Destroyed => TimeSpan.FromSeconds(rt - _destroyedTime),
            VehicleSpawnerState.Idle => TimeSpan.FromSeconds(rt - _idleTime),
            _ => TimeSpan.Zero
        };
    }

    [UsedImplicitly]
    private void Update()
    {
        float rt = Time.realtimeSinceStartup;

        bool forceUpdate = false;

        // Destroyed
        if (SpawnInfo.LinkedVehicle is null || SpawnInfo.LinkedVehicle.isExploded)
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
        if (vehicle.isDrowned)
            return true;

        if (vehicle.anySeatsOccupied)
            return false;

        Vector3 pos = vehicle.transform.position;
        Team vehicleTeam = _teamManager.GetTeam(vehicle.lockedGroup);

        return !_playerService.OnlinePlayers.Any(x => x.InRadiusOf(pos, EnemyIdleDistance) || x.Team.IsFriendly(vehicleTeam) && x.InRadiusOf(pos, IdleDistance));
    }

    private void CheckRespawn(float rt)
    {
        if (State == VehicleSpawnerState.Ready)
            return;

        if (rt - _destroyedTime >= VehicleInfo.RespawnTime.TotalSeconds)
        {
            RespawnVehicle();
        }
    }

    public void RespawnVehicle()
    {
        _ = _vehicleService.SpawnVehicleAsync(SpawnInfo, CancellationToken.None);
    }

    private void UpdateLinkedSignsTick(float rt)
    {
        // basically wait until realtime ticks from .997 to 1.014 or whatever, keeps signs in sync with each other
        if (_lastSignUpdate < 1 || (int)Math.Round(rt) != (int)Math.Floor(rt) || (int)Math.Round(_lastRealtime) != (int)Math.Floor(_lastRealtime) - 1)
        {
            return;
        }

        UpdateLinkedSigns(rt);
    }

    public void UpdateLinkedSigns()
    {
        GameThread.AssertCurrent();

        _lastSignUpdate = 0;
        Update();
        UpdateLinkedSigns(Time.realtimeSinceStartup);
    }

    private void UpdateLinkedSigns(float rt)
    {
        _lastSignUpdate = rt;
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
    Ready
}
#pragma warning restore IDE0051