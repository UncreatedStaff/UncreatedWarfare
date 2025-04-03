using System;
using Uncreated.Warfare.Events;
using Uncreated.Warfare.Events.Models;
using Uncreated.Warfare.Events.Models.Squads;
using Uncreated.Warfare.Events.Models.Zones;
using Uncreated.Warfare.Kits.Requests;
using Uncreated.Warfare.Layouts.Teams;
using Uncreated.Warfare.Zones;

namespace Uncreated.Warfare.Kits.Tweaks;

internal sealed class KitGiveDefaultOnLeaveSquadKit : IAsyncEventListener<SquadMemberLeft>, IAsyncEventListener<PlayerEnteredZone>
{
    private readonly KitRequestService _kitRequestService;
    private readonly ZoneStore _zoneStore;

    public KitGiveDefaultOnLeaveSquadKit(KitRequestService kitRequestService, ZoneStore zoneStore)
    {
        _kitRequestService = kitRequestService;
        _zoneStore = zoneStore;
    }

    public async UniTask HandleEventAsync(SquadMemberLeft e, IServiceProvider serviceProvider, CancellationToken token = default)
    {
        if (e.Player.Team == Team.NoTeam)
            return;
        
        KitPlayerComponent kitPlayerComponent = e.Player.Component<KitPlayerComponent>();
        if (!kitPlayerComponent.HasKit || kitPlayerComponent.ActiveClass == Class.Unarmed)
            return;

        bool isLowAmmo = false;
        if (_zoneStore.IsInMainBase(e.Player) || (isLowAmmo = _zoneStore.IsInWarRoom(e.Player)))
        {
            await _kitRequestService.GiveAvailableFreeKitAsync(e.Player, silent: false, isLowAmmo: isLowAmmo, token: token);
        }
        else
        {
            e.Player.Save.NeedsNewKitOnSpawn = true;
        }
    }

    [EventListener(RequiresMainThread = true)]
    public async UniTask HandleEventAsync(PlayerEnteredZone e, IServiceProvider serviceProvider, CancellationToken token = default)
    {
        if (e.Zone.Type is not ZoneType.MainBase and not ZoneType.WarRoom || !string.Equals(e.Player.Team.Faction.FactionId, e.Zone.Faction, StringComparison.Ordinal))
            return;

        if (e.Player.Save.NeedsNewKitOnSpawn)
        {
            e.Player.Save.NeedsNewKitOnSpawn = false;
            await _kitRequestService.GiveAvailableFreeKitAsync(e.Player, silent: false, isLowAmmo: _zoneStore.IsInWarRoom(e.Player), token).ConfigureAwait(false);
        }
    }
}