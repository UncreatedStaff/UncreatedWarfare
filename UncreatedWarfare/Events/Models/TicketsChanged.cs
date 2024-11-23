using System;
using System.Collections.Generic;
using System.Text;
using Uncreated.Warfare.Layouts.Teams;
using Uncreated.Warfare.Layouts.Tickets;

namespace Uncreated.Warfare.Events.Models;
/// <summary>
/// Event listener args which fires after a a certain <see cref="Layouts.Teams.Team"/> gains or loses tickets.
/// </summary>
internal class TicketsChanged 
{
    /// <summary>
    /// The new number of tickets belong to <see cref="Team"/>.
    /// </summary>
    public required int NewNumber {  get; init; }
    /// <summary>
    /// The team's change in tickets. A positive indicates a gain, while a negative value indicates a loss.
    /// </summary>
    public required int Change {  get; init; }
    /// <summary>
    /// The <see cref="Layouts.Teams.Team"/> that gained or lost tickets.
    /// </summary>
    public required Team Team {  get; init; }
    /// <summary>
    /// The instance of <see cref="ITicketTracker"/> that was resonsible for orchestrating this change. 
    /// </summary>
    public required ITicketTracker TicketTracker { get; init; }
}
