using Rocket.Unturned.Player;
using SDG.Unturned;
using System;
using System.Collections.Generic;
using System.Linq;
using Uncreated.Warfare.Teams;
using UnityEngine;

namespace Uncreated.Warfare
{
    public static class UCBarricadeManager
    {
        private static readonly List<RegionCoordinate> regionBuffer = new List<RegionCoordinate>(48);
        [Obsolete]
        public static void TryAddItemToStorage(BarricadeDrop drop, ushort itemID)
        {
            if (drop?.interactable is InteractableStorage storage)
            {
                storage.items.tryAddItem(new Item(itemID, true));
            }
        }
        public static void TryAddItemToStorage(BarricadeDrop drop, Guid item)
        {
            if (drop?.interactable is InteractableStorage storage && Assets.find(item) is ItemAsset iasset)
            {
                storage.items.tryAddItem(new Item(iasset.id, true));
            }
        }
        public static InteractableSign GetSignFromLook(UnturnedPlayer player)
        {
            Transform look = player.Player.look.aim;
            Ray ray = new Ray
            {
                direction = look.forward,
                origin = look.position
            };
            //4 units for normal reach
            if (Physics.Raycast(ray, out RaycastHit hit, 4, RayMasks.BARRICADE))
            {
                return hit.transform.GetComponent<InteractableSign>();
            }
            else
            {
                return null;
            }
        }
        public static BarricadeDrop GetSignFromInteractable(InteractableSign sign)
        {
            for (int x = 0; x < Regions.WORLD_SIZE; x++)
            {
                for (int y = 0; y < Regions.WORLD_SIZE; y++)
                {
                    BarricadeRegion region = BarricadeManager.regions[x, y];
                    if (region == null) continue;
                    for (int i = 0; i < region.drops.Count; i++)
                    {
                        if (region.drops[i].interactable is InteractableSign sign2 && sign2.gameObject == sign.gameObject)
                        {
                            return region.drops[i];
                        }
                    }
                }
            }
            return null;
        }
        public static SDG.Unturned.StructureData GetStructureDataFromLook(UnturnedPlayer player, out StructureDrop drop)
        {
            Transform structureTransform = GetTransformFromLook(player.Player.look, RayMasks.STRUCTURE);
            if (structureTransform == null)
            {
                drop = null;
                return null;
            }
            drop = StructureManager.FindStructureByRootTransform(structureTransform);
            if (drop == null)
                return null;
            return drop.GetServersideData();
        }
        public static SDG.Unturned.BarricadeData GetBarricadeDataFromLook(PlayerLook look) => GetBarricadeDataFromLook(look, out _);
        public static SDG.Unturned.BarricadeData GetBarricadeDataFromLook(PlayerLook look, out BarricadeDrop drop)
        {
            Transform barricadeTransform = GetBarricadeTransformFromLook(look);
            if (barricadeTransform == null)
            {
                drop = null;
                return null;
            }
            drop = BarricadeManager.FindBarricadeByRootTransform(barricadeTransform);
            if (drop == null)
                return null;
            return drop.GetServersideData();
        }
        public static Transform GetTransformFromLook(PlayerLook look, int Raymask) =>
            Physics.Raycast(look.aim.position, look.aim.forward, out RaycastHit hit, 4, Raymask) ? hit.transform : default;
        public static Transform GetBarricadeTransformFromLook(PlayerLook look) => GetTransformFromLook(look, RayMasks.BARRICADE);
        public static Transform GetVehicleTransformFromLook(PlayerLook look) => GetTransformFromLook(look, RayMasks.VEHICLE);
        public static T GetInteractableFromLook<T>(PlayerLook look, int Raymask = RayMasks.BARRICADE) where T : Interactable
        {
            Transform barricadeTransform = GetTransformFromLook(look, Raymask);
            if (barricadeTransform == null) return null;
            if (barricadeTransform.TryGetComponent(out T interactable))
                return interactable;
            else return null;
        }
        [Obsolete]
        public static bool IsBarricadeNearby(ushort id, float range, Vector3 origin, out BarricadeDrop drop)
        {
            float sqrRange = range * range;
            for (int x = 0; x < Regions.WORLD_SIZE; x++)
            {
                for (int y = 0; y < Regions.WORLD_SIZE; y++)
                {
                    BarricadeRegion region = BarricadeManager.regions[x, y];
                    if (region == null) continue;
                    for (int i = 0; i < region.drops.Count; i++)
                    {
                        if (region.drops[i].GetServersideData().barricade.id == id && (region.drops[i].model.position - origin).sqrMagnitude <= sqrRange)
                        {
                            drop = region.drops[i];
                            return true;
                        }
                    }
                }
            }
            drop = null;
            return false;
        }
        public static IEnumerable<BarricadeDrop> GetBarricadesByGUID(Guid ID)
        {
            List<BarricadeDrop> list = new List<BarricadeDrop>();

            for (int x = 0; x < Regions.WORLD_SIZE; x++)
            {
                for (int y = 0; y < Regions.WORLD_SIZE; y++)
                {
                    BarricadeRegion region = BarricadeManager.regions[x, y];
                    if (region == null) continue;
                    for (int i = 0; i < region.drops.Count; i++)
                    {
                        if (region.drops[i].GetServersideData().barricade.asset.GUID == ID)
                        {
                            list.Add(region.drops[i]);
                        }
                    }
                }
            }

            return list;
        }
        public static IEnumerable<BarricadeDrop> AllBarricades
        {
            get
            {
                List<BarricadeDrop> list = new List<BarricadeDrop>();
                for (int x = 0; x < Regions.WORLD_SIZE; x++)
                {
                    for (int y = 0; y < Regions.WORLD_SIZE; y++)
                    {
                        BarricadeRegion region = BarricadeManager.regions[x, y];
                        if (region == null) continue;
                        for (int i = 0; i < region.drops.Count; i++)
                        {
                            list.Add(region.drops[i]);
                        }
                    }
                }
                return list;
            }
        }
        public static List<BarricadeDrop> GetBarricadesWhere(Predicate<BarricadeDrop> predicate)
        {
            List<BarricadeDrop> list = new List<BarricadeDrop>();
            for (int x = 0; x < Regions.WORLD_SIZE; x++)
            {
                for (int y = 0; y < Regions.WORLD_SIZE; y++)
                {
                    BarricadeRegion region = BarricadeManager.regions[x, y];
                    if (region == null) continue;
                    for (int i = 0; i < region.drops.Count; i++)
                    {
                        if (predicate.Invoke(region.drops[i]))
                        {
                            list.Add(region.drops[i]);
                        }
                    }
                }
            }

            return list;
        }
        public static List<BarricadeDrop> GetBarricadesWhere(float range, Vector3 origin, Predicate<BarricadeDrop> predicate)
        {
            lock (regionBuffer)
            {
                regionBuffer.Clear();
                float sqrRange = range * range;
                List<BarricadeDrop> list = new List<BarricadeDrop>();
                Regions.getRegionsInRadius(origin, range, regionBuffer);
                for (int r = 0; r < regionBuffer.Count; r++)
                {
                    RegionCoordinate rc = regionBuffer[r];
                    BarricadeRegion region = BarricadeManager.regions[rc.x, rc.y];
                    for (int i = 0; i < region.drops.Count; i++)
                    {
                        if (predicate.Invoke(region.drops[i]))
                        {
                            list.Add(region.drops[i]);
                        }
                    }
                }
                return list;
            }
        }
        public static int CountBarricadesWhere(float range, Vector3 origin, Predicate<BarricadeDrop> predicate)
        {
            lock (regionBuffer)
            {
                regionBuffer.Clear();
                float sqrRange = range * range;
                int rtn = 0;
                Regions.getRegionsInRadius(origin, range, regionBuffer);
                for (int r = 0; r < regionBuffer.Count; r++)
                {
                    RegionCoordinate rc = regionBuffer[r];
                    BarricadeRegion region = BarricadeManager.regions[rc.x, rc.y];
                    for (int i = 0; i < region.drops.Count; i++)
                    {
                        if (predicate.Invoke(region.drops[i]))
                        {
                            rtn++;
                        }
                    }
                }
                return rtn;
            }
        }
        public static int CountBarricadesWhere(Predicate<BarricadeDrop> predicate)
        {
            int rtn = 0;
            for (int x = 0; x < Regions.WORLD_SIZE; x++)
            {
                for (int y = 0; y < Regions.WORLD_SIZE; y++)
                {
                    BarricadeRegion region = BarricadeManager.regions[x, y];
                    if (region == null) continue;
                    for (int i = 0; i < region.drops.Count; i++)
                    {
                        if (predicate.Invoke(region.drops[i]))
                        {
                            rtn++;
                        }
                    }
                }
            }

            return rtn;
        }
        public static IEnumerable<BarricadeDrop> GetNearbyBarricades(IEnumerable<BarricadeDrop> selection, float range, Vector3 origin, bool sortClosest)
        {
            List<BarricadeDrop> list = new List<BarricadeDrop>();
            if (range == 0) return list;
            IEnumerator<BarricadeDrop> drops = selection.GetEnumerator();
            float sqrRange = range * range;
            while (drops.MoveNext())
            {
                if ((drops.Current.model.position - origin).sqrMagnitude <= sqrRange)
                {
                    list.Add(drops.Current);
                }
            }
            drops.Dispose();

            return sortClosest ? list.OrderBy(x => (origin - x.model.position).sqrMagnitude) : list as IEnumerable<BarricadeDrop>;
        }
        public static IEnumerable<BarricadeDrop> GetNearbyBarricades(Guid id, float range, Vector3 origin, bool sortClosest)
        {
            lock (regionBuffer)
            {
                regionBuffer.Clear();
                float sqrRange = range * range;
                List<BarricadeDrop> list = new List<BarricadeDrop>();
                Regions.getRegionsInRadius(origin, range, regionBuffer);
                for (int r = 0; r < regionBuffer.Count; r++)
                {
                    RegionCoordinate rc = regionBuffer[r];
                    BarricadeRegion region = BarricadeManager.regions[rc.x, rc.y];
                    for (int i = 0; i < region.drops.Count; i++)
                    {
                        if (region.drops[i].GetServersideData().barricade.asset.GUID == id && (region.drops[i].model.position - origin).sqrMagnitude <= sqrRange)
                        {
                            list.Add(region.drops[i]);
                        }
                    }
                }
                return sortClosest ? list.OrderBy(x => (origin - x.model.position).sqrMagnitude) : list as IEnumerable<BarricadeDrop>;
            }
        }
        public static IEnumerable<BarricadeDrop> GetNearbyBarricades(Guid id, float range, Vector3 origin, ulong team, bool sortClosest)
        {
            lock (regionBuffer)
            {
                regionBuffer.Clear();
                float sqrRange = range * range;
                //ulong group = TeamManager.GetGroupID(team);
                ulong group = team;
                L.Log($"Group: {group}");
                L.Log($"Group from GetGroupID: {TeamManager.GetGroupID(team)}");
                List<BarricadeDrop> list = new List<BarricadeDrop>();
                Regions.getRegionsInRadius(origin, range, regionBuffer);
                for (int r = 0; r < regionBuffer.Count; r++)
                {
                    RegionCoordinate rc = regionBuffer[r];
                    BarricadeRegion region = BarricadeManager.regions[rc.x, rc.y];
                    for (int i = 0; i < region.drops.Count; i++)
                    {
                        if (region.drops[i].GetServersideData().group == group && region.drops[i].GetServersideData().barricade.asset.GUID == id && (region.drops[i].model.position - origin).sqrMagnitude <= sqrRange)
                        {
                            list.Add(region.drops[i]);
                        }
                    }
                }
                return sortClosest ? list.OrderBy(x => (origin - x.model.position).sqrMagnitude) : list as IEnumerable<BarricadeDrop>;
            }
        }
        public static int CountNearbyBarricades(Guid id, float range, Vector3 origin, ulong team)
        {
            lock (regionBuffer)
            {
                regionBuffer.Clear();
                float sqrRange = range * range;
                int rtn = 0;
                ulong group = TeamManager.GetGroupID(team);
                Regions.getRegionsInRadius(origin, range, regionBuffer);
                for (int r = 0; r < regionBuffer.Count; r++)
                {
                    RegionCoordinate rc = regionBuffer[r];
                    BarricadeRegion region = BarricadeManager.regions[rc.x, rc.y];
                    for (int i = 0; i < region.drops.Count; i++)
                    {
                        if (region.drops[i].GetServersideData().group == group && region.drops[i].GetServersideData().barricade.asset.GUID == id && (region.drops[i].model.position - origin).sqrMagnitude <= sqrRange)
                        {
                            rtn++;
                        }
                    }
                }
                return rtn;
            }
        }
        public static bool BarricadeExists(Guid id, float range, Vector3 origin, ulong team, out BarricadeDrop drop)
        {
            lock (regionBuffer)
            {
                regionBuffer.Clear();
                float sqrRange = range * range;
                ulong group = TeamManager.GetGroupID(team);
                Regions.getRegionsInRadius(origin, range, regionBuffer);
                for (int r = 0; r < regionBuffer.Count; r++)
                {
                    RegionCoordinate rc = regionBuffer[r];
                    BarricadeRegion region = BarricadeManager.regions[rc.x, rc.y];
                    for (int i = 0; i < region.drops.Count; i++)
                    {
                        drop = region.drops[i];
                        if (drop.GetServersideData().group == group && drop.GetServersideData().barricade.asset.GUID == id && (drop.model.position - origin).sqrMagnitude <= sqrRange)
                        {
                            return true;
                        }
                    }
                }
                drop = null;
                return false;
            }
        }
        public static bool BarricadeExists(Guid id, float range, Vector3 origin, out BarricadeDrop drop)
        {
            lock (regionBuffer)
            {
                regionBuffer.Clear();
                float sqrRange = range * range;
                Regions.getRegionsInRadius(origin, range, regionBuffer);
                for (int r = 0; r < regionBuffer.Count; r++)
                {
                    RegionCoordinate rc = regionBuffer[r];
                    BarricadeRegion region = BarricadeManager.regions[rc.x, rc.y];
                    for (int i = 0; i < region.drops.Count; i++)
                    {
                        drop = region.drops[i];
                        if (drop.asset.GUID == id && (drop.model.position - origin).sqrMagnitude <= sqrRange)
                        {
                            return true;
                        }
                    }
                }
                drop = null;
                return false;
            }
        }
        public static IEnumerable<BarricadeDrop>[] GetNearbyBarricades(Guid[] ids, float range, Vector3 origin, bool sortClosest)
        {
            lock (regionBuffer)
            {
                regionBuffer.Clear();
                float sqrRange = range * range;
                IEnumerable<BarricadeDrop>[] lists = new List<BarricadeDrop>[ids.Length];
                if (ids.Length == 0 || range == 0) return lists;
                for (int i = 0; i < lists.Length; i++)
                    lists[i] = new List<BarricadeDrop>();
                Regions.getRegionsInRadius(origin, range, regionBuffer);
                for (int r2 = 0; r2 < regionBuffer.Count; r2++)
                {
                    RegionCoordinate rc = regionBuffer[r2];
                    BarricadeRegion region = BarricadeManager.regions[rc.x, rc.y];
                    for (int i = 0; i < region.drops.Count; i++)
                    {
                        if ((region.drops[i].model.position - origin).sqrMagnitude > sqrRange) continue;
                        for (int r = 0; r < ids.Length; r++)
                        {
                            if (region.drops[i].GetServersideData().barricade.asset.GUID == ids[r])
                            {
                                (lists[r] as List<BarricadeDrop>).Add(region.drops[i]);
                            }
                        }
                    }
                }
                if (sortClosest)
                    for (int i = 0; i < lists.Length; i++)
                        lists[i] = lists[i].OrderBy(x => (origin - x.model.position).sqrMagnitude);
                return lists;
            }
        }
        public static IEnumerable<BarricadeDrop>[] GetNearbyBarricades(Guid[] ids, float[] ranges, Vector3 origin, bool sortClosest)
        {
            lock (regionBuffer)
            {
                regionBuffer.Clear();
                IEnumerable<BarricadeDrop>[] lists = new List<BarricadeDrop>[ids.Length];
                if (ids.Length == 0) return lists;
                float[] sqrRanges = new float[ranges.Length];
                float maxRange = 0;
                for (int i = 0; i < ids.Length; i++)
                {
                    lists[i] = new List<BarricadeDrop>();
                    sqrRanges[i] = ranges[i] * ranges[i];
                    if (ranges[i] > maxRange) maxRange = ranges[i];
                }
                if (maxRange == 0) return lists;
                Regions.getRegionsInRadius(origin, maxRange, regionBuffer);
                for (int r2 = 0; r2 < regionBuffer.Count; r2++)
                {
                    RegionCoordinate rc = regionBuffer[r2];
                    BarricadeRegion region = BarricadeManager.regions[rc.x, rc.y];
                    for (int i = 0; i < region.drops.Count; i++)
                    {
                        for (int r = 0; r < ids.Length; r++)
                        {
                            if (region.drops[i].GetServersideData().barricade.asset.GUID == ids[r] && (region.drops[i].model.position - origin).sqrMagnitude <= sqrRanges[r])
                            {
                                (lists[r] as List<BarricadeDrop>).Add(region.drops[i]);
                            }
                        }
                    }
                }
                if (sortClosest)
                    for (int i = 0; i < lists.Length; i++)
                        lists[i] = lists[i].OrderBy(x => (origin - x.model.position).sqrMagnitude);
                return lists;
            }
        }
#pragma warning disable CS0612 // Type or member is obsolete
        [Obsolete]
        public static List<SDG.Unturned.ItemData> GetNearbyItems(ushort id, float range, Vector3 origin)
        {
            lock (regionBuffer)
            {
                regionBuffer.Clear();
                float sqrRange = range * range;
                List<SDG.Unturned.ItemData> list = new List<SDG.Unturned.ItemData>();
                Regions.getRegionsInRadius(origin, range, regionBuffer);
                for (int r = 0; r < regionBuffer.Count; r++)
                {
                    RegionCoordinate rc = regionBuffer[r];
                    ItemRegion region = ItemManager.regions[rc.x, rc.y];
                    for (int i = 0; i < region.items.Count; i++)
                    {
                        if (region.items[i].item.id == id && (region.items[i].point - origin).sqrMagnitude <= sqrRange)
                        {
                            list.Add(region.items[i]);
                        }
                    }
                }
                return list;
            }
        }
        public static List<SDG.Unturned.ItemData> GetNearbyItems(Guid id, float range, Vector3 origin)
        {
            if (Assets.find(id) is ItemAsset iasset)
            {
                return GetNearbyItems(iasset.id, range, origin);
            }
            return new List<SDG.Unturned.ItemData>();
        }
        public static List<SDG.Unturned.ItemData> GetNearbyItems(ItemAsset id, float range, Vector3 origin)
        {
            return GetNearbyItems(id.id, range, origin);
        }
        public static T GetInteractable2FromLook<T>(PlayerLook look, int Raymask = RayMasks.BARRICADE) where T : Interactable2
        {
            Transform barricadeTransform = GetTransformFromLook(look, Raymask);
            if (barricadeTransform == null) return null;
            if (barricadeTransform.TryGetComponent(out T interactable))
                return interactable;
            else return null;
        }
        [Obsolete]
        public static bool RemoveSingleItemFromStorage(InteractableStorage storage, ushort item_id)
        {
            for (byte i = 0; i < storage.items.items.Count; i++)
            {
                if (storage.items.getItem(i).item.id == item_id)
                {
                    storage.items.removeItem(i);
                    return true;
                }
            }
            return false;
        }
        public static bool RemoveSingleItemFromStorage(InteractableStorage storage, Guid id)
        {
            if (Assets.find(id) is ItemAsset iasset)
            {
                return RemoveSingleItemFromStorage(storage, iasset.id);
            }
            return false;
        }
        [Obsolete]
        public static int RemoveNumberOfItemsFromStorage(InteractableStorage storage, ushort item_id, int amount)
        {
            int counter = 0;

            for (byte i = (byte)(storage.items.getItemCount() - 1); i >= 0; i--)
            {
                if (storage.items.getItem(i).item.id == item_id)
                {
                    counter++;
                    storage.items.removeItem(i);

                    if (counter == amount)
                        return counter;
                }
            }
            return counter;
        }
        public static int RemoveNumberOfItemsFromStorage(InteractableStorage storage, Guid id, int amount)
        {
            if (Assets.find(id) is ItemAsset iasset)
            {
                return RemoveNumberOfItemsFromStorage(storage, iasset.id, amount);
            }
            return 0;
        }
        public static InteractableVehicle GetVehicleFromLook(PlayerLook look) => GetInteractableFromLook<InteractableVehicle>(look, RayMasks.VEHICLE);

        public static BarricadeDrop GetDropFromBarricadeData(SDG.Unturned.BarricadeData data)
        {
            if (Regions.tryGetCoordinate(data.point, out byte x, out byte y))
            {
                BarricadeRegion region = BarricadeManager.regions[x, y];
                for (int i = 0; i < region.drops.Count; i++)
                {
                    if (region.drops[i].instanceID == data.instanceID)
                    {
                        return region.drops[i];
                    }
                }
            }
            List<BarricadeRegion> barricadeRegions = BarricadeManager.regions.Cast<BarricadeRegion>().ToList();
            return barricadeRegions.SelectMany(brd => brd.drops).Where(d => d.instanceID == data.instanceID).FirstOrDefault();
        }
        public static SDG.Unturned.BarricadeData GetBarricadeFromInstID(uint instanceID, out BarricadeDrop drop)
        {
            for (int x = 0; x < Regions.WORLD_SIZE; x++)
            {
                for (int y = 0; y < Regions.WORLD_SIZE; y++)
                {
                    BarricadeRegion region = BarricadeManager.regions[x, y];
                    if (region == default) continue;
                    for (int i = 0; i < region.drops.Count; i++)
                    {
                        if (region.drops[i].GetServersideData().instanceID == instanceID)
                        {
                            drop = region.drops[i];
                            return region.drops[i].GetServersideData();
                        }
                    }
                }
            }
            for (int vr = 0; vr < BarricadeManager.vehicleRegions.Count; vr++)
            {
                VehicleBarricadeRegion region = BarricadeManager.vehicleRegions[vr];
                for (int i = 0; i < region.drops.Count; i++)
                {
                    if (region.drops[i].instanceID == instanceID)
                    {
                        drop = region.drops[i];
                        return region.drops[i].GetServersideData();
                    }
                }
            }
            drop = default;
            return default;
        }
        public static BarricadeDrop GetBarricadeFromInstID(uint instanceID)
        {
            for (int x = 0; x < Regions.WORLD_SIZE; x++)
            {
                for (int y = 0; y < Regions.WORLD_SIZE; y++)
                {
                    BarricadeRegion region = BarricadeManager.regions[x, y];
                    if (region == default) continue;
                    for (int i = 0; i < region.drops.Count; i++)
                    {
                        if (region.drops[i].GetServersideData().instanceID == instanceID)
                        {
                            return region.drops[i];
                        }
                    }
                }
            }
            for (int vr = 0; vr < BarricadeManager.vehicleRegions.Count; vr++)
            {
                VehicleBarricadeRegion region = BarricadeManager.vehicleRegions[vr];
                for (int i = 0; i < region.drops.Count; i++)
                {
                    if (region.drops[i].instanceID == instanceID)
                    {
                        return region.drops[i];
                    }
                }
            }
            return default;
        }
        public static SDG.Unturned.StructureData GetStructureFromInstID(uint instanceID, out StructureDrop drop)
        {
            for (int x = 0; x < Regions.WORLD_SIZE; x++)
            {
                for (int y = 0; y < Regions.WORLD_SIZE; y++)
                {
                    StructureRegion region = StructureManager.regions[x, y];
                    if (region == default) continue;
                    for (int i = 0; i < region.drops.Count; i++)
                    {
                        if (region.drops[i].GetServersideData().instanceID == instanceID)
                        {
                            drop = region.drops[i];
                            return region.drops[i].GetServersideData();
                        }
                    }
                }
            }
            drop = default;
            return default;
        }
        public static StructureDrop GetStructureFromInstID(uint instanceID)
        {
            for (int x = 0; x < Regions.WORLD_SIZE; x++)
            {
                for (int y = 0; y < Regions.WORLD_SIZE; y++)
                {
                    StructureRegion region = StructureManager.regions[x, y];
                    if (region == default) continue;
                    for (int i = 0; i < region.drops.Count; i++)
                    {
                        if (region.drops[i].GetServersideData().instanceID == instanceID)
                        {
                            return region.drops[i];
                        }
                    }
                }
            }
            return default;
        }
        public static BarricadeDrop GetBarriadeBySerializedTransform(SerializableTransform t)
        {
            if (Regions.tryGetCoordinate(t.Position, out byte x, out byte y))
            {
                BarricadeRegion region = BarricadeManager.regions[x, y];
                if (region == default) return null;
                for (int i = 0; i < region.drops.Count; i++)
                {
                    if (t == region.drops[i].model)
                    {
                        return region.drops[i];
                    }
                }
            }
            return null;
        }
        public static StructureDrop GetStructureBySerializedTransform(SerializableTransform t)
        {
            if (Regions.tryGetCoordinate(t.Position, out byte x, out byte y))
            {
                StructureRegion region = StructureManager.regions[x, y];
                if (region == default) return null;
                for (int i = 0; i < region.drops.Count; i++)
                {
                    if (t == region.drops[i].model)
                    {
                        return region.drops[i];
                    }
                }
            }
            return null;
        }

        public static bool RemoveNearbyItemsByID(Guid id, int amount, Vector3 center, float radius)
        {
            List<RegionCoordinate> regions = new List<RegionCoordinate>();
            Regions.getRegionsInRadius(center, radius, regions);
            return RemoveNearbyItemsByID(id, amount, center, radius, regions);
        }
        public static bool RemoveNearbyItemsByID(Guid id, int amount, Vector3 center, float radius, List<RegionCoordinate> search)
        {
            if (!(Assets.find(id) is ItemAsset asset))
                return false;
            float sqrRadius = radius * radius;
            if (ItemManager.regions == null || sqrRadius == 0 || sqrRadius < 0) return true;
            int removed_count = 0;
            for (int i = 0; i < search.Count; i++)
            {
                RegionCoordinate r = search[i];
                {
                    if (ItemManager.regions[r.x, r.y] != null)
                        for (int j = ItemManager.regions[r.x, r.y].items.Count - 1; j >= 0; j--)
                        {
                            if (removed_count < amount)
                            {
                                SDG.Unturned.ItemData item = ItemManager.regions[r.x, r.y].items[j];
                                if (item.item.id == asset.id && (item.point - center).sqrMagnitude <= sqrRadius)
                                {
                                    Data.SendTakeItem.Invoke(SDG.NetTransport.ENetReliability.Reliable,
                                        Regions.EnumerateClients(r.x, r.y, ItemManager.ITEM_REGIONS), r.x, r.y, item.instanceID);
                                    ItemManager.regions[r.x, r.y].items.RemoveAt(j);
                                    removed_count++;
                                }
                            }
                        }
                }
            }
            return removed_count >= amount;
        }
#pragma warning restore CS0612 // Type or member is obsolete
    }
}
