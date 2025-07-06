using Uncreated.Warfare.Players;

namespace Uncreated.Warfare.Events.Models.Fobs;

/// <summary>
/// Invokes a fob UI update for the player this event was for.
/// </summary>
public interface IPlayerNeedsFobUIUpdateEvent
{
    WarfarePlayer? Player { get; }
}