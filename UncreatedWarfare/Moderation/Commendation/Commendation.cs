using System.Text.Json.Serialization;

namespace Uncreated.Warfare.Moderation.Commendation;
[JsonConverter(typeof(ModerationEntryConverter))]
[ModerationEntry(ModerationEntryType.Commendation)]
public class Commendation : ModerationEntry
{
    public override string GetDisplayName() => "Commendation";
}