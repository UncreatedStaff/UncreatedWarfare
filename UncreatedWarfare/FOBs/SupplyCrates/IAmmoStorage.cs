using System;
using Uncreated.Warfare.Layouts.Teams;

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
    /// Amount of ammo currently in storage.
    /// </summary>
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
    event Action? AmmoCountUpdated;

    /// <summary>
    /// Remove <paramref name="ammoCount"/> ammo supplies from the storage, possibly destroying it.
    /// </summary>
    void SubtractAmmo(float ammoCount);
}

/// <summary>
/// A virtual ammo crate wrapper that should be disposed after usage.
/// </summary>
public interface ITemporaryAmmoStorage : IAmmoStorage, IDisposable;