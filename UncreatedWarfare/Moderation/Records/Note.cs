namespace Uncreated.Warfare.Moderation.Records;

[ModerationEntry(ModerationEntryType.Note)]
public class Note : ModerationEntry
{
    public override string GetDisplayName() => "Note";
}