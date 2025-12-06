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


    private IKitDataStore _kitDataStore = null!;
    private KitSignService _kitSignService = null!;
    private LoadoutService _loadoutService = null!;

#nullable disable

    public WarfarePlayer Player { get; private set; }

#nullable restore

    /// <summary>
    /// Comparer used to sort cached loadouts.
    /// </summary>
    [field: MaybeNull]
    public IComparer<Kit?> LoadoutComparer => field ??= new LoadoutComparerImpl(this);

    /// <summary>
    /// The primary key of the player's current kit.
    /// </summary>
    public uint? ActiveKitKey { get; private set; }

    /// <summary>
    /// The internal name of the player's current kit.
    /// </summary>
    public string? ActiveKitId { get; private set; }

    /// <summary>
    /// The class of the player's current kit.
    /// </summary>
    public Class ActiveClass { get; private set; }

    /// <summary>
    /// The branch of the player's current kit.
    /// </summary>
    public Branch ActiveBranch { get; private set; }

    /// <summary>
    /// If the player has a kit equipped.
    /// </summary>
    public bool HasKit => ActiveKitKey.HasValue;

    /// <summary>
    /// If the player's current kit was equipped with low ammo.
    /// </summary>
    public bool HasLowAmmo { get; private set; }

    /// <summary>
    /// Get a copy of the kit from the kit cache if it's added. Use <see cref="GetActiveKitAsync"/> to get an up-to-date copy.
    /// </summary>
    /// <remarks>Guaranteed to have <see cref="KitInclude.Giveable"/> data.</remarks>
    public Kit? CachedKit { get; internal set; }

    /// <summary>
    /// Ordered list of all loadouts including the <see cref="KitInclude.Cached"/> include level.
    /// </summary>
    public IReadOnlyList<Kit> Loadouts { get; private set; } = Array.Empty<Kit>();

    void IPlayerComponent.Init(IServiceProvider serviceProvider, bool isOnJoin)
    {
        _kitDataStore = serviceProvider.GetRequiredService<IKitDataStore>();
        _kitSignService = serviceProvider.GetRequiredService<KitSignService>();
        _loadoutService = serviceProvider.GetRequiredService<LoadoutService>();
    }

    /// <summary>
    /// Get an up-to-date copy of the actively equipped kit.
    /// </summary>
    public Task<Kit?> GetActiveKitAsync(KitInclude include, CancellationToken token = default)
    {
        uint? kit = ActiveKitKey;
        if (!kit.HasValue)
            return Task.FromResult<Kit?>(null);

        return _kitDataStore.QueryKitAsync(kit.Value, include, token);
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

        IReadOnlyList<Kit> loadouts = await _loadoutService.GetLoadouts(Player.Steam64, KitInclude.Cached, token)
            .ConfigureAwait(false);

        List<uint> favorites = await favoritesTask.ConfigureAwait(false);

        List<uint> access = await dbContext.KitAccess
            .Where(x => x.Steam64 == s64)
            .Select(x => x.KitId)
            .ToListAsync(token).ConfigureAwait(false);

        await UniTask.SwitchToMainThread(token);

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
    internal void UpdateKit(Kit? kit, bool isLowAmmo)
    {
        if (kit != null)
        {
            _ = kit.Items;
            ActiveKitKey = kit.Key;
            ActiveKitId = kit.Id;
            ActiveClass = kit.Class;
            ActiveBranch = kit.Branch;
            CachedKit = kit;
        }
        else
        {
            CachedKit = null;
            ActiveKitKey = null;
            ActiveKitId = null;
            ActiveClass = Class.None;
            ActiveBranch = Branch.Default;
        }

        HasLowAmmo = isLowAmmo;
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