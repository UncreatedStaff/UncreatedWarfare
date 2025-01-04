using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Uncreated.Framework.UI;
using Uncreated.Warfare.Buildables;
using Uncreated.Warfare.Configuration;
using Uncreated.Warfare.Database.Abstractions;
using Uncreated.Warfare.Events;
using Uncreated.Warfare.Events.Components;
using Uncreated.Warfare.Events.Models;
using Uncreated.Warfare.Events.Models.Barricades;
using Uncreated.Warfare.Events.Models.Items;
using Uncreated.Warfare.Events.Models.Structures;
using Uncreated.Warfare.Interaction;
using Uncreated.Warfare.Kits.Items;
using Uncreated.Warfare.Models;
using Uncreated.Warfare.Models.Assets;
using Uncreated.Warfare.Models.Kits;
using Uncreated.Warfare.Players.Permissions;
using Uncreated.Warfare.Teams;
using Uncreated.Warfare.Translations;
using Uncreated.Warfare.Translations.Collections;
using Uncreated.Warfare.Util;
using Uncreated.Warfare.Zones;

namespace Uncreated.Warfare.Kits.Whitelists;
public class WhitelistService :
    IAsyncEventListener<SalvageBarricadeRequested>,
    IAsyncEventListener<SalvageStructureRequested>,
    IAsyncEventListener<PlaceBarricadeRequested>,
    IAsyncEventListener<PlaceStructureRequested>,
    IAsyncEventListener<ChangeSignTextRequested>,
    IAsyncEventListener<ItemPickupRequested>,
    IDisposable
{
    public static readonly PermissionLeaf PermissionChangeSignText = new PermissionLeaf("warfare::actions.barricades.edit_sign");
    public static readonly PermissionLeaf PermissionPickUpAnyItem = new PermissionLeaf("warfare::actions.items.pickup_any");
    public static readonly PermissionLeaf PermissionDestroyBuildable = new PermissionLeaf("warfare::actions.buildables.destroy_any");
    public static readonly PermissionLeaf PermissionPlaceBuildable = new PermissionLeaf("warfare::actions.buildables.place_any");

    private readonly IWhitelistDbContext _dbContext;
    private readonly ZoneStore _zoneStore;
    private readonly ChatService _chatService;
    private readonly WarfareModule _module;
    private readonly BuildableSaver _buildableSaver;
    private readonly SemaphoreSlim _semaphore;
    private readonly WhitelistTranslations _translations;
    private readonly AssetRedirectService _assetRedirectService;
    private readonly IFactionDataStore _factionDataStore;

    public WhitelistService(IWhitelistDbContext dbContext, ZoneStore zoneStore, ChatService chatService,
        WarfareModule module, BuildableSaver buildableSaver, TranslationInjection<WhitelistTranslations> translations,
        AssetRedirectService assetRedirectService, IFactionDataStore factionDataStore)
    {
        _dbContext = dbContext;
        _dbContext.ChangeTracker.AutoDetectChangesEnabled = false;

        _zoneStore = zoneStore;
        _chatService = chatService;
        _module = module;
        _buildableSaver = buildableSaver;
        _assetRedirectService = assetRedirectService;
        _factionDataStore = factionDataStore;
        _translations = translations.Value;

        _semaphore = new SemaphoreSlim(1, 1);
    }

    /// <summary>
    /// Add an item's whitelist if it doesn't already exist, or update it's amount if it does.
    /// </summary>
    /// <returns><see langword="true"/> if the whitelist was added or it's amount was updated, otherwise <see langword="false"/>.</returns>
    public async Task<bool> WhitelistItem(IAssetLink<ItemAsset> item, int amount = -1, CancellationToken token = default)
    {
        await _semaphore.WaitAsync(token).ConfigureAwait(false);
        try
        {
            if (amount <= 0)
                amount = -1;

            string guidLookup = item.Guid.ToString("N", CultureInfo.InvariantCulture);
            string idLookup = item.Id.ToString();

            ItemWhitelist? whitelist = await _dbContext.Whitelists.FirstOrDefaultAsync(
                whitelist => Convert.ToString(whitelist.Item) == guidLookup || idLookup != "0" && Convert.ToString(whitelist.Item) == idLookup,
                token
            ).ConfigureAwait(false);

            if (whitelist == null)
            {
                _dbContext.Whitelists.Add(new ItemWhitelist
                {
                    Item = new UnturnedAssetReference(item),
                    Amount = amount
                });
            }
            else if (whitelist.Amount != amount)
            {
                whitelist.Amount = amount;
                _dbContext.Update(whitelist);
            }
            else if (whitelist.Item.Guid == Guid.Empty && item.Guid != Guid.Empty)
            {
                whitelist.Item = new UnturnedAssetReference(item);
                _dbContext.Update(whitelist);
            }
            else return false;

            await _dbContext.SaveChangesAsync(token).ConfigureAwait(false);
        }
        finally
        {
            _semaphore.Release();
        }

        return true;
    }

    /// <summary>
    /// Remove an item's whitelist if it exists.
    /// </summary>
    /// <returns><see langword="true"/> if the whitelist was removed, otherwise <see langword="false"/>.</returns>
    public async Task<bool> RemoveWhitelistedItem(IAssetLink<ItemAsset> item, CancellationToken token = default)
    {
        await _semaphore.WaitAsync(token).ConfigureAwait(false);
        try
        {
            string guidLookup = item.Guid.ToString("N", CultureInfo.InvariantCulture);
            string idLookup = item.Id.ToString();

            ItemWhitelist? whitelist = await _dbContext.Whitelists.FirstOrDefaultAsync(
                whitelist => Convert.ToString(whitelist.Item) == guidLookup || idLookup != "0" && Convert.ToString(whitelist.Item) == idLookup,
                token
            ).ConfigureAwait(false);

            if (whitelist == null)
            {
                return false;
            }

            _dbContext.Remove(whitelist);
            await _dbContext.SaveChangesAsync(token).ConfigureAwait(false);
        }
        finally
        {
            _semaphore.Release();
        }

        return true;
    }

    /// <summary>
    /// Get the amount value on whitelists. Takes <see cref="IWhitelistExceptionProvider"/>'s into account.
    /// </summary>
    /// <returns>0 if the item isn't whitelisted, -1 if it's infinite, otherwise the amount.</returns>
    public async Task<int> GetWhitelistedAmount(IAssetContainer assetContainer, CancellationToken token = default)
    {
        ItemWhitelist? whitelist = await GetWhitelistAsync(assetContainer, token).ConfigureAwait(false);
        if (whitelist != null)
        {
            return whitelist.Amount <= 0 ? -1 : whitelist.Amount;
        }

        foreach (IWhitelistExceptionProvider provider in _module.ScopedProvider.Resolve<IEnumerable<IWhitelistExceptionProvider>>())
        {
            if (!GameThread.IsCurrent)
            {
                await UniTask.SwitchToMainThread(token);
            }

            int amt = await provider.GetWhitelistAmount(assetContainer).ConfigureAwait(false);
            if (amt != 0)
                return amt < 0 ? -1 : amt;
        }

        return 0;
    }

    /// <summary>
    /// Get the amount value on whitelists. Takes <see cref="IWhitelistExceptionProvider"/>'s into account.
    /// </summary>
    /// <returns>0 if the item isn't whitelisted, -1 if it's infinite, otherwise the amount.</returns>
    public Task<int> GetWhitelistedAmount(ItemAsset asset, CancellationToken token = default)
    {
        return GetWhitelistedAmount(AssetLink.Create(asset), token);
    }

    /// <summary>
    /// Retreive the whitelist model for the given <see cref="IAssetContainer"/> or <see cref="IAssetLink{TAsset}"/>.
    /// </summary>
    public async Task<ItemWhitelist?> GetWhitelistAsync(IAssetContainer assetContainer, CancellationToken token = default)
    {
        await _semaphore.WaitAsync(token).ConfigureAwait(false);
        try
        {
            string guidLookup = assetContainer.Guid.ToString("N", CultureInfo.InvariantCulture);
            string idLookup = assetContainer.Id.ToString();

            return await _dbContext.Whitelists.FirstOrDefaultAsync(
                whitelist => Convert.ToString(whitelist.Item) == guidLookup || idLookup != "0" && Convert.ToString(whitelist.Item) == idLookup,
                token
            ).ConfigureAwait(false);
        }
        finally
        {
            _semaphore.Release();
        }
    }

    /// <summary>
    /// Retreive the whitelist model for the given <see cref="ItemAsset"/>.
    /// </summary>
    public async Task<ItemWhitelist?> GetWhitelistAsync(ItemAsset asset, CancellationToken token = default)
    {
        await _semaphore.WaitAsync(token).ConfigureAwait(false);
        try
        {
            string guidLookup = asset.GUID.ToString("N", CultureInfo.InvariantCulture);
            string idLookup = asset.id.ToString();

            return await _dbContext.Whitelists.FirstOrDefaultAsync(
                whitelist => Convert.ToString(whitelist.Item) == guidLookup || idLookup != "0" && Convert.ToString(whitelist.Item) == idLookup,
                token
            ).ConfigureAwait(false);
        }
        finally
        {
            _semaphore.Release();
        }
    }
    
    /// <summary>
    /// Retreive the whitelist model for the given item <see cref="Guid"/>.
    /// </summary>
    public async Task<ItemWhitelist?> GetWhitelistAsync(Guid guid, CancellationToken token = default)
    {
        await UniTask.SwitchToMainThread(token);
        Asset? asset = Assets.find(guid);
        
        await _semaphore.WaitAsync(token).ConfigureAwait(false);
        try
        {
            string guidLookup = guid.ToString("N", CultureInfo.InvariantCulture);
            string idLookup = asset?.id.ToString(CultureInfo.InvariantCulture) ?? "0";

            return await _dbContext.Whitelists.FirstOrDefaultAsync(
                whitelist => Convert.ToString(whitelist.Item) == guidLookup || idLookup != "0" && Convert.ToString(whitelist.Item) == idLookup,
                token
            ).ConfigureAwait(false);
        }
        finally
        {
            _semaphore.Release();
        }
    }

    [EventListener(RequiresMainThread = false)]
    async UniTask IAsyncEventListener<ChangeSignTextRequested>.HandleEventAsync(ChangeSignTextRequested e, IServiceProvider serviceProvider, CancellationToken token)
    {
        UserPermissionStore permissions = serviceProvider.GetRequiredService<UserPermissionStore>();
        if (await permissions.HasPermissionAsync(e.Player, PermissionChangeSignText, token))
        {
            return;
        }

        await UniTask.SwitchToMainThread(token);
        Kit? kit = e.Player.Component<KitPlayerComponent>().CachedKit;

        ItemAsset asset = e.Buildable.Asset;
        if (kit != null && kit.ContainsItem(asset.GUID, e.Player.Team, _assetRedirectService, _factionDataStore))
            return;

        ItemWhitelist? whitelist = await GetWhitelistAsync(asset, token).ConfigureAwait(false);

        if (whitelist is { Amount: > 0 })
        {
            return;
        }

        CommonTranslations translations = serviceProvider.GetRequiredService<TranslationInjection<CommonTranslations>>().Value;
        _chatService.Send(e.Player, translations.NoPermissionsSpecific, PermissionChangeSignText);
        e.Cancel();
    }

    [EventListener(RequiresMainThread = true)]
    UniTask IAsyncEventListener<SalvageBarricadeRequested>.HandleEventAsync(SalvageBarricadeRequested e, IServiceProvider serviceProvider, CancellationToken token)
    {
        return HandleSalvageRequest(e, serviceProvider, token).AsUniTask();
    }

    [EventListener(RequiresMainThread = true)]
    UniTask IAsyncEventListener<SalvageStructureRequested>.HandleEventAsync(SalvageStructureRequested e, IServiceProvider serviceProvider, CancellationToken token)
    {
        return HandleSalvageRequest(e, serviceProvider, token).AsUniTask();
    }

    [EventListener(RequiresMainThread = true)]
    async UniTask IAsyncEventListener<PlaceBarricadeRequested>.HandleEventAsync(PlaceBarricadeRequested e, IServiceProvider serviceProvider, CancellationToken token)
    {
        if (e.OriginalPlacer == null)
            return;

        UserPermissionStore permissions = serviceProvider.GetRequiredService<UserPermissionStore>();
        if (await permissions.HasPermissionAsync(e.OriginalPlacer, PermissionPlaceBuildable, token))
        {
            return;
        }

        await UniTask.SwitchToMainThread(token);

        if (_zoneStore.IsInMainBase(e.OriginalPlacer))
        {
            _chatService.Send(e.OriginalPlacer, _translations.WhitelistProhibitedPlace, e.Asset);
            e.Cancel();
            return;
        }

        IAssetLink<ItemAsset> assetContainer = AssetLink.Create(e.Asset);

        int whitelistAmount = await GetWhitelistedAmount(assetContainer, token);
        await UniTask.SwitchToMainThread(token);

        if (whitelistAmount == -1)
            return;

        Kit? equippedKit = e.OriginalPlacer.Component<KitPlayerComponent>().CachedKit;

        if (equippedKit == null && whitelistAmount == 0)
        {
            _chatService.Send(e.OriginalPlacer, _translations.WhitelistProhibitedPlace, e.Asset);
            e.Cancel();
            return;
        }

        int maximumPlacedBarricades = equippedKit == null ? whitelistAmount : Math.Max(equippedKit.CountItems(assetContainer), whitelistAmount);

        if (maximumPlacedBarricades == 0)
        {
            _chatService.Send(e.OriginalPlacer, _translations.WhitelistProhibitedPlace, e.Asset);
            e.Cancel();
            return;
        }

        // count barricades of same type placed by placing player
        int placedBarricades = BarricadeUtility.CountBarricadesWhere(barricade => barricade.asset.GUID == e.Asset.GUID && barricade.GetServersideData().owner == e.OriginalPlacer.Steam64.m_SteamID);

        // technically this is a vulnerability since multiple 'place barricade requested' events could run at once,
        // but it's impossible in vanilla to place barricades that quickly anyway so not really worth fixing
        if (placedBarricades < maximumPlacedBarricades)
        {
            return;
        }

        int amountNeededToDestroy = placedBarricades - maximumPlacedBarricades + 1;
        foreach (BarricadeInfo info in BarricadeUtility.EnumerateBarricades()
                     .Where(barricade => barricade.Drop.asset.GUID == e.Asset.GUID && barricade.Drop.GetServersideData().owner == e.OriginalPlacer.Steam64.m_SteamID)
                     .OrderBy(barricade => barricade.Drop.model.TryGetComponent(out BuildableContainer comp) ? comp.CreateTime.Ticks : 0)
                     .ToList())
        {

            if (await _buildableSaver.IsBarricadeSavedAsync(info.Drop.instanceID, token))
            {
                continue;
            }

            --amountNeededToDestroy;
            DestroyerComponent.AddOrUpdate(info.Drop.model.gameObject, 0ul, false, EDamageOrigin.VehicleDecay);
            BarricadeManager.destroyBarricade(info.Drop, info.Coord.x, info.Coord.y, info.Plant);

            if (amountNeededToDestroy == 0)
            {
                break;
            }
        }

        if (amountNeededToDestroy > 0)
        {
            _chatService.Send(e.OriginalPlacer, _translations.WhitelistProhibitedPlaceAmt, amountNeededToDestroy, e.Asset);
            e.Cancel();
        }
    }

    [EventListener(RequiresMainThread = true)]
    async UniTask IAsyncEventListener<PlaceStructureRequested>.HandleEventAsync(PlaceStructureRequested e, IServiceProvider serviceProvider, CancellationToken token)
    {
        if (e.OriginalPlacer == null)
            return;

        UserPermissionStore permissions = serviceProvider.GetRequiredService<UserPermissionStore>();
        if (await permissions.HasPermissionAsync(e.OriginalPlacer, PermissionPlaceBuildable, token))
        {
            return;
        }

        await UniTask.SwitchToMainThread(token);

        if (_zoneStore.IsInMainBase(e.OriginalPlacer))
        {
            _chatService.Send(e.OriginalPlacer, _translations.WhitelistProhibitedPlace, e.Asset);
            e.Cancel();
            return;
        }

        IAssetLink<ItemAsset> assetContainer = AssetLink.Create(e.Asset);

        int whitelistAmount = await GetWhitelistedAmount(assetContainer, token);
        await UniTask.SwitchToMainThread(token);

        if (whitelistAmount == -1)
            return;

        Kit? equippedKit = e.OriginalPlacer.Component<KitPlayerComponent>().CachedKit;

        if (equippedKit == null && whitelistAmount == 0)
        {
            e.Cancel();
            return;
        }

        int maximumPlacedStructures = equippedKit == null ? whitelistAmount : Math.Max(equippedKit.CountItems(assetContainer), whitelistAmount);

        if (maximumPlacedStructures == 0)
        {
            _chatService.Send(e.OriginalPlacer, _translations.WhitelistProhibitedPlace, e.Asset);
            e.Cancel();
            return;
        }

        // count structures of same type placed by placing player
        int placedStructures = StructureUtility.CountStructuresWhere(structure => structure.asset.GUID == e.Asset.GUID && structure.GetServersideData().owner == e.OriginalPlacer.Steam64.m_SteamID);

        // technically this is a vulnerability since multiple 'place structure requested' events could run at once,
        // but it's impossible in vanilla to place structures that quickly anyway so not really worth fixing
        if (placedStructures < maximumPlacedStructures)
        {
            return;
        }

        int amountNeededToDestroy = placedStructures - maximumPlacedStructures + 1;
        foreach (StructureInfo info in StructureUtility.EnumerateStructures()
                     .Where(structure => structure.Drop.asset.GUID == e.Asset.GUID && structure.Drop.GetServersideData().owner == e.OriginalPlacer.Steam64.m_SteamID)
                     .OrderBy(structure => structure.Drop.model.TryGetComponent(out BuildableContainer comp) ? comp.CreateTime.Ticks : 0)
                     .ToList())
        {

            if (await _buildableSaver.IsStructureSavedAsync(info.Drop.instanceID, token))
            {
                continue;
            }

            --amountNeededToDestroy;
            DestroyerComponent.AddOrUpdate(info.Drop.model.gameObject, 0ul, false, EDamageOrigin.VehicleDecay);
            StructureManager.destroyStructure(info.Drop, info.Coord.x, info.Coord.y, Vector3.zero);

            if (amountNeededToDestroy == 0)
            {
                break;
            }
        }

        if (amountNeededToDestroy > 0)
        {
            _chatService.Send(e.OriginalPlacer, _translations.WhitelistProhibitedPlaceAmt, amountNeededToDestroy, e.Asset);
            e.Cancel();
        }
    }

    [EventListener(RequiresMainThread = true)]
    async UniTask IAsyncEventListener<ItemPickupRequested>.HandleEventAsync(ItemPickupRequested e, IServiceProvider serviceProvider, CancellationToken token)
    {
        UserPermissionStore permissions = serviceProvider.GetRequiredService<UserPermissionStore>();
        if (await permissions.HasPermissionAsync(e.Player, PermissionPickUpAnyItem, token))
        {
            return;
        }

        await UniTask.SwitchToMainThread(token);

        IAssetLink<ItemAsset> assetContainer = AssetLink.Create(e.Asset);

        int whitelistAmount = await GetWhitelistedAmount(assetContainer, token);
        await UniTask.SwitchToMainThread(token);

        if (whitelistAmount == -1)
            return;

        // don't allow putting kit items or non-whitelisted items in storage
        if (e.DestinationPage == Page.Storage && whitelistAmount == 0)
        {
            e.Cancel();
            return;
        }

        Kit? equippedKit = e.Player.Component<KitPlayerComponent>().CachedKit;

        if (equippedKit == null && whitelistAmount == 0)
        {
            e.Cancel();
            _chatService.Send(e.Player, _translations.WhitelistNoKit);
            return;
        }

        int maximumItems = equippedKit == null ? whitelistAmount : Math.Max(equippedKit.CountItems(assetContainer), whitelistAmount);

        int itemCount = ItemUtility.CountItems(e.PlayerObject, assetContainer, maximumItems);

        if (itemCount >= maximumItems)
        {
            if (whitelistAmount == 0)
            {
                e.Cancel();
                _chatService.Send(e.Player, _translations.WhitelistProhibitedPlace, e.Asset);
            }
            else
            {
                e.Cancel();
                _chatService.Send(e.Player, _translations.WhitelistProhibitedPlaceAmt, maximumItems, e.Asset);
            }
        }
    }

    private async Task HandleSalvageRequest(SalvageRequested e, IServiceProvider serviceProvider, CancellationToken token)
    {
        UserPermissionStore permissions = serviceProvider.GetRequiredService<UserPermissionStore>();
        if (await permissions.HasPermissionAsync(e.Player, PermissionDestroyBuildable, token))
        {
            return;
        }

        await UniTask.SwitchToMainThread(token);

        Kit? kit = e.Player.Component<KitPlayerComponent>().CachedKit;

        ItemAsset asset = e.Buildable.Asset;
        if (kit != null && kit.ContainsItem(asset.GUID, e.Player.Team, _assetRedirectService, _factionDataStore))
            return;

        ItemWhitelist? whitelist = await GetWhitelistAsync(asset, token).ConfigureAwait(false);

        if (whitelist is not { Amount: not 0 })
        {
            _chatService.Send(e.Player, _translations.WhitelistProhibitedSalvage, e.Buildable.Asset);
            e.Cancel();
        }
    }

    void IDisposable.Dispose()
    {
        _semaphore.Dispose();
    }
}