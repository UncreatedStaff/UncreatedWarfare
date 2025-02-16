namespace Uncreated.Warfare.Kits;

public class KitDefaults
{
    /// <returns>The number of ammo boxes required to refill the kit based on it's <see cref="Class"/>.</returns>
    public static int GetAmmoCost(Class @class) => @class switch
    {
        Class.HAT or Class.MachineGunner or Class.CombatEngineer => 3,
        Class.LAT or Class.AutomaticRifleman or Class.Grenadier => 2,
        _ => 1
    };

    public static float GetDefaultTeamLimit(Class @class) => @class switch
    {
        Class.HAT => 0.1f,
        _ => 1f
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