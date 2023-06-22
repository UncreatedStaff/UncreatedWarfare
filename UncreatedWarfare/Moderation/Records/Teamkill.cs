using SDG.Unturned;
using System;

namespace Uncreated.Warfare.Moderation.Records;
[ModerationEntry(ModerationEntryType.Teamkill)]
public class Teamkill : ModerationEntry
{
    public EDeathCause Cause { get; set; }
    public Guid? Item { get; set; }
    public string? ItemName { get; set; }
    public ELimb? Limb { get; set; }
    public float Distance { get; set; }
    public string? DeathMessage { get; set; }
}