using DanielWillett.ModularRpcs.Abstractions;
using DanielWillett.ModularRpcs.Annotations;
using DanielWillett.ModularRpcs.Async;
using DanielWillett.ModularRpcs.Exceptions;
using DanielWillett.ModularRpcs.Reflection;
using DanielWillett.SpeedBytes;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;
using Uncreated.Warfare.Events;
using Uncreated.Warfare.Events.Models;
using Uncreated.Warfare.Events.Models.Players;
using Uncreated.Warfare.Networking;
using Uncreated.Warfare.Players;
using Uncreated.Warfare.Players.Management;
using Uncreated.Warfare.Services;

namespace Uncreated.Warfare.Discord;

/// <summary>
/// Handles replicating the server's player list and startup state (shutdown, startup, etc.)
/// </summary>
/// <remarks>This class is meant to be used on both the server and the discord bot.</remarks>
[RpcClass]
public class RemotePlayerListService : ILayoutHostedService, IAsyncEventListener<PlayerJoined>, IAsyncEventListener<PlayerLeft>, IAsyncEventListener<HomebaseConnected>
{
    private readonly IPlayerService? _playerService;
    private readonly IUserDataService? _userDataService;
    private readonly ILogger<RemotePlayerListService> _logger;

    private ReplicatedServerState _replicatedServerState;

    /// <summary>
    /// State declaring the status of the server.
    /// </summary>
    public ReplicatedServerState ReplicatedServerState => _replicatedServerState;

    // these events are meant to be used from the discord bot

    /// <summary>
    /// Invoked when a player connects to the server.
    /// </summary>
    public event Action<ReplicatedPlayerListEntry>? OnPlayerConnected;

    /// <summary>
    /// Invoked when a player disconnects from the server.
    /// </summary>
    public event Action<ReplicatedPlayerListEntry>? OnPlayerDisconnected;

    /// <summary>
    /// Invoked when the entire player list is refreshed (occasionally to prevent desync).
    /// </summary>
    public event Action<ReplicatedPlayerListEntry[]>? OnPlayerListRefreshed;

    /// <summary>
    /// Invoked when the server's state updated
    /// </summary>
    public event Action<ReplicatedServerState, ReplicatedServerState>? OnStateUpdated;

    public RemotePlayerListService(IServiceProvider serviceProvider)
    {
        if (WarfareModule.IsActive)
        {
            // these are only needed on the server
            _playerService = serviceProvider.GetRequiredService<IPlayerService>();
            _userDataService = serviceProvider.GetRequiredService<IUserDataService>();
        }

        _logger = serviceProvider.GetRequiredService<ILogger<RemotePlayerListService>>();
    }

    UniTask ILayoutHostedService.StartAsync(CancellationToken token)
    {
        // ILayoutHostedService already has the player connection lock, need to separate the unlocked version of the function
        return SendCurrentPlayerListIntl(token).AsUniTask();
    }

    UniTask ILayoutHostedService.StopAsync(CancellationToken token)
    {
        return UniTask.CompletedTask;
    }

    internal Task UpdateReplicatedServerState()
    {
        return UpdateReplicatedServerState(_replicatedServerState.Type, _replicatedServerState.Description);
    }

    internal async Task UpdateReplicatedServerState(ServerStateType type, string? description)
    {
        if (type != _replicatedServerState.Type || !string.Equals(description, _replicatedServerState.Description, StringComparison.Ordinal))
        {
            _replicatedServerState = new ReplicatedServerState(type, description, DateTimeOffset.UtcNow);
        }

        try
        {
            await SendServerStateUpdated(_replicatedServerState.Type, _replicatedServerState.Description, _replicatedServerState.StartTime).IgnoreNoConnections();
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, $"Error sending player list state update: {_replicatedServerState.Type} (desc: {_replicatedServerState.Description}).");
        }
    }

    [RpcReceive]
    private void ReceiveServerState(ServerStateType type, string? description, DateTimeOffset time)
    {
        ReplicatedServerState state = new ReplicatedServerState(type, description, time);
        if (state.Equals(_replicatedServerState))
            return;

        ReplicatedServerState oldState = _replicatedServerState;
        _replicatedServerState = state;
        OnStateUpdated?.Invoke(oldState, state);
    }

    /// <summary>
    /// Send the current player list to homebase.
    /// </summary>
    [RpcReceive]
    internal async Task SendCurrentPlayerList(CancellationToken token = default)
    {
        await _playerService!.TakePlayerConnectionLock(token);
        try
        {
            await SendCurrentPlayerListIntl(token);
        }
        finally
        {
            _playerService.ReleasePlayerConnectionLock();
        }
    }

    private async Task SendCurrentPlayerListIntl(CancellationToken token)
    {
        IReadOnlyList<WarfarePlayer> list = _playerService!.GetThreadsafePlayerList();

        ulong[] discordIds = await _userDataService!.GetDiscordIdsAsync(list.Select(x => x.Steam64.m_SteamID).ToList(), token).ConfigureAwait(false);

        ByteWriter writer = new ByteWriter();

        int size = ProxyGenerator.Instance.CalculateOverheadSize(SendPlayerList, out _);
        if (size == -1)
        {
            _logger.LogError("SendPlayerList not registered.");
            return;
        }

        writer.WriteBlock(0, size);

        writer.Write(_replicatedServerState.Type);
        writer.WriteNullable(_replicatedServerState.Description);
        writer.Write(_replicatedServerState.StartTime);

        writer.Write(list.Count);
        int index = 0;
        foreach (WarfarePlayer player in list)
        {
            writer.Write(player.Steam64.m_SteamID);
            writer.Write(player.Names.PlayerName);
            writer.Write(player.Names.CharacterName);
            writer.Write(player.Names.NickName);
            writer.Write(discordIds[index++]);
        }

        try
        {
            await SendPlayerList(writer, true, token).IgnoreNoConnections();
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, $"Error sending player list of {list.Count} players.");
        }
    }

    /// <summary>
    /// Requests a player list from the remote party (the server).
    /// </summary>
    [RpcSend(nameof(SendCurrentPlayerList)), RpcFireAndForget]
    public virtual void SendPlayerListRequest(IModularRpcRemoteConnection connection)
    {
        _ = RpcTask.NotImplemented;
    }

    [RpcReceive]
    private void ReceivePlayerConnected(ulong steam64, string playerName, string characterName, string nickName, ulong discordId)
    {
        try
        {
            OnPlayerConnected?.Invoke(new ReplicatedPlayerListEntry(steam64, playerName, characterName, nickName, discordId));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error invoking OnPlayerConnected.");
        }
    }

    [RpcReceive]
    private void ReceivePlayerDisconnected(ulong steam64, string playerName, string characterName, string nickName, ulong discordId)
    {
        try
        {
            OnPlayerDisconnected?.Invoke(new ReplicatedPlayerListEntry(steam64, playerName, characterName, nickName, discordId));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error invoking OnPlayerDisconnected.");
        }
    }

    [RpcReceive(Raw = true)]
    private void ReceivePlayerList(ArraySegment<byte> data)
    {
        if (OnPlayerListRefreshed == null)
            return;

        ByteReader reader = new ByteReader();
        reader.LoadNew(data);

        ReplicatedServerState state = new ReplicatedServerState(reader.ReadEnum<ServerStateType>(), reader.ReadNullableString(), reader.ReadDateTimeOffset());

        if (!state.Equals(_replicatedServerState))
        {
            ReplicatedServerState oldState = _replicatedServerState;
            _replicatedServerState = state;
            OnStateUpdated?.Invoke(oldState, state);
        }

        int ct = reader.ReadInt32();
        if (ct > byte.MaxValue)
            throw new RpcParseException("Invalid player count amount, expected 0-255.") { ErrorCode = 10 };

        ReplicatedPlayerListEntry[] entries = new ReplicatedPlayerListEntry[ct];

        for (int i = 0; i < ct; ++i)
        {
            entries[i] = new ReplicatedPlayerListEntry(reader.ReadUInt64(), reader.ReadString(), reader.ReadString(), reader.ReadString(), reader.ReadUInt64());
        }

        try
        {
            OnPlayerListRefreshed?.Invoke(entries);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error invoking OnPlayerListRefreshed.");
        }
    }

    [RpcSend(nameof(ReceiveServerState)), RpcTimeout(Timeouts.Seconds * 3)]
    protected virtual RpcTask SendServerStateUpdated(ServerStateType stateType, string? description, DateTimeOffset startTime) => RpcTask.NotImplemented;

    [RpcSend(nameof(ReceivePlayerConnected)), RpcTimeout(Timeouts.Seconds * 3)]
    protected virtual RpcTask SendPlayerConnected(ulong steam64, string playerName, string characterName, string nickName, ulong discordId) => RpcTask.NotImplemented;

    [RpcSend(nameof(ReceivePlayerDisconnected)), RpcTimeout(Timeouts.Seconds * 3)]
    protected virtual RpcTask SendPlayerDisconnected(ulong steam64, string playerName, string characterName, string nickName, ulong discordId) => RpcTask.NotImplemented;

    [RpcSend(nameof(ReceivePlayerList), Raw = true), RpcTimeout(Timeouts.Seconds * 5)]
    protected virtual RpcTask SendPlayerList(ByteWriter writer, bool canTakeOwnership, CancellationToken token = default)
    {
        return _ = RpcTask.NotImplemented;
    }

    [EventListener(Priority = int.MinValue)]
    async UniTask IAsyncEventListener<PlayerJoined>.HandleEventAsync(PlayerJoined e, IServiceProvider serviceProvider, CancellationToken token)
    {
        WarfarePlayer player = e.Player;
        ulong discordId = await _userDataService!.GetDiscordIdAsync(player.Steam64.m_SteamID, token);
        SendPlayerConnected(player.Steam64.m_SteamID, player.Names.PlayerName, player.Names.CharacterName, player.Names.NickName, discordId);
    }
    
    [EventListener(Priority = int.MinValue)]
    async UniTask IAsyncEventListener<PlayerLeft>.HandleEventAsync(PlayerLeft e, IServiceProvider serviceProvider, CancellationToken token)
    {
        WarfarePlayer player = e.Player;
        ulong discordId = await _userDataService!.GetDiscordIdAsync(player.Steam64.m_SteamID, token);
        SendPlayerDisconnected(player.Steam64.m_SteamID, player.Names.PlayerName, player.Names.CharacterName, player.Names.NickName, discordId);
    }

    async UniTask IAsyncEventListener<HomebaseConnected>.HandleEventAsync(HomebaseConnected e, IServiceProvider serviceProvider, CancellationToken token)
    {
        await SendCurrentPlayerList(token);
    }
}

public record struct ReplicatedPlayerListEntry(ulong Steam64, string PlayerName, string CharacterName, string NickName, ulong DiscordId);

public record struct ReplicatedServerState(ServerStateType Type, string? Description, DateTimeOffset StartTime);

public enum ServerStateType : byte
{
    Unknown,
    Shutdown,
    Loading,
    Active,
    PendingShutdownTime,
    PendingShutdownAfterGame
}