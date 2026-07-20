using Uncreated.Warfare.FOBs;

namespace Uncreated.Warfare.Events.Models.Fobs;

public abstract class PlayerNearbyResourceFobEvent : PlayerEvent
{
    /// <summary>
    /// Base event for when a player walks in and out of a <see cref="IResourceFob"/>'s radius.
    /// </summary>
    public required IResourceFob Fob { get; init; }
}