using Uncreated.Warfare.Layouts.Teams;
using Uncreated.Warfare.Teams;
using Uncreated.Warfare.Translations;

namespace Uncreated.Warfare.Layouts.Tickets;
public interface ITicketTracker
{
    int GetTickets(Team team);
    void SetTickets(Team team, int tickets);
    void IncrementTickets(Team team, int tickets);
    TicketBleedSeverity GetBleedSeverity(Team team);
}

public interface ICustomUITicketTracker : ITicketTracker
{
    /// <summary>
    /// If <see cref="GetTicketText"/> will be called for each player instead of for each team.
    /// </summary>
    bool PlayerSpecific { get; }

    /// <summary>
    /// Gets custom information for the ticket part of the flag UI.
    /// </summary>
    /// <param name="fallback">Whether or not to fallback to the default text.</param>
    (string Tickets, FactionInfo? FlagFaction) GetTicketText(LanguageSet set, out bool fallback);
}