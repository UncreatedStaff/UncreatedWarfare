using Uncreated.Warfare.Interaction.Commands;
using Uncreated.Warfare.Locations;
using Uncreated.Warfare.Translations;
using Uncreated.Warfare.Zones;

namespace Uncreated.Warfare.Commands;

[Command("zone", "zones"), MetadataFile]
public class ZoneCommand : ICommand;

public class ZoneCommandTranslations : PropertiesTranslationCollection
{
    protected override string FileName => "Zone Command";

    [TranslationData("Send the caller's location and angle.", Parameters = [ "X (m)", "Y (m)", "Z (m)", "Yaw (°)" ])]
    public readonly Translation<float, float, float, float> ZoneUtilLocation = new Translation<float, float, float, float>("<#e6e3d5>Location: {0}, {1}, {2} | Yaw: {3}°.", arg0Fmt: "0.##", arg1Fmt: "0.##", arg2Fmt: "0.##", arg3Fmt: "0.##");

    [TranslationData("Response to the caller when they're not in a zone and try to run a zone command.")]
    public readonly Translation ZoneNoResultsLocation = new Translation("<#ff8c69>You aren't in any existing zone.");

    [TranslationData("Response to the caller when they try to run a zone command but their input doesn't match an active zone.")]
    public readonly Translation ZoneNoResultsName = new Translation("<#ff8c69>Couldn't find a zone by that name.");

    [TranslationData("Response to the caller when they try to run a zone command but their input doesn't match an active zone and they're not in a zone.")]
    public readonly Translation ZoneNoResults = new Translation("<#ff8c69>You must be in a zone or specify a valid zone name to use this command.");

    [TranslationData("Sent when a player visualizes a zone's border.", Parameters = [ "Number of border particles spawned", "Zone name" ])]
    public readonly Translation<int, Zone> ZoneVisualizeSuccess = new Translation<int, Zone>("<#e6e3d5>Spawned {0} particles around <color=#cedcde>{1}</color>.", arg1Fmt: Flag.NAME_FORMAT);

    [TranslationData("Sent after a player teleports to a zone.", IsPriorityTranslation = false)]
    public readonly Translation<Zone> ZoneGoSuccess = new Translation<Zone>("<#e6e3d5>Teleported to <#5a6e5c>{0}</color>.", arg0Fmt: Flag.NAME_FORMAT);

    [TranslationData("Sent after a player teleports to a location that isn't a zone.", IsPriorityTranslation = false)]
    public readonly Translation<string> ZoneGoSuccessRaw = new Translation<string>("<#e6e3d5>Teleported to <#cedcde>{0}</color>.");

    [TranslationData("Sent after a player teleports to a grid location.", IsPriorityTranslation = false)]
    public readonly Translation<GridLocation> ZoneGoSuccessGridLocation = new Translation<GridLocation>("<#e6e3d5>Teleported to <#ff8c69>{0}</color>.");
}