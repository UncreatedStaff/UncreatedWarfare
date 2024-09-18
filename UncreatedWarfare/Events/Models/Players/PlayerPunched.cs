namespace Uncreated.Warfare.Events.Models.Players;

/// <summary>
/// Handles <see cref="PlayerEquipment.OnPunch_Global"/>.
/// </summary>
public class PlayerPunched : PlayerEvent
{
    public required EPlayerPunch PunchType { get; init; }
}
