using System.Text.Json.Serialization;

namespace Uncreated.Warfare.Moderation.Commendation;
[JsonConverter(typeof(ModerationEntryConverter))]
public class Commendation : ModerationEntry
{
    public override string GetDisplayName() => "Commendation";
}