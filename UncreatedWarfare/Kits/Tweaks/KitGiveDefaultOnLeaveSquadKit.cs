using System;
using Uncreated.Warfare.Events.Models;
using Uncreated.Warfare.Events.Models.Squads;
using Uncreated.Warfare.Kits.Requests;
using Uncreated.Warfare.Layouts.Teams;
using Uncreated.Warfare.Zones;

namespace Uncreated.Warfare.Kits.Tweaks;

internal sealed class KitGiveDefaultOnLeaveSquadKit : IAsyncEventListener<SquadMemberLeft>
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

        await _kitRequestService.GiveAvailableFreeKitAsync(e.Player, silent: true, isLowAmmo: !_zoneStore.IsInMainBase(e.Player) || _zoneStore.IsInWarRoom(e.Player), token: token);
    }
}