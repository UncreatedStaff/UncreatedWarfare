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
    /// Whether or not all viewers see the same thing, so <see cref="Resolve"/> will not be passed a player if this is <see langword="true"/>.
    /// </summary>
    bool PlayerAgnostic { get; }

    /// <summary>
    /// Decides which asset a player should see.
    /// </summary>
    /// <param name="player">The player viewing the clothing.</param>
    /// <param name="onPlayer">The player wearing the clothing.</param>
    /// <param name="slot">Information about the player.</param>
    /// <param name="kit">The kit the player has equipped.</param>
    /// <returns>The clothing type that should actually be seen by the <paramref name="player"/>.</returns>
    ItemClothingAsset? Resolve(WarfarePlayer? player, WarfarePlayer onPlayer, in ClothingItem slot, Kit kit, ref byte quality, ref byte[] state);

    /// <summary>
    /// Decides whether or not a kit is checked for cosmetics. For example, public kits are usually not checked.
    /// </summary>
    bool ShouldKitCosmeticsBeInstanced(Kit kit);
}