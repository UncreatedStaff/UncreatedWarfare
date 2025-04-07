using Steamworks;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Uncreated.Warfare.Players;
using Uncreated.Warfare.Players.Management;
using Uncreated.Warfare.Util.List;

namespace Uncreated.Warfare.Tests.Utility;

internal class TestPlayerService : IPlayerService, IDisposable
{
    private readonly SemaphoreSlim _connectionLock = new SemaphoreSlim(1, 1);

    public Dictionary<ulong, WarfarePlayer> Players { get; set; } = new Dictionary<ulong, WarfarePlayer>();

    /// <inheritdoc />
    public ReadOnlyTrackingList<WarfarePlayer> OnlinePlayers => Players.Values.ToReadOnlyTrackingList();

    /// <inheritdoc />
    public ValueTask<IPlayer> CreateOfflinePlayerAsync(CSteamID steam64, CancellationToken token = default)
    {
        return new ValueTask<IPlayer>(new OfflinePlayer(steam64));
    }

    /// <inheritdoc />
    public IReadOnlyList<WarfarePlayer> GetThreadsafePlayerList()
    {
        return Players.Values.ToList();
    }

    /// <inheritdoc />
    public WarfarePlayer GetOnlinePlayer(ulong steam64)
    {
        return Players[steam64];
    }

    /// <inheritdoc />
    public WarfarePlayer GetOnlinePlayerOrNull(ulong steam64)
    {
        Players.TryGetValue(steam64, out WarfarePlayer player);
        return player;
    }

    /// <inheritdoc />
    public WarfarePlayer GetOnlinePlayerThreadSafe(ulong steam64)
    {
        return GetOnlinePlayer(steam64);
    }

    /// <inheritdoc />
    public WarfarePlayer GetOnlinePlayerOrNullThreadSafe(ulong steam64)
    {
        return GetOnlinePlayerOrNull(steam64);
    }

    /// <inheritdoc />
    public Task TakePlayerConnectionLock(CancellationToken token)
    {
        return _connectionLock.WaitAsync(token);
    }

    /// <inheritdoc />
    public void ReleasePlayerConnectionLock()
    {
        _connectionLock.Release();
    }

    /// <inheritdoc />
    public bool IsPlayerOnline(ulong steam64)
    {
        return Players.ContainsKey(steam64);
    }

    /// <inheritdoc />
    public bool IsPlayerOnlineThreadSafe(ulong steam64)
    {
        return IsPlayerOnline(steam64);
    }

    /// <inheritdoc />
    public void SubscribeToPlayerEvent<TDelegate>(Action<WarfarePlayer, TDelegate> subscribe, TDelegate value)
        where TDelegate : MulticastDelegate
    { }

    /// <inheritdoc />
    public void UnsubscribeFromPlayerEvent<TDelegate>(Action<WarfarePlayer, TDelegate> unsubscribe, TDelegate value)
        where TDelegate : MulticastDelegate
    { }

    public async Task AddPlayer(WarfarePlayer player, CancellationToken token = default)
    {
        await _connectionLock.WaitAsync(token);
        try
        {
            Players = new Dictionary<ulong, WarfarePlayer>(Players)
            {
                { player.Steam64.m_SteamID, player }
            };
        }
        finally
        {
            _connectionLock.Release();
        }
    }

    /// <inheritdoc />
    public void Dispose()
    {
        _connectionLock.Dispose();
    }
}
