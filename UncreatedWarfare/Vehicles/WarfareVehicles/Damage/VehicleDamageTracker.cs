using System;
using System.Collections.Generic;
using System.Text;
using Uncreated.Warfare.Util.DamageTracking;

namespace Uncreated.Warfare.Vehicles.WarfareVehicles.Damage;
public class VehicleDamageTracker : DamageTracker
{
    public WarfareVehicle? LatestDamageInstigatorVehicle { get; private set; }
    public virtual void RecordDamage(CSteamID cSteamID, ushort damage, EDamageOrigin cause)
    {
        base.RecordDamage(cSteamID, damage, cause);
        LatestDamageInstigatorVehicle = null;
    }
    public virtual void RecordDamage(ushort damage, EDamageOrigin cause)
    {
        base.RecordDamage(damage, cause);
        LatestDamageInstigatorVehicle = null;
    }
    public void RecordDamage(CSteamID cSteamID, ushort damage, EDamageOrigin cause, WarfareVehicle instigatorVehicle)
    {
        RecordDamage(cSteamID, damage, cause);
        LatestDamageInstigatorVehicle = instigatorVehicle;
    }
}
