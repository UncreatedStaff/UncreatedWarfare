using SDG.Unturned;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using Uncreated.Warfare.Teams;
using UnityEngine;

namespace Uncreated.Warfare;

public static class UCBarricadeManager
{
    internal static readonly List<RegionCoordinate> RegionBuffer = new List<RegionCoordinate>(48);
    public static BarricadeDrop? GetSignFromInteractable(InteractableSign sign)
        => BarricadeManager.FindBarricadeByRootTransform(sign.transform);
    public static BarricadeDrop? GetBarricadeFromPosition(Vector3 pos, float tolerance = 0.05f)
    {
        if (Regions.tryGetCoordinate(pos, out byte x, out byte y))
        {
            BarricadeRegion region = BarricadeManager.regions[x, y];
            if (tolerance == 0f)
            {
                for (int i = 0; i < region.drops.Count; ++i)
                {
                    if (region.drops[i].model.position == pos)
                        return region.drops[i];
                }
            }
            else
            {
                tolerance = tolerance < 0 ? -tolerance : tolerance;
                for (int i = 0; i < region.drops.Count; i++)
                {
                    BarricadeDrop drop = region.drops[i];
                    Vector3 pos2 = drop.model.position - pos;
                    if (pos2.x > -tolerance && pos2.x < tolerance &&
                        pos2.y > -tolerance && pos2.y < tolerance &&
                        pos2.z > -tolerance && pos2.z < tolerance)
                    {
                        return drop;
                    }
                }
            }
        }
        return null;
    }
    public static StructureDrop? GetStructureFromPosition(Vector3 pos, float tolerance = 0.05f)
    {
        if (Regions.tryGetCoordinate(pos, out byte x, out byte y))
        {
            StructureRegion region = StructureManager.regions[x, y];
            if (tolerance == 0f)
            {
                foreach (StructureDrop drop in region.drops)
                {
                    if (drop.model.position == pos)
                        return drop;
                }
            }
            else
            {
                tolerance = tolerance < 0 ? -tolerance : tolerance;
                foreach (StructureDrop drop in region.drops)
                {
                    Vector3 pos2 = drop.model.position - pos;
                    if (pos2.x > -tolerance && pos2.x < tolerance &&
                        pos2.y > -tolerance && pos2.y < tolerance &&
                        pos2.z > -tolerance && pos2.z < tolerance)
                    {
                        return drop;
                    }
                }
            }
        }
        return null;
    }
    public static BarricadeData? GetBarricadeDataFromLook(PlayerLook look, out BarricadeDrop? drop)
    {
#if DEBUG
        using IDisposable profiler = ProfilingUtils.StartTracking();
#endif
        Transform? barricadeTransform = GetBarricadeTransformFromLook(look);
        if (barricadeTransform == null)
        {
            drop = null;
            return null;
        }
        drop = BarricadeManager.FindBarricadeByRootTransform(barricadeTransform);
        return drop?.GetServersideData();
    }
    public static Transform? GetTransformFromLook(PlayerLook look, int mask) =>
        Physics.Raycast(look.aim.position, look.aim.forward, out RaycastHit hit, 4, mask) ? hit.transform : default;
    public static Transform? GetBarricadeTransformFromLook(PlayerLook look) => GetTransformFromLook(look, RayMasks.BARRICADE);
    public static T? GetInteractableFromLook<T>(PlayerLook look, int mask = RayMasks.BARRICADE) where T : Interactable
    {
        Transform? barricadeTransform = GetTransformFromLook(look, mask);
        if (barricadeTransform == null) return null;
        return barricadeTransform.GetComponent<T>();
    }
    public static bool IsBarricadeNearby(Guid guid, float range, Vector3 origin, out BarricadeDrop drop)
    {
        float sqrRange = range * range;
        for (int x = 0; x < Regions.WORLD_SIZE; x++)
        {
            for (int y = 0; y < Regions.WORLD_SIZE; y++)
            {
                BarricadeRegion region = BarricadeManager.regions[x, y];
                foreach (BarricadeDrop drop2 in region.drops)
                {
                    if (drop2.asset.GUID == guid && (drop2.model.position - origin).sqrMagnitude <= sqrRange)
                    {
                        drop = drop2;
                        return true;
                    }
                }
            }
        }
        drop = null!;
        return false;
    }
    public static IEnumerable<BarricadeDrop> GetBarricadesByGuid(Guid guid)
    {
#if DEBUG
        using IDisposable profiler = ProfilingUtils.StartTracking();
#endif
        List<BarricadeDrop> list = new List<BarricadeDrop>();

        for (int x = 0; x < Regions.WORLD_SIZE; x++)
        {
            for (int y = 0; y < Regions.WORLD_SIZE; y++)
            {
                BarricadeRegion region = BarricadeManager.regions[x, y];
                foreach (BarricadeDrop drop in region.drops)
                {
                    if (drop.asset.GUID == guid)
                    {
                        list.Add(drop);
                    }
                }
            }
        }

        return list;
    }
    /// <summary>Only non-planted barricades.</summary>
    public static IEnumerable<BarricadeDrop> AllBarricades
    {
        get
        {
            //List<BarricadeDrop> list = new List<BarricadeDrop>();
            for (int x = 0; x < Regions.WORLD_SIZE; x++)
            {
                for (int y = 0; y < Regions.WORLD_SIZE; y++)
                {
                    BarricadeRegion region = BarricadeManager.regions[x, y];
                    foreach (BarricadeDrop barricade in region.drops)
                        yield return barricade;
                }
            }
        }
    }
    public static List<BarricadeDrop> GetBarricadesWhere(Predicate<BarricadeDrop> predicate)
    {
#if DEBUG
        using IDisposable profiler = ProfilingUtils.StartTracking();
#endif
        List<BarricadeDrop> list = new List<BarricadeDrop>();
        for (int x = 0; x < Regions.WORLD_SIZE; x++)
        {
            for (int y = 0; y < Regions.WORLD_SIZE; y++)
            {
                BarricadeRegion region = BarricadeManager.regions[x, y];
                foreach (BarricadeDrop barricade in region.drops)
                {
                    if (predicate.Invoke(barricade))
                        list.Add(barricade);
                }
            }
        }

        return list;
    }
    public static List<BarricadeDrop> GetBarricadesWhere(float range, Vector3 origin, Predicate<BarricadeDrop> predicate)
    {
        lock (RegionBuffer)
        {
#if DEBUG
            using IDisposable profiler = ProfilingUtils.StartTracking();
#endif
            RegionBuffer.Clear();
            List<BarricadeDrop> list = new List<BarricadeDrop>();
            Regions.getRegionsInRadius(origin, range, RegionBuffer);
            for (int r = 0; r < RegionBuffer.Count; r++)
            {
                RegionCoordinate rc = RegionBuffer[r];
                BarricadeRegion region = BarricadeManager.regions[rc.x, rc.y];
                foreach (BarricadeDrop barricade in region.drops)
                {
                    if (predicate.Invoke(barricade))
                        list.Add(barricade);
                }
            }
            return list;
        }
    }
    public static int CountBarricadesWhere(float range, Vector3 origin, Predicate<BarricadeDrop> predicate)
    {
        lock (RegionBuffer)
        {
#if DEBUG
            using IDisposable profiler = ProfilingUtils.StartTracking();
#endif
            RegionBuffer.Clear();
            int rtn = 0;
            Regions.getRegionsInRadius(origin, range, RegionBuffer);
            for (int r = 0; r < RegionBuffer.Count; r++)
            {
                RegionCoordinate rc = RegionBuffer[r];
                BarricadeRegion region = BarricadeManager.regions[rc.x, rc.y];
                foreach (BarricadeDrop barricade in region.drops)
                {
                    if (predicate.Invoke(barricade))
                        ++rtn;
                }
            }
            return rtn;
        }
    }
    public static int CountBarricadesWhere(Predicate<BarricadeDrop> predicate)
    {
#if DEBUG
        using IDisposable profiler = ProfilingUtils.StartTracking();
#endif
        int rtn = 0;
        for (int x = 0; x < Regions.WORLD_SIZE; x++)
        {
            for (int y = 0; y < Regions.WORLD_SIZE; y++)
            {
                BarricadeRegion region = BarricadeManager.regions[x, y];
                foreach (BarricadeDrop barricade in region.drops)
                {
                    if (predicate.Invoke(barricade))
                        ++rtn;
                }
            }
        }

        return rtn;
    }
    public static IEnumerable<BarricadeDrop> GetNearbyBarricades(IEnumerable<BarricadeDrop> selection, float range, Vector3 origin, bool sortClosest)
    {
#if DEBUG
        using IDisposable profiler = ProfilingUtils.StartTracking();
#endif
        List<BarricadeDrop> list = new List<BarricadeDrop>();
        if (range == 0) return list;
        float sqrRange = range * range;
        foreach (BarricadeDrop barricade in selection)
        {
            if (barricade == null)
                continue;
            if ((barricade.model.position - origin).sqrMagnitude <= sqrRange)
                list.Add(barricade);
        }

        return sortClosest ? list.OrderBy(x => (origin - x.model.position).sqrMagnitude) : list;
    }
    public static IEnumerable<BarricadeDrop> GetNearbyBarricades(Guid id, float range, Vector3 origin, bool sortClosest)
    {
        lock (RegionBuffer)
        {
#if DEBUG
            using IDisposable profiler = ProfilingUtils.StartTracking();
#endif
            RegionBuffer.Clear();
            float sqrRange = range * range;
            List<BarricadeDrop> list = new List<BarricadeDrop>();
            Regions.getRegionsInRadius(origin, range, RegionBuffer);
            for (int r = 0; r < RegionBuffer.Count; r++)
            {
                RegionCoordinate rc = RegionBuffer[r];
                BarricadeRegion region = BarricadeManager.regions[rc.x, rc.y];
                foreach (BarricadeDrop barricade in region.drops)
                {
                    if (barricade.asset.GUID == id && (barricade.model.position - origin).sqrMagnitude <= sqrRange)
                    {
                        list.Add(barricade);
                    }
                }
            }
            return sortClosest ? list.OrderBy(x => (origin - x.model.position).sqrMagnitude) : list;
        }
    }
    public static IEnumerable<BarricadeDrop> GetNearbyBarricades(Guid id, float range, Vector3 origin, ulong team, bool sortClosest)
    {
        lock (RegionBuffer)
        {
#if DEBUG
            using IDisposable profiler = ProfilingUtils.StartTracking();
#endif
            RegionBuffer.Clear();
            float sqrRange = range * range;
            //ulong group = TeamManager.GetGroupID(team);
            ulong group = team;
            List<BarricadeDrop> list = new List<BarricadeDrop>();
            Regions.getRegionsInRadius(origin, range, RegionBuffer);
            for (int r = 0; r < RegionBuffer.Count; r++)
            {
                RegionCoordinate rc = RegionBuffer[r];
                BarricadeRegion region = BarricadeManager.regions[rc.x, rc.y];
                foreach (BarricadeDrop barricade in region.drops)
                {
                    if (barricade.GetServersideData().group == group && barricade.asset.GUID == id && (barricade.model.position - origin).sqrMagnitude <= sqrRange)
                    {
                        list.Add(barricade);
                    }
                }
            }
            return sortClosest ? list.OrderBy(x => (origin - x.model.position).sqrMagnitude) : list;
        }
    }
    public static int CountNearbyBarricades(Guid id, float range, Vector3 origin, ulong team)
    {
        lock (RegionBuffer)
        {
#if DEBUG
            using IDisposable profiler = ProfilingUtils.StartTracking();
#endif
            RegionBuffer.Clear();
            float sqrRange = range * range;
            int rtn = 0;
            ulong group = TeamManager.GetGroupID(team);
            Regions.getRegionsInRadius(origin, range, RegionBuffer);
            for (int r = 0; r < RegionBuffer.Count; r++)
            {
                RegionCoordinate rc = RegionBuffer[r];
                BarricadeRegion region = BarricadeManager.regions[rc.x, rc.y];
                foreach (BarricadeDrop barricade in region.drops)
                {
                    if (barricade.GetServersideData().group == group && barricade.asset.GUID == id && (barricade.model.position - origin).sqrMagnitude <= sqrRange)
                        ++rtn;
                }
            }
            return rtn;
        }
    }
    public static int CountNearbyBarricades(Guid id, float range, Vector3 origin)
    {
        lock (RegionBuffer)
        {
#if DEBUG
            using IDisposable profiler = ProfilingUtils.StartTracking();
#endif
            RegionBuffer.Clear();
            float sqrRange = range * range;
            int rtn = 0;
            Regions.getRegionsInRadius(origin, range, RegionBuffer);
            for (int r = 0; r < RegionBuffer.Count; r++)
            {
                RegionCoordinate rc = RegionBuffer[r];
                BarricadeRegion region = BarricadeManager.regions[rc.x, rc.y];
                foreach (BarricadeDrop barricade in region.drops)
                {
                    if (barricade.asset.GUID == id && (barricade.model.position - origin).sqrMagnitude <= sqrRange)
                        ++rtn;
                }
            }
            return rtn;
        }
    }
    public static bool BarricadeExists(Guid id, float range, Vector3 origin, ulong team, out BarricadeDrop? drop)
    {
        lock (RegionBuffer)
        {
#if DEBUG
            using IDisposable profiler = ProfilingUtils.StartTracking();
#endif
            RegionBuffer.Clear();
            float sqrRange = range * range;
            ulong group = TeamManager.GetGroupID(team);
            Regions.getRegionsInRadius(origin, range, RegionBuffer);
            for (int r = 0; r < RegionBuffer.Count; r++)
            {
                RegionCoordinate rc = RegionBuffer[r];
                BarricadeRegion region = BarricadeManager.regions[rc.x, rc.y];
                foreach (BarricadeDrop barricade in region.drops)
                {
                    if (barricade.GetServersideData().group == group && barricade.asset.GUID == id && (barricade.model.position - origin).sqrMagnitude <= sqrRange)
                    {
                        drop = barricade;
                        return true;
                    }
                }
            }
            drop = null;
            return false;
        }
    }
    public static bool BarricadeExists(Guid id, float range, Vector3 origin, out BarricadeDrop? drop)
    {
        lock (RegionBuffer)
        {
#if DEBUG
            using IDisposable profiler = ProfilingUtils.StartTracking();
#endif
            RegionBuffer.Clear();
            float sqrRange = range * range;
            Regions.getRegionsInRadius(origin, range, RegionBuffer);
            for (int r = 0; r < RegionBuffer.Count; r++)
            {
                RegionCoordinate rc = RegionBuffer[r];
                BarricadeRegion region = BarricadeManager.regions[rc.x, rc.y];
                foreach (BarricadeDrop barricade in region.drops)
                {
                    if (barricade.asset.GUID == id && (barricade.model.position - origin).sqrMagnitude <= sqrRange)
                    {
                        drop = barricade;
                        return true;
                    }
                }
            }
            drop = null;
            return false;
        }
    }
#pragma warning disable CS0612 // Type or member is obsolete
    [Obsolete]
    public static List<ItemData> GetNearbyItems(ushort id, float range, Vector3 origin)
    {
        lock (RegionBuffer)
        {
#if DEBUG
            using IDisposable profiler = ProfilingUtils.StartTracking();
#endif
            RegionBuffer.Clear();
            float sqrRange = range * range;
            List<ItemData> list = new List<ItemData>();
            Regions.getRegionsInRadius(origin, range, RegionBuffer);
            for (int r = 0; r < RegionBuffer.Count; r++)
            {
                RegionCoordinate rc = RegionBuffer[r];
                ItemRegion region = ItemManager.regions[rc.x, rc.y];
                foreach (ItemData item in region.items)
                {
                    if (item.item.id == id && (item.point - origin).sqrMagnitude <= sqrRange)
                    {
                        list.Add(item);
                    }
                }
            }
            return list;
        }
    }
    public static List<ItemData> GetNearbyItems(Guid guid, float range, Vector3 origin)
    {
        lock (RegionBuffer)
        {
#if DEBUG
            using IDisposable profiler = ProfilingUtils.StartTracking();
#endif
            RegionBuffer.Clear();
            float sqrRange = range * range;
            List<ItemData> list = new List<ItemData>();
            Regions.getRegionsInRadius(origin, range, RegionBuffer);
            for (int r = 0; r < RegionBuffer.Count; r++)
            {
                RegionCoordinate rc = RegionBuffer[r];
                ItemRegion region = ItemManager.regions[rc.x, rc.y];
                foreach (ItemData item in region.items)
                {
                    if ((item.point - origin).sqrMagnitude <= sqrRange && item.item.GetAsset().GUID == guid)
                    {
                        list.Add(item);
                    }
                }
            }
            return list;
        }
    }
    public static List<ItemData> GetNearbyItems(ItemAsset id, float range, Vector3 origin)
    {
        return GetNearbyItems(id.GUID, range, origin);
    }
    public static T? GetInteractable2FromLook<T>(PlayerLook look, int mask = RayMasks.BARRICADE) where T : Interactable2
    {
        Transform? barricadeTransform = GetTransformFromLook(look, mask);
        return barricadeTransform == null ? null : barricadeTransform.GetComponent<T>();
    }
    public static bool RemoveSingleItemFromStorage(InteractableStorage storage, Guid guid)
        => RemoveNumberOfItemsFromStorage(storage, guid, 1) > 0;
    public static int RemoveNumberOfItemsFromStorage(InteractableStorage storage, Guid guid, int amount)
    {
        ThreadUtil.assertIsGameThread();
        if (amount < 1)
            return 0;
#if DEBUG
        using IDisposable profiler = ProfilingUtils.StartTracking();
#endif
        int counter = 0;

        for (int i = Math.Min(storage.items.items.Count - 1, byte.MaxValue); i >= 0; --i)
        {
            if (storage.items.items[i].item.GetAsset().GUID == guid)
            {
                counter++;
                storage.items.removeItem((byte)i);

                if (counter >= amount)
                    break;
            }
        }
        return counter;
    }
    public static BarricadeDrop? FindBarricadeDrop(BarricadeData data)
    {
#if DEBUG
        using IDisposable profiler = ProfilingUtils.StartTracking();
#endif
        uint id = data.instanceID;
        if (Regions.tryGetCoordinate(data.point, out byte x, out byte y))
        {
            BarricadeRegion region = BarricadeManager.regions[x, y];
            foreach (BarricadeDrop barricade in region.drops)
            {
                if (barricade.instanceID == id)
                    return barricade;
            }
        }
        for (byte x1 = 0; x1 < Regions.WORLD_SIZE; x1++)
        {
            for (byte y1 = 0; y1 < Regions.WORLD_SIZE; y1++)
            {
                if (x1 == x && y1 == y)
                    continue;
                BarricadeRegion region = BarricadeManager.regions[x1, y1];
                foreach (BarricadeDrop barricade in region.drops)
                {
                    if (barricade.instanceID == id)
                        return barricade;
                }
            }
        }

        for (int i = 0; i < BarricadeManager.vehicleRegions.Count; ++i)
        {
            foreach (BarricadeDrop barricade in BarricadeManager.vehicleRegions[i].drops)
            {
                if (barricade.instanceID == id)
                    return barricade;
            }
        }

        return null;
    }
    public static BarricadeDrop? FindBarricade(uint instanceID)
    {
#if DEBUG
        using IDisposable profiler = ProfilingUtils.StartTracking();
#endif
        for (byte x = 0; x < Regions.WORLD_SIZE; x++)
        {
            for (byte y = 0; y < Regions.WORLD_SIZE; y++)
            {
                BarricadeRegion region = BarricadeManager.regions[x, y];
                foreach (BarricadeDrop barricade in region.drops)
                {
                    if (barricade.instanceID == instanceID)
                        return barricade;
                }
            }
        }

        for (int i = 0; i < BarricadeManager.vehicleRegions.Count; ++i)
        {
            foreach (BarricadeDrop barricade in BarricadeManager.vehicleRegions[i].drops)
            {
                if (barricade.instanceID == instanceID)
                    return barricade;
            }
        }

        return null;
    }
    public static BarricadeDrop? FindBarricade(uint instanceID, Vector3 expectedPosition)
    {
        if (BarricadeManager.regions == null)
            throw new InvalidOperationException("Barricade manager has not yet been initialized.");
#if DEBUG
        using IDisposable profiler = ProfilingUtils.StartTracking();
#endif
        bool f = false;
        if (Regions.tryGetCoordinate(expectedPosition, out byte x1, out byte y1))
        {
            f = true;
            BarricadeDrop? drop = ScanBarricadeRegion(instanceID, x1, y1);
            if (drop != null) return drop;
            drop = ScanBarricadeRegion(instanceID, (byte)(x1 - 1), y1);
            if (drop != null) return drop;
            drop = ScanBarricadeRegion(instanceID, (byte)(x1 + 1), y1);
            if (drop != null) return drop;
            drop = ScanBarricadeRegion(instanceID, x1, (byte)(y1 - 1));
            if (drop != null) return drop;
            drop = ScanBarricadeRegion(instanceID, x1, (byte)(y1 + 1));
            if (drop != null) return drop;
            drop = ScanBarricadeRegion(instanceID, (byte)(x1 - 1), (byte)(y1 - 1));
            if (drop != null) return drop;
            drop = ScanBarricadeRegion(instanceID, (byte)(x1 - 1), (byte)(y1 + 1));
            if (drop != null) return drop;
            drop = ScanBarricadeRegion(instanceID, (byte)(x1 + 1), (byte)(y1 - 1));
            if (drop != null) return drop;
            drop = ScanBarricadeRegion(instanceID, (byte)(x1 + 1), (byte)(y1 + 1));
            if (drop != null) return drop;
        }
        for (int x = 0; x < Regions.WORLD_SIZE; ++x)
        {
            for (int y = 0; y < Regions.WORLD_SIZE; ++y)
            {
                if (f && (x - x1) is -1 or 0 or 1 && (y - y1) is -1 or 0 or 1)
                    continue;
                BarricadeRegion region = BarricadeManager.regions[x, y];
                foreach (BarricadeDrop drop in region.drops)
                    if (drop.instanceID == instanceID)
                        return drop;
            }
        }
        for (int vr = 0; vr < BarricadeManager.vehicleRegions.Count; ++vr)
        {
            VehicleBarricadeRegion region = BarricadeManager.vehicleRegions[vr];
            for (int i = 0; i < region.drops.Count; ++i)
                if (region.drops[i].instanceID == instanceID)
                    return region.drops[i];
        }
        return default;
    }
    public static StructureDrop? FindStructure(uint instanceID, Vector3 expectedPosition)
    {
        if (StructureManager.regions == null)
            throw new InvalidOperationException("Structure manager has not yet been initialized.");
#if DEBUG
        using IDisposable profiler = ProfilingUtils.StartTracking();
#endif
        bool f = false;
        if (Regions.tryGetCoordinate(expectedPosition, out byte x1, out byte y1))
        {
            f = true;
            StructureDrop? drop = ScanStructureRegion(instanceID, x1, y1);
            if (drop != null) return drop;
            drop = ScanStructureRegion(instanceID, (byte)(x1 - 1), y1);
            if (drop != null) return drop;
            drop = ScanStructureRegion(instanceID, (byte)(x1 + 1), y1);
            if (drop != null) return drop;
            drop = ScanStructureRegion(instanceID, x1, (byte)(y1 - 1));
            if (drop != null) return drop;
            drop = ScanStructureRegion(instanceID, x1, (byte)(y1 + 1));
            if (drop != null) return drop;
            drop = ScanStructureRegion(instanceID, (byte)(x1 - 1), (byte)(y1 - 1));
            if (drop != null) return drop;
            drop = ScanStructureRegion(instanceID, (byte)(x1 - 1), (byte)(y1 + 1));
            if (drop != null) return drop;
            drop = ScanStructureRegion(instanceID, (byte)(x1 + 1), (byte)(y1 - 1));
            if (drop != null) return drop;
            drop = ScanStructureRegion(instanceID, (byte)(x1 + 1), (byte)(y1 + 1));
            if (drop != null) return drop;
        }
        for (int x = 0; x < Regions.WORLD_SIZE; ++x)
        {
            for (int y = 0; y < Regions.WORLD_SIZE; ++y)
            {
                if (f && x - x1 is -1 or 0 or 1 && y - y1 is -1 or 0 or 1)
                    continue;
                StructureRegion region = StructureManager.regions[x, y];
                foreach (StructureDrop drop in region.drops)
                    if (drop.instanceID == instanceID)
                        return drop;
            }
        }
        return default;
    }
    public static StructureDrop? FindStructure(uint instanceID)
    {
#if DEBUG
        using IDisposable profiler = ProfilingUtils.StartTracking();
#endif
        for (int x = 0; x < Regions.WORLD_SIZE; x++)
        {
            for (int y = 0; y < Regions.WORLD_SIZE; y++)
            {
                StructureRegion region = StructureManager.regions[x, y];
                if (region == default) continue;
                foreach (StructureDrop drop in region.drops)
                {
                    if (drop.instanceID == instanceID)
                    {
                        return drop;
                    }
                }
            }
        }
        return null;
    }
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static BarricadeDrop? ScanBarricadeRegion(uint instanceID, byte x, byte y)
    {
        if (x < 0 || y < 0 || x > Regions.WORLD_SIZE || y > Regions.WORLD_SIZE)
            return null;
        BarricadeRegion region = BarricadeManager.regions[x, y];
        foreach (BarricadeDrop drop in region.drops)
            if (drop.instanceID == instanceID)
                return drop;
        return null;
    }
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static StructureDrop? ScanStructureRegion(uint instanceID, byte x, byte y)
    {
        if (x < 0 || y < 0 || x > Regions.WORLD_SIZE || y > Regions.WORLD_SIZE)
            return null;
        StructureRegion region = StructureManager.regions[x, y];
        foreach (StructureDrop drop in region.drops)
            if (drop.instanceID == instanceID)
                return drop;
        return null;
    }
    public static bool RemoveNearbyItemsByID(Guid id, int amount, Vector3 center, float radius)
    {
        List<RegionCoordinate> regions = new List<RegionCoordinate>();
        Regions.getRegionsInRadius(center, radius, regions);
        return RemoveNearbyItemsByID(id, amount, center, radius, regions);
    }
    internal static void SendRemoveItem(byte x, byte y, uint instanceId, bool shouldPlayEffect)
    {
        ThreadUtil.assertIsGameThread();
        Data.SendDestroyItem.Invoke(SDG.NetTransport.ENetReliability.Reliable,
            Regions.EnumerateClients(x, y, ItemManager.ITEM_REGIONS), x, y, instanceId, false);
    }
    public static void DestroyItem(byte x, byte y, uint instanceId, bool shouldPlayEffect)
    {
        SendRemoveItem(x, y, instanceId, false);
        ItemRegion region = ItemManager.regions[x, y];
        for (int i = region.items.Count - 1; i >= 0; --i)
        {
            if (region.items[i].instanceID == instanceId)
            {
                region.items.RemoveAt(i);
                break;
            }
        }
    }
    public static bool RemoveNearbyItemsByID(Guid id, int amount, Vector3 center, float radius, List<RegionCoordinate> search)
    {
#if DEBUG
        using IDisposable profiler = ProfilingUtils.StartTracking();
#endif
        float sqrRadius = radius * radius;
        if (ItemManager.regions == null || sqrRadius <= 0) return true;
        int ct = 0;
        for (int i = 0; i < search.Count; i++)
        {
            RegionCoordinate r = search[i];
            ItemRegion region = ItemManager.regions[r.x, r.y];
            for (int j = region.items.Count - 1; j >= 0; j--)
            {
                if (ct < amount)
                {
                    ItemData item = region.items[j];
                    if ((item.point - center).sqrMagnitude <= sqrRadius && item.item.GetAsset().GUID == id)
                    {
                        SendRemoveItem(r.x, r.y, item.instanceID, false);
                        region.items.RemoveAt(j);
                        ++ct;
                    }
                }
            }
        }
        return ct >= amount;
    }
#pragma warning restore CS0612 // Type or member is obsolete
}
