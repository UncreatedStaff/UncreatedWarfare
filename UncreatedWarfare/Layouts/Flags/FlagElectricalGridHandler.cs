using System;
using System.Linq;
using Uncreated.Warfare.Events.Models;
using Uncreated.Warfare.Events.Models.Flags;
using Uncreated.Warfare.Layouts.Teams;
using Uncreated.Warfare.Zones;

namespace Uncreated.Warfare.Layouts.Flags;

public class FlagElectricalGridHandler : IElectricalGridHandler, IEventListener<FlagObjectiveChanged>
{
    private readonly Layout _layout;
    private readonly ElectricalGridService _electricalGridService;
    private readonly ZoneStore _zoneStore;
    private readonly IFlagRotationService _flagRotationService;
    private readonly ILogger<FlagElectricalGridHandler> _logger;

    private bool _hasIsEnabled;

    public FlagElectricalGridHandler(
        Layout layout,
        ElectricalGridService electricalGridService,
        ZoneStore zoneStore,
        IFlagRotationService flagRotationService,
        ILogger<FlagElectricalGridHandler> logger)
    {
        _layout = layout;
        _electricalGridService = electricalGridService;
        _zoneStore = zoneStore;
        _flagRotationService = flagRotationService;
        _logger = logger;
    }

    public bool IsEnabled
    {
        get
        {
            if (_hasIsEnabled)
                return field;

            field = _flagRotationService.GridBehaivor is not (ElectricalGridBehaivor.Disabled or > ElectricalGridBehaivor.EnabledWhenInRotation);
            _hasIsEnabled = true;
            return true;
        }
    }

    /// <inheritdoc />
    public void Start()
    {
        foreach (Team team in _layout.TeamManager.AllTeams)
        {
            Zone? zone = _zoneStore.SearchZone(ZoneType.MainBase, team.Faction);
            if (zone == null)
                continue;

            _electricalGridService.SetPowerForZoneObjects(zone, true, true);
        }

        IFlagRotationService? rotationService = _layout.ServiceProvider.ResolveOptional<IFlagRotationService>();

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

            _electricalGridService.SetPowerForZoneObjects(zone, isEnabled, isEnabled);
        }

    }

    /// <inheritdoc />
    public void Stop()
    {

    }

    public bool IsPowered(LevelObject @object)
    {
        if (_flagRotationService.GridBehaivor == ElectricalGridBehaivor.AllEnabled)
            return true;

        if (_flagRotationService.GridBehaivor != ElectricalGridBehaivor.Disabled)
        {
            // rotation
            if (IsRegisteredGridObject(@object.instanceID))
            {
                _logger.LogConditional("Object {0} is inside of a zone in rotation.", @object.instanceID);
                return true;
            }
        }

        // main bases
        foreach (Team team in _layout.TeamManager.AllTeams)
        {
            Zone? zone = _zoneStore.SearchZone(ZoneType.MainBase, team.Faction);
            if (zone == null)
                continue;

            if (!zone.GridObjects.Contains(@object.instanceID))
                continue;

            _logger.LogConditional("Object {0} is inside of a team zone in rotation.", @object.instanceID);
            return true;
        }

        _logger.LogConditional("Object {0} is not inside of a zone in rotation.", @object.instanceID);
        return false;
    }

    public bool IsPowered(InteractablePower otherInteractable)
    {
        if (_flagRotationService.GridBehaivor == ElectricalGridBehaivor.AllEnabled)
            return true;

        Vector3 point = otherInteractable.transform.position;
        if (_flagRotationService.GridBehaivor != ElectricalGridBehaivor.Disabled)
        {
            // rotation
            if (IsPointInRotation(point))
            {
                _logger.LogConditional($"Interactable non-object {otherInteractable.name} is inside of a zone in rotation.");
                return true;
            }
        }

        // main bases
        foreach (Team team in _layout.TeamManager.AllTeams)
        {
            Zone? zone = _zoneStore.SearchZone(ZoneType.MainBase, team.Faction);
            if (zone == null)
                continue;

            Vector3 position = point;
            if (!_zoneStore.ProximityZones!.Any(x => x.Zone.Name.Equals(zone.Name, StringComparison.Ordinal) && x.Proximity.TestPoint(in position)))
                continue;

            _logger.LogConditional("Interactable {0} is inside of a team zone in rotation.", otherInteractable.name);
            return true;
        }

        _logger.LogConditional("Interactable {0} is not inside of a zone in rotation.", otherInteractable.name);
        return false;
    }

    void IEventListener<FlagObjectiveChanged>.HandleEvent(FlagObjectiveChanged e, IServiceProvider serviceProvider)
    {
        if (!_electricalGridService.Enabled)
            return;

        if (_flagRotationService is not { GridBehaivor: ElectricalGridBehaivor.EnabledWhenObjective })
        {
            return;
        }

        if (e.OldObjective != null)
        {
            _electricalGridService.SetPowerForZoneObjects(e.OldObjective.Region.Primary.Zone, false, true);
        }
        if (e.NewObjective != null)
        {
            _electricalGridService.SetPowerForZoneObjects(e.NewObjective.Region.Primary.Zone, true, true);
        }

        _electricalGridService.CheckPowerForAllBarricades();
    }

    private bool IsRegisteredGridObject(uint instId)
    {
        IEnumerable<FlagObjective> rotation = _flagRotationService.GridBehaivor == ElectricalGridBehaivor.EnabledWhenObjective
            ? _flagRotationService.EnumerateObjectives()
            : _flagRotationService.ActiveFlags;

        foreach (FlagObjective zoneCluster in rotation)
        {
            if (zoneCluster.Region.Primary.Zone.GridObjects.Contains(instId))
            {
                return true;
            }
        }

        return false;
    }

    private bool IsPointInRotation(Vector3 position)
    {
        IEnumerable<FlagObjective> rotation = _flagRotationService.GridBehaivor == ElectricalGridBehaivor.EnabledWhenObjective
            ? _flagRotationService.EnumerateObjectives()
            : _flagRotationService.ActiveFlags;

        foreach (FlagObjective zoneCluster in rotation)
        {
            if (zoneCluster.Region.TestPoint(position))
            {
                return true;
            }
        }

        return false;
    }
}