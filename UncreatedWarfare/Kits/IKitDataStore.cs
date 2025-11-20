using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using MySqlConnector;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
using System.Linq.Expressions;
using System.Runtime.CompilerServices;
using Uncreated.Warfare.Database.Abstractions;
using Uncreated.Warfare.Events;
using Uncreated.Warfare.Events.Logging;
using Uncreated.Warfare.Events.Models;
using Uncreated.Warfare.Events.Models.Players;
using Uncreated.Warfare.Kits.Loadouts;
using Uncreated.Warfare.Logging;
using Uncreated.Warfare.Models.Factions;
using Uncreated.Warfare.Models.Kits;
using Uncreated.Warfare.Models.Kits.Bundles;
using Uncreated.Warfare.Models.Localization;
using Uncreated.Warfare.Players;
using Uncreated.Warfare.Players.Management;
using Uncreated.Warfare.Services;
using Uncreated.Warfare.Teams;
using Uncreated.Warfare.Translations.Languages;

namespace Uncreated.Warfare.Kits;

public interface IKitDataStore
{
    /// <summary>
    /// Dictionary of cached kits by their internal ID.
    /// </summary>
    IReadOnlyDictionary<string, Kit> CachedKitsById { get; }

    /// <summary>
    /// Dictionary of cached kits by their primary key.
    /// </summary>
    IReadOnlyDictionary<uint, Kit> CachedKitsByKey { get; }

    /// <summary>
    /// If the cache is being kept up to date.
    /// </summary>
    bool CacheEnabled { get; }

    /// <summary>
    /// Invoked when a kit's properties are updated.
    /// </summary>
    event Action<Kit>? KitUpdated;

    /// <summary>
    /// Invoked when a kit is fully deleted.
    /// </summary>
    event Action<KitModel>? KitRemoved;

    /// <summary>
    /// Invoked when a new kit is added.
    /// </summary>
    event Action<Kit>? KitAdded;

    /// <summary>
    /// Create a new kit with the given ID and basic information.
    /// </summary>
    /// <param name="kitId">Unique internal kit ID.</param>
    /// <param name="class">Class of the newly created kit.</param>
    /// <param name="displayName">Optional sign text for the default language.</param>
    /// <param name="creator">Player responsible for creating the kit, or default if not applicable.</param>
    /// <param name="createAction">Invoked before the kit is saved to modify the starting options.</param>
    /// <returns>The new kit.</returns>
    /// <exception cref="ArgumentException"><paramref name="kitId"/> not unique or <paramref name="createAction"/> threw an exception.</exception>
    Task<Kit> AddKitAsync(string kitId /* do not rename kitId */, Class @class, string? displayName, CSteamID creator, Action<KitModel> createAction, CancellationToken token = default);

    /// <summary>
    /// Create a new kit with the given ID and basic information.
    /// </summary>
    /// <param name="kitId">Unique internal kit ID.</param>
    /// <param name="class">Class of the newly created kit.</param>
    /// <param name="displayName">Optional sign text for the default language.</param>
    /// <param name="creator">Player responsible for creating the kit, or default if none.</param>
    /// <param name="createAction">Invoked before the kit is saved to modify the starting options.</param>
    /// <returns>The new kit.</returns>
    /// <exception cref="ArgumentException"><paramref name="kitId"/> not unique or <paramref name="createAction"/> threw an exception.</exception>
    Task<Kit> AddKitAsync(string kitId /* do not rename kitId */, Class @class, string? displayName, CSteamID creator, Func<KitModel, Task> createAction, CancellationToken token = default);

    /// <summary>
    /// Delete the kit with the given primary key.
    /// </summary>
    /// <param name="primaryKey">The kit to delete.</param>
    /// <param name="include">What information to include in the returned object.</param>
    /// <returns>The kit that was deleted, or <see langword="null"/> if it wasn't found.</returns>
    Task<KitModel?> DeleteKitAsync(uint primaryKey, KitInclude include = KitInclude.Base, CancellationToken token = default);

    /// <summary>
    /// Modify the kit with the given <paramref name="primaryKey"/>.
    /// </summary>
    /// <param name="primaryKey">The kit to modify.</param>
    /// <param name="include">What information to include.</param>
    /// <param name="updateAction">Callback to apply the modifications.</param>
    /// <param name="updater">Player who is updating the kit, or default if not applicable.</param>
    /// <returns>The updated kit, or <see langword="null"/> if the kit wasn't found.</returns>
    Task<Kit?> UpdateKitAsync(uint primaryKey, KitInclude include, Action<KitModel> updateAction, CSteamID updater, CancellationToken token = default);

    /// <summary>
    /// Modify the kit with the given <paramref name="primaryKey"/>.
    /// </summary>
    /// <param name="primaryKey">The kit to modify.</param>
    /// <param name="include">What information to include.</param>
    /// <param name="updateAction">Callback to apply the modifications.</param>
    /// <returns>The updated kit, or <see langword="null"/> if the kit wasn't found.</returns>
    Task<Kit?> UpdateKitAsync(uint primaryKey, KitInclude include, Action<KitModel> updateAction, CancellationToken token = default)
    {
        return UpdateKitAsync(primaryKey, include, updateAction, CSteamID.Nil, token);
    }


    /// <summary>
    /// Modify the kit with the given <paramref name="primaryKey"/>.
    /// </summary>
    /// <param name="primaryKey">The kit to modify.</param>
    /// <param name="include">What information to include.</param>
    /// <param name="updateAction">Callback to apply the modifications.</param>
    /// <param name="updater">Player who is updating the kit, or default if not applicable.</param>
    /// <returns>The updated kit, or <see langword="null"/> if the kit wasn't found.</returns>
    Task<Kit?> UpdateKitAsync(uint primaryKey, KitInclude include, Func<KitModel, Task> updateAction, CSteamID updater, CancellationToken token = default);

    /// <summary>
    /// Modify the kit with the given <paramref name="primaryKey"/>.
    /// </summary>
    /// <param name="primaryKey">The kit to modify.</param>
    /// <param name="include">What information to include.</param>
    /// <param name="updateAction">Callback to apply the modifications.</param>
    /// <returns>The updated kit, or <see langword="null"/> if the kit wasn't found.</returns>
    Task<Kit?> UpdateKitAsync(uint primaryKey, KitInclude include, Func<KitModel, Task> updateAction, CancellationToken token = default)
    {
        return UpdateKitAsync(primaryKey, include, updateAction, CSteamID.Nil, token);
    }

    /// <summary>
    /// Query an elite bundle by it's primary key.
    /// </summary>
    /// <param name="primaryKey">The unique primary key of an elite bundle.</param>
    /// <param name="includeKits">If the list of kits should be included.</param>
    /// <param name="kitListInclude">Data to be included in the list of kits, if <paramref name="includeKits"/> is <see langword="true"/>.</param>
    /// <param name="includeFaction">If the <see cref="Faction"/> should also be included.</param>
    /// <returns>The found bundle, or <see langword="null"/> if no bundles are found.</returns>
    Task<EliteBundle?> QueryEliteBundleAsync(uint primaryKey, bool includeKits = false, bool includeFaction = false, KitInclude kitListInclude = KitInclude.Default, CancellationToken token = default);

    /// <summary>
    /// Query an elite bundle by it's ID.
    /// </summary>
    /// <param name="bundleId">The unique ID of an elite bundle.</param>
    /// <param name="includeKits">If the list of kits should be included.</param>
    /// <param name="kitListInclude">Data to be included in the list of kits, if <paramref name="includeKits"/> is <see langword="true"/>.</param>
    /// <param name="includeFaction">If the <see cref="Faction"/> should also be included.</param>
    /// <returns>The found bundle, or <see langword="null"/> if no bundles are found.</returns>
    Task<EliteBundle?> QueryEliteBundleAsync(string bundleId, bool includeKits = false, bool includeFaction = false, KitInclude kitListInclude = KitInclude.Default, CancellationToken token = default);

    /// <summary>
    /// Query an elite bundle by it's ID.
    /// </summary>
    /// <param name="query">Action against the <see cref="EliteBundle"/> <see cref="DbSet{TEntity}"/>.</param>
    /// <param name="includeKits">If the list of kits should be included.</param>
    /// <param name="kitListInclude">Data to be included in the list of kits, if <paramref name="includeKits"/> is <see langword="true"/>.</param>
    /// <param name="includeFaction">If the <see cref="Faction"/> should also be included.</param>
    /// <returns>The found bundle, or <see langword="null"/> if no bundles are found.</returns>
    Task<EliteBundle?> QueryEliteBundleAsync(Func<IQueryable<EliteBundle>, IQueryable<EliteBundle>> query, bool includeKits = false, bool includeFaction = false, KitInclude kitListInclude = KitInclude.Default, CancellationToken token = default);

    /// <summary>
    /// Query an elite bundle by it's ID.
    /// </summary>
    /// <param name="query">Action against the <see cref="EliteBundle"/> <see cref="DbSet{TEntity}"/>.</param>
    /// <param name="includeKits">If the list of kits should be included.</param>
    /// <param name="kitListInclude">Data to be included in the list of kits, if <paramref name="includeKits"/> is <see langword="true"/>.</param>
    /// <param name="includeFaction">If the <see cref="Faction"/> should also be included.</param>
    /// <returns>The found bundle, or <see langword="null"/> if no bundles are found.</returns>
    Task<IList<EliteBundle>> QueryEliteBundlesAsync(Func<IQueryable<EliteBundle>, IQueryable<EliteBundle>> query, bool includeKits = false, bool includeFaction = false, KitInclude kitListInclude = KitInclude.Default, CancellationToken token = default);

    /// <summary>
    /// Query data from a kit other than the kit itself (ex. querying an ID using .Select(x => x.Id)).
    /// </summary>
    /// <typeparam name="T">The type of data to be returned.</typeparam>
    /// <param name="include">What information to include, only applicable when returning <see cref="KitModel"/>.</param>
    /// <param name="query">Action against the <see cref="KitModel"/> <see cref="DbSet{TEntity}"/>.</param>
    /// <returns>The first match to the query.</returns>
    Task<T?> QueryFirstAsync<T>(Func<IQueryable<KitModel>, IQueryable<T>> query, KitInclude include = KitInclude.Base, CancellationToken token = default);

    /// <summary>
    /// Query data from a kit other than the kit itself (ex. querying all IDs using .Select(x => x.Id)).
    /// </summary>
    /// <typeparam name="T">The type of data to be returned.</typeparam>
    /// <param name="include">What information to include, only applicable when returning <see cref="KitModel"/>.</param>
    /// <param name="query">Action against the <see cref="KitModel"/> <see cref="DbSet{TEntity}"/>.</param>
    /// <returns>All matches to the query.</returns>
    Task<List<T>> QueryListAsync<T>(Func<IQueryable<KitModel>, IQueryable<T>> query, KitInclude include = KitInclude.Base, CancellationToken token = default);
        
    /// <summary>
    /// Find a kit by it's <paramref name="primaryKey"/>.
    /// </summary>
    /// <param name="include">What type of data to be included in the return value.</param>
    /// <returns>The found kit, or <see langword="null"/> if no results are found.</returns>
    Task<Kit?> QueryKitAsync(uint primaryKey, KitInclude include, CancellationToken token = default);

    /// <summary>
    /// Find a kit by it's <paramref name="kitId"/>.
    /// </summary>
    /// <param name="include">What type of data to be included in the return value.</param>
    /// <returns>The found kit, or <see langword="null"/> if no results are found.</returns>
    Task<Kit?> QueryKitAsync(string kitId, KitInclude include, CancellationToken token = default);

    /// <summary>
    /// Find a kit by a custom where filter.
    /// </summary>
    /// <param name="include">What type of data to be included in the return value.</param>
    /// <param name="predicate">Filter applied using the where filter.</param>
    /// <returns>The found kit, or <see langword="null"/> if no results are found.</returns>
    Task<Kit?> QueryKitAsync(KitInclude include, Expression<Func<KitModel, bool>> predicate, CancellationToken token = default);

    /// <summary>
    /// Find a kit by a custom where filter.
    /// </summary>
    /// <param name="include">What type of data to be included in the return value.</param>
    /// <param name="query">Action against the <see cref="KitModel"/> <see cref="DbSet{TEntity}"/>.</param>
    /// <returns>The found kit, or <see langword="null"/> if no results are found.</returns>
    Task<Kit?> QueryKitAsync(KitInclude include, Func<IQueryable<KitModel>, IQueryable<KitModel>> query, CancellationToken token = default);

    /// <summary>
    /// Find kits by a custom where filter.
    /// </summary>
    /// <param name="output">List to output matching kits to. It will not be cleared, only appended to.</param>
    /// <param name="include">What type of data to be included in the return value.</param>
    /// <param name="predicate">Filter applied using the where filter.</param>
    /// <returns>The number of matching kits.</returns>
    Task<int> QueryKitsAsync(KitInclude include, IList<Kit> output, Expression<Func<KitModel, bool>> predicate, CancellationToken token = default);
    
    /// <summary>
    /// Find kits by a custom queryable filter.
    /// </summary>
    /// <param name="output">List to output matching kits to. It will not be cleared, only appended to.</param>
    /// <param name="include">What type of data to be included in the return value.</param>
    /// <param name="query">Action against the <see cref="KitModel"/> <see cref="DbSet{TEntity}"/>.</param>
    /// <returns>The number of matching kits.</returns>
    Task<int> QueryKitsAsync(KitInclude include, IList<Kit> output, Func<IQueryable<KitModel>, IQueryable<KitModel>> query, CancellationToken token = default);

    /// <summary>
    /// Find kits by a custom where filter.
    /// </summary>
    /// <param name="include">What type of data to be included in the return value.</param>
    /// <param name="predicate">Filter applied using the where filter.</param>
    /// <returns>An array of all matching kits.</returns>
    Task<Kit[]> QueryKitsAsync(KitInclude include, Expression<Func<KitModel, bool>> predicate, CancellationToken token = default);

    /// <summary>
    /// Find kits by a custom queryable filter.
    /// </summary>
    /// <param name="include">What type of data to be included in the return value.</param>
    /// <param name="query">Action against the <see cref="KitModel"/> <see cref="DbSet{TEntity}"/>.</param>
    /// <returns>An array of all matching kits.</returns>
    Task<Kit[]> QueryKitsAsync(KitInclude include, Func<IQueryable<KitModel>, IQueryable<KitModel>> query, CancellationToken token = default);
}

public class MySqlKitsDataStore : IKitDataStore, IEventListener<PlayerLeft>, IAsyncEventListener<PlayerPending>, IHostedService
{
    private readonly IKitsDbContext _dbContext;
    private readonly IFactionDataStore _factionDataStore;
    private readonly ICachableLanguageDataStore _languageDataStore;
    private readonly LanguageService? _languageService;
    private readonly ILogger<MySqlKitsDataStore> _logger;
    private readonly IPlayerService? _playerService;
    private readonly KitSignService? _kitSigns;
    private readonly ActionLoggerService? _actionLog;
    private bool _isInUpdate;
    private bool _isInAdd;

    private readonly ConcurrentDictionary<string, Kit>? _idCache;
    private readonly ConcurrentDictionary<uint, Kit>? _keyCache;

    private readonly SemaphoreSlim _semaphore = new SemaphoreSlim(1, 1);

    // used in GetOrCreateKit
    private readonly Func<uint, KitModel, Kit> _getOrCreateUpdateFuncId;
    private readonly Func<uint, Kit, KitModel, Kit> _getOrCreateAddFuncId;

    public IReadOnlyDictionary<string, Kit> CachedKitsById { get; }

    public IReadOnlyDictionary<uint, Kit> CachedKitsByKey { get; }

    public bool CacheEnabled { get; }

    /// <inheritdoc />
    public event Action<Kit>? KitUpdated;

    /// <inheritdoc />
    public event Action<KitModel>? KitRemoved;

    /// <inheritdoc />
    public event Action<Kit>? KitAdded;

    public MySqlKitsDataStore(IServiceProvider serviceProvider, ILogger<MySqlKitsDataStore> logger)
    {
        _dbContext = serviceProvider.GetRequiredService<IKitsDbContext>();
        _factionDataStore = serviceProvider.GetRequiredService<IFactionDataStore>();
        _languageDataStore = serviceProvider.GetRequiredService<ICachableLanguageDataStore>();

        _playerService = serviceProvider.GetService<IPlayerService>();

        _languageService = serviceProvider.GetService<LanguageService>();

        if (WarfareModule.IsActive)
        {
            _kitSigns = serviceProvider.GetService<KitSignService>();
            _actionLog = serviceProvider.GetService<ActionLoggerService>();
            CacheEnabled = true;
        }

        _logger = logger;
        _dbContext.ChangeTracker.AutoDetectChangesEnabled = false;

        if (CacheEnabled)
        {
            _keyCache = new ConcurrentDictionary<uint, Kit>();
            _idCache = new ConcurrentDictionary<string, Kit>();
            CachedKitsById = new ReadOnlyDictionary<string, Kit>(_idCache);
            CachedKitsByKey = new ReadOnlyDictionary<uint, Kit>(_keyCache);
        }
        else
        {
            CachedKitsById = new Dictionary<string, Kit>(0);
            CachedKitsByKey = new Dictionary<uint, Kit>(0);
        }

        _getOrCreateUpdateFuncId = (_, model) => new Kit(model, _factionDataStore, _languageDataStore);
        _getOrCreateAddFuncId = (_, value, model) =>
        {
            value.UpdateFromModel(model, _factionDataStore, _languageDataStore);
            return value;
        };
    }

    [EventListener(Priority = int.MinValue, RequiresMainThread = false)]
    void IEventListener<PlayerLeft>.HandleEvent(PlayerLeft e, IServiceProvider serviceProvider)
    {
        if (!CacheEnabled)
            return;

        foreach (Kit kit in _keyCache!.Values)
        {
            if (kit.Type != KitType.Loadout || LoadoutIdHelper.Parse(kit.Id, out CSteamID player) < 0)
                continue;

            if (player.m_SteamID != e.Steam64.m_SteamID && _playerService!.IsPlayerOnlineThreadSafe(player))
                continue;

            ulong s64 = player.m_SteamID;
            if (Provider.pending.Exists(x => x.playerID.steamID.m_SteamID == s64))
                continue;

            // player is leaving or (offline and not pending)
            _keyCache!.TryRemove(kit.Key, out _);
            _idCache!.TryRemove(kit.Id, out _);
        }
    }

    [EventListener(RequiresMainThread = false)]
    async UniTask IAsyncEventListener<PlayerPending>.HandleEventAsync(PlayerPending e, IServiceProvider serviceProvider, CancellationToken token)
    {
        if (!CacheEnabled)
            return;

        // cache loadouts for joining player
        await _semaphore.WaitAsync(token);
        try
        {
            string likeExpr = e.Steam64.m_SteamID.ToString(CultureInfo.InvariantCulture) + "_%";
            await foreach (KitModel model in ApplyIncludes(KitInclude.Default, _dbContext.Kits, false)
                               .Where(x => x.Type == KitType.Loadout && EF.Functions.Like(x.Id, likeExpr))
                               .AsAsyncEnumerable()
                               .WithCancellation(token))
            {
                Kit kit = new Kit(model, _factionDataStore, _languageDataStore);
                _keyCache![kit.Key] = kit;
                _idCache![kit.Id] = kit;
            }
        }
        finally
        {
            _semaphore.Release();
        }

        _logger.LogConditional($"Cached pending player's loadouts: {e.Steam64.m_SteamID}.");
    }

    /// <inheritdoc />
    public async Task<Kit> AddKitAsync(string kitId, Class @class, string? displayName, CSteamID creator, Action<KitModel> createAction, CancellationToken token = default)
    {
        if (_isInAdd)
            throw new InvalidOperationException("Nested invocation to AddKitAsync.");

        KitModel model = await CreateNewKitModel(kitId, @class, displayName, creator, token).ConfigureAwait(false);
        Kit kit;

        await _semaphore.WaitAsync(token).ConfigureAwait(false);
        try
        {
            _isInAdd = true;
            
            try
            {
                createAction(model);
            }
            catch (Exception ex)
            {
                _dbContext.Entry(model).State = EntityState.Detached;
                throw new ArgumentException("Exception thrown from create callback.", nameof(createAction), ex);
            }

            _dbContext.Kits.Add(model);
            _dbContext.ChangeTracker.DetectChanges();

            try
            {
                await _dbContext.SaveChangesAsync(token).ConfigureAwait(false);
            }
            catch (DbUpdateException ex) when (ex.GetBaseException() is MySqlException { ErrorCode: MySqlErrorCode.DuplicateUnique or MySqlErrorCode.NonUnique or MySqlErrorCode.DuplicateKeyEntry })
            {
                throw new ArgumentException($"Duplicate kit id: \"{kitId}\".", nameof(kitId), ex);
            }

            kit = GetOrCreateKit(model, KitInclude.All);

            if (WarfareModule.IsActive)
            {
                ApplyCreateKitModuleActive(kit, model.Creator);
            }

            try
            {
                KitAdded?.Invoke(kit);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error invoking KitAdded for kit {kit.Id}.");
            }
        }
        finally
        {
            _isInAdd = false;
            _dbContext.ChangeTracker.Clear();
            _semaphore.Release();
        }

        return kit;
    }

    /// <inheritdoc />
    public async Task<Kit> AddKitAsync(string kitId, Class @class, string? displayName, CSteamID creator, Func<KitModel, Task> createAction, CancellationToken token = default)
    {
        if (_isInAdd)
            throw new InvalidOperationException("Nested invocation to AddKitAsync.");

        KitModel model = await CreateNewKitModel(kitId, @class, displayName, creator, token).ConfigureAwait(false);
        Kit kit;

        await _semaphore.WaitAsync(token).ConfigureAwait(false);
        try
        {
            _isInAdd = true;
            
            try
            {
                await createAction(model).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                throw new ArgumentException("Exception thrown from create callback.", nameof(createAction), ex);
            }

            _dbContext.Kits.Add(model);
            _dbContext.ChangeTracker.DetectChanges();

            try
            {
                await _dbContext.SaveChangesAsync(token).ConfigureAwait(false);
            }
            catch (DbUpdateException ex) when (ex.GetBaseException() is MySqlException { ErrorCode: MySqlErrorCode.DuplicateUnique or MySqlErrorCode.NonUnique })
            {
                throw new ArgumentException($"Duplicate kit id: \"{kitId}\".");
            }

            kit = GetOrCreateKit(model, KitInclude.All);
            if (WarfareModule.IsActive)
            {
                ApplyCreateKitModuleActive(kit, model.Creator);
            }

            try
            {
                KitAdded?.Invoke(kit);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error invoking KitAdded for kit {kit.Id}.");
            }
        }
        finally
        {
            _isInAdd = false;
            _dbContext.ChangeTracker.Clear();
            _semaphore.Release();
        }

        return kit;
    }

    private ValueTask<KitModel> CreateNewKitModel(string kitId, Class @class, string? displayName, CSteamID creator, CancellationToken token)
    {
        KitModel model = new KitModel
        {
            Id = kitId,
            Class = @class,
            Type = KitType.Special,
            Branch = KitDefaults.GetDefaultBranch(@class),
            CreatedAt = DateTimeOffset.UtcNow,
            Creator = creator.GetEAccountType() == EAccountType.k_EAccountTypeIndividual ? creator.m_SteamID : 0,
            Disabled = false,
            Season = WarfareModule.Season,
            RequestCooldown = KitDefaults.GetDefaultRequestCooldown(@class),
            MinRequiredSquadMembers = KitDefaults.GetDefaultMinRequiredSquadMembers(@class),
            RequiresSquad = KitDefaults.GetDefaultRequiresSquad(@class),
            SquadLevel = KitDefaults.GetDefaultSquadLevel(@class),
            Translations = new List<KitTranslation>(displayName != null ? 1 : 0),
            Access             = [ ],
            Bundles            = [ ],
            Delays             = [ ],
            FactionFilter      = [ ],
            Items              = [ ],
            MapFilter          = [ ],
            Skillsets          = [ ],
            UnlockRequirements = [ ]
        };

        if (displayName != null)
        {
            LanguageInfo? lang = _languageService?.GetDefaultLanguage();
            if (lang is null && _languageDataStore != null)
            {
                // for usage on non-warfare platforms
                return LanguageFallback(model, _languageDataStore, displayName, token);
            }

            if (lang is { Key: not 0 })
            {
                model.Translations.Add(new KitTranslation { Value = displayName, LanguageId = lang.Key });
            }
        }

        return new ValueTask<KitModel>(model);
        
        static ValueTask<KitModel> LanguageFallback(KitModel model, ILanguageDataStore dataStore, string displayName, CancellationToken token)
        {
            Task<LanguageInfo?> infoTask = dataStore.GetInfo(1, true, token);
            if (!infoTask.IsCompleted)
            {
                return new ValueTask<KitModel>(CoreAsync(infoTask, model, displayName));
            }

            LanguageInfo? lang = infoTask.Result;
            if (lang is { Key: not 0 })
            {
                model.Translations.Add(new KitTranslation { Value = displayName, LanguageId = lang.Key });
            }

            return new ValueTask<KitModel>(model);

            static async Task<KitModel> CoreAsync(Task<LanguageInfo?> infoTask, KitModel model, string displayName)
            {
                LanguageInfo? lang = await infoTask;
                if (lang is { Key: not 0 })
                {
                    model.Translations.Add(new KitTranslation { Value = displayName, LanguageId = lang.Key });
                }

                return model;
            }
        }
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private void ApplyCreateKitModuleActive(Kit kit, ulong creatingPlayer)
    {
        _actionLog?.AddAction(new ActionLogEntry(ActionLogTypes.CreatedKit, kit.Id + " " + kit.Key.ToString(CultureInfo.InvariantCulture), creatingPlayer));

        if (kit.Type == KitType.Loadout && LoadoutIdHelper.Parse(kit.Id, out CSteamID player) >= 0)
        {
            WarfarePlayer? onlinePlayer = _playerService?.GetOnlinePlayerOrNullThreadSafe(player);
            if (onlinePlayer != null)
            {
                KitPlayerComponent comp = onlinePlayer.Component<KitPlayerComponent>();
                comp.UpdateLoadout(kit);
            }
        }

        UpdateSigns(kit.Id, null);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private void UpdateSigns(string kitId, WarfarePlayer? player)
    {
        if (player != null)
            _kitSigns?.UpdateSigns(kitId, player);
        else
            _kitSigns?.UpdateSigns(kitId);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private void UpdateLoadouts(WarfarePlayer? player)
    {
        if (player != null)
            _kitSigns?.UpdateLoadoutSigns(player);
        else
            _kitSigns?.UpdateLoadoutSigns();
    }

    public async Task<KitModel?> DeleteKitAsync(uint primaryKey, KitInclude include = KitInclude.Base, CancellationToken token = default)
    {
        KitModel? kit;

        await _semaphore.WaitAsync(token).ConfigureAwait(false);
        try
        {
            kit = await GetKitModel(primaryKey, include, token, true);
            if (kit == null)
                return null;

            uint pk = kit.PrimaryKey;

            _dbContext.Kits.Remove(kit);

            await _dbContext.SaveChangesAsync(token).ConfigureAwait(false);

            if (CacheEnabled)
            {
                _keyCache!.TryRemove(pk, out _);
                _idCache!.TryRemove(kit.Id, out _);
            }

            if (WarfareModule.IsActive)
            {
                if (kit.Type == KitType.Loadout && LoadoutIdHelper.Parse(kit.Id, out CSteamID player) >= 0)
                {
                    WarfarePlayer? onlinePlayer = _playerService?.GetOnlinePlayerOrNullThreadSafe(player);
                    if (onlinePlayer != null)
                    {
                        KitPlayerComponent comp = onlinePlayer.Component<KitPlayerComponent>();
                        comp.RemoveLoadout(pk);
                    }
                }

                UpdateSigns(kit.Id, null);
            }

            try
            {
                KitRemoved?.Invoke(kit);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error invoking KitRemoved for kit {kit.Id}.");
            }
        }
        finally
        {
            _dbContext.ChangeTracker.Clear();
            _semaphore.Release();
        }

        return kit;
    }

    public async Task<Kit?> UpdateKitAsync(uint primaryKey, KitInclude include, Action<KitModel> updateAction, CSteamID updater, CancellationToken token = default)
    {
        Kit createdKit;

        // prevents updateAction from calling UpdateKitAsync again.
        if (_isInUpdate)
            throw new InvalidOperationException("Nested invocation to UpdateKitAsync.");

        await _semaphore.WaitAsync(token).ConfigureAwait(false);
        try
        {
            _isInUpdate = true;

            KitModel? kit = await GetKitModel(primaryKey, include, token, true);
            if (kit == null)
                return null;

            string oldId = kit.Id;

            KitType oldType = kit.Type;

            kit.LastEditor = updater.GetEAccountType() == EAccountType.k_EAccountTypeIndividual ? updater.m_SteamID : 0;
            kit.LastEditedAt = DateTimeOffset.UtcNow;

            try
            {
                updateAction(kit);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Update callback threw an exception.");
                return null;
            }

            OnKitUpdatePreSave(kit, oldId);

            _dbContext.ChangeTracker.DetectChanges();

            try
            {
                await _dbContext.SaveChangesAsync(token).ConfigureAwait(false);
            }
            catch (DbUpdateException ex) when (ex.GetBaseException() is MySqlException { ErrorCode: MySqlErrorCode.DuplicateUnique })
            {
                throw new ArgumentException($"Duplicate kit id: \"{kit.Id}\".");
            }

            createdKit = GetOrCreateKit(kit, include);

            OnKitUpdatePostSave(createdKit, kit, include, oldType, oldId);

            try
            {
                KitUpdated?.Invoke(createdKit);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error invoking KitUpdated for kit {kit.Id}.");
            }
        }
        finally
        {
            _isInUpdate = false;
            _dbContext.ChangeTracker.Clear();
            _semaphore.Release();
        }

        return createdKit;
    }

    public async Task<Kit?> UpdateKitAsync(uint primaryKey, KitInclude include, Func<KitModel, Task> updateAction, CSteamID updater, CancellationToken token = default)
    {
        Kit createdKit;

        // prevents updateAction from calling UpdateKitAsync again.
        if (_isInUpdate)
            throw new InvalidOperationException("Nested invocation to UpdateKitAsync.");

        await _semaphore.WaitAsync(token).ConfigureAwait(false);
        try
        {
            _isInUpdate = true;

            KitModel? kit = await GetKitModel(primaryKey, include, token, true);
            if (kit == null)
                return null;

            string oldId = kit.Id;
            KitType oldType = kit.Type;

            kit.LastEditor = updater.GetEAccountType() == EAccountType.k_EAccountTypeIndividual ? updater.m_SteamID : 0;
            kit.LastEditedAt = DateTimeOffset.UtcNow;

            try
            {
                await updateAction(kit).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Update callback threw an exception.");
                return null;
            }

            OnKitUpdatePreSave(kit, oldId);

            _dbContext.ChangeTracker.DetectChanges();

            try
            {
                await _dbContext.SaveChangesAsync(token).ConfigureAwait(false);
            }
            catch (DbUpdateException ex) when (ex.GetBaseException() is MySqlException { ErrorCode: MySqlErrorCode.DuplicateUnique })
            {
                throw new ArgumentException($"Duplicate kit id: \"{kit.Id}\".");
            }

            createdKit = GetOrCreateKit(kit, include);

            OnKitUpdatePostSave(createdKit, kit, include, oldType, oldId);

            try
            {
                KitUpdated?.Invoke(createdKit);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error invoking KitUpdated for kit {kit.Id}.");
            }
        }
        finally
        {
            _isInUpdate = false;
            _dbContext.ChangeTracker.Clear();
            _semaphore.Release();
        }

        return createdKit;
    }

    private void OnKitUpdatePreSave(KitModel kit, string oldId)
    {
        if (kit.Id == null)
        {
            kit.Id = oldId;
        }
        else if (CacheEnabled && !kit.Id.Equals(oldId, StringComparison.Ordinal))
        {
            if (!_idCache!.TryRemove(kit.Id, out Kit kitMdl))
                return;

            if (kitMdl.Key != kit.PrimaryKey)
                _idCache.TryAdd(kitMdl.Id, kitMdl);
            else
                kitMdl.UpdateFromModel(kit, _factionDataStore, _languageDataStore);
        }
    }

    private void OnKitUpdatePostSave(Kit createdKit, KitModel kit, KitInclude include, KitType oldType, string oldId)
    {
        if (CacheEnabled)
        {
            if (_keyCache!.TryGetValue(kit.PrimaryKey, out Kit byKey))
            {
                byKey.UpdateFromModel(kit, _factionDataStore, _languageDataStore);
            }

            if (_idCache!.TryGetValue(kit.Id, out Kit byId) && !ReferenceEquals(byKey, byId))
            {
                byId.UpdateFromModel(kit, _factionDataStore, _languageDataStore);
            }
        }

        if (!WarfareModule.IsActive)
            return;

        if (kit.Type == KitType.Loadout && LoadoutIdHelper.Parse(kit.Id, out CSteamID player) >= 0 && (include & KitInclude.Cached) == KitInclude.Cached)
        {
            WarfarePlayer? onlinePlayer = _playerService?.GetOnlinePlayerOrNullThreadSafe(player);
            if (onlinePlayer != null)
            {
                KitPlayerComponent comp = onlinePlayer.Component<KitPlayerComponent>();
                comp.UpdateLoadout(createdKit);
            }
        }

        if (oldType != KitType.Loadout && kit.Type != KitType.Loadout)
        {
            if (!oldId.Equals(kit.Id, StringComparison.Ordinal))
            {
                UpdateSigns(oldId, null);
            }

            UpdateSigns(kit.Id, null);
            return;
        }

        if (_playerService != null)
        {
            CSteamID sId1 = default, sId2 = default;
            bool parsed1 = oldType == KitType.Loadout && LoadoutIdHelper.Parse(oldId, out sId1) >= 0;
            bool parsed2 = kit.Type == KitType.Loadout && LoadoutIdHelper.Parse(kit.Id, out sId2) >= 0;
            if (parsed1 & parsed2)
            {
                WarfarePlayer? pl1 = _playerService.GetOnlinePlayerOrNullThreadSafe(sId1.m_SteamID);
                if (pl1 != null || sId1.m_SteamID == sId2.m_SteamID)
                {
                    UpdateLoadouts(pl1);
                    if (pl1 == null)
                        return;
                }

                WarfarePlayer? pl2 = _playerService.GetOnlinePlayerOrNullThreadSafe(sId2.m_SteamID);
                if (pl2 != null || pl1 == null)
                    UpdateLoadouts(pl2);

                return;
            }
        }

        UpdateLoadouts(null);
    }

    /// <inheritdoc />
    public async Task<T?> QueryFirstAsync<T>(Func<IQueryable<KitModel>, IQueryable<T>> query, KitInclude include = KitInclude.Base, CancellationToken token = default)
    {
        await _semaphore.WaitAsync(token).ConfigureAwait(false);
        try
        {
            return await query(ApplyIncludes(include, _dbContext.Kits, false)).FirstOrDefaultAsync(token).ConfigureAwait(false);
        }
        finally
        {
            _semaphore.Release();
        }
    }

    /// <inheritdoc />
    public async Task<List<T>> QueryListAsync<T>(Func<IQueryable<KitModel>, IQueryable<T>> query, KitInclude include = KitInclude.Base, CancellationToken token = default)
    {
        await _semaphore.WaitAsync(token).ConfigureAwait(false);
        try
        {
            return await query(ApplyIncludes(include, _dbContext.Kits, false)).ToListAsync(token).ConfigureAwait(false);
        }
        finally
        {
            _semaphore.Release();
        }
    }

    /// <inheritdoc />
    public async Task<EliteBundle?> QueryEliteBundleAsync(uint primaryKey, bool includeKits = false, bool includeFaction = false, KitInclude kitListInclude = KitInclude.Translations, CancellationToken token = default)
    {
        await _semaphore.WaitAsync(token).ConfigureAwait(false);
        try
        {
            return await ApplyIncludes(_dbContext.EliteBundles, includeKits, includeFaction, kitListInclude, false).FirstOrDefaultAsync(x => x.PrimaryKey == primaryKey, token).ConfigureAwait(false);
        }
        finally
        {
            _semaphore.Release();
        }
    }

    /// <inheritdoc />
    public async Task<EliteBundle?> QueryEliteBundleAsync(string bundleId, bool includeKits = false, bool includeFaction = false, KitInclude kitListInclude = KitInclude.Translations, CancellationToken token = default)
    {
        await _semaphore.WaitAsync(token).ConfigureAwait(false);
        try
        {
            return await ApplyIncludes(_dbContext.EliteBundles, includeKits, includeFaction, kitListInclude, false).FirstOrDefaultAsync(x => x.Id == bundleId, token).ConfigureAwait(false);
        }
        finally
        {
            _semaphore.Release();
        }
    }

    /// <inheritdoc />
    public async Task<EliteBundle?> QueryEliteBundleAsync(Func<IQueryable<EliteBundle>, IQueryable<EliteBundle>> query, bool includeKits = false, bool includeFaction = false, KitInclude kitListInclude = KitInclude.Translations, CancellationToken token = default)
    {
        await _semaphore.WaitAsync(token).ConfigureAwait(false);
        try
        {
            return await query(ApplyIncludes(_dbContext.EliteBundles, includeKits, includeFaction, kitListInclude, false)).FirstOrDefaultAsync(token).ConfigureAwait(false);
        }
        finally
        {
            _semaphore.Release();
        }
    }

    /// <inheritdoc />
    public async Task<IList<EliteBundle>> QueryEliteBundlesAsync(Func<IQueryable<EliteBundle>, IQueryable<EliteBundle>> query, bool includeKits = false, bool includeFaction = false, KitInclude kitListInclude = KitInclude.Translations, CancellationToken token = default)
    {
        await _semaphore.WaitAsync(token).ConfigureAwait(false);
        try
        {
            return await query(ApplyIncludes(_dbContext.EliteBundles, includeKits, includeFaction, kitListInclude, false)).ToListAsync(token).ConfigureAwait(false);
        }
        finally
        {
            _semaphore.Release();
        }
    }

    private Kit GetOrCreateKit(KitModel model, KitInclude include)
    {
        // all cached kits must have Cached at least
        if (!CacheEnabled || (include & KitInclude.Cached) != KitInclude.Cached)
        {
            return new Kit(model, _factionDataStore, _languageDataStore);
        }

        // loadout of offline player, shouldn't be cached
        if (model.Type == KitType.Loadout && LoadoutIdHelper.Parse(model.Id, out CSteamID steam64) >= 0 && (_playerService == null || !_playerService.IsPlayerOnlineThreadSafe(steam64)))
        {
            _idCache!.TryRemove(model.Id, out _);
            _keyCache!.TryRemove(model.PrimaryKey, out _);
            return new Kit(model, _factionDataStore, _languageDataStore);
        }

        Kit kitByPk = _keyCache!.AddOrUpdate(model.PrimaryKey, _getOrCreateUpdateFuncId, _getOrCreateAddFuncId, model);

        Kit? prev = null;

        // threadsafe exchange
        _idCache!.AddOrUpdate(
            model.Id,
            _ => { prev = null; return kitByPk; },
            (_, old) => { prev = old; return kitByPk; }
        );

        if (prev != null && !ReferenceEquals(prev, kitByPk))
            prev.UpdateFromModel(model, _factionDataStore, _languageDataStore);

        return kitByPk;
    }

    private ConfiguredTaskAwaitable<KitModel> GetKitModel(uint primaryKey, KitInclude include, CancellationToken token, bool track)
    {
        return ApplyIncludes(include, _dbContext.Kits, track).FirstOrDefaultAsync(kit => kit!.PrimaryKey == primaryKey, token).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public Task<Kit?> QueryKitAsync(uint primaryKey, KitInclude include, CancellationToken token = default)
    {
        return QueryKitAsync(include, x => x.PrimaryKey == primaryKey, token);
    }

    /// <inheritdoc />
    public Task<Kit?> QueryKitAsync(string kitId, KitInclude include, CancellationToken token = default)
    {
        return QueryKitAsync(include, x => x.Id == kitId, token);
    }

    /// <inheritdoc />
    public async Task<Kit?> QueryKitAsync(KitInclude include, Expression<Func<KitModel, bool>> predicate, CancellationToken token = default)
    {
        await _semaphore.WaitAsync(token).ConfigureAwait(false);
        try
        {
            KitModel? model = await ApplyIncludes(include, _dbContext.Kits, false).FirstOrDefaultAsync(predicate, token).ConfigureAwait(false);
            return model == null ? null : GetOrCreateKit(model, include);
        }
        finally
        {
            _semaphore.Release();
        }
    }

    /// <inheritdoc />
    public async Task<Kit?> QueryKitAsync(KitInclude include, Func<IQueryable<KitModel>, IQueryable<KitModel>> query, CancellationToken token = default)
    {
        await _semaphore.WaitAsync(token).ConfigureAwait(false);
        try
        {
            KitModel? model = await query(ApplyIncludes(include, _dbContext.Kits, false)).FirstOrDefaultAsync(token).ConfigureAwait(false);
            return model == null ? null : GetOrCreateKit(model, include);
        }
        finally
        {
            _semaphore.Release();
        }
    }

    /// <inheritdoc />
    public async Task<int> QueryKitsAsync(KitInclude include, IList<Kit> output, Expression<Func<KitModel, bool>> predicate, CancellationToken token = default)
    {
        int ct = 0;
        await _semaphore.WaitAsync(token).ConfigureAwait(false);
        try
        {
            await foreach (KitModel model in ApplyIncludes(include, _dbContext.Kits, false).Where(predicate).AsAsyncEnumerable().WithCancellation(token))
            {
                ++ct;
                output.Add(GetOrCreateKit(model, include));
            }
        }
        finally
        {
            _semaphore.Release();
        }

        return ct;
    }

    /// <inheritdoc />
    public async Task<int> QueryKitsAsync(KitInclude include, IList<Kit> output, Func<IQueryable<KitModel>, IQueryable<KitModel>> query, CancellationToken token = default)
    {
        int ct = 0;
        await _semaphore.WaitAsync(token).ConfigureAwait(false);
        try
        {
            await foreach (KitModel model in query(ApplyIncludes(include, _dbContext.Kits, false)).AsAsyncEnumerable().WithCancellation(token))
            {
                ++ct;
                output.Add(GetOrCreateKit(model!, include));
            }
        }
        finally
        {
            _semaphore.Release();
        }

        return ct;
    }

    /// <inheritdoc />
    public async Task<Kit[]> QueryKitsAsync(KitInclude include, Expression<Func<KitModel, bool>> predicate, CancellationToken token = default)
    {
        await _semaphore.WaitAsync(token).ConfigureAwait(false);
        try
        {
            List<KitModel> models = await ApplyIncludes(include, _dbContext.Kits, false).Where(predicate).ToListAsync(token);
            Kit[] kits = new Kit[models.Count];
            for (int i = 0; i < models.Count; ++i)
            {
                kits[i] = GetOrCreateKit(models[i]!, include);
            }

            return kits;
        }
        finally
        {
            _semaphore.Release();
        }
    }

    /// <inheritdoc />
    public async Task<Kit[]> QueryKitsAsync(KitInclude include, Func<IQueryable<KitModel>, IQueryable<KitModel>> query, CancellationToken token = default)
    {
        await _semaphore.WaitAsync(token).ConfigureAwait(false);
        try
        {
            List<KitModel> models = await query(ApplyIncludes(include, _dbContext.Kits, false)).ToListAsync(token);
            Kit[] kits = new Kit[models.Count];
            for (int i = 0; i < models.Count; ++i)
            {
                kits[i] = GetOrCreateKit(models[i], include);
            }

            return kits;
        }
        finally
        {
            _semaphore.Release();
        }
    }

    internal static IQueryable<EliteBundle> ApplyIncludes(IQueryable<EliteBundle> model, bool includeKits, bool includeFaction, KitInclude kitListInclude, bool track)
    {
        IQueryable<EliteBundle> mdl = model;
        if (!track)
            mdl = mdl.AsNoTrackingWithIdentityResolution();
        if (includeKits)
        {
            if ((kitListInclude & KitInclude.None) == 0)
            {
                mdl = mdl.Include(x => x.Kits).ThenInclude(x => x.Kit);
                if ((kitListInclude & KitInclude.Items) != 0)
                {
                    mdl = mdl.Include(x => x.Kits).ThenInclude(x => x.Kit).ThenInclude(x => x.Items);
                }
                if ((kitListInclude & KitInclude.UnlockRequirements) != 0)
                {
                    mdl = mdl.Include(x => x.Kits).ThenInclude(x => x.Kit).ThenInclude(x => x.UnlockRequirements);
                }
                if ((kitListInclude & KitInclude.FactionFilter) != 0)
                {
                    mdl = mdl.Include(x => x.Kits).ThenInclude(x => x.Kit).ThenInclude(x => x.FactionFilter);
                }
                if ((kitListInclude & KitInclude.MapFilter) != 0)
                {
                    mdl = mdl.Include(x => x.Kits).ThenInclude(x => x.Kit).ThenInclude(x => x.MapFilter);
                }
                if ((kitListInclude & KitInclude.Translations) != 0)
                {
                    mdl = mdl.Include(x => x.Kits).ThenInclude(x => x.Kit).ThenInclude(x => x.Translations);
                }
                if ((kitListInclude & KitInclude.Skillsets) != 0)
                {
                    mdl = mdl.Include(x => x.Kits).ThenInclude(x => x.Kit).ThenInclude(x => x.Skillsets);
                }
                if ((kitListInclude & KitInclude.Bundles) != 0)
                {
                    mdl = mdl.Include(x => x.Kits).ThenInclude(x => x.Kit).ThenInclude(x => x.Bundles).ThenInclude(x => x.Bundle);
                }
                if ((kitListInclude & KitInclude.Access) != 0)
                {
                    mdl = mdl.Include(x => x.Kits).ThenInclude(x => x.Kit).ThenInclude(x => x.Access);
                }
                if ((kitListInclude & KitInclude.Delays) != 0)
                {
                    mdl = mdl.Include(x => x.Kits).ThenInclude(x => x.Kit).ThenInclude(x => x.Delays);
                }
                if ((kitListInclude & (KitInclude)(1 << 10)) != 0)
                {
                    mdl = mdl.Include(x => x.Kits).ThenInclude(x => x.Kit).ThenInclude(x => x.Faction);
                }
            }
            else
            {
                mdl = mdl.Include(x => x.Kits);
            }
        }

        if (includeFaction)
        {
            mdl = mdl.Include(x => x.Faction).ThenInclude(x => x!.Translations).Include(x => x.Faction);
        }

        return mdl;
    }

    internal static IQueryable<KitModel> ApplyIncludes(KitInclude include, IQueryable<KitModel> model, bool track)
    {
        IQueryable<KitModel> mdl = model;
        if (!track)
            mdl = mdl.AsNoTrackingWithIdentityResolution();

        if ((include & KitInclude.None) != 0)
            return mdl;

        if ((include & KitInclude.Items) != 0)
        {
            mdl = mdl.Include(x => x.Items);
        }
        if ((include & KitInclude.UnlockRequirements) != 0)
        {
            mdl = mdl.Include(x => x.UnlockRequirements);
        }
        if ((include & KitInclude.FactionFilter) != 0)
        {
            mdl = mdl.Include(x => x.FactionFilter);
        }
        if ((include & KitInclude.MapFilter) != 0)
        {
            mdl = mdl.Include(x => x.MapFilter);
        }
        if ((include & KitInclude.Translations) != 0)
        {
            mdl = mdl.Include(x => x.Translations);
        }
        if ((include & KitInclude.Skillsets) != 0)
        {
            mdl = mdl.Include(x => x.Skillsets);
        }
        if ((include & KitInclude.Bundles) != 0)
        {
            mdl = mdl.Include(x => x.Bundles).ThenInclude(x => x.Bundle);
        }
        if ((include & KitInclude.Access) != 0)
        {
            mdl = mdl.Include(x => x.Access);
        }
        if ((include & KitInclude.Delays) != 0)
        {
            mdl = mdl.Include(x => x.Delays);
        }
        if ((include & (KitInclude)(1 << 10)) != 0)
        {
            mdl = mdl.Include(x => x.Faction);
        }

        return mdl;
    }

    async UniTask IHostedService.StartAsync(CancellationToken token)
    {
        if (!CacheEnabled)
            return;

        await _semaphore.WaitAsync(token).ConfigureAwait(false);
        try
        {
            await foreach (KitModel model in ApplyIncludes(KitInclude.Cached, _dbContext.Kits, false)
                               .Where(x => x.Type != KitType.Loadout)
                               .AsAsyncEnumerable()
                               .WithCancellation(token))
            {
                Kit kit = new Kit(model, _factionDataStore, _languageDataStore);

                _keyCache![kit.Key] = kit;
                _idCache![kit.Id] = kit;
            }
        }
        finally
        {
            _semaphore.Release();
        }
    }

    /// <inheritdoc />
    UniTask IHostedService.StopAsync(CancellationToken token)
    {
        return UniTask.CompletedTask;
    }
}