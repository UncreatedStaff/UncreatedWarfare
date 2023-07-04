namespace Uncreated.Warfare.Moderation.Punishments;
[ModerationEntry(ModerationEntryType.Ban)]
public class Ban : DurationPunishment
{
    public override string GetDisplayName() => "Ban";
}
