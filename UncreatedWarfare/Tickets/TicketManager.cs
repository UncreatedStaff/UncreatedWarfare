using SDG.NetTransport;
using System;
using Uncreated.Warfare.Configuration;
using Uncreated.Warfare.Events;
using Uncreated.Warfare.Events.Players;
using Uncreated.Warfare.Gamemodes;
using Uncreated.Warfare.Singletons;
using Uncreated.Warfare.Teams;
using Flag = Uncreated.Warfare.Gamemodes.Flags.Flag;

namespace Uncreated.Warfare.Tickets;

public class TicketManager : BaseSingleton, IPlayerPreInitListener, IGameStartListener, IGameTickListener, IFlagCapturedListener, IFlagNeutralizedListener, IUIListener
{
    public static TicketManager Singleton;
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
    public override void Load()
    {
        Singleton = this;
        if (Provider is null)
            throw new InvalidOperationException("You must set TicketManager." + nameof(Provider) + " before TicketManager loads.");
        Provider.Manager = this;
        _t1Tickets = 0;
        _t2Tickets = 0;
        Provider.Load();
        EventDispatcher.GroupChanged += OnGroupChanged;
    }
    public override void Unload()
    {
        Singleton = null!;
        Provider.Unload();
        Provider.Manager = null!;
        Provider = null!;
        Team1Tickets = 0;
        Team2Tickets = 0;
        EventDispatcher.GroupChanged -= OnGroupChanged;
    }
    void IGameTickListener.Tick()
    {
        if (Data.Gamemode != null && Data.Gamemode.State == State.Active && Provider != null)
        {
            Provider.Tick();
        }
    }
    public void ShowUI(UCPlayer player)
    {
        ulong team = player.GetTeam();
        if (team is 1 or 2)
        {
            if (Provider == null || player is null || !player.IsOnline || player.HasUIHidden)
                return;
            TicketUI.SendToPlayer(player.Connection);
            player.HasTicketUI = true;
            string? url = TeamManager.GetFaction(team).FlagImageURL;
            if (url is not null)
                TicketUI.Flag.SetImage(player.Connection, url);
            Provider.UpdateUI(player);
        }
    }
    public void UpdateUI(UCPlayer player) => Provider?.UpdateUI(player);
    public void UpdateUI(ulong team)
    {
        if (Provider == null) return;
        if (SDG.Unturned.Provider.clients.Count < 1) return;
        Provider.UpdateUI(team);
    }
    public void UpdateUI()
    {
        if (Provider == null) return;
        if (SDG.Unturned.Provider.clients.Count < 1) return;
        Provider.UpdateUI(1ul);
        Provider.UpdateUI(2ul);
    }
    void IFlagCapturedListener.OnFlagCaptured(Flag flag, ulong capturedTeam, ulong lostTeam)
    {
        if (Provider is IFlagCapturedListener cl)
            cl.OnFlagCaptured(flag, capturedTeam, lostTeam);
    }
    void IFlagNeutralizedListener.OnFlagNeutralized(Flag flag, ulong capturedTeam, ulong lostTeam)
    {
        if (Provider is IFlagNeutralizedListener cl)
            cl.OnFlagNeutralized(flag, capturedTeam, lostTeam);
    }
    private void OnGroupChanged(GroupChanged e)
    {
        if (e.NewTeam is > 0 and < 3)
            ShowUI(e.Player);
        else
            HideUI(e.Player);
    }
    void IPlayerPreInitListener.OnPrePlayerInit(UCPlayer player, bool wasAlreadyOnline)
    {
        if (Provider is IPlayerPreInitListener il)
            il.OnPrePlayerInit(player, wasAlreadyOnline);
        ShowUI(player);
    }
    void IGameStartListener.OnGameStarting(bool isOnLoad)
    {
        Provider?.OnGameStarting(isOnLoad);
    }
    public void HideUI(UCPlayer player)
    {
        player.HasTicketUI = false;
        TicketUI.ClearFromPlayer(player.Connection);
    }

    public void SendWinUI(ulong winner)
    {
        Gamemode.WinToastUI.SendToAllPlayers();
        string img1 = TeamManager.Team1Faction.FlagImageURL;
        string img2 = TeamManager.Team2Faction.FlagImageURL;
        foreach (LanguageSet set in LanguageSet.All())
        {
            string t1tickets = T.WinUIValueTickets.Translate(set.Language, this.Team1Tickets);
            if (this.Team1Tickets <= 0)
                t1tickets = t1tickets.Colorize("969696");
            string t2tickets = T.WinUIValueTickets.Translate(set.Language, this.Team2Tickets);
            if (this.Team2Tickets <= 0)
                t2tickets = t2tickets.Colorize("969696");
            string header = T.WinUIHeaderWinner.Translate(set.Language, TeamManager.GetFactionSafe(winner)!);
            while (set.MoveNext())
            {
                if (!set.Next.IsOnline || set.Next.HasUIHidden) continue;
                ITransportConnection c = set.Next.Connection;
                Gamemode.WinToastUI.Team1Flag.SetImage(c, img1);
                Gamemode.WinToastUI.Team2Flag.SetImage(c, img2);
                Gamemode.WinToastUI.Team1Tickets.SetText(c, t1tickets);
                Gamemode.WinToastUI.Team2Tickets.SetText(c, t2tickets);
                Gamemode.WinToastUI.Header.SetText(c, header);
            }
        }
    }
}