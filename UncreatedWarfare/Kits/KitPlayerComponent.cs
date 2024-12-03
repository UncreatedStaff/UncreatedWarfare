using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;
using Uncreated.Warfare.Database.Abstractions;
using Uncreated.Warfare.Models.Kits;
using Uncreated.Warfare.Players;
using Uncreated.Warfare.Players.Management;

namespace Uncreated.Warfare.Kits;

[PlayerComponent]
public class KitPlayerComponent : IPlayerComponent
{
    private KitManager _kitManager;
    internal List<uint>? AccessibleKits;
    internal List<uint>? FavoritedKits;
    internal bool FavoritesDirty;

    public WarfarePlayer Player { get; private set; }

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
    /// <remarks>Storing a reference to the kit itself can't be done since <see cref="Kit"/> is a live EF entity.</remarks>
    public Kit? CachedKit
    {
        get
        {
            if (!ActiveKitKey.HasValue)
                return null;

            _kitManager.Cache.TryGetKit(ActiveKitKey.Value, out Kit kit);
            return kit;
        }
    }

    void IPlayerComponent.Init(IServiceProvider serviceProvider, bool isOnJoin)
    {
        _kitManager = serviceProvider.GetRequiredService<KitManager>();
    }

    /// <summary>
    /// Get an up-to-date copy of the actively equipped kit.
    /// </summary>
    public async Task<Kit?> GetActiveKitAsync(CancellationToken token = default, Func<IKitsDbContext, IQueryable<Kit>>? set = null)
    {
        uint? kit = ActiveKitKey;
        if (!kit.HasValue)
            return null;

        return await _kitManager.GetKit(kit.Value, token, set);
    }

    /// <summary>
    /// Set the current kit, or <see langword="null"/> for no kit.
    /// </summary>
    public void UpdateKit(Kit? kit)
    {
        if (kit != null)
        {
            ActiveKitKey = kit.PrimaryKey;
            ActiveKitId = kit.InternalName;
            ActiveClass = kit.Class;
            ActiveBranch = kit.Branch;
        }
        else
        {
            ActiveKitKey = null;
            ActiveKitId = null;
            ActiveClass = Class.None;
            ActiveBranch = Branch.Default;
        }
    }

    WarfarePlayer IPlayerComponent.Player { get => Player; set => Player = value; }
}
