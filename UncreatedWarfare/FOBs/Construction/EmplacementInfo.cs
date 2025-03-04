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
    public required IAssetLink<VehicleAsset> Vehicle { get; set; }

    /// <summary>
    /// If this emplacement should warn friendlies when they're in the fire zone.
    /// </summary>
    public bool ShouldWarnFriendlies { get; set; } = false;

    /// <summary>
    /// If this emplacement should warn enemies when they're in the fire zone and have the ability to sense incoming projectiles.
    /// </summary>
    public bool ShouldWarnEnemies { get; set; } = false;
}