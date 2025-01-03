using System;
using System.Collections.Generic;
using Uncreated.Warfare.Events.Models.Tickets;
using Uncreated.Warfare.Layouts.Teams;
using Uncreated.Warfare.Services;
using Uncreated.Warfare.Util;

namespace Uncreated.Warfare.Layouts.Tickets;
public abstract class BaseTicketTracker : ILayoutHostedService, ITicketTracker
{
    private readonly Dictionary<Team, int> _ticketMap;
    protected readonly Layout layout;
    public BaseTicketTracker(Layout layout)
    {
        _ticketMap = new Dictionary<Team, int>();
        this.layout = layout;
    }
    public int GetTickets(Team team)
    {
        if (_ticketMap.TryGetValue(team, out int tickets))
            return tickets;

        return 0;
    }
    public void SetTickets(Team team, int tickets)
    {
        GameThread.AssertCurrent();

        if (tickets < 0)
            throw new ArgumentException("Cannot set tickets to a negative number.");

        _ticketMap[team] = tickets;
        _ = WarfareModule.EventDispatcher.DispatchEventAsync(new TicketsChanged
        { 
            NewNumber = _ticketMap[team],
            Change = tickets,
            Team = team,
            TicketTracker = this
        });
    }
    public void IncrementTickets(Team team, int tickets)
    {
        GameThread.AssertCurrent();

        if (!team.IsValid)
            return;

        if (!_ticketMap.ContainsKey(team))
        {
            _ticketMap[team] = 0;
        }
        int newNumber = Mathf.Clamp(_ticketMap[team] + tickets, 0, int.MaxValue);
        _ticketMap[team] = newNumber;
        _ = WarfareModule.EventDispatcher.DispatchEventAsync(new TicketsChanged
        {
            NewNumber = _ticketMap[team],
            Change = tickets,
            Team = team,
            TicketTracker = this
        });
    }
    public abstract UniTask StartAsync(CancellationToken token);
    public abstract UniTask StopAsync(CancellationToken token);
    public abstract TicketBleedSeverity GetBleedSeverity(Team team);
}
