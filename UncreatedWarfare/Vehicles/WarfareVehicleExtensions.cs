﻿namespace Uncreated.Warfare.Vehicles;
public static class WarfareVehicleExtensions
{
    public static bool CanTransport(this WarfareVehicleInfo vehicleInfo, InteractableVehicle vehicle)
    {
        return vehicle != null && vehicleInfo.CanTransport(vehicle.passengers.Length);
    }

    public static bool CanTransport(this WarfareVehicleInfo vehicleInfo, int passengerCt)
    {
        return !IsEmplacement(vehicleInfo.Type)
               && vehicleInfo.Crew.Seats != null
               && vehicleInfo.Crew.Seats.Count < passengerCt;
    }

    public static bool IsGroundVehicle(this VehicleType type)   => !IsAircraft(type);
    public static bool IsArmor(this VehicleType type)           => type is VehicleType.APC or VehicleType.IFV or VehicleType.MBT or VehicleType.ScoutCar;
    public static bool IsLogistics(this VehicleType type)       => type is VehicleType.LogisticsGround or VehicleType.TransportAir;
    public static bool IsAircraft(this VehicleType type)        => type is VehicleType.TransportAir or VehicleType.AttackHeli or VehicleType.Jet;
    public static bool IsAssaultAircraft(this VehicleType type) => type is VehicleType.AttackHeli or VehicleType.Jet;
    public static bool IsEmplacement(this VehicleType type)     => type is VehicleType.HMG or VehicleType.ATGM or VehicleType.AA or VehicleType.Mortar;
    public static bool IsFlyingEngine(this EEngine engine)      => engine is EEngine.BLIMP or EEngine.PLANE or EEngine.HELICOPTER;
}
