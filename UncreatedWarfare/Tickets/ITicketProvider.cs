using SDG.NetTransport;
using Uncreated.Warfare.Events;
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
    public virtual void Load()
    {
        EventDispatcher.OnVehicleDestroyed += OnVehicleDestroyed;
    }
    public virtual void Unload()
    {
        EventDispatcher.OnVehicleDestroyed -= OnVehicleDestroyed;
    }
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
    protected virtual void OnVehicleDestroyed(Events.Vehicles.VehicleDestroyed e)
    {
        if (e.VehicleData is not null)
        {
            if (e.Team == 1)
                TicketManager.Singleton.Team1Tickets -= e.VehicleData.TicketCost;
            else if (e.Team == 2)
                TicketManager.Singleton.Team2Tickets -= e.VehicleData.TicketCost;
        }
    }
}