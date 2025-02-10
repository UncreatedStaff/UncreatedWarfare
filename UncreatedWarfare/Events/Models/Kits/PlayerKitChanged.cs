using Uncreated.Warfare.Kits;

namespace Uncreated.Warfare.Events.Models.Kits;

/// <summary>
/// Handles when a player's kit is changed, re-equipped, or dequiped.
/// </summary>
public class PlayerKitChanged : PlayerEvent
{
    /// <summary>
    /// The kit the player changed to, or <see langword="null"/> if the kit was dequipped.
    /// </summary>
    public required Kit? Kit { get; init; }

    /// <summary>
    /// The ID of the kit the player changed to, or 0 if the kit was dequipped.
    /// </summary>
    public required uint KitId { get; init; }

    /// <summary>
    /// The name of the kit the player changed to, or <see langword="null"/> if the kit was dequipped.
    /// </summary>
    public required string? KitName { get; init; }

    /// <summary>
    /// The class of the kit that was changed to.
    /// </summary>
    public required Class Class { get; init; }

    /// <summary>
    /// If the kit was requested instead of being given using /kit give or similar unofficial methods.
    /// </summary>
    public required bool WasRequested { get; init; }
}