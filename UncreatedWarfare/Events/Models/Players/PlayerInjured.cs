using Uncreated.Warfare.Players;

namespace Uncreated.Warfare.Events.Models.Players;

/// <summary>
/// Handles a player being injured.
/// </summary>
public class PlayerInjured : PlayerEvent
{
    private readonly DamagePlayerParameters _parameters;

    /// <summary>
    /// Mutable properties for how players are damaged to get injured.
    /// </summary>
    public ref readonly DamagePlayerParameters Parameters => ref _parameters;

    /// <summary>
    /// The player that instigated the damage.
    /// </summary>
    public required WarfarePlayer? Instigator { get; init; }
    public PlayerInjured(in DamagePlayerParameters parameters)
    {
        _parameters = parameters;
    }
}