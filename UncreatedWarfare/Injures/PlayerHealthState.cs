namespace Uncreated.Warfare.Injures;

/// <summary>
/// Defines a player's state (injured/not injured).
/// </summary>
public enum PlayerHealthState
{
    /// <summary>
    /// Not injured or dead.
    /// </summary>
    Healthy,

    /// <summary>
    /// Injured, unable to stand up, slowly bleeding out.
    /// </summary>
    Injured,

    /// <summary>
    /// Dead awaiting respawn.
    /// </summary>
    Dead,

    /// <summary>
    /// Not connected to the server.
    /// </summary>
    Offline
}