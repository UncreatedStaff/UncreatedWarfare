using Uncreated.Warfare.Players;

namespace Uncreated.Warfare.Layouts.Phases;

public interface IAttackDefenseDecider
{
    /// <summary>
    /// If the player is actively attacking the objective.
    /// </summary>
    bool IsAttacking(WarfarePlayer player);

    /// <summary>
    /// If the player is actively defending the objective.
    /// </summary>
    bool IsDefending(WarfarePlayer player);
}
