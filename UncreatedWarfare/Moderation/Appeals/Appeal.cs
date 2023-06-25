using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Uncreated.SQL;
using Uncreated.Warfare.Moderation.Punishments;

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
    public Punishment?[] Punishments { get; set; } = Array.Empty<Punishment>();

    /// <summary>
    /// Keys to the punishments being appealed.
    /// </summary>
    public PrimaryKey[] PunishmentKeys { get; set; } = Array.Empty<PrimaryKey>();

    /// <summary>
    /// Responses to the asked questions.
    /// </summary>
    public AppealResponse[] Responses { get; set; }

    internal override async Task FillDetail(DatabaseInterface db)
    {
        if (Punishments.Length != PunishmentKeys.Length)
            Punishments = new Punishment[PunishmentKeys.Length];
        for (int i = 0; i < PunishmentKeys.Length; ++i)
        {
            PrimaryKey key = PunishmentKeys[i];
            if (db.Cache.TryGet<Punishment>(key.Key, out Punishment? p, DatabaseInterface.DefaultInvalidateDuration))
                Punishments[i] = p;
            else
            {
                p = await db.ReadOne<Punishment>(key).ConfigureAwait(false);
                Punishments[i] = p;
            }
        }
    }
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