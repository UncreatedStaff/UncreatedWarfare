using SDG.Unturned;
using System.Threading.Tasks;
using Uncreated.Warfare.Gamemodes.Interfaces;
using Uncreated.Warfare.Tickets;

namespace Uncreated.Warfare.Gamemodes.Flags;

public abstract class TicketFlagGamemode<TProvider> : FlagGamemode, ITickets where TProvider : class, ITicketProvider, new()
{
    private TicketManager _ticketManager;
    public TicketManager TicketManager => _ticketManager;
    protected TicketFlagGamemode(string name, float eventLoopSpeed) : base(name, eventLoopSpeed)
    { }
    protected override Task PreInit()
    {
        AddSingletonRequirement(ref _ticketManager);
        _ticketManager.Provider = new TProvider();
        return base.PreInit();
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
    protected void SendWinUI(ulong winner) => TicketManager.SendWinUI(winner);
}

public abstract class TicketGamemode<TProvider> : TeamGamemode, ITickets where TProvider : class, ITicketProvider, new()
{
    private TicketManager _ticketManager;
    public TicketManager TicketManager => _ticketManager;
    protected TicketGamemode(string name, float eventLoopSpeed) : base(name, eventLoopSpeed)
    { }
    protected override Task PreInit()
    {
        AddSingletonRequirement(ref _ticketManager);
        _ticketManager.Provider = new TProvider();
        return base.PreInit();
    }
    protected override void EventLoopAction()
    {
        if (State == EState.ACTIVE && TicketManager.Provider != null)
            TicketManager.Provider.Tick();
        base.EventLoopAction();
    }
    public override Task DeclareWin(ulong winner)
    {
        ThreadUtil.assertIsGameThread();
        SendWinUI(winner);
        return base.DeclareWin(winner);
    }
    protected virtual void SendWinUI(ulong winner) => TicketManager.SendWinUI(winner);
}
