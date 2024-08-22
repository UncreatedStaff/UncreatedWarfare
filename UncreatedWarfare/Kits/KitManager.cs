using DanielWillett.ModularRpcs.Reflection;
using DanielWillett.ModularRpcs.Routing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;
using Uncreated.Framework.UI;
using Uncreated.Warfare.Database;
using Uncreated.Warfare.Database.Abstractions;
using Uncreated.Warfare.Events;
using Uncreated.Warfare.Events.Models.Items;
using Uncreated.Warfare.Events.Models.Players;
using Uncreated.Warfare.Interaction.Commands;
using Uncreated.Warfare.Kits.Items;
using Uncreated.Warfare.Layouts.Teams;
using Uncreated.Warfare.Maps;
using Uncreated.Warfare.Models.Factions;
using Uncreated.Warfare.Models.Kits;
using Uncreated.Warfare.Players;
using Uncreated.Warfare.Players.ItemTracking;
using Uncreated.Warfare.Players.Management;
using Uncreated.Warfare.Players.Skillsets;
using Uncreated.Warfare.Services;
using Uncreated.Warfare.Squads;
using Uncreated.Warfare.Sync;
using Uncreated.Warfare.Teams;
using Uncreated.Warfare.Translations.Languages;
using Uncreated.Warfare.Util;
using Uncreated.Warfare.Util.Timing;

// ReSharper disable ConstantConditionalAccessQualifier

namespace Uncreated.Warfare.Kits;

public partial class KitManager :
    ISessionHostedService,
    IEventListener<QuestCompleted>,
    IEventListener<PlayerJoined>,
    IEventListener<PlayerLeft>,
    IEventListener<GroupChanged>,
    IEventListener<SwapClothingRequested>,
    IDisposable
{
    private readonly ILoopTicker _favoritesTicker;
    private readonly ILogger<KitManager> _logger;
    private readonly PlayerService _playerService;
    private readonly IServiceProvider _serviceProvider;
    private readonly LanguageService _languageService;

    public const string DefaultKitId = "default";

    public static readonly Guid[] BlacklistedWeaponsTextItems =
    {
        new Guid("3879d9014aca4a17b3ed749cf7a9283e"), // Laser Designator
        new Guid("010de9d7d1fd49d897dc41249a22d436")  // Laser Rangefinder
    };

    public KitMenuUI MenuUI { get; }
    public KitDataCache Cache { get; }
    public KitDistribution Distribution { get; }
    public KitRequests Requests { get; }
    public KitSigns Signs { get; }
    public KitLayouts Layouts { get; }
    public KitBoosting Boosting { get; }
    public KitLoadouts<WarfareDbContext> Loadouts { get; }
    public KitDefaults<WarfareDbContext> Defaults { get; }

    public event KitChanged? OnKitChanged;

    /// <summary>
    /// Doesn't include changes due to group change.
    /// </summary>
    public event KitChanged? OnManualKitChanged;
    public event KitAccessCallback? OnKitAccessChanged;
    public event Action? OnFavoritesRefreshed;

    public KitManager(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
        _logger = serviceProvider.GetRequiredService<ILogger<KitManager>>();
        _playerService = serviceProvider.GetRequiredService<PlayerService>();
        _languageService = serviceProvider.GetRequiredService<LanguageService>();

        MenuUI = serviceProvider.GetRequiredService<KitMenuUI>();

        IRpcRouter rpcRouter = serviceProvider.GetRequiredService<IRpcRouter>();

        Cache = new KitDataCache(this, serviceProvider);
        Distribution = new KitDistribution(this, serviceProvider);
        Requests = new KitRequests(this, serviceProvider);
        Signs = new KitSigns(this, serviceProvider);
        Layouts = new KitLayouts(this, serviceProvider);
        Boosting = new KitBoosting(this);
        Loadouts = ProxyGenerator.Instance.CreateProxy<KitLoadouts<WarfareDbContext>>(rpcRouter, [ this, serviceProvider ]);
        Defaults = new KitDefaults<WarfareDbContext>(this, serviceProvider);

        _favoritesTicker = serviceProvider.GetRequiredService<ILoopTickerFactory>().CreateTicker(TimeSpan.FromMinutes(1d), TimeSpan.FromMinutes(1d), false, SaveFavoritesTick);
    }

    internal static void ConfigureServices(IServiceCollection serviceCollection)
    {
        serviceCollection.AddScoped<KitManager>();
        serviceCollection.AddScoped(serviceProvider => serviceProvider.GetRequiredService<KitManager>().Cache);
        serviceCollection.AddScoped(serviceProvider => serviceProvider.GetRequiredService<KitManager>().Distribution);
        serviceCollection.AddScoped(serviceProvider => serviceProvider.GetRequiredService<KitManager>().Requests);
        serviceCollection.AddScoped(serviceProvider => serviceProvider.GetRequiredService<KitManager>().Signs);
        serviceCollection.AddScoped(serviceProvider => serviceProvider.GetRequiredService<KitManager>().Layouts);
        serviceCollection.AddScoped(serviceProvider => serviceProvider.GetRequiredService<KitManager>().Boosting);
        serviceCollection.AddScoped(serviceProvider => serviceProvider.GetRequiredService<KitManager>().Loadouts);
        serviceCollection.AddScoped(serviceProvider => serviceProvider.GetRequiredService<KitManager>().Defaults);
    }

    void IDisposable.Dispose()
    {
        _favoritesTicker.Dispose();
    }

    // pre-made include sets for EF queries
    public static IQueryable<Kit> Set(IKitsDbContext dbContext)
        => dbContext.Kits
            .Include(x => x.Translations);

    public static IQueryable<Kit> ItemsSet(IKitsDbContext dbContext)
        => dbContext.Kits
            .Include(x => x.ItemModels);

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

    async UniTask ISessionHostedService.StartAsync(CancellationToken token)
    {
        await CreateMissingDefaultKits(token).ConfigureAwait(false);

        await ValidateKits(token).ConfigureAwait(false);

        PlayerLife.OnPreDeath += OnPreDeath;

        // todo await CountTranslations(token).ConfigureAwait(false);

        await Cache.ReloadCache(token).ConfigureAwait(false);
    }

    UniTask ISessionHostedService.StopAsync(CancellationToken token)
    {
        PlayerLife.OnPreDeath -= OnPreDeath;
        Cache.Clear();
        return UniTask.CompletedTask;
    }

    public void InvokeOnKitAccessChanged(Kit kit, ulong player, bool newAccess, KitAccessType newType)
    {
        if (OnKitAccessChanged == null)
            return;

        UniTask.Create(async () =>
        {
            await UniTask.SwitchToMainThread();
            OnKitAccessChanged?.Invoke(kit, player, newAccess, newType);
        });
    }

    public void InvokeOnKitChanged(WarfarePlayer player, Kit? kit, Kit? oldKit)
    {
        if (OnKitChanged == null)
            return;

        UniTask.Create(async () =>
        {
            await UniTask.SwitchToMainThread();
            OnKitChanged?.Invoke(player, kit, oldKit);
        });
    }

    public void InvokeOnManualKitChanged(WarfarePlayer player, Kit? kit, Kit? oldKit)
    {
        if (OnManualKitChanged == null)
            return;

        UniTask.Create(async () =>
        {
            await UniTask.SwitchToMainThread();
            OnManualKitChanged?.Invoke(player, kit, oldKit);
        });
    }

    public async Task<Kit?> GetKit(uint pk, CancellationToken token = default, Func<IKitsDbContext, IQueryable<Kit>>? set = null)
    {
        await using IKitsDbContext dbContext = _serviceProvider.GetRequiredService<WarfareDbContext>();

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
        await using IKitsDbContext dbContext = _serviceProvider.GetRequiredService<WarfareDbContext>();

        return await FindKit(dbContext, id, token, exactMatchOnly, set);
    }

    /// <summary>
    /// Find a kit by internal name, or if <paramref name="exactMatchOnly"/> is <see langword="false"/>, by display name as well.
    /// </summary>
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
        int index = CollectionUtility.StringIndexOf(list, x => x.Value.InternalName, id, exactMatchOnly);
        if (index == -1)
            index = CollectionUtility.StringIndexOf(list, x => x.Value.GetDisplayName(_languageService, null, true), id, exactMatchOnly);

        kit = index == -1 ? null : list[index].Value;
        if (kit == null)
            return null;

        uint pk = kit.PrimaryKey;
        kit = await queryable.FirstOrDefaultAsync(x => x.PrimaryKey == pk, token).ConfigureAwait(false);
        if (kit != null)
            Cache.OnKitUpdated(kit, dbContext);

        return kit;
    }

    private void SaveFavoritesTick(ILoopTicker ticker, TimeSpan timeSinceStart, TimeSpan deltaTime)
    {
        Task.Run(async () =>
        {
            try
            {
                await SaveAllPlayerFavorites(CancellationToken.None);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to run save favorites ticker loop.");
            }
        });
    }

    public async Task SaveAllPlayerFavorites(CancellationToken token)
    {
        await UniTask.SwitchToMainThread(token);
        List<Task> tasks = new List<Task>(4);
        foreach (WarfarePlayer player in _playerService.OnlinePlayers)
        {
            if (UnturnedUIDataSource.GetData<KitMenuUIData>(player.Steam64, MenuUI.Parent) is not { FavoritesDirty: true, FavoriteKits: { } fk })
                continue;

            tasks.Add(SaveFavorites(player, fk, token));
        }

        await Task.WhenAll(tasks);
    }

    private async Task ValidateKits(CancellationToken token = default)
    {
        await using WarfareDbContext dbContext = _serviceProvider.GetRequiredService<WarfareDbContext>();
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

    // todo private async Task CountTranslations(CancellationToken token = default)
    // {
    //     await using IKitsDbContext dbContext = _serviceProvider.GetRequiredService<WarfareDbContext>();
    // 
    //     List<Kit> allKits = await dbContext.Kits
    //         .Include(x => x.Translations)
    //         .Include(x => x.FactionFilter)
    //         .Include(x => x.MapFilter)
    //         .Where(x => x.Type == KitType.Public || x.Type == KitType.Elite)
    //         .ToListAsync(token).ConfigureAwait(false);
    // 
    //     await UniTask.SwitchToMainThread(token);
    // 
    //     Localization.ClearSection(TranslationSection.Kits);
    //     int ct = 0;
    // 
    //     foreach (Kit kit in allKits)
    //     {
    //         if (kit is not { Type: KitType.Public or KitType.Elite, Requestable: true })
    //             continue;
    // 
    //         ++ct;
    //         foreach (KitTranslation language in kit.Translations)
    //         {
    //             if (Data.LanguageDataStore.GetInfoCached(language.LanguageId) is { } langInfo)
    //                 langInfo.IncrementSection(TranslationSection.Kits, 1);
    //         }
    //     }
    // 
    //     Localization.IncrementSection(TranslationSection.Kits, ct);
    // }

    /// <summary>
    /// This is ran on startup to make sure unarmed kits exist for all teams as well as the fallback default kit exists.
    /// </summary>
    private async Task CreateMissingDefaultKits(CancellationToken token = default)
    {
        ITeamManager<Team> teamManager = _serviceProvider.GetRequiredService<ITeamManager<Team>>();

        await using IFactionDbContext factionDbContext = _serviceProvider.GetRequiredService<WarfareDbContext>();
        factionDbContext.ChangeTracker.AutoDetectChangesEnabled = false;

        foreach (Team team in teamManager.AllTeams)
        {
            if (team.Faction.IsDefaultFaction)
                continue;

            uint? unarmedKit = team.Faction.UnarmedKit;
            Faction? factionModel;
            if (unarmedKit.HasValue)
            {
                Kit? kit = await GetKit(unarmedKit.Value, token);
                if (kit != null)
                    continue;

                factionModel = await factionDbContext.Factions.FirstOrDefaultAsync(x => x.Key == team.Faction.PrimaryKey, token).ConfigureAwait(false);

                kit = await FindKit(team.Faction.KitPrefix + "unarmed", token);
                if (kit != null)
                {
                    if (factionModel != null)
                    {
                        factionModel.UnarmedKit = kit;
                        factionDbContext.Update(factionModel);
                    }

                    team.Faction.UnarmedKit = kit.PrimaryKey;
                    _logger.LogWarning("Team {0}'s unarmed kit wasn't configured but a possible match was found with ID: {1}, using that one instead.", team.Faction.Name, kit.InternalName);
                    continue;
                }

                _logger.LogWarning("Team {0}'s unarmed kit \"{1}\" was not found, an attempt will be made to auto-generate one.", team.Faction.Name, unarmedKit);
            }
            else
            {
                _logger.LogWarning("Team {0}'s unarmed kit hasn't been configured, an attempt will be made to auto-generate one.", team.Faction.Name);
                factionModel = await factionDbContext.Factions.FirstOrDefaultAsync(x => x.Key == team.Faction.PrimaryKey, token).ConfigureAwait(false);
            }

            Kit newKit = await Defaults.CreateDefaultKit(team.Faction, team.Faction.KitPrefix + "unarmed", token).ConfigureAwait(false);
            team.Faction.UnarmedKit = newKit.PrimaryKey;
            if (factionModel != null)
            {
                factionModel.UnarmedKit = newKit;
                factionDbContext.Update(factionModel);
            }
            else
            {
                _logger.LogWarning("Unable to update unarmed kit for faction {0}.", team.Faction.Name);
            }
        }

        await factionDbContext.SaveChangesAsync(token).ConfigureAwait(false);

        bool needsDefaultKit = false;

        Kit? defaultKit = await FindKit(DefaultKitId, token);
        if (defaultKit == null)
        {
            _logger.LogWarning("The overall default kit \"{0}\" was not found, an attempt will be made to auto-generate one.", DefaultKitId);
            needsDefaultKit = true;
        }

        if (!needsDefaultKit)
            return;

        defaultKit = await Defaults.CreateDefaultKit(null, DefaultKitId, token);
        _logger.LogInformation("Created default kit: \"{0}\".", defaultKit.InternalName);
    }

    /// <summary>
    /// Gets the exact <see cref="IKitItem"/> and <see cref="IPageKitItem"/> that the player is holding from their kit (accounts for the item being moved or dropped and picked up).
    /// </summary>
    public async Task<IPageKitItem?> GetHeldItemFromKit(WarfarePlayer player, CancellationToken token = default)
    {
        await UniTask.SwitchToMainThread(token);
        ItemJar? held = ItemUtility.GetHeldItem(player, out Page page);
        return held != null ? await GetItemFromKit(player, held, page, token).ConfigureAwait(false) : null;
    }

    /// <summary>
    /// Gets the exact <see cref="IKitItem"/> and <see cref="IPageKitItem"/> that the given item refers to fromtheir kit (accounts for the item being moved or dropped and picked up).
    /// </summary>
    public Task<IPageKitItem?> GetItemFromKit(WarfarePlayer player, ItemJar jar, Page page, CancellationToken token = default)
    {
        return GetItemFromKit(player, jar.x, jar.y, jar.item, page, token);
    }

    /// <summary>
    /// Gets the exact <see cref="IKitItem"/> and <see cref="IPageKitItem"/> that the given item refers to from their kit (accounts for the item being moved or dropped and picked up).
    /// </summary>
    public async Task<IPageKitItem?> GetItemFromKit(WarfarePlayer player, byte x, byte y, Item item, Page page, CancellationToken token = default)
    {
        KitPlayerComponent kitComponent = player.Component<KitPlayerComponent>();
        Kit? cachedKit = kitComponent.CachedKit;
        if (cachedKit is { ItemModels.Count: > 0 })
        {
            await UniTask.SwitchToMainThread(token);
            if (cachedKit is { ItemModels.Count: > 0 })
                return GetItemFromKit(player, cachedKit, x, y, item, page);
        }

        Kit? kit = await kitComponent.GetActiveKitAsync(token, ItemsSet).ConfigureAwait(false);

        if (kit is null)
            return null;

        await UniTask.SwitchToMainThread(token);

        return GetItemFromKit(player, kit, x, y, item, page);
    }

    /// <summary>
    /// Gets the exact <see cref="IKitItem"/> and <see cref="IPageKitItem"/> that the given item refers to from their kit (accounts for the item being moved or dropped and picked up).
    /// </summary>
    public IPageKitItem? GetItemFromKit(WarfarePlayer player, Kit kit, ItemJar jar, Page page)
    {
        return GetItemFromKit(player, kit, jar.x, jar.y, jar.item, page);
    }

    /// <summary>
    /// Gets the exact <see cref="IKitItem"/> and <see cref="IPageKitItem"/> that the given item refers to from their kit (accounts for the item being moved or dropped and picked up).
    /// </summary>
    public IPageKitItem? GetItemFromKit(WarfarePlayer player, Kit kit, byte x, byte y, Item item, Page page)
    {
#if DEBUG
        _logger.LogDebug("Looking for kit item: {0} at {1}, ({2}, {3}).", item.GetAsset()?.itemName ?? "null", page, x, y);
#endif
        ItemTrackingPlayerComponent trackerComponent = player.Component<ItemTrackingPlayerComponent>();
        for (int i = 0; i < trackerComponent.ItemTransformations.Count; ++i)
        {
            ItemTransformation t = trackerComponent.ItemTransformations[i];
            if (t.Item != item)
                continue;

            if (t.NewX != x || t.NewY != y || t.NewPage != page)
                continue;

#if DEBUG
            _logger.LogDebug("Transforming: {0} <- {1}, ({2} <- {3}, {4} <- {5}).", t.OldPage, page, t.OldX, x, t.OldY, y);
#endif
            x = t.OldX;
            y = t.OldY;
            page = t.OldPage;
            break;
        }

        ItemAsset asset = item.GetAsset();
        if (asset == null)
            return null;

        FactionInfo faction = player.Team.Faction;
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

#if DEBUG
        _logger.LogDebug("Kit item at: {0}, ({1}, {2}) not found.", page, x, y);
#endif
        return null;
    }

    internal void OnTeamPlayerCountChanged(WarfarePlayer? allPlayer = null)
    {
        if (allPlayer != null)
        {
            Signs.UpdateSigns(allPlayer);
        }

        foreach (Kit item in Cache.KitDataByKey.Values)
        {
            if (item.TeamLimit < 1f)
                Signs.UpdateSigns(item);
        }
    }

    private void OnPreDeath(PlayerLife life)
    {
        WarfarePlayer? player = _playerService.GetOnlinePlayer(life.player);
        
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

                bool notInKit = !active.ContainsItem(asset.GUID, player?.Team ?? Team.NoTeam) && Whitelister.IsWhitelisted(asset.GUID, out _);
                if (notInKit || (percentage < 0.3f && asset.type != EItemType.GUN))
                {
                    if (notInKit)
                        ItemManager.dropItem(jar.item, life.player.transform.position, false, true, true);

                    life.player.inventory.removeItem(page, (byte)index);
                }
            }
        }
    }

    /// <remarks>Thread Safe</remarks>
    public Task TryGiveKitOnJoinTeam(WarfarePlayer player, CancellationToken token = default)
        => TryGiveKitOnJoinTeam(player, player.Team, token);

    /// <remarks>Thread Safe</remarks>
    public async Task TryGiveKitOnJoinTeam(WarfarePlayer player, Team team, CancellationToken token = default)
    {
        Kit? kit = await GetDefaultKit(team, token, x => RequestableSet(x, false)).ConfigureAwait(false);
        if (kit == null)
        {
            _logger.LogWarning("Unable to give {0} ({1}) a kit, no default kit available.", player.Names.CharacterName, player.Steam64);

            await UniTask.SwitchToMainThread(token);

            ItemUtility.ClearInventoryAndSlots(player);
            return;
        }

        _logger.LogDebug("Giving kit: {0} ({1}) to {2} ({3}) on joining team {4}.", kit.InternalName, kit.PrimaryKey, player.Names.CharacterName, player.Steam64, team.Faction.Name);
        await Requests.GiveKit(player, kit, false, false, token).ConfigureAwait(false);
    }

    public async Task<Kit?> TryGiveUnarmedKit(WarfarePlayer player, bool manual, CancellationToken token = default)
    {
        if (!player.IsOnline)
            return null;
        Kit? kit = await GetDefaultKit(player.Team, token, x => RequestableSet(x, false)).ConfigureAwait(false);
        if (kit == null || !player.IsOnline)
            return null;

        await Requests.GiveKit(player, kit, manual, true, token).ConfigureAwait(false);
        return kit;
    }

    public async Task<Kit?> TryGiveRiflemanKit(WarfarePlayer player, bool manual, bool tip, CancellationToken token = default)
    {
        await using IKitsDbContext dbContext = _serviceProvider.GetRequiredService<WarfareDbContext>();

        List<Kit> kits = await dbContext.Kits
            .Include(x => x.FactionFilter)
            .Include(x => x.MapFilter)
            .Include(x => x.UnlockRequirementsModels)
            .Where(x => x.Class == Class.Rifleman && !x.Disabled)
            .ToListAsync(token).ConfigureAwait(false);

        FactionInfo? t = player.Team.Faction;
        Kit? rifleman = null;

        foreach (Kit riflemanCandidate in kits.Where(k =>
                     !k.IsFactionAllowed(t) &&
                     !k.IsCurrentMapAllowed() &&
                     (k is { Type: KitType.Public, CreditCost: <= 0 } || HasAccessQuick(k, player)) &&
                     !k.IsLimited(_playerService, out _, out _, player.Team, false) &&
                     !k.IsClassLimited(_playerService, out _, out _, player.Team, false) &&
                     k.MeetsUnlockRequirementsFast(player)
                 ))
        {
            if (!await riflemanCandidate.MeetsUnlockRequirementsAsync(player, token))
            {
                continue;
            }

            rifleman = riflemanCandidate;
            break;
        }


        rifleman ??= await GetDefaultKit(player.Team, token).ConfigureAwait(false);
        if (rifleman != null)
        {
            uint id = rifleman.PrimaryKey;
            rifleman = await RequestableSet(dbContext, false)
                .FirstOrDefaultAsync(x => x.PrimaryKey == id, token).ConfigureAwait(false);
        }

        await Requests.GiveKit(player, rifleman, manual, tip, token).ConfigureAwait(false);
        return rifleman;
    }

    public async Task<Kit?> GetDefaultKit(Team team, CancellationToken token = default, Func<IKitsDbContext, IQueryable<Kit>>? set = null)
    {
        uint? unarmedKey = team.Faction.UnarmedKit;

        Kit? kit = team.IsValid && unarmedKey.HasValue ? await GetKit(unarmedKey.Value, token, set: set).ConfigureAwait(false) : null;

        if (kit is { Disabled: true })
            kit = null;

        kit ??= await FindKit(DefaultKitId, token, set: set).ConfigureAwait(false);

        return kit;
    }

    public async Task<Kit?> GetRecommendedSquadleaderKit(ulong team, CancellationToken token = default)
    {
        await using IKitsDbContext dbContext = _serviceProvider.GetRequiredService<WarfareDbContext>();

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
        GameThread.AssertCurrent();

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

    private void ValidateKit(Kit kit, IDbContext context)
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
                    _logger.LogWarning("Duplicate faction filter found in kit \"{0}\" {1}: {2}.", kit.InternalName, kit.PrimaryKey, comparer.Faction.Name);
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
                _logger.LogWarning("Faction was not in whitelist for kit \"{0}\" {1}: {2}.", kit.InternalName, kit.PrimaryKey, kit.Faction.Name);
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
                    _logger.LogWarning("Duplicate map filter found in kit \"{0}\" {1}: {2}.", kit.InternalName, kit.PrimaryKey, MapScheduler.GetMapName(comparer.Map));
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
                    _logger.LogWarning("Duplicate skillset found in kit \"{0}\" {1}: {2}.", kit.InternalName, kit.PrimaryKey, kit.Skillsets[j].Skillset);
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

                    _logger.LogWarning("Duplicate unlock requirement found in kit \"{0}\" {1}: {2}.", kit.InternalName, kit.PrimaryKey, kit.UnlockRequirementsModels[i].Json);
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

                _logger.LogWarning("Duplicate item found in kit \"{0}\" {1}:" + Environment.NewLine + "{2}.", kit.InternalName, kit.PrimaryKey, comparer.CreateRuntimeItem());
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
                        _logger.LogError("Conflicting item found in kit \"{0}\" {1}:" + Environment.NewLine + "{2} / {3}.", kit.InternalName, kit.PrimaryKey, comparer.CreateRuntimeItem(), item.CreateRuntimeItem());
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
                    _logger.LogError("Overlapping item found in kit \"{0}\" {1}:" + Environment.NewLine + "{2} / {3}.", kit.InternalName, kit.PrimaryKey, comparer.CreateRuntimeItem(), item.CreateRuntimeItem());
                }
            }
        }
    }

    private async Task SetupPlayer(WarfarePlayer player, CancellationToken token = default)
    {
        Kit? kit = await player.Component<KitPlayerComponent>().GetActiveKitAsync(token).ConfigureAwait(false);
        if (kit == null || !kit.Requestable || (kit.Type != KitType.Loadout && kit.IsLimited(out _, out _, player.Team)) || (kit.Type == KitType.Loadout && kit.IsClassLimited(out _, out _, player.Team)))
        {
            await TryGiveRiflemanKit(player, false, false, token).ConfigureAwait(false);
        }
        else if (UCWarfare.Config.ModifySkillLevels)
        {
            if (kit.Skillsets is { Count: > 0 })
                player.Component<SkillsetPlayerComponent>().EnsureSkillsets(kit.Skillsets.Select(x => x.Skillset));
            else
                player.Component<SkillsetPlayerComponent>().EnsureDefaultSkillsets();
        }
    }

    async Task IPlayerPostInitListenerAsync.OnPostPlayerInit(WarfarePlayer player, bool wasAlreadyOnline, CancellationToken token)
    {
        if (Data.Gamemode is not TeamGamemode { UseTeamSelector: true })
        {
            _ = RefreshFavorites(player, false, token);
            await SetupPlayer(player, token).ConfigureAwait(false);
            return;
        }

        //ItemUtility.ClearInventory(player);
        player.Component<SkillsetPlayerComponent>().EnsureDefaultSkillsets();
        _ = RefreshFavorites(player, false, token);
        _ = Boosting.IsNitroBoosting(player.Steam64.m_SteamID, token);
        _ = Requests.RemoveKit(player, false, player.DisconnectToken);
    }

    /// <remarks>Thread Safe</remarks>
    public async Task<bool> GiveAccess(string kitId, WarfarePlayer player, KitAccessType type, CancellationToken token = default)
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
    public async Task<bool> GiveAccess(Kit kit, WarfarePlayer player, KitAccessType type, CancellationToken token = default)
    {
        // todo make update type and timestamp
        if (!player.IsOnline)
        {
            return await GiveAccess(kit, player.Steam64.m_SteamID, type, token).ConfigureAwait(false);
        }

        await player.PurchaseSync.WaitAsync(token).ConfigureAwait(false);
        try
        {
            KitPlayerComponent comp = player.Component<KitPlayerComponent>();
            if (comp.AccessibleKits != null && comp.AccessibleKits.Contains(kit.PrimaryKey))
                return true;
            bool alreadyApplied = await AddAccessRow(kit.PrimaryKey, player.Steam64.m_SteamID, type, token).ConfigureAwait(false);
            if (!alreadyApplied)
                return false;

            (comp.AccessibleKits ??= new List<uint>(4)).Add(kit.PrimaryKey);
            InvokeOnKitAccessChanged(kit, player.Steam64.m_SteamID, true, type);

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
        WarfarePlayer? online = _playerService.GetOnlinePlayerOrNull(player);
        if (online != null && online.IsOnline)
            return await GiveAccess(kit, online, type, token).ConfigureAwait(false);

        if (!await AddAccessRow(kit.PrimaryKey, player, type, token).ConfigureAwait(false))
            return false;

        InvokeOnKitAccessChanged(kit, player, true, type);
        return true;
    }

    /// <remarks>Thread Safe</remarks>
    public async Task<bool> RemoveAccess(string kitId, WarfarePlayer player, CancellationToken token = default)
    {
        Kit? kit = await FindKit(kitId, token, set: x => x.Kits).ConfigureAwait(false);
        if (kit == null)
            return false;
        return await RemoveAccess(kit, player.Steam64.m_SteamID, token).ConfigureAwait(false);
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
    public async Task<bool> RemoveAccess(Kit kit, WarfarePlayer player, CancellationToken token = default)
    {
        if (!player.IsOnline)
        {
            return await RemoveAccess(kit, player.Steam64.m_SteamID, token).ConfigureAwait(false);
        }

        await player.PurchaseSync.WaitAsync(token).ConfigureAwait(false);
        bool access;
        try
        {
            access = await RemoveAccessRow(kit.PrimaryKey, player.Steam64.m_SteamID, token).ConfigureAwait(false);
            if (access)
            {
                KitPlayerComponent comp = player.Component<KitPlayerComponent>();
                comp.AccessibleKits?.Remove(kit.PrimaryKey);
                InvokeOnKitAccessChanged(kit, player.Steam64.m_SteamID, false, KitAccessType.Unknown);
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
        WarfarePlayer? online = _playerService.GetOnlinePlayerOrNull(player);
        if (online is { IsOnline: true })
            return await RemoveAccess(kit, online, token).ConfigureAwait(false);
        bool res = await RemoveAccessRow(kit.PrimaryKey, player, token).ConfigureAwait(false);
        InvokeOnKitAccessChanged(kit, player, false, KitAccessType.Unknown);
        return res;
    }

    /// <remarks>Thread Safe</remarks>
    public bool HasAccessQuick(Kit kit, WarfarePlayer player) => HasAccessQuick(kit.PrimaryKey, player);

    /// <remarks>Thread Safe</remarks>
    public bool HasAccessQuick(uint kit, WarfarePlayer player)
    {
        KitPlayerComponent comp = player.Component<KitPlayerComponent>();
        if (comp.AccessibleKits == null || kit == 0)
            return false;

        for (int i = 0; i < comp.AccessibleKits.Count; ++i)
        {
            if (comp.AccessibleKits[i] == kit)
                return true;
        }

        return false;
    }

    /// <remarks>Thread Safe</remarks>
    public ValueTask<bool> HasAccessQuick(Kit kit, ulong player, CancellationToken token = default)
        => HasAccessQuick(kit.PrimaryKey, player, token);

    /// <remarks>Thread Safe</remarks>
    public Task<bool> HasAccess(Kit kit, WarfarePlayer player, CancellationToken token = default)
        => HasAccess(kit.PrimaryKey, player.Steam64.m_SteamID, token);

    /// <remarks>Thread Safe</remarks>
    public Task<bool> HasAccess(Kit kit, ulong player, CancellationToken token = default)
        => HasAccess(kit.PrimaryKey, player, token);

    /// <remarks>Thread Safe</remarks>
    public Task<bool> HasAccess(uint kit, WarfarePlayer player, CancellationToken token = default)
        => HasAccess(kit, player.Steam64.m_SteamID, token);

    /// <remarks>Thread Safe</remarks>
    public Task<bool> HasAccess(IKitsDbContext dbContext, Kit kit, WarfarePlayer player, CancellationToken token = default)
        => HasAccess(dbContext, kit.PrimaryKey, player.Steam64.m_SteamID, token);

    /// <remarks>Thread Safe</remarks>
    public Task<bool> HasAccess(IKitsDbContext dbContext, Kit kit, ulong player, CancellationToken token = default)
        => HasAccess(dbContext, kit.PrimaryKey, player, token);

    /// <remarks>Thread Safe</remarks>
    public Task<bool> HasAccess(IKitsDbContext dbContext, uint kit, WarfarePlayer player, CancellationToken token = default)
        => HasAccess(dbContext, kit, player.Steam64.m_SteamID, token);

    /// <remarks>Thread Safe</remarks>
    public ValueTask<bool> HasAccessQuick(uint kit, ulong player, CancellationToken token = default)
    {
        if (kit == 0)
            return new ValueTask<bool>(false);
        WarfarePlayer? pl = _playerService.GetOnlinePlayerOrNull(player);
        if (pl != null && pl.IsOnline)
            return new ValueTask<bool>(HasAccessQuick(kit, pl));
        return new ValueTask<bool>(HasAccess(kit, player, token));
    }

    /// <summary>Use with purchase sync.</summary>
    public bool IsFavoritedQuick(uint kit, WarfarePlayer player)
    {
        GameThread.AssertCurrent();

        if (kit == 0)
            return false;

        if (UnturnedUIDataSource.GetData<KitMenuUIData>(player.Steam64, MenuUI.Parent) is not { FavoriteKits: { } favs })
            return false;

        for (int i = 0; i < favs.Count; ++i)
        {
            if (favs[i] == kit)
                return true;
        }

        return false;
    }

    public async Task RefreshFavorites(WarfarePlayer player, bool psLock, CancellationToken token = default)
    {
        CombinedTokenSources tokens = token.CombineTokensIfNeeded(player.DisconnectToken, UCWarfare.UnloadCancel);
        if (psLock)
            await player.PurchaseSync.WaitAsync(token);
        try
        {
            await using IKitsDbContext dbContext = _serviceProvider.GetRequiredService<WarfareDbContext>();
            ulong s64 = player.Steam64.m_SteamID;

            await UniTask.SwitchToMainThread(token);
            if (UnturnedUIDataSource.GetData<KitMenuUIData>(player.Steam64, MenuUI.Parent) is not { } data)
            {
                data = new KitMenuUIData(MenuUI, MenuUI.Parent, player, _serviceProvider);
                UnturnedUIDataSource.AddData(data);
            }

            data.FavoriteKits = await dbContext.KitFavorites
                .Where(x => x.Steam64 == s64)
                .Select(x => x.KitId)
                .ToListAsync(token);

            data.FavoritesDirty = false;
        }
        finally
        {
            if (psLock)
                player.PurchaseSync.Release();
        }

        UniTask.Create(async () =>
        {
            await UniTask.SwitchToMainThread(CancellationToken.None);

            tokens.Dispose();
            OnFavoritesRefreshed?.Invoke();
            MenuUI.OnFavoritesRefreshed(player);
            Signs.UpdateSigns(player);
        });
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
        if (newAccess)
            return;

        // dequip kit after losing access
        WarfarePlayer? pl = _playerService.GetOnlinePlayerOrNull(player);
        if (pl == null || kit == null)
            return;

        KitPlayerComponent comp = pl.Component<KitPlayerComponent>();

        uint? activeKit = comp.ActiveKitKey;
        if (!activeKit.HasValue || activeKit.Value != kit.PrimaryKey)
            return;

        Task.Run(async () =>
        {
            try
            {
                await Distribution.DequipKit(pl, true, kit);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error dequipping kit {0} for {1} after they lost access.", kit.InternalName, pl.Steam64.m_SteamID);
            }
        });
    }

    internal Task OnKitDeleted(Kit kit)
    {
        KitSync.OnKitDeleted(kit.PrimaryKey);

        Task.Run(async () =>
        {
            try
            {
                await Distribution.DequipKit(kit, true);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error dequipping kit {0} for all players after it was deleted.", kit.InternalName);
            }
        });
        return Task.CompletedTask;
    }

    internal Task OnKitUpdated(Kit kit)
    {
        KitSync.OnKitUpdated(kit);

        return Task.CompletedTask;
    }

    /// <summary>
    /// This function should be invoked when a kit's class, branch, or any other major properties change.
    /// </summary>
    internal void InvokeAfterMajorKitUpdate(Kit kit, bool manual)
    {
        GameThread.AssertCurrent();

        if (kit is null)
            return;

        BitArray mask = new BitArray(_playerService.OnlinePlayers.Count);
        for (int i = 0; i < _playerService.OnlinePlayers.Count; ++i)
        {
            WarfarePlayer player = _playerService.OnlinePlayers[i];
            KitPlayerComponent component = player.Component<KitPlayerComponent>();
            uint? activeKit = component.ActiveKitKey;

            if (!activeKit.HasValue || activeKit.Value != kit.PrimaryKey)
                continue;

            component.UpdateKit(kit);
            mask[i] = true;
        }

        if (OnKitChanged != null)
        {
            // waits a frame in case something tries to lock the kit and to ensure we are on main thread.
            UniTask.Create(async () =>
            {
                await UniTask.NextFrame();

                if (OnKitChanged == null)
                    return;

                for (int i = 0; i < _playerService.OnlinePlayers.Count; ++i)
                {
                    if (mask[i])
                        OnKitChanged.Invoke(_playerService.OnlinePlayers[i], kit, kit);
                }
            });
        }

        if (!manual || OnManualKitChanged == null)
            return;

        UniTask.Create(async () =>
        {
            await UniTask.NextFrame();

            if (OnManualKitChanged == null)
                return;

            for (int i = 0; i < _playerService.OnlinePlayers.Count; ++i)
            {
                if (mask[i])
                    OnManualKitChanged.Invoke(_playerService.OnlinePlayers[i], kit, kit);
            }
        });
    }

    void IEventListener<PlayerJoined>.HandleEvent(PlayerJoined e, IServiceProvider serviceProvider)
    {
        OnTeamPlayerCountChanged();
        PlayerEquipment equipment = e.Player.UnturnedPlayer.equipment;
        for (int i = 0; i < 8; ++i)
            equipment.ServerClearItemHotkey((byte)i);
    }

    [EventListener(Priority = int.MaxValue)]
    void IEventListener<PlayerLeft>.HandleEvent(PlayerLeft e, IServiceProvider serviceProvider)
    {
        OnTeamPlayerCountChanged();
        if (UnturnedUIDataSource.GetData<KitMenuUIData>(e.Steam64, MenuUI.Parent) is not { FavoritesDirty: true, FavoriteKits: { } fk })
        {
            return;
        }

        Task.Run(async () =>
        {
            try
            {
                await SaveFavorites(e.Player, fk, CancellationToken.None);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to run save favorites ticker loop.");
            }
        });
    }

    void IEventListener<GroupChanged>.HandleEvent(GroupChanged e, IServiceProvider serviceProvider)
    {
        OnTeamPlayerCountChanged(e.Player);
    }

    void IEventListener<QuestCompleted>.HandleEvent(QuestCompleted e, IServiceProvider serviceProvider)
    {
        Signs.UpdateSigns(e.Player);
    }

    void IEventListener<SwapClothingRequested>.HandleEvent(SwapClothingRequested e, IServiceProvider serviceProvider)
    {
        // prevent removing clothing that has storage to make item tracking much easier
        if (e.Player.OnDuty())
            return;

        if (e.CurrentClothing == null || !e.Player.Component<KitPlayerComponent>().HasKit || e.Type is not ClothingType.Backpack and not ClothingType.Pants and not ClothingType.Shirt and not ClothingType.Vest)
        {
            return;
        }

        e.Player.SendChat(T.NoRemovingClothing);
        e.Cancel();
    }
}