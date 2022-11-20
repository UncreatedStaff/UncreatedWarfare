using SDG.Unturned;
using System.Collections.Generic;
using Uncreated.Warfare.Components;
using Uncreated.Warfare.Vehicles;
using VehicleSpawn = Uncreated.Warfare.Vehicles.VehicleSpawn;

namespace Uncreated.Warfare.Events.Vehicles;
public class VehicleDestroyed : EventState
{
    private readonly InteractableVehicle _vehicle;
    private readonly VehicleBayComponent? _vehicleSpawnComponent;
    private readonly VehicleComponent? _component;
    private readonly SpottedComponent? _spotter;
    private readonly VehicleSpawn? _vehicleSpawn;
    private readonly VehicleData? _vehicleData;
    private readonly UCPlayer? _lockedOwner;
    private readonly UCPlayer? _instigator;
    private readonly UCPlayer? _lastDriver;
    private readonly ulong _ownerId;
    private readonly ulong _instigatorId;
    private readonly ulong _lastDriverId;
    private readonly ulong _lockedTeam;
    public InteractableVehicle Vehicle => _vehicle;
    public VehicleBayComponent? SpawnComponent => _vehicleSpawnComponent;
    public VehicleComponent? Component => _component;
    public SpottedComponent? Spotter => _spotter;
    public VehicleSpawn? Spawn => _vehicleSpawn;
    public VehicleData? VehicleData => _vehicleData;
    public UCPlayer? Owner => _lockedOwner;
    public UCPlayer? Instigator => _instigator;
    public UCPlayer? LastDriver => _lastDriver;
    public ulong OwnerId => _ownerId;
    public ulong InstigatorId => _instigatorId;
    public ulong LastDriverId => _lastDriverId;
    public ulong Team => _lockedTeam;
    public KeyValuePair<ulong, float>[] Assists { get; set; }
    public VehicleDestroyed(InteractableVehicle vehicle, SpottedComponent? spotted)
    {
        _spotter = spotted;
        _vehicle = vehicle;
        _lockedTeam = vehicle.lockedGroup.m_SteamID.GetTeam();
        _ownerId = vehicle.lockedOwner.m_SteamID;
        _lockedOwner = UCPlayer.FromID(_ownerId);
        _component = vehicle.GetComponent<VehicleComponent>();
        _vehicleData = VehicleBay.GetSingletonQuick()?.GetDataSync(vehicle.asset.GUID);
        if (_component != null)
        {
            if (_component.LastInstigator != 0)
            {
                _instigator = UCPlayer.FromID(_component.LastInstigator);
                _instigatorId = _component.LastInstigator;
                _lastDriver = UCPlayer.FromID(_component.LastDriver);
            }
            else
            {
                _instigator = _lastDriver = UCPlayer.FromID(_component.LastDriver);
                _instigatorId = _component.LastDriver;
            }

            _lastDriverId = _component.LastDriver;
        }
        else if (vehicle.passengers.Length > 0 && vehicle.passengers[0].player != null && vehicle.passengers[0].player.player != null)
        {
            _lastDriver = _instigator = UCPlayer.FromPlayer(vehicle.passengers[0].player.player);
            _lastDriverId = _lastDriver is null ? 0 : _lastDriver.Steam64;
        }
        if (VehicleSpawner.Loaded && VehicleSpawner.HasLinkedSpawn(_vehicle.instanceID, out _vehicleSpawn))
            _vehicleSpawnComponent = _vehicleSpawn.Component;
    }
}
