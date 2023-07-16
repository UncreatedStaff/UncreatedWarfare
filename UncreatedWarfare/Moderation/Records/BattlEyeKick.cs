using System.Text.Json.Serialization;

namespace Uncreated.Warfare.Moderation.Records;
[ModerationEntry(ModerationEntryType.BattlEyeKick)]
[JsonConverter(typeof(ModerationEntryConverter))]
public class BattlEyeKick : ModerationEntry
{
    public override string GetDisplayName() => "BattlEye Kick";
}
