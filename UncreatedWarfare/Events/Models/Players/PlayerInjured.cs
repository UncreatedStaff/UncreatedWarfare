using System;
using Uncreated.Warfare.Events.Logging;
using Uncreated.Warfare.Players;
using Uncreated.Warfare.Util;

namespace Uncreated.Warfare.Events.Models.Players;

/// <summary>
/// Handles a player being injured.
/// </summary>
public class PlayerInjured : PlayerEvent, IActionLoggableEvent
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

    /// <inheritdoc />
    public ActionLogEntry GetActionLogEntry(IServiceProvider serviceProvider, ref ActionLogEntry[]? multipleEntries)
    {
        return new ActionLogEntry(ActionLogTypes.PlayerInjured,
            $"{Player}, Cause: {EnumUtility.GetNameSafe(Parameters.cause)}, Limb: {EnumUtility.GetNameSafe(Parameters.limb)}.",
            Instigator
        );
    }
}