namespace Uncreated.Warfare.Moderation.Commendation;

[ModerationEntry(ModerationEntryType.BugReportAccepted)]
public class BugReportAccepted : ModerationEntry
{
    public string? Commit { get; set; }
    public override string GetDisplayName() => "Bug Report Accepted";
}