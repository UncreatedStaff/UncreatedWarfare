﻿namespace Uncreated.Warfare.Vehicles.Events;
#if false
internal class VehicleLockRequestedHandler : IEventListener<ChangeVehicleLockRequested>
{
    void IEventListener<ChangeVehicleLockRequested>.HandleEvent(ChangeVehicleLockRequested e, IServiceProvider serviceProvider)
    {
        if (e.IsLocking || e.Vehicle.isDead || e.Player.OnDuty())
            return;

        // unlocking

        if (!e.Vehicle.TryGetComponent(out VehicleComponent vehicleComponent))
            return;

        if (TeamManager.IsInAnyMain(e.Vehicle.transform.position) && e.Vehicle.lockedOwner.m_SteamID == e.Steam64.m_SteamID)
            return;

        e.Player.SendChat(T.UnlockVehicleNotAllowed);
        e.Cancel();
    }
}
#endif