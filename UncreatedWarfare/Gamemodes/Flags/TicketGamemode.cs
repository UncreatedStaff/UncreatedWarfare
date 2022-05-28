using System;
using Uncreated.Warfare.Gamemodes.Interfaces;
using Uncreated.Warfare.Tickets;

namespace Uncreated.Warfare.Gamemodes.Flags;

public abstract class TicketGamemode : FlagGamemode, ITickets
{
    private TicketManager _ticketManager;
    public TicketManager TicketManager => _ticketManager;
    protected abstract bool TimeToTicket();
    protected TicketGamemode(string Name, float EventLoopSpeed) : base(Name, EventLoopSpeed)
    { }
    protected override void PreInit()
    {
        AddSingletonRequirement(ref _ticketManager);
        base.PreInit();
    }
    protected override void PostDispose()
    {
        base.PostDispose();
    }
    public override void PlayerInit(UCPlayer player, bool wasAlreadyOnline)
    {
        base.PlayerInit(player, wasAlreadyOnline);
    }
    protected override void EventLoopAction()
    {
        if (TimeToTicket())
            EvaluateTickets();
        base.EventLoopAction();
    }
    protected virtual void EvaluateTickets()
    {
        if (EveryXSeconds(20))
        {
            TicketManager.OnFlag20Seconds();
        }
    }
    public override void OnGroupChanged(UCPlayer player, ulong oldGroup, ulong newGroup, ulong oldteam, ulong newteam)
    {
        TicketManager.OnGroupChanged(player.Player.channel.owner, oldteam, newteam);
        base.OnGroupChanged(player, oldGroup, newGroup, oldteam, newteam);
    }
}
