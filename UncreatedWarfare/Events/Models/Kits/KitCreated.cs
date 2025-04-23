using Uncreated.Warfare.Kits;

namespace Uncreated.Warfare.Events.Models.Kits;

/// <summary>
/// Invoked after a kit is created.
/// </summary>
[EventModel(EventSynchronizationContext.Pure)]
public class KitCreated
{
    /// <summary>
    /// The newly created kit.
    /// </summary>
    public required Kit Kit { get; init; }
}