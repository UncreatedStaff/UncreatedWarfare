namespace Uncreated.Warfare.Events.Models.Objects;

/// <summary>
/// Triggers after a <see cref="InteractableObjectQuest"/> or <see cref="InteractableObjectNote"/> gets interacted with.
/// </summary>
[EventModel(EventSynchronizationContext.Pure)]
public class QuestObjectInteracted : PlayerEvent
{
    /// <summary>
    /// The object that was interacted with.
    /// </summary>
    public required LevelObject Object { get; init; }
    
    /// <summary>
    /// The transform of the object.
    /// </summary>
    public required Transform Transform { get; init; }

    /// <summary>
    /// The <see cref="InteractableObjectQuest"/> or <see cref="InteractableObjectNote"/> that was interacted with.
    /// </summary>
    public required InteractableObject Interactable { get; init; }

    /// <summary>
    /// Coordinate of the object region in <see cref="LevelObjects.objects"/>.
    /// </summary>
    public required RegionCoord RegionPosition { get; init; }

    /// <summary>
    /// The index of the object in it's region.
    /// </summary>
    public required int ObjectIndex { get; init; }
}