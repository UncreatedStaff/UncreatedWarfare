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
            using IDisposable profiler = ProfilingUtils.StartTracking();
            base.Init();
            TicketManager = new TicketManager();
        }
        protected override void EventLoopAction()
        {
            using IDisposable profiler = ProfilingUtils.StartTracking();
            if (TimeToTicket())
                EvaluateTickets();
            base.EventLoopAction();
        }
        protected virtual void EvaluateTickets()
        {
            if (Every10Seconds)
            {
                using IDisposable profiler = ProfilingUtils.StartTracking();
                TicketManager.OnFlag10Seconds();
            }
        }
        public override void OnGroupChanged(UCPlayer player, ulong oldGroup, ulong newGroup, ulong oldteam, ulong newteam)
        {
            using IDisposable profiler = ProfilingUtils.StartTracking();
            TicketManager.OnGroupChanged(player.Player.channel.owner, oldteam, newteam);
            base.OnGroupChanged(player, oldGroup, newGroup, oldteam, newteam);
        }
        public override void OnPlayerJoined(UCPlayer player, bool wasAlreadyOnline, bool shouldRespawn)
        {
            using IDisposable profiler = ProfilingUtils.StartTracking();
            TicketManager.OnPlayerJoined(player);
            base.OnPlayerJoined(player, wasAlreadyOnline, shouldRespawn);
        }
        public override void Dispose()
        {
            using IDisposable profiler = ProfilingUtils.StartTracking();
            TicketManager?.Dispose();
            base.Dispose();
        }
    }
}
