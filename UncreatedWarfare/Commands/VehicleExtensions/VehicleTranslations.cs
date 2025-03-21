using Uncreated.Warfare.Players;
using Uncreated.Warfare.Teams;
using Uncreated.Warfare.Translations;
using Uncreated.Warfare.Translations.ValueFormatters;

namespace Uncreated.Warfare.Commands;

internal sealed class VehicleTranslations : PropertiesTranslationCollection
{
    protected override string FileName => "Commands/Vehicle";

    [TranslationData("Sent to the player when they try to use a vehicle command without looking at a vehicle (and they have more than one nearby vehicle).")]
    public readonly Translation VehicleMustBeLookingAtLinkedVehicle = new Translation("<#ff8c69>You must be looking at a vehicle or own only one nearby.");

    [TranslationData("Sent to the player when they try to use a vehicle command on a vehicle not in their group.")]
    public readonly Translation<FactionInfo> VehicleNotOnSameTeam = new Translation<FactionInfo>("<#ff8c69>This vehicle is on {0} but you're not.", arg0Fmt: FactionInfo.FormatColorDisplayName);

    [TranslationData("Sent to the player when they try to use a vehicle command on a vehicle they don't own.")]
    public readonly Translation<IPlayer?> VehicleLinkedVehicleNotOwnedByCaller = new Translation<IPlayer?>("<#ff8c69>This vehicle is owned by {0}.", arg0Fmt: WarfarePlayer.FormatColoredCharacterName);
    
    [TranslationData("Sent to the player after /vehicle give is ran successfully.")]
    public readonly Translation<VehicleAsset, IPlayer> VehicleGiven = new Translation<VehicleAsset, IPlayer>("<#d1bda7>Gave your <#a0ad8e>{0}</color> to {1}.", arg0Fmt: AssetValueFormatter, arg1Fmt: WarfarePlayer.FormatColoredCharacterName);

    [TranslationData("Sent to the player after /vehicle give is ran successfully.")]
    public readonly Translation<VehicleAsset, IPlayer> VehicleGivenDm = new Translation<VehicleAsset, IPlayer>("<#d1bda7>{1} gave you their <#a0ad8e>{0}</color>.", arg1Fmt: WarfarePlayer.FormatColoredNickName);

    [TranslationData("Sent when a player tries to kick another player who isn't in the vehicle.")]
    public readonly Translation<IPlayer> VehicleTargetNotInVehicle = new Translation<IPlayer>("<#ff8c69>{0} is not in a vehicle.", arg0Fmt: WarfarePlayer.FormatColoredNickName);

    [TranslationData("Sent when a player tries to kick from a seat that is empty.")]
    public readonly Translation<int> VehicleSeatNotOccupied = new Translation<int>("<#ff8c69>Seat <#ddd>#{0}</color> is not occupied.");

    [TranslationData("Sent when a player tries to kick from a seat but the plugin can't figure out which seat to choose from their input.")]
    public readonly Translation<string> VehicleSeatNotValidText = new Translation<string>("<#ff8c69>Unable to choose a seat from \"<#fff>{0}</color>\".");

    [TranslationData("Sent to the kicked player when a player is kicked from a vehicle.")]
    public readonly Translation<VehicleAsset, IPlayer, int> VehicleOwnerKickedDM = new Translation<VehicleAsset, IPlayer, int>("<#d1a8a8>The owner of the <#ccc>{0}</color>, {1}, kicked you out of seat <#ddd>#{2}</color>.", arg1Fmt: WarfarePlayer.FormatColoredNickName);

    [TranslationData("Sent to the calling player when a player is kicked from a vehicle.")]
    public readonly Translation<VehicleAsset, IPlayer, int> VehicleKickedPlayer = new Translation<VehicleAsset, IPlayer, int>("<#d1bda7>Kicked {1} out of seat <#ddd>#{2}</color> in your <#ccc>{0}</color>.", arg1Fmt: WarfarePlayer.FormatColoredNickName);
}