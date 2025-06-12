using System;
using Uncreated.Warfare.Events.Logging;
using Uncreated.Warfare.Fobs;
using Uncreated.Warfare.FOBs;
using Uncreated.Warfare.FOBs.SupplyCrates;
using Uncreated.Warfare.Players;
using Uncreated.Warfare.Util;

namespace Uncreated.Warfare.Events.Models.Fobs;

/// <summary>
/// Event listener args which fires after supplies are added or removed from a <see cref="IResourceFob"/>.
/// </summary>
[EventModel(EventSynchronizationContext.Pure)]
public class FobSuppliesChanged : IActionLoggableEvent, IFobNeedsUIUpdateEvent
{
    /// <summary>
    /// The <see cref="IResourceFob"/> where supplies were added or removed.
    /// </summary>
    public required IResourceFob Fob { get; init; }
    /// <summary>
    /// The number of supplies that were added (positive) or removed (negative).
    /// </summary>
    public required float AmountDelta { get; init; }
    /// <summary>
    /// The type of supplies that were added or removed.
    /// </summary>
    public required SupplyType SupplyType { get; init; }
    /// <summary>
    /// The reason the supplies were added or removed.
    /// </summary>
    public required SupplyChangeReason ChangeReason { get; init; }
    /// <summary>
    /// The player who resupplied this fob, if this event was invoked due to resupplying a fob.
    /// </summary>
    public required WarfarePlayer? Resupplier { get; init; }

    /// <inheritdoc />
    public ActionLogEntry GetActionLogEntry(IServiceProvider serviceProvider, ref ActionLogEntry[]? multipleEntries)
    {
        return new ActionLogEntry(ActionLogTypes.FobUpdated,
            $"FOB \"{Fob.Name}\" for team {Fob.Team}, " +
            $"{EnumUtility.GetNameSafe(SupplyType)} supply updated because {EnumUtility.GetNameSafe(ChangeReason)} by " +
            $"{AmountDelta:0.##} to {(SupplyType == SupplyType.Ammo ? Fob.AmmoCount : Fob.BuildCount):0.##} supplies. ",
            Resupplier
        );
    }

    IFob IFobNeedsUIUpdateEvent.Fob => Fob;
}
