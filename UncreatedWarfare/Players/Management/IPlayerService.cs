using System.Collections.Generic;
using Uncreated.Warfare.Util;
using Uncreated.Warfare.Util.List;

namespace Uncreated.Warfare.Players.Management;

/// <summary>
/// Handles keeping track of online <see cref="WarfarePlayer"/>'s.
/// </summary>
public interface IPlayerService
{
    /// <summary>
    /// List of all online players. Not to be accessed from any thread other than the game thread.
    /// </summary>
    /// <exception cref="GameThreadException"/>
    ReadOnlyTrackingList<WarfarePlayer> OnlinePlayers { get; }

    /// <summary>
    /// Gets a copy of the player list for working on non-game threads.
    /// </summary>
    IReadOnlyList<WarfarePlayer> GetThreadsafePlayerList();

    /// <summary>
    /// Get a player who's known to be online. Not to be invoked from any thread other than the game thread.
    /// </summary>
    /// <exception cref="GameThreadException"/>
    /// <exception cref="PlayerOfflineException"/>
    WarfarePlayer GetOnlinePlayer(ulong steam64);

    /// <summary>
    /// Get a player if they're online, otherwise <see langword="null"/>. Not to be invoked from any thread other than the game thread.
    /// </summary>
    /// <exception cref="GameThreadException"/>
    WarfarePlayer? GetOnlinePlayerOrNull(ulong steam64);

    /// <summary>
    /// Get a player who's known to be online.
    /// </summary>
    /// <exception cref="PlayerOfflineException"/>
    WarfarePlayer GetOnlinePlayerThreadSafe(ulong steam64);

    /// <summary>
    /// Get a player if they're online, otherwise <see langword="null"/>.
    /// </summary>
    WarfarePlayer? GetOnlinePlayerOrNullThreadSafe(ulong steam64);
}
