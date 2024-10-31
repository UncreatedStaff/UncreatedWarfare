using System.Collections.Generic;
using Uncreated.Warfare.Util.List;

namespace Uncreated.Warfare.Players.Management;

/// <summary>
/// Implementation of <see cref="IPlayerService"/> with an empty player list. For use in external applications.
/// </summary>
public class NullPlayerService : IPlayerService
{
    /// <inheritdoc />
    public ReadOnlyTrackingList<WarfarePlayer> OnlinePlayers { get; } = new ReadOnlyTrackingList<WarfarePlayer>(new TrackingList<WarfarePlayer>(0));

    /// <inheritdoc />
    public IReadOnlyList<WarfarePlayer> GetThreadsafePlayerList()
    {
        return OnlinePlayers;
    }

    /// <inheritdoc />
    public WarfarePlayer GetOnlinePlayer(ulong steam64)
    {
        throw new PlayerOfflineException(steam64);
    }

    /// <inheritdoc />
    public WarfarePlayer? GetOnlinePlayerOrNull(ulong steam64)
    {
        return null;
    }

    /// <inheritdoc />
    public WarfarePlayer GetOnlinePlayerThreadSafe(ulong steam64)
    {
        throw new PlayerOfflineException(steam64);
    }

    /// <inheritdoc />
    public WarfarePlayer? GetOnlinePlayerOrNullThreadSafe(ulong steam64)
    {
        return null;
    }

    /// <inheritdoc />
    Task IPlayerService.TakePlayerConnectionLock(CancellationToken token)
    {
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    void IPlayerService.ReleasePlayerConnectionLock() { }

    /// <inheritdoc />
    public bool IsPlayerOnline(ulong steam64)
    {
        return false;
    }

    /// <inheritdoc />
    public bool IsPlayerOnlineThreadSafe(ulong steam64)
    {
        return false;
    }
}
