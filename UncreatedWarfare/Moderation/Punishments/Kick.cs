using System.Text.Json.Serialization;

namespace Uncreated.Warfare.Moderation.Punishments;
[ModerationEntry(ModerationEntryType.Kick)]
[JsonConverter(typeof(ModerationEntryConverter))]
public class Kick : Punishment
{
    public override string GetDisplayName() => "Kick";
}
