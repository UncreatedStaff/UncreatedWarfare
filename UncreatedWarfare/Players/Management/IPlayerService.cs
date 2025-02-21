using System;
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
    /// Create an <see cref="IPlayer"/> instance from a possibly offline player and fetch their usernames for display.
    /// </summary>
    ValueTask<IPlayer> CreateOfflinePlayerAsync(CSteamID steam64, CancellationToken token = default);

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

    /// <summary>
    /// Temporarily stop players from joining. Note that the lock will already be taken on startup and only has to be released whenever players are first allowed to join.
    /// </summary>
    /// <remarks>Call <see cref="ReleasePlayerConnectionLock"/> to allow players to join again.</remarks>
    Task TakePlayerConnectionLock(CancellationToken token);

    /// <summary>
    /// Re-allow players to join after calling <see cref="TakePlayerConnectionLock"/>.
    /// </summary>
    void ReleasePlayerConnectionLock();

    /// <summary>
    /// Quickly check if a player is online.
    /// </summary>
    /// <exception cref="GameThreadException"/>
    bool IsPlayerOnline(ulong steam64);

    /// <summary>
    /// Quickly check if a player is online.
    /// </summary>
    bool IsPlayerOnlineThreadSafe(ulong steam64);

    /// <summary>
    /// Subscribes to an instance event on a player.
    /// </summary>
    void SubscribeToPlayerEvent<TDelegate>(Action<WarfarePlayer, TDelegate> subscribe, TDelegate value) where TDelegate : MulticastDelegate;

    /// <summary>
    /// Unsubscribes from an instance event on a player.
    /// </summary>
    void UnsubscribeFromPlayerEvent<TDelegate>(Action<WarfarePlayer, TDelegate> unsubscribe, TDelegate value) where TDelegate : MulticastDelegate;
}