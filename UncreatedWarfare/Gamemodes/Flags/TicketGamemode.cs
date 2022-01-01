using Uncreated.Warfare.Tickets;

namespace Uncreated.Warfare.Gamemodes.Flags
{
    public abstract class TicketGamemode : FlagGamemode
    {
        public static TicketManager TicketManager;
        protected abstract bool TimeToTicket();
        protected TicketGamemode(string Name, float EventLoopSpeed) : base(Name, EventLoopSpeed)
        { }
        public override void Init()
        {
            base.Init();
            TicketManager = new TicketManager();
        }
        protected override void EventLoopAction()
        {
            if (TimeToTicket())
                EvaluateTickets();
            base.EventLoopAction();
        }
        protected abstract void EvaluateTickets();
        public override void OnGroupChanged(UCPlayer player, ulong oldGroup, ulong newGroup, ulong oldteam, ulong newteam)
        {
            TicketManager.OnGroupChanged(player.Player.channel.owner, oldteam, newteam);
            base.OnGroupChanged(player, oldGroup, newGroup, oldteam, newteam);
        }
        public override void OnPlayerJoined(UCPlayer player, bool wasAlreadyOnline = false)
        {
            TicketManager.OnPlayerJoined(player);
            base.OnPlayerJoined(player, wasAlreadyOnline);
        }
        public override void Dispose()
        {
            TicketManager?.Dispose();
            base.Dispose();
        }
    }
}
