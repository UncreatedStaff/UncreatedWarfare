using Microsoft.Extensions.DependencyInjection;
using System;
using System.Linq;
using Uncreated.Warfare.Events;
using Uncreated.Warfare.Events.Models;
using Uncreated.Warfare.Events.Models.Barricades;
using Uncreated.Warfare.Events.Models.Structures;
using Uncreated.Warfare.Fobs;
using Uncreated.Warfare.Interaction;
using Uncreated.Warfare.Zones;

namespace Uncreated.Warfare.Tweaks;
internal class PlacementRestrictions : IEventListener<PlaceBarricadeRequested>, IEventListener<PlaceStructureRequested>, IEventListener<TriggerTrapRequested>
{
    private readonly ZoneStore _globalZoneStore;
    private readonly FobManager? _fobManager;
    private readonly ChatService _chatService;

    public PlacementRestrictions(ZoneStore globalZoneStore, IServiceProvider serviceProvider)
    {
        _globalZoneStore = globalZoneStore;
        _chatService = serviceProvider.GetRequiredService<ChatService>();
        _fobManager = serviceProvider.GetService<FobManager>(); // optional dependency
    }

    [EventListener(Priority = 1)]
    void IEventListener<PlaceBarricadeRequested>.HandleEvent(PlaceBarricadeRequested e, IServiceProvider serviceProvider)
    {
        if (e.OriginalPlacer == null || e.OriginalPlacer.OnDuty())
            return;

        // in any main or lobby
        if (_globalZoneStore.IsInMainBase(e.Position) || _globalZoneStore.IsInLobby(e.Position))
        {
            _chatService.Send(e.OriginalPlacer, T.WhitelistProhibitedPlace, e.Asset);
            e.Cancel();
            return;
        }

        // non-whitelisted barricade on vehicle
        if (e.IsOnVehicle && !UCWarfare.Config.ModerationSettings.AllowedBarricadesOnVehicles.ContainsAsset(e.Asset))
        {
            e.Cancel();
            _chatService.Send(e.OriginalPlacer, T.NoPlacementOnVehicle, e.Asset);
            return;
        }

        // trap in FOB or main (landmines are considered EBuild.SKPIKE)
        if (e.Asset.build is EBuild.SPIKE or EBuild.WIRE && !IsTrapPositionValid(e.Position))
        {
            e.Cancel();
            _chatService.Send(e.OriginalPlacer,T.ProhibitedPlacement, e.Asset);
            return;
        }

        /* todo add zone permissions
        if (!onDuty && !CheckZoneBuildablePermissions(player, point, asset))
        {
            shouldAllow = false;
            return;
        }
        */

        // enemy AMC zone
        if (e.OriginalPlacer.Team.Opponents.Any(x => _globalZoneStore.IsInAntiMainCamp(e.Position, x.Faction)))
        {
            e.Cancel();
            _chatService.Send(e.OriginalPlacer, T.WhitelistProhibitedPlace, e.Asset);
        }
    }

    [EventListener(Priority = 1)]
    void IEventListener<PlaceStructureRequested>.HandleEvent(PlaceStructureRequested e, IServiceProvider serviceProvider)
    {
        if (e.OriginalPlacer == null || e.OriginalPlacer.OnDuty())
            return;

        // in any main or lobby
        if (_globalZoneStore.IsInMainBase(e.Position) || _globalZoneStore.IsInLobby(e.Position))
        {
            _chatService.Send(e.OriginalPlacer, T.WhitelistProhibitedPlace, e.Asset);
            e.Cancel();
            return;
        }

        // enemy AMC zone
        if (e.OriginalPlacer.Team.Opponents.Any(x => _globalZoneStore.IsInAntiMainCamp(e.Position, x.Faction)))
        {
            e.Cancel();
            _chatService.Send(e.OriginalPlacer, T.WhitelistProhibitedPlace, e.Asset);
        }
    }

    [EventListener(Priority = 1)]
    void IEventListener<TriggerTrapRequested>.HandleEvent(TriggerTrapRequested e, IServiceProvider serviceProvider)
    {
        // cancel invalid traps from going off
        if (!IsTrapPositionValid(e.Barricade.GetServersideData().point))
        {
            e.Cancel();
        }
    }

    public bool IsTrapPositionValid(Vector3 point)
    {
        if (_globalZoneStore.IsInMainBase(point) || _globalZoneStore.IsInLobby(point) || _globalZoneStore.IsInAntiMainCamp(point))
        {
            return false;
        }

        if (_fobManager != null)
        {
            // todo non-static
            return !FobManager.IsPointInFOB(point, out _);
        }

        return true;
    }

    /* todo
    public ZoneFlags GetNoBuildingZoneFlags(ItemAsset asset)
    {
        if (asset is ItemBarricadeAsset barricade)
        {
            if (RallyManager.IsRally(barricade))
                return ZoneFlags.NoRallies | ZoneFlags.NoBuilding;

            if (barricade.build is EBuild.SPIKE or EBuild.WIRE)
                return ZoneFlags.NoTraps | ZoneFlags.NoBuilding;
        }

        if (Gamemode.Config.FOBRadios.Value.HasGuid(asset.GUID))
            return ZoneFlags.NoRadios | ZoneFlags.NoBuilding;

        if (Gamemode.Config.BarricadeFOBBunkerBase.MatchGuid(asset.GUID))
            return ZoneFlags.NoBunkers | ZoneFlags.NoBuilding;

        if (FOBManager.FindBuildable(asset) is not null)
            return ZoneFlags.NoFOBBuilding | ZoneFlags.NoBuilding;

        return ZoneFlags.NoBuilding;
    } */
}
