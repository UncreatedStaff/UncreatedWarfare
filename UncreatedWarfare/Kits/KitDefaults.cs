namespace Uncreated.Warfare.Kits;

public class KitDefaults
{
    public static int? GetDefaultMinRequiredSquadMembers(Class @class) => @class switch
    {
        Class.Rifleman => null,
        Class.Crewman => null,
        Class.Pilot => null,
        Class.Squadleader => null,
        Class.Medic => 0,
        Class.Breacher => 0,
        Class.LAT => 0,
        Class.AutomaticRifleman => 3,
        Class.Grenadier => 3,
        Class.APRifleman => 3,
        Class.Marksman => 4,
        Class.MachineGunner => 4,
        Class.Sniper => 4,
        Class.HAT => 4,
        Class.CombatEngineer => 4,
        Class.SpecOps => 4,
        _ => null
    };
    public static bool GetDefaultRequiresSquad(Class @class) => @class switch
    {
        Class.Rifleman => false,
        _ => true
    };

    public static float GetDefaultRequestCooldown(Class @class) => @class switch
    {
        _ => 0f
    };

    public static bool ShouldDequipOnExitVehicle(Class @class)
    {
        return @class is Class.LAT or Class.HAT;
    }

    public static Branch GetDefaultBranch(Class @class)
        => @class switch
        {
            Class.Pilot => Branch.Airforce,
            Class.Crewman => Branch.Armor,
            _ => Branch.Infantry
        };

    public static SquadLevel GetDefaultSquadLevel(Class @class) => @class switch
    {
        _ => SquadLevel.Member
    };
}