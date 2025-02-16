using System;

namespace Uncreated.Warfare.Kits;

/// <summary>
/// Stores information about a single kit access instance.
/// </summary>
public readonly struct KitAccessRow
{
    /// <summary>
    /// The player who has access.
    /// </summary>
    public CSteamID Player { get; }

    /// <summary>
    /// The reason they were given access.
    /// </summary>
    public KitAccessType Type { get; }

    /// <summary>
    /// When they were given access.
    /// </summary>
    public DateTimeOffset Timestamp { get; }

    internal KitAccessRow(CSteamID player, KitAccessType type, DateTimeOffset timestamp)
    {
        Player = player;
        Type = type;
        Timestamp = timestamp;
    }
}
