using Uncreated.Warfare.Translations;
using Uncreated.Warfare.Translations.Addons;
using Uncreated.Warfare.Vehicles.WarfareVehicles;

namespace Uncreated.Warfare.Players.UI;
public class TipTranslations : PropertiesTranslationCollection
{
    protected override string FileName => "Tips";

    [TranslationData("Sent to tell a player that their vehicle was resupplied.")]
    public readonly Translation<VehicleType> LogisticsVehicleResupplied = new Translation<VehicleType>("Your <#009933>{0}</color> has been auto resupplied.", TranslationOptions.TMProUI, UppercaseAddon.Instance);

    [TranslationData("Sent to tell a player that the kit they just requested has low ammo.")]
    public readonly Translation KitGiveLowAmmo = new Translation("Low ammo. Resupply your kit at an <#e25d5d>AMMO CRATE</color>.", TranslationOptions.TMProUI);
    
    [TranslationData("Sent to tell a player how to call for a medic.")]
    public readonly Translation CallMedic = new Translation("You've been hurt, but a <#e25d5d>MEDIC</color> can revive you.", TranslationOptions.TMProUI);
    
    [TranslationData("Sent to the driver of a vehicle when they're near a repair station that doesn't have enough build supplies.")]
    public readonly Translation RepairStationVehicleNoBuild = new Translation("<#ffab87>NO BUILD", TranslationOptions.TMProUI);
}