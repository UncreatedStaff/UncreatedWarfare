using System;
using System.Runtime.CompilerServices;
using Uncreated.Warfare.Events;
using Uncreated.Warfare.Events.Models;
using Uncreated.Warfare.Events.Models.Barricades;
using Uncreated.Warfare.Layouts.Teams;
using Uncreated.Warfare.Zones;

namespace Uncreated.Warfare.Tweaks;

internal sealed class LandmineExplosionRestrictions(ITeamManager<Team> teamManager, ZoneStore zoneStore)
    : IEventListener<TriggerTrapRequested>, IEventListener<PlaceBarricadeRequested>
{
    [EventListener(Priority = 1)]
    void IEventListener<TriggerTrapRequested>.HandleEvent(TriggerTrapRequested e, IServiceProvider serviceProvider)
    {
        if (e.Barricade.asset is not ItemTrapAsset { isExplosive: true })
            return;

        if (e.TriggeringPlayer != null && e.TriggeringPlayer.ComponentOrNull<VanishPlayerComponent>() is { IsActive: true })
        {
            e.Cancel();
            return;
        }

        Team placedTeam = teamManager.GetTeam(Unsafe.As<ulong, CSteamID>(ref e.Barricade.GetServersideData().group));

        if (e.TriggeringTeam != null && e.TriggeringTeam.IsFriendly(placedTeam))
        {
            // allow players to trigger their own landmines with throwables
            if (e.TriggeringPlayer == null || !e.TriggeringPlayer.Equals(e.ServersideData.owner) || e.TriggeringThrowable == null)
                e.Cancel();
        }
        else if (!CheckLandminePosition(e.ServersideData.point))
        {
            e.Cancel();
        }
    }

    [EventListener(Priority = 1)]
    void IEventListener<PlaceBarricadeRequested>.HandleEvent(PlaceBarricadeRequested e, IServiceProvider serviceProvider)
    {
        if (e.Barricade.asset is not ItemTrapAsset)
        {
            return;
        }

        if (!CheckLandminePosition(e.Position))
        {
            e.Cancel();
        }
    }

    private bool CheckLandminePosition(Vector3 position)
    {
        return !(zoneStore.IsInsideZone(position, ZoneType.MainBase, null) || zoneStore.IsInsideZone(position, ZoneType.AntiMainCampArea, null));
    }
}
