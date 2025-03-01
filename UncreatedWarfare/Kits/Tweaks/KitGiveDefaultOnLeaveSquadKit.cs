using System;
using System.Collections.Generic;
using System.Linq;
using Uncreated.Warfare.Events.Models;
using Uncreated.Warfare.Events.Models.Items;
using Uncreated.Warfare.Events.Models.Squads;
using Uncreated.Warfare.Models.Kits;
using Uncreated.Warfare.Teams;
using Uncreated.Warfare.Translations.Languages;

namespace Uncreated.Warfare.Kits.Tweaks;

internal sealed class KitGiveDefaultOnLeaveSquadKit : IAsyncEventListener<SquadMemberLeft>
{
    private readonly KitBestowService _kitBestowService;
    private readonly IKitDataStore _kitDataStore;

    public KitGiveDefaultOnLeaveSquadKit(KitBestowService kitBestowService, IKitDataStore kitDataStore)
    {
        _kitBestowService = kitBestowService;
        _kitDataStore = kitDataStore;
    }
    public async UniTask HandleEventAsync(SquadMemberLeft e, IServiceProvider serviceProvider, CancellationToken token = default)
    {
        KitPlayerComponent kitPlayerComponent = e.Player.Component<KitPlayerComponent>();
        if (!kitPlayerComponent.HasKit || kitPlayerComponent.ActiveClass == Class.Unarmed)
            return;

        Kit? riflemanKit = await _kitDataStore.QueryKitAsync(
            KitInclude.Base, kits => kits.Where(
                x => x.Type == KitType.Public
                     && x.PremiumCost == 0
                     && !x.RequiresNitro
                     && !x.Disabled
                     && x.FactionId == e.Player.Team.Faction.PrimaryKey
                     && x.Id.EndsWith("rif1")), token); // convention for default rifleman kit ends with "rif1" e.g. usrif1token);
            
        if (riflemanKit == null)
            return;
        
        await UniTask.SwitchToMainThread(token);
        
        _kitBestowService.BestowKit(e.Player, new KitBestowData(riflemanKit)
        {
            Silent = true,
            IsLowAmmo = true
        });
    }
}