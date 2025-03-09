using System;
using System.Collections.Generic;
using Uncreated.Warfare.Kits.Items;
using Uncreated.Warfare.Layouts.Teams;
using Uncreated.Warfare.Models.Kits;
using Uncreated.Warfare.Players;
using Uncreated.Warfare.Players.ItemTracking;
using Uncreated.Warfare.Players.Skillsets;
using Uncreated.Warfare.Players.UI;
using Uncreated.Warfare.Teams;
using Uncreated.Warfare.Translations;
using Uncreated.Warfare.Util;
using Uncreated.Warfare.Util.Inventory;

namespace Uncreated.Warfare.Kits;

public class KitBestowService
{
    private readonly IItemDistributionService _itemDistributionService;
    private readonly TipService _tipService;
    private readonly TipTranslations _translations;

    public KitBestowService(IItemDistributionService itemDistributionService, TipService tipService, TranslationInjection<TipTranslations> translations)
    {
        _itemDistributionService = itemDistributionService;
        _tipService = tipService;
        _translations = translations.Value;
    }
    
    /// <summary>
    /// Remove any kit from the player (effectively clearing their inventory).
    /// </summary>
    public void BestowEmptyKit(WarfarePlayer player)
    {
        GameThread.AssertCurrent();

        if (!player.IsOnline)
            throw new ArgumentException("Player offline.", nameof(player));

        _itemDistributionService.ClearInventory(player);

        player.Save.KitId = 0;
        player.Save.Save();

        player.Component<SkillsetPlayerComponent>().EnsureSkillsets(Array.Empty<Skillset>());
        player.Component<KitPlayerComponent>().UpdateKit(null, false);
    }

    /// <summary>
    /// Actually apply a kit to a player.
    /// </summary>
    public void BestowKit(WarfarePlayer player, KitBestowData data)
    {
        GameThread.AssertCurrent();

        if (!player.IsOnline)
            throw new ArgumentException("Player offline.", nameof(player));

        Kit kit = data.Kit;

        // get not included exceptions out of the way
        IKitItem[] items = kit.Items;
        Skillset[] skillsets = kit.Skillsets;

        using (BestowKitGiveItemsState state = new BestowKitGiveItemsState(data, player))
        {
            _itemDistributionService.GiveItems(items, player, state);
        }

        player.Save.KitId = kit.Key;
        player.Save.Save();

        player.Component<SkillsetPlayerComponent>().EnsureSkillsets(skillsets);
        player.Component<KitPlayerComponent>().UpdateKit(kit, data.IsLowAmmo);

        if (data.IsLowAmmo)
        {
            _tipService.TryGiveTip(player, 300, _translations.KitGiveLowAmmo);
        }

        // equip primary or secondary
        if (player.UnturnedPlayer.inventory.getItemCount((byte)Page.Primary) > 0)
        {
            player.UnturnedPlayer.equipment.ServerEquip((byte)Page.Primary, 0, 0);
        }
        else if (player.UnturnedPlayer.inventory.getItemCount((byte)Page.Secondary) > 0)
        {
            player.UnturnedPlayer.equipment.ServerEquip((byte)Page.Secondary, 0, 0);
        }
    }

    /// <summary>
    /// Restock the player's current kit without removing any items.
    /// </summary>
    public void RestockKit(WarfarePlayer player, bool resupplyAmmoBags)
    {
        GameThread.AssertCurrent();

        if (!player.IsOnline)
            throw new ArgumentException("Player offline.", nameof(player));

        Kit? kit = player.Component<KitPlayerComponent>().CachedKit;
        if (kit == null)
            return;

        IKitItem[] items = kit.Items;

        using BestowKitGiveItemsState state = new BestowKitGiveItemsState(new KitBestowData(kit) { RestockOnly = true, ResupplyAmmoBags = resupplyAmmoBags}, player);
        _itemDistributionService.RestockItems(items, player, state);
    }

    private struct BestowKitGiveItemsState : IItemDistributionState, IDisposable
    {
        private static readonly ItemTransformation NullItemTransformation = new ItemTransformation(0, 0, byte.MaxValue, byte.MaxValue, byte.MaxValue, byte.MaxValue, null!);

        private IEnumerator<KitLayoutTransformation> _enumerator;

        private readonly BestowKitOptions _options;
        private readonly Kit _kit;
        private readonly IReadOnlyList<KitLayoutTransformation> _layoutTransformations;
        private readonly List<KeyValuePair<Guid, int>>? _itemCountsTable;

        private readonly ItemTrackingPlayerComponent _itemTracking;

        private ItemTransformation _pendingTransformation;

        public readonly Kit? Kit => _kit;
        public Team RequestingTeam { get; }

        public bool Silent { get; }
        public bool ResupplyAmmoBags { get; }

        public BestowKitGiveItemsState(in KitBestowData data, WarfarePlayer requester)
        {
            _kit = data.Kit;
            Silent = data.Silent;
            _options = data.IsLowAmmo ? BestowKitOptions.LowAmmo : 0;
            if (data.Silent)
                _options |= BestowKitOptions.Silent;
            if (data.RestockOnly)
                _options |= BestowKitOptions.IsRestock;
            ResupplyAmmoBags = data.ResupplyAmmoBags;
            _layoutTransformations = data.Layouts ?? Array.Empty<KitLayoutTransformation>();
            _itemCountsTable = (_options & BestowKitOptions.LowAmmo) != 0 ? new List<KeyValuePair<Guid, int>>(16) : null;

            RequestingTeam = requester.Team;

            // ReSharper disable once GenericEnumeratorNotDisposed
            _enumerator = _layoutTransformations.GetEnumerator();

            _itemTracking = requester.Component<ItemTrackingPlayerComponent>();
        }

        public readonly bool ShouldGrantItem(IClothingItem item, ref KitItemResolutionResult resolvedItem)
        {
            return true;
        }

        public bool ShouldGrantItem(IPageItem item, ref KitItemResolutionResult resolvedItem, ref byte x, ref byte y, ref Page page, ref byte rotation)
        {
            if (!ResupplyAmmoBags && item is IRedirectedItem {Item: RedirectType.AmmoBag})
                return false;
            
            if (_pendingTransformation.NewX != byte.MaxValue)
                _pendingTransformation = NullItemTransformation;

            // check for layout transformation
            while (_enumerator.MoveNext())
            {
                KitLayoutTransformation? t = _enumerator.Current;
                if (t == null || t.OldX != x || t.OldY != y || t.OldPage != page)
                {
                    continue;
                }

                _pendingTransformation = new ItemTransformation(t.OldPage, t.NewPage, t.OldX, t.OldY, t.NewX, t.NewY, null!);
                x = t.NewX;
                y = t.NewY;
                page = t.NewPage;
                rotation = t.NewRotation;
                break;
            }

            try
            {
                _enumerator.Reset();
            }
            catch
            {
                _enumerator.Dispose();
                _enumerator = _layoutTransformations.GetEnumerator();
            }

            if (_itemCountsTable != null)
            {
                UpdateItemCountsTable(in resolvedItem);
            }

            if (page is not Page.Primary and not Page.Secondary && (_options & BestowKitOptions.LowAmmo) != 0)
            {
                ApplyLowAmmoChanges(ref resolvedItem);
            }

            return true;
        }

        /// <inheritdoc />
        public void OnAddingPreviousItem(in KitItemResolutionResult result, byte x, byte y, byte rot, Page page, Item item)
        {
            if (_pendingTransformation.NewX == byte.MaxValue)
                return;

            _itemTracking.ItemTransformations.Add(new ItemTransformation(_pendingTransformation.OldPage, page, _pendingTransformation.OldX, _pendingTransformation.OldY, x, y, item));
        }

        /// <inheritdoc />
        public void OnDroppingPreviousItem(in KitItemResolutionResult result, Vector3 dropNearbyPosition, Item item)
        {
            if (_pendingTransformation.NewX == byte.MaxValue)
                return;

            _itemTracking.ItemDropTransformations.Add(new ItemDropTransformation(_pendingTransformation.OldPage, _pendingTransformation.OldX, _pendingTransformation.OldY, item));
        }

        private readonly void UpdateItemCountsTable(in KitItemResolutionResult resolvedItem)
        {
            if (resolvedItem.Asset == null)
                return;

            Guid guid = resolvedItem.Asset.GUID;
            for (int i = 0; i < _itemCountsTable!.Count; ++i)
            {
                KeyValuePair<Guid, int> row = _itemCountsTable[i];
                if (row.Key == guid)
                {
                    _itemCountsTable[i] = new KeyValuePair<Guid, int>(row.Key, row.Value + 1);
                    return;
                }
            }

            _itemCountsTable.Add(new KeyValuePair<Guid, int>(guid, 1));
        }

        private readonly void ApplyLowAmmoChanges(ref KitItemResolutionResult result)
        {
            switch (result.Asset)
            {
                case ItemMagazineAsset { deleteEmpty: true }:
                    // low ammo does not spawn any 'deleteEmpty' magazines
                    result.Asset = null;
                    break;

                case ItemMagazineAsset:
                    // low ammo causes all regular magazines to be empty
                    result.Amount = 0;
                    break;

                case ItemGunAsset:
                    // low ammo causes extra guns to spawn empty
                    result.State[10] = 0;
                    break;

                case ItemChargeAsset or ItemTrapAsset or ItemThrowableAsset:
                    // low ammo does not spawn any sort of charges, mines, or grenades
                    result.Asset = null;
                    break;

                case not null:
                    Guid guid = result.Asset.GUID;
                    if (_itemCountsTable!.Find(x => x.Key == guid).Value > 0)
                    {
                        // for all other items, low ammo only allows 1 of that particular item to be given
                        result.Asset = null;
                    }
                    break;
            }
        }

        public readonly void Dispose()
        {
            _enumerator.Dispose();
        }
    }

    [Flags]
    private enum BestowKitOptions
    {
        LowAmmo = 1,
        Silent = 1 << 1,
        IsRestock = 1 << 2
    }
}

/// <summary>
/// Data needed to bestow a kit onto a player.
/// </summary>
public readonly struct KitBestowData
{
    public Kit Kit { get; }
    public IReadOnlyList<KitLayoutTransformation>? Layouts { get; }
    public bool IsLowAmmo { get; init; }
    public bool Silent { get; init; }
    public bool ResupplyAmmoBags { get; init; } = true;
    internal bool RestockOnly { get; init; }
    public KitBestowData(Kit kit) : this(kit, null) { }
    internal KitBestowData(Kit kit, IReadOnlyList<KitLayoutTransformation>? layouts)
    {
        Kit = kit;
        Layouts = layouts;
    }

    internal KitBestowData Copy(IReadOnlyList<KitLayoutTransformation>? layouts)
    {
        return new KitBestowData(Kit, layouts)
        {
            IsLowAmmo = IsLowAmmo,
            Silent = Silent,
            RestockOnly = RestockOnly,
            ResupplyAmmoBags = ResupplyAmmoBags
        };
    }
}