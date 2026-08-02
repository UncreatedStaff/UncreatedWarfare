using System;
using Uncreated.Warfare.Layouts.Teams;
using Uncreated.Warfare.Util;

namespace Uncreated.Warfare.FOBs.SupplyCrates;

/// <summary>
/// An ammo storage device, such as a ammo bag or supply crate.
/// </summary>
public interface IAmmoStorage
{
    /// <summary>
    /// Whether or not this ammo storage allows players to change their kit or not.
    /// </summary>
    bool CanChangeKit { get; }

    /// <summary>
    /// If <see langword="true"/>, allows rearming or switching kits when this ammo storage has less than the required amount, so long as its greater than 0.
    /// </summary>
    bool AllowDiscountedRearm { get; }

    /// <summary>
    /// Amount of ammo currently in storage. May be <see cref="float.PositiveInfinity"/> for infinite ammo caches.
    /// </summary>
    /// <exception cref="GameThreadException"/>
    float AmmoCount { get; }
    
    /// <summary>
    /// Steam ID of the owner of the ammo storage.
    /// </summary>
    CSteamID Owner { get; }
    
    /// <summary>
    /// Team that owns the ammo storage.
    /// </summary>
    Team Team { get; }

    /// <summary>
    /// The origin of the ammo crate.
    /// </summary>
    Vector3 Point { get; }

    /// <summary>
    /// The valid distance away from the ammo crate that it can be realistically interacted from.
    /// </summary>
    /// <remarks>Usually this should be <c>4</c> plus the maximum radius of the object, plus a little padding.</remarks>
    float InteractRange { get; }

    /// <summary>
    /// Invoked when the ammo count on this storage is updated.
    /// </summary>
    event Action<float>? AmmoCountUpdated;

    /// <summary>
    /// Allows synchronizing requests so you can't have two people requesting kits at the same time to avoid ammo limits.
    /// </summary>
    /// <value>The semaphore to use, or <see langword="null"/> to not synchronize access.</value>
    SemaphoreSlim? InteractSemaphore { get; }

    /// <summary>
    /// Remove <paramref name="ammoCount"/> ammo supplies from the storage, possibly destroying it.
    /// </summary>
    /// <exception cref="GameThreadException"/>
    void SubtractAmmo(float ammoCount);
}

/// <summary>
/// A virtual ammo crate wrapper that should be disposed after usage.
/// </summary>
public interface ITemporaryAmmoStorage : IAmmoStorage, IDisposable;