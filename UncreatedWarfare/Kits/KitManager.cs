using SDG.NetTransport;
using SDG.Unturned;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Uncreated.Encoding;
using Uncreated.Framework;
using Uncreated.Networking;
using Uncreated.Networking.Async;
using Uncreated.SQL;
using Uncreated.Warfare.Commands;
using Uncreated.Warfare.Commands.CommandSystem;
using Uncreated.Warfare.Events;
using Uncreated.Warfare.Events.Items;
using Uncreated.Warfare.Events.Players;
using Uncreated.Warfare.Gamemodes;
using Uncreated.Warfare.Gamemodes.Interfaces;
using Uncreated.Warfare.Levels;
using Uncreated.Warfare.Maps;
using Uncreated.Warfare.Players;
using Uncreated.Warfare.Quests;
using Uncreated.Warfare.Singletons;
using Uncreated.Warfare.Squads;
using Uncreated.Warfare.Sync;
using Uncreated.Warfare.Teams;
using UnityEngine;
// ReSharper disable ConstantConditionalAccessQualifier

namespace Uncreated.Warfare.Kits;
// todo add delays to kits
public partial class KitManager : ListSqlSingleton<Kit>, IQuestCompletedHandlerAsync, IPlayerConnectListenerAsync, IPlayerPostInitListenerAsync, IJoinedTeamListenerAsync, IGameTickListener, IPlayerDisconnectListener, ITCPConnectedListener
{
    private static int _v = 0;
    public static readonly KitMenuUI MenuUI = new KitMenuUI();
    private readonly List<Kit> _kitListTemp = new List<Kit>(64);
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
        EventDispatcher.ItemMoved += OnItemMoved;
        EventDispatcher.ItemDropped += OnItemDropped;
        EventDispatcher.ItemPickedUp += OnItemPickedUp;
        EventDispatcher.SwapClothingRequested += OnSwapClothingRequested;
        OnItemsRefreshed += OnItemsRefreshedIntl;
        //await MigrateOldKits(token).ConfigureAwait(false);
        List<SqlItem<Kit>>? dirty = null;
        WriteWait();
        try
        {
            foreach (SqlItem<Kit> kit in List)
            {
                if (kit.Item is { IsLoadDirty: true })
                    (dirty ??= new List<SqlItem<Kit>>()).Add(kit);
            }
        }
        finally
        {
            WriteRelease();
        }
        if (dirty != null)
        {
            foreach (SqlItem<Kit> kit in dirty)
            {
                if (kit.Item != null)
                {
                    L.Log("Saving kit " + kit.Item.Id + " after dirty load.");
                    await kit.SaveItem(token).ConfigureAwait(false);
                    kit.Item.IsLoadDirty = false;
                }
            }
        }
    }
    public override async Task PreUnload(CancellationToken token)
    {
        //EventDispatcher.InventoryItemRemoved -= OnInventoryItemRemoved;
        EventDispatcher.SwapClothingRequested -= OnSwapClothingRequested;
        EventDispatcher.ItemPickedUp -= OnItemPickedUp;
        EventDispatcher.ItemDropped -= OnItemDropped;
        EventDispatcher.ItemMoved -= OnItemMoved;
        EventDispatcher.PlayerLeft -= OnPlayerLeaving;
        EventDispatcher.PlayerJoined -= OnPlayerJoined;
        EventDispatcher.GroupChanged -= OnGroupChanged;
        PlayerLife.OnPreDeath -= OnPreDeath;
        await SaveAllPlayerFavorites(token).ConfigureAwait(false);
        await base.PreUnload(token).ConfigureAwait(false);
    }
    private void OnItemsRefreshedIntl()
    {
        UCWarfare.RunTask(async () =>
        {
            bool needsT1Unarmed = false, needsT2Unarmed = false, needsDefault = false;
            await WaitAsync().ConfigureAwait(false);
            try
            {
                SqlItem<Kit>? kit = string.IsNullOrEmpty(TeamManager.Team1UnarmedKit) ? null : FindKitNoLock(TeamManager.Team1UnarmedKit!);
                if (kit?.Item == null)
                {
                    needsT1Unarmed = true;
                    L.LogError("Team 1's unarmed kit, \"" + TeamManager.Team1UnarmedKit + "\", was not found, an attempt will be made to auto-generate one.");
                }
                kit = string.IsNullOrEmpty(TeamManager.Team2UnarmedKit) ? null : FindKitNoLock(TeamManager.Team2UnarmedKit!);
                if (kit?.Item == null)
                {
                    needsT2Unarmed = true;
                    L.LogError("Team 2's unarmed kit, \"" + TeamManager.Team2UnarmedKit + "\", was not found, an attempt will be made to auto-generate one.");
                }
                kit = string.IsNullOrEmpty(TeamManager.DefaultKit) ? null : FindKitNoLock(TeamManager.DefaultKit);
                if (kit?.Item == null)
                {
                    needsDefault = true;
                    L.LogError("The default kit, \"" + TeamManager.DefaultKit + "\", was not found, an attempt will be made to auto-generate one.");
                }
            }
            finally
            {
                Release();
            }

            if (!needsDefault && !needsT1Unarmed && !needsT2Unarmed)
                return;
            if (needsT1Unarmed && !string.IsNullOrEmpty(TeamManager.Team1UnarmedKit))
            {
                SqlItem<Kit> proxy = await CreateDefaultKit(TeamManager.Team1Faction, TeamManager.Team1UnarmedKit!);
                L.Log("Created default kit for team 1: \"" + proxy.Item?.Id + "\".");
            }
            if (needsT2Unarmed && !string.IsNullOrEmpty(TeamManager.Team2UnarmedKit))
            {
                SqlItem<Kit> proxy = await CreateDefaultKit(TeamManager.Team2Faction, TeamManager.Team2UnarmedKit!);
                L.Log("Created default kit for team 2: \"" + proxy.Item?.Id + "\".");
            }
            if (needsDefault && !string.IsNullOrEmpty(TeamManager.DefaultKit))
            {
                SqlItem<Kit> proxy = await CreateDefaultKit(null, TeamManager.DefaultKit);
                L.Log("Created default kit: \"" + proxy.Item?.Id + "\".");
            }
        });
    }

    private static readonly IKitItem[] DefaultKitItems =
    {
        // MRE
        new PageItem(new Guid("acf7e825832f4499bb3b7cbec4f634ca"), 0, 0, 0, Array.Empty<byte>(), 1, Page.Hands),
        new PageItem(new Guid("acf7e825832f4499bb3b7cbec4f634ca"), 1, 0, 0, Array.Empty<byte>(), 1, Page.Hands),

        // Bottled Soda
        new PageItem(new Guid("c83390665c6546b8befbf6f15ef202c4"), 2, 0, 0, Array.Empty<byte>(), 1, Page.Hands),

        // Bottled Water
        new PageItem(new Guid("f81d68ebb2a8490dbe1545d432b9c099"), 2, 1, 0, Array.Empty<byte>(), 1, Page.Hands),

        // Binoculars
        new PageItem(new Guid("f260c581cf504098956f424d62345982"), 0, 2, 0, Array.Empty<byte>(), 1, Page.Hands),

        // Earpiece
        new ClothingItem(new Guid("2ecf1b15a59f4125a2d55c88479529c2"), ClothingType.Mask, Array.Empty<byte>())
    };

    private static readonly IKitItem[] DefaultKitClothes =
    {
        new ClothingItem(new Guid("c3adf16156004b40a839ed1b80583c32"), ClothingType.Shirt, Array.Empty<byte>()),
        new ClothingItem(new Guid("67a6ec52e4b24ffd89f75ceee0eb5179"), ClothingType.Pants, Array.Empty<byte>())
    };

    private async Task<SqlItem<Kit>> CreateDefaultKit(FactionInfo? faction, string name, CancellationToken token = default)
    {
        List<IKitItem> items = new List<IKitItem>(DefaultKitItems.Length + 6);
        items.AddRange(DefaultKitItems);
        SqlItem<Kit>? proxy = await FindKit(name, token, true).ConfigureAwait(false);
        if (proxy is not null)
            return proxy;
        if (faction != null)
        {
            if (faction.DefaultShirt.ValidReference(out ItemShirtAsset _) && !items.Exists(x => x is IClothingJar { Type: ClothingType.Shirt }))
                items.Add(new AssetRedirectClothing(RedirectType.Shirt, ClothingType.Shirt));
            if (faction.DefaultPants.ValidReference(out ItemPantsAsset _) && !items.Exists(x => x is IClothingJar { Type: ClothingType.Pants }))
                items.Add(new AssetRedirectClothing(RedirectType.Pants, ClothingType.Pants));
            if (faction.DefaultVest.ValidReference(out ItemVestAsset _) && !items.Exists(x => x is IClothingJar { Type: ClothingType.Vest }))
                items.Add(new AssetRedirectClothing(RedirectType.Vest, ClothingType.Vest));
            if (faction.DefaultHat.ValidReference(out ItemHatAsset _) && !items.Exists(x => x is IClothingJar { Type: ClothingType.Hat }))
                items.Add(new AssetRedirectClothing(RedirectType.Hat, ClothingType.Hat));
            if (faction.DefaultMask.ValidReference(out ItemMaskAsset _) && !items.Exists(x => x is IClothingJar { Type: ClothingType.Mask }))
                items.Add(new AssetRedirectClothing(RedirectType.Mask, ClothingType.Mask));
            if (faction.DefaultBackpack.ValidReference(out ItemBackpackAsset _) && !items.Exists(x => x is IClothingJar { Type: ClothingType.Backpack }))
                items.Add(new AssetRedirectClothing(RedirectType.Backpack, ClothingType.Backpack));
            if (faction.DefaultGlasses.ValidReference(out ItemGlassesAsset _) && !items.Exists(x => x is IClothingJar { Type: ClothingType.Glasses }))
                items.Add(new AssetRedirectClothing(RedirectType.Glasses, ClothingType.Glasses));
            Kit kit = new Kit(name, Class.Unarmed, GetDefaultBranch(Class.Unarmed), KitType.Special, SquadLevel.Member, faction)
            {
                Items = items.ToArray(),
                FactionFilter = new PrimaryKey[] { faction.PrimaryKey },
                FactionFilterIsWhitelist = true
            };
            proxy = await AddOrUpdate(kit, token).ConfigureAwait(false);
        }
        else
        {
            for (int i = 0; i < DefaultKitClothes.Length; ++i)
            {
                if (DefaultKitClothes[i] is IClothingJar jar && !items.Exists(x => x is IClothingJar jar2 && jar2.Type == jar.Type))
                {
                    items.Add(DefaultKitClothes[i]);
                }
            }
            Kit kit = new Kit(name, Class.Unarmed, GetDefaultBranch(Class.Unarmed), KitType.Special, SquadLevel.Member, null)
            {
                Items = items.ToArray()
            };
            proxy = await AddOrUpdate(kit, token).ConfigureAwait(false);
        }
        ActionLog.Add(ActionLogType.CreateKit, name);
        UpdateSigns(proxy);
        return proxy;
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
    public static bool ShouldAllowLayouts(Kit kit)
    {
        return true;
    }
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

        await GiveKit(player, kit, true, token).ConfigureAwait(false);
    }
    public async Task<SqlItem<Kit>?> TryGiveUnarmedKit(UCPlayer player, CancellationToken token = default)
    {
#if DEBUG
        using IDisposable profiler = ProfilingUtils.StartTracking();
#endif
        SqlItem<Kit>? kit = await GetDefaultKit(player.GetTeam(), token).ConfigureAwait(false);
        if (kit?.Item != null)
        {
            await GiveKit(player, kit, true, token).ConfigureAwait(false);
            return kit;
        }
        return null;
    }
    public async Task<SqlItem<Kit>?> TryGiveRiflemanKit(UCPlayer player, bool tip = true, CancellationToken token = default)
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
        await GiveKit(player, rifleman, tip, token).ConfigureAwait(false);
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
    public async Task<SqlItem<Kit>?> GetRecommendedSquadleaderKit(ulong team, CancellationToken token = default)
    {
        await WriteWaitAsync(token).ConfigureAwait(false);
        try
        {
            for (int i = 0; i < Items.Count; ++i)
            {
                if (Items[i].Item is { } kit)
                {
                    if (kit.Class == Class.Squadleader && kit.IsPublicKit && kit.IsRequestable(team))
                        return Items[i];
                }
            }
        }
        finally
        {
            WriteRelease();
        }

        return null;
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
    public async Task GiveKit(UCPlayer player, SqlItem<Kit>? kit, bool tip = true, CancellationToken token = default)
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
            WriteWait();
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
                    GrantKit(player, kit, tip);
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
    internal async Task GiveKitNoLock(UCPlayer player, SqlItem<Kit>? kit, bool tip = true, CancellationToken token = default, bool psLock = true)
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
                GrantKit(player, kit, tip);
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
                GrantKit(player, null, false);
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
    private static void GrantKit(UCPlayer player, SqlItem<Kit>? kit, bool tip = true)
    {
        ThreadUtil.assertIsGameThread();
#if DEBUG
        using IDisposable profiler = ProfilingUtils.StartTracking();
#endif
        Kit? k = kit?.Item;
        if (UCWarfare.Config.ModifySkillLevels)
            player.EnsureSkillsets(k?.Skillsets ?? Array.Empty<Skillset>());
        player.ChangeKit(kit);
        if (k != null)
        {
            UpdateSigns(k);
            GiveKitToPlayerInventory(player, k, true, false, tip);
            if (player.HotkeyBindings != null)
            {
                foreach (HotkeyBinding binding in player.HotkeyBindings)
                {
                    if (binding.Kit.Key == k.PrimaryKey.Key)
                    {
                        byte index = KitEx.GetHotkeyIndex(binding.Slot);
                        if (index != byte.MaxValue)
                        {
                            byte x = binding.Item.X, y = binding.Item.Y;
                            Page page = binding.Item.Page;
                            foreach (ItemTransformation transformation in player.ItemTransformations)
                            {
                                if (transformation.OldPage == page && transformation.OldX == x && transformation.OldY == y)
                                {
                                    x = transformation.NewX;
                                    y = transformation.NewY;
                                    page = transformation.NewPage;
                                    break;
                                }
                            }

                            ItemAsset? asset = binding.GetAsset(k, player.GetTeam());
                            if (asset != null && KitEx.CanBindHotkeyTo(asset, page))
                                player.Player.equipment.ServerBindItemHotkey(index, asset, (byte)page, x, y);
                        }
                    }
                }
            }
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

            GiveKitToPlayerInventory(player, k, true, ignoreAmmoBags);
            bool playEffectEquip = true;
            bool playEffectDrop = true;
            foreach (KeyValuePair<ItemJar, Page> jar in nonKitItems)
            {
                if (!player.Player.inventory.tryAddItem(jar.Key.item, jar.Key.x, jar.Key.y, (byte)jar.Value, jar.Key.rot))
                {
                    if (!player.Player.inventory.tryAddItem(jar.Key.item, false, playEffectEquip))
                    {
                        ItemManager.dropItem(jar.Key.item, player.Position, playEffectDrop, true, true);
                        playEffectDrop = false;
                    }
                    else playEffectEquip = false;
                }
            }
        }
        finally
        {
            kit.Release();
        }
    }
    private static void GiveKitToPlayerInventory(UCPlayer player, Kit? kit, bool clear, bool ignoreAmmobags, bool tip = true)
    {
        if (player == null) throw new ArgumentNullException(nameof(player));
#if DEBUG
        using IDisposable profiler = ProfilingUtils.StartTracking();
#endif

        if (clear) UCInventoryManager.ClearInventory(player, !Data.UseFastKits);
        else ThreadUtil.assertIsGameThread();

        player.ItemTransformations.Clear();
        player.ItemDropTransformations.Clear();
        player.Player.equipment.dequip();
        if (kit == null)
            return;
        LayoutTransformation[] layout = player.LayoutTransformations == null || !player.HasDownloadedKitData || !kit.PrimaryKey.IsValid || !ShouldAllowLayouts(kit)
            ? Array.Empty<LayoutTransformation>()
            : player.LayoutTransformations.Where(x => x.Kit.Key == kit.PrimaryKey.Key).ToArray();
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
                if ((flag & (1 << (int)clothingJar.Type)) == 0)
                {
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
                        inv.InvokeAndLoopback(id, ENetReliability.Reliable, Provider.GatherRemoteClientConnections(), asset.GUID, 100, state, !hasPlayedEffect);
                        hasPlayedEffect = true;
                    }
                }
                else
                {
                    L.LogWarning("Duplicate " + clothingJar.Type + " defined for " + kit.Id + ", " + item + ".");
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
                    })?.InvokeAndLoopback(id, ENetReliability.Reliable, Provider.GatherRemoteClientConnections(), Guid.Empty, 100, blank, false);
                }
            }
            Items[] p = player.Player.inventory.items;
            bool ohi = Data.GetOwnerHasInventory(player.Player.inventory);
            if (ohi)
                Data.SetOwnerHasInventory(player.Player.inventory, false);
            List<(Item, IItemJar)>? toAddLater = null;
            for (int i = 0; i < kit.Items.Length; ++i)
            {
                IKitItem item = kit.Items[i];
                if (item is not IItemJar jar)
                    continue;
                ItemAsset? asset = item.GetItem(kit, faction, out byte amt, out byte[] state);
                if (item is not IAssetRedirect && asset is ItemGunAsset && !UCWarfare.Config.DisableAprilFools && HolidayUtil.isHolidayActive(ENPCHoliday.APRIL_FOOLS))
                {
                                           // Dootpressor
                    if (Assets.find(new Guid("c3d3123823334847a9fd294e5d764889")) is ItemBarrelAsset barrel)
                    {
                        unsafe
                        {
                            fixed (byte* ptr = state)
                            {
                                UnsafeBitConverter.GetBytes(ptr, barrel.id, (int)AttachmentType.Barrel);
                                ptr[(int)AttachmentType.Barrel / 2 + 13] = 100;
                            }
                        }
                    }
                }
                // ignore ammo bag if enabled
                if (asset != null && ignoreAmmobags && Gamemode.Config.BarricadeAmmoBag.MatchGuid(asset.GUID))
                {
                    L.LogDebug("[GIVE KIT] Skipping ammo bag: " + jar + ".");
                    continue;
                }
                Page givePage;
                byte giveX, giveY, giveRot;
                bool layoutAffected;

                void ResetToOriginal()
                {
                    layoutAffected = false;
                    giveX = jar.X;
                    giveY = jar.Y;
                    giveRot = jar.Rotation;
                    givePage = jar.Page;
                }

                ResetToOriginal();

                // find layout override
                for (int j = 0; j < layout.Length; ++j)
                {
                    ref LayoutTransformation l = ref layout[j];
                    if (l.OldPage == givePage && l.OldX == giveX && l.OldY == giveY)
                    {
                        layoutAffected = true;
                        givePage = l.NewPage;
                        giveX = l.NewX;
                        giveY = l.NewY;
                        giveRot = l.NewRotation;
                        L.LogDebug("[GIVE KIT] Found layout for item " + item + " (to: " + givePage + ", (" + giveX + ", " + giveY + ") rot: " + giveRot + ".)");
                        break;
                    }
                }

                // checks for overlapping items and retries overlapping layout-affected items
                retry:
                if ((int)givePage < PlayerInventory.PAGES - 2 && asset != null)
                {
                    Items page = p[(int)givePage];
                    Item itm = new Item(asset.id, amt, 100, state);
                    // ensures multiple items are not put in the slots (causing the ghost gun issue)
                    if (givePage is Page.Primary or Page.Secondary)
                    {
                        if (page.getItemCount() > 0)
                        {
                            L.LogWarning("[GIVE KIT] Duplicate " + givePage.ToString().ToLowerInvariant() + " defined for " + kit.Id + ", " + item + ".");
                            L.Log("[GIVE KIT] Removing " + (page.items[0].GetAsset().itemName) + " in place of duplicate.");
                            (toAddLater ??= new List<(Item, IItemJar)>(2)).Add((page.items[0].item, jar));
                            page.removeItem(0);
                        }

                        giveX = 0;
                        giveY = 0;
                        giveRot = 0;
                    }
                    else if (UCInventoryManager.IsOutOfBounds(page, giveX, giveY, asset.size_x, asset.size_y, giveRot))
                    {
                        // if an item is out of range of it's container with a layout override, remove it and try again
                        if (layoutAffected)
                        {
                            L.LogDebug("[GIVE KIT] Out of bounds layout item in " + givePage + " defined for " + kit.Id + ", " + item + ".");
                            L.LogDebug("[GIVE KIT] Retrying at original position.");
                            ResetToOriginal();
                            goto retry;
                        }
                        L.LogWarning("[GIVE KIT] Out of bounds item in " + givePage + " defined for " + kit.Id + ", " + item + ".");
                        (toAddLater ??= new List<(Item, IItemJar)>(2)).Add((itm, jar));
                    }

                    int ic2 = page.getItemCount();
                    for (int j = 0; j < ic2; ++j)
                    {
                        ItemJar? jar2 = page.getItem((byte)j);
                        if (jar2 != null && UCInventoryManager.IsOverlapping(giveX, giveY, asset.size_x, asset.size_y, jar2.x, jar2.y, jar2.size_x, jar2.size_y, giveRot, jar2.rot))
                        {
                            // if an overlap is detected with a layout override, remove it and try again
                            if (layoutAffected)
                            {
                                L.LogDebug("[GIVE KIT] Overlapping layout item in " + givePage + " defined for " + kit.Id + ", " + item + ".");
                                L.LogDebug("[GIVE KIT] Retrying at original position.");
                                ResetToOriginal();
                                goto retry;
                            }
                            L.LogWarning("[GIVE KIT] Overlapping item in " + givePage + " defined for " + kit.Id + ", " + item + ".");
                            L.Log("[GIVE KIT] Removing " + (jar2.GetAsset().itemName) + " (" + jar2.x + ", " + jar2.y + " @ " + jar2.rot + "), in place of duplicate.");
                            page.removeItem((byte)j--);
                            (toAddLater ??= new List<(Item, IItemJar)>(2)).Add((jar2.item, jar));
                        }
                    }

                    if (layoutAffected)
                    {
                        player.ItemTransformations.Add(new ItemTransformation(jar.Page, givePage, jar.X, jar.Y, giveX, giveY, itm));
                    }
                    page.addItem(giveX, giveY, giveRot, itm);
                }
                // if a clothing item asset redirect is missing it's likely a kit being requested on a faction without those clothes.
                else if (item is not (IAssetRedirect and IClothingJar))
                    ReportItemError(kit, item, asset);
            }

            // try to add removed items later
            if (toAddLater is { Count: > 0 })
            {
                for (int i = 0; i < toAddLater.Count; ++i)
                {
                    (Item item, IItemJar jar) = toAddLater[i];
                    if (!player.Player.inventory.tryAddItemAuto(item, false, false, false, !hasPlayedEffect))
                    {
                        ItemManager.dropItem(item, player.Position, !hasPlayedEffect, true, false);
                        player.ItemDropTransformations.Add(new ItemDropTransformation(jar.Page, jar.X, jar.Y, item));
                    }
                    else
                    {
                        for (int pageIndex = 0; pageIndex < PlayerInventory.STORAGE; ++pageIndex)
                        {
                            Items page = player.Player.inventory.items[pageIndex];
                            int c = page.getItemCount();
                            for (int index = 0; index < c; ++index)
                            {
                                ItemJar jar2 = page.getItem((byte)index);
                                if (jar2.item != item)
                                    continue;
                                player.ItemTransformations.Add(new ItemTransformation(jar.Page, (Page)pageIndex, jar.X, jar.Y, jar2.x, jar2.y, item));
                                goto exit;
                            }
                        }
                    }
                    exit:

                    if (!hasPlayedEffect)
                        hasPlayedEffect = true;
                }
            }
            if (ohi)
                Data.SetOwnerHasInventory(player.Player.inventory, true);
            UCInventoryManager.SendPages(player);
        }
        else
        {
            foreach (IKitItem item in kit.Items)
            {
                if (item is IClothingJar clothing)
                {
                    ItemAsset? asset = item.GetItem(kit, faction, out byte amt, out byte[] state);
                    if (asset is null)
                    {
                        ReportItemError(kit, item, null);
                        return;
                    }
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
                    e:
                    ReportItemError(kit, item, asset);
                    Item uitem = new Item(asset.id, amt, 100, state);
                    if (!player.Player.inventory.tryAddItem(uitem, true))
                    {
                        ItemManager.dropItem(uitem, player.Position, false, true, true);
                    }
                }
            }
            foreach (IKitItem item in kit.Items)
            {
                if (item is not IClothingJar)
                {
                    ItemAsset? asset = item.GetItem(kit, faction, out byte amt, out byte[] state);
                    if (asset is null)
                    {
                        ReportItemError(kit, item, null);
                        return;
                    }
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

        if (kit.Class != Class.Unarmed && tip)
        {
            if (player.IsSquadLeader())
                Tips.TryGiveTip(player, 1200, T.TipActionMenuSl);
            else
                Tips.TryGiveTip(player, 3600, T.TipActionMenu);
        }

        // equip primary or secondary
        if (player.Player.inventory.items[(int)Page.Primary].getItemCount() > 0)
            player.Player.equipment.ServerEquip((byte)Page.Primary, 0, 0);
        else if (player.Player.inventory.items[(int)Page.Secondary].getItemCount() > 0)
            player.Player.equipment.ServerEquip((byte)Page.Secondary, 0, 0);
    }
    internal static void TryReverseLayoutTransformations(UCPlayer player, IKitItem[] kitItems, PrimaryKey kit)
    {
        ThreadUtil.assertIsGameThread();
        if (player.LayoutTransformations is not { Count: > 0 })
            return;
        
        for (int i = 0; i < player.LayoutTransformations.Count; ++i)
        {
            LayoutTransformation t = player.LayoutTransformations[i];
            if (t.Kit.Key != kit.Key)
                continue;
            
            ReverseLayoutTransformation(t, player, kitItems, kit, ref i);
            if (i < 0) i = 0;
        }
    }
    private static void ReverseLayoutTransformation(LayoutTransformation transformation, UCPlayer player, IKitItem[] kitItems, PrimaryKey kit, ref int i)
    {
        if (player.LayoutTransformations == null)
            return;
        PlayerInventory inv = player.Player.inventory;
        Items page = inv.items[(int)transformation.NewPage];
        ItemJar? current = page.getItem(page.getIndex(transformation.NewX, transformation.NewY));
        if (current == null || current.item?.GetAsset() is not { } asset1)
        {
            L.LogDebug("Current item not in inventory.");
            return;
        }
        IItemJar? original = (IItemJar?)kitItems.FirstOrDefault(x => x is IItemJar jar && jar.X == transformation.OldX && jar.Y == transformation.OldY && jar.Page == transformation.OldPage);
        if (original == null)
        {
            L.LogDebug("Transformation was not in original kit items.");
            return;
        }
        page = inv.items[(int)transformation.OldPage];
        int ct = page.getItemCount();
        for (int i2 = 0; i2 < ct; ++i2)
        {
            ItemJar jar = page.getItem((byte)i2);
            if (UCInventoryManager.IsOverlapping(original.X, original.Y, asset1.size_x, asset1.size_y, jar.x, jar.y, jar.size_x, jar.size_y, original.Rotation, jar.rot))
            {
                L.LogDebug($"Found reverse collision at {transformation.OldPage}, ({jar.x}, {jar.y}) @ rot {jar.rot}.");
                if (jar == current)
                {
                    L.LogDebug(" Collision was same item.");
                    continue;
                }
                int index = player.LayoutTransformations.FindIndex(x =>
                    x.Kit.Key == kit.Key && x.NewX == jar.x && x.NewY == jar.y &&
                    x.NewPage == transformation.OldPage);
                if (index < 0)
                {
                    L.LogDebug(" Unable to recursively move back.");
                    return;
                }
                LayoutTransformation lt = player.LayoutTransformations[index];
                player.LayoutTransformations.RemoveAtFast(index);
                if (i <= index)
                    --i;
                ReverseLayoutTransformation(lt, player, kitItems, kit, ref i);
                break;
            }
        }
        inv.ReceiveDragItem((byte)transformation.NewPage, current.x, current.y, (byte)original.Page, original.X, original.Y, original.Rotation);
        L.LogDebug($"Reversing {transformation.NewPage}, ({current.x}, {current.y}) to {original.Page}, ({original.X}, {original.Y}) @ rot {original.Rotation}.");
    }
    public static List<LayoutTransformation> GetLayoutTransformations(UCPlayer player, PrimaryKey kit)
    {
        ThreadUtil.assertIsGameThread();
        List<LayoutTransformation> output = new List<LayoutTransformation>(player.ItemTransformations.Count);
        Items[] p = player.Player.inventory.items;
        for (int i = 0; i < player.ItemTransformations.Count; i++)
        {
            ItemTransformation transformation = player.ItemTransformations[i];
            Items upage = p[(int)transformation.NewPage];
            ItemJar? jar = upage.getItem(upage.getIndex(transformation.NewX, transformation.NewY));
            if (jar != null && jar.item == transformation.Item)
                output.Add(new LayoutTransformation(transformation.OldPage, transformation.NewPage, transformation.OldX, transformation.OldY, jar.x, jar.y, jar.rot, kit));
            else
                L.LogDebug($"Unable to convert ItemTransformation to LayoutTransformation: {transformation.OldPage} -> {transformation.NewPage}, ({transformation.OldX} -> {transformation.NewX}, {transformation.OldY} -> {transformation.NewY}).");
        }

        return output;
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
        PlayerEquipment equipment = player.Player.equipment;
        for (int i = 0; i < 8; ++i)
            equipment.ServerClearItemHotkey((byte)i);
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
                                    QuestManager.TryAddQuest(player, quest);
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
                    await GiveKit(pl, t1def == kit ? null : t1def, false, token);
                else if (team == 2 && (t2def ??= await GetDefaultKit(2ul, token))?.Item != null)
                    await GiveKit(pl, t2def == kit ? null : t2def, false, token);
                else
                    await GiveKit(pl, null, false, token);
            }
        }
    }
    /// <remarks>Thread Safe</remarks>
    public async Task DequipKit(UCPlayer player, CancellationToken token = default)
    {
        ulong team = player.GetTeam();
        SqlItem<Kit>? dkit = await GetDefaultKit(team, token);
        if (dkit?.Item != null)
            await GiveKit(player, dkit, true, token);
        else
            await GiveKit(player, null, false, token);
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

    public async Task<(SqlItem<Kit>?, StandardErrorCode)> UpgradeLoadout(ulong fromPlayer, ulong player, Class @class, string loadoutName, CancellationToken token = default)
    {
        Kit dequipKit;
        SqlItem<Kit>? existing;
        await WaitAsync(token).ConfigureAwait(false);
        try
        {
            existing = FindKitNoLock(loadoutName, true);
            if (existing?.Item is not { } kit)
                return (existing, StandardErrorCode.NotFound);

            if (kit.Season >= UCWarfare.Season)
                return (existing, StandardErrorCode.InvalidData);

            await RemoveAccess(existing, player, token).ConfigureAwait(false);
            ActionLog.Add(ActionLogType.ChangeKitAccess, player.ToString(Data.AdminLocale) + " DENIED ACCESS TO " + loadoutName, fromPlayer);
            Class oldClass = kit.Class;
            kit.FactionFilterIsWhitelist = false;
            kit.FactionFilter = Array.Empty<PrimaryKey>();
            kit.Class = @class;
            kit.Faction = null;
            kit.UpdateLastEdited(fromPlayer);
            kit.Items = KitEx.GetDefaultLoadoutItems(@class);
            kit.RequiresNitro = false;
            kit.WeaponText = KitEx.PendingKitWeaponText;
            kit.Disabled = true;
            kit.Season = UCWarfare.Season;
            kit.ClothingSetCache = null;
            kit.ItemListCache = null;
            kit.MapFilterIsWhitelist = false;
            kit.MapFilter = Array.Empty<PrimaryKey>();
            kit.Branch = GetDefaultBranch(@class);
            kit.TeamLimit = GetDefaultTeamLimit(@class);
            kit.RequestCooldown = GetDefaultRequestCooldown(@class);
            kit.SquadLevel = SquadLevel.Member;
            kit.CreditCost = 0;
            kit.Type = KitType.Loadout;
            kit.UnlockRequirements = Array.Empty<UnlockRequirement>();
            kit.PremiumCost = 0m;

            await existing.SaveItem(token).ConfigureAwait(false);
            ActionLog.Add(ActionLogType.UpgradeLoadout, $"ID: {loadoutName} (#{kit.PrimaryKey.Key}). Class: {oldClass} -> {@class}. Old Faction: {kit.FactionKey}", fromPlayer);

            await GiveAccess(kit, player, KitAccessType.Purchase, token).ConfigureAwait(false);
            ActionLog.Add(ActionLogType.ChangeKitAccess, player.ToString(Data.AdminLocale) + " GIVEN ACCESS TO " + loadoutName + ", REASON: " + KitAccessType.Purchase, fromPlayer);
            dequipKit = kit;
        }
        finally
        {
            Release();
        }

        await UCWarfare.ToUpdate(token);
        if (UCPlayer.FromID(player) is { } pl)
            Signs.UpdateLoadoutSigns(pl);

        await DequipKit(dequipKit, token).ConfigureAwait(false);

        return (existing, StandardErrorCode.Success);
    }
    public async Task<(SqlItem<Kit>?, StandardErrorCode)> UnlockLoadout(ulong fromPlayer, ulong player, string loadoutName, CancellationToken token = default)
    {
        Kit dequipKit;
        SqlItem<Kit>? existing;
        await WaitAsync(token).ConfigureAwait(false);
        try
        {
            existing = FindKitNoLock(loadoutName, true);
            if (existing?.Item is not { } kit)
                return (existing, StandardErrorCode.NotFound);
            
            ActionLog.Add(ActionLogType.UnlockLoadout, loadoutName, fromPlayer);
            
            kit.UpdateLastEdited(fromPlayer);
            kit.Disabled = false;
            kit.ClothingSetCache = null;
            kit.ItemListCache = null;
            if (kit.WeaponText == null || kit.WeaponText.Equals(KitEx.PendingKitWeaponText, StringComparison.Ordinal))
                kit.WeaponText = DetectWeaponText(kit);
            dequipKit = kit;
        }
        finally
        {
            Release();
        }

        await UCWarfare.ToUpdate(token);
        if (UCPlayer.FromID(player) is { } pl)
            Signs.UpdateLoadoutSigns(pl);

        await DequipKit(dequipKit, token).ConfigureAwait(false);

        return (existing, StandardErrorCode.Success);
    }
    public async Task<(SqlItem<Kit>, StandardErrorCode)> CreateLoadout(ulong fromPlayer, ulong player, Class @class, string displayName, CancellationToken token = default)
    {
#if DEBUG
        using IDisposable profiler = ProfilingUtils.StartTracking();
#endif
        char let = await GetLoadoutCharacter(player);
        string loadoutName = player.ToString(Data.AdminLocale) + "_" + let;
        SqlItem<Kit>? existing = await FindKit(loadoutName, token, true).ConfigureAwait(false);
        if (existing?.Item != null)
            return (existing, StandardErrorCode.GenericError);

        IKitItem[] items = KitEx.GetDefaultLoadoutItems(@class);
        Kit loadout = new Kit(loadoutName, @class, GetDefaultBranch(@class), KitType.Loadout, SquadLevel.Member, null)
        {
            Items = items,
            Creator = fromPlayer,
            WeaponText = KitEx.PendingKitWeaponText,
            Disabled = true
        };
        SetTextNoLock(fromPlayer, loadout, displayName);
        SqlItem<Kit> kit = await AddOrUpdate(loadout, token).ConfigureAwait(false);
        ActionLog.Add(ActionLogType.CreateKit, loadoutName, fromPlayer);
        
        await GiveAccess(kit, player, KitAccessType.Purchase, token).ConfigureAwait(false);
        ActionLog.Add(ActionLogType.ChangeKitAccess, player.ToString(Data.AdminLocale) + " GIVEN ACCESS TO " + loadoutName + ", REASON: " + KitAccessType.Purchase, fromPlayer);
        await UCWarfare.ToUpdate(token);
        if (UCPlayer.FromID(player) is { } pl)
            Signs.UpdateLoadoutSigns(pl);
        return (kit, StandardErrorCode.Success);
    }
    public static string DetectWeaponText(Kit kit)
    {
        IKitItem[] items = kit.Items;
        List<KeyValuePair<IItemJar, ItemGunAsset>> guns = new List<KeyValuePair<IItemJar, ItemGunAsset>>(2);
        for (int i = 0; i < items.Length; ++i)
        {
            if (items[i] is IItem item and IItemJar jar && Assets.find(item.Item) is ItemGunAsset asset)
                guns.Add(new KeyValuePair<IItemJar, ItemGunAsset>(jar, asset));
        }

        if (guns.Count == 0)
            return string.Empty;

        guns.Sort((a, b) => a.Key.Page.CompareTo(b.Key.Page));
        return string.Join(", ", guns.Select(x => x.Value.itemName.ToUpperInvariant()));
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
        await ctx.Caller.PurchaseSync.WaitAsync(token).ConfigureAwait(false);
        try
        {
            if (!ctx.Caller.HasDownloadedKitData)
                await DownloadPlayerKitData(ctx.Caller, false, token).ConfigureAwait(false);
        }
        finally
        {
            ctx.Caller.PurchaseSync.Release();
        }
        await proxy.Enter(token).ConfigureAwait(false);
        try
        {

            await UCWarfare.ToUpdate(token);
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
                        throw ctx.Reply(T.RequestKitCantAfford, ctx.Caller.CachedCredits, kit.CreditCost);
                }
            }
            else if (!kit.RequiresNitro && !HasAccessQuick(kit, ctx.Caller) && !UCWarfare.Config.OverrideKitRequirements)
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
            if (kit.UnlockRequirements != null)
            {
                for (int i = 0; i < kit.UnlockRequirements.Length; i++)
                {
                    UnlockRequirement req = kit.UnlockRequirements[i];
                    if (req == null || req.CanAccess(ctx.Caller))
                        continue;
                    throw req.RequestKitFailureToMeet(ctx, kit);
                }
            }
            bool hasAccess = kit.CreditCost == 0 && kit.IsPublicKit || UCWarfare.Config.OverrideKitRequirements;
            if (!hasAccess)
            {
                hasAccess = await HasAccess(kit, ctx.Caller.Steam64, token).ConfigureAwait(false);
                await UCWarfare.ToUpdate(token);
                if (!hasAccess)
                {
                    if (kit.RequiresNitro)
                    {
                        try
                        {
                            bool nitroBoosting = await IsNitroBoosting(ctx.CallerID, token).ConfigureAwait(false) ?? ctx.Caller.Save.WasNitroBoosting;
                            await UCWarfare.ToUpdate(token);
                            if (!nitroBoosting)
                                throw ctx.Reply(T.RequestKitMissingNitro);
                        }
                        catch (TimeoutException)
                        {
                            throw ctx.Reply(T.UnknownError);
                        }
                    }
                    else if (kit.IsPaid)
                        throw ctx.Reply(T.RequestKitMissingAccess);
                    else if (ctx.Caller.CachedCredits >= kit.CreditCost)
                        throw ctx.Reply(T.RequestKitNotBought, kit.CreditCost);
                    else
                        throw ctx.Reply(T.RequestKitCantAfford, ctx.Caller.CachedCredits, kit.CreditCost);
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
        if (!ctx.Caller.HasDownloadedKitData)
            await DownloadPlayerKitData(ctx.Caller, false, token).ConfigureAwait(false);

        ulong team = ctx.Caller.GetTeam();
        await proxy.Enter(token).ConfigureAwait(false);
        Kit? kit;
        try
        {
            kit = proxy.Item;
            if (kit == null)
                throw ctx.Reply(T.KitNotFound, proxy.LastPrimaryKey.ToString());
            if (!kit.IsPublicKit)
                throw ctx.Reply(T.RequestNotBuyable);
            if (kit.CreditCost == 0 || HasAccessQuick(kit, ctx.Caller))
                throw ctx.Reply(T.RequestKitAlreadyOwned);
            if (ctx.Caller.CachedCredits < kit.CreditCost)
                throw ctx.Reply(T.RequestKitCantAfford, ctx.Caller.CachedCredits, kit.CreditCost);

            await ctx.Caller.PurchaseSync.WaitAsync(token).ConfigureAwait(false);
            try
            {
                await Points.UpdatePointsAsync(ctx.Caller, false, token).ConfigureAwait(false);
                if (ctx.Caller.CachedCredits < kit.CreditCost)
                {
                    await UCWarfare.ToUpdate();
                    throw ctx.Reply(T.RequestKitCantAfford, ctx.Caller.CachedCredits, kit.CreditCost);
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

            bool access = await AddAccessRow(kit.PrimaryKey, ctx.CallerID, KitAccessType.Credits, token).ConfigureAwait(false);
            await UCWarfare.ToUpdate(token);
            if (access)
            {
                (ctx.Caller.AccessibleKits ??= new List<SqlItem<Kit>>(4)).Add(proxy);
                if (OnKitAccessChanged != null)
                    UCWarfare.RunOnMainThread(() => OnKitAccessChanged?.Invoke(proxy, ctx.CallerID, true, KitAccessType.Credits), default);
            }
            else throw ctx.SendUnknownError();
            
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
        await GiveKitNoLock(ctx.Caller, proxy, true, token).ConfigureAwait(false);
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
            await TryGiveRiflemanKit(player, false, token).ConfigureAwait(false);
        else if (UCWarfare.Config.ModifySkillLevels)
            player.EnsureSkillsets(kit.Skillsets ?? Array.Empty<Skillset>());
    }
    async Task IPlayerPostInitListenerAsync.OnPostPlayerInit(UCPlayer player, CancellationToken token)
    {
        if (Data.Gamemode is not TeamGamemode { UseTeamSelector: true })
        {
            _ = RefreshFavorites(player, false, token);
            await SetupPlayer(player, token).ConfigureAwait(false);
            return;
        }
        //UCInventoryManager.ClearInventory(player);
        player.EnsureSkillsets(Array.Empty<Skillset>());
        _ = RefreshFavorites(player, false, token);
        _ = IsNitroBoosting(player.Steam64, token);
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
        if (Data.Gamemode != null && Data.Gamemode.EveryMinute && Provider.clients.Count > 0)
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
    public static bool IsNitroBoostingQuick(ulong player)
    {
        return PlayerSave.TryReadSaveFile(player, out PlayerSave save) && save.WasNitroBoosting;
    }
    /// <exception cref="TimeoutException"/>
    public static async Task<bool?> IsNitroBoosting(ulong player, CancellationToken token = default)
    {
        bool?[]? state = await IsNitroBoosting(new ulong[] { player }, token).ConfigureAwait(false);
        return state == null || state.Length < 1 ? null : state[0];
    }
    /// <exception cref="TimeoutException"/>
    public static async Task<bool?[]?> IsNitroBoosting(ulong[] players, CancellationToken token = default)
    {
        if (!UCWarfare.CanUseNetCall)
            return null;
        bool?[] rtn = new bool?[players.Length];

        RequestResponse response = await KitEx.NetCalls.RequestIsNitroBoosting
            .Request(KitEx.NetCalls.RespondIsNitroBoosting, UCWarfare.I.NetClient!, players, 8192);
        if (response.TryGetParameter(0, out byte[] state))
        {
            int len = Math.Min(state.Length, rtn.Length);
            for (int i = 0; i < len; ++i)
            {
                byte b = state[i];
                rtn[i] = b switch { 0 => false, 1 => true, _ => null };
                if (b is 0 or 1)
                {
                    if (!PlayerSave.TryReadSaveFile(players[i], out PlayerSave save))
                    {
                        if (b != 1)
                            continue;
                        save = new PlayerSave(players[i]);
                    }
                    else if (save.WasNitroBoosting == (b == 1))
                        continue;
                    save.WasNitroBoosting = b == 1;
                    PlayerSave.WriteToSaveFile(save);
                }
            }
        }
        else throw new TimeoutException("Timed out while checking nitro status.");

        return rtn;
    }
    internal void OnNitroBoostingUpdated(ulong player, byte state)
    {
#if DEBUG
        using IDisposable profiler = ProfilingUtils.StartTracking();
#endif
        if (state is 0 or 1)
        {
            if (!PlayerSave.TryReadSaveFile(player, out PlayerSave save))
            {
                if (state != 1)
                    return;
                save = new PlayerSave(player);
            }
            else if (save.WasNitroBoosting == (state == 1))
                return;
            save.WasNitroBoosting = state == 1;
            PlayerSave.WriteToSaveFile(save);
            if (UCPlayer.FromID(player) is { } pl)
            {
                pl.SendChat(state == 1 ? T.StartedNitroBoosting : T.StoppedNitroBoosting);
                WriteWait();
                try
                {
                    for (int i = 0; i < Items.Count; ++i)
                    {
                        if (Items[i] is { Item: { RequiresNitro: true, Id: { } id } })
                            Signs.UpdateKitSigns(pl, id);
                    }
                }
                finally
                {
                    WriteRelease();
                }
                if (state == 0 && pl.ActiveKit is { Item.RequiresNitro: true })
                {
                    UCWarfare.RunTask(TryGiveRiflemanKit, pl, true, Data.Gamemode.UnloadToken,
                        ctx: "Giving rifleman kit to " + player + " after losing nitro boost.");
                }
            }
        }

        string stateStr = state switch { 0 => "Not Boosting", 1 => "Boosting", _ => "Unknown" };
        ActionLog.Add(ActionLogType.NitroBoostStateUpdated, "State: \"" + stateStr + "\".", player);
        L.Log("Player {" + player + "} nitro boost status updated: \"" + stateStr + "\".", ConsoleColor.Magenta);
    }
    async Task ITCPConnectedListener.OnConnected(CancellationToken token)
    {
        int v = _v;
        if (PlayerManager.OnlinePlayers.Count < 1)
            return;
        CheckLoaded();

        await UCWarfare.ToUpdate();
        CheckLoaded();

        ulong[] players = new ulong[PlayerManager.OnlinePlayers.Count];
        for (int i = 0; i < PlayerManager.OnlinePlayers.Count; ++i)
            players[i] = PlayerManager.OnlinePlayers[i].Steam64;
        RequestResponse response = await KitEx.NetCalls.RequestIsNitroBoosting.Request(KitEx.NetCalls.RespondIsNitroBoosting,
            UCWarfare.I.NetClient!, players, 8192);
        CheckLoaded();
        if (response.Responded && response.TryGetParameter(0, out byte[] bytes))
        {
            int len = Math.Min(bytes.Length, players.Length);
            for (int i = 0; i < len; ++i)
                OnNitroBoostingUpdated(players[i], bytes[i]);
        }

        void CheckLoaded()
        {
            if (v != _v || !IsLoaded || !UCWarfare.CanUseNetCall)
                throw new OperationCanceledException();
        }
    }

    public Task<IItemJar?> GetHeldItemFromKit(UCPlayer player, CancellationToken token = default)
    {
        ItemJar? held = player.GetHeldItem(out byte page);
        return held == null
            ? Task.FromResult<IItemJar?>(null)
            : GetItemFromKit(player, held, (Page)page, token);
    }

    /// <summary>Gets the exact <see cref="IKitItem"/> and <see cref="IItemJar"/> that the player is holding from their kit (accounts for the item being moved).</summary>
    public Task<IItemJar?> GetItemFromKit(UCPlayer player, ItemJar jar, Page page, CancellationToken token = default) =>
        GetItemFromKit(player, jar.x, jar.y, jar.item, page, token);
    public async Task<IItemJar?> GetItemFromKit(UCPlayer player, byte x, byte y, Item item, Page page, CancellationToken token = default)
    {
        SqlItem<Kit>? proxy = player.ActiveKit!;
        if (proxy?.Item is null)
            return null;
        await proxy.Enter(token);
        try
        {
            await UCWarfare.ToUpdate(token);
            WriteWait();
            try
            {
                L.LogDebug("Looking for kit item: " + (item.GetAsset()?.itemName ?? "null") + $" at {page}, ({x}, {y}).");
                for (int i = 0; i < player.ItemTransformations.Count; ++i)
                {
                    ItemTransformation t = player.ItemTransformations[i];
                    if (t.Item != item)
                        continue;
                    if (t.NewX == x && t.NewY == y && t.NewPage == page)
                    {
                        L.LogDebug($"Transforming: {t.OldPage} <- {page}, ({t.OldX} <- {x}, {t.OldY} <- {y}).");
                        x = t.OldX;
                        y = t.OldY;
                        page = t.OldPage;
                        break;
                    }
                }

                ItemAsset asset = item.GetAsset();
                if (asset == null)
                    return null;
                foreach (IKitItem item2 in proxy.Item.Items)
                {
                    if (item2 is IItemJar jar2 && jar2.Page == page && jar2.X == x && jar2.Y == y)
                    {
                        if (jar2 is IItem pgItem)
                        {
                            if (pgItem.Item == asset.GUID)
                                return jar2;
                        }
                        else
                        {
                            if (item2.GetItem(proxy.Item, TeamManager.GetFactionSafe(player.GetTeam()), out _, out _) is { } asset2 && asset2.GUID == asset.GUID)
                                return jar2;
                        }

                        break;
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
            proxy.Release();
        }

        L.LogDebug($"Kit item at: {page}, ({x}, {y}) not found.");
        return null;
    }
    private void OnItemDropped(ItemDropped e)
    {
        UCPlayer pl = e.Player;
        if (e.Item != null)
            pl.ItemDropTransformations.Add(new ItemDropTransformation(e.OldPage, e.OldX, e.OldY, e.Item));
        
        if (e.Player.HotkeyBindings is not { Count: > 0 })
            return;
        CancellationToken tkn = UCWarfare.UnloadCancel;
        if (Data.Gamemode != null)
            tkn.CombineIfNeeded(Data.Gamemode.UnloadToken);
        tkn.CombineIfNeeded(e.Player.DisconnectToken);
        if (e.Item == null)
            return;

        // move hotkey to a different item of the same type
        UCWarfare.RunTask(async token =>
        {
            IItemJar? jar2 = await GetItemFromKit(e.Player, e.OldX, e.OldY, e.Item, e.OldPage, token).ConfigureAwait(false);
            if (jar2 == null || !e.Player.IsOnline) return;
            await e.Player.PurchaseSync.WaitAsync(token).ConfigureAwait(false);
            try
            {
                if (!e.Player.IsOnline || e.Player.HotkeyBindings is not { Count: > 0 }) return;
                await UCWarfare.ToUpdate(token);
                if (!e.Player.IsOnline || e.Player.HotkeyBindings is not { Count: > 0 }) return;
                for (int i = 0; i < e.Player.HotkeyBindings.Count; ++i)
                {
                    HotkeyBinding b = e.Player.HotkeyBindings[i];

                    if (b.Item is IItem item && jar2 is IItem item2 && item.Item == item2.Item ||
                        b.Item is IAssetRedirect redir && jar2 is IAssetRedirect redir2 && redir.RedirectType == redir2.RedirectType)
                    {
                        // found a binding for that item
                        if (b.Item.X != jar2.X || b.Item.Y != jar2.Y || b.Item.Page != jar2.Page)
                            continue;
                        ItemAsset? asset = b.Item switch
                        {
                            IItem item3 => Assets.find<ItemAsset>(item3.Item),
                            IKitItem ki => ki.GetItem(e.Player.ActiveKit?.Item, TeamManager.GetFactionSafe(e.Player.GetTeam()), out _, out _),
                            _ => null
                        };
                        if (asset == null)
                            return;
                        int hotkeyIndex = KitEx.GetHotkeyIndex(b.Slot);
                        if (hotkeyIndex < 0) return;
                        PlayerInventory inv = e.Player.Player.inventory;
                        // find new item to bind the item to
                        for (int p = PlayerInventory.SLOTS; p < PlayerInventory.STORAGE; ++p)
                        {
                            Items page = inv.items[p];
                            int c = page.getItemCount();
                            for (int index = 0; index < c; ++index)
                            {
                                ItemJar jar = page.getItem((byte)index);
                                if (jar.x == jar2.X && jar.y == jar2.Y && p == (int)jar2.Page)
                                    continue;
                                if (jar.GetAsset() is { } asset2 && asset2.GUID == asset.GUID && KitEx.CanBindHotkeyTo(asset2, (Page)p))
                                {
                                    e.Player.Player.equipment.ServerBindItemHotkey((byte)hotkeyIndex, asset, (byte)p, jar.x, jar.y);
                                    return;
                                }
                            }
                        }

                        break;
                    }
                }
            }
            finally
            {
                e.Player.PurchaseSync.Release();
            }
        }, tkn, ctx: "Set keybind to new item after it's dropped.");
    }
    private static void OnItemPickedUp(ItemPickedUp e)
    {
        UCPlayer pl = e.Player;
        if (e.Jar != null)
        {
            byte origX = byte.MaxValue, origY = byte.MaxValue;
            Page origPage = (Page)byte.MaxValue;
            for (int i = 0; i < pl.ItemDropTransformations.Count; ++i)
            {
                ItemDropTransformation d = pl.ItemDropTransformations[i];
                if (d.Item == e.Jar.item)
                {
                    for (int j = 0; j < pl.ItemTransformations.Count; ++j)
                    {
                        ItemTransformation t = pl.ItemTransformations[j];
                        if (t.Item == e.Jar.item)
                        {
                            pl.ItemTransformations[j] = new ItemTransformation(t.OldPage, e.Page, t.OldX, t.OldY, e.X, e.Y, t.Item);
                            origX = t.OldX;
                            origY = t.OldY;
                            origPage = t.OldPage;
                            goto rebind;
                        }
                    }

                    origX = d.OldX;
                    origY = d.OldY;
                    origPage = d.OldPage;
                    pl.ItemTransformations.Add(new ItemTransformation(d.OldPage, e.Page, d.OldX, d.OldY, e.X, e.Y, e.Jar.item));
                    pl.ItemDropTransformations.RemoveAtFast(i);
                    break;
                }
            }
            rebind:
            // resend hotkeys from picked up item
            if (UCWarfare.IsLoaded && e.Player.HotkeyBindings != null && origX < byte.MaxValue)
            {
                CancellationToken tkn = UCWarfare.UnloadCancel;
                if (Data.Gamemode != null)
                    tkn.CombineIfNeeded(Data.Gamemode.UnloadToken);
                tkn.CombineIfNeeded(e.Player.DisconnectToken);
                UCWarfare.RunTask(async token =>
                {
                    token.ThrowIfCancellationRequested();

                    await e.Player.PurchaseSync.WaitAsync(token).ConfigureAwait(false);
                    try
                    {
                        if (e.Player.HotkeyBindings == null)
                            return;

                        SqlItem<Kit>? proxy = e.Player.ActiveKit;
                        if (proxy?.Item is null)
                            return;

                        await proxy.Enter(token).ConfigureAwait(false);
                        try
                        {
                            Kit? kit = e.Player.ActiveKit?.Item;
                            if (kit == null) return;
                            foreach (HotkeyBinding binding in e.Player.HotkeyBindings)
                            {
                                if (binding.Kit.Key == kit.PrimaryKey.Key)
                                {
                                    if (binding.Item.X == origX && binding.Item.Y == origY && binding.Item.Page == origPage)
                                    {
                                        ItemAsset? asset = binding.GetAsset(kit, e.Player.GetTeam());
                                        if (asset != null)
                                        {
                                            byte index = KitEx.GetHotkeyIndex(binding.Slot);
                                            if (index == byte.MaxValue || !KitEx.CanBindHotkeyTo(asset, e.Page)) continue;
                                            e.Player.Player.equipment.ServerBindItemHotkey(index, asset, (byte)e.Page, e.X, e.Y);
                                            // L.LogDebug($"Updating old hotkey (picked up): {asset.itemName} at {e.Page}, ({e.X}, {e.Y}).");
                                            break;
                                        }
                                    }
                                }
                            }
                        }
                        finally
                        {
                            proxy.Release();
                        }
                    }
                    finally
                    {
                        e.Player.PurchaseSync.Release();
                    }

                }, tkn, ctx: "Checking for new binding of picked up item.");
            }
        }
    }
    private static void OnItemMoved(ItemMoved e)
    {
        if (e.NewX == e.OldX && e.NewY == e.OldY && e.NewPage == e.OldPage)
            return;
        UCPlayer pl = e.Player;
        byte origX = byte.MaxValue, origY = byte.MaxValue;
        Page origPage = (Page)byte.MaxValue;
        byte swapOrigX = byte.MaxValue, swapOrigY = byte.MaxValue;
        Page swapOrigPage = (Page)byte.MaxValue;
        if (e.Jar != null)
        {
            for (int i = 0; i < pl.ItemTransformations.Count; ++i)
            {
                ItemTransformation t = pl.ItemTransformations[i];
                if (t.Item == e.Jar.item)
                {
                    pl.ItemTransformations[i] = new ItemTransformation(t.OldPage, e.NewPage, t.OldX, t.OldY, e.NewX, e.NewY, t.Item);
                    origX = t.OldX;
                    origY = t.OldY;
                    origPage = t.OldPage;
                    goto swap;
                }
            }
            
            pl.ItemTransformations.Add(new ItemTransformation(e.OldPage, e.NewPage, e.OldX, e.OldY, e.NewX, e.NewY, e.Jar.item));
            origX = e.OldX;
            origY = e.OldY;
            origPage = e.OldPage;
        }

        swap:
        if (e.IsSwap && e.SwappedJar != null)
        {
            for (int i = 0; i < pl.ItemTransformations.Count; ++i)
            {
                ItemTransformation t = pl.ItemTransformations[i];
                if (t.Item == e.SwappedJar.item)
                {
                    pl.ItemTransformations[i] = new ItemTransformation(t.OldPage, e.OldPage, t.OldX, t.OldY, e.OldX, e.OldY, t.Item);
                    swapOrigX = t.OldX;
                    swapOrigY = t.OldY;
                    swapOrigPage = t.OldPage;
                    goto rebind;
                }
            }
            
            pl.ItemTransformations.Add(new ItemTransformation(e.NewPage, e.OldPage, e.NewX, e.NewY, e.OldX, e.OldY, e.SwappedJar.item));
            swapOrigX = e.NewX;
            swapOrigY = e.NewY;
            swapOrigPage = e.NewPage;
        }
        
        rebind:
        // resend hotkeys from moved item(s)
        if (UCWarfare.IsLoaded && e.Player.HotkeyBindings != null && (origX < byte.MaxValue || swapOrigX < byte.MaxValue))
        {
            CancellationToken tkn = UCWarfare.UnloadCancel;
            if (Data.Gamemode != null)
                tkn.CombineIfNeeded(Data.Gamemode.UnloadToken);
            tkn.CombineIfNeeded(e.Player.DisconnectToken);
            UCWarfare.RunTask(async token =>
            {
                token.ThrowIfCancellationRequested();
                
                await e.Player.PurchaseSync.WaitAsync(token).ConfigureAwait(false);
                try
                {
                    if (e.Player.HotkeyBindings == null)
                        return;

                    SqlItem<Kit>? proxy = e.Player.ActiveKit;
                    if (proxy?.Item is null)
                        return;

                    await proxy.Enter(token).ConfigureAwait(false);
                    try
                    {
                        Kit? kit = e.Player.ActiveKit?.Item;
                        if (kit == null) return;
                        foreach (HotkeyBinding binding in e.Player.HotkeyBindings)
                        {
                            if (binding.Kit.Key == kit.PrimaryKey.Key)
                            {
                                byte index = KitEx.GetHotkeyIndex(binding.Slot);
                                if (index == byte.MaxValue) continue;
                                if (binding.Item.X == origX && binding.Item.Y == origY && binding.Item.Page == origPage)
                                {
                                    ItemAsset? asset = binding.GetAsset(kit, e.Player.GetTeam());
                                    if (asset != null && KitEx.CanBindHotkeyTo(asset, e.NewPage))
                                    {
                                        e.Player.Player.equipment.ServerBindItemHotkey(index, asset, (byte)e.NewPage, e.NewX, e.NewY);
                                        // L.LogDebug($"Updating old hotkey: {asset.itemName} at {e.NewPage}, ({e.NewX}, {e.NewY}).");
                                    }
                                    if (!e.IsSwap)
                                        break;
                                }
                                else if (binding.Item.X == swapOrigX && binding.Item.Y == swapOrigY && binding.Item.Page == swapOrigPage)
                                {
                                    ItemAsset? asset = binding.GetAsset(kit, e.Player.GetTeam());
                                    if (asset != null && !KitEx.CanBindHotkeyTo(asset, e.OldPage))
                                    {
                                        e.Player.Player.equipment.ServerBindItemHotkey(index, asset, (byte)e.OldPage, e.OldX, e.OldY);
                                        // L.LogDebug($"Updating old swap hotkey: {asset.itemName} at {e.OldPage}, ({e.OldX}, {e.OldY}).");
                                    }
                                }
                            }
                        }
                    }
                    finally
                    {
                        proxy.Release();
                    }
                }
                finally
                {
                    e.Player.PurchaseSync.Release();
                }

            }, tkn, ctx: "Checking for new binding of moved item.");
        }
    }
    private static void OnSwapClothingRequested(SwapClothingRequested e)
    {
        if (e.Player.OnDuty()) return;

        if (e.Player.HasKit && e.Type is ClothingType.Backpack or ClothingType.Pants or ClothingType.Shirt or ClothingType.Vest)
        {
            e.Player.SendChat(T.NoRemovingClothing);
            e.Break();
        }
    }
    private static void VerifyKitIntegrity(Kit kit)
    {
        if (kit.FactionFilter != null)
        {
            for (int i = 0; i < kit.FactionFilter.Length; ++i)
            {
                PrimaryKey comparator = kit.FactionFilter[i];
                for (int j = kit.FactionFilter.Length - 1; j > i; --j)
                {
                    if (comparator.Key == kit.FactionFilter[j].Key)
                    {
                        PrimaryKey[] keys = kit.FactionFilter;
                        L.LogWarning("Duplicate faction filter found in kit \"" + kit.Id + "\" " + kit.PrimaryKey +
                                     ": " + TeamManager.GetFactionInfo(keys[i])?.Name + ".");
                        Util.RemoveFromArray(ref keys, j);
                        kit.FactionFilter = keys;
                        kit.IsLoadDirty = true;
                    }
                }
            }
            if (kit.FactionFilterIsWhitelist && kit.FactionKey.IsValid && !Array.Exists(kit.FactionFilter, x => x.Key == kit.FactionKey.Key))
            {
                PrimaryKey[] keys = kit.FactionFilter;
                Util.AddToArray(ref keys!, kit.FactionKey);
                L.LogWarning("Faction was not in whitelist for kit \"" + kit.Id + "\" " + kit.PrimaryKey +
                             ": " + TeamManager.GetFactionInfo(kit.FactionKey)?.Name + ".");
                kit.FactionFilter = keys;
                kit.IsLoadDirty = true;
            }
        }
        if (kit.MapFilter != null)
        {
            for (int i = 0; i < kit.MapFilter.Length; ++i)
            {
                PrimaryKey comparator = kit.MapFilter[i];
                for (int j = kit.MapFilter.Length - 1; j > i; --j)
                {
                    if (comparator.Key == kit.MapFilter[j].Key)
                    {
                        PrimaryKey[] keys = kit.MapFilter;
                        L.LogWarning("Duplicate map filter found in kit \"" + kit.Id + "\" " + kit.PrimaryKey + ": " + MapScheduler.GetMapName(keys[i]) + ".");
                        Util.RemoveFromArray(ref keys, j);
                        kit.MapFilter = keys;
                        kit.IsLoadDirty = true;
                    }
                }
            }
        }
        if (kit.Skillsets != null)
        {
            for (int i = 0; i < kit.Skillsets.Length; ++i)
            {
                Skillset comparator = kit.Skillsets[i];
                for (int j = kit.Skillsets.Length - 1; j > i; --j)
                {
                    if (comparator == kit.Skillsets[j])
                    {
                        Skillset[] skillsets = kit.Skillsets;
                        L.LogWarning("Duplicate skillset found in kit \"" + kit.Id + "\" " + kit.PrimaryKey + ": " + skillsets[i] + ".");
                        Util.RemoveFromArray(ref skillsets, j);
                        kit.Skillsets = skillsets;
                        kit.IsLoadDirty = true;
                    }
                }
            }
        }
        if (kit.UnlockRequirements != null)
        {
            for (int i = 0; i < kit.UnlockRequirements.Length; ++i)
            {
                UnlockRequirement comparator = kit.UnlockRequirements[i];
                for (int j = kit.UnlockRequirements.Length - 1; j > i; --j)
                {
                    if (comparator.Equals(kit.UnlockRequirements[j]))
                    {
                        UnlockRequirement[] reqs = kit.UnlockRequirements;
                        L.LogWarning("Duplicate unlock requirement found in kit \"" + kit.Id + "\" " + kit.PrimaryKey + ": " + reqs[i] + ".");
                        Util.RemoveFromArray(ref reqs, j);
                        kit.UnlockRequirements = reqs;
                        kit.IsLoadDirty = true;
                    }
                }
            }
        }
        if (kit.RequestSigns != null)
        {
            for (int i = 0; i < kit.RequestSigns.Length; ++i)
            {
                PrimaryKey comparator = kit.RequestSigns[i];
                for (int j = kit.RequestSigns.Length - 1; j > i; --j)
                {
                    if (comparator.Key == kit.RequestSigns[j].Key)
                    {
                        PrimaryKey[] keys = kit.RequestSigns;
                        L.LogWarning("Duplicate request sign found in kit \"" + kit.Id + "\" " + kit.PrimaryKey + ": " + keys[i] + ".");
                        Util.RemoveFromArray(ref keys, j);
                        kit.RequestSigns = keys;
                        kit.IsLoadDirty = true;
                    }
                }
            }
        }
        if (kit.Items != null)
        {
            for (int i = 0; i < kit.Items.Length; ++i)
            {
                IKitItem comparator = kit.Items[i];
                for (int j = kit.Items.Length - 1; j > i; --j)
                {
                    if (comparator.Equals(kit.Items[j]))
                    {
                        IKitItem[] items = kit.Items;
                        L.LogWarning("Duplicate item found in kit \"" + kit.Id + "\" " + kit.PrimaryKey + ":" + Environment.NewLine + comparator);
                        Util.RemoveFromArray(ref items, j);
                        kit.Items = items;
                        kit.IsLoadDirty = true;
                    }
                }
            }

            List<int>? alreadyChecked = null;
            for (int i = 0; i < kit.Items.Length; ++i)
            {
                IKitItem comparator = kit.Items[i];
                if (alreadyChecked != null && alreadyChecked.Contains(i))
                    continue;
                if (comparator is IClothingJar cjar)
                {
                    for (int j = kit.Items.Length - 1; j >= 0; --j)
                    {
                        if (i == j)
                            continue;
                        if (kit.Items[j] is IClothingJar cjar2 && cjar.Type == cjar2.Type)
                            L.LogError("Conflicting item found in kit \"" + kit.Id + "\" " + kit.PrimaryKey + ":" + Environment.NewLine + comparator + " / " + kit.Items[j]);
                    }
                }
                else if (comparator is IItemJar ijar)
                {
                    for (int j = kit.Items.Length - 1; j >= 0; --j)
                    {
                        if (i == j)
                            continue;
                        if (kit.Items[j] is IItemJar ijar2)
                        {
                            if (ijar.Page != ijar2.Page) continue;
                            ItemAsset? asset1 = comparator.GetItem(kit, null, out _, out _);
                            ItemAsset? asset2 = kit.Items[j].GetItem(kit, null, out _, out _);
                            if (asset1 != null && asset2 != null && UCInventoryManager.IsOverlapping(ijar, ijar2, asset1, asset2))
                            {
                                if (alreadyChecked != null && alreadyChecked.Contains(j))
                                    continue;
                                alreadyChecked ??= new List<int>();
                                alreadyChecked.Add(j);
                                L.LogError("Overlapping item found in kit \"" + kit.Id + "\" " + kit.PrimaryKey + ":" + Environment.NewLine
                                           + comparator + " / " + kit.Items[j]);
                            }
                        }
                    }
                }
            }
        }
    }
}