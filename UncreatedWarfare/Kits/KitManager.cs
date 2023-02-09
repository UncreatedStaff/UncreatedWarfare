using MySqlConnector;
using SDG.NetTransport;
using SDG.Unturned;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Uncreated.Framework;
using Uncreated.Json;
using Uncreated.Networking;
using Uncreated.SQL;
using Uncreated.Warfare.Commands;
using Uncreated.Warfare.Commands.CommandSystem;
using Uncreated.Warfare.Events;
using Uncreated.Warfare.Events.Players;
using Uncreated.Warfare.Gamemodes;
using Uncreated.Warfare.Gamemodes.Interfaces;
using Uncreated.Warfare.Point;
using Uncreated.Warfare.Quests;
using Uncreated.Warfare.Singletons;
using Uncreated.Warfare.Squads;
using Uncreated.Warfare.Structures;
using Uncreated.Warfare.Sync;
using Uncreated.Warfare.Teams;
using UnityEngine;

namespace Uncreated.Warfare.Kits;
// todo add delays to kits
public class KitManager : ListSqlSingleton<Kit>, IQuestCompletedHandlerAsync, IPlayerConnectListenerAsync, IPlayerPostInitListenerAsync, IJoinedTeamListenerAsync, IGameTickListener, IPlayerDisconnectListener
{
    public static readonly KitMenuUI MenuUI = new KitMenuUI();
    public override bool AwaitLoad => true;
    public override MySqlDatabase Sql => Data.AdminSql;
    public static event KitChanged? OnKitChanged;
    public static event KitAccessCallback? OnKitAccessChanged;
    public KitManager() : base("kits", SCHEMAS)
    {
        OnItemDeleted += OnKitDeleted;
        OnItemUpdated += OnKitUpdated;
        OnItemAdded   += OnKitUpdated;
        OnKitAccessChanged += OnKitAccessChangedIntl;
    }
    public override async Task PostLoad(CancellationToken token)
    {
        PlayerLife.OnPreDeath += OnPreDeath;
        EventDispatcher.GroupChanged += OnGroupChanged;
        EventDispatcher.PlayerJoined += OnPlayerJoined;
        EventDispatcher.PlayerLeft += OnPlayerLeaving;
        OnItemsRefreshed += OnItemsRefreshedIntl;
        //await MigrateOldKits(token).ConfigureAwait(false);
        await base.PostLoad(token).ConfigureAwait(false);
    }
    private void OnItemsRefreshedIntl()
    {
        UCWarfare.RunTask(async () =>
        {
            await WaitAsync().ConfigureAwait(false);
            try
            {
                SqlItem<Kit>? kit = string.IsNullOrEmpty(TeamManager.Team1UnarmedKit) ? null : FindKitNoLock(TeamManager.Team1UnarmedKit!);
                if (kit?.Item == null)
                    L.LogError("Team 1's unarmed kit, \"" + TeamManager.Team1UnarmedKit + "\", was not found, it should be added to the " + TeamManager.Team2Faction.Name + " faction.");
                kit = string.IsNullOrEmpty(TeamManager.Team2UnarmedKit) ? null : FindKitNoLock(TeamManager.Team2UnarmedKit!);
                if (kit?.Item == null)
                    L.LogError("Team 2's unarmed kit, \"" + TeamManager.Team2UnarmedKit + "\", was not found, it should be added to the " + TeamManager.Team2Faction.Name + " faction.");
                kit = string.IsNullOrEmpty(TeamManager.DefaultKit) ? null : FindKitNoLock(TeamManager.DefaultKit);
                if (kit?.Item == null)
                    L.LogError("The default kit, \"" + TeamManager.Team2UnarmedKit + "\", was not found, it should be added to the team config.");
            }
            finally
            {
                Release();
            }
        });
    }
    public override async Task PreUnload(CancellationToken token)
    {
        EventDispatcher.PlayerLeft -= OnPlayerLeaving;
        EventDispatcher.PlayerJoined -= OnPlayerJoined;
        EventDispatcher.GroupChanged -= OnGroupChanged;
        PlayerLife.OnPreDeath -= OnPreDeath;
        await SaveAllPlayerFavorites(token).ConfigureAwait(false);
        await base.PreUnload(token).ConfigureAwait(false);
    }
    public static KitManager? GetSingletonQuick() => Data.Is(out IKitRequests r) ? r.KitManager : null;
    public static float GetDefaultTeamLimit(Class @class) => @class switch
    {
        Class.HAT => 0.1f,
        _ => 1f
    };
    public static float GetDefaultRequestCooldown(Class @class) => @class switch
    {
        _ => 0f
    };
    /// <returns>The number of ammo boxes required to refill the kit based on it's <see cref="Class"/>.</returns>
    public static int GetAmmoCost(Class @class) => @class switch
    {
        Class.HAT or Class.MachineGunner or Class.CombatEngineer => 3,
        Class.LAT or Class.AutomaticRifleman or Class.Grenadier => 2,
        _ => 1
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
    public async Task<SqlItem<Kit>?> TryGiveUnarmedKit(UCPlayer player, CancellationToken token = default)
    {
#if DEBUG
        using IDisposable profiler = ProfilingUtils.StartTracking();
#endif
        SqlItem<Kit>? kit = await GetDefaultKit(player.GetTeam(), token).ConfigureAwait(false);
        if (kit?.Item != null)
        {
            await GiveKit(player, kit, token).ConfigureAwait(false);
            return kit;
        }
        return null;
    }
    public async Task<SqlItem<Kit>?> TryGiveRiflemanKit(UCPlayer player, CancellationToken token = default)
    {
#if DEBUG
        using IDisposable profiler = ProfilingUtils.StartTracking();
#endif
        SqlItem<Kit>? rifleman;
        await WaitAsync(token).ConfigureAwait(false);
        ulong t2 = player.GetTeam();
        try
        {
            FactionInfo? t = player.Faction;
            rifleman = Items.FirstOrDefault(k =>
                k.Item != null &&
                k.Item.Class == Class.Rifleman &&
               !k.Item.Disabled &&
               !k.Item.IsFactionAllowed(t) &&
               !k.Item.IsCurrentMapAllowed() &&
               (k.Item.Type == KitType.Public && k.Item.CreditCost <= 0 || HasAccessQuick(k, player)) &&
               !k.Item.IsLimited(out _, out _, t2, false) &&
               !k.Item.IsClassLimited(out _, out _, t2, false) &&
                k.Item.MeetsUnlockRequirements(player)
            );
        }
        finally
        {
            Release();
        }

        if (rifleman?.Item == null)
        {
            rifleman = await GetDefaultKit(t2, token).ConfigureAwait(false);
        }
        await GiveKit(player, rifleman, token).ConfigureAwait(false);
        return rifleman?.Item == null ? null : rifleman;
    }
    private async Task<SqlItem<Kit>?> GetDefaultKit(ulong team, CancellationToken token = default)
    {
        string? kitname = team == 1 ? TeamManager.Team1UnarmedKit : TeamManager.Team2UnarmedKit;
        SqlItem<Kit>? kit = team is 1 or 2 && kitname != null ? (await FindKit(kitname, token).ConfigureAwait(false)) : null;
        if (kit?.Item == null)
            kit = await FindKit(TeamManager.DefaultKit, token).ConfigureAwait(false);
        return kit;
    }
    public static Branch GetDefaultBranch(Class @class)
        => @class switch
        {
            Class.Pilot => Branch.Airforce,
            Class.Crewman => Branch.Armor,
            _ => Branch.Infantry
        };
    public SqlItem<Kit>? GetRandomPublicKit()
    {
        List<SqlItem<Kit>> kits = new List<SqlItem<Kit>>(List.ToArray().Where(x => x.Item is { IsPublicKit: true, Requestable: true }));
        return kits.Count == 0 ? null : kits[UnityEngine.Random.Range(0, kits.Count)];
    }
    public static bool ShouldDequipOnExitVehicle(Class @class) => @class is Class.LAT or Class.HAT;
    /// <remarks>Thread Safe</remarks>
    public async Task<SqlItem<Kit>?> FindKit(string id, CancellationToken token = default, bool exactMatchOnly = true)
    {
        await WriteWaitAsync(token).ConfigureAwait(false);
        try
        {
            int index = F.StringIndexOf(List, x => x.Item?.Id, id, exactMatchOnly);
            return index == -1 ? null : List[index];
        }
        finally
        {
            WriteRelease();
        }
    }

    /// <param name="index">Indexed from 1.</param>
    /// <remarks>Thread Safe</remarks>
    public static async Task<SqlItem<Kit>?> GetLoadout(UCPlayer player, int index, ulong team, CancellationToken token = default)
    {
        await player.PurchaseSync.WaitAsync(token).ConfigureAwait(false);
        try
        {
            return GetLoadoutQuick(player, index, team);
        }
        finally
        {
            player.PurchaseSync.Release();
        }
    }
    /// <param name="index">Indexed from 1.</param>
    public static SqlItem<Kit>? GetLoadoutQuick(UCPlayer player, int index, ulong team)
    {
        if (index <= 0)
            throw new ArgumentOutOfRangeException(nameof(index));
        if (player.AccessibleKits != null)
        {
            FactionInfo? faction = TeamManager.GetFactionSafe(team);
            foreach (SqlItem<Kit> kit in player.AccessibleKits
                         .Where(x => x.Item != null && x.Item.Type == KitType.Loadout && !x.Item.IsRequestable(faction))
                         .OrderBy(x => x.Item?.Id ?? string.Empty))
            {
                if (--index <= 0)
                    return kit;
            }
        }

        return null;
    }
    public SqlItem<Kit>? FindKitNoLock(string id, bool exactMatchOnly = true)
    {
        WriteWait();
        try
        {
            int index = F.StringIndexOf(List, x => x.Item?.Id, id, exactMatchOnly);
            return index == -1 ? null : List[index];
        }
        finally
        {
            WriteRelease();
        }
    }
    /// <remarks>Thread Safe</remarks>
    public async Task<List<SqlItem<Kit>>> FindKits(string id, CancellationToken token = default, bool exactMatchOnly = true)
    {
        List<SqlItem<Kit>> kits = new List<SqlItem<Kit>>(4);
        await WriteWaitAsync(token).ConfigureAwait(false);
        try
        {
            F.StringSearch(List, kits, x => x.Item?.Id, id, exactMatchOnly);
        }
        finally
        {
            WriteRelease();
        }

        return kits;
    }
    public List<SqlItem<Kit>> FindKitsNoLock(string id, bool exactMatchOnly = true)
    {
        WriteWait();
        try
        {
            List<SqlItem<Kit>> kits = new List<SqlItem<Kit>>(4);
            F.StringSearch(List, kits, x => x.Item?.Id, id, exactMatchOnly);
            return kits;
        }
        finally
        {
            WriteRelease();
        }
    }
    /// <remarks>Thread Safe</remarks>
    public async Task GiveKit(UCPlayer player, SqlItem<Kit>? kit, CancellationToken token = default)
    {
        if (player == null) throw new ArgumentNullException(nameof(player));
        SqlItem<Kit>? oldKit = null;
        if (kit is null)
        {
            await RemoveKit(player, token).ConfigureAwait(false);
            return;
        }
        await kit.Enter(token).ConfigureAwait(false);
        try
        {
            await WriteWaitAsync(token).ConfigureAwait(false);
            Kit? kitItem;
            try
            {
                kitItem = kit.Item;
            }
            finally
            {
                WriteRelease();
            }
            if (kitItem == null)
            {
                await RemoveKit(player, token).ConfigureAwait(false);
                return;
            }

            await player.PurchaseSync.WaitAsync(token).ConfigureAwait(false);
            try
            {
                await UCWarfare.ToUpdate(token);
#if DEBUG
                using IDisposable profiler = ProfilingUtils.StartTracking();
#endif
                WriteWait();
                try
                {
                    if (player.HasKit)
                        oldKit = player.ActiveKit;
                    GrantKit(player, kit);
                    if (oldKit?.Item != null)
                        UpdateSigns(oldKit);
                }
                finally
                {
                    WriteRelease();
                }
            }
            finally
            {
                player.PurchaseSync.Release();
            }
        }
        finally
        {
            kit.Release();
        }
        if (OnKitChanged != null)
            UCWarfare.RunOnMainThread(() => OnKitChanged?.Invoke(player, kit, oldKit), default);
    }
    internal async Task GiveKitNoLock(UCPlayer player, SqlItem<Kit>? kit, CancellationToken token = default, bool psLock = true)
    {
        if (player == null) throw new ArgumentNullException(nameof(player));
        SqlItem<Kit>? oldKit = null;
        Kit? kitItem;
        await WriteWaitAsync(token).ConfigureAwait(false);
        try
        {
            kitItem = kit?.Item;
        }
        finally
        {
            WriteRelease();
        }
        if (kitItem == null)
        {
            await RemoveKit(player, token, psLock).ConfigureAwait(false);
            return;
        }
        if (psLock)
            await player.PurchaseSync.WaitAsync(token).ConfigureAwait(false);
        try
        {
            if (kitItem == null)
            {
                await RemoveKit(player, token, psLock).ConfigureAwait(false);
                return;
            }
            await UCWarfare.ToUpdate(token);
            WriteWait();
            try
            {
                if (player.HasKit)
                    oldKit = player.ActiveKit;
                GrantKit(player, kit);
                if (oldKit?.Item != null)
                    UpdateSigns(oldKit);
            }
            finally
            {
                WriteRelease();
            }
        }
        finally
        {
            if (psLock)
                player.PurchaseSync.Release();
        }

        if (OnKitChanged != null)
            UCWarfare.RunOnMainThread(() => OnKitChanged?.Invoke(player, kit, oldKit), default);
    }
    /// <remarks>Thread Safe</remarks>
    private async Task RemoveKit(UCPlayer player, CancellationToken token = default, bool psLock = true)
    {
        if (psLock)
            await player.PurchaseSync.WaitAsync(token).ConfigureAwait(false);
        try
        {
            SqlItem<Kit>? oldKit = null;
            if (player.HasKit)
                oldKit = player.ActiveKit;
            await UCWarfare.ToUpdate(token);
            WriteWait();
            try
            {
                GrantKit(player, null);
                if (oldKit?.Item != null)
                    UpdateSigns(oldKit);
            }
            finally
            {
                WriteRelease();
            }
            if (OnKitChanged != null)
                UCWarfare.RunOnMainThread(() => OnKitChanged?.Invoke(player, null, oldKit), default);
        }
        finally
        {
            if (psLock)
                player.PurchaseSync.Release();
        }
    }
    private static void GrantKit(UCPlayer player, SqlItem<Kit>? kit)
    {
        ThreadUtil.assertIsGameThread();
#if DEBUG
        using IDisposable profiler = ProfilingUtils.StartTracking();
#endif
        if (UCWarfare.Config.ModifySkillLevels)
            player.EnsureSkillsets(kit?.Item?.Skillsets ?? Array.Empty<Skillset>());
        player.ChangeKit(kit);
        if (kit?.Item != null)
        {
            UpdateSigns(kit);
            GiveKitToPlayerInventory(player, kit.Item, true);
        }
        else
            UCInventoryManager.ClearInventory(player, true);
    }
    public async Task ResupplyKit(UCPlayer player, bool ignoreAmmoBags = false, CancellationToken token = default)
    {
        if (player.HasKit)
        {
            await ResupplyKit(player, player.ActiveKit!, ignoreAmmoBags, token).ConfigureAwait(false);
        }
    }
    public async Task ResupplyKit(UCPlayer player, SqlItem<Kit> kit, bool ignoreAmmoBags = false, CancellationToken token = default)
    {
        if (kit is null) throw new ArgumentNullException(nameof(kit));
        await kit.Enter(token).ConfigureAwait(false);
        try
        {
            await UCWarfare.ToUpdate(token);
#if DEBUG
            using IDisposable profiler = ProfilingUtils.StartTracking();
#endif
            List<KeyValuePair<ItemJar, Page>> nonKitItems = new List<KeyValuePair<ItemJar, Page>>(16);
            Kit? k;
            WriteWait();
            try
            {
                k = kit.Item;
            }
            finally
            {
                WriteRelease();
            }
            
            for (byte page = 0; page < PlayerInventory.PAGES - 2; ++page)
            {
                byte count = player.Player.inventory.getItemCount(page);
                for (byte i = 0; i < count; ++i)
                {
                    ItemJar jar = player.Player.inventory.items[page].getItem(i);
                    ItemAsset? asset = jar.item.GetAsset();
                    if (asset is null) continue;
                    if (k == null || !k.ContainsItem(asset.GUID))
                    {
                        WhitelistItem? item = null;
                        if (!Whitelister.Loaded || Whitelister.IsWhitelisted(asset.GUID, out item))
                        {
                            if (item != null && item.Amount < byte.MaxValue && item.Amount != 0)
                            {
                                int amt = 0;
                                for (int w = 0; w < nonKitItems.Count; ++w)
                                {
                                    if (nonKitItems[w].Key.GetAsset() is { } ia2 && ia2.GUID == item.Item)
                                    {
                                        ++amt;
                                        if (amt >= item.Amount)
                                            goto s;
                                    }
                                }
                            }
                            nonKitItems.Add(new KeyValuePair<ItemJar, Page>(jar, (Page)page));
                            s: ;
                        }
                    }
                }
            }

            GiveKitToPlayerInventory(player, k, true);
            foreach (KeyValuePair<ItemJar, Page> jar in nonKitItems)
            {
                if (!player.Player.inventory.tryAddItem(jar.Key.item, jar.Key.x, jar.Key.y, (byte)jar.Value, jar.Key.rot) &&
                    !player.Player.inventory.tryAddItem(jar.Key.item, true))
                    ItemManager.dropItem(jar.Key.item, player.Position, false, true, true);
            }
        }
        finally
        {
            kit.Release();
        }
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
                             IBaseItem i2 => i2.Item.ToString("N"),
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
    async Task IQuestCompletedHandlerAsync.OnQuestCompleted(QuestCompleted e, CancellationToken token)
    {
        await WaitAsync(token).ConfigureAwait(false);
        try
        {
            await UCWarfare.ToUpdate(token);
            if (!e.Player.IsOnline)
                return;
            WriteWait();
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
                WriteRelease();
            }
        }
        finally
        {
            Release();
        }
    }
    async Task IPlayerConnectListenerAsync.OnPlayerConnecting(UCPlayer player, CancellationToken token)
    {
#if DEBUG
        using IDisposable profiler = ProfilingUtils.StartTracking();
#endif
        await WaitAsync(token).ConfigureAwait(false);
        try
        {
            await UCWarfare.ToUpdate(token);
            if (!player.IsOnline)
                return;
            WriteWait();
            try
            {
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
                                    player.Player.quests.ServerAddQuest(quest);
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
                WriteRelease();
            }
        }
        finally
        {
            Release();
        }
        Signs.UpdateKitSigns(player, null);
    }
    private void OnPreDeath(PlayerLife life)
    {
#if DEBUG
        using IDisposable profiler = ProfilingUtils.StartTracking();
#endif
        UCPlayer? player = UCPlayer.FromPlayer(life.player);
        Kit? active;
        WriteWait();
        try
        {
            active = player?.ActiveKit?.Item;
        }
        finally
        {
            WriteRelease();
        }
        if (active != null)
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

                    bool notInKit = !active.ContainsItem(asset.GUID) && Whitelister.IsWhitelisted(asset.GUID, out _);
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
    public async Task DequipKit(UCPlayer player, CancellationToken token = default)
    {
        ulong team = player.GetTeam();
        SqlItem<Kit>? dkit = await GetDefaultKit(team, token);
        if (dkit?.Item != null)
            await GiveKit(player, dkit, token);
        else
            await GiveKit(player, null, token);
    }
    /// <remarks>Thread Safe</remarks>
    public Task DequipKit(UCPlayer player, Kit kit, CancellationToken token = default)
    {
        if (player.ActiveKit is not null && player.ActiveKit.LastPrimaryKey.Key == kit.PrimaryKey.Key)
        {
            return DequipKit(player, token);
        }

        return Task.CompletedTask;
    }
    private void OnKitAccessChangedIntl(SqlItem<Kit> kit, ulong player, bool newAccess, KitAccessType type)
    {
        if (newAccess) return;
        UCPlayer? pl = UCPlayer.FromID(player);
        if (pl != null && kit.Item != null && pl.ActiveKit == kit)
        {
            UCWarfare.RunTask(DequipKit, pl, kit.Item, ctx: "Dequiping " + kit.Item?.Id + " from " + player + ".");
        }
    }
    private void OnKitDeleted(SqlItem<Kit> proxy, Kit kit)
    {
        if (UCWarfare.Config.EnableSync)
            KitSync.OnKitDeleted(kit.PrimaryKey);
        Task.Run(() => Util.TryWrap(DequipKit(kit), "Failed to dequip " + kit.Id + " from all."));
    }
    private void OnKitUpdated(SqlItem<Kit> kit)
    {
        if (UCWarfare.Config.EnableSync)
            KitSync.OnKitUpdated(kit);
    }
    private void OnPlayerLeaving(PlayerEvent e) => OnTeamPlayerCountChanged();
    private void OnPlayerJoined(PlayerJoined e) => OnTeamPlayerCountChanged();
    private void OnGroupChanged(GroupChanged e) => OnTeamPlayerCountChanged(e.Player);
    private readonly List<Kit> _kitListTemp = new List<Kit>(64);
    internal void OnTeamPlayerCountChanged(UCPlayer? allPlayer = null)
    {
        if (allPlayer != null)
            Signs.UpdateKitSigns(allPlayer, null);
        UCWarfare.RunTask(async () =>
        {
            await WaitAsync().ConfigureAwait(false);
            try
            {
                await UCWarfare.ToUpdate();
                WriteWait();
                try
                {
                    for (int i = 0; i < Items.Count; i++)
                    {
                        SqlItem<Kit> item = Items[i];
                        if (item.Item == null)
                            continue;
                        if (item.Item.TeamLimit < 1f)
                        {
                            _kitListTemp.Add(item.Item);
                        }
                    }
                }
                finally
                {
                    WriteRelease();
                }
                for (int i = 0; i < _kitListTemp.Count; ++i)
                    Signs.UpdateKitSigns(null, _kitListTemp[i].Id);
                _kitListTemp.Clear();
            }
            finally
            {
                Release();
            }
        }, "Updating all kit signs after team player count update");
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
        Signs.UpdateKitSigns(player, null);
    }
    private static void UpdateSignsIntl(Kit kit, UCPlayer? player)
    {
        if (kit.Type == KitType.Loadout)
        {
            Signs.UpdateLoadoutSigns(player);
        }
        else
        {
            Signs.UpdateKitSigns(player, kit.Id);
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
        Signs.UpdateKitSigns(player, kitId);
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
    public static async Task<bool> GiveAccess(string kit, UCPlayer player, KitAccessType type, CancellationToken token = default)
    {
        KitManager? manager = GetSingletonQuick();
        if (manager == null)
            return await AddAccessRow(kit, player.Steam64, type, token).ConfigureAwait(false);

        SqlItem<Kit>? proxy = await manager.FindKit(kit, token).ConfigureAwait(false);
        if (proxy?.Item == null)
            return false;
        return await GiveAccess(proxy, player, type, token).ConfigureAwait(false);
    }
    /// <remarks>Thread Safe</remarks>
    public static async Task<bool> GiveAccess(string kit, ulong player, KitAccessType type, CancellationToken token = default)
    {
        KitManager? manager = GetSingletonQuick();
        if (manager == null)
            return await AddAccessRow(kit, player, type, token).ConfigureAwait(false);

        SqlItem<Kit>? proxy = await manager.FindKit(kit, token).ConfigureAwait(false);
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
                if (OnKitAccessChanged != null)
                    UCWarfare.RunOnMainThread(() => OnKitAccessChanged?.Invoke(kit, player.Steam64, true, type), default);
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
        if (OnKitAccessChanged != null)
            UCWarfare.RunOnMainThread(() => OnKitAccessChanged?.Invoke(kit, player, true, type), default);
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
    public async Task<bool> RemoveAccess(string kit, UCPlayer player, CancellationToken token = default)
    {
        KitManager? manager = GetSingletonQuick();
        if (manager == null)
            return await RemoveAccessRow(kit, player.Steam64, token).ConfigureAwait(false);

        SqlItem<Kit>? proxy = await manager.FindKit(kit, token).ConfigureAwait(false);
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
    public static async Task<bool> RemoveAccess(string kit, ulong player, CancellationToken token = default)
    {
        KitManager? manager = GetSingletonQuick();
        if (manager == null)
            return await RemoveAccessRow(kit, player, token).ConfigureAwait(false);

        SqlItem<Kit>? proxy = await manager.FindKit(kit, token).ConfigureAwait(false);
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
        bool access;
        try
        {
            if (player.AccessibleKits != null && player.AccessibleKits.Contains(kit))
                return true;
            access = await RemoveAccessRow(kit.PrimaryKey, player.Steam64, token).ConfigureAwait(false);
            if (access)
            {
                player.AccessibleKits?.Remove(kit);
                if (OnKitAccessChanged != null)
                    UCWarfare.RunOnMainThread(() => OnKitAccessChanged?.Invoke(kit, player.Steam64, false, KitAccessType.Unknown), default);
            }
        }
        finally
        {
            player.PurchaseSync.Release();
        }

        KitManager? manager = GetSingletonQuick();
        if (manager != null)
            await manager.DequipKit(player, token);
        return access;
    }
    /// <remarks>Thread Safe</remarks>
    public static async Task<bool> RemoveAccess(SqlItem<Kit> kit, ulong player, CancellationToken token = default)
    {
        if (!kit.PrimaryKey.IsValid)
            return false;
        UCPlayer? online = UCPlayer.FromID(player);
        if (online is { IsOnline: true })
            return await RemoveAccess(kit, online, token).ConfigureAwait(false);
        bool res = await RemoveAccessRow(kit.PrimaryKey, player, token).ConfigureAwait(false);
        if (OnKitAccessChanged != null)
            UCWarfare.RunOnMainThread(() => OnKitAccessChanged?.Invoke(kit, player, false, KitAccessType.Unknown), default);
        return res;
    }
    private static async Task<bool> AddAccessRow(PrimaryKey kit, ulong player, KitAccessType type, CancellationToken token = default)
    {
        return await Data.AdminSql.NonQueryAsync(
            $"INSERT INTO `{TABLE_ACCESS}` ({SqlTypes.ColumnList(COLUMN_EXT_PK, COLUMN_ACCESS_STEAM_64, COLUMN_ACCESS_TYPE)}) " +
             "VALUES (@0, @1, @2);", new object[] { kit.Key, player, type.ToString() }, token).ConfigureAwait(false) > 0;
    }
    private static async Task<bool> AddAccessRow(string kit, ulong player, KitAccessType type, CancellationToken token = default)
    {
        PrimaryKey pk = PrimaryKey.NotAssigned;
        await Data.AdminSql.QueryAsync($"SELECT `{COLUMN_PK}` FROM `{TABLE_MAIN}` WHERE `{COLUMN_KIT_ID}`=@0;",
            new object[] { kit },
            reader =>
            {
                pk = reader.GetInt32(0);
            }, token).ConfigureAwait(false);
        if (pk.IsValid)
            return await AddAccessRow(pk, player, type, token).ConfigureAwait(false);
        return false;
    }
    private static async Task<bool> RemoveAccessRow(PrimaryKey kit, ulong player, CancellationToken token = default)
    {
        return await Data.AdminSql.NonQueryAsync(
            $"DELETE FROM `{TABLE_ACCESS}` WHERE `{COLUMN_EXT_PK}`=@0 AND `{COLUMN_ACCESS_STEAM_64}`=@1;",
            new object[] { kit.Key, player }, token).ConfigureAwait(false) > 0;
    }
    private static async Task<bool> RemoveAccessRow(string kit, ulong player, CancellationToken token = default)
    {
        PrimaryKey pk = PrimaryKey.NotAssigned;
        await Data.AdminSql.QueryAsync($"SELECT `{COLUMN_PK}` FROM `{TABLE_MAIN}` WHERE `{COLUMN_KIT_ID}`=@0;",
            new object[] { kit },
            reader =>
            {
                pk = reader.GetInt32(0);
            }, token).ConfigureAwait(false);
        if (pk.IsValid)
            return await RemoveAccessRow(pk, player, token).ConfigureAwait(false);
        return false;
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
                                              $"WHERE `{COLUMN_EXT_PK}`=@0 AND `{COLUMN_ACCESS_STEAM_64}`=@1 LIMIT 1;", new object[]
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
    /// <summary>Will not update signs.</summary>
    public static void SetTextNoLock(ulong setter, Kit kit, string? text, string language = L.Default)
    {
        if (kit is null) throw new ArgumentNullException(nameof(kit));
        kit.SignText.Remove(language);
        if (!string.IsNullOrEmpty(text))
            kit.SignText.Add(language, text!);
        kit.UpdateLastEdited(setter);
    }
    /// <remarks>Thread Safe</remarks>
    public static async Task SetText(ulong setter, SqlItem<Kit> kit, string? text, string language = L.Default, bool updateSigns = true, CancellationToken token = default)
    {
        if (kit is null) throw new ArgumentNullException(nameof(kit));
        language ??= L.Default;
        await kit.Enter(token).ConfigureAwait(false);
        try
        {
            if (kit.Item == null) return;
            SetTextNoLock(setter, kit.Item, text, language);
            await kit.SaveItem(token).ConfigureAwait(false);
            if (updateSigns)
            {
                await UCWarfare.ToUpdate(token);
                UpdateSigns(kit);
            }
        }
        finally
        {
            kit.Release();
        }
    }
    public static async Task<char> GetLoadoutCharacter(ulong playerId)
    {
        char let = 'a';
        await Data.DatabaseManager.QueryAsync("SELECT `InternalName` FROM `kit_data` WHERE `InternalName` LIKE @0 ORDER BY `InternalName`;", new object[]
        {
            playerId.ToString(Data.AdminLocale) + "_%"
        }, reader =>
        {
            string name = reader.GetString(0);
            if (name.Length < 19)
                return;
            char let2 = name[18];
            if (let2 == let)
                let++;
        });
        return let;
    }
    public async Task<(SqlItem<Kit>, StandardErrorCode)> CreateLoadout(ulong fromPlayer, ulong player, ulong team, Class @class, string displayName, CancellationToken token = default)
    {
#if DEBUG
        using IDisposable profiler = ProfilingUtils.StartTracking();
#endif
        char let = await GetLoadoutCharacter(player);
        string loadoutName = player.ToString(Data.AdminLocale) + "_" + let;
        SqlItem<Kit>? existing = await FindKit(loadoutName, token, true).ConfigureAwait(false);
        if (existing?.Item != null)
            return (existing, StandardErrorCode.GenericError);
        SqlItem<Kit>? kit = await GetDefaultKit(team, token).ConfigureAwait(false);
        IKitItem[] items;
        if (kit?.Item != null)
        {
            items = new IKitItem[kit.Item.Items.Length];
            Array.Copy(kit.Item.Items, items, items.Length);
        }
        else
            items = Array.Empty<IKitItem>();

        Kit loadout = new Kit(loadoutName, @class, GetDefaultBranch(@class), KitType.Loadout, SquadLevel.Member, TeamManager.GetFactionSafe(team))
        {
            Items = items.ToArray(),
            Creator = fromPlayer
        };
        SetTextNoLock(fromPlayer, loadout, displayName);
        kit = await AddOrUpdate(loadout, token).ConfigureAwait(false);

        ActionLog.Add(ActionLogType.CreateKit, loadoutName, fromPlayer);

        return (kit, StandardErrorCode.Success);
    }
    internal void InvokeAfterMajorKitUpdate(SqlItem<Kit> proxy)
    {
        if (proxy is null)
            return;
        for (int i = 0; i < PlayerManager.OnlinePlayers.Count; ++i)
        {
            if (PlayerManager.OnlinePlayers[i].ActiveKit == proxy)
            {
                PlayerManager.OnlinePlayers[i].ChangeKit(proxy);
            }
        }

        if (OnKitChanged == null)
            return;
        // waits a frame in case something tries to lock the kit and to ensure we are on main thread.
        UCWarfare.RunOnMainThread(() =>
        {
            if (OnKitChanged == null)
                return;
            for (int i = 0; i < PlayerManager.OnlinePlayers.Count; ++i)
            {
                if (PlayerManager.OnlinePlayers[i].ActiveKit == proxy)
                {
                    OnKitChanged.Invoke(PlayerManager.OnlinePlayers[i], proxy, proxy);
                }
            }
        }, true);
    }
    public async Task RequestLoadout(int loadoutId, CommandInteraction ctx, CancellationToken token = default)
    {
        ulong team = ctx.Caller.GetTeam();
        SqlItem<Kit>? loadout = await GetLoadout(ctx.Caller, loadoutId, team, token).ConfigureAwait(false);
        if (loadout?.Item == null)
            throw ctx.Reply(T.RequestLoadoutNotOwned);
        await loadout.Enter(token).ConfigureAwait(false);
        try
        {
            if (loadout.Item == null)
                throw ctx.Reply(T.RequestLoadoutNotOwned);
            if (loadout.Item.IsClassLimited(out _, out int allowedPlayers, team))
            {
                ctx.Reply(T.RequestKitLimited, allowedPlayers);
                return;
            }
            ctx.LogAction(ActionLogType.RequestKit, $"Loadout #{loadoutId}: {loadout.Item.Id}, Team {team}, Class: {Localization.TranslateEnum(loadout.Item.Class, 0)}");

            if (!await GrantKitRequest(ctx, loadout, token).ConfigureAwait(false))
            {
                await UCWarfare.ToUpdate(token);
                throw ctx.SendUnknownError();
            }
        }
        finally
        {
            loadout.Release();
        }
    }
    public async Task RequestKit(SqlItem<Kit> proxy, CommandInteraction ctx, CancellationToken token = default)
    {
        await proxy.Enter(token).ConfigureAwait(false);
        try
        {
            ulong team = ctx.Caller.GetTeam();
            Kit? kit;
            await WriteWaitAsync(token).ConfigureAwait(false);
            try
            {
                kit = proxy.Item;
            }
            finally
            {
                WriteRelease();
            }
            if (kit == null)
                throw ctx.Reply(T.KitNotFound, proxy.LastPrimaryKey.ToString());
            if (ctx.Caller.HasKit && ctx.Caller.ActiveKit!.PrimaryKey.Key == kit.PrimaryKey.Key)
                throw ctx.Reply(T.RequestKitAlreadyOwned);
            if (kit.Disabled || kit.Season != UCWarfare.Season && kit.Season > 0)
                throw ctx.Reply(T.RequestKitDisabled);
            if (!kit.IsCurrentMapAllowed())
                throw ctx.Reply(T.RequestKitMapBlacklisted);
            if (!kit.IsFactionAllowed(TeamManager.GetFactionSafe(team)))
                throw ctx.Reply(T.RequestKitFactionBlacklisted);
            if (kit.IsPublicKit)
            {
                if (kit.CreditCost > 0 && !HasAccessQuick(kit, ctx.Caller) && !UCWarfare.Config.OverrideKitRequirements)
                {
                    if (ctx.Caller.CachedCredits >= kit.CreditCost)
                        throw ctx.Reply(T.RequestKitNotBought, kit.CreditCost);
                    else
                        throw ctx.Reply(T.RequestKitCantAfford, kit.CreditCost - ctx.Caller.CachedCredits, kit.CreditCost);
                }
            }
            else if (!HasAccessQuick(kit, ctx.Caller) && !UCWarfare.Config.OverrideKitRequirements)
                throw ctx.Reply(T.RequestKitMissingAccess);
            if (kit.IsLimited(out _, out int allowedPlayers, team) || kit.Type == KitType.Loadout && kit.IsClassLimited(out _, out allowedPlayers, team))
                throw ctx.Reply(T.RequestKitLimited, allowedPlayers);
            if (kit.Class == Class.Squadleader && ctx.Caller.Squad is not null && !ctx.Caller.IsSquadLeader())
                throw ctx.Reply(T.RequestKitNotSquadleader);

            // cooldowns
            if (
                Data.Gamemode.State == State.Active &&
                CooldownManager.HasCooldown(ctx.Caller, CooldownType.RequestKit, out Cooldown requestCooldown) &&
                !ctx.Caller.OnDutyOrAdmin() &&
                !UCWarfare.Config.OverrideKitRequirements &&
                kit.Class is not Class.Crewman and not Class.Pilot)
                throw ctx.Reply(T.KitOnGlobalCooldown, requestCooldown);
            if (kit.IsPaid && kit.RequestCooldown > 0f &&
                CooldownManager.HasCooldown(ctx.Caller, CooldownType.PremiumKit, out Cooldown premiumCooldown, kit.Id) &&
                !ctx.Caller.OnDutyOrAdmin() &&
                !UCWarfare.Config.OverrideKitRequirements)
                throw ctx.Reply(T.KitOnCooldown, premiumCooldown);

            for (int i = 0; i < kit.UnlockRequirements.Length; i++)
            {
                UnlockRequirement req = kit.UnlockRequirements[i];
                if (req.CanAccess(ctx.Caller))
                    continue;
                throw req.RequestKitFailureToMeet(ctx, kit);
            }
            bool hasAccess = kit.CreditCost == 0 && kit.IsPublicKit || UCWarfare.Config.OverrideKitRequirements;
            if (!hasAccess)
            {
                hasAccess = await HasAccess(kit, ctx.Caller.Steam64, token).ConfigureAwait(false);
                await UCWarfare.ToUpdate(token);
                if (!hasAccess)
                {
                    if (kit.IsPaid)
                        ctx.Reply(T.RequestKitMissingAccess);
                    else if (ctx.Caller.CachedCredits >= kit.CreditCost)
                        ctx.Reply(T.RequestKitNotBought, kit.CreditCost);
                    else
                        ctx.Reply(T.RequestKitCantAfford, kit.CreditCost - ctx.Caller.CachedCredits, kit.CreditCost);
                    return;
                }
            }
            // recheck limits to make sure people can't request at the same time to avoid limits.
            if (kit.IsLimited(out _, out allowedPlayers, team) || kit.Type == KitType.Loadout && kit.IsClassLimited(out _, out allowedPlayers, team))
                throw ctx.Reply(T.RequestKitLimited, allowedPlayers);
            if (kit.Class == Class.Squadleader)
            {
                if (ctx.Caller.Squad is not null && !ctx.Caller.IsSquadLeader())
                    throw ctx.Reply(T.RequestKitNotSquadleader);
                if (ctx.Caller.Squad is null)
                {
                    if (SquadManager.Squads.Count(x => x.Team == team) < 8)
                    {
                        // create a squad automatically if someone requests a squad leader kit.
                        Squad squad = SquadManager.CreateSquad(ctx.Caller, team);
                        ctx.Reply(T.SquadCreated, squad);
                    }
                    else throw ctx.Reply(T.SquadsTooMany, SquadManager.ListUI.Squads.Length);
                }
            }
            ctx.LogAction(ActionLogType.RequestKit, $"Kit {kit.Id}, Team {team}, Class: {Localization.TranslateEnum(kit.Class, 0)}");

            if (!await GrantKitRequest(ctx, proxy, token).ConfigureAwait(false))
            {
                await UCWarfare.ToUpdate(token);
                throw ctx.SendUnknownError();
            }
        }
        finally
        {
            proxy.Release();
        }
    }
    /// <exception cref="BaseCommandInteraction"/>
    public async Task BuyKit(CommandInteraction ctx, SqlItem<Kit> proxy, Vector3? effectPos = null, CancellationToken token = default)
    {
        ulong team = ctx.Caller.GetTeam();
        await proxy.Enter(token).ConfigureAwait(false);
        Kit? kit;
        try
        {
            await WriteWaitAsync(token).ConfigureAwait(false);
            try
            {
                kit = proxy.Item;
            }
            finally
            {
                WriteRelease();
            }
            if (kit == null)
                throw ctx.Reply(T.KitNotFound, proxy.LastPrimaryKey.ToString());
            if (!kit.IsPublicKit)
                throw ctx.Reply(T.RequestNotBuyable);
            if (kit.CreditCost == 0 || HasAccessQuick(kit, ctx.Caller))
                throw ctx.Reply(T.RequestKitAlreadyOwned);
            if (ctx.Caller.CachedCredits < kit.CreditCost)
                throw ctx.Reply(T.RequestKitCantAfford, kit.CreditCost - ctx.Caller.CachedCredits, kit.CreditCost);

            await ctx.Caller.PurchaseSync.WaitAsync(token).ConfigureAwait(false);
            if (!ctx.Caller.HasDownloadedKits)
                await ctx.Caller.DownloadKits(false, token).ConfigureAwait(false);
            try
            {
                await Points.UpdatePointsAsync(ctx.Caller, false, token).ConfigureAwait(false);
                if (ctx.Caller.CachedCredits < kit.CreditCost)
                {
                    await UCWarfare.ToUpdate();
                    throw ctx.Reply(T.RequestKitCantAfford, kit.CreditCost - ctx.Caller.CachedCredits, kit.CreditCost);
                }

                CreditsParameters parameters = new CreditsParameters(ctx.Caller, team, -kit.CreditCost)
                {
                    IsPurchase = true,
                    IsPunishment = false
                };
                await Points.AwardCreditsAsync(parameters, token, false).ConfigureAwait(false);
            }
            finally
            {
                ctx.Caller.PurchaseSync.Release();
            }

            if (!await GiveAccess(kit, ctx.Caller, KitAccessType.Credits, token).ConfigureAwait(false))
            {
                await UCWarfare.ToUpdate(token);
                throw ctx.SendUnknownError();
            }
            ctx.LogAction(ActionLogType.BuyKit, "BOUGHT KIT " + kit.Id + " FOR " + kit.CreditCost + " CREDITS");
            L.Log(ctx.Caller.Name.PlayerName + " (" + ctx.Caller.Steam64 + ") bought " + kit.Id);
        }
        finally
        {
            proxy.Release();
        }

        UpdateSigns(kit, ctx.Caller);
        if (Gamemode.Config.EffectPurchase.ValidReference(out EffectAsset effect))
        {
            F.TriggerEffectReliable(effect, EffectManager.SMALL, effectPos ?? ctx.Caller.Position);
        }

        ctx.Reply(T.RequestKitBought, kit.CreditCost);
    }
    private async Task<bool> GrantKitRequest(CommandInteraction ctx, SqlItem<Kit> proxy, CancellationToken token = default)
    {
        await UCWarfare.ToUpdate(token);
        ulong team = ctx.Caller.GetTeam();
        AmmoCommand.WipeDroppedItems(ctx.CallerID);
        await GiveKitNoLock(ctx.Caller, proxy, token).ConfigureAwait(false);
        Kit? kit;
        await WriteWaitAsync(token).ConfigureAwait(false);
        try
        {
            kit = proxy?.Item;
        }
        finally
        {
            WriteRelease();
        }
        if (kit == null)
            return true;
        string id = kit.Id;
        Stats.StatsManager.ModifyKit(id, k => k.TimesRequested++);
        Stats.StatsManager.ModifyStats(ctx.CallerID, s =>
        {
            Stats.WarfareStats.KitData kitData = s.Kits.Find(k => k.KitID == id && k.Team == team);
            if (kitData == default)
            {
                kitData = new Stats.WarfareStats.KitData() { KitID = id, Team = (byte)team, TimesRequested = 1 };
                s.Kits.Add(kitData);
            }
            else
                kitData.TimesRequested++;
        }, false);

        ctx.Reply(T.RequestSignGiven, kit.Class);

        if (kit.IsPaid && kit.RequestCooldown > 0)
            CooldownManager.StartCooldown(ctx.Caller, CooldownType.PremiumKit, kit.RequestCooldown, kit.Id);
        CooldownManager.StartCooldown(ctx.Caller, CooldownType.RequestKit, CooldownManager.Config.RequestKitCooldown);

        return true;
    }
    private async Task SetupPlayer(UCPlayer player, CancellationToken token = default)
    {
        ulong team = player.GetTeam();
        Kit? kit = player.ActiveKit?.Item;
        if (kit == null || !kit.Requestable || (kit.Type != KitType.Loadout && kit.IsLimited(out _, out _, team)) || (kit.Type == KitType.Loadout && kit.IsClassLimited(out _, out _, team)))
            await TryGiveRiflemanKit(player, token).ConfigureAwait(false);
        else if (UCWarfare.Config.ModifySkillLevels)
            player.EnsureSkillsets(kit.Skillsets ?? Array.Empty<Skillset>());
    }
    async Task IPlayerPostInitListenerAsync.OnPostPlayerInit(UCPlayer player, CancellationToken token)
    {
        if (Data.Gamemode is not TeamGamemode { UseTeamSelector: true })
        {
            Task t = SetupPlayer(player, token);
            await RefreshFavorites(player, false, token).ConfigureAwait(false);
            await t.ConfigureAwait(false);
            return;
        }
        UCInventoryManager.ClearInventory(player);
        player.EnsureSkillsets(Array.Empty<Skillset>());
        await RefreshFavorites(player, false, token).ConfigureAwait(false);
    }
    Task IJoinedTeamListenerAsync.OnJoinTeamAsync(UCPlayer player, ulong team, CancellationToken token)
    {
        return TryGiveKitOnJoinTeam(player, token);
    }
    public async Task RefreshFavorites(UCPlayer player, bool @lock, CancellationToken token = default)
    {
        token.CombineIfNeeded(player.DisconnectToken, UCWarfare.UnloadCancel);
        if (@lock)
            await player.PurchaseSync.WaitAsync(token);
        try
        {
            player.KitMenuData.FavoriteKits = new List<PrimaryKey>(
                player.KitMenuData.FavoriteKits == null ? 5 : player.KitMenuData.FavoriteKits.Count);
            await Sql.QueryAsync(F.BuildSelectWhere(TABLE_FAVORITES, COLUMN_FAVORITE_PLAYER, 0, COLUMN_EXT_PK),
                new object[] { player.Steam64 }, reader =>
                {
                    player.KitMenuData.FavoriteKits.Add(reader.GetInt32(0));
                }, token).ConfigureAwait(false);
            MenuUI.OnFavoritesRefreshed(player);
            player.KitMenuData.FavoritesDirty = false;
        }
        finally
        {
            if (@lock)
                player.PurchaseSync.Release();
        }
    }
    void IGameTickListener.Tick()
    {
        if (Data.Gamemode.EveryMinute && Provider.clients.Count > 0)
        {
            UCWarfare.RunTask(SaveAllPlayerFavorites, ctx: "Save all players' favorite kits.");
        }
    }
    private async Task SaveAllPlayerFavorites(CancellationToken token)
    {
        await WriteWaitAsync(token).ConfigureAwait(false);
        try
        {
            foreach (UCPlayer player in PlayerManager.OnlinePlayers.ToArray())
            {
                if (player.KitMenuData is { FavoritesDirty: true, FavoriteKits: { } fk })
                {
                    await SaveFavorites(player, fk, token).ConfigureAwait(false);
                    token.ThrowIfCancellationRequested();
                }
            }
        }
        finally
        {
            WriteRelease();
        }
    }
    void IPlayerDisconnectListener.OnPlayerDisconnecting(UCPlayer player)
    {
        if (player.KitMenuData is { FavoritesDirty: true, FavoriteKits: { } fk })
        {
            UCWarfare.RunTask(async () =>
            {
                CancellationToken token = UCWarfare.UnloadCancel;
                await WriteWaitAsync(token).ConfigureAwait(false);
                try
                {
                    await SaveFavorites(player, fk, token).ConfigureAwait(false);
                    token.ThrowIfCancellationRequested();
                }
                finally
                {
                    WriteRelease();
                }
            });
        }
    }
    internal async Task SaveFavorites(UCPlayer player, List<PrimaryKey> favoriteKits, CancellationToken token = default)
    {
        token.CombineIfNeeded(UCWarfare.UnloadCancel);
        object[] args = new object[favoriteKits.Count + 1];
        args[0] = player.Steam64;
        StringBuilder sb = new StringBuilder("(");
        for (int i = 0; i < favoriteKits.Count; ++i)
        {
            args[i + 1] = favoriteKits[i].Key;
            if (i != 0)
                sb.Append("),(");
            sb.Append("@0,@").Append(i + 1);
        }

        sb.Append(");");

        if (args.Length <= 1)
            return;
        await Sql.NonQueryAsync($"DELETE FROM `{TABLE_FAVORITES}` WHERE `{COLUMN_FAVORITE_PLAYER}`=@0;" +
                                F.StartBuildOtherInsertQueryNoUpdate(TABLE_FAVORITES,
                                    COLUMN_FAVORITE_PLAYER, COLUMN_EXT_PK) + sb, args, token).ConfigureAwait(false);
        player.KitMenuData.FavoritesDirty = false;
    }
    #region Sql
    // ReSharper disable InconsistentNaming
    public const string TABLE_MAIN = "kits";
    public const string TABLE_ITEMS = "kits_items";
    public const string TABLE_UNLOCK_REQUIREMENTS = "kits_unlock_requirements";
    public const string TABLE_SKILLSETS = "kits_skillsets";
    public const string TABLE_FACTION_FILTER = "kits_faction_filters";
    public const string TABLE_MAP_FILTER = "kits_map_filters";
    public const string TABLE_SIGN_TEXT = "kits_sign_text";
    public const string TABLE_ACCESS = "kits_access";
    public const string TABLE_REQUEST_SIGNS = "kits_request_signs";
    public const string TABLE_FAVORITES = "kits_favorites";

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
    public const string COLUMN_COST_CREDITS = "CreditCost";
    public const string COLUMN_COST_PREMIUM = "PremiumCost";
    public const string COLUMN_SQUAD_LEVEL = "SquadLevel";
    public const string COLUMN_MAPS_WHITELIST = "MapFilterIsWhitelist";
    public const string COLUMN_FACTIONS_WHITELIST = "FactionFilterIsWhitelist";
    public const string COLUMN_CREATOR = "Creator";
    public const string COLUMN_LAST_EDITOR = "LastEditor";
    public const string COLUMN_CREATION_TIME = "CreatedAt";
    public const string COLUMN_LAST_EDIT_TIME = "LastEditedAt";

    public const string COLUMN_FILTER_FACTION = "Faction";
    public const string COLUMN_FILTER_MAP = "Map";
    public const string COLUMN_REQUEST_SIGN = "RequestSign";

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

    public const string COLUMN_FAVORITE_PLAYER = "Steam64";

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
                Nullable = true,
                ForeignKeyDeleteBehavior = ConstraintBehavior.SetNull
            },
            new Schema.Column(COLUMN_CLASS, SqlTypes.Enum(Class.None)),
            new Schema.Column(COLUMN_BRANCH, SqlTypes.Enum(Branch.Default)),
            new Schema.Column(COLUMN_TYPE, SqlTypes.Enum<KitType>()),
            new Schema.Column(COLUMN_REQUEST_COOLDOWN, SqlTypes.FLOAT) { Nullable = true },
            new Schema.Column(COLUMN_COST_CREDITS, SqlTypes.INT) { Nullable = true },
            new Schema.Column(COLUMN_COST_PREMIUM, "double") { Nullable = true },
            new Schema.Column(COLUMN_TEAM_LIMIT, SqlTypes.FLOAT) { Nullable = true },
            new Schema.Column(COLUMN_SEASON, SqlTypes.BYTE) { Nullable = true },
            new Schema.Column(COLUMN_DISABLED, SqlTypes.BOOLEAN) { Nullable = true },
            new Schema.Column(COLUMN_WEAPONS, SqlTypes.String(KitEx.WeaponTextMaxCharLimit)) { Nullable = true },
            new Schema.Column(COLUMN_SQUAD_LEVEL, SqlTypes.Enum<SquadLevel>()) { Nullable = true },
            new Schema.Column(COLUMN_MAPS_WHITELIST, SqlTypes.BOOLEAN) { Nullable = true },
            new Schema.Column(COLUMN_FACTIONS_WHITELIST, SqlTypes.BOOLEAN) { Nullable = true },
            new Schema.Column(COLUMN_CREATOR, SqlTypes.STEAM_64) { Nullable = true },
            new Schema.Column(COLUMN_LAST_EDITOR, SqlTypes.STEAM_64) { Nullable = true },
            new Schema.Column(COLUMN_CREATION_TIME, SqlTypes.DATETIME) { Nullable = true },
            new Schema.Column(COLUMN_LAST_EDIT_TIME, SqlTypes.DATETIME) { Nullable = true },
        }, true, typeof(Kit)),
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
            new Schema.Column(COLUMN_ITEM_AMOUNT, SqlTypes.BYTE) { Nullable = true },
            new Schema.Column(COLUMN_ITEM_METADATA, SqlTypes.Binary(KitEx.MaxStateArrayLimit)) { Nullable = true },
        }, false, typeof(IKitItem)),
        UnlockRequirement.GetDefaultSchema(TABLE_UNLOCK_REQUIREMENTS, COLUMN_EXT_PK, TABLE_MAIN, COLUMN_PK),
        Skillset.GetDefaultSchema(TABLE_SKILLSETS, COLUMN_EXT_PK, TABLE_MAIN, COLUMN_PK),
        F.GetForeignKeyListSchema(TABLE_FACTION_FILTER, COLUMN_EXT_PK, COLUMN_FILTER_FACTION, TABLE_MAIN, COLUMN_PK, FactionInfo.TABLE_MAIN, FactionInfo.COLUMN_PK),
        F.GetForeignKeyListSchema(TABLE_MAP_FILTER, COLUMN_EXT_PK, COLUMN_FILTER_MAP, TABLE_MAIN, COLUMN_PK, FactionInfo.TABLE_MAIN, FactionInfo.COLUMN_PK),
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
        }, false, null),
        F.GetForeignKeyListSchema(TABLE_REQUEST_SIGNS, COLUMN_EXT_PK, COLUMN_REQUEST_SIGN, TABLE_MAIN, COLUMN_PK, StructureSaver.TABLE_MAIN, StructureSaver.COLUMN_PK),
        F.GetListSchema<ulong>(TABLE_FAVORITES, COLUMN_EXT_PK, COLUMN_FAVORITE_PLAYER, TABLE_MAIN, COLUMN_PK)
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
        object[] objs = new object[hasPk ? 20 : 19];
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
        objs[11] = item.CreditCost == 0 ? DBNull.Value : item.CreditCost;
        objs[12] = item.Type is KitType.Public or KitType.Special ? DBNull.Value : item.PremiumCost;
        objs[13] = item.MapFilterIsWhitelist;
        objs[14] = item.FactionFilterIsWhitelist;
        objs[15] = item.Creator;
        objs[16] = item.LastEditor;
        objs[17] = item.CreatedTimestamp.UtcDateTime;
        objs[18] = item.LastEditedTimestamp.UtcDateTime;
        if (hasPk)
            objs[19] = pk.Key;
        await Sql.QueryAsync(F.BuildInitialInsertQuery(TABLE_MAIN, COLUMN_PK, hasPk, null, null,
                COLUMN_KIT_ID, COLUMN_FACTION, COLUMN_CLASS, COLUMN_BRANCH,
                COLUMN_TYPE, COLUMN_REQUEST_COOLDOWN, COLUMN_TEAM_LIMIT, COLUMN_SEASON,
                COLUMN_DISABLED, COLUMN_WEAPONS, COLUMN_SQUAD_LEVEL, COLUMN_COST_CREDITS,
                COLUMN_COST_PREMIUM, COLUMN_MAPS_WHITELIST, COLUMN_FACTIONS_WHITELIST, COLUMN_CREATOR,
                COLUMN_LAST_EDITOR, COLUMN_CREATION_TIME, COLUMN_LAST_EDIT_TIME),
            objs, reader =>
            {
                pk2 = reader.GetInt32(0);
            }, token).ConfigureAwait(false);
        if (pk2 >= 0)
            item.PrimaryKey = pk2;
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
                if (item2 is not IBaseItem && item2 is not IAssetRedirect || item2 is not IItemJar && item2 is not IClothingJar)
                {
                    L.LogWarning("Item in kit \"" + item.Id + "\" (" + item2 + ") not a valid type: " + item2.GetType().Name + ".");
                    continue;
                }

                one = true;
                int index2 = i * 10;
                IItem? itemObj = item2 as IItem;
                IBaseItem? baseObj = item2 as IBaseItem;
                objs[index2] = pk2;
                objs[index2 + 1] = baseObj != null ? baseObj.Item.ToString("N") : DBNull.Value;
                if (item2 is IItemJar jarObj)
                {
                    objs[index2 + 2] = jarObj.X;
                    objs[index2 + 3] = jarObj.Y;
                    objs[index2 + 4] = jarObj.Rotation % 4;
                    objs[index2 + 5] = jarObj.Page.ToString();
                }
                else
                    objs[index2 + 2] = objs[index2 + 3] = objs[index2 + 4] = objs[index2 + 5] = DBNull.Value;

                objs[index2 + 6] = item2 is IClothingJar jar ? jar.Type.ToString() : DBNull.Value;
                objs[index2 + 7] = item2 is IAssetRedirect redirObj ? redirObj.RedirectType.ToString() : DBNull.Value;
                objs[index2 + 8] = itemObj != null ? itemObj.Amount : DBNull.Value;
                objs[index2 + 9] = baseObj != null ? baseObj.State : DBNull.Value;
                F.AppendPropertyList(builder, index2, 10);
            }
            builder.Append(';');
            if (one)
                await Sql.NonQueryAsync(builder.ToString(), objs, token).ConfigureAwait(false);
            builder.Clear();
        }

        objs = new object[
            (item.UnlockRequirements != null ? item.UnlockRequirements.Length * 2 : 0) +
            (item.Skillsets != null ? item.Skillsets.Length * 3 : 0) +
            (item.FactionFilter != null ? item.FactionFilter.Length * 2 : 0) +
            (item.MapFilter != null ? item.MapFilter.Length * 2 : 0) +
            (item.RequestSigns != null ? item.RequestSigns.Length * 2 : 0) +
            (item.SignText != null ? item.SignText.Count * 3 : 0)];
        int index = 0;
        if (item.UnlockRequirements is { Length: > 0 })
        {
            builder.Append($"INSERT INTO `{TABLE_UNLOCK_REQUIREMENTS}` ({SqlTypes.ColumnList(
                COLUMN_EXT_PK, UnlockRequirement.COLUMN_JSON)}) VALUES ");
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
                objs[index] = pk2;
                objs[index + 1] = json;
                F.AppendPropertyList(builder, index, 2, i);
                index += 2;
            }
            builder.Append("; ");
        }

        if (item.Skillsets is { Length: > 0 })
        {
            builder.Append($"INSERT INTO `{TABLE_SKILLSETS}` ({SqlTypes.ColumnList(
                COLUMN_EXT_PK, Skillset.COLUMN_SKILL, Skillset.COLUMN_LEVEL)}) VALUES ");
            for (int i = 0; i < item.Skillsets.Length; ++i)
            {
                Skillset set = item.Skillsets[i];
                if (set.Speciality is not EPlayerSpeciality.DEFENSE and not EPlayerSpeciality.OFFENSE and not EPlayerSpeciality.SUPPORT)
                    continue;
                objs[index] = pk2;
                objs[index + 1] = set.Speciality switch
                    { EPlayerSpeciality.DEFENSE => set.Defense.ToString(), EPlayerSpeciality.OFFENSE => set.Offense.ToString(), _ => set.Support.ToString() };
                objs[index + 2] = set.Level;
                F.AppendPropertyList(builder, index, 3, i);
                index += 3;
            }
            builder.Append("; ");
        }
        if (item.FactionFilter is { Length: > 0 })
        {
            builder.Append($"INSERT INTO `{TABLE_FACTION_FILTER}` ({SqlTypes.ColumnList(
                COLUMN_EXT_PK, COLUMN_FILTER_FACTION)}) VALUES ");
            for (int i = 0; i < item.FactionFilter.Length; ++i)
            {
                PrimaryKey f = item.FactionFilter[i];
                if (!f.IsValid)
                    continue;
                objs[index] = pk2;
                objs[index + 1] = f.Key;
                F.AppendPropertyList(builder, index, 2, i);
                index += 2;
            }
            builder.Append("; ");
        }
        if (item.MapFilter is { Length: > 0 })
        {
            builder.Append($"INSERT INTO `{TABLE_MAP_FILTER}` ({SqlTypes.ColumnList(
                COLUMN_EXT_PK, COLUMN_FILTER_MAP)}) VALUES ");
            for (int i = 0; i < item.MapFilter.Length; ++i)
            {
                PrimaryKey f = item.MapFilter[i];
                if (!f.IsValid)
                    continue;
                objs[index] = pk2;
                objs[index + 1] = f.Key;
                F.AppendPropertyList(builder, index, 2, i);
                index += 2;
            }
            builder.Append("; ");
        }
        if (item.RequestSigns is { Length: > 0 })
        {
            builder.Append($"INSERT INTO `{TABLE_REQUEST_SIGNS}` ({SqlTypes.ColumnList(
                COLUMN_EXT_PK, COLUMN_REQUEST_SIGN)}) VALUES ");
            for (int i = 0; i < item.RequestSigns.Length; ++i)
            {
                PrimaryKey f = item.RequestSigns[i];
                if (!f.IsValid)
                    continue;
                objs[index] = pk2;
                objs[index + 1] = f.Key;
                F.AppendPropertyList(builder, index, 2, i);
                index += 2;
            }
            builder.Append("; ");
        }
        if (item.SignText is { Count: > 0 })
        {
            builder.Append($"INSERT INTO `{TABLE_SIGN_TEXT}` ({SqlTypes.ColumnList(
                COLUMN_EXT_PK, F.COLUMN_LANGUAGE, F.COLUMN_VALUE)}) VALUES ");
            int i = 0;
            foreach (KeyValuePair<string, string> pair in item.SignText)
            {
                objs[index] = pk2;
                objs[index + 1] = pair.Key;
                objs[index + 2] = pair.Value;
                F.AppendPropertyList(builder, index, 3, i++);
                index += 3;
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
        await Sql.QueryAsync(F.BuildSelectWhereLimit1(TABLE_MAIN, COLUMN_PK, 0, COLUMN_KIT_ID, COLUMN_FACTION,
                COLUMN_CLASS, COLUMN_BRANCH, COLUMN_TYPE, COLUMN_REQUEST_COOLDOWN, COLUMN_TEAM_LIMIT,
                COLUMN_SEASON, COLUMN_DISABLED, COLUMN_WEAPONS, COLUMN_SQUAD_LEVEL, COLUMN_COST_CREDITS,
                COLUMN_COST_PREMIUM, COLUMN_MAPS_WHITELIST, COLUMN_FACTIONS_WHITELIST, COLUMN_CREATOR,
                COLUMN_LAST_EDITOR, COLUMN_CREATION_TIME, COLUMN_LAST_EDIT_TIME)
            , pkObjs, reader =>
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
            await Sql.QueryAsync($"SELECT {SqlTypes.ColumnList(COLUMN_FILTER_FACTION)} " +
                                 $"FROM `{TABLE_FACTION_FILTER}`;", null, reader =>
            {
                int faction = reader.GetInt32(0);
                PrimaryKey[]? arr = obj.FactionFilter;
                Util.AddToArray(ref arr, faction);
                obj.FactionFilter = arr!;
            }, token).ConfigureAwait(false);
            await Sql.QueryAsync($"SELECT {SqlTypes.ColumnList(COLUMN_FILTER_MAP)} " +
                                 $"FROM `{TABLE_MAP_FILTER}`;", null, reader =>
            {
                int faction = reader.GetInt32(0);
                PrimaryKey[]? arr = obj.MapFilter;
                Util.AddToArray(ref arr, faction);
                obj.MapFilter = arr!;
            }, token).ConfigureAwait(false);
            await Sql.QueryAsync($"SELECT {SqlTypes.ColumnList(COLUMN_REQUEST_SIGN)} " +
                                 $"FROM `{TABLE_REQUEST_SIGNS}`;", null, reader =>
            {
                int structure = reader.GetInt32(0);
                PrimaryKey[]? arr = obj.RequestSigns;
                Util.AddToArray(ref arr, structure);
                obj.RequestSigns = arr!;
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
        await Sql.QueryAsync(F.BuildSelect(TABLE_MAIN, COLUMN_PK, COLUMN_KIT_ID, COLUMN_FACTION,
            COLUMN_CLASS, COLUMN_BRANCH, COLUMN_TYPE, COLUMN_REQUEST_COOLDOWN, COLUMN_TEAM_LIMIT,
            COLUMN_SEASON, COLUMN_DISABLED, COLUMN_WEAPONS, COLUMN_SQUAD_LEVEL, COLUMN_COST_CREDITS,
            COLUMN_COST_PREMIUM, COLUMN_MAPS_WHITELIST, COLUMN_FACTIONS_WHITELIST, COLUMN_CREATOR,
            COLUMN_LAST_EDITOR, COLUMN_CREATION_TIME, COLUMN_LAST_EDIT_TIME),
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
        await Sql.QueryAsync($"SELECT {SqlTypes.ColumnList(COLUMN_EXT_PK, COLUMN_FILTER_FACTION)} " +
                             $"FROM `{TABLE_FACTION_FILTER}`;", null, reader =>
        {
            int pk = reader.GetInt32(0);
            for (int i = 0; i < list.Count; ++i)
            {
                if (list[i].PrimaryKey.Key == pk)
                {
                    int faction = reader.GetInt32(1);
                    PrimaryKey[]? arr = list[i].FactionFilter;
                    Util.AddToArray(ref arr, faction);
                    list[i].FactionFilter = arr!;
                    break;
                }
            }
        }, token).ConfigureAwait(false);
        await Sql.QueryAsync($"SELECT {SqlTypes.ColumnList(COLUMN_EXT_PK, COLUMN_FILTER_MAP)} " +
                             $"FROM `{TABLE_MAP_FILTER}`;", null, reader =>
        {
            int pk = reader.GetInt32(0);
            for (int i = 0; i < list.Count; ++i)
            {
                if (list[i].PrimaryKey.Key == pk)
                {
                    int faction = reader.GetInt32(1);
                    PrimaryKey[]? arr = list[i].MapFilter;
                    Util.AddToArray(ref arr, faction);
                    list[i].MapFilter = arr!;
                    break;
                }
            }
        }, token).ConfigureAwait(false);
        await Sql.QueryAsync($"SELECT {SqlTypes.ColumnList(COLUMN_EXT_PK, COLUMN_REQUEST_SIGN)} " +
                             $"FROM `{TABLE_REQUEST_SIGNS}`;", null, reader =>
        {
            int pk = reader.GetInt32(0);
            for (int i = 0; i < list.Count; ++i)
            {
                if (list[i].PrimaryKey.Key == pk)
                {
                    int faction = reader.GetInt32(1);
                    PrimaryKey[]? arr = list[i].RequestSigns;
                    Util.AddToArray(ref arr, faction);
                    list[i].RequestSigns = arr!;
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
        KitType type = reader.ReadStringEnum<KitType>(colOffset + 5) ?? throw new FormatException("Invalid type: \"" + reader.GetString(colOffset + 5) + "\".");
        return new Kit
        {
            Type = type,
            Branch = reader.ReadStringEnum<Branch>(colOffset + 4) ?? throw new FormatException("Invalid branch: \"" + reader.GetString(colOffset + 4) + "\"."),
            SquadLevel = reader.IsDBNull(colOffset + 11)
                ? SquadLevel.Member
                : reader.ReadStringEnum<SquadLevel>(colOffset + 11) ?? throw new FormatException("Invalid squad level: \"" + reader.GetString(colOffset + 11) + "\"."),
            PrimaryKey = colOffset >= 0 ? reader.GetInt32(colOffset + 0) : PrimaryKey.NotAssigned,
            Id = id,
            FactionKey = reader.IsDBNull(colOffset + 2) ? -1 : reader.GetInt32(colOffset + 2),
            Class = @class,
            RequestCooldown = reader.IsDBNull(colOffset + 6) ? 0f : reader.GetFloat(colOffset + 6),
            TeamLimit = reader.IsDBNull(colOffset + 7) ? GetDefaultTeamLimit(@class) : reader.GetFloat(colOffset + 7),
            Season = reader.IsDBNull(colOffset + 8) ? UCWarfare.Season : reader.GetByte(colOffset + 8),
            Disabled = !reader.IsDBNull(colOffset + 9) && reader.GetBoolean(colOffset + 9),
            WeaponText = reader.IsDBNull(colOffset + 10) ? null : reader.GetString(colOffset + 10),
            CreditCost = reader.IsDBNull(colOffset + 12) ? 0 : reader.GetInt32(colOffset + 12),
            PremiumCost = type is KitType.Public or KitType.Special || reader.IsDBNull(colOffset + 13)
                ? 0
                : type is KitType.Loadout
                    ? decimal.Round(UCWarfare.Config.LoadoutCost, 2)
                    : new decimal(Math.Round(reader.GetDouble(colOffset + 13), 2)),
            MapFilterIsWhitelist = !reader.IsDBNull(colOffset + 14) && reader.GetBoolean(colOffset + 14), 
            FactionFilterIsWhitelist = !reader.IsDBNull(colOffset + 15) && reader.GetBoolean(colOffset + 15),
            Creator = reader.IsDBNull(colOffset + 16) ? 0ul : reader.GetUInt64(colOffset + 16),
            LastEditor = reader.IsDBNull(colOffset + 17) ? 0ul : reader.GetUInt64(colOffset + 17),
            CreatedTimestamp = reader.IsDBNull(colOffset + 18)
                ? DateTimeOffset.MinValue
                : reader.GetDateTimeOffset(colOffset + 18),
            LastEditedTimestamp = reader.IsDBNull(colOffset + 19)
                ? DateTimeOffset.MinValue
                : reader.GetDateTimeOffset(colOffset + 19),
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
#if DEBUG // Migrating old kits
    public async Task MigrateOldKits(CancellationToken token = default)
    {
        Dictionary<int, OldKit> kits = new Dictionary<int, OldKit>(256);
        int rows = 0;
        // todo actually import loadouts later, too much work rn
        await Sql.QueryAsync("SELECT * FROM `kit_data` WHERE `IsLoadout` != 1;", null, reader =>
        {
            OldKit kit = new OldKit
            {
                PrimaryKey = reader.GetInt32(0),
                Name = reader.GetString(1),
                Class = (Class)reader.GetInt32(2),
                Branch = (Branch)reader.GetInt32(3),
                Team = reader.GetUInt64(4),
                CreditCost = reader.GetUInt16(5),
                UnlockLevel = reader.GetUInt16(6),
                IsPremium = reader.GetBoolean(7),
                PremiumCost = reader.GetFloat(8),
                IsLoadout = reader.GetBoolean(9),
                TeamLimit = reader.GetFloat(10),
                Cooldown = reader.GetInt32(11),
                Disabled = reader.GetBoolean(12),
                Weapons = reader.GetString(13),
                SquadLevel = (SquadLevel)reader.GetByte(14)
            };
            kits.Add(kit.PrimaryKey.Key, kit);
            ++rows;
        }, token).ConfigureAwait(false);
        L.Log($"kit_data: Read {rows} row(s).");
        rows = 0;
        await Sql.QueryAsync("SELECT * FROM `kit_items`;", Array.Empty<object>(), reader =>
        {
            int kitPk = reader.GetInt32(1);
            if (kits.TryGetValue(kitPk, out OldKit kit))
            {
                PageItem item = new PageItem
                {
                    Item = reader.ReadGuid(2),
                    X = reader.GetByte(3),
                    Y = reader.GetByte(4),
                    Rotation = reader.GetByte(5),
                    Page = (Page)reader.GetByte(6),
                    Amount = reader.GetByte(7),
                    State = reader.ReadByteArray(8)
                };
                (kit.Items ??= new List<PageItem>(32)).Add(item);
                ++rows;
            }
        }, token).ConfigureAwait(false);
        L.Log($"kit_items: Read {rows} row(s).");
        rows = 0;
        await Sql.QueryAsync("SELECT * FROM `kit_clothes`;", Array.Empty<object>(), reader =>
        {
            int kitPk = reader.GetInt32(1);
            if (kits.TryGetValue(kitPk, out OldKit kit))
            {
                ClothingItem clothing = new ClothingItem()
                {
                    Item = reader.ReadGuid(2),
                    Type = (ClothingType)reader.GetByte(3)
                };
                (kit.Clothes ??= new List<ClothingItem>(7)).Add(clothing);
                ++rows;
            }
        }, token).ConfigureAwait(false);
        L.Log($"kit_clothes: Read {rows} row(s).");
        rows = 0;
        await Sql.QueryAsync("SELECT * FROM `kit_skillsets`;", Array.Empty<object>(), reader =>
        {
            int kitPk = reader.GetInt32(1);
            if (kits.TryGetValue(kitPk, out OldKit kit))
            {
                EPlayerSpeciality type = (EPlayerSpeciality)reader.GetByte(2);
                Skillset set;
                byte v = reader.GetByte(3);
                byte lvl = reader.GetByte(4);
                bool def = false;
                switch (type)
                {
                    case EPlayerSpeciality.OFFENSE:
                        set = new Skillset((EPlayerOffense)v, lvl);
                        break;
                    case EPlayerSpeciality.DEFENSE:
                        set = new Skillset((EPlayerDefense)v, lvl);
                        break;
                    case EPlayerSpeciality.SUPPORT:
                        set = new Skillset((EPlayerSupport)v, lvl);
                        break;
                    default:
                        set = default;
                        def = true;
                        break;
                }
                if (!def)
                {
                    (kit.Skillsets ??= new List<Skillset>(1)).Add(set);
                    ++rows;
                }
                else
                    L.LogWarning("Invalid skillset for kit " + kitPk.ToString(Data.AdminLocale) + ".");
            }
        }, token).ConfigureAwait(false);
        L.Log($"kit_skillsets: Read {rows} row(s).");
        rows = 0;
        await Sql.QueryAsync("SELECT `Kit`, `Language`, `Text` FROM `kit_lang`;", Array.Empty<object>(), reader =>
        {
            int kitPk = reader.GetInt32(0);
            if (kits.TryGetValue(kitPk, out OldKit kit))
            {
                string lang = reader.GetString(1);
                kit.SignText ??= new TranslationList(1);
                if (!kit.SignText.ContainsKey(lang))
                {
                    kit.SignText.Add(lang, reader.GetString(2));
                    ++rows;
                }
                else
                    L.LogWarning("Duplicate translation for kit " + kit.Name + " (" + kit.PrimaryKey + ") for language " + lang);
            }
        }, token).ConfigureAwait(false);
        L.Log($"kit_lang: Read {rows} row(s).");
        rows = 0;
        await Sql.QueryAsync("SELECT `Kit`, `JSON` FROM `kit_unlock_requirements`;", Array.Empty<object>(), reader =>
        {
            int kitPk = reader.GetInt32(0);
            if (kits.TryGetValue(kitPk, out OldKit kit))
            {
                Utf8JsonReader jsonReader = new Utf8JsonReader(System.Text.Encoding.UTF8.GetBytes(reader.GetString(1)));
                UnlockRequirement? req = UnlockRequirement.Read(ref jsonReader);
                if (req != null)
                {
                    (kit.UnlockRequirements ??= new List<UnlockRequirement>(1)).Add(req);
                    ++rows;
                }
            }
        }, token);
        L.Log($"kit_unlock_requirements: Read {rows} row(s).");
        rows = 0;
        await Sql.QueryAsync("SELECT `Kit`, `Steam64`, `AccessType` FROM `kit_access`;", Array.Empty<object>(), reader =>
        {
            int kitPk = reader.GetInt32(0);
            if (kits.TryGetValue(kitPk, out OldKit kit))
            {
                string type = reader.GetString(2);
                if (!Enum.TryParse(type, true, out KitAccessType etype))
                {
                    etype = type.Equals("QUEST_REWARD", StringComparison.OrdinalIgnoreCase) ? KitAccessType.QuestReward : KitAccessType.Unknown;
                }
                ulong s64 = reader.GetUInt64(1);
                (kit.Access ??= new List<AccessRow>(1)).Add(new AccessRow(s64, etype));
                ++rows;
            }
        }, token);
        L.Log($"kit_access: Read {rows} row(s).");
        rows = 0;
        List<KeyValuePair<Kit, List<AccessRow>?>> newKits = new List<KeyValuePair<Kit, List<AccessRow>?>>(kits.Count);
        foreach (OldKit kit in kits.Values)
        {
            string id = kit.Name!;
            FactionInfo? faction = null;
            if (!kit.IsLoadout)
            {
                if (id.StartsWith("prem_"))
                    id = id.Substring(5);
                if (id.StartsWith("me", StringComparison.OrdinalIgnoreCase))
                    faction = TeamManager.FindFactionInfo(FactionInfo.MEC);
                else if (id.StartsWith("us", StringComparison.OrdinalIgnoreCase))
                    faction = TeamManager.FindFactionInfo(FactionInfo.USA);
                else if (id.StartsWith("ru", StringComparison.OrdinalIgnoreCase))
                    faction = TeamManager.FindFactionInfo(FactionInfo.Russia);
                else if (id.StartsWith("ge", StringComparison.OrdinalIgnoreCase))
                    faction = TeamManager.FindFactionInfo(FactionInfo.Germany);
                else if (id.StartsWith("usmc", StringComparison.OrdinalIgnoreCase))
                    faction = TeamManager.FindFactionInfo(FactionInfo.USMC);
                else if (id.StartsWith("sov", StringComparison.OrdinalIgnoreCase))
                    faction = TeamManager.FindFactionInfo(FactionInfo.Soviet);
                else if (id.StartsWith("pl", StringComparison.OrdinalIgnoreCase))
                    faction = TeamManager.FindFactionInfo(FactionInfo.Poland);
                else if (id.StartsWith("mi", StringComparison.OrdinalIgnoreCase))
                    faction = TeamManager.FindFactionInfo(FactionInfo.Militia);
                else if (id.StartsWith("idf", StringComparison.OrdinalIgnoreCase))
                    faction = TeamManager.FindFactionInfo(FactionInfo.Israel);
                else if (id.StartsWith("fr", StringComparison.OrdinalIgnoreCase))
                    faction = TeamManager.FindFactionInfo(FactionInfo.France);
                else if (id.StartsWith("caf", StringComparison.OrdinalIgnoreCase))
                    faction = TeamManager.FindFactionInfo(FactionInfo.Canada);
                else if (id.StartsWith("sa", StringComparison.OrdinalIgnoreCase))
                    faction = TeamManager.FindFactionInfo(FactionInfo.SouthAfrica);
                else if (id.StartsWith("mz", StringComparison.OrdinalIgnoreCase))
                    faction = TeamManager.FindFactionInfo(FactionInfo.Mozambique);
            }

            Kit newKit = new Kit(kit.Name!, kit.Class == Class.None ? Class.Unarmed : kit.Class, kit.Branch == Branch.Default ? GetDefaultBranch(kit.Class) : kit.Branch,
                kit.IsPremium
                    ? (kit.PremiumCost < 0
                        ? KitType.Special
                        : KitType.Elite)
                    : (kit.IsLoadout
                        ? KitType.Loadout
                        : KitType.Public),
                kit.SquadLevel, faction)
            {
                PrimaryKey = kit.PrimaryKey,
                CreditCost = kit.CreditCost,
                Disabled = kit.Disabled,
                Season = kit.Disabled ? 1 : 2,
                WeaponText = kit.Weapons,
                TeamLimit = kit.TeamLimit,
                PremiumCost = kit.PremiumCost < 0 ? 0m : decimal.Round((decimal)kit.PremiumCost, 2),
                RequestCooldown = kit.Cooldown,
                CreatedTimestamp = DateTimeOffset.MinValue
            };
            if (!kit.IsLoadout && faction != null)
            {
                if (newKit.Type == KitType.Public)
                {
                    newKit.FactionFilter = new PrimaryKey[]
                    {
                        faction.PrimaryKey
                    };
                    newKit.FactionFilterIsWhitelist = true;
                }
                else if (faction.FactionId.Equals(FactionInfo.USA, StringComparison.OrdinalIgnoreCase) ||
                         faction.FactionId.Equals(FactionInfo.Canada, StringComparison.OrdinalIgnoreCase) ||
                         faction.FactionId.Equals(FactionInfo.USMC, StringComparison.OrdinalIgnoreCase))
                {
                    newKit.FactionFilter = new PrimaryKey[]
                    {
                        TeamManager.FindFactionInfo(FactionInfo.Russia)!.PrimaryKey,
                        TeamManager.FindFactionInfo(FactionInfo.MEC)!.PrimaryKey
                    };
                    newKit.FactionFilterIsWhitelist = false;
                }
                else if (faction.FactionId.Equals(FactionInfo.Russia, StringComparison.OrdinalIgnoreCase) ||
                         faction.FactionId.Equals(FactionInfo.Soviet, StringComparison.OrdinalIgnoreCase) ||
                         faction.FactionId.Equals(FactionInfo.MEC, StringComparison.OrdinalIgnoreCase))
                {
                    newKit.FactionFilter = new PrimaryKey[]
                    {
                        TeamManager.FindFactionInfo(FactionInfo.USA)!.PrimaryKey
                    };
                    newKit.FactionFilterIsWhitelist = false;
                }
            }
            if (kit.Skillsets != null)
                newKit.Skillsets = kit.Skillsets.ToArray();
            if (kit.UnlockRequirements != null)
                newKit.UnlockRequirements = kit.UnlockRequirements.ToArray();
            List<IKitItem> items = new List<IKitItem>((kit.Items != null ? kit.Items.Count : 0) + (kit.Clothes != null ? kit.Clothes.Count : 0));
            if (kit.Items != null)
            {
                foreach (PageItem item in kit.Items)
                {
                    RedirectType? t = item.LegacyRedirect;
                    if (t.HasValue)
                    {
                        items.Add(new AssetRedirectItem(t.Value, item.X, item.Y, item.Rotation, item.Page));
                    }
                    else
                    {
                        items.Add(item);
                    }
                }
            }
            if (kit.Clothes != null)
            {
                foreach (ClothingItem item in kit.Clothes)
                {
                    RedirectType? t = item.LegacyRedirect;
                    if (t.HasValue)
                    {
                        items.Add(new AssetRedirectClothing(t.Value, item.Type));
                    }
                    else
                    {
                        items.Add(item);
                    }
                }
            }
            newKit.Items = items.ToArray();
            if (kit.UnlockLevel > 0)
            {
                LevelUnlockRequirement req = new LevelUnlockRequirement { UnlockLevel = kit.UnlockLevel };
                UnlockRequirement[] reqs = newKit.UnlockRequirements;
                Util.AddToArray(ref reqs!, req);
                newKit.UnlockRequirements = reqs;
            }
            if (kit.SignText != null)
            {
                newKit.SignText = kit.SignText;
            }
            newKits.Add(new KeyValuePair<Kit, List<AccessRow>?>(newKit, kit.Access));
        }
        await WaitAsync(token).ConfigureAwait(false);
        string @base =
            $"INSERT INTO `{TABLE_ACCESS}` ({SqlTypes.ColumnList(COLUMN_EXT_PK, COLUMN_ACCESS_STEAM_64, COLUMN_ACCESS_TYPE)}) " +
            "VALUES ";
        StringBuilder builder = new StringBuilder(@base, 256);
        try
        {
            foreach (KeyValuePair<Kit, List<AccessRow>?> kvp in newKits)
            {
                SqlItem<Kit> res = await AddOrUpdateNoLock(kvp.Key, token).ConfigureAwait(false);
                Kit kit = res.Item!;
                L.Log($"Created kit: " + kit.Id + ".");
                if (kvp.Value is { Count: > 0 } list)
                {
                    int pk = kit.PrimaryKey.Key;
                    if (list.Count == 1)
                    {
                        AccessRow row = list[0];
                        if (await AddAccessRow(pk, row.Steam64, row.Type == KitAccessType.Unknown
                                ? (kit.Type switch
                                {
                                    KitType.Public => KitAccessType.Credits,
                                    KitType.Loadout or KitType.Elite => KitAccessType.Purchase,
                                    _ => KitAccessType.Event
                                })
                                : row.Type, token).ConfigureAwait(false))
                            L.Log($"Added 1 row to {kit.Id}'s access list.");
                    }
                    else
                    {
                        object[] objs = new object[list.Count * 3];
                        for (int i = 0; i < list.Count; ++i)
                        {
                            AccessRow row = list[i];
                            int index = i * 3;
                            objs[index] = pk;
                            objs[index + 1] = row.Steam64;
                            objs[index + 2] = row.Type == KitAccessType.Unknown
                                ? (kit.Type switch
                                {
                                    KitType.Public => KitAccessType.Credits,
                                    KitType.Loadout or KitType.Elite => KitAccessType.Purchase,
                                    _ => KitAccessType.Event
                                })
                                : row.Type;
                            F.AppendPropertyList(builder, index, 3);
                        }
                        builder.Append(';');
                        rows = await Data.AdminSql.NonQueryAsync(builder.ToString(), objs, token).ConfigureAwait(false);
                        builder.Clear();
                        builder.Append(@base);
                        L.Log($"Added {rows} rows to {kit.Id}'s access list.");
                    }
                }
            }
        }
        finally
        {
            Release();
        }
    }

    private class OldKit
    {
        public PrimaryKey PrimaryKey = PrimaryKey.NotAssigned;
        public string? Name;
        public Class Class;
        public Branch Branch;
        public ulong Team;
        public List<UnlockRequirement>? UnlockRequirements;
        public List<Skillset>? Skillsets;
        public ushort CreditCost;
        public ushort UnlockLevel;
        public bool IsPremium;
        public float PremiumCost;
        public bool IsLoadout;
        public float TeamLimit;
        public float Cooldown;
        public bool Disabled;
        public SquadLevel SquadLevel;
        public List<PageItem>? Items;
        public List<ClothingItem>? Clothes;
        public List<AccessRow>? Access;
        public TranslationList? SignText;
        public string Weapons;
    }

    private readonly struct AccessRow
    {
        public readonly ulong Steam64;
        public readonly KitAccessType Type;
        public AccessRow(ulong steam64, KitAccessType type)
        {
            Steam64 = steam64;
            Type = type;
        }
    }
#endif
}

public delegate void KitAccessCallback(SqlItem<Kit> kit, ulong player, bool newAccess, KitAccessType newType);
public delegate void KitChanged(UCPlayer player, SqlItem<Kit>? kit, SqlItem<Kit>? oldKit);

public static class KitEx
{
    public const int BranchMaxCharLimit = 16;
    public const int ClothingMaxCharLimit = 16;
    public const int ClassMaxCharLimit = 20;
    public const int TypeMaxCharLimit = 16;
    public const int RedirectTypeCharLimit = 20;
    public const int SquadLevelMaxCharLimit = 16;
    public const int KitNameMaxCharLimit = 25;
    public const int WeaponTextMaxCharLimit = 50;
    public const int SignTextMaxCharLimit = 50;
    public const int MaxStateArrayLimit = 18;
    public static void UpdateLastEdited(this Kit kit, ulong player)
    {
        if (Util.IsValidSteam64Id(player))
        {
            kit.LastEditor = player;
            kit.LastEditedTimestamp = DateTimeOffset.UtcNow;
        }
    }
    public static bool ContainsItem(this Kit kit, Guid guid, bool checkClothes = false)
    {
        for (int i = 0; i < kit.Items.Length; ++i)
        {
            if (kit.Items[i] is IItem item)
            {
                if (item.Item == guid)
                    return true;
            }
            else if (checkClothes && kit.Items[i] is IBaseItem clothing)
            {
                if (clothing.Item == guid)
                    return true;
            }
        }
        return false;
    }
    public static int CountItems(this Kit kit, Guid guid, bool checkClothes = false)
    {
        int count = 0;
        for (int i = 0; i < kit.Items.Length; ++i)
        {
            if (kit.Items[i] is IItem item)
            {
                if (item.Item == guid)
                    count++;
            }
            else if (checkClothes && kit.Items[i] is IBaseItem clothing)
            {
                if (clothing.Item == guid)
                    count++;
            }
        }
        return count;
    }

    public static string GetFlagIcon(this FactionInfo? faction)
    {
        if (faction is not { TMProSpriteIndex: { } })
            return "<sprite index=0/>";
        return "<sprite index=" + faction.TMProSpriteIndex.Value.ToString(Data.AdminLocale) + "/>";
    }
    public static char GetIcon(this Class @class)
    {
        if (SquadManager.Config is { Classes: { Length: > 0 } arr })
        {
            int i = (int)@class;
            if (arr.Length > i && arr[i].Class == @class)
                return arr[i].Icon;
            for (i = 0; i < arr.Length; ++i)
            {
                if (arr[i].Class == @class)
                    return arr[i].Icon;
            }
        }

        return @class switch
        {
            Class.Squadleader => '¦',
            Class.Rifleman => '¡',
            Class.Medic => '¢',
            Class.Breacher => '¤',
            Class.AutomaticRifleman => '¥',
            Class.Grenadier => '¬',
            Class.MachineGunner => '«',
            Class.LAT => '®',
            Class.HAT => '¯',
            Class.Marksman => '¨',
            Class.Sniper => '£',
            Class.APRifleman => '©',
            Class.CombatEngineer => 'ª',
            Class.Crewman => '§',
            Class.Pilot => '°',
            Class.SpecOps => '×',
            _ => '±'
        };
    }
    public static bool IsLimited(this Kit kit, out int currentPlayers, out int allowedPlayers, ulong team, bool requireCounts = false)
    {
#if DEBUG
        using IDisposable profiler = ProfilingUtils.StartTracking();
#endif
        ulong t = team is 1 or 2 ? team : TeamManager.GetTeamNumber(kit.Faction);
        currentPlayers = 0;
        allowedPlayers = Provider.maxPlayers;
        if (!requireCounts && kit.TeamLimit >= 1f)
            return false;
        IEnumerable<UCPlayer> friendlyPlayers = t == 0 ? PlayerManager.OnlinePlayers : PlayerManager.OnlinePlayers.Where(k => k.GetTeam() == t);
        allowedPlayers = Mathf.CeilToInt(kit.TeamLimit * friendlyPlayers.Count());
        currentPlayers = friendlyPlayers.Count(k => k.ActiveKit == kit);
        if (kit.TeamLimit >= 1f)
            return false;
        return currentPlayers + 1 > allowedPlayers;
    }
    public static bool IsClassLimited(this Kit kit, out int currentPlayers, out int allowedPlayers, ulong team, bool requireCounts = false)
    {
#if DEBUG
        using IDisposable profiler = ProfilingUtils.StartTracking();
#endif
        ulong t = team is 1 or 2 ? team : TeamManager.GetTeamNumber(kit.Faction);
        currentPlayers = 0;
        allowedPlayers = Provider.maxPlayers;
        if (!requireCounts && (kit.TeamLimit >= 1f))
            return false;
        IEnumerable<UCPlayer> friendlyPlayers = t == 0 ? PlayerManager.OnlinePlayers : PlayerManager.OnlinePlayers.Where(k => k.GetTeam() == t);
        allowedPlayers = Mathf.CeilToInt(kit.TeamLimit * friendlyPlayers.Count());
        currentPlayers = friendlyPlayers.Count(k => k.KitClass == kit.Class);
        if (kit.TeamLimit >= 1f)
            return false;
        return currentPlayers + 1 > allowedPlayers;
    }
    public static bool TryParseClass(string val, out Class @class)
    {
        if (Enum.TryParse(val, true, out @class))
            return true;
        // checks old values for the enum before renaming.
        if (val.Equals("AUTOMATIC_RIFLEMAN", StringComparison.OrdinalIgnoreCase))
            @class = Class.AutomaticRifleman;
        else if (val.Equals("MACHINE_GUNNER", StringComparison.OrdinalIgnoreCase))
            @class = Class.MachineGunner;
        else if (val.Equals("AP_RIFLEMAN", StringComparison.OrdinalIgnoreCase))
            @class = Class.APRifleman;
        else if (val.Equals("COMBAT_ENGINEER", StringComparison.OrdinalIgnoreCase))
            @class = Class.CombatEngineer;
        else if (val.Equals("SPEC_OPS", StringComparison.OrdinalIgnoreCase))
            @class = Class.SpecOps;
        else
        {
            @class = default;
            return false;
        }

        return true;
    }
    public static class NetCalls
    {
        public const int PlayerHasAccessCode = -4;
        public const int PlayerHasNoAccessCode = -3;

        public static readonly NetCall<ulong, ulong, string, KitAccessType, bool> RequestSetKitAccess = new NetCall<ulong, ulong, string, KitAccessType, bool>(ReceiveSetKitAccess);
        public static readonly NetCall<ulong, ulong, string[], KitAccessType, bool> RequestSetKitsAccess = new NetCall<ulong, ulong, string[], KitAccessType, bool>(ReceiveSetKitsAccess);
        public static readonly NetCallRaw<Kit?> CreateKit = new NetCallRaw<Kit?>(ReceiveCreateKit, Util.ReadIReadWriteObjectNullable<Kit>, Util.WriteIReadWriteObjectNullable);
        public static readonly NetCall<string> RequestKitClass = new NetCall<string>(ReceiveRequestKitClass);
        public static readonly NetCall<string> RequestKit = new NetCall<string>(ReceiveKitRequest);
        public static readonly NetCallRaw<string[]> RequestKits = new NetCallRaw<string[]>(ReceiveKitsRequest, null, null);
        public static readonly NetCall<ulong, ulong, byte, Class, string> RequestCreateLoadout = new NetCall<ulong, ulong, byte, Class, string>(ReceiveCreateLoadoutRequest);
        public static readonly NetCall<string, ulong> RequestKitAccess = new NetCall<string, ulong>(ReceiveKitAccessRequest);
        public static readonly NetCall<string[], ulong> RequestKitsAccess = new NetCall<string[], ulong>(ReceiveKitsAccessRequest);


        public static readonly NetCall<string, Class, string> SendKitClass = new NetCall<string, Class, string>(1114);
        public static readonly NetCallRaw<Kit?> SendKit = new NetCallRaw<Kit?>(1117, Util.ReadIReadWriteObjectNullable<Kit>, Util.WriteIReadWriteObjectNullable);
        public static readonly NetCallRaw<Kit[]> SendKits = new NetCallRaw<Kit[]>(1118, Util.ReadIReadWriteArray<Kit>, Util.WriteIReadWriteArray);
        public static readonly NetCall<string, int> SendAckCreateLoadout = new NetCall<string, int>(1111);
        public static readonly NetCall<int[]> SendAckSetKitsAccess = new NetCall<int[]>(1133);
        public static readonly NetCall<byte, byte[]> SendKitsAccess = new NetCall<byte, byte[]>(1137);

        [NetCall(ENetCall.FROM_SERVER, 1100)]
        internal static async Task<StandardErrorCode> ReceiveSetKitAccess(MessageContext context, ulong admin, ulong player, string kit, KitAccessType type, bool state)
        {
            KitManager? manager = KitManager.GetSingletonQuick();
            if (manager == null)
                return StandardErrorCode.GenericError;

            SqlItem<Kit>? proxy = await manager.FindKit(kit).ConfigureAwait(false);
            if (proxy?.Item != null)
            {
                await proxy.Enter().ConfigureAwait(false);
                try
                {
                    if (proxy.Item != null)
                    {
                        await (state ? KitManager.GiveAccess(proxy, player, type) : KitManager.RemoveAccess(proxy, player)).ConfigureAwait(false);
                        ActionLog.Add(ActionLogType.ChangeKitAccess, player.ToString(Data.AdminLocale) + 
                                                                           (state ? (" GIVEN ACCESS TO " + kit + ", REASON: " + type) : 
                                                                           (" DENIED ACCESS TO " + kit + ".")), admin);
                        UCPlayer? onlinePlayer = UCPlayer.FromID(player);
                        if (onlinePlayer != null && onlinePlayer.IsOnline)
                            KitManager.UpdateSigns(proxy.Item, onlinePlayer);
                        return StandardErrorCode.Success;
                    }
                }
                finally
                {
                    proxy.Release();
                }
            }

            return StandardErrorCode.NotFound;
        }
        [NetCall(ENetCall.FROM_SERVER, 1132)]
        internal static async Task ReceiveSetKitsAccess(MessageContext context, ulong admin, ulong player, string[] kits, KitAccessType type, bool state)
        {
            KitManager? manager = KitManager.GetSingletonQuick();
            int[] successes = new int[kits.Length];
            if (manager == null)
            {
                for (int i = 0; i < successes.Length; ++i)
                    successes[i] = (int)StandardErrorCode.Success;
                context.Reply(SendAckSetKitsAccess, successes);
                return;
            }

            await manager.WaitAsync().ConfigureAwait(false);
            try
            {
                for (int i = 0; i < kits.Length; ++i)
                {
                    string kit = kits[i];
                    SqlItem<Kit>? proxy = await manager.FindKit(kit).ConfigureAwait(false);
                    if (proxy?.Item != null)
                    {
                        await (state ? KitManager.GiveAccess(proxy, player, type) : KitManager.RemoveAccess(proxy, player)).ConfigureAwait(false);
                        ActionLog.Add(ActionLogType.ChangeKitAccess, player.ToString(Data.AdminLocale) +
                            (state ? (" GIVEN ACCESS TO " + kit + ", REASON: " + type) :
                                (" DENIED ACCESS TO " + kit + ".")), admin);
                        UCPlayer? onlinePlayer = UCPlayer.FromID(player);
                        if (onlinePlayer != null && onlinePlayer.IsOnline)
                            KitManager.UpdateSigns(proxy.Item, onlinePlayer);
                        continue;
                    }

                    successes[i] = (int)StandardErrorCode.NotFound;
                }
            }
            finally
            {
                manager.Release();
            }
            context.Reply(SendAckSetKitsAccess, successes);
        }
        /// <returns><see cref="PlayerHasAccessCode"/> if the player has access to the kit, <see cref="PlayerHasNoAccessCode"/> if they don't,<br/>
        /// <see cref="KitNotFoundErrorCode"/> if the kit isn't found, and <see cref="MessageContext.CODE_GENERIC_FAILURE"/> if <see cref="KitManager"/> isn't loaded.</returns>
        [NetCall(ENetCall.FROM_SERVER, 1134)]
        private static async Task<int> ReceiveKitAccessRequest(MessageContext context, string kit, ulong player)
        {
            KitManager? manager = KitManager.GetSingletonQuick();

            if (manager == null)
                return (int)StandardErrorCode.GenericError;
            SqlItem<Kit>? proxy = await manager.FindKit(kit).ConfigureAwait(false);
            if (proxy?.Item == null)
                return (int)StandardErrorCode.NotFound;

            return await KitManager.HasAccess(proxy.LastPrimaryKey, player).ConfigureAwait(false) ? PlayerHasAccessCode : PlayerHasNoAccessCode;
        }

        [NetCall(ENetCall.FROM_SERVER, 1136)]
        private static async Task ReceiveKitsAccessRequest(MessageContext context, string[] kits, ulong player)
        {
            KitManager? manager = KitManager.GetSingletonQuick();

            byte[] outp = new byte[kits.Length];
            if (manager == null)
            {
                for (int i = 0; i < outp.Length; ++i)
                    outp[i] = (int)StandardErrorCode.GenericError;
                context.Reply(SendKitsAccess, (byte)StandardErrorCode.GenericError, outp);
                return;
            }
            for (int i = 0; i < kits.Length; ++i)
            {
                SqlItem<Kit>? proxy = await manager.FindKit(kits[i]).ConfigureAwait(false);
                if (proxy?.Item == null)
                    outp[i] = (int)StandardErrorCode.NotFound;
                else outp[i] = (byte)(await KitManager.HasAccess(proxy.LastPrimaryKey, player).ConfigureAwait(false) ? PlayerHasAccessCode : PlayerHasNoAccessCode);
            }
            context.Reply(SendKitsAccess, (byte)StandardErrorCode.Success, outp);
        }

        [NetCall(ENetCall.FROM_SERVER, 1109)]
        internal static async Task<StandardErrorCode> ReceiveCreateKit(MessageContext context, Kit? kit)
        {
            KitManager? manager = KitManager.GetSingletonQuick();
            if (manager == null || kit == null)
                return StandardErrorCode.GenericError;
            await manager.AddOrUpdate(kit);
            return StandardErrorCode.Success;
        }

        [NetCall(ENetCall.FROM_SERVER, 1113)]
        internal static async Task ReceiveRequestKitClass(MessageContext context, string kitID)
        {
            KitManager? manager = KitManager.GetSingletonQuick();
            if (manager == null)
                goto bad;
            SqlItem<Kit>? proxy = await manager.FindKit(kitID).ConfigureAwait(false);
            if (proxy?.Item == null)
                goto bad;
            await proxy.Enter().ConfigureAwait(false);
            try
            {
                if (proxy.Item == null)
                    goto bad;
                string signtext = proxy.Item.GetDisplayName();
                context.Reply(SendKitClass, kitID, proxy.Item.Class, signtext);
                return;
            }
            finally
            {
                proxy.Release();
            }
            bad:
            context.Reply(SendKitClass, kitID, Class.None, kitID);
        }

        [NetCall(ENetCall.FROM_SERVER, 1115)]
        internal static async Task ReceiveKitRequest(MessageContext context, string kitID)
        {
            KitManager? manager = KitManager.GetSingletonQuick();
            if (manager == null)
                goto bad;
            SqlItem<Kit>? proxy = await manager.FindKit(kitID).ConfigureAwait(false);
            if (proxy?.Item == null)
                goto bad;
            await proxy.Enter().ConfigureAwait(false);
            try
            {
                if (proxy.Item == null)
                    goto bad;
                context.Reply(SendKit, proxy.Item);
                return;
            }
            finally
            {
                proxy.Release();
            }
            bad:
            context.Reply(SendKit, null);
        }
        [NetCall(ENetCall.FROM_SERVER, 1116)]
        internal static async Task ReceiveKitsRequest(MessageContext context, string[] kitIDs)
        {
            List<Kit> kits = new List<Kit>(kitIDs.Length);
            KitManager? manager = KitManager.GetSingletonQuick();
            if (manager != null)
            {
                await manager.WaitAsync().ConfigureAwait(false);
                try
                {
                    for (int i = 0; i < kitIDs.Length; i++)
                    {
                        SqlItem<Kit>? proxy = manager.FindKitNoLock(kitIDs[i]);
                        if (proxy?.Item != null)
                        {
                            kits.Add(proxy.Item);
                        }
                    }
                }
                finally
                {
                    manager.Release();
                }
            }
            context.Reply(SendKits, kits.ToArray());
        }
        [NetCall(ENetCall.FROM_SERVER, 1110)]
        private static async Task ReceiveCreateLoadoutRequest(MessageContext context, ulong fromPlayer, ulong player, byte team, Class @class, string displayName)
        {
            KitManager? manager = KitManager.GetSingletonQuick();
            if (manager != null)
            {
                (SqlItem<Kit> kit, StandardErrorCode code) = await manager.CreateLoadout(fromPlayer, player, team, @class, displayName);

                context.Reply(SendAckCreateLoadout, kit.Item is null ? string.Empty : kit.Item.Id, (int)code);
            }
            else
            {
                context.Reply(SendAckCreateLoadout, string.Empty, (int)StandardErrorCode.GenericError);
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
    QuestReward
}
