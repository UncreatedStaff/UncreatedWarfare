namespace Uncreated.Warfare.Events.Models.Players;

public class PlayerDropMarkerRequested : CancellablePlayerEvent
{
    /// <summary>
    /// The world position of the marker that the player is requesting to place.
    /// </summary>
    public required Vector3 MarkerWorldPosition { get; set; }

    /// <summary>
    /// The displayed text of the marker that the player is requesting to place.
    /// </summary>
    public required string MarkerDisplayText { get; set; }

    /// <summary>
    /// If the marker is being placed instead of cleared.
    /// </summary>
    public required bool IsNewMarkerBeingPlaced { get; set; }
}