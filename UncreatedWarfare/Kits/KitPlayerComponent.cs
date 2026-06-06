using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.ObjectModel;
using System.Linq;
using Uncreated.Warfare.Database.Abstractions;
using Uncreated.Warfare.Kits.Loadouts;
using Uncreated.Warfare.Players;
using Uncreated.Warfare.Players.Management;

namespace Uncreated.Warfare.Kits;

[PlayerComponent]
public class KitPlayerComponent : IPlayerComponent
{
    private readonly HashSet<uint> _accessibleKits = new HashSet<uint>(16);
    private readonly HashSet<uint> _favoritedKits = new HashSet<uint>(16);
    private IDictionary<uint, BasicKitStats>? _cachedKitStats;


    private IKitDataStore _kitDataStore = null!;
    private KitSignService _kitSignService = null!;
    private LoadoutService _loadoutService = null!;
    private IKitStatisticService _kitStatService = null!;

#nullable disable

    public WarfarePlayer Player { get; private set; }

#nullable restore

    /// <summary>
    /// Comparer used to sort cached loadouts.
    /// </summary>
    [field: MaybeNull]
    public IComparer<Kit?> LoadoutComparer => field ??= new LoadoutComparerImpl(this);

    /// <summary>
    /// The player's current kit parameters.
    /// </summary>
    public CurrentKitState? ActiveKit { get; private set; }

    /// <summary>
    /// Gets the player's equipped kit, skipping their current kit if they're previewing a kit.
    /// </summary>
    /// <remarks>
    /// This should be used over <see cref="ActiveKit"/> for game logic
    /// so the preview kit doesn't affect gameplay.
    /// </remarks>
    public CurrentKitState? GetActiveEffectiveKit()
    {
        CurrentKitState? previewFallback = ActiveKit;
        while (previewFallback is { IsPreview: true })
            previewFallback = previewFallback.PreviewFallback;

        return previewFallback;
    }

    /// <summary>
    /// If the player has a kit equipped.
    /// </summary>
    public bool HasKit => ActiveKit != null;

    /// <summary>
    /// Ordered list of all loadouts including the <see cref="KitInclude.Cached"/> include level.
    /// </summary>
    public IReadOnlyList<Kit> Loadouts { get; private set; } = Array.Empty<Kit>();

    void IPlayerComponent.Init(IServiceProvider serviceProvider, bool isOnJoin)
    {
        _kitDataStore = serviceProvider.GetRequiredService<IKitDataStore>();
        _kitSignService = serviceProvider.GetRequiredService<KitSignService>();
        _loadoutService = serviceProvider.GetRequiredService<LoadoutService>();
        _kitStatService = serviceProvider.GetRequiredService<IKitStatisticService>();
    }

    /// <summary>
    /// Check whether or not this player is using a kit with the given <paramref name="class"/>.
    /// </summary>
    public bool IsClass(Class @class, bool useEffectiveKit = true)
    {
        CurrentKitState? ac = useEffectiveKit ? GetActiveEffectiveKit() : ActiveKit;
        if (@class == Class.None)
        {
            return ac == null || ac.Class == Class.None;
        }

        return ac != null && ac.Class == @class;
    }

    /// <summary>
    /// Check whether or not this player is using the kit with the given primary key.
    /// </summary>
    public bool IsKit(uint kitPk, bool useEffectiveKit = true)
    {
        CurrentKitState? ac = useEffectiveKit ? GetActiveEffectiveKit() : ActiveKit;
        return kitPk != 0 && ac != null && ac.Key == kitPk;
    }

    /// <summary>
    /// Check whether or not this player is using the kit with the given primary key.
    /// </summary>
    public bool IsKit(uint? kitPk, bool useEffectiveKit = true)
    {
        CurrentKitState? ac = useEffectiveKit ? GetActiveEffectiveKit() : ActiveKit;
        if (kitPk is null or 0)
            return ac == null;
        
        return ac != null && ac.Key == kitPk.Value;
    }

    /// <summary>
    /// Check whether or not this player is using the given kit.
    /// </summary>
    public bool IsKit(Kit? kit, bool useEffectiveKit = true)
    {
        CurrentKitState? ac = useEffectiveKit ? GetActiveEffectiveKit() : ActiveKit;
        if (kit == null)
            return ac == null;
        return ac != null && ac.Key == kit.Key;
    }

    /// <summary>
    /// Check whether or not this player is using the kit with the given ID.
    /// </summary>
    public bool IsKit(string? kitId, bool useEffectiveKit = true)
    {
        CurrentKitState? ac = useEffectiveKit ? GetActiveEffectiveKit() : ActiveKit;
        if (string.IsNullOrEmpty(kitId))
            return ac == null;
        return ac != null && ac.Id == kitId;
    }

    /// <summary>
    /// Get an up-to-date copy of the actively equipped kit.
    /// </summary>
    public Task<Kit?> GetActiveKitAsync(KitInclude include, CancellationToken token = default)
    {
        CurrentKitState? state = ActiveKit;
        if (state == null)
            return Task.FromResult<Kit?>(null);

        return _kitDataStore.QueryKitAsync(state.Key, include, token);
    }

    /// <summary>
    /// Get an up-to-date copy of the actively equipped kit.
    /// </summary>
    public Task<Kit?> GetActiveEffectiveKitAsync(KitInclude include, CancellationToken token = default)
    {
        CurrentKitState? state = GetActiveEffectiveKit();
        if (state == null)
            return Task.FromResult<Kit?>(null);

        return _kitDataStore.QueryKitAsync(state.Key, include, token);
    }

    /// <summary>
    /// Reloads all caches for this player.
    /// </summary>
    internal async Task ReloadCacheAsync(IKitsDbContext dbContext, CancellationToken token = default)
    {
        ulong s64 = Player.Steam64.m_SteamID;
        Task<List<uint>> favoritesTask = dbContext.KitFavorites
            .Where(x => x.Steam64 == s64)
            .Select(x => x.KitId)
            .ToListAsync(token);

        Task<IDictionary<uint, BasicKitStats>> statsTask = _kitStatService
            .QueryAllBasicKitStats(Player.Steam64, token: token);

        IReadOnlyList<Kit> loadouts = await _loadoutService.GetLoadouts(Player.Steam64, KitInclude.Cached, token)
            .ConfigureAwait(false);

        List<uint> favorites = await favoritesTask.ConfigureAwait(false);

        List<uint> access = await dbContext.KitAccess
            .Where(x => x.Steam64 == s64)
            .Select(x => x.KitId)
            .ToListAsync(token).ConfigureAwait(false);

        IDictionary<uint, BasicKitStats> cachedKitStats = await statsTask.ConfigureAwait(false);

        await UniTask.SwitchToMainThread(token);

        _cachedKitStats = cachedKitStats;

        UpdateLoadouts(loadouts);

        lock (_accessibleKits)
        {
            _accessibleKits.Clear();
            foreach (uint kit in access)
                _accessibleKits.Add(kit);
        }

        lock (_favoritedKits)
        {
            _favoritedKits.Clear();
            foreach (uint kit in favorites)
                _favoritedKits.Add(kit);
        }
    }

    internal void RemoveLoadout(uint pk)
    {
        lock (_accessibleKits)
        {
            int index = -1;
            for (int i = 0; i < Loadouts.Count; ++i)
            {
                if (Loadouts[i].Key != pk)
                    continue;
                index = i;
                break;
            }

            if (index == -1)
                return;

            List<Kit> newList = Loadouts.ToList();

            newList.RemoveAt(index);
            newList.Sort(LoadoutComparer);

            Loadouts = new ReadOnlyCollection<Kit>(newList);
        }
    }

    internal void UpdateLoadouts(IReadOnlyList<Kit> loadouts)
    {
        lock (_accessibleKits)
        {
            Loadouts = loadouts is IList<Kit> list
                ? new ReadOnlyCollection<Kit>(list)
                : loadouts;
        }

        // can be null in kit download pending player task
        _kitSignService?.UpdateLoadoutSigns(Player);
    }

    internal void UpdateLoadout(Kit loadout)
    {
        if (loadout.Type != KitType.Loadout || LoadoutIdHelper.Parse(loadout.Id, out CSteamID steam64) < 0 || !Player.Equals(steam64))
            return;

        lock (_accessibleKits)
        {
            List<Kit> newList = Loadouts.ToList();

            IComparer<Kit?> comparer = LoadoutComparer;

            newList.Sort(comparer);
            int index = newList.BinarySearch(loadout, comparer);
            if (index >= 0)
                newList[index] = loadout;
            else
                newList.Insert(~index, loadout);

            Loadouts = new ReadOnlyCollection<Kit>(newList);
        }
    }


    /// <summary>
    /// Set the current kit, or <see langword="null"/> for no kit.
    /// </summary>
    internal void UpdateKit(CurrentKitState? kit)
    {
        ActiveKit = kit;
    }

    public bool TryGetBasicStatsFor(uint kitPk, [NotNullWhen(true)] out BasicKitStats? kitStats)
    {
        IDictionary<uint, BasicKitStats>? stats = _cachedKitStats;
        if (stats == null || kitPk == 0)
        {
            kitStats = null;
            return false;
        }

        lock (stats)
        {
            return stats.TryGetValue(kitPk, out kitStats);
        }
    }

    public void UpdateBasicStats(uint kitPk, Action<BasicKitStats> action)
    {
        IDictionary<uint, BasicKitStats>? stats = _cachedKitStats;
        if (stats == null || kitPk == 0)
            return;

        lock (stats)
        {
            if (!stats.TryGetValue(kitPk, out BasicKitStats s))
            {
                s = new BasicKitStats(kitPk);
                stats.Add(kitPk, s);
            }

            action(s);
        }
    }

    public bool IsKitAccessible(uint kitPk)
    {
        lock (_accessibleKits)
        {
            return _accessibleKits.Contains(kitPk);
        }
    }

    public bool IsKitFavorited(uint kitPk)
    {
        lock (_favoritedKits)
        {
            return _favoritedKits.Contains(kitPk);
        }
    }

    internal bool AddAccessibleKit(uint kitPk)
    {
        lock (_accessibleKits)
        {
            return _accessibleKits.Add(kitPk);
        }
    }

    internal bool RemoveAccessibleKit(uint kitPk)
    {
        lock (_accessibleKits)
        {
            return _accessibleKits.Remove(kitPk);
        }
    }

    internal bool AddFavoriteKit(uint kitPk)
    {
        lock (_favoritedKits)
        {
            return _favoritedKits.Add(kitPk);
        }
    }

    internal bool RemoveFavoriteKit(uint kitPk)
    {
        lock (_favoritedKits)
        {
            return _favoritedKits.Remove(kitPk);
        }
    }

    WarfarePlayer IPlayerComponent.Player { get => Player; set => Player = value; }

    private class LoadoutComparerImpl : IComparer<Kit?>
    {
        private readonly KitPlayerComponent _component;
        public LoadoutComparerImpl(KitPlayerComponent component)
        {
            _component = component;
        }

        public int Compare(Kit? a, Kit? b)
        {
            if (ReferenceEquals(a, b))
                return 0;

            if (a == null)
                return 1;

            if (b == null)
                return -1;

            if (a.Key == b.Key)
                return 0;

            if (_component.IsKitFavorited(a.Key))
            {
                if (!_component.IsKitFavorited(b.Key))
                    return -1;
            }
            else if (_component.IsKitFavorited(b.Key))
            {
                return 1;
            }

            int aParse = LoadoutIdHelper.Parse(a.Id, out _);
            int bParse = LoadoutIdHelper.Parse(b.Id, out _);

            if (aParse < 0)
            {
                if (bParse >= 0)
                    return 1;
            }
            else if (bParse < 0)
                return -1;

            return aParse.CompareTo(bParse);
        }
    }
}