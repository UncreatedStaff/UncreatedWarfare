using System.Text.Json.Serialization;

namespace Uncreated.Warfare.Moderation.Punishments;
[ModerationEntry(ModerationEntryType.Ban)]
[JsonConverter(typeof(ModerationEntryConverter))]
public class Ban : DurationPunishment
{
    public override string GetDisplayName() => "Ban";
}
