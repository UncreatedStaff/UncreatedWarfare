using System.Text.Json.Serialization;

namespace Uncreated.Warfare.Moderation.Records;

[ModerationEntry(ModerationEntryType.Note)]
[JsonConverter(typeof(ModerationEntryConverter))]
public class Note : ModerationEntry
{
    public override string GetDisplayName() => "Note";
}