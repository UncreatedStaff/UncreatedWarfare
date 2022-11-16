using SDG.Unturned;
using System;
using System.Collections.Generic;
using System.Linq;
using Uncreated.Warfare.Teams;
using UnityEngine;

namespace Uncreated.Warfare.Vehicles;

public static class UCVehicleManager
{
    public static VehicleBarricadeRegion? FindRegionFromVehicleWithIndex(this InteractableVehicle vehicle, out ushort index, int subvehicleIndex = 0)
    {
#if DEBUG
        using IDisposable profiler = ProfilingUtils.StartTracking();
#endif
        if (vehicle == null)
        {
            index = ushort.MaxValue;
            return null;
        }
        for (ushort i = 0; i < BarricadeManager.vehicleRegions.Count; i++)
        {
            VehicleBarricadeRegion vehicleRegion = BarricadeManager.vehicleRegions[i];
            if (vehicleRegion.vehicle == vehicle && vehicleRegion.subvehicleIndex == subvehicleIndex)
            {
                index = i;
                return vehicleRegion;
            }
        }
        index = ushort.MaxValue;
        return null;
    }
    public static IEnumerable<InteractableVehicle> GetNearbyVehicles(Guid id, float radius, Vector3 origin)
    {
#if DEBUG
        using IDisposable profiler = ProfilingUtils.StartTracking();
#endif
        float sqrRadius = radius * radius;
        List<InteractableVehicle> vehicles = new List<InteractableVehicle>();
        List<InteractableVehicle> newvehicles = new List<InteractableVehicle>(vehicles.Count);
        VehicleManager.getVehiclesInRadius(origin, sqrRadius, vehicles);
        for (int v = 0; v < vehicles.Count; v++)
        {
            if (vehicles[v].asset.GUID == id)
                newvehicles.Add(vehicles[v]);
        }
        vehicles.Clear();
        return newvehicles;
    }
    public static int CountNearbyVehicles(Guid id, float radius, Vector3 origin)
    {
#if DEBUG
        using IDisposable profiler = ProfilingUtils.StartTracking();
#endif
        float sqrRadius = radius * radius;
        List<InteractableVehicle> vehicles = new List<InteractableVehicle>();
        int amt = 0;
        VehicleManager.getVehiclesInRadius(origin, sqrRadius, vehicles);
        for (int v = 0; v < vehicles.Count; v++)
        {
            if (vehicles[v].asset.GUID == id)
                ++amt;
        }
        vehicles.Clear();
        return amt;
    }
    public static bool IsVehicleNearby(Guid id, float radius, Vector3 origin, out InteractableVehicle vehicle)
    {
#if DEBUG
        using IDisposable profiler = ProfilingUtils.StartTracking();
#endif
        float sqrRadius = radius * radius;
        List<InteractableVehicle> vehicles = new List<InteractableVehicle>();
        VehicleManager.getVehiclesInRadius(origin, sqrRadius, vehicles);
        for (int v = 0; v < vehicles.Count; v++)
        {
            if (vehicles[v].asset.GUID == id)
            {
                vehicle = vehicles[v];
                return !vehicle.isDead;
            }
        }
        vehicles.Clear();
        vehicle = null!;
        return false;
    }
    public static IEnumerable<InteractableVehicle> GetNearbyVehicles(IEnumerable<Guid> ids, float radius, Vector3 origin)
    {
#if DEBUG
        using IDisposable profiler = ProfilingUtils.StartTracking();
#endif
        float sqrRadius = radius * radius;
        List<InteractableVehicle> vehicles = new List<InteractableVehicle>();
        List<InteractableVehicle> newvehicles = new List<InteractableVehicle>(vehicles.Count);
        VehicleManager.getVehiclesInRadius(origin, sqrRadius, vehicles);
        for (int v = 0; v < vehicles.Count; v++)
        {
            if (ids.Contains(vehicles[v].asset.GUID))
                newvehicles.Add(vehicles[v]);
        }
        vehicles.Clear();
        return newvehicles;
    }
    public static InteractableVehicle? GetNearestLogi(Vector3 point, float radius, ulong team = 0)
    {
#if DEBUG
        using IDisposable profiler = ProfilingUtils.StartTracking();
#endif
        List<InteractableVehicle> vehicles = new List<InteractableVehicle>();
        VehicleManager.getVehiclesInRadius(point, Mathf.Pow(radius, 2), vehicles);
        VehicleBay? bay = VehicleBay.GetSingletonQuick();
        if (bay == null)
            return null;
        team = TeamManager.GetGroupID(team);
        return vehicles.FirstOrDefault(v =>
            v.lockedGroup.m_SteamID == team &&
            bay.GetDataSync(v.asset.GUID) is { } vehicleData &&
            vehicleData.Type is EVehicleType.LOGISTICS or EVehicleType.HELI_TRANSPORT);
    }
}
