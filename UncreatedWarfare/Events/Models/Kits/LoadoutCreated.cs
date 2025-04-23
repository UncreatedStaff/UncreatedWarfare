namespace Uncreated.Warfare.Events.Models.Kits;

/// <summary>
/// Invoked after a new loadout kit is created.
/// </summary>
[EventModel(EventSynchronizationContext.Pure)]
public class LoadoutCreated : KitCreated
{
    /// <summary>
    /// The one-based ID of the loadout determining it's kit ID letter.
    /// </summary>
    public required int LoadoutId { get; init; }

    /// <summary>
    /// The player the loadout was created for.
    /// </summary>
    public required CSteamID Player { get; init; }
}