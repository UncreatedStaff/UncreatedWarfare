using System;
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
#if DEBUG
            using IDisposable profiler = ProfilingUtils.StartTracking();
#endif
            base.Init();
            TicketManager = new TicketManager();
        }
        protected override void EventLoopAction()
        {
#if DEBUG
            using IDisposable profiler = ProfilingUtils.StartTracking();
#endif
            if (TimeToTicket())
                EvaluateTickets();
            base.EventLoopAction();
        }
        protected virtual void EvaluateTickets()
        {
            if (EveryXSeconds(20))
            {
#if DEBUG
                using IDisposable profiler = ProfilingUtils.StartTracking();
#endif
                TicketManager.OnFlag20Seconds();
            }
        }
        public override void OnGroupChanged(UCPlayer player, ulong oldGroup, ulong newGroup, ulong oldteam, ulong newteam)
        {
#if DEBUG
            using IDisposable profiler = ProfilingUtils.StartTracking();
#endif
            TicketManager.OnGroupChanged(player.Player.channel.owner, oldteam, newteam);
            base.OnGroupChanged(player, oldGroup, newGroup, oldteam, newteam);
        }
        public override void OnPlayerJoined(UCPlayer player, bool wasAlreadyOnline, bool shouldRespawn)
        {
#if DEBUG
            using IDisposable profiler = ProfilingUtils.StartTracking();
#endif
            TicketManager.OnPlayerJoined(player);
            base.OnPlayerJoined(player, wasAlreadyOnline, shouldRespawn);
        }
        public override void Dispose()
        {
#if DEBUG
            using IDisposable profiler = ProfilingUtils.StartTracking();
#endif
            TicketManager?.Dispose();
            base.Dispose();
        }
    }
}
