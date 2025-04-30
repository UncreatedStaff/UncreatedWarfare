using System;
using System.Collections.Generic;
using Uncreated.Warfare.Events.Models.Tickets;
using Uncreated.Warfare.Layouts.Phases;
using Uncreated.Warfare.Layouts.Teams;
using Uncreated.Warfare.Services;
using Uncreated.Warfare.Util;

namespace Uncreated.Warfare.Layouts.Tickets;
public abstract class BaseTicketTracker : ILayoutHostedService, ITicketTracker
{
    private readonly Dictionary<Team, int> _ticketMap;

    protected Layout Layout { get; }

    protected BaseTicketTracker(Layout layout)
    {
        _ticketMap = new Dictionary<Team, int>();
        Layout = layout;
    }

    public int GetTickets(Team team)
    {
        _ticketMap.TryGetValue(team, out int tickets);
        return tickets;
    }
    public void SetTickets(Team team, int tickets)
    {
        GameThread.AssertCurrent();

        if (tickets < 0)
            throw new ArgumentException("Cannot set tickets to a negative number.", nameof(tickets));

        _ticketMap.TryGetValue(team, out int oldTickets);
        _ticketMap[team] = tickets;

        TicketsChanged args = new TicketsChanged
        {
            NewNumber = tickets,
            Change = tickets - oldTickets,
            Team = team,
            TicketTracker = this
        };

        _ = WarfareModule.EventDispatcher.DispatchEventAsync(args, default);
    }
    public void IncrementTickets(Team team, int tickets)
    {
        GameThread.AssertCurrent();

        // todo: id prefer a better way to keep tickets from running in main phases
        if (!team.IsValid || Layout.ActivePhase is not ActionPhase)
            return;

        int newTicketCount;
        if (!_ticketMap.TryGetValue(team, out int oldTickets))
        {
            newTicketCount = Math.Max(0, tickets);
            _ticketMap.Add(team, newTicketCount);
        }
        else
        {
            newTicketCount = Math.Max(0, oldTickets + tickets);
            _ticketMap[team] = newTicketCount;
        }

        _ = WarfareModule.EventDispatcher.DispatchEventAsync(new TicketsChanged
        {
            NewNumber = newTicketCount,
            Change = tickets,
            Team = team,
            TicketTracker = this
        });
    }
    public abstract UniTask StartAsync(CancellationToken token);
    public abstract UniTask StopAsync(CancellationToken token);
    public abstract TicketBleedSeverity GetBleedSeverity(Team team);
}
