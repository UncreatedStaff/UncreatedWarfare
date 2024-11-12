using Uncreated.Warfare.Translations;
using Uncreated.Warfare.Translations.Addons;
using Uncreated.Warfare.Vehicles;

namespace Uncreated.Warfare.Players.UI;
public class TipTranslations : PropertiesTranslationCollection
{
    protected override string FileName => "Tips";

    [TranslationData("Sent to tell a player that another player requested a UAV.")]
    public readonly Translation<IPlayer> UAVRequest = new Translation<IPlayer>("<#d9c69a>{0} Requested a UAV!", TranslationOptions.TMProUI, WarfarePlayer.FormatColoredNickName);

    [TranslationData("Sent to tell a player that they need to build a radio after they get out of a logi for the first time.")]
    public readonly Translation PlaceRadio = new Translation("Place a <#ababab>FOB RADIO</color>.", TranslationOptions.TMProUI);

    [TranslationData("Sent to tell a player that they need to build a bunker.")]
    public readonly Translation PlaceBunker = new Translation("Build a <#a5c3d9>FOB BUNKER</color> so that your team can spawn.", TranslationOptions.TMProUI);

    [TranslationData("Sent to tell a player that they need to drop supplies near a FOB.")]
    public readonly Translation UnloadSupplies = new Translation("<#d9c69a>DROP SUPPLIES</color> onto the FOB.", TranslationOptions.TMProUI);

    [TranslationData("Sent to tell a player that their teammate is shoveling something.")]
    public readonly Translation<IPlayer> HelpBuild = new Translation<IPlayer>("<#d9c69a>{0} needs help building!", TranslationOptions.TMProUI, WarfarePlayer.FormatColoredNickName);

    [TranslationData("Sent to tell a player that their vehicle was resupplied.")]
    public readonly Translation<VehicleType> LogisticsVehicleResupplied = new Translation<VehicleType>("Your <#009933>{0}</color> has been auto resupplied.", TranslationOptions.TMProUI, UppercaseAddon.Instance);

    [TranslationData("Sent to tell a player how to open the action menu.")]
    public readonly Translation ActionMenu = new Translation("Press <#a5c3d9><plugin_1/></color> for field actions", TranslationOptions.TMProUI);

    [TranslationData("Sent to tell a squad leader how to open the action menu.")]
    public readonly Translation ActionMenuSquadLeader = new Translation("Press <#a5c3d9><plugin_1/></color> for <#85c996>squad actions</color>", TranslationOptions.TMProUI);

    [TranslationData("Sent to tell a player how to call for a medic.")]
    public readonly Translation CallMedic = new Translation("You are hurt. Press <#d9a5bb><plugin_1/></color> to call for a medic.", TranslationOptions.TMProUI);
}