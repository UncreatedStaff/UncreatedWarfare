using System;
using Uncreated.Warfare.Events.Logging;
using Uncreated.Warfare.Kits;
using Uncreated.Warfare.Util;

namespace Uncreated.Warfare.Events.Models.Kits;

/// <summary>
/// Handles when a player's kit is changed, re-equipped, or dequiped.
/// </summary>
[EventModel(EventSynchronizationContext.Pure)]
public class PlayerKitChanged : PlayerEvent, IActionLoggableEvent
{
    /// <summary>
    /// The new state of the player's kit parameters, or <see langword="null"/> if the player has no kit.
    /// </summary>
    public required CurrentKitState? State { get; init; }

    /// <summary>
    /// If the kit was requested instead of being given using /kit give or similar unofficial methods.
    /// </summary>
    public required bool WasRequested { get; init; }

    /// <inheritdoc />
    public ActionLogEntry GetActionLogEntry(IServiceProvider serviceProvider, ref ActionLogEntry[]? multipleEntries)
    {
        return new ActionLogEntry(ActionLogTypes.ChangedKit,
            State == null ? "Kit removed" : $"Kit {State.Key} ({State.Id}) class: {EnumUtility.GetNameSafe(State.Class)}, requested: {WasRequested}, {State.ParameterString}",
            Player.Steam64.m_SteamID
        );
    }
}