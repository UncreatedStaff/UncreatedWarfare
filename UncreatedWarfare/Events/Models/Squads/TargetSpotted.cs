using Uncreated.Warfare.Layouts.Teams;
using Uncreated.Warfare.Players;
using Uncreated.Warfare.Squads.Spotted;

namespace Uncreated.Warfare.Events.Models.Squads;

/// <summary>
/// Invoked when an object starts getting spotted.
/// </summary>
[EventModel(EventSynchronizationContext.Pure)]
public class TargetSpotted
{
    /// <summary>
    /// The object that spotted <see cref="Target"/>. This is usually a <see cref="WarfarePlayer"/>, in which case <see cref="Player"/> will be the same.
    /// </summary>
    public required ISpotter Spotter { get; init; }

    /// <summary>
    /// The player that spotted <see cref="Target"/>, if it was a player.
    /// </summary>
    public required WarfarePlayer? Player { get; init; }

    /// <summary>
    /// The object that was spotted.
    /// </summary>
    public required SpottableObjectComponent Target { get; init; }
    
    /// <summary>
    /// The team that spotted <see cref="Target"/>.
    /// </summary>
    public required Team Team { get; init; }

    /// <summary>
    /// The team that <see cref="Target"/> belongs to.
    /// </summary>
    public required Team TargetTeam { get; init; }
    
    /// <summary>
    /// Friendly name of the <see cref="Target"/>.
    /// </summary>
    public required string TargetName { get; init; }
}