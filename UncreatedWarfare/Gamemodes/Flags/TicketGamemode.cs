﻿using SDG.Unturned;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
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
            TicketCounter++;
            if (TicketCounter >= 60)
                TicketCounter = 0;
            base.EventLoopAction();
        }
        protected abstract void EvaluateTickets();
        public override void OnGroupChanged(SteamPlayer player, ulong oldGroup, ulong newGroup, ulong oldteam, ulong newteam)
        {
            TicketManager.OnGroupChanged(player, oldteam, newteam);
            base.OnGroupChanged(player, oldGroup, newGroup, oldteam, newteam);
        }
        public override void OnPlayerJoined(UCPlayer player)
        {
            TicketManager.OnPlayerJoined(player);
            base.OnPlayerJoined(player);
        }
        public override void Dispose()
        {
            TicketManager?.Dispose();
            base.Dispose();
        }
    }
}