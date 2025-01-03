using Uncreated.Warfare.Configuration;
using Uncreated.Warfare.Players;

namespace Uncreated.Warfare.Events.Models.Players;

/// <summary>
/// Invoked when a player uses a consumable item on another player.
/// </summary>
public sealed class PlayerAided : CancellablePlayerEvent
{
    /// <summary>
    /// The player who aided <see cref="PlayerEvent.Player"/>.
    /// </summary>
    public required WarfarePlayer Medic { get; init; }

    /// <summary>
    /// The amount of health added to <see cref="PlayerEvent.Player"/>.
    /// </summary>
    public required int HealthChange { get; init; }
    public required bool BleedStateChanged { get; init; }
    public required bool BrokenBonesStateChanged { get; init; }
    public required int FoodChange { get; init; }
    public required int WaterChange { get; init; }
    public required int InfectionChange { get; init; }
    public required int StaminaChange { get; init; }
    public required int WarmthChange { get; init; }
    public required int ExperienceChange { get; init; }

    /// <summary>
    /// The item used to aid.
    /// </summary>
    public required IAssetLink<ItemConsumeableAsset> Item { get; init; }

    /// <summary>
    /// If this aid revived <see cref="PlayerEvent.Player"/>.
    /// </summary>
    public required bool IsRevive { get; init; }
}
