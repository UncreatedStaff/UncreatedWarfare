using MySqlConnector;
using SDG.Unturned;
using Steamworks;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using SDG.NetTransport;
using Uncreated.Framework;
using Uncreated.Json;
using Uncreated.Networking;
using Uncreated.SQL;
using Uncreated.Warfare.Configuration;
using Uncreated.Warfare.Events.Players;
using Uncreated.Warfare.Gamemodes;
using Uncreated.Warfare.Gamemodes.Interfaces;
using Uncreated.Warfare.Quests;
using Uncreated.Warfare.Singletons;
using Uncreated.Warfare.Sync;
using Uncreated.Warfare.Teams;

namespace Uncreated.Warfare.Kits;

public class KitManager : ListSqlSingleton<Kit>, IQuestCompletedHandlerAsync, IPlayerConnectListenerAsync
{
    private static KitManager? _km;
    public override bool AwaitLoad => true;
    public override MySqlDatabase Sql => Data.AdminSql;
    public static event KitChanged? OnKitChanged;
    public static event KitAccessCallback? OnKitAccessChanged;

    public KitManager() : base("kits", SCHEMAS)
    {
        OnItemDeleted += OnKitDeleted;
        OnKitAccessChanged += OnKitAccessChangedIntl;
    }
    public override Task PostLoad()
    {
        PlayerLife.OnPreDeath += OnPreDeath;
        return base.PostLoad();
    }
    public override Task PreUnload()
    {
        PlayerLife.OnPreDeath -= OnPreDeath;
        return base.PreUnload();
    }
    public static KitManager? GetSingletonQuick()
    {
        if (_km == null || !_km.IsLoaded)
            return _km = Data.Singletons.GetSingleton<KitManager>();
        return _km.IsLoaded ? _km : null;
    }
    public static float GetDefaultTeamLimit(Class @class) => @class switch
    {
        Class.HAT => 0.1f,
        _ => 1f
    };
    /// <remarks>Thread Safe</remarks>
    public async Task TryGiveKitOnJoinTeam(UCPlayer player, CancellationToken token = default)
    {
        SqlItem<Kit>? kit = await GetDefaultKit(player.GetTeam(), token).ConfigureAwait(false);
        if (kit?.Item == null)
        {
            L.LogWarning("Unable to give " + player.CharacterName + " a kit.");
            await UCWarfare.ToUpdate(token);
            UCInventoryManager.ClearInventory(player);
            return;
        }

        await GiveKit(player, kit, token).ConfigureAwait(false);
    }
    private async Task<SqlItem<Kit>?> GetDefaultKit(ulong team, CancellationToken token = default)
    {
        SqlItem<Kit>? kit = team is 1 or 2 ? (await FindKit(team == 1 ? TeamManager.Team1UnarmedKit : TeamManager.Team2UnarmedKit, token).ConfigureAwait(false)) : null;
        if (kit?.Item == null)
            kit = await FindKit(TeamManager.DefaultKit, token).ConfigureAwait(false);
        return kit;
    }
    /// <remarks>Thread Safe</remarks>
    public async Task<SqlItem<Kit>?> FindKit(string id, CancellationToken token = default, bool exactMatchOnly = true)
    {
        await WaitAsync(token).ConfigureAwait(false);
        try
        {
            int index = F.StringSearch(List, x => x.Item?.Id, id, exactMatchOnly);
            return index == -1 ? null : List[index];
        }
        finally
        {
            Release();
        }
    }
    /// <remarks>Thread Safe</remarks>
    public async Task GiveKit(UCPlayer player, SqlItem<Kit>? kit, CancellationToken token = default)
    {
        if (player == null) throw new ArgumentNullException(nameof(player));
        SqlItem<Kit>? oldKit = null;
        if (kit?.Item == null)
        {
            if (player.HasKit)
                oldKit = player.ActiveKit;
            await UCWarfare.ToUpdate(token);
            player.EnsureSkillsets(Array.Empty<Skillset>());
            GiveKitToPlayerInventory(player, null, true);
            player.ChangeKit(null);
            if (oldKit?.Item != null)
                UpdateSigns(oldKit);
            OnKitChanged?.Invoke(player, oldKit, null);
            return;
        }
        await kit.Enter(token);
        try
        {
            if (kit.Item == null)
                throw new ArgumentNullException(nameof(kit));
    #if DEBUG
            using IDisposable profiler = ProfilingUtils.StartTracking();
    #endif
            await UCWarfare.ToUpdate(token);
            if (player.HasKit)
            {
                oldKit = player.ActiveKit;
                player.EnsureSkillsets(kit.Item.Skillsets);
                GiveKitToPlayerInventory(player, kit.Item, true);

                player.ChangeKit(kit);
                if (oldKit?.Item != null)
                    UpdateSigns(oldKit);
                UpdateSigns(kit);
            }
        }
        finally
        {
            kit.Release();
        }

        OnKitChanged?.Invoke(player, oldKit, kit);
    }
    private static void GiveKitToPlayerInventory(UCPlayer player, Kit? kit, bool clear = true)
    {
        if (player == null) throw new ArgumentNullException(nameof(player));
#if DEBUG
        using IDisposable profiler = ProfilingUtils.StartTracking();
#endif

        if (clear) UCInventoryManager.ClearInventory(player, !Data.UseFastKits);
        else ThreadUtil.assertIsGameThread();

        player.Player.equipment.dequip();
        if (kit == null)
            return;
        IKitItem[] items = kit.Items;
        FactionInfo? faction = player.Faction;
        if (Data.UseFastKits)
        {
            NetId id = player.Player.clothing.GetNetId();
            byte flag = 0;
            bool hasPlayedEffect = false;
            for (int i = 0; i < items.Length; ++i)
            {
                IKitItem item = items[i];
                if (item is not IClothingJar clothingJar)
                    continue;
                ItemAsset? asset = item.GetItem(kit, faction, out _, out byte[] state);
                if (asset == null || asset.type != clothingJar.Type.GetItemType())
                {
                    ReportItemError(kit, item, asset);
                    continue;
                }
                flag |= (byte)(1 << (int)clothingJar.Type);
                ClientInstanceMethod<Guid, byte, byte[], bool>? inv =
                    clothingJar.Type switch
                    {
                        ClothingType.Shirt => Data.SendWearShirt,
                        ClothingType.Pants => Data.SendWearPants,
                        ClothingType.Hat => Data.SendWearHat,
                        ClothingType.Backpack => Data.SendWearBackpack,
                        ClothingType.Vest => Data.SendWearVest,
                        ClothingType.Mask => Data.SendWearMask,
                        ClothingType.Glasses => Data.SendWearGlasses,
                        _ => null
                    };
                if (inv != null)
                {
                    inv.InvokeAndLoopback(id, ENetReliability.Reliable, Provider.EnumerateClients_Remote(), asset.GUID, 100, state, !hasPlayedEffect);
                    hasPlayedEffect = true;
                }
            }
            byte[] blank = Array.Empty<byte>();
            for (int i = 0; i < 7; ++i)
            {
                if (((flag >> i) & 1) != 1)
                {
                    ((ClothingType)i switch
                    {
                        ClothingType.Shirt => Data.SendWearShirt,
                        ClothingType.Pants => Data.SendWearPants,
                        ClothingType.Hat => Data.SendWearHat,
                        ClothingType.Backpack => Data.SendWearBackpack,
                        ClothingType.Vest => Data.SendWearVest,
                        ClothingType.Mask => Data.SendWearMask,
                        ClothingType.Glasses => Data.SendWearGlasses,
                        _ => null
                    })?.InvokeAndLoopback(id, ENetReliability.Reliable, Provider.EnumerateClients_Remote(), Guid.Empty, 100, blank, false);
                }
            }
            Items[] p = player.Player.inventory.items;
            bool ohi = Data.GetOwnerHasInventory(player.Player.inventory);
            if (ohi)
                Data.SetOwnerHasInventory(player.Player.inventory, false);
            for (int i = 0; i < kit.Items.Length; ++i)
            {
                IKitItem item = kit.Items[i];
                if (item is not IItemJar jar)
                    continue;
                ItemAsset? asset = item.GetItem(kit, faction, out byte amt, out byte[] state);
                if ((int)jar.Page < PlayerInventory.PAGES - 2 && asset != null)
                    p[(int)jar.Page].addItem(jar.X, jar.Y, jar.Rotation, new Item(asset.id, amt, 100, state));
                else
                    ReportItemError(kit, item, asset);
            }
            if (ohi)
                Data.SetOwnerHasInventory(player.Player.inventory, true);
            UCInventoryManager.SendPages(player);
        }
        else
        {
            foreach (IKitItem item in kit.Items)
            {
                ItemAsset? asset = item.GetItem(kit, faction, out byte amt, out byte[] state);
                if (asset is null)
                {
                    ReportItemError(kit, item, null);
                    return;
                }
                if (item is IClothingJar clothing)
                {
                    if (clothing.Type == ClothingType.Shirt)
                    {
                        if (asset is ItemShirtAsset shirt)
                            player.Player.clothing.askWearShirt(shirt, 100, state, true);
                        else goto e;
                    }
                    else if (clothing.Type == ClothingType.Pants)
                    {
                        if (asset is ItemPantsAsset pants)
                            player.Player.clothing.askWearPants(pants, 100, state, true);
                        else goto e;
                    }
                    else if (clothing.Type == ClothingType.Vest)
                    {
                        if (asset is ItemVestAsset vest)
                            player.Player.clothing.askWearVest(vest, 100, state, true);
                        else goto e;
                    }
                    else if (clothing.Type == ClothingType.Hat)
                    {
                        if (asset is ItemHatAsset hat)
                            player.Player.clothing.askWearHat(hat, 100, state, true);
                        else goto e;
                    }
                    else if (clothing.Type == ClothingType.Mask)
                    {
                        if (asset is ItemMaskAsset mask)
                            player.Player.clothing.askWearMask(mask, 100, state, true);
                        else goto e;
                    }
                    else if (clothing.Type == ClothingType.Backpack)
                    {
                        if (asset is ItemBackpackAsset backpack)
                            player.Player.clothing.askWearBackpack(backpack, 100, state, true);
                        else goto e;
                    }
                    else if (clothing.Type == ClothingType.Glasses)
                    {
                        if (asset is ItemGlassesAsset glasses)
                            player.Player.clothing.askWearGlasses(glasses, 100, state, true);
                        else goto e;
                    }
                    else
                        goto e;
                    continue;
                }

                e:
                ReportItemError(kit, item, asset);
                Item uitem = new Item(asset.id, amt, 100, state);

                if (item is not IItemJar jar || !player.Player.inventory.tryAddItem(uitem, jar.X, jar.Y, (byte)jar.Page, jar.Rotation))
                {
                    if (!player.Player.inventory.tryAddItem(uitem, true))
                    {
                        ItemManager.dropItem(uitem, player.Position, false, true, true);
                    }
                }
            }
        }
    }
    private static void ReportItemError(Kit kit, IKitItem item, ItemAsset? asset)
    {
        if (asset == null)
        {
            L.LogWarning("Unknown item in kit \"" + kit.Id + "\": {" +
                         item switch
                         {
                             IItem i2 => i2.Item.ToString("N"),
                             IClothing i2 => i2.Item.ToString("N"),
                             _ => item.ToString()
                         } + "}.");
        }
        else if (item is IClothingJar clothing)
        {
            L.LogWarning("Invalid " + clothing.Type.ToString().ToLowerInvariant() +
                         " in kit \"" + kit.Id + "\" for item " + asset.itemName +
                         " {" + asset.GUID.ToString("N") + "}.");
        }
        else
        {
            L.LogWarning("Invalid item" +
                         " in kit \"" + kit.Id + "\" for item " + asset.itemName +
                         " {" + asset.GUID.ToString("N") + "}.");
        }
    }
    public async Task OnQuestCompleted(QuestCompleted e)
    {
        await WaitAsync().ConfigureAwait(false);
        try
        {
            foreach (SqlItem<Kit> kit in Items)
            {
                if (kit.Item != null && kit.Item.Type != KitType.Loadout && kit.Item.UnlockRequirements != null)
                {
                    for (int j = 0; j < kit.Item.UnlockRequirements.Length; j++)
                    {
                        if (kit.Item.UnlockRequirements[j] is QuestUnlockRequirement { UnlockPresets.Length: > 0 } req && !req.CanAccess(e.Player))
                        {
                            for (int r = 0; r < req.UnlockPresets.Length; r++)
                            {
                                if (req.UnlockPresets[r] == e.PresetKey)
                                {
                                    e.Break();
                                    return;
                                }
                            }
                        }
                    }
                }
            }
        }
        finally
        {
            Release();
        }
    }
    public async Task OnPlayerConnecting(UCPlayer player)
    {
#if DEBUG
        using IDisposable profiler = ProfilingUtils.StartTracking();
#endif
        await WaitAsync().ConfigureAwait(false);
        try
        {
            await UCWarfare.ToUpdate();
            if (!player.IsOnline)
                return;
            foreach (SqlItem<Kit> kit in Items)
            {
                if (kit.Item != null && kit.Item.Type != KitType.Loadout && kit.Item.UnlockRequirements != null)
                {
                    for (int j = 0; j < kit.Item.UnlockRequirements.Length; j++)
                    {
                        if (kit.Item.UnlockRequirements[j] is QuestUnlockRequirement { UnlockPresets.Length: > 0 } req && !req.CanAccess(player))
                        {
                            if (Assets.find(req.QuestID) is QuestAsset quest)
                            {
                                player.Player.quests.sendAddQuest(quest.id);
                            }
                            else
                            {
                                L.LogWarning("Unknown quest id " + req.QuestID + " in kit requirement for " + kit.Item.Id);
                            }
                            for (int r = 0; r < req.UnlockPresets.Length; r++)
                            {
                                BaseQuestTracker? tracker = QuestManager.CreateTracker(player, req.UnlockPresets[r]);
                                if (tracker == null)
                                {
                                    L.LogWarning("Failed to create tracker for kit " + kit.Item.Id + ", player " + player.Name.PlayerName);
                                }
                            }
                        }
                    }
                }
            }
        }
        finally
        {
            Release();
        }
        RequestSigns.UpdateAllSigns(player);
    }
    private void OnPreDeath(PlayerLife life)
    {
#if DEBUG
        using IDisposable profiler = ProfilingUtils.StartTracking();
#endif
        UCPlayer? player = UCPlayer.FromPlayer(life.player);
        if (player?.ActiveKit?.Item != null)
        {
            for (byte page = 0; page < PlayerInventory.PAGES - 1; page++)
            {
                if (page == PlayerInventory.AREA)
                    continue;

                for (byte index = 0; index < life.player.inventory.getItemCount(page); index++)
                {
                    ItemJar jar = life.player.inventory.getItem(page, index);

                    if (Assets.find(EAssetType.ITEM, jar.item.id) is not ItemAsset asset) continue;
                    float percentage = (float)jar.item.amount / asset.amount;

                    bool notInKit = !player.ActiveKit.Item.HasItemOfID(asset.GUID) && Whitelister.IsWhitelisted(asset.GUID, out _);
                    if (notInKit || (percentage < 0.3 && asset.type != EItemType.GUN))
                    {
                        if (notInKit)
                        {
                            ItemManager.dropItem(jar.item, life.player.transform.position, false, true, true);
                        }

                        life.player.inventory.removeItem(page, index);
                        index--;
                    }
                }
            }
        }
    }
    /// <remarks>Thread Safe</remarks>
    public async Task DequipKit(Kit kit, CancellationToken token = default)
    {
        SqlItem<Kit>? t1def = null;
        SqlItem<Kit>? t2def = null;
        for (int i = 0; i < PlayerManager.OnlinePlayers.Count; ++i)
        {
            UCPlayer pl = PlayerManager.OnlinePlayers[i];
            if (pl.ActiveKit is not null && pl.ActiveKit.LastPrimaryKey.Key == kit.PrimaryKey.Key)
            {
                ulong team = pl.GetTeam();
                if (team == 1 && (t1def ??= await GetDefaultKit(1ul, token))?.Item != null)
                    await GiveKit(pl, t1def == kit ? null : t1def, token);
                else if (team == 2 && (t2def ??= await GetDefaultKit(2ul, token))?.Item != null)
                    await GiveKit(pl, t2def == kit ? null : t2def, token);
                else
                    await GiveKit(pl, null, token);
            }
        }
    }
    /// <remarks>Thread Safe</remarks>
    public async Task DequipKit(UCPlayer player, Kit kit, CancellationToken token = default)
    {
        if (player.ActiveKit is not null && player.ActiveKit.LastPrimaryKey.Key == kit.PrimaryKey.Key)
        {
            ulong team = player.GetTeam();
            SqlItem<Kit>? dkit = await GetDefaultKit(team, token);
            if (dkit?.Item != null)
                await GiveKit(player, dkit, token);
            else
                await GiveKit(player, null, token);
        }
    }
    private void OnKitAccessChangedIntl(SqlItem<Kit> kit, ulong player)
    {
        UCPlayer? pl = UCPlayer.FromID(player);
        if (pl != null && kit.Item != null && pl.ActiveKit == kit && !HasAccessQuick(kit, pl))
        {
            Task.Run(() => Util.TryWrap(DequipKit(pl, kit.Item), "Failed to dequip " + kit.Item.Id + " from " + player + "."));
        }
    }
    private void OnKitDeleted(SqlItem<Kit> proxy, Kit kit)
    {
        Task.Run(() => Util.TryWrap(DequipKit(kit), "Failed to dequip " + kit.Id + " from all."));
    }
    /// <remarks>Thread Safe</remarks>
    public static void UpdateSigns()
    {
        if (UCWarfare.IsMainThread)
            UpdateSignsIntl(null);
        else
            UCWarfare.RunOnMainThread(() => UpdateSignsIntl(null));
    }
    /// <remarks>Thread Safe</remarks>
    public static void UpdateSigns(UCPlayer player)
    {
        if (UCWarfare.IsMainThread)
            UpdateSignsIntl(player);
        else
            UCWarfare.RunOnMainThread(() => UpdateSignsIntl(player));
    }
    /// <remarks>Thread Safe</remarks>
    public static void UpdateSigns(SqlItem<Kit> kit)
    {
        if (UCWarfare.IsMainThread)
            UpdateSignsIntl(kit, null);
        else
            UCWarfare.RunOnMainThread(() => UpdateSignsIntl(kit, null));
    }
    /// <remarks>Thread Safe</remarks>
    public static void UpdateSigns(Kit kit)
    {
        if (UCWarfare.IsMainThread)
            UpdateSignsIntl(kit, null);
        else
            UCWarfare.RunOnMainThread(() => UpdateSignsIntl(kit, null));
    }
    /// <remarks>Thread Safe</remarks>
    public static void UpdateSigns(Kit kit, UCPlayer player)
    {
        if (UCWarfare.IsMainThread)
            UpdateSignsIntl(kit, player);
        else
            UCWarfare.RunOnMainThread(() => UpdateSignsIntl(kit, player));
    }
    /// <remarks>Thread Safe</remarks>
    public static void UpdateSigns(string kitId)
    {
        if (UCWarfare.IsMainThread)
            UpdateSignsIntl(kitId, null);
        else
            UCWarfare.RunOnMainThread(() => UpdateSignsIntl(kitId, null));
    }
    /// <remarks>Thread Safe</remarks>
    public static void UpdateSigns(string kitId, UCPlayer player)
    {
        if (UCWarfare.IsMainThread)
            UpdateSignsIntl(kitId, player);
        else
            UCWarfare.RunOnMainThread(() => UpdateSignsIntl(kitId, player));
    }
    private static void UpdateSignsIntl(UCPlayer? player)
    {
        if (RequestSigns.Loaded)
        {
            if (player is null)
            {
                for (int i = 0; i < RequestSigns.Singleton.Count; i++)
                {
                    RequestSigns.Singleton[i].InvokeUpdate();
                }
            }
            else
            {
                for (int i = 0; i < RequestSigns.Singleton.Count; i++)
                {
                    RequestSigns.Singleton[i].InvokeUpdate(player);
                }
            }
        }
    }
    private static void UpdateSignsIntl(Kit kit, UCPlayer? player)
    {
        if (RequestSigns.Loaded)
        {
            if (kit.Type == KitType.Loadout)
            {
                if (player is null)
                {
                    for (int i = 0; i < RequestSigns.Singleton.Count; i++)
                    {
                        if (RequestSigns.Singleton[i].KitName.StartsWith(Signs.LOADOUT_PREFIX, StringComparison.OrdinalIgnoreCase))
                            RequestSigns.Singleton[i].InvokeUpdate();
                    }
                }
                else
                {
                    for (int i = 0; i < RequestSigns.Singleton.Count; i++)
                    {
                        if (RequestSigns.Singleton[i].KitName.StartsWith(Signs.LOADOUT_PREFIX, StringComparison.OrdinalIgnoreCase))
                            RequestSigns.Singleton[i].InvokeUpdate(player);
                    }
                }
            }
            else
            {
                UpdateSignsIntl(kit.Id, player);
            }
        }
    }
    private static void UpdateSignsIntl(SqlItem<Kit> kit, UCPlayer? player)
    {
        if (kit?.Item == null)
            return;
        UpdateSignsIntl(kit.Item, player);
    }
    private static void UpdateSignsIntl(string kitId, UCPlayer? player)
    {
        if (RequestSigns.Loaded)
        {
            if (player is null)
            {
                for (int i = 0; i < RequestSigns.Singleton.Count; i++)
                {
                    if (RequestSigns.Singleton[i].KitName.Equals(kitId, StringComparison.Ordinal))
                        RequestSigns.Singleton[i].InvokeUpdate();
                }
            }
            else
            {
                for (int i = 0; i < RequestSigns.Singleton.Count; i++)
                {
                    if (RequestSigns.Singleton[i].KitName.Equals(kitId, StringComparison.Ordinal))
                        RequestSigns.Singleton[i].InvokeUpdate(player);
                }
            }
        }
    }
    /// <remarks>Thread Safe</remarks>
    public async Task<bool> GiveAccess(Kit kit, UCPlayer player, KitAccessType type, CancellationToken token = default)
    {
        SqlItem<Kit>? proxy = await FindProxy(kit.PrimaryKey, token).ConfigureAwait(false);
        if (proxy?.Item == null)
            return false;
        return await GiveAccess(proxy, player, type, token).ConfigureAwait(false);
    }
    /// <remarks>Thread Safe</remarks>
    public async Task<bool> GiveAccess(Kit kit, ulong player, KitAccessType type, CancellationToken token = default)
    {
        SqlItem<Kit>? proxy = await FindProxy(kit.PrimaryKey, token).ConfigureAwait(false);
        if (proxy?.Item == null)
            return false;
        return await GiveAccess(proxy, player, type, token).ConfigureAwait(false);
    }
    /// <remarks>Thread Safe</remarks>
    public static async Task<bool> GiveAccess(SqlItem<Kit> kit, UCPlayer player, KitAccessType type, CancellationToken token = default)
    {
        if (!player.IsOnline)
        {
            return await GiveAccess(kit, player.Steam64, type, token).ConfigureAwait(false);
        }

        await player.PurchaseSync.WaitAsync(token).ConfigureAwait(false);
        try
        {
            if (player.AccessibleKits != null && player.AccessibleKits.Contains(kit))
                return true;
            bool access = await AddAccessRow(kit.PrimaryKey, player.Steam64, type, token).ConfigureAwait(false);
            if (access)
            {
                (player.AccessibleKits ??= new List<SqlItem<Kit>>(4)).Add(kit);
                OnKitAccessChanged?.Invoke(kit, player.Steam64);
            }

            return access;
        }
        finally
        {
            player.PurchaseSync.Release();
        }
    }
    /// <remarks>Thread Safe</remarks>
    public static async Task<bool> GiveAccess(SqlItem<Kit> kit, ulong player, KitAccessType type, CancellationToken token = default)
    {
        if (!kit.PrimaryKey.IsValid)
            return false;
        UCPlayer? online = UCPlayer.FromID(player);
        if (online != null && online.IsOnline)
            return await GiveAccess(kit, online, type, token).ConfigureAwait(false);
        bool res = await AddAccessRow(kit.PrimaryKey, player, type, token).ConfigureAwait(false);
        OnKitAccessChanged?.Invoke(kit, player);
        return res;
    }
    /// <remarks>Thread Safe</remarks>
    public async Task<bool> RemoveAccess(Kit kit, UCPlayer player, CancellationToken token = default)
    {
        SqlItem<Kit>? proxy = await FindProxy(kit.PrimaryKey, token).ConfigureAwait(false);
        if (proxy?.Item == null)
            return false;
        return await RemoveAccess(proxy, player, token).ConfigureAwait(false);
    }
    /// <remarks>Thread Safe</remarks>
    public async Task<bool> RemoveAccess(Kit kit, ulong player, CancellationToken token = default)
    {
        SqlItem<Kit>? proxy = await FindProxy(kit.PrimaryKey, token).ConfigureAwait(false);
        if (proxy?.Item == null)
            return false;
        return await RemoveAccess(proxy, player, token).ConfigureAwait(false);
    }
    /// <remarks>Thread Safe</remarks>
    public static async Task<bool> RemoveAccess(SqlItem<Kit> kit, UCPlayer player, CancellationToken token = default)
    {
        if (!player.IsOnline)
        {
            return await RemoveAccess(kit, player.Steam64, token).ConfigureAwait(false);
        }

        await player.PurchaseSync.WaitAsync(token).ConfigureAwait(false);
        try
        {
            if (player.AccessibleKits != null && player.AccessibleKits.Contains(kit))
                return true;
            bool access = await RemoveAccessRow(kit.PrimaryKey, player.Steam64, token).ConfigureAwait(false);
            if (access)
            {
                (player.AccessibleKits ??= new List<SqlItem<Kit>>(4)).Add(kit);
                OnKitAccessChanged?.Invoke(kit, player.Steam64);
            }

            return access;
        }
        finally
        {
            player.PurchaseSync.Release();
        }
    }
    /// <remarks>Thread Safe</remarks>
    public static async Task<bool> RemoveAccess(SqlItem<Kit> kit, ulong player, CancellationToken token = default)
    {
        if (!kit.PrimaryKey.IsValid)
            return false;
        UCPlayer? online = UCPlayer.FromID(player);
        if (online != null && online.IsOnline)
            return await RemoveAccess(kit, online, token).ConfigureAwait(false);
        bool res = await RemoveAccessRow(kit.PrimaryKey, player, token).ConfigureAwait(false);
        OnKitAccessChanged?.Invoke(kit, player);
        return res;
    }
    private static async Task<bool> AddAccessRow(PrimaryKey kit, ulong player, KitAccessType type, CancellationToken token = default)
    {
        return await Data.AdminSql.NonQueryAsync(
            $"INSERT INTO `{TABLE_ACCESS}` ({SqlTypes.ColumnList(COLUMN_EXT_PK, COLUMN_ACCESS_STEAM_64, COLUMN_ACCESS_TYPE)}) " +
            $"VALUES (@0, @1, @2);", new object[] { kit.Key, player, type.ToString() }, token).ConfigureAwait(false) > 0;
    }
    private static async Task<bool> RemoveAccessRow(PrimaryKey kit, ulong player, CancellationToken token = default)
    {
        return await Data.AdminSql.NonQueryAsync(
            $"DELETE FROM `{TABLE_ACCESS}` WHERE `{COLUMN_EXT_PK}`=@0 AND `{COLUMN_ACCESS_STEAM_64}`=@1;",
            new object[] { kit.Key, player }, token).ConfigureAwait(false) > 0;
    }
    /// <remarks>Thread Safe</remarks>
    public static bool HasAccessQuick(SqlItem<Kit> kit, UCPlayer player) => HasAccessQuick(kit.PrimaryKey, player);
    /// <remarks>Thread Safe</remarks>
    public static bool HasAccessQuick(Kit kit, UCPlayer player) => HasAccessQuick(kit.PrimaryKey, player);
    /// <remarks>Thread Safe</remarks>
    public static bool HasAccessQuick(PrimaryKey kit, UCPlayer player)
    {
        if (player.AccessibleKits == null || !kit.IsValid)
            return false;
        int k = kit.Key;
        for (int i = 0; i < player.AccessibleKits.Count; ++i)
        {
            if (player.AccessibleKits[i].PrimaryKey.Key == k)
                return true;
        }

        return false;
    }
    /// <remarks>Thread Safe</remarks>
    public static Task<bool> HasAccess(SqlItem<Kit> kit, UCPlayer player, CancellationToken token = default)
        => HasAccess(kit.PrimaryKey, player.Steam64, token);
    /// <remarks>Thread Safe</remarks>
    public static Task<bool> HasAccess(SqlItem<Kit> kit, ulong player, CancellationToken token = default)
        => HasAccess(kit.PrimaryKey, player, token);
    /// <remarks>Thread Safe</remarks>
    public static Task<bool> HasAccess(Kit kit, UCPlayer player, CancellationToken token = default)
        => HasAccess(kit.PrimaryKey, player.Steam64, token);
    /// <remarks>Thread Safe</remarks>
    public static Task<bool> HasAccess(Kit kit, ulong player, CancellationToken token = default)
        => HasAccess(kit.PrimaryKey, player, token);
    /// <remarks>Thread Safe</remarks>
    public static Task<bool> HasAccess(PrimaryKey kit, UCPlayer player, CancellationToken token = default)
        => HasAccess(kit, player.Steam64, token);
    /// <remarks>Thread Safe</remarks>
    public static async Task<bool> HasAccess(PrimaryKey kit, ulong player, CancellationToken token = default)
    {
        if (!kit.IsValid)
            return false;
        int ct = 0;
        await Data.DatabaseManager.QueryAsync($"SELECT COUNT(`{COLUMN_ACCESS_STEAM_64}`) FROM `{TABLE_ACCESS}` " +
                                              $"WHERE `{COLUMN_EXT_PK}`=@0 AND `{COLUMN_ACCESS_STEAM_64}`=@1;", new object[]
        {
            kit.Key, player
        }, reader => ct = reader.GetInt32(0), token).ConfigureAwait(false);
        return ct > 0;
    }
    /// <remarks>Thread Safe</remarks>
    public static ValueTask<bool> HasAccessQuick(SqlItem<Kit> kit, ulong player, CancellationToken token = default)
        => HasAccessQuick(kit.PrimaryKey, player, token);
    /// <remarks>Thread Safe</remarks>
    public static ValueTask<bool> HasAccessQuick(Kit kit, ulong player, CancellationToken token = default)
        => HasAccessQuick(kit.PrimaryKey, player, token);
    /// <remarks>Thread Safe</remarks>
    public static ValueTask<bool> HasAccessQuick(PrimaryKey kit, ulong player, CancellationToken token = default)
    {
        if (!kit.IsValid)
            return new ValueTask<bool>(false);
        UCPlayer? pl = UCPlayer.FromID(player);
        if (pl != null && pl.IsOnline)
            return new ValueTask<bool>(HasAccessQuick(kit, pl));
        return new ValueTask<bool>(HasAccess(kit, player, token));
    }

    #region Sql
    // ReSharper disable InconsistentNaming
    public const string TABLE_MAIN = "kits";
    public const string TABLE_ITEMS = "kits_items";
    public const string TABLE_UNLOCK_REQUIREMENTS = "kits_unlock_requirements";
    public const string TABLE_SKILLSETS = "kits_skillsets";
    public const string TABLE_FACTION_BLACKLIST = "kits_faction_blacklist";
    public const string TABLE_SIGN_TEXT = "kits_sign_text";
    public const string TABLE_ACCESS = "kits_access";

    public const string COLUMN_PK = "pk";
    public const string COLUMN_KIT_ID = "Id";
    public const string COLUMN_FACTION = "Faction";
    public const string COLUMN_CLASS = "Class";
    public const string COLUMN_BRANCH = "Branch";
    public const string COLUMN_TYPE = "Type";
    public const string COLUMN_REQUEST_COOLDOWN = "RequestCooldown";
    public const string COLUMN_TEAM_LIMIT = "TeamLimit";
    public const string COLUMN_SEASON = "Season";
    public const string COLUMN_DISABLED = "Disabled";
    public const string COLUMN_WEAPONS = "Weapons";
    public const string COLUMN_SQUAD_LEVEL = "SquadLevel";

    public const string COLUMN_EXT_PK = "Kit";
    public const string COLUMN_ITEM_GUID = "Item";
    public const string COLUMN_ITEM_X = "X";
    public const string COLUMN_ITEM_Y = "Y";
    public const string COLUMN_ITEM_ROTATION = "Rotation";
    public const string COLUMN_ITEM_PAGE = "Page";
    public const string COLUMN_ITEM_CLOTHING = "ClothingSlot";
    public const string COLUMN_ITEM_REDIRECT = "Redirect";
    public const string COLUMN_ITEM_AMOUNT = "Amount";
    public const string COLUMN_ITEM_METADATA = "Metadata";

    public const string COLUMN_ACCESS_STEAM_64 = "Steam64";
    public const string COLUMN_ACCESS_TYPE = "AccessType";
    private static readonly Schema[] SCHEMAS =
    {
        new Schema(TABLE_MAIN, new Schema.Column[]
        {
            new Schema.Column(COLUMN_PK, SqlTypes.INCREMENT_KEY)
            {
                PrimaryKey = true,
                AutoIncrement = true
            },
            new Schema.Column(COLUMN_KIT_ID, SqlTypes.String(KitEx.KitNameMaxCharLimit)),
            new Schema.Column(COLUMN_FACTION, SqlTypes.INCREMENT_KEY)
            {
                ForeignKey = true,
                ForeignKeyColumn = FactionInfo.COLUMN_PK,
                ForeignKeyTable = FactionInfo.TABLE_MAIN,
                Nullable = true
            },
            new Schema.Column(COLUMN_CLASS, SqlTypes.Enum(Class.None)),
            new Schema.Column(COLUMN_BRANCH, SqlTypes.Enum(Branch.Default)),
            new Schema.Column(COLUMN_TYPE, SqlTypes.Enum<KitType>()),
            new Schema.Column(COLUMN_REQUEST_COOLDOWN, SqlTypes.FLOAT) { Nullable = true },
            new Schema.Column(COLUMN_TEAM_LIMIT, SqlTypes.FLOAT) { Nullable = true },
            new Schema.Column(COLUMN_SEASON, SqlTypes.BYTE) { Nullable = true },
            new Schema.Column(COLUMN_DISABLED, SqlTypes.BOOLEAN) { Nullable = true },
            new Schema.Column(COLUMN_WEAPONS, SqlTypes.String(KitEx.WeaponTextMaxCharLimit)) { Nullable = true },
            new Schema.Column(COLUMN_SQUAD_LEVEL, SqlTypes.Enum<SquadLevel>()) { Nullable = true }
        }, true, typeof(KitOld)),
        new Schema(TABLE_ITEMS, new Schema.Column[]
        {
            new Schema.Column(COLUMN_EXT_PK, SqlTypes.INCREMENT_KEY)
            {
                ForeignKey = true,
                ForeignKeyTable = TABLE_MAIN,
                ForeignKeyColumn = COLUMN_PK
            },
            new Schema.Column(COLUMN_ITEM_GUID, SqlTypes.GUID_STRING) { Nullable = true },
            new Schema.Column(COLUMN_ITEM_X, SqlTypes.BYTE) { Nullable = true },
            new Schema.Column(COLUMN_ITEM_Y, SqlTypes.BYTE) { Nullable = true },
            new Schema.Column(COLUMN_ITEM_ROTATION, SqlTypes.BYTE) { Nullable = true },
            new Schema.Column(COLUMN_ITEM_PAGE, SqlTypes.Enum<Page>()) { Nullable = true },
            new Schema.Column(COLUMN_ITEM_CLOTHING, SqlTypes.Enum<ClothingType>()) { Nullable = true },
            new Schema.Column(COLUMN_ITEM_REDIRECT, SqlTypes.Enum<RedirectType>()) { Nullable = true },
            new Schema.Column(COLUMN_ITEM_AMOUNT, SqlTypes.BYTE) { Default = "0" },
            new Schema.Column(COLUMN_ITEM_METADATA, SqlTypes.Binary(KitEx.MaxStateArrayLimit)) { Nullable = true },
        }, false, typeof(IKitItem)),
        UnlockRequirement.GetDefaultSchema(TABLE_UNLOCK_REQUIREMENTS, COLUMN_EXT_PK, TABLE_MAIN, COLUMN_PK),
        Skillset.GetDefaultSchema(TABLE_SKILLSETS, COLUMN_EXT_PK, TABLE_MAIN, COLUMN_PK),
        F.GetListSchema<PrimaryKey>(TABLE_FACTION_BLACKLIST, COLUMN_EXT_PK, COLUMN_FACTION, TABLE_MAIN, COLUMN_PK),
        F.GetTranslationListSchema(TABLE_SIGN_TEXT, COLUMN_EXT_PK, TABLE_MAIN, COLUMN_PK, KitEx.SignTextMaxCharLimit),
        new Schema(TABLE_ACCESS, new Schema.Column[]
        {
            new Schema.Column(COLUMN_EXT_PK, SqlTypes.INCREMENT_KEY)
            {
                ForeignKey = true,
                ForeignKeyTable = TABLE_MAIN,
                ForeignKeyColumn = COLUMN_PK
            },
            new Schema.Column(COLUMN_ACCESS_STEAM_64, SqlTypes.STEAM_64),
            new Schema.Column(COLUMN_ACCESS_TYPE, SqlTypes.Enum<KitAccessType>()),
        }, false, null)
    };
    // ReSharper restore InconsistentNaming
    [Obsolete]
    protected override async Task AddOrUpdateItem(Kit? item, PrimaryKey pk, CancellationToken token = default)
    {
        if (item == null)
        {
            if (!pk.IsValid)
                throw new ArgumentException("If item is null, pk must have a value to delete the item.", nameof(pk));
            await Sql.NonQueryAsync($"DELETE FROM `{TABLE_MAIN}` WHERE `{COLUMN_PK}`=@0;", new object[] { pk.Key }, token).ConfigureAwait(false);
            return;
        }
        bool hasPk = pk.IsValid;
        int pk2 = PrimaryKey.NotAssigned;
        object[] objs = new object[hasPk ? 12 : 11];
        objs[0] = item.Id ??= (hasPk ? pk.ToString() : "invalid_" + unchecked((uint)DateTime.UtcNow.Ticks));
        objs[1] = item.FactionKey.IsValid && item.FactionKey.Key != 0 ? item.FactionKey.Key : DBNull.Value;
        objs[2] = item.Class.ToString();
        objs[3] = item.Branch.ToString();
        objs[4] = item.Type.ToString();
        objs[5] = item.RequestCooldown <= 0f ? DBNull.Value : item.RequestCooldown;
        objs[6] = item.TeamLimit;
        objs[7] = item.Season;
        objs[8] = item.Disabled;
        objs[9] = (object?)item.WeaponText ?? DBNull.Value;
        objs[10] = item.SquadLevel <= SquadLevel.Member ? DBNull.Value : item.SquadLevel.ToString();
        if (hasPk)
            objs[11] = pk.Key;
        await Sql.QueryAsync($"INSERT INTO `{TABLE_MAIN}` ({SqlTypes.ColumnList(COLUMN_KIT_ID, COLUMN_FACTION, COLUMN_CLASS, COLUMN_BRANCH,
            COLUMN_TYPE, COLUMN_REQUEST_COOLDOWN, COLUMN_TEAM_LIMIT, COLUMN_SEASON, COLUMN_DISABLED, COLUMN_WEAPONS,
            COLUMN_SQUAD_LEVEL)}" +
            (hasPk ? $",`{COLUMN_PK}`" : string.Empty) +
            ") VALUES (@0,@1,@2,@3,@4,@5,@6,@7,@8,@9,@10" +
            (hasPk ? ",@11" : string.Empty) +
            ") ON DUPLICATE KEY UPDATE " +
            $"{SqlTypes.ColumnUpdateList(0, COLUMN_KIT_ID, COLUMN_FACTION, COLUMN_CLASS, COLUMN_BRANCH,
                COLUMN_TYPE, COLUMN_REQUEST_COOLDOWN, COLUMN_TEAM_LIMIT, COLUMN_SEASON, COLUMN_DISABLED, COLUMN_WEAPONS,
                COLUMN_SQUAD_LEVEL)}," +
            $"`{COLUMN_PK}`=LAST_INSERT_ID(`{COLUMN_PK}`);" +
            "SET @pk := (SELECT LAST_INSERT_ID() as `pk`);" +
            $"DELETE FROM `{TABLE_ITEMS}` WHERE `{COLUMN_EXT_PK}`=@pk;" +
            $"DELETE FROM `{TABLE_UNLOCK_REQUIREMENTS}` WHERE `{COLUMN_EXT_PK}`=@pk;" +
            $"DELETE FROM `{TABLE_SKILLSETS}` WHERE `{COLUMN_EXT_PK}`=@pk;" +
            $"DELETE FROM `{TABLE_FACTION_BLACKLIST}` WHERE `{COLUMN_EXT_PK}`=@pk;" +
            $"DELETE FROM `{TABLE_SIGN_TEXT}` WHERE `{COLUMN_EXT_PK}`=@pk; SELECT @pk;",
            objs, reader =>
            {
                pk2 = reader.GetInt32(0);
            }, token).ConfigureAwait(false);

        StringBuilder builder = new StringBuilder(128);
        if (item.Items is { Length: > 0 })
        {
            builder.Append($"INSERT INTO `{TABLE_ITEMS}` ({SqlTypes.ColumnList(
                COLUMN_EXT_PK, COLUMN_ITEM_GUID, COLUMN_ITEM_X, COLUMN_ITEM_Y, COLUMN_ITEM_ROTATION, COLUMN_ITEM_PAGE,
                COLUMN_ITEM_CLOTHING, COLUMN_ITEM_REDIRECT, COLUMN_ITEM_AMOUNT, COLUMN_ITEM_METADATA)}) VALUES ");
            objs = new object[item.Items.Length * 10];
            bool one = false;
            for (int i = 0; i < item.Items.Length; ++i)
            {
                IKitItem item2 = item.Items[i];
                if (item2 is not IItem && item2 is not IClothing && item2 is not IAssetRedirect || item2 is not IItemJar && item2 is not IClothingJar)
                {
                    L.LogWarning("Item in kit \"" + item.Id + "\" (" + item2 + ") not a valid type: " + item2.GetType().Name + ".");
                    continue;
                }

                one = true;
                int index = i * 10;
                IItem? itemObj = item2 as IItem;
                IClothing? clothingObj = item2 as IClothing;
                objs[index] = pk2;
                objs[index + 1] = itemObj != null ? itemObj.Item.ToString("N") : DBNull.Value;
                if (item2 is IItemJar jarObj)
                {
                    objs[index + 2] = jarObj.X;
                    objs[index + 3] = jarObj.Y;
                    objs[index + 4] = jarObj.Rotation % 4;
                    objs[index + 5] = jarObj.Page.ToString();
                }
                else
                    objs[index + 2] = objs[index + 3] = objs[index + 4] = objs[index + 5] = DBNull.Value;

                objs[index + 6] = clothingObj != null ? clothingObj.Item.ToString("N") : DBNull.Value;
                objs[index + 7] = item2 is IAssetRedirect redirObj ? redirObj.RedirectType.ToString() : DBNull.Value;
                objs[index + 8] = itemObj != null ? itemObj.Amount : DBNull.Value;
                objs[index + 9] = itemObj != null
                    ? itemObj.State
                    : clothingObj != null
                        ? clothingObj.State
                        : DBNull.Value;
                F.AppendPropertyList(builder, index, 10);
            }
            builder.Append(';');
            if (one)
                await Sql.NonQueryAsync(builder.ToString(), objs, token).ConfigureAwait(false);
            builder.Clear();
        }

        if (item.UnlockRequirements is { Length: > 0 })
        {
            builder.Append($"INSERT INTO `{TABLE_UNLOCK_REQUIREMENTS}` ({SqlTypes.ColumnList(
                COLUMN_EXT_PK, UnlockRequirement.COLUMN_JSON)}) VALUES ");
            objs = new object[item.UnlockRequirements.Length * 2];
            using MemoryStream str = new MemoryStream(48);
            for (int i = 0; i < item.UnlockRequirements.Length; ++i)
            {
                UnlockRequirement req = item.UnlockRequirements[i];
                if (i != 0)
                    str.Seek(0L, SeekOrigin.Begin);
                Utf8JsonWriter writer = new Utf8JsonWriter(str, JsonEx.condensedWriterOptions);
                UnlockRequirement.Write(writer, req);
                writer.Dispose();
                string json = System.Text.Encoding.UTF8.GetString(str.GetBuffer(), 0, checked((int)str.Position));
                int index = i * 2;
                objs[index] = pk2;
                objs[index + 1] = json;
                F.AppendPropertyList(builder, index, 2);
            }
            builder.Append("; ");
        }

        if (item.Skillsets is { Length: > 0 })
        {
            builder.Append($"INSERT INTO `{TABLE_SKILLSETS}` ({SqlTypes.ColumnList(
                COLUMN_EXT_PK, Skillset.COLUMN_SKILL, Skillset.COLUMN_LEVEL)}) VALUES ");
            objs = new object[item.Skillsets.Length * 3];
            for (int i = 0; i < item.Skillsets.Length; ++i)
            {
                Skillset set = item.Skillsets[i];
                if (set.Speciality is not EPlayerSpeciality.DEFENSE and not EPlayerSpeciality.OFFENSE and not EPlayerSpeciality.SUPPORT)
                    continue;
                int index = i * 3;
                objs[index] = pk2;
                objs[index + 1] = set.Speciality switch
                    { EPlayerSpeciality.DEFENSE => set.Defense.ToString(), EPlayerSpeciality.OFFENSE => set.Offense.ToString(), _ => set.Support.ToString() };
                objs[index + 2] = set.Level;
                F.AppendPropertyList(builder, index, 3);
            }
            builder.Append("; ");
        }
        if (item.FactionBlacklist is { Length: > 0 })
        {
            builder.Append($"INSERT INTO `{TABLE_FACTION_BLACKLIST}` ({SqlTypes.ColumnList(
                COLUMN_EXT_PK, COLUMN_FACTION)}) VALUES ");
            objs = new object[item.FactionBlacklist.Length * 2];
            for (int i = 0; i < item.FactionBlacklist.Length; ++i)
            {
                PrimaryKey f = item.FactionBlacklist[i];
                if (!f.IsValid)
                    continue;
                int index = i * 2;
                objs[index] = pk2;
                objs[index + 1] = f.Key;
                F.AppendPropertyList(builder, index, 2);
            }
            builder.Append("; ");
        }
        if (item.SignText is { Count: > 0 })
        {
            builder.Append($"INSERT INTO `{TABLE_SIGN_TEXT}` ({SqlTypes.ColumnList(
                COLUMN_EXT_PK, F.COLUMN_LANGUAGE, F.COLUMN_VALUE)}) VALUES ");
            objs = new object[item.SignText.Count * 3];
            int i = 0;
            foreach (KeyValuePair<string, string> pair in item.SignText)
            {
                int index = i * 3;
                objs[index] = pk2;
                objs[index + 1] = pair.Key;
                objs[index + 2] = pair.Value;
                F.AppendPropertyList(builder, index, 3);
                ++i;
            }
            builder.Append(';');
        }

        if (builder.Length > 0)
        {
            await Sql.NonQueryAsync(builder.ToString(), objs, token).ConfigureAwait(false);
            builder.Clear();
        }
    }
    [Obsolete]
    protected override async Task<Kit?> DownloadItem(PrimaryKey pk, CancellationToken token = default)
    {
        Kit? obj = null;
        if (!pk.IsValid)
            throw new ArgumentException("Primary key is not valid.", nameof(pk));
        int pk2 = pk;
        object[] pkObjs = { pk2 };
        await Sql.QueryAsync(
            $"SELECT {SqlTypes.ColumnList(COLUMN_KIT_ID, COLUMN_FACTION, COLUMN_CLASS, COLUMN_BRANCH,
                COLUMN_TYPE, COLUMN_REQUEST_COOLDOWN, COLUMN_TEAM_LIMIT, COLUMN_SEASON, COLUMN_DISABLED, COLUMN_WEAPONS,
                COLUMN_SQUAD_LEVEL)} FROM `{TABLE_MAIN}` " +
            $"WHERE `{COLUMN_PK}`=@0 LIMIT 1;", pkObjs, reader =>
            {
                obj = ReadKit(reader, -1);
            }, token).ConfigureAwait(false);
        if (obj != null)
        {
            List<IKitItem> items = new List<IKitItem>(16);
            await Sql.QueryAsync(
                $"SELECT {SqlTypes.ColumnList(
                    COLUMN_ITEM_GUID, COLUMN_ITEM_X, COLUMN_ITEM_Y, COLUMN_ITEM_ROTATION, COLUMN_ITEM_PAGE,
                    COLUMN_ITEM_CLOTHING, COLUMN_ITEM_REDIRECT, COLUMN_ITEM_AMOUNT, COLUMN_ITEM_METADATA)} " +
                $"FROM `{TABLE_ITEMS}` WHERE `{COLUMN_EXT_PK}`=@0;", pkObjs, reader =>
                {
                    items.Add(ReadItem(reader, -1));
                }, token).ConfigureAwait(false);
            obj.Items = items.ToArray();
            items = null!;

            await Sql.QueryAsync($"SELECT {SqlTypes.ColumnList(UnlockRequirement.COLUMN_JSON)} " +
                                 $"FROM `{TABLE_UNLOCK_REQUIREMENTS}` WHERE `{COLUMN_EXT_PK}`=@0;", pkObjs, reader =>
            {
                UnlockRequirement? req = UnlockRequirement.Read(reader, -1);
                if (req == null)
                    throw new FormatException("Invalid unlock requirement from JSON data \"" + reader.GetString(0) + "\".");
                UnlockRequirement[]? arr = obj.UnlockRequirements;
                Util.AddToArray(ref arr, req);
                obj.UnlockRequirements = arr!;
            }, token).ConfigureAwait(false);
            await Sql.QueryAsync($"SELECT {SqlTypes.ColumnList(Skillset.COLUMN_SKILL, Skillset.COLUMN_LEVEL)} " +
                                 $"FROM `{TABLE_SKILLSETS}` WHERE `{COLUMN_EXT_PK}`=@0;", pkObjs, reader =>
            {
                Skillset set = Skillset.Read(reader);
                Skillset[]? arr = obj.Skillsets;
                Util.AddToArray(ref arr, set);
                obj.Skillsets = arr!;
            }, token).ConfigureAwait(false);
            await Sql.QueryAsync($"SELECT {SqlTypes.ColumnList(COLUMN_FACTION)} " +
                                 $"FROM `{TABLE_FACTION_BLACKLIST}`;", null, reader =>
            {
                int faction = reader.GetInt32(0);
                PrimaryKey[]? arr = obj.FactionBlacklist;
                Util.AddToArray(ref arr, faction);
                obj.FactionBlacklist = arr!;
            }, token).ConfigureAwait(false);
            await Sql.QueryAsync($"SELECT {SqlTypes.ColumnList(F.COLUMN_LANGUAGE, F.COLUMN_VALUE)} " +
                                 $"FROM `{TABLE_SIGN_TEXT}` WHERE `{COLUMN_EXT_PK}`=@0;", pkObjs, reader =>
            {
                F.ReadToTranslationList(reader, obj.SignText ??= new TranslationList(1), -1);
            }, token).ConfigureAwait(false);
        }
        return obj;
    }
    [Obsolete]
    protected override async Task<Kit[]> DownloadAllItems(CancellationToken token = default)
    {
        List<Kit> list = new List<Kit>(32);
        await Sql.QueryAsync($"SELECT {SqlTypes.ColumnList(
            COLUMN_PK, COLUMN_KIT_ID, COLUMN_FACTION, COLUMN_CLASS, COLUMN_BRANCH, COLUMN_TYPE,
            COLUMN_REQUEST_COOLDOWN, COLUMN_TEAM_LIMIT, COLUMN_SEASON, COLUMN_DISABLED, COLUMN_WEAPONS, COLUMN_SQUAD_LEVEL)}" +
                             $" FROM `{TABLE_MAIN}`;",
            null, reader =>
            {
                list.Add(ReadKit(reader));
            }, token).ConfigureAwait(false);

        if (list.Count == 0)
            return list.ToArray();

        List<KeyValuePair<int, IKitItem>> tempList = new List<KeyValuePair<int, IKitItem>>(list.Count * 16);
        await Sql.QueryAsync(
            $"SELECT {SqlTypes.ColumnList(
                COLUMN_EXT_PK, COLUMN_ITEM_GUID, COLUMN_ITEM_X, COLUMN_ITEM_Y, COLUMN_ITEM_ROTATION, COLUMN_ITEM_PAGE,
                COLUMN_ITEM_CLOTHING, COLUMN_ITEM_REDIRECT, COLUMN_ITEM_AMOUNT, COLUMN_ITEM_METADATA)} " +
            $"FROM `{TABLE_ITEMS}`;", null, reader =>
            {
                int pk = reader.GetInt32(0);
                IKitItem item = ReadItem(reader);
                tempList.Add(new KeyValuePair<int, IKitItem>(pk, item));
            }, token).ConfigureAwait(false);

        int[] ct = new int[list.Count];
        for (int i = 0; i < tempList.Count; ++i)
        {
            int pk = tempList[i].Key;
            for (int j = 0; j < list.Count; ++j)
            {
                if (list[j].PrimaryKey.Key == pk)
                {
                    ++ct[j];
                    break;
                }
            }
        }

        for (int i = 0; i < list.Count; ++i)
            list[i].Items = new IKitItem[ct[i]];

        for (int i = 0; i < tempList.Count; ++i)
        {
            int pk = tempList[i].Key;
            for (int j = 0; j < list.Count; ++j)
            {
                if (list[j].PrimaryKey.Key == pk)
                {
                    Kit kit = list[j];
                    kit.Items[kit.Items.Length - ct[j]--] = tempList[i].Value;
                    break;
                }
            }
        }

        tempList = null!;
        await Sql.QueryAsync($"SELECT {SqlTypes.ColumnList(COLUMN_EXT_PK, UnlockRequirement.COLUMN_JSON)} " +
                             $"FROM `{TABLE_UNLOCK_REQUIREMENTS}`;", null, reader =>
        {
            int pk = reader.GetInt32(0);
            for (int i = 0; i < list.Count; ++i)
            {
                if (list[i].PrimaryKey.Key == pk)
                {
                    UnlockRequirement? req = UnlockRequirement.Read(reader);
                    if (req != null)
                    {
                        UnlockRequirement[]? arr = list[i].UnlockRequirements;
                        Util.AddToArray(ref arr, req);
                        list[i].UnlockRequirements = arr!;
                        break;
                    }
                    throw new FormatException("Invalid unlock requirement from JSON data \"" + reader.GetString(1) + "\".");
                }
            }
        }, token).ConfigureAwait(false);
        await Sql.QueryAsync($"SELECT {SqlTypes.ColumnList(COLUMN_EXT_PK, Skillset.COLUMN_SKILL, Skillset.COLUMN_LEVEL)} " +
                             $"FROM `{TABLE_SKILLSETS}`;", null, reader =>
        {
            int pk = reader.GetInt32(0);
            for (int i = 0; i < list.Count; ++i)
            {
                if (list[i].PrimaryKey.Key == pk)
                {
                    Skillset set = Skillset.Read(reader);
                    Skillset[]? arr = list[i].Skillsets;
                    Util.AddToArray(ref arr, set);
                    list[i].Skillsets = arr!;
                    break;
                }
            }
        }, token).ConfigureAwait(false);
        await Sql.QueryAsync($"SELECT {SqlTypes.ColumnList(COLUMN_EXT_PK, COLUMN_FACTION)} " +
                             $"FROM `{TABLE_FACTION_BLACKLIST}`;", null, reader =>
        {
            int pk = reader.GetInt32(0);
            for (int i = 0; i < list.Count; ++i)
            {
                if (list[i].PrimaryKey.Key == pk)
                {
                    int faction = reader.GetInt32(1);
                    PrimaryKey[]? arr = list[i].FactionBlacklist;
                    Util.AddToArray(ref arr, faction);
                    list[i].FactionBlacklist = arr!;
                    break;
                }
            }
        }, token).ConfigureAwait(false);
        await Sql.QueryAsync($"SELECT {SqlTypes.ColumnList(COLUMN_EXT_PK, F.COLUMN_LANGUAGE, F.COLUMN_VALUE)} " +
                             $"FROM `{TABLE_SIGN_TEXT}`;", null, reader =>
        {
            int pk = reader.GetInt32(0);
            for (int i = 0; i < list.Count; ++i)
            {
                if (list[i].PrimaryKey.Key == pk)
                {
                    F.ReadToTranslationList(reader, list[i].SignText ??= new TranslationList(1));
                    break;
                }
            }
        }, token).ConfigureAwait(false);
        
        return list.ToArray();
    }
    /// <exception cref="FormatException"/>
    private static Kit ReadKit(MySqlDataReader reader, int colOffset = 0)
    {
        string id = reader.GetString(colOffset + 1);
        if (id.Length is < 1 or > KitEx.KitNameMaxCharLimit)
            throw new FormatException("Invalid kit ID: \"" + id + "\".");
        Class @class = reader.ReadStringEnum<Class>(colOffset + 3) ?? throw new FormatException("Invalid class: \"" + reader.GetString(colOffset + 3) + "\".");
        return new Kit
        {
            Type = reader.ReadStringEnum<KitType>(colOffset + 5) ?? throw new FormatException("Invalid type: \"" + reader.GetString(colOffset + 5) + "\"."),
            Branch = reader.ReadStringEnum<Branch>(colOffset + 4) ?? throw new FormatException("Invalid branch: \"" + reader.GetString(colOffset + 4) + "\"."),
            SquadLevel = reader.IsDBNull(colOffset + 11)
                ? SquadLevel.Member
                : reader.ReadStringEnum<SquadLevel>(colOffset + 11) ?? throw new FormatException("Invalid squad level: \"" + reader.GetString(colOffset + 11) + "\"."),
            PrimaryKey = reader.GetInt32(colOffset + 0),
            Id = id,
            FactionKey = reader.IsDBNull(colOffset + 2) ? -1 : reader.GetInt32(colOffset + 2),
            Class = @class,
            RequestCooldown = reader.IsDBNull(colOffset + 6) ? 0f : reader.GetFloat(colOffset + 6),
            TeamLimit = reader.IsDBNull(colOffset + 7) ? GetDefaultTeamLimit(@class) : reader.GetFloat(colOffset + 7),
            Season = reader.IsDBNull(colOffset + 8) ? UCWarfare.Season : reader.GetByte(colOffset + 8),
            Disabled = !reader.IsDBNull(colOffset + 9) && reader.GetBoolean(colOffset + 9),
            WeaponText = reader.IsDBNull(colOffset + 10) ? null : reader.GetString(colOffset + 10)
        };
    }
    /// <exception cref="FormatException"/>
    private static IKitItem ReadItem(MySqlDataReader reader, int colOffset = 0)
    {
        IKitItem item;
        bool hasGuid = !reader.IsDBNull(colOffset + 1);
        bool hasRedir = !reader.IsDBNull(colOffset + 7);
        bool hasPageStuff = !(reader.IsDBNull(colOffset + 2) || reader.IsDBNull(colOffset + 3) || reader.IsDBNull(colOffset + 5));
        bool hasClothing = !reader.IsDBNull(colOffset + 6);
        if (!hasGuid && !hasRedir)
            throw new FormatException("Item row must either have a GUID or a redirect type.");
        if (!hasPageStuff && !hasClothing)
            throw new FormatException("Item row must either have jar information or a clothing type.");
        if (hasGuid)
        {
            Guid? guid = reader.ReadGuidString(colOffset + 1);
            if (!guid.HasValue)
            {
                if (hasRedir)
                {
                    L.LogWarning("Invalid GUID in item row: \"" + reader.GetString(colOffset + 1) + "\", falling back to redirect type.");
                    goto redir;
                }
                throw new FormatException("Invalid GUID in item row: \"" + reader.GetString(colOffset + 1) + "\".");
            }
            if (hasPageStuff)
            {
                byte x = reader.GetByte(colOffset + 2),
                     y = reader.GetByte(colOffset + 3),
                     rot = reader.IsDBNull(colOffset + 4) ? (byte)0 : reader.GetByte(colOffset + 4),
                     amt = reader.IsDBNull(colOffset + 8) ? (byte)0 : reader.GetByte(colOffset + 8);
                rot %= 4;
                Page? pg = reader.ReadStringEnum<Page>(colOffset + 5);
                if (!pg.HasValue)
                    throw new FormatException("Invalid page in item row: \"" + reader.GetString(colOffset + 5) + "\".");
                item = new PageItem(guid.Value, x, y, rot, reader.IsDBNull(colOffset + 9) ? Array.Empty<byte>() : reader.ReadByteArray(colOffset + 9), amt, pg.Value);
            }
            else
            {
                ClothingType? type = reader.ReadStringEnum<ClothingType>(colOffset + 6);
                if (!type.HasValue)
                    throw new FormatException("Invalid clothing type in item row: \"" + reader.GetString(colOffset + 6) + "\".");
                item = new ClothingItem(guid.Value, type.Value, reader.IsDBNull(colOffset + 9) ? Array.Empty<byte>() : reader.ReadByteArray(colOffset + 9));
            }

            return item;
        }
        redir:
        RedirectType? redirect = reader.ReadStringEnum<RedirectType>(colOffset + 7);
        if (!redirect.HasValue)
            throw new FormatException("Invalid redirect in item row: \"" + reader.GetString(colOffset + 7) + "\".");
        if (hasPageStuff)
        {
            byte x = reader.GetByte(colOffset + 2),
                y = reader.GetByte(colOffset + 3),
                rot = reader.IsDBNull(colOffset + 4) ? (byte)0 : reader.GetByte(colOffset + 4);
            rot %= 4;
            Page? pg = reader.ReadStringEnum<Page>(colOffset + 5);
            if (!pg.HasValue)
                throw new FormatException("Invalid page in item row: \"" + reader.GetString(colOffset + 5) + "\".");
            item = new AssetRedirectItem(redirect.Value, x, y, rot, pg.Value);
        }
        else
        {
            ClothingType? type = reader.ReadStringEnum<ClothingType>(colOffset + 6);
            if (!type.HasValue)
                throw new FormatException("Invalid clothing type in item row: \"" + reader.GetString(colOffset + 6) + "\".");
            item = new AssetRedirectClothing(redirect.Value, type.Value);
        }

        return item;
    }
    #endregion
}

public delegate void KitAccessCallback(SqlItem<Kit> kit, ulong player);
public delegate void KitChanged(UCPlayer player, SqlItem<Kit>? oldKit, SqlItem<Kit>? newKit);
public class KitManagerOld : BaseReloadSingleton
{
    internal readonly SemaphoreSlim _threadLocker = new SemaphoreSlim(1, 5);
    public static event KitAccessCallback? OnKitAccessChanged;
    public Dictionary<int, KitOld> Kits = new Dictionary<int, KitOld>(256);
    private static KitManagerOld _singleton;
    public static bool Loaded => _singleton.IsLoaded();
    public KitManagerOld() : base("kits") { }
    public override void Load()
    {
        PlayerLife.OnPreDeath += PlayerLife_OnPreDeath;
        Kits.Clear();
    }
    public override void Reload()
    {
        Task.Run(async () =>
        {
            await ReloadKits();
            await UCWarfare.ToUpdate();
            if (RequestSigns.Loaded)
            {
                RequestSigns.UpdateAllSigns();
            }
            if (!KitExists(TeamManager.Team1UnarmedKit, out _))
                L.LogError("Team 1's unarmed kit, \"" + TeamManager.Team1UnarmedKit + "\", was not found, it should be added to \"" + Data.Paths.KitsStorage + "kits.json\".");
            if (!KitExists(TeamManager.Team2UnarmedKit, out _))
                L.LogError("Team 2's unarmed kit, \"" + TeamManager.Team2UnarmedKit + "\", was not found, it should be added to \"" + Data.Paths.KitsStorage + "kits.json\".");
            if (!KitExists(TeamManager.DefaultKit, out _))
                L.LogError("The default kit, \"" + TeamManager.DefaultKit + "\", was not found, it should be added to \"" + Data.Paths.KitsStorage + "kits.json\".");
        }).ConfigureAwait(false);
    }
    internal static string Search(string searchTerm)
    {
        KitManager singleton = GetSingleton();
        StringBuilder sb = new StringBuilder();
        singleton._threadLocker.Wait();
        try
        {
            int c = 0;
            foreach (KitOld v in singleton.Kits.Values)
            {
                if (v.GetDisplayName().IndexOf(searchTerm, StringComparison.OrdinalIgnoreCase) != -1)
                {
                    if (c != 0)
                        sb.Append(", ");
                    sb.Append(v.Name);
                    if (++c > 12) break;
                }
            }
        }
        finally
        {
            singleton._threadLocker.Release();
        }
        return sb.ToString();
    }
    public override void Unload()
    {
        _isLoaded = false;
        _singleton = null!;
        PlayerLife.OnPreDeath -= PlayerLife_OnPreDeath;
    }
    public static void ResupplyKit(UCPlayer player, KitOld kit, bool ignoreAmmoBags = false)
    {
#if DEBUG
        using IDisposable profiler = ProfilingUtils.StartTracking();
#endif
        List<ItemJar> nonKitItems = new List<ItemJar>();

        for (byte page = 0; page < PlayerInventory.PAGES - 1; page++)
        {
            if (page == PlayerInventory.AREA)
                continue;

            byte count = player.Player.inventory.getItemCount(page);

            for (byte index = 0; index < count; index++)
            {
                ItemJar jar = player.Player.inventory.getItem(page, 0);
                if (Assets.find(EAssetType.ITEM, jar.item.id) is not ItemAsset asset) continue;
                if (!kit.HasItemOfID(asset.GUID) && Whitelister.IsWhitelisted(asset.GUID, out _))
                {
                    nonKitItems.Add(jar);
                }
                player.Player.inventory.removeItem(page, 0);
            }
        }

        for (int i = 0; i < kit.Clothes.Count; i++)
        {
            ClothingItem clothing = kit.Clothes[i];
            if (Assets.find(clothing.Item) is ItemAsset asset)
            {
                ushort old = 0;
                switch (clothing.Type)
                {
                    case ClothingType.Glasses:
                        if (player.Player.clothing.glasses != asset.id)
                        {
                            old = player.Player.clothing.glasses;
                            player.Player.clothing.askWearGlasses(asset.id, 100, asset.getState(true), true);
                        }
                        break;
                    case ClothingType.Hat:
                        if (player.Player.clothing.hat != asset.id)
                        {
                            old = player.Player.clothing.hat;
                            player.Player.clothing.askWearHat(asset.id, 100, asset.getState(true), true);
                        }
                        break;
                    case ClothingType.Backpack:
                        if (player.Player.clothing.backpack != asset.id)
                        {
                            old = player.Player.clothing.backpack;
                            player.Player.clothing.askWearBackpack(asset.id, 100, asset.getState(true), true);
                        }
                        break;
                    case ClothingType.Mask:
                        if (player.Player.clothing.mask != asset.id)
                        {
                            old = player.Player.clothing.mask;
                            player.Player.clothing.askWearMask(asset.id, 100, asset.getState(true), true);
                        }
                        break;
                    case ClothingType.Pants:
                        if (player.Player.clothing.pants != asset.id)
                        {
                            old = player.Player.clothing.pants;
                            player.Player.clothing.askWearPants(asset.id, 100, asset.getState(true), true);
                        }
                        break;
                    case ClothingType.Shirt:
                        if (player.Player.clothing.shirt != asset.id)
                        {
                            old = player.Player.clothing.shirt;
                            player.Player.clothing.askWearShirt(asset.id, 100, asset.getState(true), true);
                        }
                        break;
                    case ClothingType.Vest:
                        if (player.Player.clothing.vest != asset.id)
                        {
                            old = player.Player.clothing.vest;
                            player.Player.clothing.askWearVest(asset.id, 100, asset.getState(true), true);
                        }
                        break;
                }
                if (old != 0)
                    player.Player.inventory.removeItem(2, 0);
            }
        }
        foreach (PageItem i in kit.Items)
        {
            if (ignoreAmmoBags && Gamemode.Config.BarricadeAmmoBag.MatchGuid(i.Item))
                continue;
            if (Assets.find(i.Item) is ItemAsset itemasset)
            {
                Item item = new Item(itemasset.id, i.Amount, 100, Util.CloneBytes(i.State));

                if (!player.Player.inventory.tryAddItem(item, i.X, i.Y, i.Page, i.Rotation))
                    player.Player.inventory.tryAddItem(item, true);
            }
        }

        foreach (ItemJar jar in nonKitItems)
        {
            player.Player.inventory.tryAddItem(jar.item, true);
        }
    }
    public static bool TryGiveUnarmedKit(UCPlayer player)
    {
#if DEBUG
        using IDisposable profiler = ProfilingUtils.StartTracking();
#endif
        string unarmedKit = string.Empty;
        if (player.IsTeam1)
            unarmedKit = TeamManager.Team1UnarmedKit;
        else if (player.IsTeam2)
            unarmedKit = TeamManager.Team2UnarmedKit;

        if (KitExists(unarmedKit, out KitOld kit))
        {
            GiveKit(player, kit);
            return true;
        }
        return false;
    }
    public static bool TryGiveRiflemanKit(UCPlayer player)
    {
#if DEBUG
        using IDisposable profiler = ProfilingUtils.StartTracking();
#endif
        ulong t = player.GetTeam();
        KitOld rifleman = GetKitsWhere(k =>
                !k.Disabled &&
                k.Team == t &&
                k.Class == Class.Rifleman &&
                !k.IsPremium &&
                !k.IsLoadout &&
                k.TeamLimit == 1 &&
                k.UnlockRequirements.Length == 0
            ).FirstOrDefault();

        if (rifleman != null)
        {
            GiveKit(player, rifleman);
            return true;
        }
        return false;
    }
    public static void UpdateText(KitOld kit, string text, string language = L.DEFAULT, bool updateSigns = true)
    {
#if DEBUG
        using IDisposable profiler = ProfilingUtils.StartTracking();
#endif
        kit.SignTexts.Remove(language);
        kit.SignTexts.Add(language, text);
        if (updateSigns && UCWarfare.IsMainThread)
            UpdateSigns(kit);
    }
    public static async Task<char> GetLoadoutCharacter(ulong playerId)
    {
        char let = 'a';
        await Data.DatabaseManager.QueryAsync("SELECT `InternalName` FROM `kit_data` WHERE `InternalName` LIKE @0 ORDER BY `InternalName`;", new object[1]
        {
            playerId.ToString() + "_%"
        }, R =>
        {
            string name = R.GetString(0);
            if (name.Length < 19)
                return;
            char let2 = name[18];
            if (let2 == let)
                let++;
        });
        return let;
    }
    internal async Task<(KitOld?, int)> CreateLoadout(ulong fromPlayer, ulong player, ulong team, Class @class, string displayName)
    {
#if DEBUG
        using IDisposable profiler = ProfilingUtils.StartTracking();
#endif
        char let = await GetLoadoutCharacter(player);
        string loadoutName = player.ToString() + "_" + let;

        if (KitExists(loadoutName, out _))
            return (null, 555);
        else
        {
            List<PageItem> items;
            List<ClothingItem> clothes;
            if (team is 1 or 2 && KitExists(team == 1 ? TeamManager.Team1UnarmedKit : TeamManager.Team2UnarmedKit, out KitOld unarmedKit))
            {
                items = unarmedKit.Items.ToList();
                clothes = unarmedKit.Clothes.ToList();
            }
            else
            {
                items = new List<PageItem>(0);
                clothes = new List<ClothingItem>(0);
            }

            KitOld loadout = new KitOld(loadoutName, items, clothes);

            loadout.IsLoadout = true;
            loadout.Team = team;
            loadout.Class = @class;
            if (@class == Class.Pilot)
                loadout.Branch = Branch.Airforce;
            else if (@class == Class.Crewman)
                loadout.Branch = Branch.Armor;
            else
                loadout.Branch = Branch.Infantry;

            if (@class == Class.HAT)
                loadout.TeamLimit = 0.1F;
            await UCWarfare.ToUpdate();
            UpdateText(loadout, displayName);

            await AddKit(loadout);

            ActionLogger.Add(EActionLogType.CREATE_KIT, loadoutName, fromPlayer);

            return (loadout, 0);
        }
    }
    internal static void InvokeKitCreated(KitOld kit)
    {
        OnKitCreated?.Invoke(kit);
        if (UCWarfare.Config.EnableSync)
        {
            KitSync.OnKitUpdated(kit.Name);
        }
    }

    internal static void InvokeKitDeleted(KitOld kit)
    {
        OnKitDeleted?.Invoke(kit);
        if (UCWarfare.Config.EnableSync)
        {
            KitSync.OnKitDeleted(kit.Name);
        }
    }

    internal static void InvokeKitUpdated(KitOld kit)
    {
        OnKitUpdated?.Invoke(kit);
        if (UCWarfare.Config.EnableSync)
        {
            KitSync.OnKitUpdated(kit.Name);
        }
    }

    internal static void InvokeKitAccessUpdated(KitOld kit, ulong player)
    {
        OnKitAccessChanged?.Invoke(kit, player);
        if (UCWarfare.Config.EnableSync)
        {
            KitSync.OnAccessChanged(player);
        }
    }
}

public static class KitEx
{
    public const int BranchMaxCharLimit = 16;
    public const int ClothingMaxCharLimit = 16;
    public const int ClassMaxCharLimit = 20;
    public const int TypeMaxCharLimit = 16;
    public const int RedirectTypeCharLimit = 16;
    public const int SquadLevelMaxCharLimit = 16;
    public const int KitNameMaxCharLimit = 25;
    public const int WeaponTextMaxCharLimit = 50;
    public const int SignTextMaxCharLimit = 50;
    public const int MaxStateArrayLimit = 18;
    public static bool HasItemOfID(this Kit kit, Guid guid)
    {
        for (int i = 0; i < kit.Items.Length; ++i)
        {
            if (kit.Items[i] is IItem item)
            {
                if (item.Item == guid)
                    return true;
            }
            else if (kit.Items[i] is IClothing clothing)
            {
                if (clothing.Item == guid)
                    return true;
            }
        }
        return false;
    }

    public static bool IsLimited(this KitOld kit, out int currentPlayers, out int allowedPlayers, ulong team, bool requireCounts = false)
    {
#if DEBUG
        using IDisposable profiler = ProfilingUtils.StartTracking();
#endif
        ulong Team = team == 1 || team == 2 ? team : kit.Team;
        currentPlayers = 0;
        allowedPlayers = 24;
        if (!requireCounts && (kit.IsPremium || kit.TeamLimit >= 1f))
            return false;
        IEnumerable<UCPlayer> friendlyPlayers = Team == 0 ? PlayerManager.OnlinePlayers : PlayerManager.OnlinePlayers.Where(k => k.GetTeam() == Team);
        allowedPlayers = (int)Math.Ceiling(kit.TeamLimit * friendlyPlayers.Count());
        currentPlayers = friendlyPlayers.Count(k => k.KitName == kit.Name);
        if (kit.IsPremium || kit.TeamLimit >= 1f)
            return false;
        return currentPlayers + 1 > allowedPlayers;
    }

    public static bool IsClassLimited(this KitOld kit, out int currentPlayers, out int allowedPlayers, ulong team, bool requireCounts = false)
    {
#if DEBUG
        using IDisposable profiler = ProfilingUtils.StartTracking();
#endif
        ulong Team = team == 1 || team == 2 ? team : kit.Team;
        currentPlayers = 0;
        allowedPlayers = 24;
        if (!requireCounts && (kit.IsPremium || kit.TeamLimit >= 1f))
            return false;
        IEnumerable<UCPlayer> friendlyPlayers = Team == 0 ? PlayerManager.OnlinePlayers : PlayerManager.OnlinePlayers.Where(k => k.GetTeam() == Team);
        allowedPlayers = (int)Math.Ceiling(kit.TeamLimit * friendlyPlayers.Count());
        currentPlayers = friendlyPlayers.Count(k => k.KitClass == kit.Class);
        if (kit.IsPremium || kit.TeamLimit >= 1f)
            return false;
        return currentPlayers + 1 > allowedPlayers;
    }
    public static class NetCalls
    {
        public static readonly NetCall<ulong, ulong, string, KitAccessType, bool> RequestSetKitAccess = new NetCall<ulong, ulong, string, KitAccessType, bool>(ReceiveSetKitAccess);
        public static readonly NetCall<ulong, ulong, string[], KitAccessType, bool> RequestSetKitsAccess = new NetCall<ulong, ulong, string[], KitAccessType, bool>(ReceiveSetKitsAccess);
        public static readonly NetCallRaw<Kit?> CreateKit = new NetCallRaw<Kit?>(ReceiveCreateKit, Kit.Read, Kit.Write);
        public static readonly NetCall<string> RequestKitClass = new NetCall<string>(ReceiveRequestKitClass);
        public static readonly NetCall<string> RequestKit = new NetCall<string>(ReceiveKitRequest);
        public static readonly NetCallRaw<string[]> RequestKits = new NetCallRaw<string[]>(ReceiveKitsRequest, null, null);
        public static readonly NetCall<ulong, ulong, byte, Class, string> RequestCreateLoadout = new NetCall<ulong, ulong, byte, Class, string>(ReceiveCreateLoadoutRequest);
        public static readonly NetCall<string, ulong> RequestKitAccess = new NetCall<string, ulong>(ReceiveKitAccessRequest);
        public static readonly NetCall<string[], ulong> RequestKitsAccess = new NetCall<string[], ulong>(ReceiveKitsAccessRequest);


        public static readonly NetCall<string, Class, string> SendKitClass = new NetCall<string, Class, string>(1114);
        public static readonly NetCallRaw<Kit?> SendKit = new NetCallRaw<Kit?>(1117, Kit.Read, Kit.Write);
        public static readonly NetCallRaw<Kit?[]> SendKits = new NetCallRaw<Kit?[]>(1118, Kit.ReadMany, Kit.WriteMany);
        public static readonly NetCall<string, int> SendAckCreateLoadout = new NetCall<string, int>(1111);
        public static readonly NetCall<bool> SendAckSetKitAccess = new NetCall<bool>(1101);
        public static readonly NetCall<bool[]> SendAckSetKitsAccess = new NetCall<bool[]>(1133);
        public static readonly NetCall<byte, bool> SendKitAccess = new NetCall<byte, bool>(1135);
        public static readonly NetCall<byte, byte[]> SendKitsAccess = new NetCall<byte, byte[]>(1137);

        [NetCall(ENetCall.FROM_SERVER, 1100)]
        internal static Task ReceiveSetKitAccess(MessageContext context, ulong admin, ulong player, string kit, KitAccessType type, bool state)
        {
            if (KitManager.KitExists(kit, out KitOld k))
            {
                Task<bool> t = state ? KitManager.GiveAccess(k, player, type) : KitManager.RemoveAccess(k, player);
                if (state)
                    ActionLogger.Add(EActionLogType.CHANGE_KIT_ACCESS, player.ToString(Data.Locale) + " GIVEN ACCESS TO " + kit + ", REASON: " + type.ToString(), admin);
                else
                    ActionLogger.Add(EActionLogType.CHANGE_KIT_ACCESS, player.ToString(Data.Locale) + " DENIED ACCESS TO " + kit, admin);
                context.Reply(SendAckSetKitAccess, true);
                KitManager.UpdateSigns(k);
                return t;
            }
            context.Reply(SendAckSetKitAccess, false);
            return Task.CompletedTask;
        }
        [NetCall(ENetCall.FROM_SERVER, 1132)]
        internal static async Task ReceiveSetKitsAccess(MessageContext context, ulong admin, ulong player, string[] kits, KitAccessType type, bool state)
        {
            bool[] successes = new bool[kits.Length];
            for (int i = 0; i < kits.Length; ++i)
            {
                string kit = kits[i];
                if (KitManager.KitExists(kit, out KitOld k))
                {
                    Task<bool> t = state ? KitManager.GiveAccess(k, player, type) : KitManager.RemoveAccess(k, player);
                    await t;
                    if (state)
                        ActionLogger.Add(EActionLogType.CHANGE_KIT_ACCESS, player.ToString(Data.Locale) + " GIVEN ACCESS TO " + kit + ", REASON: " + type.ToString(), admin);
                    else
                        ActionLogger.Add(EActionLogType.CHANGE_KIT_ACCESS, player.ToString(Data.Locale) + " DENIED ACCESS TO " + kit, admin);
                    successes[i] = true;
                }
            }
            context.Reply(SendAckSetKitsAccess, successes);
        }
        [NetCall(ENetCall.FROM_SERVER, 1134)]
        private static async Task ReceiveKitAccessRequest(MessageContext context, string kit, ulong player)
        {
            if (!KitManager.Loaded)
                context.Reply(SendKitAccess, (byte)1, false);
            else if (KitManager.KitExists(kit, out KitOld k))
                context.Reply(SendKitAccess, (byte)0, await KitManager.HasAccess(kit, player));
            else
                context.Reply(SendKitAccess, (byte)2, false);
        }

        [NetCall(ENetCall.FROM_SERVER, 1136)]
        private static async Task ReceiveKitsAccessRequest(MessageContext context, string[] kits, ulong player)
        {
            byte[] outp = new byte[kits.Length];
            if (!KitManager.Loaded)
                context.Reply(SendKitsAccess, (byte)1, outp);
            else
            {
                for (int i = 0; i < kits.Length; ++i)
                {
                    if (KitManager.KitExists(kits[i], out KitOld k))
                        outp[i] = await KitManager.HasAccess(k, player) ? (byte)2 : (byte)1;
                    else outp[i] = 3;
                }
            }
            context.Reply(SendKitsAccess, (byte)0, outp);
        }

        [NetCall(ENetCall.FROM_SERVER, 1109)]
        internal static Task ReceiveCreateKit(MessageContext context, KitOld? kit)
        {
            if (kit != null) return KitManager.AddKit(kit);
            return Task.CompletedTask;
        }

        [NetCall(ENetCall.FROM_SERVER, 1113)]
        internal static void ReceiveRequestKitClass(MessageContext context, string kitID)
        {
            if (KitManager.KitExists(kitID, out KitOld kit))
            {
                if (!kit.SignTexts.TryGetValue(L.DEFAULT, out string signtext))
                    signtext = kit.SignTexts.Values.FirstOrDefault() ?? kit.Name;

                context.Reply(SendKitClass, kitID, kit.Class, signtext);
            }
            else
            {
                context.Reply(SendKitClass, kitID, Class.None, kitID);
            }
        }

        [NetCall(ENetCall.FROM_SERVER, 1115)]
        internal static void ReceiveKitRequest(MessageContext context, string kitID)
        {
            if (KitManager.KitExists(kitID, out KitOld kit))
            {
                context.Reply(SendKit, kit);
            }
            else
            {
                context.Reply(SendKit, kit);
            }
        }
        [NetCall(ENetCall.FROM_SERVER, 1116)]
        internal static void ReceiveKitsRequest(MessageContext context, string[] kitIDs)
        {
            KitOld[] kits = new KitOld[kitIDs.Length];
            for (int i = 0; i < kitIDs.Length; i++)
            {
                if (KitManager.KitExists(kitIDs[i], out KitOld kit))
                {
                    kits[i] = kit;
                }
                else
                {
                    kits[i] = null!;
                }
            }
            context.Reply(SendKits, kits);
        }
        [NetCall(ENetCall.FROM_SERVER, 1110)]
        private static async Task ReceiveCreateLoadoutRequest(MessageContext context, ulong fromPlayer, ulong player, byte team, Class @class, string displayName)
        {
            if (KitManager.Loaded)
            {
                (KitOld? kit, int code) = await KitManager.GetSingleton().CreateLoadout(fromPlayer, player, team, @class, displayName);

                context.Reply(SendAckCreateLoadout, kit is null ? string.Empty : kit.Name, code);
            }
            else
            {
                context.Reply(SendAckCreateLoadout, string.Empty, 554);
            }
        }
    }
}
public enum KitAccessType : byte
{
    Unknown,
    Credits,
    Event,
    Purchase,
    QuestReward,
    [Obsolete]
    QUEST_REWARD = QuestReward
}
