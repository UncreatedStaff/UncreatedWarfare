using System.Collections.Generic;
using Uncreated.Warfare.Components;
using Uncreated.Warfare.Players;
using Uncreated.Warfare.Players.Management;

namespace Uncreated.Warfare.Events.Models.Vehicles;
public class VehicleDestroyed
{
    private readonly InteractableVehicle _vehicle;
    private readonly VehicleComponent? _component;
    private readonly SpottedComponent? _spotter;
    private readonly WarfarePlayer? _lockedOwner;
    private readonly WarfarePlayer? _instigator;
    private readonly WarfarePlayer? _lastDriver;
    private readonly ulong _ownerId;
    private readonly ulong _instigatorId;
    private readonly ulong _lastDriverId;
    private readonly ulong _lockedTeam;
    public InteractableVehicle Vehicle => _vehicle;
    public VehicleComponent? Component => _component;
    public SpottedComponent? Spotter => _spotter;
    public WarfarePlayer? Owner => _lockedOwner;
    public WarfarePlayer? Instigator => _instigator;
    public WarfarePlayer? LastDriver => _lastDriver;
    public ulong OwnerId => _ownerId;
    public ulong InstigatorId => _instigatorId;
    public ulong LastDriverId => _lastDriverId;
    public ulong Team => _lockedTeam;
    public InteractableVehicle? ActiveVehicle { get; set; }
    public KeyValuePair<ulong, float>[] Assists { get; set; }
    public EDamageOrigin DamageOrigin { get; }
    public VehicleDestroyed(InteractableVehicle vehicle, IPlayerService playerService, SpottedComponent? spotted)
    {
        _spotter = spotted;
        _vehicle = vehicle;
        _lockedTeam = vehicle.lockedGroup.m_SteamID;
        _ownerId = vehicle.lockedOwner.m_SteamID;
        _lockedOwner = playerService.GetOnlinePlayerOrNull(_ownerId);
        _component = vehicle.GetComponent<VehicleComponent>();
        if (_component != null)
        {
            if (_component.LastInstigator != 0)
            {
                _instigator = playerService.GetOnlinePlayerOrNull(_component.LastInstigator);
                _instigatorId = _component.LastInstigator;
                _lastDriver = playerService.GetOnlinePlayerOrNull(_component.LastDriver);
            }
            else
            {
                _instigator = _lastDriver = playerService.GetOnlinePlayerOrNull(_component.LastDriver);
                _instigatorId = _component.LastDriver;
            }

            _lastDriverId = _component.LastDriver;
            DamageOrigin = _component.LastDamageOrigin;
            ActiveVehicle = _component.LastDamagedFromVehicle;
        }
        else if (vehicle.passengers.Length > 0 && vehicle.passengers[0].player != null && vehicle.passengers[0].player.player != null)
        {
            _lastDriver = _instigator = playerService.GetOnlinePlayerOrNull(vehicle.passengers[0].player.player);
            _lastDriverId = _lastDriver is null ? 0 : _lastDriver.Steam64.m_SteamID;
        }
    }
}
