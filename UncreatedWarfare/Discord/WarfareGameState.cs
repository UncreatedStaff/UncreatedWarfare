using DanielWillett.ModularRpcs.Abstractions;
using DanielWillett.ModularRpcs.Annotations;
using DanielWillett.ModularRpcs.Async;
using DanielWillett.ModularRpcs.Routing;
using DanielWillett.ModularRpcs.Serialization;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using Uncreated.Warfare.Events.Models;
using Uncreated.Warfare.Events.Models.Squads;
using Uncreated.Warfare.Layouts;
using Uncreated.Warfare.Layouts.Phases;
using Uncreated.Warfare.Layouts.Teams;
using Uncreated.Warfare.Networking;
using Uncreated.Warfare.Quests.Daily;
using Uncreated.Warfare.Services;
using Uncreated.Warfare.Squads;
using Uncreated.Warfare.Stats;

namespace Uncreated.Warfare.Discord;

[RpcClass]
public class WarfareGameStateService :
    IEventListener<SquadUpdated>,
    IEventListener<DailyQuestsUpdated>,
    IEventListener<HomebaseConnected>,
    ILayoutHostedService,
    ILayoutPhaseListener<ILayoutPhase>
{
    private readonly WarfareModule? _warfare;
    private readonly IRpcConnectionLifetime? _connectionLifetime;
    private readonly ILogger<WarfareGameStateService> _logger;
    private readonly object _sync = new object();

    public bool HasInformation { get; set; }

    public string? PublicIP { get; set; }
    public ushort Port { get; set; }
    public string? BookmarkHost { get; set; }
    public TeamInfo[]? ActiveFactions { get; set; }
    public string? LayoutName { get; set; }
    public string? Phase { get; set; }
    public ulong GameId { get; set; }
    public DateTime LayoutStartTime { get; set; }
    public WarfareRank[]? Ranks { get; set; }
    public DailyQuestDay?[]? DailyQuests { get; set; }
    public List<SquadInfo>? Squads { get; set; }

    public event Action? OnLayoutUpdated;
    public event Action<int>? OnSquadsUpdated;
    public event Action? OnDailyQuestsUpdated;

    public WarfareGameStateService(IServiceProvider serviceProvider, ILogger<WarfareGameStateService> logger)
    {
        if (WarfareModule.IsActive)
        {
            _warfare = serviceProvider.GetRequiredService<WarfareModule>();
        }
        else
        {
            _connectionLifetime = serviceProvider.GetRequiredService<IRpcConnectionLifetime>();
        }
        _logger = logger;
    }

    private void SendFullState(bool send = true)
    {
        if (_warfare == null || !_warfare.IsLayoutActive())
            return;

        HasInformation = true;

        Layout layout = _warfare.GetActiveLayout();
        GameId = layout.LayoutId;
        LayoutName = layout.LayoutInfo.DisplayName;
        LayoutStartTime = layout.LayoutStats.StartTimestamp.UtcDateTime;

        Phase = layout.ActivePhase?.Name;

        Ranks = layout.ServiceProvider.ResolveOptional<PointsService>()?.Ranks.ToArray();

        PublicIP = SteamGameServer.GetPublicIP().ToString();
        Port = Provider.port;
        BookmarkHost = Provider.configData.Browser.BookmarkHost;

        ITeamManager<Team>? teamManager = layout.ServiceProvider.ResolveOptional<ITeamManager<Team>>();
        ActiveFactions = teamManager?.AllTeams.Select(x => new TeamInfo
        {
            PrimaryKey = x.Faction.PrimaryKey,
            Role = teamManager.GetLayoutRole(x)
        }).ToArray();

        DailyQuestService? questService = layout.ServiceProvider.ResolveOptional<DailyQuestService>();
        DailyQuests = questService?.Days;
        
        SquadManager? squadManager = layout.ServiceProvider.ResolveOptional<SquadManager>();
        if (squadManager != null)
        {
            List<SquadInfo> squads = new List<SquadInfo>(squadManager.Squads.Count);
            foreach (Squad squad in squadManager.Squads)
            {
                squads.Add(new SquadInfo
                {
                    Faction = squad.Team.Faction.PrimaryKey,
                    IdentificationNumber = squad.TeamIdentificationNumber,
                    Locked = squad.IsLocked,
                    Members = squad.GetMemberList(),
                    Name = squad.Name
                });
            }

            Squads = squads;
        }
        else
        {
            Squads = null;
        }

        InvokeLayoutUpdated();
        InvokeDailyQuestsUpdated();
        InvokeSquadsUpdated();

        if (!send)
            return;

        Task.Run(async () =>
        {
            try
            {
                await SendFullStateRpc(GameId, PublicIP, Port, BookmarkHost, ActiveFactions, LayoutName, Phase, LayoutStartTime, Ranks, DailyQuests, Squads);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending full game state.");
            }
        });
    }

    /// <inheritdoc />
    public UniTask OnPhaseStarted(ILayoutPhase phase, CancellationToken token)
    {
        Phase = phase.Name;
        try
        {
            SendPhaseUpdated(GameId, Phase);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending phase update.");
        }
        return UniTask.CompletedTask;
    }

    /// <inheritdoc />
    public UniTask OnPhaseEnded(ILayoutPhase phase, CancellationToken token)
    {
        Phase = null;
        try
        {
            SendPhaseUpdated(GameId, Phase);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending phase update.");
        }
        return UniTask.CompletedTask;
    }

    [RpcSend(nameof(ReceiveFullGameState))]
    protected virtual RpcTask SendFullStateRpc(
        ulong gameId,
        string? publicIP,
        ushort port,
        string? bookmarkHost,
        TeamInfo[]? activeFactions,
        string? layoutName,
        string? phase,
        DateTime layoutStartTime,
        WarfareRank[]? ranks,
        DailyQuestDay?[]? dailyQuests,
        List<SquadInfo>? squads)
    {
        return RpcTask.NotImplemented;
    }

    [RpcReceive]
    private void ReceiveFullGameState(
        ulong gameId,
        string? publicIP,
        ushort port,
        string? bookmarkHost,
        TeamInfo[]? activeFactions,
        string? layoutName,
        string? phase,
        DateTime layoutStartTime,
        WarfareRank[]? ranks,
        DailyQuestDay?[]? dailyQuests,
        List<SquadInfo>? squads)
    {
        lock (_sync)
        {
            PublicIP = publicIP;
            Port = port;
            BookmarkHost = bookmarkHost;

            GameId = gameId;
            ActiveFactions = activeFactions;
            LayoutName = layoutName;
            Phase = phase;
            LayoutStartTime = layoutStartTime;
            Ranks = ranks;

            DailyQuests = dailyQuests;

            Squads = squads;

            InvokeLayoutUpdated();
            InvokeDailyQuestsUpdated();
            InvokeSquadsUpdated();
        }
    }


    private void CheckResetGame(ulong gameId)
    {
        if (gameId == 0 || GameId == gameId) return;

        GameId = gameId;
        ActiveFactions = null;
        LayoutName = null;
        _connectionLifetime?.ForEachRemoteConnection(c =>
        {
            RequestGameInfo(c);
            return false;
        });
    }

    [RpcReceive]
    internal async Task ReceiveGameInfoRequest()
    {
        if (!WarfareModule.IsActive)
            return;

        await UniTask.SwitchToMainThread();
        SendFullState(false);

        await SendFullStateRpc(GameId, PublicIP, Port, BookmarkHost, ActiveFactions, LayoutName, Phase, LayoutStartTime, Ranks, DailyQuests, Squads);
    }

    [RpcSend(nameof(ReceiveGameInfoRequest))]
    protected virtual void RequestGameInfo(IModularRpcRemoteConnection connection) => _ = RpcTask.NotImplemented;


    [RpcSend(nameof(ReceiveSquadUpdate))]
    protected virtual void SendSquadUpdate(ulong gameId, string name, uint faction, int id, bool isLocked, ulong[] members) => _ = RpcTask.NotImplemented;

    [RpcSend(nameof(ReceiveRemoveSquad))]
    protected virtual void SendRemoveSquad(ulong gameId, uint faction, int id) => _ = RpcTask.NotImplemented;

    [RpcSend(nameof(ReceivePhaseUpdated))]
    protected virtual void SendPhaseUpdated(ulong gameId, string? phase) => _ = RpcTask.NotImplemented;

    [RpcSend(nameof(ReceiveDailyQuests))]
    protected virtual void SendDailyQuests(ulong gameId, DailyQuestDay?[]? quests) => _ = RpcTask.NotImplemented;

    [RpcReceive]
    private void ReceiveSquadUpdate(ulong gameId, string name, uint faction, int id, bool isLocked, ulong[] members)
    {
        lock (_sync)
        {
            CheckResetGame(gameId);
            SquadInfo squad = new SquadInfo
            {
                Faction = faction,
                Name = name,
                IdentificationNumber = id,
                Members = members,
                Locked = isLocked
            };

            int index = (Squads ??= new List<SquadInfo>()).FindIndex(x => x.Faction == faction && x.IdentificationNumber == id);
            if (index < 0)
                Squads.Add(squad);
            else
                Squads[index] = squad;
            InvokeSquadsUpdated(index);
        }
    }

    [RpcReceive]
    private void ReceiveRemoveSquad(ulong gameId, uint faction, int id)
    {
        lock (_sync)
        {
            CheckResetGame(gameId);
            if (Squads == null)
                return;

            for (int i = Squads.Count - 1; i >= 0; i--)
            {
                SquadInfo squad = Squads[i];
                if (squad.Faction == faction && squad.IdentificationNumber == id)
                    Squads.RemoveAt(i);
            }

            InvokeSquadsUpdated();
        }
    }

    [RpcReceive]
    private void ReceivePhaseUpdated(ulong gameId, string? phase)
    {
        lock (_sync)
        {
            CheckResetGame(gameId);
            Phase = phase;

            InvokeLayoutUpdated();
        }
    }

    [RpcReceive]
    private void ReceiveDailyQuests(ulong gameId, DailyQuestDay?[]? quests)
    {
        lock (_sync)
        {
            CheckResetGame(gameId);
            DailyQuests = quests;
            InvokeDailyQuestsUpdated();
        }
    }

    private void InvokeLayoutUpdated()
    {
        try
        {
            OnLayoutUpdated?.Invoke();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error invoking OnLayoutUpdated.");
        }
    }

    private void InvokeDailyQuestsUpdated()
    {
        try
        {
            OnDailyQuestsUpdated?.Invoke();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error invoking OnDailyQuestsUpdated.");
        }
    }

    private void InvokeSquadsUpdated(int squadIndex = -1)
    {
        try
        {
            OnSquadsUpdated?.Invoke(squadIndex);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error invoking OnSquadsUpdated.");
        }
    }

    UniTask ILayoutHostedService.StartAsync(CancellationToken token)
    {
        SendFullState();
        return UniTask.CompletedTask;
    }

    UniTask ILayoutHostedService.StopAsync(CancellationToken token)
    {
        return UniTask.CompletedTask;
    }

    void IEventListener<DailyQuestsUpdated>.HandleEvent(DailyQuestsUpdated e, IServiceProvider serviceProvider)
    {
        if (e.Days == null)
        {
            return;
        }

        DailyQuests = e.Days;
        InvokeDailyQuestsUpdated();
        try
        {
            SendDailyQuests(GameId, DailyQuests);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending daily quests.");
        }
    }

    void IEventListener<SquadUpdated>.HandleEvent(SquadUpdated e, IServiceProvider serviceProvider)
    {
        switch (e)
        {
            case SquadDisbanded:
                try
                {
                    SendRemoveSquad(GameId, e.Squad.Team.Faction.PrimaryKey, e.Squad.TeamIdentificationNumber);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error sending squad remove.");
                }
                ReceiveRemoveSquad(GameId, e.Squad.Team.Faction.PrimaryKey, e.Squad.TeamIdentificationNumber);
                break;

            default:
                ulong[] members = e.Squad.GetMemberList();
                try
                {
                    SendSquadUpdate(GameId, e.Squad.Name, e.Squad.Team.Faction.PrimaryKey, e.Squad.TeamIdentificationNumber, e.Squad.IsLocked, members);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error sending squad update.");
                }
                ReceiveSquadUpdate(GameId, e.Squad.Name, e.Squad.Team.Faction.PrimaryKey, e.Squad.TeamIdentificationNumber, e.Squad.IsLocked, members);
                break;
        }
    }

    void IEventListener<HomebaseConnected>.HandleEvent(HomebaseConnected e, IServiceProvider serviceProvider)
    {
        SendFullState();
    }
}

[RpcSerializable(SerializationHelper.MinimumStringSize + SerializationHelper.MinimumArraySize + 4 + 4 + 1, isFixedSize: false)]
public struct SquadInfo : IRpcSerializable
{
    public string Name { get; set; }
    public ulong[] Members { get; set; }
    public uint Faction { get; set; }
    public int IdentificationNumber { get; set; }
    public bool Locked { get; set; }

    /// <inheritdoc />
    public int GetSize(IRpcSerializer serializer)
    {
        return serializer.GetSize(Name) + serializer.GetSize(Members) + 4 + 4 + 1;
    }

    /// <inheritdoc />
    public int Write(Span<byte> writeTo, IRpcSerializer serializer)
    {
        int index = 0;
        index += serializer.WriteObject(Name, writeTo);
        index += serializer.WriteObject(Members, writeTo[index..]);
        index += serializer.WriteObject(Faction, writeTo[index..]);
        index += serializer.WriteObject(IdentificationNumber, writeTo[index..]);
        index += serializer.WriteObject(Locked, writeTo[index..]);
        return index;
    }

    /// <inheritdoc />
    public int Read(Span<byte> readFrom, IRpcSerializer serializer)
    {
        int index = 0;
        Name = serializer.ReadObject<string>(readFrom, out int bytesRead) ?? string.Empty;
        index += bytesRead;

        Members = serializer.ReadObject<ulong[]>(readFrom[index..], out bytesRead) ?? Array.Empty<ulong>();
        index += bytesRead;

        Faction = serializer.ReadObject<uint>(readFrom[index..], out bytesRead);
        index += bytesRead;

        IdentificationNumber = serializer.ReadObject<int>(readFrom[index..], out bytesRead);
        index += bytesRead;

        Locked = serializer.ReadObject<bool>(readFrom[index..], out bytesRead);
        index += bytesRead;

        return index;
    }
}

[RpcSerializable(5, isFixedSize: true)]
public struct TeamInfo : IRpcSerializable
{
    public uint PrimaryKey;
    public LayoutRole Role;

    /// <inheritdoc />
    public int GetSize(IRpcSerializer serializer)
    {
        return 5;
    }

    /// <inheritdoc />
    public int Write(Span<byte> writeTo, IRpcSerializer serializer)
    {
        MemoryMarshal.Write(writeTo, ref PrimaryKey);
        writeTo[4] = (byte)Role;
        return 5;
    }

    /// <inheritdoc />
    public int Read(Span<byte> readFrom, IRpcSerializer serializer)
    {
        PrimaryKey = MemoryMarshal.Read<uint>(readFrom);
        Role = (LayoutRole)readFrom[4];
        return 5;
    }
}