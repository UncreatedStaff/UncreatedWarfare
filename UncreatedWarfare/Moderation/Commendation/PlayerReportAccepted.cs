using Uncreated.SQL;

namespace Uncreated.Warfare.Moderation.Commendation;

[ModerationEntry(ModerationEntryType.PlayerReportAccepted)]
public class PlayerReportAccepted : ModerationEntry
{
    public PrimaryKey Report { get; set; }
}