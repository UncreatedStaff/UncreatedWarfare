using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using Uncreated.Warfare.Players;
using Uncreated.Warfare.Players.Management;

namespace Uncreated.Warfare.Kits;

[PlayerComponent]
public class KitPlayerComponent : IPlayerComponent
{
    private readonly HashSet<uint> _accessibleKits = new HashSet<uint>(16);
    private readonly HashSet<uint> _favoritedKits = new HashSet<uint>(16);

    private IKitDataStore _kitDataStore = null!;

#nullable disable

    public WarfarePlayer Player { get; private set; }

#nullable restore


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
    /// Get a copy of the kit from the kit cache if it's added. Use <see cref="GetActiveKitAsync"/> to get an up-to-date copy.
    /// </summary>
    /// <remarks>Guaranteed to have <see cref="KitInclude.Giveable"/> data.</remarks>
    public Kit? CachedKit { get; private set; }

    void IPlayerComponent.Init(IServiceProvider serviceProvider, bool isOnJoin)
    {
        _kitDataStore = serviceProvider.GetRequiredService<IKitDataStore>();
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
    /// Set the current kit, or <see langword="null"/> for no kit.
    /// </summary>
    internal void UpdateKit(Kit? kit)
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

    internal void AddAccessibleKit(uint kitPk)
    {
        lock (_accessibleKits)
        {
            _accessibleKits.Add(kitPk);
        }
    }

    internal void RemoveAccessibleKit(uint kitPk)
    {
        lock (_accessibleKits)
        {
            _accessibleKits.Remove(kitPk);
        }
    }

    internal void AddFavoriteKit(uint kitPk)
    {
        lock (_favoritedKits)
        {
            _favoritedKits.Add(kitPk);
        }
    }

    internal void RemoveFavoriteKit(uint kitPk)
    {
        lock (_favoritedKits)
        {
            _favoritedKits.Remove(kitPk);
        }
    }

    WarfarePlayer IPlayerComponent.Player { get => Player; set => Player = value; }
}
