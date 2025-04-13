using DanielWillett.ModularRpcs.Abstractions;
using DanielWillett.ModularRpcs.Annotations;
using DanielWillett.ModularRpcs.Async;
using DanielWillett.ModularRpcs.Serialization;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;
using Uncreated.Warfare.Events;
using Uncreated.Warfare.Events.Models;
using Uncreated.Warfare.Events.Models.Players;
using Uncreated.Warfare.Layouts;
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
public class RemotePlayerListService :
    IHostedService,
    ILayoutHostedService,
    IAsyncEventListener<PlayerJoined>,
    IAsyncEventListener<PlayerLeft>,
    IEventListener<HomebaseConnected>
{
    private readonly IPlayerService? _playerService;
    private readonly IUserDataService? _userDataService;
    private readonly WarfareModule? _warfare;
    private readonly ILogger<RemotePlayerListService> _logger;
    private ReplicatedServerState _replicatedServerStateWarfareOnly;

    // these events are meant to be used from the discord bot

    /// <summary>
    /// Invoked when a player connects to the server.
    /// </summary>
    public event Action<ReplicatedPlayerListEntry, IModularRpcRemoteConnection>? OnPlayerConnected;

    /// <summary>
    /// Invoked when a player disconnects from the server.
    /// </summary>
    public event Action<ReplicatedPlayerListEntry, IModularRpcRemoteConnection>? OnPlayerDisconnected;

    /// <summary>
    /// Invoked when the entire player list is refreshed (occasionally to prevent desync).
    /// </summary>
    public event Action<ReplicatedPlayerListEntry[], IModularRpcRemoteConnection>? OnPlayerListRefreshed;

    /// <summary>
    /// Invoked when the server's state updated
    /// </summary>
    public event Action<ReplicatedServerState, IModularRpcRemoteConnection>? OnStateUpdated;

    public RemotePlayerListService(IServiceProvider serviceProvider)
    {
        if (WarfareModule.IsActive)
        {
            // these are only needed on the server
            _playerService = serviceProvider.GetRequiredService<IPlayerService>();
            _userDataService = serviceProvider.GetRequiredService<IUserDataService>();
            _warfare = serviceProvider.GetRequiredService<WarfareModule>();
        }

        _logger = serviceProvider.GetRequiredService<ILogger<RemotePlayerListService>>();
    }

    public async UniTask StopAsync(CancellationToken token)
    {
        await UpdateReplicatedServerState(ServerStateType.Shutdown, null);
    }

    public async UniTask StartAsync(CancellationToken token)
    {
        await UpdateReplicatedServerState(ServerStateType.Loading, Provider.serverName);
    }

    async UniTask ILayoutHostedService.StartAsync(CancellationToken token)
    {
        Layout? layout = _warfare != null && _warfare.IsLayoutActive() ? _warfare.GetActiveLayout() : null;
        if (layout != null)
        {
            await UpdateReplicatedServerState(ServerStateType.Active, layout.LayoutInfo.DisplayName, false);
        }
        else if (_replicatedServerStateWarfareOnly.Type != ServerStateType.Shutdown)
        {
            await UpdateReplicatedServerState(ServerStateType.Loading, Provider.serverName, false);
        }

        // ILayoutHostedService already has the player connection lock, need to separate the unlocked version of the function
        await SendCurrentPlayerListIntl(token).AsUniTask();
    }

    async UniTask ILayoutHostedService.StopAsync(CancellationToken token)
    {
        await UpdateReplicatedServerState(ServerStateType.Loading, null);
    }

    internal async Task UpdateReplicatedServerState(ServerStateType type, string? description, bool send = true)
    {
        ReplicatedServerState state = new ReplicatedServerState(type, description, DateTimeOffset.UtcNow, Provider.maxPlayers);
        _replicatedServerStateWarfareOnly = state;
        if (!send)
            return;

        try
        {
            await SendServerStateUpdated(state).IgnoreNoConnections();
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, $"Error sending player list state update: {state.Type} (desc: {state.Description}).");
        }
    }

    [RpcReceive]
    private void ReceiveServerState(ReplicatedServerState state, IModularRpcRemoteConnection connection)
    {
        OnStateUpdated?.Invoke(state, connection);
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

        ReplicatedPlayerListEntry[] entries = new ReplicatedPlayerListEntry[list.Count];
        int index = 0;
        foreach (WarfarePlayer player in list)
        {
            entries[index] = new ReplicatedPlayerListEntry(player.Steam64.m_SteamID, player.Names.PlayerName,
                player.Names.CharacterName, player.Names.NickName, discordIds[index], player.Team.Faction.PrimaryKey, player.IsOnDuty);

            ++index;
        }

        try
        {
            await SendPlayerList(_replicatedServerStateWarfareOnly, entries).IgnoreNoConnections();
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
    private void ReceivePlayerConnected(ReplicatedPlayerListEntry player, IModularRpcRemoteConnection connection)
    {
        try
        {
            OnPlayerConnected?.Invoke(player, connection);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error invoking OnPlayerConnected.");
        }
    }

    [RpcReceive]
    private void ReceivePlayerDisconnected(ReplicatedPlayerListEntry player, IModularRpcRemoteConnection connection)
    {
        try
        {
            OnPlayerDisconnected?.Invoke(player, connection);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error invoking OnPlayerDisconnected.");
        }
    }

    [RpcReceive]
    private void ReceivePlayerList(ReplicatedServerState state, ReplicatedPlayerListEntry[] playerList, IModularRpcRemoteConnection connection)
    {
        try
        {
            OnStateUpdated?.Invoke(state, connection);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error invoking OnPlayerListRefreshed.");
        }
        try
        {
            OnPlayerListRefreshed?.Invoke(playerList, connection);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error invoking OnPlayerListRefreshed.");
        }
    }

    [RpcSend(nameof(ReceiveServerState)), RpcTimeout(Timeouts.Seconds * 3)]
    protected virtual RpcTask SendServerStateUpdated(ReplicatedServerState state) => RpcTask.NotImplemented;

    [RpcSend(nameof(ReceivePlayerConnected)), RpcTimeout(Timeouts.Seconds * 3)]
    protected virtual RpcTask SendPlayerConnected(ReplicatedPlayerListEntry player) => RpcTask.NotImplemented;

    [RpcSend(nameof(ReceivePlayerDisconnected)), RpcTimeout(Timeouts.Seconds * 3)]
    protected virtual RpcTask SendPlayerDisconnected(ReplicatedPlayerListEntry player) => RpcTask.NotImplemented;

    [RpcSend(nameof(ReceivePlayerList)), RpcTimeout(Timeouts.Seconds * 5)]
    protected virtual RpcTask SendPlayerList(ReplicatedServerState state, ReplicatedPlayerListEntry[] playerList)
    {
        return _ = RpcTask.NotImplemented;
    }

    [EventListener(Priority = int.MinValue)]
    async UniTask IAsyncEventListener<PlayerJoined>.HandleEventAsync(PlayerJoined e, IServiceProvider serviceProvider, CancellationToken token)
    {
        WarfarePlayer player = e.Player;
        ulong discordId = await _userDataService!.GetDiscordIdAsync(player.Steam64.m_SteamID, token).ConfigureAwait(false);
        await SendPlayerConnected(
            new ReplicatedPlayerListEntry(player.Steam64.m_SteamID, player.Names.PlayerName, player.Names.CharacterName, player.Names.NickName, discordId, e.Player.Team.Faction.PrimaryKey, e.Player.IsOnDuty)
        ).IgnoreNoConnections();
    }
    
    [EventListener(Priority = int.MinValue)]
    async UniTask IAsyncEventListener<PlayerLeft>.HandleEventAsync(PlayerLeft e, IServiceProvider serviceProvider, CancellationToken token)
    {
        WarfarePlayer player = e.Player;
        ulong discordId = await _userDataService!.GetDiscordIdAsync(player.Steam64.m_SteamID, token).ConfigureAwait(false);
        await SendPlayerDisconnected(
            new ReplicatedPlayerListEntry(player.Steam64.m_SteamID, player.Names.PlayerName, player.Names.CharacterName, player.Names.NickName, discordId, e.Player.Team.Faction.PrimaryKey, e.Player.IsOnDuty)
        ).IgnoreNoConnections();
    }

    void IEventListener<HomebaseConnected>.HandleEvent(HomebaseConnected e, IServiceProvider serviceProvider)
    {
        Task.Run(async () =>
        {
            try
            {
                await SendCurrentPlayerList(CancellationToken.None);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending player list.");
            }
        });
    }
}

[RpcSerializable(8 + 8 + 4 + 1 + SerializationHelper.MinimumStringSize * 3, isFixedSize: false)]
public record struct ReplicatedPlayerListEntry(ulong Steam64, string PlayerName, string CharacterName, string NickName, ulong DiscordId, uint Faction, bool IsOnDuty) : IRpcSerializable
{
    /// <inheritdoc />
    public int GetSize(IRpcSerializer serializer)
    {
        return 8 + 8 + 4 + 1 + serializer.GetSize(PlayerName) + serializer.GetSize(CharacterName) + serializer.GetSize(NickName);
    }

    /// <inheritdoc />
    public int Write(Span<byte> writeTo, IRpcSerializer serializer)
    {
        int index = 0;
        index += serializer.WriteObject(Steam64, writeTo);
        index += serializer.WriteObject(PlayerName, writeTo[index..]);
        index += serializer.WriteObject(CharacterName, writeTo[index..]);
        index += serializer.WriteObject(NickName, writeTo[index..]);
        index += serializer.WriteObject(DiscordId, writeTo[index..]);
        index += serializer.WriteObject(Faction, writeTo[index..]);
        index += serializer.WriteObject(IsOnDuty, writeTo[index..]);
        return index;
    }

    /// <inheritdoc />
    public int Read(Span<byte> readFrom, IRpcSerializer serializer)
    {
        int index = 0;
        
        Steam64 = serializer.ReadObject<ulong>(readFrom, out int bytesRead);
        index += bytesRead;

        PlayerName = serializer.ReadObject<string>(readFrom[index..], out bytesRead) ?? string.Empty;
        index += bytesRead;

        CharacterName = serializer.ReadObject<string>(readFrom[index..], out bytesRead) ?? string.Empty;
        index += bytesRead;

        NickName = serializer.ReadObject<string>(readFrom[index..], out bytesRead) ?? string.Empty;
        index += bytesRead;

        DiscordId = serializer.ReadObject<ulong>(readFrom[index..], out bytesRead);
        index += bytesRead;

        Faction = serializer.ReadObject<uint>(readFrom[index..], out bytesRead);
        index += bytesRead;

        IsOnDuty = serializer.ReadObject<bool>(readFrom[index..], out bytesRead);
        index += bytesRead;

        return index;
    }
}

[RpcSerializable(1 + 1 + SerializationHelper.MinimumStringSize * 2, isFixedSize: false)]
public record struct ReplicatedServerState(ServerStateType Type, string? Description, DateTimeOffset StartTime, byte MaxPlayers) : IRpcSerializable
{
    /// <inheritdoc />
    public int GetSize(IRpcSerializer serializer)
    {
        return 1 + 1 + serializer.GetSize(Description) + serializer.GetSize(StartTime);
    }

    /// <inheritdoc />
    public int Write(Span<byte> writeTo, IRpcSerializer serializer)
    {
        int index = 0;
        index += serializer.WriteObject(Type, writeTo);
        index += serializer.WriteObject(Description, writeTo[index..]);
        index += serializer.WriteObject(StartTime, writeTo[index..]);
        index += serializer.WriteObject(MaxPlayers, writeTo[index..]);
        return index;
    }

    /// <inheritdoc />
    public int Read(Span<byte> readFrom, IRpcSerializer serializer)
    {
        int index = 0;

        Type = serializer.ReadObject<ServerStateType>(readFrom, out int bytesRead);
        index += bytesRead;

        Description = serializer.ReadObject<string>(readFrom[index..], out bytesRead) ?? string.Empty;
        index += bytesRead;

        StartTime = serializer.ReadObject<DateTimeOffset>(readFrom[index..], out bytesRead);
        index += bytesRead;

        MaxPlayers = serializer.ReadObject<byte>(readFrom[index..], out bytesRead);
        index += bytesRead;

        return index;
    }
}

public enum ServerStateType : byte
{
    Unknown,
    Shutdown,
    Loading,
    Active
}