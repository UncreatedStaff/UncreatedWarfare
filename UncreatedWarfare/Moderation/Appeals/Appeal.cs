using System;
using Uncreated.SQL;

namespace Uncreated.Warfare.Moderation.Appeals;
[ModerationEntry(ModerationEntryType.Appeal)]
public class Appeal : ModerationEntry
{
    /// <summary>
    /// Unique ID of the ticket.
    /// </summary>
    public Guid TicketId { get; set; }

    /// <summary>
    /// <see langword="null"/> if the appeal hasn't been resolved yet, otherwise whether or not the appeal was accepted.
    /// </summary>
    public bool? AppealState { get; set; }

    /// <summary>
    /// Punishments being appealed.
    /// </summary>
    public PrimaryKey[] Punishments { get; set; }

    /// <summary>
    /// Responses to the asked questions.
    /// </summary>
    public AppealResponse[] Responses { get; set; }
}

public readonly struct AppealResponse
{
    public string Question { get; }
    public string Response { get; }
    public AppealResponse(string question, string response)
    {
        Question = question;
        Response = response;
    }
}