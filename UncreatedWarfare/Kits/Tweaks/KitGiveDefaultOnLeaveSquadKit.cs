using System;
using Uncreated.Warfare.Events;
using Uncreated.Warfare.Events.Models;
using Uncreated.Warfare.Events.Models.Squads;
using Uncreated.Warfare.Events.Models.Zones;
using Uncreated.Warfare.Kits.Requests;
using Uncreated.Warfare.Layouts.Teams;
using Uncreated.Warfare.Zones;

namespace Uncreated.Warfare.Kits.Tweaks;

internal sealed class KitGiveDefaultOnLeaveSquadKit : IEventListener<SquadMemberLeft>, IEventListener<PlayerEnteredZone>
{
    private readonly KitRequestService _kitRequestService;
    private readonly ZoneStore _zoneStore;

    public KitGiveDefaultOnLeaveSquadKit(KitRequestService kitRequestService, ZoneStore zoneStore)
    {
        _kitRequestService = kitRequestService;
        _zoneStore = zoneStore;
    }

    public void HandleEvent(SquadMemberLeft e, IServiceProvider serviceProvider)
    {
        if (e.Player.Team == Team.NoTeam)
            return;
        
        KitPlayerComponent kitPlayerComponent = e.Player.Component<KitPlayerComponent>();
        if (!kitPlayerComponent.HasKit || kitPlayerComponent.ActiveClass == Class.Unarmed)
            return;

        bool isLowAmmo = false;
        if (_zoneStore.IsInMainBase(e.Player) || (isLowAmmo = _zoneStore.IsInWarRoom(e.Player)))
        {
            _ = Task.Run(async () =>
            {
                try
                {
                    await _kitRequestService.GiveAvailableFreeKitAsync(e.Player, silent: false, isLowAmmo: isLowAmmo, e.Player.DisconnectToken);
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex);
                }
            });
        }
        else
        {
            e.Player.Save.NeedsNewKitOnSpawn = true;
        }
    }

    [EventListener(RequiresMainThread = true)]
    public void HandleEvent(PlayerEnteredZone e, IServiceProvider serviceProvider)
    {
        if (e.Zone.Type is not ZoneType.MainBase and not ZoneType.WarRoom || !string.Equals(e.Player.Team.Faction.FactionId, e.Zone.Faction, StringComparison.Ordinal))
            return;

        if (!e.Player.Save.NeedsNewKitOnSpawn)
            return;

        e.Player.Save.NeedsNewKitOnSpawn = false;

        _ = Task.Run(async () =>
        {
            try
            {
                await _kitRequestService.GiveAvailableFreeKitAsync(e.Player, silent: false, isLowAmmo: _zoneStore.IsInWarRoom(e.Player), e.Player.DisconnectToken).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
            }
        });
    }
}