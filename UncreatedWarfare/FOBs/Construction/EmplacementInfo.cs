using Uncreated.Warfare.Configuration;

namespace Uncreated.Warfare.FOBs.Construction;

/// <summary>
/// Stores extra data required for <see cref="ShovelableType.Emplacement"/> buildables.
/// </summary>
public class EmplacementInfo
{
    /// <summary>
    /// The vehicle to be spawned on build.
    /// </summary>
    public IAssetLink<VehicleAsset> EmplacementVehicle { get; set; }

    /// <summary>
    /// The base barricade to be spawned under the vehicle.
    /// </summary>
    public IAssetLink<ItemPlaceableAsset> BaseBuildable { get; set; }

    /// <summary>
    /// Item used with /ammo refilling.
    /// </summary>
    public IAssetLink<ItemAsset> Ammo { get; set; }

    /// <summary>
    /// Number of <see cref="Ammo"/> items to spawn.
    /// </summary>
    public int AmmoCount { get; set; }

    /// <summary>
    /// If this emplacement should warn friendlies when they're in the fire zone.
    /// </summary>
    public bool ShouldWarnFriendlies { get; set; }

    /// <summary>
    /// If this emplacement should warn enemies when they're in the fire zone and have the ability to sense incoming projectiles.
    /// </summary>
    public bool ShouldWarnEnemies { get; set; }
}