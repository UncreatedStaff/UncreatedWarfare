using Uncreated.Warfare.Configuration;
using Uncreated.Warfare.Players;

namespace Uncreated.Warfare.Events.Models.Players;

/// <summary>
/// Invoked when a player tries to use a consumable item on another player.
/// </summary>
[EventModel(EventSynchronizationContext.PerPlayer, SynchronizedModelTags = [ "modify_inventory", "modify_useable" ])]
public sealed class AidPlayerRequested : CancellablePlayerEvent
{
    /// <summary>
    /// The player who aided <see cref="PlayerEvent.Player"/>.
    /// </summary>
    public required WarfarePlayer Medic { get; init; }

    /// <summary>
    /// The item used to aid.
    /// </summary>
    public required IAssetLink<ItemConsumeableAsset> Item { get; init; }
    
    /// <summary>
    /// If this aid revived <see cref="PlayerEvent.Player"/>.
    /// </summary>
    public bool IsRevive { get; internal set; }

    /// <summary>
    /// If this aid revived <see cref="PlayerEvent.Player"/> and if the medic was awarded points.
    /// This will be <see langword="false"/> if the player recently revived the medic already.
    /// </summary>
    public bool IsEffectiveRevive { get; internal set; }

    public required byte StartingHealth { get; init; }
    public required bool StartingBleedState { get; init; }
    public required bool StartingBrokenBonesState { get; init; }
    public required byte StartingFood { get; init; }
    public required byte StartingWater { get; init; }
    public required byte StartingInfection { get; init; }
    public required byte StartingStamina { get; init; }
    public required uint StartingWarmth { get; init; }
    public required uint StartingExperience { get; init; }
}
