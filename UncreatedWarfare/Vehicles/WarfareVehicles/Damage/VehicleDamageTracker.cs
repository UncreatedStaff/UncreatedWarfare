using Uncreated.Warfare.Players;
using Uncreated.Warfare.Util.DamageTracking;

namespace Uncreated.Warfare.Vehicles.WarfareVehicles.Damage;
public class VehicleDamageTracker : DamageTracker
{
    /// <summary>
    /// Used to track who most recently locked the player for the <see cref="VehicleManager.OnToggledVehicleLock"/> event.
    /// </summary>
    public WarfarePlayer? LastLockingPlayer { get; set; }

    public WarfareVehicle? LatestDamageInstigatorVehicle { get; private set; }
    public void RecordDamage(WarfarePlayer onlineEnemyGunner, WarfareVehicle instigatorVehicle, ushort damage, EDamageOrigin cause)
    {
        base.RecordDamage(onlineEnemyGunner, damage, cause);
        LatestDamageInstigatorVehicle = instigatorVehicle;
    }
    public override void RecordDamage(WarfarePlayer onlineInstigator, ushort damage, EDamageOrigin cause)
    {
        base.RecordDamage(onlineInstigator, damage, cause);
        LatestDamageInstigatorVehicle = null;
    }
    public override void RecordDamage(CSteamID cSteamID, ushort damage, EDamageOrigin cause)
    {
        base.RecordDamage(cSteamID, damage, cause);
        LatestDamageInstigatorVehicle = null;
    }
    public override void RecordDamage(EDamageOrigin cause)
    {
        base.RecordDamage(cause);
        LatestDamageInstigatorVehicle = null;
    }
}
