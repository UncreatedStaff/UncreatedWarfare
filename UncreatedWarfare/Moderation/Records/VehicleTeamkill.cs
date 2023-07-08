using SDG.Unturned;
using System;

namespace Uncreated.Warfare.Moderation.Records;
[ModerationEntry(ModerationEntryType.VehicleTeamkill)]
public class VehicleTeamkill : ModerationEntry
{
    public EDamageOrigin Origin { get; set; }
    public Guid Vehicle { get; set; }
    public string VehicleName { get; set; }
    public Guid? Item { get; set; }
    public string? ItemName { get; set; }
    public string DeathMessage { get; set; }
    public override string GetDisplayName() => "Vehicle Teamkill";
}
