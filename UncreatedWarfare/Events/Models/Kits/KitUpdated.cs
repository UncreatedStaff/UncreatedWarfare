using Uncreated.Warfare.Kits;

namespace Uncreated.Warfare.Events.Models.Kits;

/// <summary>
/// Invoked after a kit is changed.
/// </summary>
[EventModel(EventSynchronizationContext.Pure)]
public class KitUpdated
{
    /// <summary>
    /// The kit that was updated.
    /// </summary>
    public required Kit Kit { get; init; }
}