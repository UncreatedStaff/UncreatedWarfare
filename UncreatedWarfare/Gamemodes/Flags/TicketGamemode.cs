using SDG.NetTransport;
using SDG.Unturned;
using System.Threading.Tasks;
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
    protected override void EventLoopAction()
    {
        base.EventLoopAction();
        if (State == EState.ACTIVE && TicketManager.Provider != null)
            TicketManager.Provider.Tick();
    }
    public override Task DeclareWin(ulong winner)
    {
        ThreadUtil.assertIsGameThread();
        SendWinUI(winner);
        return base.DeclareWin(winner);
    }
    protected void SendWinUI(ulong winner)
    {
        WinToastUI.SendToAllPlayers();
        string img1 = TeamManager.Team1Faction.FlagImageURL;
        string img2 = TeamManager.Team2Faction.FlagImageURL;
        foreach (LanguageSet set in LanguageSet.All())
        {
            string t1tickets = T.WinUIValueTickets.Translate(set.Language, TicketManager.Team1Tickets);
            if (TicketManager.Team1Tickets <= 0)
                t1tickets = t1tickets.Colorize("969696");
            string t2tickets = T.WinUIValueTickets.Translate(set.Language, TicketManager.Team2Tickets);
            if (TicketManager.Team2Tickets <= 0)
                t2tickets = t2tickets.Colorize("969696");
            string header = T.WinUIHeaderWinner.Translate(set.Language, TeamManager.GetFactionSafe(winner)!);
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
