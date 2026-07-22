using Uncreated.Warfare.FOBs;

namespace Uncreated.Warfare.Events.Models.Fobs;

/// <summary>
/// Event listener args which fires after a player enters the radius of a friendly <see cref="IResourceFob"/>.
/// </summary>
[EventModel(EventSynchronizationContext.Pure)]
public class PlayerEnteredFriendlyFob : PlayerNearbyResourceFobEvent
{

}