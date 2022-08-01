using SDG.NetTransport;
using System;
using Uncreated.Warfare.Events.Players;
using Uncreated.Warfare.Gamemodes.Interfaces;
using Uncreated.Warfare.Teams;
using Uncreated.Warfare.Tickets;

namespace Uncreated.Warfare.Gamemodes.Flags;

public abstract class TicketGamemode<TProvider> : FlagGamemode, ITickets where TProvider : class, ITicketProvider, new()
{
    private TicketManager _ticketManager;
    public TicketManager TicketManager => _ticketManager;
    protected TicketGamemode(string Name, float EventLoopSpeed) : base(Name, EventLoopSpeed)
    { }
    protected override void PreInit()
    {
        AddSingletonRequirement(ref _ticketManager);
        _ticketManager.Provider = new TProvider();
        base.PreInit();
    }
    protected override void PostInit()
    {
        base.PostInit();
    }
    protected override void EventLoopAction()
    {
        base.EventLoopAction();
        TicketManager.Provider.Tick();
    }
    public override void DeclareWin(ulong winner)
    {
        base.DeclareWin(winner);
        SendWinUI(winner);
    }
    protected void SendWinUI(ulong winner)
    {
        WinToastUI.SendToAllPlayers();
        string img1 = TeamManager.Team1Faction.FlagImageURL;
        string img2 = TeamManager.Team2Faction.FlagImageURL;
        string tick1 = TicketManager.Team1Tickets.ToString(Data.Locale);
        string tick2 = TicketManager.Team2Tickets.ToString(Data.Locale);
        foreach (LanguageSet set in Localization.EnumerateLanguageSets())
        {
            string t1tickets = Localization.Translate("win_ui_value_tickets", set.Language, tick1);
            if (TicketManager.Team1Tickets <= 0)
                t1tickets = t1tickets.Colorize("969696");
            string t2tickets = Localization.Translate("win_ui_value_tickets", set.Language, tick2);
            if (TicketManager.Team2Tickets <= 0)
                t2tickets = t2tickets.Colorize("969696");
            string header = Localization.Translate("win_ui_header_winner", set.Language, TeamManager.TranslateName(winner, set.Language, true));
            while (set.MoveNext())
            {
                if (!set.Next.IsOnline || set.Next.HasUIHidden) continue;
                ITransportConnection c = set.Next.Connection;
                WinToastUI.Team1Flag.SetImage(c, img1);
                WinToastUI.Team2Flag.SetImage(c, img2);
                WinToastUI.Team1Tickets.SetText(c, t1tickets);
                WinToastUI.Team2Tickets.SetText(c, t2tickets);
                WinToastUI.Header.SetText(c, header);
            }
        }
    }
}
