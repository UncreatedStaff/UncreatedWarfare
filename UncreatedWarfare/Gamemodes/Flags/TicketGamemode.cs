using SDG.NetTransport;
using Uncreated.Warfare.Gamemodes.Interfaces;
using Uncreated.Warfare.Teams;
using Uncreated.Warfare.Tickets;

namespace Uncreated.Warfare.Gamemodes.Flags;

public abstract class TicketFlagGamemode<TProvider> : FlagGamemode, ITickets where TProvider : class, ITicketProvider, new()
{
    private TicketManager _ticketManager;
    public TicketManager TicketManager => _ticketManager;
    protected TicketFlagGamemode(string name, float eventLoopSpeed) : base(name, eventLoopSpeed)
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
    public override void DeclareWin(ulong winner)
    {
        base.DeclareWin(winner);
        SendWinUI(winner);
    }
    protected void SendWinUI(ulong winner) => TicketManager.SendWinUI(winner);
}

public abstract class TicketGamemode<TProvider> : TeamGamemode, ITickets where TProvider : class, ITicketProvider, new()
{
    private TicketManager _ticketManager;
    public TicketManager TicketManager => _ticketManager;
    protected TicketGamemode(string name, float eventLoopSpeed) : base(name, eventLoopSpeed)
    { }
    protected override void PreInit()
    {
        AddSingletonRequirement(ref _ticketManager);
        _ticketManager.Provider = new TProvider();
        base.PreInit();
    }
    protected override void EventLoopAction()
    {
        if (State == EState.ACTIVE && TicketManager.Provider != null)
            TicketManager.Provider.Tick();
        base.EventLoopAction();
    }
    public override void DeclareWin(ulong winner)
    {
        SendWinUI(winner);
        base.DeclareWin(winner);
    }
    protected void SendWinUI(ulong winner) => TicketManager.SendWinUI(winner);
}
