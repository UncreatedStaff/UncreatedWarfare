using Uncreated.Warfare.Players.Cooldowns;
using Uncreated.Warfare.Translations;

namespace Uncreated.Warfare.Fobs.SupplyCrates;

public class AmmoTranslations : PropertiesTranslationCollection
{
    protected override string FileName => "Commands/Ammo";

    public readonly Translation AmmoNoTarget = new Translation("<#ffab87>Look at an <#cedcde>AMMO CRATE</color>, <#cedcde>AMMO BAG</color> or <#cedcde>VEHICLE</color> in order to resupply.");
    
    public readonly Translation<float, float> AmmoResuppliedKit = new Translation<float, float>("<#d1bda7>Resupplied kit. Consumed: <#e25d5d>{0} AMMO</color> <#948f8a>({1} left)</color>.", arg0Fmt: "0.#", arg1Fmt: "0.#");
    
    public readonly Translation<int> AmmoResuppliedKitMain = new Translation<int>("<#d1bda7>Resupplied kit. Consumed: <#e25d5d>{0} AMMO</color>.", arg0Fmt: "0.#");
    
    public readonly Translation VehicleAutoSupply = new Translation("<#b3a6a2>Vehicle has been <#ebbda9>AUTO RESUPPLIED</color>.");
    
    public readonly Translation AmmoNotNearFOB = new Translation("<#b3a6a2>This ammo crate is not built on a friendly FOB.");
    
    public readonly Translation<float, float> AmmoInsufficient = new Translation<float, float>("<#b3a6a2>Insufficient ammo. <#e25d5d>{0}/{1} AMMO</color> needed.", arg0Fmt: "0.#", arg1Fmt: "0.#");
    public readonly Translation AmmoAlreadyFull = new Translation("<#b3a6a2>Your kit is already full on ammo.</color>");
    
    public readonly Translation AmmoNoKit = new Translation("<#b3a6a2>You don't have a kit yet. Go request one from the armory in your team's headquarters.");
    
    public readonly Translation AmmoWrongTeam = new Translation("<#b3a6a2>You cannot rearm with enemy ammunition.");
    
    public readonly Translation<Cooldown> AmmoCooldown = new Translation<Cooldown>("<#b7bab1>More <#cedcde>AMMO</color> arriving in: <color=#de95a8>{0}</color>", arg0Fmt: Cooldown.FormatTimeShort);
    
    public readonly Translation AmmoNotRifleman = new Translation("<#b7bab1>You must be a <#cedcde>RIFLEMAN</color> in order to place this <#cedcde>AMMO BAG</color>.");
    
    public readonly Translation AmmoNotNearRepairStation = new Translation("<#b3a6a2>Your vehicle must be next to a <#cedcde>REPAIR STATION</color> in order to rearm.");
    
    
    // toasts
    [TranslationData(IsPriorityTranslation = false)]
    public Translation<float> ToastGainAmmo = new Translation<float>("<color=#e25d5d>+{0} AMMO</color>", TranslationOptions.TMProUI);

    [TranslationData(IsPriorityTranslation = false)]
    public Translation<float> ToastLoseAmmo = new Translation<float>("<color=#e25d5d>-{0} AMMO</color>", TranslationOptions.TMProUI);
    
    [TranslationData(IsPriorityTranslation = false)]
    public Translation ToastAmmoNotNearFob = new Translation("<color=#b3a6a2>No fob nearby</color>", TranslationOptions.TMProUI);
    public Translation ToastAmmoNotNearVehicle = new Translation("<color=#b3a6a2>Throw at vehicle or emplacement to resupply</color>", TranslationOptions.TMProUI);
    public Translation<float, float> ToastInsufficientAmmo = new Translation<float, float>("<#b3a6a2>Insufficient ammo: <#e25d5d>{0}/{1} AMMO</color> needed.", TranslationOptions.TMProUI);

    
#if false

    public readonly Translation<VehicleData, int, int> AmmoResuppliedVehicle = new Translation<VehicleData, int, int>("<#d1bda7>Resupplied {0}. Consumed: <#e25d5d>{1} AMMO</color> <#948f8a>({2} left)</color>.", arg0Fmt: VehicleData.FormatColoredName);
    
    public readonly Translation<VehicleData, int> AmmoResuppliedVehicleMain = new Translation<VehicleData, int>("<#d1bda7>Resupplied {0}. Consumed: <#e25d5d>{1} AMMO</color>.", arg0Fmt: VehicleData.FormatColoredName);
    
    public readonly Translation AmmoVehicleCantRearm = new Translation("<#d1bda7>You cannot ressuply this vehicle.");
    
    public readonly Translation AmmoInVehicle = new Translation("<#d1bda7>You cannot ressuply this vehicle while flying, try exiting the vehicle.");
    
    public readonly Translation<VehicleData> AmmoVehicleFullAlready = new Translation<VehicleData>("<#b3a6a2>Your {0} does not need to be resupplied.", arg0Fmt: VehicleData.FormatColoredName);
    
    public readonly Translation<VehicleData> AmmoVehicleNotNearRepairStation = new Translation<VehicleData>("<#b3a6a2>Your {0} must be next to a <color=#e3d5ba>REPAIR STATION</color> in order to rearm.", arg0Fmt: VehicleData.FormatColoredName);
#endif
}