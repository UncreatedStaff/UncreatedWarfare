using System;
using Uncreated.Warfare.Events.Logging;
using Uncreated.Warfare.Players;

namespace Uncreated.Warfare.Events.Models.Players;

/// <summary>
/// Invoked after a player takes damage, including the hit that kills them.
/// </summary>
public class PlayerDamaged : PlayerEvent, IActionLoggableEvent
{
    private readonly DamagePlayerParameters _parameters;

    /// <summary>
    /// Properties for how players are damaged.
    /// </summary>
    public ref readonly DamagePlayerParameters Parameters => ref _parameters;

    /// <summary>
    /// If this damage injured the player.
    /// </summary>
    public bool IsInjure { get; set; }

    /// <summary>
    /// If this damage killed the player.
    /// </summary>
    public bool IsDeath { get; set; }

    /// <summary>
    /// Player who caused the damage.
    /// </summary>
    public required WarfarePlayer? Instigator { get; init; }

    public PlayerDamaged(in DamagePlayerParameters parameters)
    {
        _parameters = parameters;
    }

    /// <inheritdoc />
    public ActionLogEntry GetActionLogEntry(IServiceProvider serviceProvider, ref ActionLogEntry[]? multipleEntries)
    {
        return new ActionLogEntry(ActionLogTypes.Chat,
            $"Damaged {Player}: {_parameters.damage * _parameters.times}, Cause: {_parameters.cause}, Limb: {_parameters.limb}, Direction: {_parameters.direction:F2}, Injured: {IsInjure}, Killed: {IsDeath}",
            _parameters.killer
        );
    }
}
