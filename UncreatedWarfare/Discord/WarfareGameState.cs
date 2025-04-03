using DanielWillett.ModularRpcs.Annotations;
using DanielWillett.ModularRpcs.Async;
using System;
using System.Collections.Generic;
using Uncreated.Warfare.Events.Models;
using Uncreated.Warfare.Events.Models.Squads;
using Uncreated.Warfare.Quests.Daily;
using Uncreated.Warfare.Squads;
using Uncreated.Warfare.Stats;

namespace Uncreated.Warfare.Discord;

[RpcClass]
public class WarfareGameStateService : IEventListener<SquadUpdated>
{
    private readonly SquadManager? _squadManager;

    public bool HasInformation { get; set; }

    public string? PublicIP { get; set; }
    public List<uint>? ActiveFactions { get; set; }
    public string? LayoutName { get; set; }
    public long GameId { get; set; }
    public List<WarfareRank>? Ranks { get; set; }
    public DailyQuestRegenerateResult DailyQuests { get; set; }
    public List<SquadInfo> Squads { get; set; }

    public WarfareGameStateService(SquadManager? squadManager = null)
    {
        _squadManager = squadManager;
    }

    void IEventListener<SquadUpdated>.HandleEvent(SquadUpdated e, IServiceProvider serviceProvider)
    {
        switch (e)
        {
            case SquadDisbanded:
                SendRemoveSquad(GameId, e.Squad.Team.Faction.PrimaryKey, e.Squad.TeamIdentificationNumber);
                ReceiveRemoveSquad(GameId, e.Squad.Team.Faction.PrimaryKey, e.Squad.TeamIdentificationNumber);
                break;

            default:
                ulong[] members = e.Squad.GetMemberList();
                SendSquadUpdate(GameId, e.Squad.Name, e.Squad.Team.Faction.PrimaryKey, e.Squad.TeamIdentificationNumber, e.Squad.IsLocked, members);
                ReceiveSquadUpdate(GameId, e.Squad.Name, e.Squad.Team.Faction.PrimaryKey, e.Squad.TeamIdentificationNumber, e.Squad.IsLocked, members);
                break;
        }
    }

    private void CheckResetGame(long gameId)
    {
        if (GameId == gameId) return;

        GameId = gameId;
        ActiveFactions = null;
        LayoutName = null;
        RequestGameInfo();
    }

    [RpcReceive]
    internal void SendGameInfo()
    {
        // todo
    }

    [RpcSend(nameof(SendGameInfo))]
    protected virtual void RequestGameInfo() => _ = RpcTask.NotImplemented;

    [RpcSend(nameof(ReceiveSquadUpdate))]
    protected virtual void SendSquadUpdate(long gameId, string name, uint faction, int id, bool isLocked, ulong[] members) => _ = RpcTask.NotImplemented;

    [RpcSend(nameof(ReceiveRemoveSquad))]
    protected virtual void SendRemoveSquad(long gameId, uint faction, int id) => _ = RpcTask.NotImplemented;

    [RpcReceive]
    private void ReceiveSquadUpdate(long gameId, string name, uint faction, int id, bool isLocked, ulong[] members)
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

        int index = Squads.FindIndex(x => x.Faction == faction && x.IdentificationNumber == id);
        if (index < 0)
            Squads.Add(squad);
        else
            Squads[index] = squad;
    }

    [RpcReceive]
    private void ReceiveRemoveSquad(long gameId, uint faction, int id)
    {
        CheckResetGame(gameId);
        for (int i = Squads.Count - 1; i >= 0; i--)
        {
            SquadInfo squad = Squads[i];
            if (squad.Faction == faction && squad.IdentificationNumber == id)
                Squads.RemoveAt(i);
        }
    }
}

public struct SquadInfo
{
    public string Name { get; set; }
    public ulong[] Members { get; set; }
    public uint Faction { get; set; }
    public int IdentificationNumber { get; set; }
    public bool Locked { get; set; }
}

public struct GameInfo
{

}