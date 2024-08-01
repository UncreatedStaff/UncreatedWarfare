using Cysharp.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Uncreated.Warfare.Commands.Dispatch;
using Uncreated.Warfare.Database;
using Uncreated.Warfare.Database.Abstractions;
using Uncreated.Warfare.Events;
using Uncreated.Warfare.Events.Items;
using Uncreated.Warfare.Events.Players;
using Uncreated.Warfare.Kits.Items;
using Uncreated.Warfare.Maps;
using Uncreated.Warfare.Models.Factions;
using Uncreated.Warfare.Models.Kits;
using Uncreated.Warfare.Players.Layouts;
using Uncreated.Warfare.Players.Management.Legacy;
using Uncreated.Warfare.Singletons;
using Uncreated.Warfare.Squads;
using Uncreated.Warfare.Sync;
using Uncreated.Warfare.Teams;
using Uncreated.Warfare.Util;

// ReSharper disable ConstantConditionalAccessQualifier

namespace Uncreated.Warfare.Kits;

public partial class KitManager : BaseAsyncReloadSingleton, IQuestCompletedHandler, IPlayerConnectListener, IPlayerPostInitListenerAsync, IGameTickListener, IPlayerDisconnectListener, ITCPConnectedListener
{
    private static KitMenuUI? _menuUi;
    public static readonly Guid[] BlacklistedWeaponsTextItems =
    {
        new Guid("3879d9014aca4a17b3ed749cf7a9283e"), // Laser Designator
        new Guid("010de9d7d1fd49d897dc41249a22d436")  // Laser Rangefinder
    };
    public static KitMenuUI MenuUI => _menuUi ??= new KitMenuUI();
    public override bool AwaitLoad => true;
    public KitDataCache Cache { get; }
    public KitDistribution Distribution { get; }
    public KitRequests Requests { get; }
    public KitSigns Signs { get; }
    public KitLayouts Layouts { get; }
    public KitBoosting Boosting { get; }
    public KitLoadouts<WarfareDbContext> Loadouts { get; }
    public KitDefaults<WarfareDbContext> Defaults { get; }

    public static event KitChanged? OnKitChanged;
    /// <summary>
    /// Doesn't include changes due to group change.
    /// </summary>
    public static event KitChanged? OnManualKitChanged;
    public static event KitAccessCallback? OnKitAccessChanged;
    public static event System.Action? OnFavoritesRefreshed;

    public static KitManager? GetSingletonQuick()
    {
        if (Data.Gamemode is IKitRequests k)
            return k.KitManager;

        return Data.Singletons.GetSingleton<KitManager>();
    }

    public KitManager() : base("kits")
    {
        Cache = new KitDataCache(this);
        Distribution = new KitDistribution(this);
        Requests = new KitRequests(this);
        Signs = new KitSigns(this);
        Layouts = new KitLayouts(this);
        Boosting = new KitBoosting(this);
        Loadouts = new KitLoadouts<WarfareDbContext>(this);
        Defaults = new KitDefaults<WarfareDbContext>(this);
    }
    public static IQueryable<Kit> Set(IKitsDbContext dbContext)
        => dbContext.Kits
            .Include(x => x.Translations);

    public static IQueryable<Kit> FullSet(IKitsDbContext dbContext)
        => dbContext.Kits
            .Include(x => x.Translations)
            .Include(x => x.FactionFilter)
            .Include(x => x.UnlockRequirementsModels)
            .Include(x => x.MapFilter)
            .Include(x => x.ItemModels)
            .Include(x => x.Skillsets);
    public static IQueryable<Kit> RequestableSet(IKitsDbContext dbContext, bool isVerified)
    {
        IQueryable<Kit> set = dbContext.Kits
            .Include(x => x.Translations)
            .Include(x => x.ItemModels)
            .Include(x => x.Skillsets);

        if (isVerified)
        {
            set.Include(x => x.FactionFilter)
               .Include(x => x.UnlockRequirementsModels)
               .Include(x => x.MapFilter);
        }

        return set;
    }
    protected override async Task LoadAsync(CancellationToken token)
    {
        await CreateMissingDefaultKits(token).ConfigureAwait(false);

        await ValidateKits(token).ConfigureAwait(false);

        PlayerLife.OnPreDeath += OnPreDeath;
        EventDispatcher.GroupChanged += OnGroupChanged;
        EventDispatcher.PlayerJoined += OnPlayerJoined;
        EventDispatcher.PlayerLeft += OnPlayerLeaving;
        EventDispatcher.ItemMoved += OnItemMoved;
        EventDispatcher.ItemDropped += OnItemDropped;
        EventDispatcher.ItemPickedUp += OnItemPickedUp;
        EventDispatcher.SwapClothingRequested += OnSwapClothingRequested;

        await CountTranslations(token).ConfigureAwait(false);

        await Cache.ReloadCache(token).ConfigureAwait(false);
    }
    protected override Task UnloadAsync(CancellationToken token)
    {
        PlayerLife.OnPreDeath -= OnPreDeath;
        EventDispatcher.GroupChanged -= OnGroupChanged;
        EventDispatcher.PlayerJoined -= OnPlayerJoined;
        EventDispatcher.PlayerLeft -= OnPlayerLeaving;
        EventDispatcher.ItemMoved -= OnItemMoved;
        EventDispatcher.ItemDropped -= OnItemDropped;
        EventDispatcher.ItemPickedUp -= OnItemPickedUp;
        EventDispatcher.SwapClothingRequested -= OnSwapClothingRequested;
        Cache.Clear();
        return Task.CompletedTask;
    }
    public override Task ReloadAsync(CancellationToken token)
    {
        return Cache.ReloadCache(token);

    }
    public static void InvokeOnKitAccessChanged(Kit kit, ulong player, bool newAccess, KitAccessType newType)
    {
        if (OnKitAccessChanged != null)
            UCWarfare.RunOnMainThread(() => OnKitAccessChanged?.Invoke(kit, player, newAccess, newType));
    }
    public static void InvokeOnKitChanged(UCPlayer player, Kit? kit, Kit? oldKit)
    {
        if (OnKitAccessChanged != null)
            UCWarfare.RunOnMainThread(() => OnKitChanged?.Invoke(player, kit, oldKit));
    }
    public static void InvokeOnManualKitChanged(UCPlayer player, Kit? kit, Kit? oldKit)
    {
        if (OnManualKitChanged != null)
            UCWarfare.RunOnMainThread(() => OnManualKitChanged?.Invoke(player, kit, oldKit));
    }

    public async Task<Kit?> GetKit(uint pk, CancellationToken token = default, Func<IKitsDbContext, IQueryable<Kit>>? set = null)
    {
        await using IKitsDbContext dbContext = new WarfareDbContext();

        return await GetKit(dbContext, pk, token, set);
    }
    public async Task<Kit?> GetKit(IKitsDbContext dbContext, uint pk, CancellationToken token = default, Func<IKitsDbContext, IQueryable<Kit>>? set = null)
    {
        Kit? kit = await (set?.Invoke(dbContext) ?? Set(dbContext)).FirstOrDefaultAsync(x => x.PrimaryKey == pk, token).ConfigureAwait(false);
        if (kit != null)
            Cache.OnKitUpdated(kit, dbContext);

        return kit;
    }

    /// <remarks>Thread Safe</remarks>
    public async Task<Kit?> FindKit(string id, CancellationToken token = default, bool exactMatchOnly = true, Func<IKitsDbContext, IQueryable<Kit>>? set = null)
    {
        await using IKitsDbContext dbContext = new WarfareDbContext();

        return await FindKit(dbContext, id, token, exactMatchOnly, set);
    }
    public async Task<Kit?> FindKit(IKitsDbContext dbContext, string id, CancellationToken token = default, bool exactMatchOnly = true, Func<IKitsDbContext, IQueryable<Kit>>? set = null)
    {
        Kit? kit = Cache.GetKit(id);

        if (kit != null)
            id = kit.InternalName;

        IQueryable<Kit> queryable = (set?.Invoke(dbContext) ?? Set(dbContext));
        kit = await queryable.FirstOrDefaultAsync(x => x.InternalName == id, token).ConfigureAwait(false);
        if (kit != null)
        {
            Cache.OnKitUpdated(kit, dbContext);
            return kit;
        }

        if (exactMatchOnly)
            return null;

        KeyValuePair<uint, Kit>[] list = Cache.KitDataByKey.ToArray();
        int index = F.StringIndexOf(list, x => x.Value.InternalName, id, exactMatchOnly);
        if (index == -1)
            index = F.StringIndexOf(list, x => x.Value.GetDisplayName(null, true), id, exactMatchOnly);

        kit = index == -1 ? null : list[index].Value;
        if (kit == null)
            return null;
        id = kit.InternalName;
        kit = await queryable.FirstOrDefaultAsync(x => x.InternalName == id, token).ConfigureAwait(false);
        if (kit != null)
            Cache.OnKitUpdated(kit, dbContext);

        return kit;
    }
    void IPlayerConnectListener.OnPlayerConnecting(UCPlayer player)
    {
        PlayerEquipment equipment = player.Player.equipment;
        for (int i = 0; i < 8; ++i)
            equipment.ServerClearItemHotkey((byte)i);

        ((IPlayerConnectListener)Cache).OnPlayerConnecting(player);
    }
    void IPlayerDisconnectListener.OnPlayerDisconnecting(UCPlayer player)
    {
        ((IPlayerDisconnectListener)Cache).OnPlayerDisconnecting(player);

        if (player.KitMenuData is { FavoritesDirty: true, FavoriteKits: { } fk })
        {
            UCWarfare.RunTask(SaveFavorites, player, fk, CancellationToken.None);
        }
    }
    Task ITCPConnectedListener.OnConnected(CancellationToken token)
    {
        return ((ITCPConnectedListener)Boosting).OnConnected(token);
    }
    void IGameTickListener.Tick()
    {
        if (Data.Gamemode == null || !Data.Gamemode.EveryMinute || Provider.clients.Count <= 0)
            return;

        UCWarfare.RunTask(SaveAllPlayerFavorites, CancellationToken.None, ctx: "Save all players' favorite kits.");
    }
    public async Task<Kit?> GetKitFromSign(BarricadeDrop drop, UCPlayer looker, CancellationToken token = default)
    {
        await UniTask.SwitchToMainThread(token);
        Kit? kit = Warfare.Signs.GetKitFromSign(drop, out int loadoutId);
        if (kit is null && loadoutId > 0)
        {
            kit = await Loadouts.GetLoadout(looker, loadoutId, token).ConfigureAwait(false);
            return kit;
        }

        return kit;
    }
    public async Task SaveAllPlayerFavorites(CancellationToken token)
    {
        await UniTask.SwitchToMainThread(token);
        List<Task> tasks = new List<Task>(4);
        foreach (UCPlayer player in PlayerManager.OnlinePlayers)
        {
            if (player.KitMenuData is not { FavoritesDirty: true, FavoriteKits: { } fk })
                continue;

            tasks.Add(SaveFavorites(player, fk, token));
        }

        await Task.WhenAll(tasks);
    }
    private async Task ValidateKits(CancellationToken token = default)
    {
        await using WarfareDbContext dbContext = new WarfareDbContext();
        List<Kit> allKits = await Set(dbContext)
            .Include(x => x.MapFilter)
            .Include(x => x.FactionFilter)
            .Include(x => x.ItemModels)
            .Include(x => x.UnlockRequirementsModels)
            .Include(x => x.Skillsets)
            .Include(x => x.Faction)
            .Where(x => !x.Disabled).ToListAsync(token);

        bool any = false;
        foreach (Kit kit in allKits)
        {
            ValidateKit(kit, dbContext);
            any |= kit.IsLoadDirty;
        }

        if (any)
            await dbContext.SaveChangesAsync(token).ConfigureAwait(false);
    }
    private async Task CountTranslations(CancellationToken token = default)
    {
        await using IKitsDbContext dbContext = new WarfareDbContext();

        List<Kit> allKits = await dbContext.Kits
            .Include(x => x.Translations)
            .Include(x => x.FactionFilter)
            .Include(x => x.MapFilter)
            .Where(x => x.Type == KitType.Public || x.Type == KitType.Elite)
            .ToListAsync(token).ConfigureAwait(false);

        await UniTask.SwitchToMainThread(token);

        Localization.ClearSection(TranslationSection.Kits);
        int ct = 0;

        foreach (Kit kit in allKits)
        {
            if (kit is not { Type: KitType.Public or KitType.Elite, Requestable: true })
                continue;

            ++ct;
            foreach (KitTranslation language in kit.Translations)
            {
                if (Data.LanguageDataStore.GetInfoCached(language.LanguageId) is { } langInfo)
                    langInfo.IncrementSection(TranslationSection.Kits, 1);
            }
        }

        Localization.IncrementSection(TranslationSection.Kits, ct);
    }
    private async Task CreateMissingDefaultKits(CancellationToken token = default)
    {
        bool needsT1Unarmed = false, needsT2Unarmed = false, needsDefault = false;

        Kit? kit = !TeamManager.Team1UnarmedKit.HasValue ? null : await GetKit(TeamManager.Team1UnarmedKit.Value, token).ConfigureAwait(false);
        if (kit == null)
        {
            needsT1Unarmed = true;
            L.LogWarning("Team 1's unarmed kit was not found, an attempt will be made to auto-generate one.");
        }

        kit = !TeamManager.Team2UnarmedKit.HasValue ? null : await GetKit(TeamManager.Team2UnarmedKit.Value, token).ConfigureAwait(false);
        if (kit == null)
        {
            needsT2Unarmed = true;
            L.LogWarning("Team 2's unarmed kit was not found, an attempt will be made to auto-generate one.");
        }

        kit = string.IsNullOrEmpty(TeamManager.DefaultKit) ? null : await FindKit(TeamManager.DefaultKit, token).ConfigureAwait(false);
        if (kit == null)
        {
            needsDefault = true;
            L.LogWarning("The default kit, \"" + TeamManager.DefaultKit + "\", was not found, an attempt will be made to auto-generate one.");
        }

        if (!needsDefault && !needsT1Unarmed && !needsT2Unarmed)
            return;

        if (needsT1Unarmed || needsT2Unarmed)
        {
            await using IFactionDbContext factionDbContext = new WarfareDbContext();
            FactionInfo team1Faction = TeamManager.Team1Faction;
            FactionInfo team2Faction = TeamManager.Team2Faction;
            uint k1 = team1Faction.PrimaryKey.Key, k2 = team2Faction.PrimaryKey.Key;

            Faction? t1Faction = needsT1Unarmed ? await factionDbContext.Factions.FirstOrDefaultAsync(x => x.Key == k1, token).ConfigureAwait(false) : null;
            Faction? t2Faction = needsT2Unarmed ? await factionDbContext.Factions.FirstOrDefaultAsync(x => x.Key == k2, token).ConfigureAwait(false) : null;

            if (needsT1Unarmed)
            {
                Kit newKit = await Defaults.CreateDefaultKit(team1Faction, team1Faction.KitPrefix + "unarmed", token).ConfigureAwait(false);

                team1Faction.UnarmedKit = newKit.PrimaryKey;
                if (t1Faction != null)
                {
                    t1Faction.UnarmedKitId = newKit.PrimaryKey;
                    factionDbContext.Update(t1Faction);
                }
                else L.LogError("Failed to find faction for team 1.");

                L.Log("Created default kit for team 1: \"" + newKit.GetDisplayName() + "\".");
            }

            if (needsT2Unarmed)
            {
                Kit newKit = await Defaults.CreateDefaultKit(team2Faction, team2Faction.KitPrefix + "unarmed", token).ConfigureAwait(false);

                team2Faction.UnarmedKit = newKit.PrimaryKey;
                if (t2Faction != null)
                {
                    t2Faction.UnarmedKitId = newKit.PrimaryKey;
                    factionDbContext.Update(t2Faction);
                }
                else L.LogError("Failed to find faction for team 2.");

                L.Log("Created default kit for team 2: \"" + newKit.GetDisplayName() + "\".");
            }

            await factionDbContext.SaveChangesAsync(token).ConfigureAwait(false);
        }

        if (needsDefault && !string.IsNullOrEmpty(TeamManager.DefaultKit))
        {
            Kit newKit = await Defaults.CreateDefaultKit(null, TeamManager.DefaultKit, token);
        }
    }
    public async Task<IPageKitItem?> GetHeldItemFromKit(UCPlayer player, CancellationToken token = default)
    {
        await UniTask.SwitchToMainThread(token);
        ItemJar? held = player.GetHeldItem(out byte page);
        return held != null ? await GetItemFromKit(player, held, (Page)page, token).ConfigureAwait(false) : null;
    }

    /// <summary>Gets the exact <see cref="IKitItem"/> and <see cref="IPageKitItem"/> that the player is holding from their kit (accounts for the item being moved).</summary>
    public Task<IPageKitItem?> GetItemFromKit(UCPlayer player, ItemJar jar, Page page, CancellationToken token = default) =>
        GetItemFromKit(player, jar.x, jar.y, jar.item, page, token);
    /// <summary>Gets the exact <see cref="IKitItem"/> and <see cref="IPageKitItem"/> that the player is holding from their kit (accounts for the item being moved).</summary>
    public async Task<IPageKitItem?> GetItemFromKit(UCPlayer player, byte x, byte y, Item item, Page page, CancellationToken token = default)
    {
        if (player.CachedActiveKitInfo is { ItemModels.Count: > 0 })
        {
            await UniTask.SwitchToMainThread(token);
            if (player.CachedActiveKitInfo is { ItemModels.Count: > 0 })
                return GetItemFromKit(player, player.CachedActiveKitInfo, x, y, item, page);
        }

        Kit? kit = await player.GetActiveKit(token, x => x.Kits.Include(y => y.ItemModels)).ConfigureAwait(false);

        if (kit is null)
            return null;

        await UniTask.SwitchToMainThread(token);

        return GetItemFromKit(player, kit, x, y, item, page);
    }
    /// <summary>Gets the exact <see cref="IKitItem"/> and <see cref="IPageKitItem"/> that the player is holding from their kit (accounts for the item being moved).</summary>
    public IPageKitItem? GetItemFromKit(UCPlayer player, Kit kit, ItemJar jar, Page page)
        => GetItemFromKit(player, kit, jar.x, jar.y, jar.item, page);
    /// <summary>Gets the exact <see cref="IKitItem"/> and <see cref="IPageKitItem"/> that the player is holding from their kit (accounts for the item being moved).</summary>
    public IPageKitItem? GetItemFromKit(UCPlayer player, Kit kit, byte x, byte y, Item item, Page page)
    {
        L.LogDebug("Looking for kit item: " + (item.GetAsset()?.itemName ?? "null") + $" at {page}, ({x}, {y}).");
        for (int i = 0; i < player.ItemTransformations.Count; ++i)
        {
            ItemTransformation t = player.ItemTransformations[i];
            if (t.Item != item)
                continue;

            if (t.NewX != x || t.NewY != y || t.NewPage != page)
                continue;

            L.LogDebug($"Transforming: {t.OldPage} <- {page}, ({t.OldX} <- {x}, {t.OldY} <- {y}).");
            x = t.OldX;
            y = t.OldY;
            page = t.OldPage;
            break;
        }

        ItemAsset asset = item.GetAsset();
        if (asset == null)
            return null;
        FactionInfo? faction = TeamManager.GetFactionSafe(player.GetTeam());
        foreach (IKitItem item2 in kit.Items)
        {
            if (item2 is not IPageKitItem jar2 || jar2.Page != page || jar2.X != x || jar2.Y != y)
                continue;

            if (jar2 is ISpecificKitItem { Item.Id: 0 } pgItem)
            {
                if (pgItem.Item.Equals(asset.GUID))
                    return jar2;
            }
            else if (item2.GetItem(kit, faction, out _, out _) is { } asset2 && asset2.GUID == asset.GUID)
                return jar2;

            break;
        }

        L.LogDebug($"Kit item at: {page}, ({x}, {y}) not found.");
        return null;
    }
    void IQuestCompletedHandler.OnQuestCompleted(QuestCompleted e)
    {
        ((IQuestCompletedHandler)Cache).OnQuestCompleted(e);
        Signs.UpdateSigns(e.Player);
    }
    private void OnPlayerLeaving(PlayerEvent e) => OnTeamPlayerCountChanged();
    private void OnPlayerJoined(PlayerJoined e) => OnTeamPlayerCountChanged();
    private void OnGroupChanged(GroupChanged e) => OnTeamPlayerCountChanged(e.Player);
    internal void OnTeamPlayerCountChanged(UCPlayer? allPlayer = null)
    {
        if (allPlayer != null)
            Warfare.Signs.UpdateKitSigns(allPlayer, null);

        foreach (Kit item in Cache.KitDataByKey.Values)
        {
            if (item.TeamLimit < 1f)
                Signs.UpdateSigns(item);
        }
    }
    private void OnPreDeath(PlayerLife life)
    {
        UCPlayer? player = UCPlayer.FromPlayer(life.player);
        
        if (player == null || !player.ActiveKit.HasValue)
            return;

        Kit? active = Cache.GetKit(player.ActiveKit.Value);
        if (active == null)
            return;
        
        for (byte page = 0; page < PlayerInventory.PAGES - 2; page++)
        {
            for (int index = life.player.inventory.getItemCount(page) - 1; index >= 0; --index)
            {
                ItemJar jar = life.player.inventory.getItem(page, (byte)index);

                if (Assets.find(EAssetType.ITEM, jar.item.id) is not ItemAsset asset)
                    continue;
                
                float percentage = (float)jar.item.amount / asset.amount;

                bool notInKit = !active.ContainsItem(asset.GUID, player == null ? 0 : player.GetTeam()) && Whitelister.IsWhitelisted(asset.GUID, out _);
                if (notInKit || (percentage < 0.3f && asset.type != EItemType.GUN))
                {
                    if (notInKit)
                        ItemManager.dropItem(jar.item, life.player.transform.position, false, true, true);

                    life.player.inventory.removeItem(page, (byte)index);
                }
            }
        }
    }
    private void OnItemDropped(ItemDropped e)
    {
        UCPlayer pl = e.Player;
        if (e.Item != null)
            pl.ItemDropTransformations.Add(new ItemDropTransformation(e.OldPage, e.OldX, e.OldY, e.Item));

        if (e.Player.HotkeyBindings is not { Count: > 0 })
            return;
        CancellationToken tkn = UCWarfare.UnloadCancel;
        CombinedTokenSources tokens = tkn.CombineTokensIfNeeded(e.Player.DisconnectToken, Data.Gamemode != null ? Data.Gamemode.UnloadToken : default);
        if (e.Item == null)
            return;

        // move hotkey to a different item of the same type
        UCWarfare.RunTask(static async (mngr, ev, tokens) =>
        {
            IPageKitItem? jar2 = await mngr.GetItemFromKit(ev.Player, ev.OldX, ev.OldY, ev.Item!, ev.OldPage, tokens.Token).ConfigureAwait(false);
            if (jar2 == null || !ev.Player.IsOnline)
                return;

            await ev.Player.PurchaseSync.WaitAsync(tokens.Token).ConfigureAwait(false);
            try
            {
                if (!ev.Player.IsOnline || ev.Player.HotkeyBindings is not { Count: > 0 })
                    return;

                Kit? activeKit = await ev.Player.GetActiveKit(tokens.Token).ConfigureAwait(false);

                await UCWarfare.ToUpdate(tokens.Token);
                if (!ev.Player.IsOnline || ev.Player.HotkeyBindings is not { Count: > 0 })
                    return;

                for (int i = 0; i < ev.Player.HotkeyBindings.Count; ++i)
                {
                    HotkeyBinding b = ev.Player.HotkeyBindings[i];

                    if ((b.Item is not ISpecificKitItem item || jar2 is not ISpecificKitItem item2 || item.Item != item2.Item) &&
                        (b.Item is not IAssetRedirectKitItem redir || jar2 is not IAssetRedirectKitItem redir2 || redir.RedirectType != redir2.RedirectType))
                    {
                        continue;
                    }

                    // found a binding for that item
                    if (b.Item.X != jar2.X || b.Item.Y != jar2.Y || b.Item.Page != jar2.Page)
                        continue;
                    ItemAsset? asset = b.Item switch
                    {
                        ISpecificKitItem item3 => item3.Item.GetAsset<ItemAsset>(),
                        IKitItem ki => ki.GetItem(activeKit, TeamManager.GetFactionSafe(ev.Player.GetTeam()), out _, out _),
                        _ => null
                    };
                    if (asset == null)
                        return;
                    int hotkeyIndex = KitEx.GetHotkeyIndex(b.Slot);
                    if (hotkeyIndex == byte.MaxValue)
                        return;
                    PlayerInventory inv = ev.Player.Player.inventory;
                    // find new item to bind the item to
                    for (int p = PlayerInventory.SLOTS; p < PlayerInventory.STORAGE; ++p)
                    {
                        SDG.Unturned.Items page = inv.items[p];
                        int c = page.getItemCount();
                        for (int index = 0; index < c; ++index)
                        {
                            ItemJar jar = page.getItem((byte)index);
                            if (jar.x == jar2.X && jar.y == jar2.Y && p == (int)jar2.Page)
                                continue;

                            if (jar.GetAsset() is not { } asset2 || asset2.GUID != asset.GUID || !KitEx.CanBindHotkeyTo(asset2, (Page)p))
                                continue;

                            ev.Player.Player.equipment.ServerBindItemHotkey((byte)hotkeyIndex, asset, (byte)p, jar.x, jar.y);
                            return;
                        }
                    }

                    break;
                }
            }
            finally
            {
                tokens.Dispose();
                ev.Player.PurchaseSync.Release();
            }
        }, this, e, tokens, ctx: "Set keybind to new item after it's dropped.");
    }
    private void OnItemPickedUp(ItemPickedUp e)
    {
        UCPlayer pl = e.Player;
        if (e.Jar == null)
            return;

        byte origX = byte.MaxValue, origY = byte.MaxValue;
        Page origPage = (Page)byte.MaxValue;
        for (int i = 0; i < pl.ItemDropTransformations.Count; ++i)
        {
            ItemDropTransformation d = pl.ItemDropTransformations[i];
            if (d.Item != e.Jar.item)
                continue;
            
            for (int j = 0; j < pl.ItemTransformations.Count; ++j)
            {
                ItemTransformation t = pl.ItemTransformations[j];
                if (t.Item != e.Jar.item)
                    continue;

                pl.ItemTransformations[j] = new ItemTransformation(t.OldPage, e.Page, t.OldX, t.OldY, e.X, e.Y, t.Item);
                origX = t.OldX;
                origY = t.OldY;
                origPage = t.OldPage;
                goto rebind;
            }

            origX = d.OldX;
            origY = d.OldY;
            origPage = d.OldPage;
            pl.ItemTransformations.Add(new ItemTransformation(d.OldPage, e.Page, d.OldX, d.OldY, e.X, e.Y, e.Jar.item));
            pl.ItemDropTransformations.RemoveAtFast(i);
            break;
        }
        rebind:
        // resend hotkeys from picked up item
        if (!UCWarfare.IsLoaded || e.Player.HotkeyBindings == null || origX >= byte.MaxValue)
            return;

        CancellationToken tkn = UCWarfare.UnloadCancel;
        CombinedTokenSources tokens = tkn.CombineTokensIfNeeded(e.Player.DisconnectToken, Data.Gamemode != null ? Data.Gamemode.UnloadToken : default);
        UCWarfare.RunTask(async tokens =>
        {
            tokens.Token.ThrowIfCancellationRequested();

            await e.Player.PurchaseSync.WaitAsync(tokens.Token).ConfigureAwait(false);
            try
            {
                if (e.Player.HotkeyBindings == null)
                    return;

                Kit? activeKit = await e.Player.GetActiveKit(tokens.Token).ConfigureAwait(false);
                if (activeKit == null)
                    return;

                await UCWarfare.ToUpdate(tokens.Token);

                foreach (HotkeyBinding binding in e.Player.HotkeyBindings)
                {
                    if (binding.Kit != activeKit.PrimaryKey || binding.Item.X != origX || binding.Item.Y != origY || binding.Item.Page != origPage)
                        continue;

                    ItemAsset? asset = binding.GetAsset(activeKit, e.Player.GetTeam());
                    if (asset == null)
                        continue;

                    byte index = KitEx.GetHotkeyIndex(binding.Slot);
                    if (index == byte.MaxValue || !KitEx.CanBindHotkeyTo(asset, e.Page)) continue;
                    e.Player.Player.equipment.ServerBindItemHotkey(index, asset, (byte)e.Page, e.X, e.Y);
                    // L.LogDebug($"Updating old hotkey (picked up): {asset.itemName} at {e.Page}, ({e.X}, {e.Y}).");
                    break;
                }
            }
            finally
            {
                tokens.Dispose();
                e.Player.PurchaseSync.Release();
            }

        }, tokens, ctx: "Checking for new binding of picked up item.");
    }
    private void OnItemMoved(ItemMoved e)
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
                if (t.Item != e.Jar.item)
                    continue;

                pl.ItemTransformations[i] = new ItemTransformation(t.OldPage, e.NewPage, t.OldX, t.OldY, e.NewX, e.NewY, t.Item);
                origX = t.OldX;
                origY = t.OldY;
                origPage = t.OldPage;
                goto swap;
            }

            pl.ItemTransformations.Add(new ItemTransformation(e.OldPage, e.NewPage, e.OldX, e.OldY, e.NewX, e.NewY, e.Jar.item));
            origX = e.OldX;
            origY = e.OldY;
            origPage = e.OldPage;
        }

        swap:
        if (e is { IsSwap: true, SwappedJar: not null })
        {
            for (int i = 0; i < pl.ItemTransformations.Count; ++i)
            {
                ItemTransformation t = pl.ItemTransformations[i];
                if (t.Item != e.SwappedJar.item)
                    continue;

                pl.ItemTransformations[i] = new ItemTransformation(t.OldPage, e.OldPage, t.OldX, t.OldY, e.OldX, e.OldY, t.Item);
                swapOrigX = t.OldX;
                swapOrigY = t.OldY;
                swapOrigPage = t.OldPage;
                goto rebind;
            }

            pl.ItemTransformations.Add(new ItemTransformation(e.NewPage, e.OldPage, e.NewX, e.NewY, e.OldX, e.OldY, e.SwappedJar.item));
            swapOrigX = e.NewX;
            swapOrigY = e.NewY;
            swapOrigPage = e.NewPage;
        }

        rebind:
        // resend hotkeys from moved item(s)
        if (!UCWarfare.IsLoaded || e.Player.HotkeyBindings == null || (origX >= byte.MaxValue && swapOrigX >= byte.MaxValue))
            return;

        CancellationToken tkn = UCWarfare.UnloadCancel;
        CombinedTokenSources tokens = tkn.CombineTokensIfNeeded(e.Player.DisconnectToken, Data.Gamemode != null ? Data.Gamemode.UnloadToken : default);
        UCWarfare.RunTask(async tokens =>
        {
            tokens.Token.ThrowIfCancellationRequested();

            await e.Player.PurchaseSync.WaitAsync(tokens.Token).ConfigureAwait(false);
            try
            {
                if (e.Player.HotkeyBindings == null)
                    return;

                Kit? kit = await e.Player.GetActiveKit(tokens.Token).ConfigureAwait(false);
                if (kit is null)
                    return;

                await UCWarfare.ToUpdate(tokens.Token);

                foreach (HotkeyBinding binding in e.Player.HotkeyBindings)
                {
                    if (binding.Kit != kit.PrimaryKey)
                        continue;

                    byte index = KitEx.GetHotkeyIndex(binding.Slot);
                    if (index == byte.MaxValue)
                        continue;

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
            finally
            {
                tokens.Dispose();
                e.Player.PurchaseSync.Release();
            }

        }, tokens, ctx: "Checking for new binding of moved item.");
    }
    private void OnSwapClothingRequested(SwapClothingRequested e)
    {
        if (e.Player.OnDuty()) return;

        if (e.Player.HasKit && e.Type is ClothingType.Backpack or ClothingType.Pants or ClothingType.Shirt or ClothingType.Vest)
        {
            e.Player.SendChat(T.NoRemovingClothing);
            e.Break();
        }
    }

    /// <remarks>Thread Safe</remarks>
    public Task TryGiveKitOnJoinTeam(UCPlayer player, CancellationToken token = default)
        => TryGiveKitOnJoinTeam(player, player.GetTeam(), token);

    /// <remarks>Thread Safe</remarks>
    public async Task TryGiveKitOnJoinTeam(UCPlayer player, ulong team, CancellationToken token = default)
    {
        Kit? kit = await GetDefaultKit(team, token, x => RequestableSet(x, false)).ConfigureAwait(false);
        if (kit == null)
        {
            L.LogWarning("Unable to give " + player.CharacterName + " a kit.");
            await UniTask.SwitchToMainThread(token);
            ItemUtility.ClearInventoryAndSlots(player);
            return;
        }
        L.LogDebug($"Giving kit: {kit.InternalName} ({kit.PrimaryKey}).");
        await Requests.GiveKit(player, kit, false, false, token).ConfigureAwait(false);
    }
    public async Task<Kit?> TryGiveUnarmedKit(UCPlayer player, bool manual, CancellationToken token = default)
    {
        if (!player.IsOnline)
            return null;
        Kit? kit = await GetDefaultKit(player.GetTeam(), token, x => RequestableSet(x, false)).ConfigureAwait(false);
        if (kit == null || !player.IsOnline)
            return null;

        await Requests.GiveKit(player, kit, manual, true, token).ConfigureAwait(false);
        return kit;
    }
    public async Task<Kit?> TryGiveRiflemanKit(UCPlayer player, bool manual, bool tip, CancellationToken token = default)
    {
        await using IKitsDbContext dbContext = new WarfareDbContext();

        List<Kit> kits = await dbContext.Kits
            .Include(x => x.FactionFilter)
            .Include(x => x.MapFilter)
            .Include(x => x.UnlockRequirementsModels)
            .Where(x => x.Class == Class.Rifleman && !x.Disabled)
            .ToListAsync(token).ConfigureAwait(false);

        ulong t2 = player.GetTeam();
        FactionInfo? t = player.Faction;
        Kit? rifleman = kits.FirstOrDefault(k =>
            !k.IsFactionAllowed(t) &&
            !k.IsCurrentMapAllowed() &&
            (k is { Type: KitType.Public, CreditCost: <= 0 } || HasAccessQuick(k, player)) &&
            !k.IsLimited(out _, out _, t2, false) &&
            !k.IsClassLimited(out _, out _, t2, false) &&
            k.MeetsUnlockRequirements(player)
        );

        rifleman ??= await GetDefaultKit(t2, token).ConfigureAwait(false);
        if (rifleman != null)
        {
            uint id = rifleman.PrimaryKey;
            rifleman = await RequestableSet(dbContext, false)
                .FirstOrDefaultAsync(x => x.PrimaryKey == id, token).ConfigureAwait(false);
        }
        await Requests.GiveKit(player, rifleman, manual, tip, token).ConfigureAwait(false);
        return rifleman;
    }
    public async Task<Kit?> GetDefaultKit(ulong team, CancellationToken token = default, Func<IKitsDbContext, IQueryable<Kit>>? set = null)
    {
        uint? kitname = team == 1 ? TeamManager.Team1UnarmedKit : TeamManager.Team2UnarmedKit;

        Kit? kit = team is 1 or 2 && kitname.HasValue ? await GetKit(kitname.Value, token, set: set).ConfigureAwait(false) : null;

        if (kit is { Disabled: true })
            kit = null;

        kit ??= await FindKit(TeamManager.DefaultKit, token, set: set).ConfigureAwait(false);

        return kit;
    }
    public async Task<Kit?> GetRecommendedSquadleaderKit(ulong team, CancellationToken token = default)
    {
        await using IKitsDbContext dbContext = new WarfareDbContext();

        List<Kit> kits = await dbContext.Kits
            .Where(x => x.Class == Class.Squadleader && !x.Disabled && x.Type == KitType.Public).ToListAsync(token).ConfigureAwait(false);

        foreach (Kit kit in kits)
        {
            if (kit.IsPublicKit && kit.IsRequestable(team))
                return kit;
        }

        return null;
    }
    public Kit? GetRandomPublicKit()
    {
        ThreadUtil.assertIsGameThread();

        List<Kit> kits = new List<Kit>(Cache.KitDataByKey.Values.Where(x => x is { IsPublicKit: true, Requestable: true }));
        return kits.Count == 0 ? null : kits[RandomUtility.GetIndex((ICollection)kits)];
    }
    public bool TryCreateSquadOnRequestSquadleaderKit(CommandContext ctx)
    {
        if (ctx.Caller.Squad is not null && !ctx.Caller.IsSquadLeader())
        {
            ctx.Reply(T.RequestKitNotSquadleader);
            return false;
        }

        if (ctx.Caller.Squad is not null)
            return false;
        
        ulong team = ctx.Caller.GetTeam();
        if (SquadManager.Squads.Count(x => x.Team == team) < 8)
        {
            // create a squad automatically if someone requests a squad leader kit.
            Squad squad = SquadManager.CreateSquad(ctx.Caller, team);
            ctx.Reply(T.SquadCreated, squad);
            return true;
        }

        ctx.Reply(T.SquadsTooMany, SquadManager.ListUI.Squads.Length);
        return false;
    }
    private static void ValidateKit(Kit kit, IDbContext context)
    {
        if (kit.FactionFilter != null)
        {
            // duplicate faction filters
            for (int i = 0; i < kit.FactionFilter.Count; ++i)
            {
                KitFilteredFaction comparer = kit.FactionFilter[i];
                for (int j = kit.FactionFilter.Count - 1; j > i; --j)
                {
                    if (comparer.FactionId != kit.FactionFilter[j].FactionId)
                        continue;

                    context.Remove(kit.FactionFilter[j]);
                    kit.FactionFilter.RemoveAt(j);
                    L.LogWarning($"Duplicate faction filter found in kit \"{kit.InternalName}\" {kit.PrimaryKey}: {comparer.Faction.Name}.");
                    kit.IsLoadDirty = true;
                }
            }

            // primary faction missing from whitelist
            if (kit is { FactionFilterIsWhitelist: true, FactionId: not null, Faction: not null } && !kit.FactionFilter.Exists(x => x.FactionId == kit.FactionId.Value))
            {
                KitFilteredFaction faction = new KitFilteredFaction
                {
                    Faction = kit.Faction,
                    FactionId = kit.FactionId.Value,
                    Kit = kit,
                    KitId = kit.PrimaryKey
                };
                kit.FactionFilter.Add(faction);
                context.Add(faction);
                L.LogWarning($"Faction was not in whitelist for kit \"{kit.InternalName}\" {kit.PrimaryKey}: {kit.Faction.Name}.");
                kit.IsLoadDirty = true;
            }
        }
        if (kit.MapFilter != null)
        {
            // duplicate map filters
            for (int i = 0; i < kit.MapFilter.Count; ++i)
            {
                KitFilteredMap comparer = kit.MapFilter[i];
                for (int j = kit.MapFilter.Count - 1; j > i; --j)
                {
                    if (comparer.Map != kit.MapFilter[j].Map)
                        continue;

                    context.Remove(kit.MapFilter[j]);
                    kit.MapFilter.RemoveAt(j);
                    L.LogWarning($"Duplicate map filter found in kit \"{kit.InternalName}\" {kit.PrimaryKey}: {MapScheduler.GetMapName(comparer.Map)}.");
                    kit.IsLoadDirty = true;
                }
            }
        }
        if (kit.Skillsets != null)
        {
            // duplicate skillset types
            for (int i = 0; i < kit.Skillsets.Count; ++i)
            {
                KitSkillset comparer = kit.Skillsets[i];
                for (int j = kit.Skillsets.Count - 1; j > i; --j)
                {
                    if (!comparer.Skillset.TypeEquals(kit.Skillsets[j].Skillset))
                        continue;

                    context.Remove(kit.Skillsets[j]);
                    L.LogWarning($"Duplicate skillset found in kit \"{kit.InternalName}\" {kit.PrimaryKey}: {kit.Skillsets[j].Skillset}.");
                    kit.Skillsets.RemoveAt(j);
                    kit.IsLoadDirty = true;
                }
            }
        }
        if (kit.UnlockRequirementsModels != null)
        {
            // duplicate unlock requirements
            for (int i = 0; i < kit.UnlockRequirementsModels.Count; ++i)
            {
                KitUnlockRequirement comparer = kit.UnlockRequirementsModels[i];
                for (int j = kit.UnlockRequirementsModels.Count - 1; j > i; --j)
                {
                    if (!comparer.Json.Equals(kit.UnlockRequirementsModels[j].Json, StringComparison.OrdinalIgnoreCase))
                        continue;

                    L.LogWarning($"Duplicate unlock requirement found in kit \"{kit.InternalName}\" {kit.PrimaryKey}: {kit.UnlockRequirementsModels[i].Json}.");
                    context.Remove(kit.UnlockRequirementsModels[j]);
                    kit.UnlockRequirementsModels.RemoveAt(j);
                    kit.IsLoadDirty = true;
                }
            }
        }

        if (kit.ItemModels == null)
            return;

        // duplicate items
        for (int i = 0; i < kit.ItemModels.Count; ++i)
        {
            KitItemModel comparer = kit.ItemModels[i];
            for (int j = kit.ItemModels.Count - 1; j > i; --j)
            {
                if (!comparer.Equals(kit.ItemModels[j]))
                    continue;

                L.LogWarning($"Duplicate item found in kit \"{kit.InternalName}\" {kit.PrimaryKey}:{Environment.NewLine}{comparer.CreateRuntimeItem()}.");
                context.Remove(kit.ItemModels[j]);
                kit.ItemModels.RemoveAt(j);
                kit.IsLoadDirty = true;
            }
        }

        List<int>? alreadyChecked = null;
        // conflicting items
        for (int i = 0; i < kit.ItemModels.Count; ++i)
        {
            KitItemModel comparer = kit.ItemModels[i];
            if (alreadyChecked != null && alreadyChecked.Contains(i))
                continue;
            if (comparer.ClothingSlot.HasValue)
            {
                for (int j = kit.ItemModels.Count - 1; j >= 0; --j)
                {
                    if (i == j)
                        continue;
                    KitItemModel item = kit.ItemModels[j];
                    if (item.ClothingSlot.HasValue && item.ClothingSlot.Value == comparer.ClothingSlot.Value)
                        L.LogError($"Conflicting item found in kit \"{kit.InternalName}\" {kit.PrimaryKey}:{Environment.NewLine}{comparer.CreateRuntimeItem()} / {item.CreateRuntimeItem()}.");
                }
            }
            else if (comparer is { X: not null, Y: not null, Page: not null })
            {
                comparer.TryGetItemSize(out byte sizeX, out byte sizeY);
                for (int j = kit.ItemModels.Count - 1; j >= 0; --j)
                {
                    if (i == j || kit.ItemModels[j] is not { X: not null, Y: not null, Page: not null } item || item.Page != comparer.Page)
                        continue;

                    item.TryGetItemSize(out byte sizeX2, out byte sizeY2);
                    if (!ItemUtility.IsOverlapping(comparer.X.Value, comparer.Y.Value, sizeX, sizeY, item.X.Value, item.Y.Value, sizeX2, sizeY2, comparer.Rotation ?? 0, item.Rotation ?? 0))
                        continue;

                    if (alreadyChecked != null && alreadyChecked.Contains(j))
                        continue;

                    (alreadyChecked ??= []).Add(j);
                    L.LogError($"Overlapping item found in kit \"{kit.InternalName}\" {kit.PrimaryKey}:{Environment.NewLine}{comparer.CreateRuntimeItem()} / {item.CreateRuntimeItem()}.");
                }
            }
        }
    }

    private async Task SetupPlayer(UCPlayer player, CancellationToken token = default)
    {
        ulong team = player.GetTeam();
        Kit? kit = await player.GetActiveKit(token).ConfigureAwait(false);
        if (kit == null || !kit.Requestable || (kit.Type != KitType.Loadout && kit.IsLimited(out _, out _, team)) || (kit.Type == KitType.Loadout && kit.IsClassLimited(out _, out _, team)))
            await TryGiveRiflemanKit(player, false, false, token).ConfigureAwait(false);
        else if (UCWarfare.Config.ModifySkillLevels)
        {
            if (kit.Skillsets is { Count: > 0 })
                player.EnsureSkillsets(kit.Skillsets.Select(x => x.Skillset));
            else
                player.EnsureDefaultSkillsets();
        }
    }
    async Task IPlayerPostInitListenerAsync.OnPostPlayerInit(UCPlayer player, bool wasAlreadyOnline, CancellationToken token)
    {
        if (Data.Gamemode is not TeamGamemode { UseTeamSelector: true })
        {
            _ = RefreshFavorites(player, false, token);
            await SetupPlayer(player, token).ConfigureAwait(false);
            return;
        }
        //ItemUtility.ClearInventory(player);
        player.EnsureDefaultSkillsets();
        _ = RefreshFavorites(player, false, token);
        _ = Boosting.IsNitroBoosting(player.Steam64, token);
        _ = Requests.RemoveKit(player, false, player.DisconnectToken);
    }

    /// <remarks>Thread Safe</remarks>
    public async Task<bool> GiveAccess(string kitId, UCPlayer player, KitAccessType type, CancellationToken token = default)
    {
        Kit? kit = await FindKit(kitId, token, set: x => x.Kits).ConfigureAwait(false);
        if (kit == null)
            return false;
        return await GiveAccess(kit, player, type, token).ConfigureAwait(false);
    }

    /// <remarks>Thread Safe</remarks>
    public async Task<bool> GiveAccess(string kitId, ulong player, KitAccessType type, CancellationToken token = default)
    {
        Kit? kit = await FindKit(kitId, token, set: x => x.Kits).ConfigureAwait(false);
        if (kit == null)
            return false;
        return await GiveAccess(kit, player, type, token).ConfigureAwait(false);
    }

    /// <remarks>Thread Safe</remarks>
    public async Task<bool> GiveAccess(Kit kit, UCPlayer player, KitAccessType type, CancellationToken token = default)
    {
        // todo make update type and timestamp
        if (!player.IsOnline)
        {
            return await GiveAccess(kit, player.Steam64, type, token).ConfigureAwait(false);
        }

        await player.PurchaseSync.WaitAsync(token).ConfigureAwait(false);
        try
        {
            if (player.AccessibleKits != null && player.AccessibleKits.Contains(kit.PrimaryKey))
                return true;
            bool alreadyApplied = await AddAccessRow(kit.PrimaryKey, player.Steam64, type, token).ConfigureAwait(false);
            if (!alreadyApplied)
                return false;

            (player.AccessibleKits ??= new List<uint>(4)).Add(kit.PrimaryKey);
            InvokeOnKitAccessChanged(kit, player.Steam64, true, type);

            return true;
        }
        finally
        {
            player.PurchaseSync.Release();
        }
    }

    /// <remarks>Thread Safe</remarks>
    public async Task<bool> GiveAccess(Kit kit, ulong player, KitAccessType type, CancellationToken token = default)
    {
        if (kit.PrimaryKey == 0)
            return false;
        UCPlayer? online = UCWarfare.IsLoaded ? UCPlayer.FromID(player) : null;
        if (online != null && online.IsOnline)
            return await GiveAccess(kit, online, type, token).ConfigureAwait(false);
        if (!await AddAccessRow(kit.PrimaryKey, player, type, token).ConfigureAwait(false))
            return false;
        InvokeOnKitAccessChanged(kit, player, true, type);
        return true;
    }

    /// <remarks>Thread Safe</remarks>
    public async Task<bool> RemoveAccess(string kitId, UCPlayer player, CancellationToken token = default)
    {
        Kit? kit = await FindKit(kitId, token, set: x => x.Kits).ConfigureAwait(false);
        if (kit == null)
            return false;
        return await RemoveAccess(kit, player.Steam64, token).ConfigureAwait(false);
    }
    /// <remarks>Thread Safe</remarks>
    public async Task<bool> RemoveAccess(string kitId, ulong player, CancellationToken token = default)
    {
        Kit? kit = await FindKit(kitId, token, set: x => x.Kits).ConfigureAwait(false);
        if (kit == null)
            return false;
        return await RemoveAccess(kit, player, token).ConfigureAwait(false);
    }
    /// <remarks>Thread Safe</remarks>
    public async Task<bool> RemoveAccess(Kit kit, UCPlayer player, CancellationToken token = default)
    {
        if (!player.IsOnline)
        {
            return await RemoveAccess(kit, player.Steam64, token).ConfigureAwait(false);
        }

        await player.PurchaseSync.WaitAsync(token).ConfigureAwait(false);
        bool access;
        try
        {
            access = await RemoveAccessRow(kit.PrimaryKey, player.Steam64, token).ConfigureAwait(false);
            if (access)
            {
                player.AccessibleKits?.Remove(kit.PrimaryKey);
                InvokeOnKitAccessChanged(kit, player.Steam64, false, KitAccessType.Unknown);
            }
        }
        finally
        {
            player.PurchaseSync.Release();
        }

        await Distribution.DequipKit(player, true, token);
        return access;
    }
    /// <remarks>Thread Safe</remarks>
    public async Task<bool> RemoveAccess(Kit kit, ulong player, CancellationToken token = default)
    {
        if (kit.PrimaryKey == 0)
            return false;
        UCPlayer? online = UCPlayer.FromID(player);
        if (online is { IsOnline: true })
            return await RemoveAccess(kit, online, token).ConfigureAwait(false);
        bool res = await RemoveAccessRow(kit.PrimaryKey, player, token).ConfigureAwait(false);
        InvokeOnKitAccessChanged(kit, player, false, KitAccessType.Unknown);
        return res;
    }

    /// <remarks>Thread Safe</remarks>
    public bool HasAccessQuick(Kit kit, UCPlayer player) => HasAccessQuick(kit.PrimaryKey, player);

    /// <remarks>Thread Safe</remarks>
    public bool HasAccessQuick(uint kit, UCPlayer player)
    {
        if (player.AccessibleKits == null || kit == 0)
            return false;
        uint k = kit;
        for (int i = 0; i < player.AccessibleKits.Count; ++i)
        {
            if (player.AccessibleKits[i] == k)
                return true;
        }

        return false;
    }

    /// <remarks>Thread Safe</remarks>
    public ValueTask<bool> HasAccessQuick(Kit kit, ulong player, CancellationToken token = default)
        => HasAccessQuick(kit.PrimaryKey, player, token);

    /// <remarks>Thread Safe</remarks>
    public Task<bool> HasAccess(Kit kit, UCPlayer player, CancellationToken token = default)
        => HasAccess(kit.PrimaryKey, player.Steam64, token);

    /// <remarks>Thread Safe</remarks>
    public Task<bool> HasAccess(Kit kit, ulong player, CancellationToken token = default)
        => HasAccess(kit.PrimaryKey, player, token);

    /// <remarks>Thread Safe</remarks>
    public Task<bool> HasAccess(uint kit, UCPlayer player, CancellationToken token = default)
        => HasAccess(kit, player.Steam64, token);

    /// <remarks>Thread Safe</remarks>
    public ValueTask<bool> HasAccessQuick(uint kit, ulong player, CancellationToken token = default)
    {
        if (kit == 0)
            return new ValueTask<bool>(false);
        UCPlayer? pl = UCPlayer.FromID(player);
        if (pl != null && pl.IsOnline)
            return new ValueTask<bool>(HasAccessQuick(kit, pl));
        return new ValueTask<bool>(HasAccess(kit, player, token));
    }
    /// <summary>Use with purchase sync.</summary>
    public bool IsFavoritedQuick(uint kit, UCPlayer player)
    {
        if (kit == 0)
            return false;

        if (player.KitMenuData is not { FavoriteKits: { } favs })
            return false;

        for (int i = 0; i < favs.Count; ++i)
        {
            if (favs[i] == kit)
                return true;
        }

        return false;
    }
    public async Task RefreshFavorites(UCPlayer player, bool psLock, CancellationToken token = default)
    {
        CombinedTokenSources tokens = token.CombineTokensIfNeeded(player.DisconnectToken, UCWarfare.UnloadCancel);
        if (psLock)
            await player.PurchaseSync.WaitAsync(token);
        try
        {
            await using IKitsDbContext dbContext = new WarfareDbContext();
            ulong s64 = player.Steam64;

            player.KitMenuData.FavoriteKits = await dbContext.KitFavorites.Where(x => x.Steam64 == s64)
                .Select(x => x.KitId).ToListAsync(token);

            player.KitMenuData.FavoritesDirty = false;
        }
        finally
        {
            if (psLock)
                player.PurchaseSync.Release();
        }

        UCWarfare.RunOnMainThread(() =>
        {
            tokens.Dispose();
            OnFavoritesRefreshed?.Invoke();
            MenuUI.OnFavoritesRefreshed(player);
            Warfare.Signs.UpdateKitSigns(player, null);
        }, true, token);
    }
    public string GetWeaponText(Kit kit)
    {
        IKitItem[] items = kit.Items;
        List<KeyValuePair<IPageKitItem, ItemGunAsset>> guns = new List<KeyValuePair<IPageKitItem, ItemGunAsset>>(2);
        for (int i = 0; i < items.Length; ++i)
        {
            if (items[i] is ISpecificKitItem item and IPageKitItem jar && item.Item.TryGetAsset(out ItemGunAsset asset))
            {
                for (int b = 0; b < BlacklistedWeaponsTextItems.Length; ++b)
                {
                    if (BlacklistedWeaponsTextItems[b] == asset.GUID)
                        goto skip;
                }

                guns.Add(new KeyValuePair<IPageKitItem, ItemGunAsset>(jar, asset));
                skip:;
            }
        }

        if (guns.Count == 0)
            return string.Empty;

        guns.Sort((a, b) => a.Key.Page.CompareTo(b.Key.Page));
        return string.Join(", ", guns.Select(x => x.Value.itemName.ToUpperInvariant()));
    }
    internal void OnKitAccessChangedIntl(Kit kit, ulong player, bool newAccess, KitAccessType type)
    {
        if (newAccess) return;
        UCPlayer? pl = UCPlayer.FromID(player);
        if (pl == null || kit == null)
            return;

        uint? activeKit = pl.ActiveKit;
        if (activeKit.HasValue && activeKit.Value == kit.PrimaryKey)
            UCWarfare.RunTask(Distribution.DequipKit, pl, true, kit, ctx: "Dequiping " + kit.InternalName + " from " + player + " (lost access).");
    }
    internal Task OnKitDeleted(Kit kit)
    {
        KitSync.OnKitDeleted(kit.PrimaryKey);
        UCWarfare.RunTask(Distribution.DequipKit, kit, true, ctx: "Dequiping " + kit.InternalName + " from all (deleted).");

        return Task.CompletedTask;
    }
    internal Task OnKitUpdated(Kit kit)
    {
        KitSync.OnKitUpdated(kit);

        return Task.CompletedTask;
    }
    internal void InvokeAfterMajorKitUpdate(Kit kit, bool manual)
    {
        ThreadUtil.assertIsGameThread();

        if (kit is null)
            return;

        BitArray mask = new BitArray(PlayerManager.OnlinePlayers.Count);
        for (int i = 0; i < PlayerManager.OnlinePlayers.Count; ++i)
        {
            UCPlayer player = PlayerManager.OnlinePlayers[i];
            uint? activeKit = player.ActiveKit;

            if (!activeKit.HasValue || activeKit.Value != kit.PrimaryKey)
                continue;

            player.ChangeKit(kit);
            mask[i] = true;
        }

        if (OnKitChanged != null)
        {
            // waits a frame in case something tries to lock the kit and to ensure we are on main thread.
            UCWarfare.RunOnMainThread(() =>
            {
                if (OnKitChanged == null)
                    return;
                for (int i = 0; i < PlayerManager.OnlinePlayers.Count; ++i)
                {
                    if (mask[i])
                        OnKitChanged.Invoke(PlayerManager.OnlinePlayers[i], kit, kit);
                }
            }, true);
        }

        if (manual && OnManualKitChanged != null)
        {
            UCWarfare.RunOnMainThread(() =>
            {
                if (OnManualKitChanged == null)
                    return;
                for (int i = 0; i < PlayerManager.OnlinePlayers.Count; ++i)
                {
                    if (mask[i])
                        OnManualKitChanged.Invoke(PlayerManager.OnlinePlayers[i], kit, kit);
                }
            }, true);
        }
    }
}

#if FALSE
// todo add delays to kits
[SingletonDependency(typeof(KitDataCache))]
public partial class KitManager : CachedEntityFrameworkSingleton<Kit>, IQuestCompletedHandlerAsync, IPlayerConnectListenerAsync, IPlayerPostInitListenerAsync, IJoinedTeamListenerAsync, IGameTickListener, IPlayerDisconnectListener, ITCPConnectedListener
{
    private static int _v;
    private static KitMenuUI? _menuUi;
    public static KitMenuUI MenuUI => _menuUi ??= new KitMenuUI();
    private readonly List<Kit> _kitListTemp = new List<Kit>(64);

    protected override DbSet<Kit> Set => Data.DbContext.Kits;
    public KitDataCache? Cache { get; private set; }
    public KitManager() : base("kits")
    {
        OnItemDeleted      += OnKitDeleted;
        OnItemUpdated      += OnKitUpdated;
        OnItemAdded        += OnKitUpdated;
        OnItemsRefreshed   += OnItemsRefreshedIntl;
        OnKitAccessChanged += OnKitAccessChangedIntl;
    }
    protected override async Task PostLoad(CancellationToken token)
    {
        Cache = Data.Singletons.GetSingleton<KitDataCache>();
        PlayerLife.OnPreDeath += OnPreDeath;
        EventDispatcher.GroupChanged += OnGroupChanged;
        EventDispatcher.PlayerJoined += OnPlayerJoined;
        EventDispatcher.PlayerLeft += OnPlayerLeaving;
        EventDispatcher.ItemMoved += OnItemMoved;
        EventDispatcher.ItemDropped += OnItemDropped;
        EventDispatcher.ItemPickedUp += OnItemPickedUp;
        EventDispatcher.SwapClothingRequested += OnSwapClothingRequested;
        //await MigrateOldKits(token).ConfigureAwait(false);
        List<Kit>? dirty = null;
        WriteWait(token);
        try
        {
            foreach (Kit kit in Items)
            {
                VerifyKitIntegrity(kit);
                if (kit is { IsLoadDirty: true })
                    (dirty ??= new List<Kit>()).Add(kit);
            }
        }
        finally
        {
            WriteRelease();
        }

        // await Data.PurchasingDataStore.RefreshBundles(true, false, token).ConfigureAwait(false);
        // await Data.PurchasingDataStore.RefreshKits(false, false, true, token).ConfigureAwait(false);
        // await Data.PurchasingDataStore.FetchStripeKitProducts(false, token).ConfigureAwait(false);

        if (dirty != null)
        {
            await UpdateRangeNoLock(dirty, token: token).ConfigureAwait(false);
            foreach (Kit kit in dirty)
            {
                L.Log("Saved kit " + kit.InternalName + " after dirty load.");
                kit.IsLoadDirty = false;
            }
        }
    }

    public static IQueryable<Kit> IncludeKitData(IQueryable<Kit> set)
    {
        return set
            .Include(x => x.ItemModels)
            .Include(x => x.UnlockRequirementsModels)
            .Include(x => x.Bundles)
            .Include(x => x.FactionFilter)
            .Include(x => x.MapFilter)
            .Include(x => x.Skillsets)
            .Include(x => x.Translations);
    }

    protected override IQueryable<Kit> OnInclude(IQueryable<Kit> set) => IncludeKitData(set);

    protected override Task PostReload(CancellationToken token)
    {
        return DownloadPlayersKitData(PlayerManager.OnlinePlayers, true, token);
    }

    protected override Task PreUnload(CancellationToken token)
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
        return SaveAllPlayerFavorites(token);
    }

    public Kit? GetKit(uint primaryKey) => GetEntityNoLock(x => x.PrimaryKey == primaryKey);
    public Kit? GetKitNoWriteLock(uint primaryKey) => GetEntityNoWriteLock(x => x.PrimaryKey == primaryKey);
    

    /// <remarks>Thread Safe</remarks>
    public async Task<List<Kit>> FindKits(string id, CancellationToken token = default, bool exactMatchOnly = true)
    {
        List<Kit> kits = new List<Kit>(4);
        await WriteWaitAsync(token).ConfigureAwait(false);
        try
        {
            F.StringSearch(Items, kits, x => x.InternalName, id, exactMatchOnly);
        }
        finally
        {
            WriteRelease();
        }

        return kits;
    }
    public List<Kit> FindKitsNoLock(string id, bool exactMatchOnly = true)
    {
        WriteWait();
        try
        {
            List<Kit> kits = new List<Kit>(4);
            F.StringSearch(Items, kits, x => x.InternalName, id, exactMatchOnly);
            return kits;
        }
        finally
        {
            WriteRelease();
        }
    }
}
#endif