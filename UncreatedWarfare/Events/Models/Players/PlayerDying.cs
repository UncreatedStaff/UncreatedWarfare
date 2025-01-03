using Uncreated.Warfare.Players;

namespace Uncreated.Warfare.Events.Models.Players;

/// <summary>
/// Invoked just before the player dies. Does not support async event listeners.
/// </summary>
public class PlayerDying : PlayerEvent
{
    private readonly DamagePlayerParameters _parameters;

    /// <summary>
    /// Properties for how players are damaged for the final hit of damage.
    /// </summary>
    public ref readonly DamagePlayerParameters Parameters => ref _parameters;

    /// <summary>
    /// Player who caused the final hit of damage.
    /// </summary>
    public required WarfarePlayer? Instigator { get; init; }

    public PlayerDying(in DamagePlayerParameters parameters)
    {
        _parameters = parameters;
    }
}