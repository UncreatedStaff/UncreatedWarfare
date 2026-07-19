using Uncreated.Warfare.Translations;

namespace Uncreated.Warfare.Fobs.SupplyCrates;

public class AmmoTranslations : TranslationCollection
{
    public override string Name => "Commands/Ammo";

    public readonly Translation<float, float> AmmoResuppliedKit = new Translation<float, float>("<#d1bda7>Resupplied kit. Consumed: <#e25d5d>{0} AMMO</color> <#948f8a>({1} left)</color>.", arg0Fmt: "0.#", arg1Fmt: "0.#");
    
    public readonly Translation AmmoResuppliedKitInfinite = new Translation("<#d1bda7>Resupplied kit.");
    
    public readonly Translation VehicleAutoSupply = new Translation("<#b3a6a2>Vehicle has been <#ebbda9>AUTO RESUPPLIED</color>.");
    
    public readonly Translation AmmoAlreadyFull = new Translation("<#b3a6a2>Your kit is already full on ammo.</color>");
    
    public readonly Translation AmmoNoKit = new Translation("<#b3a6a2>You don't have a kit yet. Go request one from the armory in your team's headquarters.");
    
    public readonly Translation AmmoWrongTeam = new Translation("<#b3a6a2>You cannot rearm with enemy ammunition.");
    
    
    // toasts
    [TranslationData(IsPriorityTranslation = false)]
    public Translation<float> ToastGainAmmo = new Translation<float>("<color=#e25d5d>+{0} AMMO</color>", TranslationOptions.TMProUI);

    [TranslationData(IsPriorityTranslation = false)]
    public Translation<float> ToastLoseAmmo = new Translation<float>("<color=#e25d5d>-{0} AMMO</color>", TranslationOptions.TMProUI);
    
    [TranslationData(IsPriorityTranslation = false)]
    public Translation ToastAmmoNotNearFob = new Translation("<color=#b3a6a2>No fob nearby</color>", TranslationOptions.TMProUI);
    public Translation ToastAmmoNotNearVehicle = new Translation("<color=#b3a6a2>Throw at vehicle or emplacement to resupply</color>", TranslationOptions.TMProUI);
    public Translation<float, float> ToastInsufficientAmmo = new Translation<float, float>("<#b3a6a2>Insufficient ammo: <#e25d5d>{0}/{1} AMMO</color> needed.", TranslationOptions.TMProUI);
}