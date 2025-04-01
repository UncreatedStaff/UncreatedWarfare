using System;
using Uncreated.Warfare.Configuration;
using Uncreated.Warfare.Events.Logging;

namespace Uncreated.Warfare.Events.Models.Players;

/// <summary>
/// Invoked after a player melee's.
/// </summary>
public class PlayerMeleed : PlayerEvent, IActionLoggableEvent
{
    /// <summary>
    /// The asset of the melee weapon that the player swung with.
    /// </summary>
    public required ItemMeleeAsset Asset { get; init; }

    /// <summary>
    /// The <see cref="SDG.Unturned.InputInfo"/> associated with the swing.
    /// </summary>
    public required InputInfo InputInfo { get; init; }

    /// <inheritdoc />
    public ActionLogEntry GetActionLogEntry(IServiceProvider serviceProvider, ref ActionLogEntry[]? multipleEntries)
    {
        return new ActionLogEntry(ActionLogTypes.Melee,
            $"{AssetLink.ToDisplayString(Asset)} - {ActionLoggerService.DescribeInput(InputInfo)}",
            Player.Steam64.m_SteamID
        );
    }
}
