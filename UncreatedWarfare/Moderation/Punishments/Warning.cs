namespace Uncreated.Warfare.Moderation.Punishments;

[ModerationEntry(ModerationEntryType.Warning)]
public class Warning : Punishment
{
    /// <summary>
    /// <see langword="false"/> until the player actually sees the warning. This is for when a player is warned while they're offline.
    /// </summary>
    public bool HasBeenDisplayed { get; set; }
}