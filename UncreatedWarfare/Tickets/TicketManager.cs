using SDG.NetTransport;
using SDG.Unturned;
using System;
using System.Collections.Generic;
using System.Linq;
using Uncreated.Warfare.Commands;
using Uncreated.Warfare.Components;
using Uncreated.Warfare.Events;
using Uncreated.Warfare.Events.Players;
using Uncreated.Warfare.Gamemodes;
using Uncreated.Warfare.Gamemodes.Flags.Invasion;
using Uncreated.Warfare.Gamemodes.Insurgency;
using Uncreated.Warfare.Gamemodes.Interfaces;
using Uncreated.Warfare.Point;
using Uncreated.Warfare.Quests;
using Uncreated.Warfare.Singletons;
using Uncreated.Warfare.Squads;
using Uncreated.Warfare.Teams;
using Uncreated.Warfare.Vehicles;
using UnityEngine;
using Flag = Uncreated.Warfare.Gamemodes.Flags.Flag;

namespace Uncreated.Warfare.Tickets;

public class TicketManager : BaseSingleton, IPlayerInitListener, IGameStartListener
{
    public static TicketManager Singleton;
    public static Config<TicketData> config = new Config<TicketData>(Data.Paths.TicketStorage, "config.json");
    public static readonly TicketUI TicketUI = new TicketUI();
    private int _t1Tickets;
    private int _t2Tickets;
    public static bool Loaded => Singleton.IsLoaded();
    public ITicketProvider Provider { get; internal set; }
    public int Team1Tickets
    {
        get => _t1Tickets;
        set
        {
            int oldv = _t1Tickets;
            _t1Tickets = value;
            if (_t1Tickets < 0)
                _t1Tickets = 0;
            bool upd = true;
            if (Provider != null)
                Provider.OnTicketsChanged(1, oldv, _t1Tickets, ref upd);
            if (upd)
                UpdateUI(1);
        }
    }
    public int Team2Tickets
    {
        get => _t2Tickets;
        set
        {
            int oldv = _t2Tickets;
            _t2Tickets = value;
            if (_t2Tickets < 0)
                _t2Tickets = 0;
            bool upd = true;
            if (Provider != null)
                Provider.OnTicketsChanged(2ul, oldv, _t2Tickets, ref upd);
            if (upd)
                UpdateUI(2ul);
        }
    }
    public TicketManager() { }
    public override void Load()
    {
        Singleton = this;
        if (Provider is null)
            throw new InvalidOperationException("You must set TicketManager." + nameof(Provider) + " before TicketManager loads.");
        Provider.Manager = this;
        _t1Tickets = 0;
        _t2Tickets = 0;
        Provider.Load();
        EventDispatcher.OnPlayerDied += OnPlayerDeath;
        EventDispatcher.OnGroupChanged += OnGroupChanged;
        EventDispatcher.OnUIRefreshRequested += ReloadUI;
    }
    public override void Unload()
    {
        Singleton = null!;
        Provider.Unload();
        Provider.Manager = null!;
        Provider = null!;
        Team1Tickets = 0;
        Team2Tickets = 0;
        EventDispatcher.OnUIRefreshRequested -= ReloadUI;
        EventDispatcher.OnGroupChanged -= OnGroupChanged;
        EventDispatcher.OnPlayerDied -= OnPlayerDeath;
    }
    private void ReloadUI(PlayerEvent e)
    {
        SendUI(e.Player);
    }
    public void SendUI(UCPlayer player)
    {
        ulong team = player.GetTeam();
        if (team is 1 or 2)
        {
            if (Provider == null || player is null || !player.IsOnline || player.HasUIHidden)
                return;
            L.Log("Sending UI to " + player.CharacterName);
            TicketUI.SendToPlayer(player.Connection);
            string? url = TeamManager.GetFaction(team)?.FlagImageURL;
            if (url is not null)
                TicketUI.Flag.SetImage(player.Connection, url);
            Provider.UpdateUI(player);
        }
    }
    public void UpdateUI(UCPlayer player) => Provider.UpdateUI(player);
    public void UpdateUI(ulong team)
    {
        if (SDG.Unturned.Provider.clients.Count < 1) return;
        Provider.UpdateUI(team);
    }
    public void UpdateUI()
    {
        if (SDG.Unturned.Provider.clients.Count < 1) return;
        Provider.UpdateUI(1ul);
        Provider.UpdateUI(2ul);
    }
    public void OnFlagCaptured(Flag flag, ulong capturedTeam, ulong lostTeam)
    {
        if (Provider is IFlagCapturedListener cl)
            cl.OnFlagCaptured(flag, capturedTeam, lostTeam);
    }
    private void OnPlayerDeath(PlayerDied e)
    {
        if (Provider is IPlayerDeathListener pd)
            pd.OnPlayerDeath(e);
    }
    private void OnGroupChanged(GroupChanged e)
    {
        if (e.NewTeam is > 0 and < 3)
            SendUI(e.Player);
        else
            ClearUI(e.Player);
    }
    void IPlayerInitListener.OnPlayerInit(UCPlayer player, bool wasAlreadyOnline)
    {
        if (Provider is IPlayerInitListener il)
            il.OnPlayerInit(player, wasAlreadyOnline);
        SendUI(player);
    }
    void IGameStartListener.OnGameStarting(bool isOnLoad)
    {
        if (Provider != null)
            Provider.OnGameStarting(isOnLoad);
    }
    public void ClearUI(UCPlayer player) => TicketUI.ClearFromPlayer(player.Connection);
    public void ClearUI(ITransportConnection connection) => TicketUI.ClearFromPlayer(connection);
}
public class TicketData : ConfigData
{
    public int TicketHandicapDifference;
    public int FOBCost;
    public int PlayerDeathCost;
    public ushort Team1TicketUIID;
    public ushort Team2TicketUIID;

    public override void SetDefaults()
    {
        PlayerDeathCost = 1;
        TicketHandicapDifference = 40;
        FOBCost = 15;
        Team1TicketUIID = 36035;
        Team2TicketUIID = 36058;
    }
    public TicketData() { }
}
