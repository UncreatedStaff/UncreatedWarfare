using System.Linq;
using Uncreated.Warfare.Layouts;
using Uncreated.Warfare.Layouts.Teams;
using Uncreated.Warfare.Zones;

namespace Uncreated.Warfare.FreeTeamDeathmatch;

// TODO: this doesn't work for a lot of objects at once. gonna not use it for now
internal sealed class FtdmElectricalGridHandler : IElectricalGridHandler
{
    private readonly FtdmService _ftdmService;
    private readonly ZoneStore _zoneStore;
    private readonly ElectricalGridService _electricalGridService;
    private readonly Layout _layout;

    bool IElectricalGridHandler.IsEnabled => true;

    public FtdmElectricalGridHandler(
        FtdmService ftdmService,
        ZoneStore zoneStore,
        ElectricalGridService electricalGridService,
        Layout layout)
    {
        _ftdmService = ftdmService;
        _zoneStore = zoneStore;
        _electricalGridService = electricalGridService;
        _layout = layout;
    }

    public bool IsPowered(LevelObject @object)
    {
        ZoneProximity? playArea = _ftdmService.PlayArea;
        if (!playArea.HasValue)
        {
            return false;
        }

        return playArea.Value.Zone.GridObjects.Contains(@object.instanceID);
    }

    public bool IsPowered(InteractablePower otherInteractable)
    {
        ZoneProximity? playArea = _ftdmService.PlayArea;
        if (!playArea.HasValue)
        {
            return false;
        }

        return playArea.Value.Proximity.TestPoint(otherInteractable.transform.position);
    }

    void IElectricalGridHandler.Start()
    {
        foreach (Team team in _layout.TeamManager.AllTeams)
        {
            Zone? zone = _zoneStore.SearchZone(ZoneType.MainBase, team.Faction);
            if (zone == null)
                continue;

            _electricalGridService.SetPowerForZoneObjects(zone, true, true);
        }

        foreach (Zone zone in _zoneStore.Zones.Where(x => x.Type == ZoneType.Flag && x.IsPrimary))
        {
            _electricalGridService.SetPowerForZoneObjects(zone, false, false);
        }
    }

    void IElectricalGridHandler.Stop() { }
}
