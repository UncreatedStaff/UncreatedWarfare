using Uncreated.Warfare.Util.Inventory;

namespace Uncreated.Warfare.Events.Models.Items;

/// <summary>
/// Invoked when a player swaps their clothes to another item or remove clothes completely.
/// </summary>
[EventModel(EventSynchronizationContext.Pure)]
public class ClothingSwapped : PlayerEvent
{
    /// <summary>
    /// The clothing slot being modified.
    /// </summary>
    public required ClothingType Type { get; init; }

    /// <summary>
    /// The asset of the item being equipped, if any.
    /// </summary>
    public required ItemClothingAsset? Asset { get; init; }

    /// <summary>
    /// The metadata of the item being equipped, if any.
    /// </summary>
    public required byte[]? State { get; init; }

    /// <summary>
    /// The quality of the item being equipped, if any.
    /// </summary>
    public required byte Quality { get; init; }

    /// <summary>
    /// If the equip sound was played on the player's side.
    /// </summary>
    public required bool EffectPlayed { get; init; }
}