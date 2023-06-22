using Uncreated.Warfare.Commands;

namespace Uncreated.Warfare.Moderation.Punishments;
[ModerationEntry(ModerationEntryType.Mute)]
public class Mute : DurationPunishment
{
    /// <summary>
    /// Which areas of communication the mute applies to.
    /// </summary>
    public MuteType Type { get; set; }
}