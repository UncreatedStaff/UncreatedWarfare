namespace Uncreated.Warfare.Events.Models.Items;

[EventModel(EventSynchronizationContext.PerPlayer, SynchronizedModelTags = [ "modify_inventory", "modify_useable" ])]
public class CraftItemRequested : CancellablePlayerEvent
{
    // ReSharper disable once ReplaceWithFieldKeyword

    /// <summary>
    /// The original blueprint that is being crafted.
    /// </summary>
    public required Blueprint OriginalBlueprint { get; init; }

    /// <summary>
    /// The blueprint that is being crafted.
    /// </summary>
    /// <remarks>This can be changed.</remarks>
    public required Blueprint Blueprint { get; set; }

    /// <summary>
    /// The original setting of whether or not as many items as possible should be crafted instead of just one.
    /// </summary>
    public required bool OriginalCraftAll { get; set; }

    /// <summary>
    /// If as many items as possible should be crafted instead of just one.
    /// </summary>
    /// <remarks>This can be changed.</remarks>
    public required bool CraftAll { get; set; }
}