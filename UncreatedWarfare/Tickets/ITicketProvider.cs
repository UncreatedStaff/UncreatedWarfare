using SDG.NetTransport;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Uncreated.Warfare.Events.Players;
using Uncreated.Warfare.Singletons;

namespace Uncreated.Warfare.Tickets;
public interface ITicketProvider
{
    TicketManager Manager { get; internal set; }
    void Load();
    void Unload();
    int GetTeamBleed(ulong team);
    void UpdateUI(UCPlayer player);
    void UpdateUI(ulong team);
    void OnTicketsChanged(ulong team, int oldValue, int newValue, ref bool updateUI);
    void Tick();
    void OnGameStarting(bool isOnLoaded);
}
public abstract class BaseTicketProvider : ITicketProvider, IPlayerDeathListener
{
    public TicketManager Manager { get; set; }
    public abstract int GetTeamBleed(ulong team);
    public abstract void Load();
    public abstract void Unload();
    public abstract void OnGameStarting(bool isOnLoaded);
    public abstract void OnTicketsChanged(ulong team, int oldValue, int newValue, ref bool updateUI);
    public abstract void Tick();
    public abstract void GetDisplayInfo(ulong team, out string message, out string tickets, out string bleed);
    public virtual void UpdateUI(UCPlayer player)
    {
        ulong team = player.GetTeam();
        GetDisplayInfo(team, out string message, out string tickets, out string bleed);
        ITransportConnection c = player.Connection;
        TicketManager.TicketUI.Tickets.SetText(c, tickets);
        TicketManager.TicketUI.Bleed.SetText(c, bleed);
        TicketManager.TicketUI.Status.SetText(c, message);
    }
    public virtual void UpdateUI(ulong team)
    {
        GetDisplayInfo(team, out string message, out string tickets, out string bleed);
        for (int i = 0; i < PlayerManager.OnlinePlayers.Count; ++i)
        {
            if (PlayerManager.OnlinePlayers[i].GetTeam() == team)
            {
                ITransportConnection c = PlayerManager.OnlinePlayers[i].Connection;
                TicketManager.TicketUI.Tickets.SetText(c, tickets);
                TicketManager.TicketUI.Bleed.SetText(c, bleed);
                TicketManager.TicketUI.Status.SetText(c, message);
            }
        }
    }
    public virtual void OnPlayerDeath(PlayerDied e)
    {
        if (e.DeadTeam == 1)
            --Manager.Team1Tickets;
        else if (e.DeadTeam == 2)
            --Manager.Team2Tickets;
    }
}