namespace Uncreated.Warfare.Events.Models.Throwables;

[EventModel(EventSynchronizationContext.Pure)]
public class ThrowableSpawned : PlayerEvent
{
    public required UseableThrowable UseableThrowable { get; init; }
    public required GameObject Object { get; init; }
    public ItemThrowableAsset Asset => UseableThrowable.equippedThrowableAsset;
}