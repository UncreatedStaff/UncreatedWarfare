using Uncreated.Warfare.Players.Saves;

namespace Uncreated.Warfare.Events.Models.Players;

internal class PlayerEarlyJoined : PlayerEvent
{
    /// <summary>
    /// If this is the first time the player has joined the server.
    /// </summary>
    public required bool IsNewPlayer { get; init; }

    /// <summary>
    /// Save data of the player, or a fresh save data object if they're new.
    /// </summary>
    public required BinaryPlayerSave SaveData { get; init; }
}
