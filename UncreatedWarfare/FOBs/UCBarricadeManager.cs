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
    public static BarricadeDrop? GetBarricadeFromPosition(Guid guid, Vector3 pos, float tolerance = 0.05f)
    {
        if (Regions.tryGetCoordinate(pos, out byte x, out byte y))
        {
            BarricadeRegion region = BarricadeManager.regions[x, y];
            if (tolerance == 0f)
            {
                for (int i = 0; i < region.drops.Count; ++i)
                {
                    BarricadeDrop drop = region.drops[i];
                    if (drop.asset.GUID == guid && drop.model.position == pos)
                        return drop;
                }
            }
            else
            {
                tolerance = tolerance < 0 ? -tolerance : tolerance;
                for (int i = 0; i < region.drops.Count; i++)
                {
                    BarricadeDrop drop = region.drops[i];
                    if (drop.asset.GUID != guid) continue;
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
    public static StructureDrop? GetStructureFromPosition(Guid guid, Vector3 pos, float tolerance = 0.05f)
    {
        if (Regions.tryGetCoordinate(pos, out byte x, out byte y))
        {
            StructureRegion region = StructureManager.regions[x, y];
            if (tolerance == 0f)
            {
                for (int i = 0; i < region.drops.Count; i++)
                {
                    StructureDrop drop = region.drops[i];
                    if (drop.asset.GUID == guid && drop.model.position == pos)
                        return drop;
                }
            }
            else
            {
                tolerance = tolerance < 0 ? -tolerance : tolerance;
                for (int i = 0; i < region.drops.Count; i++)
                {
                    StructureDrop drop = region.drops[i];
                    if (drop.asset.GUID != guid) continue;
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
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Vector3 GetPosition(this LevelObject @object)
    {
        if (@object.transform == null)
        {
            if (@object.placeholderTransform == null)
            {
                if (@object.skybox == null)
                {
                    return @object.interactable == null
                        ? new Vector3(float.NaN, float.NaN, float.NaN)
                        : @object.interactable.transform.position;
                }
                return @object.skybox.position;
            }
            return @object.placeholderTransform.position;
        }
        return @object.transform.position;
    }
    public static LevelObject? GetObjectFromPosition(Guid guid, Vector3 pos, float tolerance = 0.15f)
    {
        if (!float.IsNaN(pos.x) && !float.IsNaN(pos.y) && !float.IsNaN(pos.z) && Regions.tryGetCoordinate(pos, out byte x, out byte y))
        {
            List<LevelObject> region = LevelObjects.objects[x, y];
            if (tolerance == 0f)
            {
                for (int i = 0; i < region.Count; i++)
                {
                    LevelObject drop = region[i];
                    if (drop.asset.GUID == guid && drop.GetPosition() == pos)
                        return drop;
                }
            }
            else
            {
                tolerance = tolerance < 0 ? -tolerance : tolerance;
                for (int i = 0; i < region.Count; i++)
                {
                    LevelObject drop = region[i];
                    if (drop.asset.GUID != guid) continue;
                    Vector3 pos2 = drop.GetPosition() - pos;
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
    public static bool IsBarricadeNearby(Guid id, float range, Vector3 origin, out BarricadeDrop drop)
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

            drop = null!;
            return false;
        }
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
    public static IEnumerable<BarricadeDrop> NonPlantedBarricades
    {
        get
        {
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
    public static IEnumerable<BarricadeDrop> PlantedBarricades
    {
        get
        {
            for (int v = 0; v < BarricadeManager.vehicleRegions.Count; ++v)
            {
                VehicleBarricadeRegion region = BarricadeManager.vehicleRegions[v];
                for (int i = 0; i < region.drops.Count; ++i)
                    yield return region.drops[i];
            }
        }
    }
    public static IEnumerable<BarricadeDrop> AllBarricades
    {
        get
        {
            for (int v = 0; v < BarricadeManager.vehicleRegions.Count; ++v)
            {
                VehicleBarricadeRegion region = BarricadeManager.vehicleRegions[v];
                for (int i = 0; i < region.drops.Count; ++i)
                    yield return region.drops[i];
            }
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
    public static int CountBarricadesWhere(float range, Vector3 origin, Predicate<BarricadeDrop> predicate, int max = -1)
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
                    {
                        if (max <= ++rtn && max > 0)
                            return rtn;
                    }
                }
            }
            return rtn;
        }
    }
    public static int CountStructuresWhere(float range, Vector3 origin, Predicate<StructureDrop> predicate, int max = -1)
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
                StructureRegion region = StructureManager.regions[rc.x, rc.y];
                foreach (StructureDrop barricade in region.drops)
                {
                    if (predicate.Invoke(barricade))
                    {
                        if (max <= ++rtn && max > 0)
                            return rtn;
                    }
                }
            }
            return rtn;
        }
    }
    public static int CountBarricadesWhere(Predicate<BarricadeDrop> predicate, int max = -1)
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
                    {
                        if (max <= ++rtn && max > 0)
                            return rtn;
                    }
                }
            }
        }

        return rtn;
    }
    public static int CountStructuresWhere(Predicate<StructureDrop> predicate, int max = -1)
    {
#if DEBUG
        using IDisposable profiler = ProfilingUtils.StartTracking();
#endif
        int rtn = 0;
        for (int x = 0; x < Regions.WORLD_SIZE; x++)
        {
            for (int y = 0; y < Regions.WORLD_SIZE; y++)
            {
                StructureRegion region = StructureManager.regions[x, y];
                foreach (StructureDrop barricade in region.drops)
                {
                    if (predicate.Invoke(barricade))
                    {
                        if (max <= ++rtn && max > 0)
                            return rtn;
                    }
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
    public static bool IsBarricadeNearby(Guid id, float range, Vector3 origin, ulong team, out BarricadeDrop drop)
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

            drop = null!;
            return false;
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
    public static BarricadeDrop? FindBarricadeDrop(Interactable interactable)
    {
        if (BarricadeManager.regions == null)
            throw new InvalidOperationException("Barricade manager has not yet been initialized.");
#if DEBUG
        using IDisposable profiler = ProfilingUtils.StartTracking();
#endif
        Vector3 expectedPosition = interactable.transform.position;
        bool f = false;
        if (Regions.tryGetCoordinate(expectedPosition, out byte x1, out byte y1))
        {
            f = true;
            BarricadeDrop? drop = ScanBarricadeRegion(interactable, x1, y1);
            if (drop != null) return drop;
            drop = ScanBarricadeRegion(interactable, (byte)(x1 - 1), y1);
            if (drop != null) return drop;
            drop = ScanBarricadeRegion(interactable, (byte)(x1 + 1), y1);
            if (drop != null) return drop;
            drop = ScanBarricadeRegion(interactable, x1, (byte)(y1 - 1));
            if (drop != null) return drop;
            drop = ScanBarricadeRegion(interactable, x1, (byte)(y1 + 1));
            if (drop != null) return drop;
            drop = ScanBarricadeRegion(interactable, (byte)(x1 - 1), (byte)(y1 - 1));
            if (drop != null) return drop;
            drop = ScanBarricadeRegion(interactable, (byte)(x1 - 1), (byte)(y1 + 1));
            if (drop != null) return drop;
            drop = ScanBarricadeRegion(interactable, (byte)(x1 + 1), (byte)(y1 - 1));
            if (drop != null) return drop;
            drop = ScanBarricadeRegion(interactable, (byte)(x1 + 1), (byte)(y1 + 1));
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
                    if (drop.interactable == interactable)
                        return drop;
            }
        }
        for (int vr = 0; vr < BarricadeManager.vehicleRegions.Count; ++vr)
        {
            VehicleBarricadeRegion region = BarricadeManager.vehicleRegions[vr];
            for (int i = 0; i < region.drops.Count; ++i)
                if (region.drops[i].interactable == interactable)
                    return region.drops[i];
        }
        return default;
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
    public static LevelObject? FindObject(uint instanceID, Vector3 expectedPosition)
    {
        if (LevelObjects.objects == null)
            throw new InvalidOperationException("LevelObjects has not yet been initialized.");
#if DEBUG
        using IDisposable profiler = ProfilingUtils.StartTracking();
#endif
        bool f = false;
        byte x1 = byte.MaxValue, y1 = byte.MaxValue;
        if (!float.IsNaN(expectedPosition.x) && !float.IsNaN(expectedPosition.y) && !float.IsNaN(expectedPosition.z) &&
            Regions.tryGetCoordinate(expectedPosition, out x1, out y1))
        {
            f = true;
            LevelObject? drop = ScanObjectRegion(instanceID, x1, y1);
            if (drop != null) return drop;
            drop = ScanObjectRegion(instanceID, (byte)(x1 - 1), y1);
            if (drop != null) return drop;
            drop = ScanObjectRegion(instanceID, (byte)(x1 + 1), y1);
            if (drop != null) return drop;
            drop = ScanObjectRegion(instanceID, x1, (byte)(y1 - 1));
            if (drop != null) return drop;
            drop = ScanObjectRegion(instanceID, x1, (byte)(y1 + 1));
            if (drop != null) return drop;
            drop = ScanObjectRegion(instanceID, (byte)(x1 - 1), (byte)(y1 - 1));
            if (drop != null) return drop;
            drop = ScanObjectRegion(instanceID, (byte)(x1 - 1), (byte)(y1 + 1));
            if (drop != null) return drop;
            drop = ScanObjectRegion(instanceID, (byte)(x1 + 1), (byte)(y1 - 1));
            if (drop != null) return drop;
            drop = ScanObjectRegion(instanceID, (byte)(x1 + 1), (byte)(y1 + 1));
            if (drop != null) return drop;
        }
        for (int x = 0; x < Regions.WORLD_SIZE; ++x)
        {
            for (int y = 0; y < Regions.WORLD_SIZE; ++y)
            {
                if (f && x - x1 is -1 or 0 or 1 && y - y1 is -1 or 0 or 1)
                    continue;
                List<LevelObject> region = LevelObjects.objects[x, y];
                for (int i = 0; i < region.Count; i++)
                {
                    if (region[i].instanceID == instanceID)
                        return region[i];
                }
            }
        }
        return default;
    }
    public static LevelObject? FindObject(Transform transform)
    {
        if (LevelObjects.objects == null)
            throw new InvalidOperationException("LevelObjects has not yet been initialized.");
#if DEBUG
        using IDisposable profiler = ProfilingUtils.StartTracking();
#endif
        bool f = false;
        Vector3 expectedPosition = transform.position;
        if (Regions.tryGetCoordinate(expectedPosition, out byte x1, out byte y1))
        {
            f = true;
            LevelObject? drop = ScanObjectRegion(transform, x1, y1);
            if (drop != null) return drop;
            drop = ScanObjectRegion(transform, (byte)(x1 - 1), y1);
            if (drop != null) return drop;
            drop = ScanObjectRegion(transform, (byte)(x1 + 1), y1);
            if (drop != null) return drop;
            drop = ScanObjectRegion(transform, x1, (byte)(y1 - 1));
            if (drop != null) return drop;
            drop = ScanObjectRegion(transform, x1, (byte)(y1 + 1));
            if (drop != null) return drop;
            drop = ScanObjectRegion(transform, (byte)(x1 - 1), (byte)(y1 - 1));
            if (drop != null) return drop;
            drop = ScanObjectRegion(transform, (byte)(x1 - 1), (byte)(y1 + 1));
            if (drop != null) return drop;
            drop = ScanObjectRegion(transform, (byte)(x1 + 1), (byte)(y1 - 1));
            if (drop != null) return drop;
            drop = ScanObjectRegion(transform, (byte)(x1 + 1), (byte)(y1 + 1));
            if (drop != null) return drop;
        }
        for (int x = 0; x < Regions.WORLD_SIZE; ++x)
        {
            for (int y = 0; y < Regions.WORLD_SIZE; ++y)
            {
                if (f && x - x1 is -1 or 0 or 1 && y - y1 is -1 or 0 or 1)
                    continue;
                List<LevelObject> region = LevelObjects.objects[x, y];
                for (int i = 0; i < region.Count; i++)
                {
                    LevelObject obj = region[i];
                    if (transform == obj.transform || transform.IsChildOf(obj.transform))
                        return obj;
                }
            }
        }
        return default;
    }
    public static LevelObject? FindObject(uint instanceID)
    {
        if (LevelObjects.objects == null)
            throw new InvalidOperationException("LevelObjects has not yet been initialized.");
#if DEBUG
        using IDisposable profiler = ProfilingUtils.StartTracking();
#endif
        for (int x = 0; x < Regions.WORLD_SIZE; x++)
        {
            for (int y = 0; y < Regions.WORLD_SIZE; y++)
            {
                List<LevelObject> region = LevelObjects.objects[x, y];
                for (int i = 0; i < region.Count; i++)
                {
                    if (region[i].instanceID == instanceID)
                        return region[i];
                }
            }
        }
        return null;
    }
    public static LevelObject? FindObject(uint instanceID, Vector3 expectedPosition, Guid guid)
    {
        if (LevelObjects.objects == null)
            throw new InvalidOperationException("LevelObjects has not yet been initialized.");
#if DEBUG
        using IDisposable profiler = ProfilingUtils.StartTracking();
#endif
        bool f = false;
        byte x1 = byte.MaxValue, y1 = byte.MaxValue;
        if (!float.IsNaN(expectedPosition.x) && !float.IsNaN(expectedPosition.y) && !float.IsNaN(expectedPosition.z) &&
            Regions.tryGetCoordinate(expectedPosition, out x1, out y1))
        {
            f = true;
            LevelObject? drop = ScanObjectRegion(instanceID, x1, y1);
            if (drop != null) return drop.GUID == guid ? drop : null;
            drop = ScanObjectRegion(instanceID, (byte)(x1 - 1), y1);
            if (drop != null) return drop.GUID == guid ? drop : null;
            drop = ScanObjectRegion(instanceID, (byte)(x1 + 1), y1);
            if (drop != null) return drop.GUID == guid ? drop : null;
            drop = ScanObjectRegion(instanceID, x1, (byte)(y1 - 1));
            if (drop != null) return drop.GUID == guid ? drop : null;
            drop = ScanObjectRegion(instanceID, x1, (byte)(y1 + 1));
            if (drop != null) return drop.GUID == guid ? drop : null;
            drop = ScanObjectRegion(instanceID, (byte)(x1 - 1), (byte)(y1 - 1));
            if (drop != null) return drop.GUID == guid ? drop : null;
            drop = ScanObjectRegion(instanceID, (byte)(x1 - 1), (byte)(y1 + 1));
            if (drop != null) return drop.GUID == guid ? drop : null;
            drop = ScanObjectRegion(instanceID, (byte)(x1 + 1), (byte)(y1 - 1));
            if (drop != null) return drop.GUID == guid ? drop : null;
            drop = ScanObjectRegion(instanceID, (byte)(x1 + 1), (byte)(y1 + 1));
            if (drop != null) return drop.GUID == guid ? drop : null;
        }
        for (int x = 0; x < Regions.WORLD_SIZE; ++x)
        {
            for (int y = 0; y < Regions.WORLD_SIZE; ++y)
            {
                if (f && x - x1 is -1 or 0 or 1 && y - y1 is -1 or 0 or 1)
                    continue;
                List<LevelObject> region = LevelObjects.objects[x, y];
                for (int i = 0; i < region.Count; i++)
                {
                    if (region[i].instanceID == instanceID)
                        return region[i].GUID == guid ? region[i] : null;
                }
            }
        }
        return default;
    }
    public static LevelObject? FindObject(uint instanceID, Guid guid)
    {
#if DEBUG
        using IDisposable profiler = ProfilingUtils.StartTracking();
#endif
        for (int x = 0; x < Regions.WORLD_SIZE; x++)
        {
            for (int y = 0; y < Regions.WORLD_SIZE; y++)
            {
                List<LevelObject> region = LevelObjects.objects[x, y];
                for (int i = 0; i < region.Count; i++)
                {
                    if (region[i].instanceID == instanceID)
                        return region[i];
                }
            }
        }
        return null;
    }
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static BarricadeDrop? ScanBarricadeRegion(Interactable interactable, byte x, byte y)
    {
        if (x > Regions.WORLD_SIZE || y > Regions.WORLD_SIZE)
            return null;
        BarricadeRegion region = BarricadeManager.regions[x, y];
        for (int i = 0; i < region.drops.Count; i++)
        {
            if (region.drops[i].interactable == interactable)
                return region.drops[i];
        }

        return null;
    }
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static BarricadeDrop? ScanBarricadeRegion(uint instanceID, byte x, byte y)
    {
        if (x > Regions.WORLD_SIZE || y > Regions.WORLD_SIZE)
            return null;
        BarricadeRegion region = BarricadeManager.regions[x, y];
        for (int i = 0; i < region.drops.Count; i++)
        {
            if (region.drops[i].instanceID == instanceID)
                return region.drops[i];
        }

        return null;
    }
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static StructureDrop? ScanStructureRegion(uint instanceID, byte x, byte y)
    {
        if (x > Regions.WORLD_SIZE || y > Regions.WORLD_SIZE)
            return null;
        StructureRegion region = StructureManager.regions[x, y];
        for (int i = 0; i < region.drops.Count; i++)
        {
            if (region.drops[i].instanceID == instanceID)
                return region.drops[i];
        }

        return null;
    }
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static LevelObject? ScanObjectRegion(uint instanceID, byte x, byte y)
    {
        if (x > Regions.WORLD_SIZE || y > Regions.WORLD_SIZE)
            return null;
        List<LevelObject> region = LevelObjects.objects[x, y];
        for (int i = 0; i < region.Count; i++)
        {
            if (region[i].instanceID == instanceID)
                return region[i];
        }

        return null;
    }
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static LevelObject? ScanObjectRegion(Transform transform, byte x, byte y)
    {
        if (x > Regions.WORLD_SIZE || y > Regions.WORLD_SIZE)
            return null;
        List<LevelObject> region = LevelObjects.objects[x, y];
        for (int i = 0; i < region.Count; i++)
        {
            LevelObject obj = region[i];
            if (transform == obj.transform || transform.IsChildOf(obj.transform))
                return obj;
        }

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
            Regions.GatherRemoteClientConnections(x, y, ItemManager.ITEM_REGIONS), x, y, instanceId, shouldPlayEffect);
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
                        EventFunctions.OnItemRemoved(item);
                        ++ct;
                    }
                }
            }
        }
        return ct >= amount;
    }
#pragma warning restore CS0612 // Type or member is obsolete
}
