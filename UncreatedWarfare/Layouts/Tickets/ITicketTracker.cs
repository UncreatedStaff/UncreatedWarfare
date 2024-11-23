using Uncreated.Warfare.Layouts.Teams;

namespace Uncreated.Warfare.Layouts.Tickets;
public interface ITicketTracker
{
    int GetTickets(Team team);
    void SetTickets(Team team, int tickets);
    void IncrementTickets(Team team, int tickets);
    TicketBleedSeverity GetBleedSeverity(Team team);
}
