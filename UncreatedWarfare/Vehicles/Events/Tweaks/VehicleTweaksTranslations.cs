using System;
using System.Collections.Generic;
using System.Text;
using Uncreated.Warfare.Kits;
using Uncreated.Warfare.Players;
using Uncreated.Warfare.Translations;

namespace Uncreated.Warfare.Vehicles.Events.Vehicles;
internal class VehicleTweaksTranslations : PropertiesTranslationCollection
{
    protected override string FileName => "Tweaks/Vehicles";

    [TranslationData("Send when a player tries to enter a crewed vehicle but does not have the required crew kit.")]
    public readonly Translation<Class> EnterVehicleWrongKit = new Translation<Class>("There are no free passenger seats in this vehicle. To enter the crew's seats, you need a <#cedcde>{0}</color> kit.");

    [TranslationData("Send when a player tries to swap to a crew seat but does not have the required crew kit.")]
    public readonly Translation<Class> SwapSeatWrongKit = new Translation<Class>("You need a <#cedcde>{0}</color> kit to enter this seat.");

    [TranslationData("Send when a player tries to swap to a crew seat but does not have the required crew kit.")]
    public readonly Translation SwapSeatCannotAbandonDriver = new Translation("You cannot abandon the driver's seat on the battlefield.");

    [TranslationData("Send when a player tries to exit an aircraft but it's too high off the ground.")]
    public readonly Translation ExitVehicleAircraftToHigh = new Translation("You cannot leave this aircraft seat because it is flying too high!.");

    [TranslationData("Send when a player tries to enter a crew seat but the vehicle is already being operated by the max number of allowed crew.")]
    public readonly Translation<int> VehicleMaxAllowedCrewReached = new Translation<int>("The vehicle is already being operated by a crew of {0}.");

    [TranslationData("Send when a player tries to enter a crew seat but its owner is still online and not in yet in the vehicle.")]
    public readonly Translation<WarfarePlayer> EnterVehicleOwnerNotInside = new Translation<WarfarePlayer>("Wait until this vehicle's owner ({0}) is in this vehicle before you can enter, or join their squad.");
}
