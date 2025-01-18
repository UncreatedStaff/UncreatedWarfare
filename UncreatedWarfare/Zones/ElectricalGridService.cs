using DanielWillett.ReflectionTools;
using System;
using System.Collections.Generic;
using System.Linq;
using Uncreated.Warfare.Events.Models;
using Uncreated.Warfare.Events.Models.Flags;
using Uncreated.Warfare.Layouts;
using Uncreated.Warfare.Layouts.Flags;
using Uncreated.Warfare.Layouts.Teams;
using Uncreated.Warfare.Patches;
using Uncreated.Warfare.Services;
using Uncreated.Warfare.Util;

namespace Uncreated.Warfare.Zones;

/// <summary>
/// Grid objects apply power to objects related to flags in the rotation.
/// <para>
/// For example, lights, gates in a parking lot, vending machines, etc may be enabled when the zone is objective or in rotation.
/// </para>
/// </summary>
[Priority(-1000 /* after IFlagRotationService implementations */)]
public class ElectricalGridService : ILevelHostedService, IEventListener<FlagObjectiveChanged>, ILayoutHostedService
{
    private readonly ILogger<ElectricalGridService> _logger;
    private readonly WarfareModule _module;
    private readonly ZoneStore _zoneStore;

    // key: instance ID, value: number of zones needing the object to be enabled
    private readonly Dictionary<uint, int> _objectHandles = new Dictionary<uint, int>(256);

    private static readonly Action<InteractablePower>? RefreshIsConnectedToPower =
        Accessor.GenerateInstanceCaller<InteractablePower, Action<InteractablePower>>("RefreshIsConnectedToPower");

    public bool Enabled { get; internal set; }

    public ElectricalGridService(ILogger<ElectricalGridService> logger, WarfareModule module, ZoneStore zoneStore)
    {
        _logger = logger;
        _module = module;
        _zoneStore = zoneStore;
    }

    /// <inheritdoc />
    public UniTask LoadLevelAsync(CancellationToken token)
    {
        if (ElectricalGridCalculationPatches.Failed)
        {
            Enabled = false;
            return UniTask.CompletedTask;
        }

        if (!Level.info.configData.Has_Global_Electricity)
        {
            _logger.LogWarning("Level does not have global electricity enabled, electrical grid effects will not work!");
            Enabled = false;
        }
        else
        {
            Enabled = true;
        }

        return UniTask.CompletedTask;
    }

    internal bool IsInteractableEnabled(IFlagRotationService flagRotation, Interactable interactable)
    {
        if (flagRotation.GridBehaivor == ElectricalGridBehaivor.AllEnabled)
            return true;

        ObjectInfo obj = default;
        if (flagRotation.GridBehaivor != ElectricalGridBehaivor.Disabled)
        {
            // rotation
            if (interactable is not InteractableObject powerObject)
            {
                if (IsPointInRotation(flagRotation, interactable.transform.position))
                {
                    _logger.LogConditional($"Interactable non-object {interactable.name} is inside of a zone in rotation.");
                    return true;
                }
            }
            else
            {
                obj = LevelObjectUtility.FindObject(powerObject.transform);
                if (!obj.HasValue)
                    return false;

                if (IsRegisteredGridObject(flagRotation, obj.Object.instanceID))
                {
                    _logger.LogConditional("Object {0} is inside of a zone in rotation.", obj.Object.instanceID);
                    return true;
                }
            }
        }

        // main bases
        Layout? layout = _module.IsLayoutActive() ? _module.GetActiveLayout() : null;
        if (layout != null)
        {
            foreach (Team team in layout.TeamManager.AllTeams)
            {
                Zone? zone = _zoneStore.SearchZone(ZoneType.MainBase, team.Faction);
                if (zone == null)
                    continue;

                if (obj.HasValue)
                {
                    if (!zone.GridObjects.Contains(obj.Object.instanceID))
                        continue;

                    _logger.LogConditional("Object {0} is inside of a team zone in rotation.", obj.Object.instanceID);
                    return true;
                }

                Vector3 position = interactable.transform.position;
                if (!_zoneStore.ProximityZones!.Any(x => x.Zone.Name.Equals(zone.Name, StringComparison.Ordinal) && x.Proximity.TestPoint(in position)))
                    continue;

                _logger.LogConditional("Interactable {0} is inside of a team zone in rotation.", interactable.name);
                return true;
            }
        }

        if (obj.HasValue)
            _logger.LogConditional("Object {0} is not inside of a zone in rotation.", obj.Object.instanceID);
        else
            _logger.LogConditional("Interactable {0} is not inside of a zone in rotation.", interactable.name);
        
        return false;
    }

    private static bool IsRegisteredGridObject(IFlagRotationService flagRotation, uint instId)
    {
        IEnumerable<FlagObjective> rotation = flagRotation.GridBehaivor == ElectricalGridBehaivor.EnabledWhenObjective
            ? flagRotation.EnumerateObjectives()
            : flagRotation.ActiveFlags;

        foreach (FlagObjective zoneCluster in rotation)
        {
            if (zoneCluster.Region.Primary.Zone.GridObjects.Contains(instId))
            {
                return true;
            }
        }

        return false;
    }

    private static bool IsPointInRotation(IFlagRotationService flagRotation, Vector3 position)
    {
        IEnumerable<FlagObjective> rotation = flagRotation.GridBehaivor == ElectricalGridBehaivor.EnabledWhenObjective
            ? flagRotation.EnumerateObjectives()
            : flagRotation.ActiveFlags;

        foreach (FlagObjective zoneCluster in rotation)
        {
            if (zoneCluster.Region.TestPoint(position))
            {
                return true;
            }
        }

        return false;
    }

    void IEventListener<FlagObjectiveChanged>.HandleEvent(FlagObjectiveChanged e, IServiceProvider serviceProvider)
    {
        if (!Enabled || !_module.IsLayoutActive())
            return;

        IFlagRotationService? rotationService = _module.GetActiveLayout().ServiceProvider.ResolveOptional<IFlagRotationService>();

        if (rotationService is not { GridBehaivor: ElectricalGridBehaivor.EnabledWhenObjective })
        {
            return;
        }

        if (e.OldObjective != null)
        {
            SetPowerForAllGrid(e.OldObjective.Region.Primary.Zone, false, true);
        }
        if (e.NewObjective != null)
        {
            SetPowerForAllGrid(e.NewObjective.Region.Primary.Zone, true, true);
        }

        CheckPowerForAllBarricades();
    }

    private static void CheckPowerForAllBarricades()
    {
        if (RefreshIsConnectedToPower == null)
            return;

        foreach (BarricadeInfo barricade in BarricadeUtility.EnumerateBarricades())
        {
            if (barricade.Drop.interactable is InteractablePower power)
                RefreshIsConnectedToPower(power);
        }
    }

    private void SetPowerForAllGrid(Zone zone, bool state, bool addHandles)
    {
#if DEBUG
        _logger.LogConditional("Setting all objects in {0} to state {1}.", zone.Name, state);
        using IDisposable? scope = _logger.BeginScope(zone.Name);
#endif
        Vector3 c = zone.Center;
        foreach (uint gridObject in zone.GridObjects)
        {
            ObjectInfo obj = LevelObjectUtility.FindObject(gridObject, c);
            
            if (!obj.HasValue)
            {
                continue;
            }

            InteractableObject? intx = obj.Object.interactable;
            if (intx is null)
                continue;

            if (addHandles)
            {
                if (state)
                    AddHandle(obj.Object.instanceID);
                else
                    RemoveHandle(obj.Object.instanceID);
            }
            else if (GetHandleCount(obj.Object.instanceID) > 0)
            {
                _logger.LogConditional("Skipping disabling {0} - {1}, already enabled from another zone.", obj.Object.instanceID, obj.Object.asset);
                continue;
            }

            bool checkWire = intx.isWired;
            if (!checkWire)
                RefreshIsConnectedToPower?.Invoke(intx);

            if (intx.objectAsset is { interactability: EObjectInteractability.BINARY_STATE, interactabilityPower: not EObjectInteractabilityPower.NONE })
            {
                _logger.LogConditional("Setting state of {0} - {1} to {2}.", obj.Object.instanceID, obj.Object.asset, state);
                if (obj.Object.interactable is InteractableObjectBinaryState s && s.isUsed != state)
                {
                    ObjectManager.forceObjectBinaryState(obj.Object.transform, state);
                }
            }

            if (checkWire)
                RefreshIsConnectedToPower?.Invoke(intx);
        }
    }

    UniTask ILayoutHostedService.StartAsync(CancellationToken token)
    {
        _objectHandles.Clear();

        Layout layout = _module.GetActiveLayout();

        IFlagRotationService? rotationService = layout.ServiceProvider.ResolveOptional<IFlagRotationService>();

        foreach (Team team in layout.TeamManager.AllTeams)
        {
            Zone? zone = _zoneStore.SearchZone(ZoneType.MainBase, team.Faction);
            if (zone == null)
                continue;

            SetPowerForAllGrid(zone, true, true);
        }

        foreach (Zone zone in _zoneStore.Zones.Where(x => x.Type == ZoneType.Flag && x.IsPrimary))
        {
            bool isEnabled = false;
            if (rotationService != null)
            {
                isEnabled = rotationService.GridBehaivor switch
                {
                    ElectricalGridBehaivor.AllEnabled => true,
                    ElectricalGridBehaivor.EnabledWhenInRotation => rotationService.ActiveFlags.Any(x => x.Region.Primary.Zone.Equals(zone)),
                    ElectricalGridBehaivor.EnabledWhenObjective => rotationService.EnumerateObjectives().Any(x => x.Region.Primary.Zone.Equals(zone)),
                    _ => false
                };
            }

            SetPowerForAllGrid(zone, isEnabled, isEnabled);
        }

        return UniTask.CompletedTask;
    }

    private void AddHandle(uint obj)
    {
        if (_objectHandles.TryGetValue(obj, out int v))
            _objectHandles[obj] = v + 1;
        else
            _objectHandles.Add(obj, 1);
    }

    private void RemoveHandle(uint obj)
    {
        if (!_objectHandles.TryGetValue(obj, out int v))
            return;

        if (v <= 1)
            _objectHandles.Remove(obj);
        else
            _objectHandles[obj] = v - 1;
    }
    
    private int GetHandleCount(uint obj)
    {
        _objectHandles.TryGetValue(obj, out int value);
        return value;
    }

    UniTask ILayoutHostedService.StopAsync(CancellationToken token)
    {
        return UniTask.CompletedTask;
    }
}