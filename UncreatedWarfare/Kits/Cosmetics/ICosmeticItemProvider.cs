using Uncreated.Warfare.Players;
using Uncreated.Warfare.Util.Inventory;

namespace Uncreated.Warfare.Kits.Cosmetics;

/// <summary>
/// Service used to determine which cosmetic items are seen by which players.
/// </summary>
public interface ICosmeticItemProvider
{
    /// <summary>
    /// Whether or not any instancing should happen for any players.
    /// </summary>
    bool IsEnabled { get; }

    /// <summary>
    /// Decides which asset a player should see, given the <paramref name="actual"/> clothes the player is wearing.
    /// </summary>
    /// <param name="player">The player viewing the clothing.</param>
    /// <param name="onPlayer">The player wearing the clothing.</param>
    /// <param name="slot">Information about the player.</param>
    /// <param name="kit">The kit the player has equipped.</param>
    /// <returns>The clothing type that should actually be seen by the <paramref name="player"/>.</returns>
    ItemClothingAsset? DecideVisibleAsset(WarfarePlayer player, WarfarePlayer onPlayer, in ClothingItem slot, Kit kit);

    /// <summary>
    /// Decides whether or not a kit is checked for cosmetics. For example, public kits are usually not checked.
    /// </summary>
    bool ShouldKitCosmeticsBeInstanced(Kit kit);
}