using Uncreated.Warfare.Configuration;

namespace Uncreated.Warfare.FOBs.StateStorage;

/// <summary>
/// Represents
/// </summary>
public class BarricadeStateSave
{
    /// <summary>
    /// The type of barricade that this state will be applied to after one is spawned.
    /// </summary>
    public required IAssetLink<ItemBarricadeAsset> BarricadeAsset { get; init; }
    /// <summary>
    /// A friendly name to help humans identify this save after it is stored.
    /// </summary>
    public required string InertFriendlyName { get; init; }
    /// <summary>
    /// The associated <see cref="FactionInfo"/> with this saved state.
    /// If present, this save should only be applied to barricades placed by a player belonging to the corresponding faction.
    /// Otherwise, this save should be applied to all barricades. 
    /// </summary>
    public string? FactionId { get; init; }
    /// <summary>
    /// The saved barricade state, encoded as a Base64 <see langword="string"/> (converted from a <see cref="byte"/> array). This encodes the barricade's stored items and more.
    /// </summary>
    public required string Base64State { get; init; }
}
