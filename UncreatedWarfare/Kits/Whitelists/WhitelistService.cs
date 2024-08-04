using Microsoft.EntityFrameworkCore;
using System;
using Uncreated.Warfare;
using Uncreated.Warfare.Database;
using Uncreated.Warfare.Database.Abstractions;
using Uncreated.Warfare.Events;
using Uncreated.Warfare.Events.Models.Barricades;
using Uncreated.Warfare.Events.Models.Structures;
using Uncreated.Warfare.Models;
using Uncreated.Warfare.Models.Kits;
using Uncreated.Warfare.Zones;

namespace Uncreated.Warfare.Kits.Whitelists;
public class WhitelistService :
    IAsyncEventListener<SalvageBarricadeRequested>,
    IAsyncEventListener<SalvageStructureRequested>,
    IAsyncEventListener<PlaceBarricadeRequested>,
    IAsyncEventListener<PlaceStructureRequested>
{
    private readonly IWhitelistDbContext _dbContext;
    private readonly KitManager _kitManager;
    private readonly ZoneStore _zoneStore;

    public WhitelistService(WarfareDbContext dbContext, KitManager kitManager, ZoneStore zoneStore)
    {
        _dbContext = dbContext;
        _kitManager = kitManager;
        _zoneStore = zoneStore;
    }

    UniTask IAsyncEventListener<SalvageBarricadeRequested>.HandleEventAsync(SalvageBarricadeRequested e, IServiceProvider serviceProvider, CancellationToken token)
    {
        return HandleSalvageRequest(e, token).AsUniTask();
    }

    UniTask IAsyncEventListener<SalvageStructureRequested>.HandleEventAsync(SalvageStructureRequested e, IServiceProvider serviceProvider, CancellationToken token)
    {
        return HandleSalvageRequest(e, token).AsUniTask();
    }

    async UniTask IAsyncEventListener<PlaceBarricadeRequested>.HandleEventAsync(PlaceBarricadeRequested e, IServiceProvider serviceProvider, CancellationToken token)
    {
        if (e.OriginalPlacer == null || e.OriginalPlacer.OnDuty())
            return;
        
        if (_zoneStore.IsInMainBase(e.OriginalPlacer))
        {
            e.OriginalPlacer.SendChat(T.WhitelistProhibitedPlace, asset);
            e.Cancel();
            return;
        }


    }

    UniTask IAsyncEventListener<PlaceStructureRequested>.HandleEventAsync(PlaceStructureRequested e, IServiceProvider serviceProvider, CancellationToken token)
    {
        throw new NotImplementedException();
    }

    private async Task HandleSalvageRequest(SalvageRequested e, CancellationToken token)
    {
        if (e.Player.OnDuty())
            return;

        Kit? kit = e.Player.Kits.CachedActiveKitInfo;

        ItemAsset asset = e.Buildable.Asset;
        if (kit != null && kit.ContainsItem(asset.GUID, e.Player.GetTeam(/* todo */)))
            return;

        string guidLookup = asset.GUID.ToString();
        string idLookup = asset.id.ToString();

        ItemWhitelist? whitelist = await _dbContext.Whitelists.FirstOrDefaultAsync(
            whitelist => whitelist.Item.ToString() == guidLookup || idLookup != "0" && whitelist.Item.ToString() == idLookup,
            token
        );

        if (whitelist is not { Amount: > 0 })
            e.Cancel();
    }
}