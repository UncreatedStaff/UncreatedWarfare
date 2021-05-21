using Rocket.API;
using Rocket.Unturned.Player;
using SDG.Unturned;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UncreatedWarfare.Teams;
using UnityEngine;
using static UncreatedWarfare.FOBs.FOBConfig;

namespace UncreatedWarfare.FOBs
{
    public class BuildManager
    {

        public BuildManager() { }

        public bool TryBuildFOB(BarricadeData foundation, UnturnedPlayer player)
        {
            List<RegionCoordinate> regions = new List<RegionCoordinate>();
            Regions.getRegionsInRadius(player.Position, 400, regions);

            List<InteractableVehicle> vehicles = new List<InteractableVehicle>();
            VehicleManager.getVehiclesInRadius(player.Position, 400, vehicles);

            List<BarricadeRegion> barricadeRegions = BarricadeManager.regions.Cast<BarricadeRegion>().ToList();

            List<BarricadeData> barricadeDatas = barricadeRegions.SelectMany(brd => brd.barricades).ToList();

            List<BarricadeData> NearbyBarricades = barricadeDatas.Where(b =>
                    (b.point - player.Position).sqrMagnitude <= Math.Pow(20, 2))
                .OrderBy(b => (b.point - player.Position).sqrMagnitude <= Math.Pow(20, 2))
                .ToList();

            List<BarricadeData> TotalFOBs = barricadeDatas.Where(b =>
                Data.TeamManager.IsFriendly(player, b.group) &&                         // All barricades that are friendly
                b.barricade.id == Data.FOBManager.config.FOBID                          // All barricades that are FOB structures
                ).ToList();

            if (TotalFOBs.Count >= Data.FOBManager.config.FobLimit)
            {
                player.Message("fob_error_limitreached");
                return false;
            }

            List<BarricadeData> NearbyFOBs = TotalFOBs.Where(b =>
                (b.point - player.Position).sqrMagnitude <= Math.Pow(300, 2)            // All fobs in a 300m radius
                ).OrderBy(b => (b.point - player.Position).magnitude).ToList();

            if (NearbyFOBs.Count != 0)
            {
                player.Message("fob_error_fobtooclose");
                return false;
            }

            ushort BuildID = 0;
            if (Data.TeamManager.IsTeam(player, ETeam.TEAM1))
                BuildID = Data.FOBManager.config.Team1BuildID;
            else if (Data.TeamManager.IsTeam(player, ETeam.TEAM2))
                BuildID = Data.FOBManager.config.Team2BuildID;

            List<ItemData> NearbyBuild = new List<ItemData>();

            NearbyBuild = ItemManager.regions.Cast<ItemRegion>().SelectMany(region => region.items).Where(item => ((item.point - player.Position).sqrMagnitude <= Mathf.Pow(50, 2)) && item.item.id == BuildID).ToList();

            List<InteractableVehicle> logitrucks = vehicles.Where(v => Data.FOBManager.config.LogiTruckIDs.Contains(v.id)).ToList();

            if (logitrucks == null || logitrucks.Count == 0)
            {
                player.Message("fob_error_nologi");
                return false;
            }

            if (NearbyBuild.Count < Data.FOBManager.config.FOBRequiredBuild)
            {
                player.Message("build_error_notenoughbuild", NearbyBuild.Count, Data.FOBManager.config.FOBRequiredBuild);
                return false;
            }

            RemoveNearbyItemsByID(BuildID, Data.FOBManager.config.FOBRequiredBuild, player.Position, 400, regions);

            Barricade barricade = new Barricade(Data.FOBManager.config.FOBID);

            UnityEngine.Quaternion quarternion = Quaternion.Euler(foundation.angle_x * 2, foundation.angle_y * 2, foundation.angle_z * 2);
            BarricadeManager.dropNonPlantedBarricade(barricade, foundation.point, quarternion, foundation.owner, foundation.group);

            EffectManager.sendEffect(29, EffectManager.MEDIUM, foundation.point);

            player.Message("fob_built");



            regions = new List<RegionCoordinate>();
            Regions.getRegionsInRadius(player.Position, 50, regions);
            barricadeRegions = BarricadeManager.regions.Cast<BarricadeRegion>().ToList();
            barricadeDatas = barricadeRegions.SelectMany(brd => brd.barricades).ToList();

            List<BarricadeData> FOBstructures = barricadeDatas.Where(b =>
                (b.point - player.Position).sqrMagnitude <= Math.Pow(20, 2) &&
                b.barricade.id == Data.FOBManager.config.FOBID &&
                Data.TeamManager.IsFriendly(player, b.group)
            ).ToList();

            Data.FOBManager.RegisterNewFOB(FOBstructures.FirstOrDefault());

            List<BarricadeDrop> barricadeDrops = barricadeRegions.SelectMany(brd => brd.drops).ToList();

            BarricadeDrop foundationDrop = barricadeDrops.Where(d => d.instanceID == foundation.instanceID).FirstOrDefault();

            if (BarricadeManager.tryGetInfo(foundationDrop.model, out byte x, out byte y, out ushort plant, out ushort index, out BarricadeRegion barricadeRegion))
            {
                BarricadeManager.destroyBarricade(barricadeRegion, x, y, plant, index);
            }

            Data.FOBManager.UpdateUIAll();

            return true;
        }

        public bool TryBuildAmmoCrate(BarricadeData foundation, UnturnedPlayer player)
        {
            List<RegionCoordinate> regions = new List<RegionCoordinate>();
            Regions.getRegionsInRadius(player.Position, 400, regions);

            List<BarricadeRegion> barricadeRegions = BarricadeManager.regions.Cast<BarricadeRegion>().ToList();

            List<BarricadeData> barricadeDatas = barricadeRegions.SelectMany(brd => brd.barricades).ToList();

            List<BarricadeData> NearbyBarricades = barricadeDatas.Where(b =>
                    (b.point - player.Position).sqrMagnitude <= Math.Pow(100, 2))
                .OrderBy(b => (b.point - player.Position).magnitude)
                .ToList();

            List<BarricadeData> NearbyFOBs = barricadeDatas.Where(b =>
                Data.TeamManager.IsFriendly(player, b.group) &&
                b.barricade.id == Data.FOBManager.config.FOBID &&
                (b.point - player.Position).sqrMagnitude <= Math.Pow(100, 2)
                ).OrderBy(b => (b.point - player.Position).magnitude)
                .ToList();

            List<BarricadeData> NearbyAmmoCrates = barricadeDatas.Where(b =>
                Data.TeamManager.IsFriendly(player, b.group) &&
                b.barricade.id == Data.FOBManager.config.AmmoCrateID &&
                (b.point - NearbyFOBs.FirstOrDefault().point).sqrMagnitude <= Math.Pow(100, 2)
                ).ToList();

            if (NearbyFOBs.Count == 0)
            {
                player.Message("build_error_fobtoofar");
                return false;
            }
            if (NearbyAmmoCrates.Count != 0)
            {
                player.Message("ammocrate_error_alreadyexists");
                return false;
            }

            ushort BuildID = 0;
            if (Data.TeamManager.IsTeam(player, ETeam.TEAM1))
                BuildID = Data.FOBManager.config.Team1BuildID;
            else if (Data.TeamManager.IsTeam(player, ETeam.TEAM2))
                BuildID = Data.FOBManager.config.Team2BuildID;

            List<ItemData> NearbyBuild = new List<ItemData>();

            NearbyBuild = ItemManager.regions.Cast<ItemRegion>().SelectMany(region => region.items).Where(item => ((item.point - NearbyFOBs.FirstOrDefault().point).sqrMagnitude <= Mathf.Pow(100, 2)) && item.item.id == BuildID).ToList();

            if (NearbyBuild.Count < Data.FOBManager.config.AmmoCrateRequiredBuild)
            {
                player.Message("build_error_notenoughbuild", NearbyBuild.Count, Data.FOBManager.config.AmmoCrateRequiredBuild);
                return false;
            }

            RemoveNearbyItemsByID(BuildID, Data.FOBManager.config.AmmoCrateRequiredBuild, player.Position, 400, regions);

            Barricade barricade = new Barricade(Data.FOBManager.config.AmmoCrateID);

            UnityEngine.Quaternion quarternion = Quaternion.Euler(foundation.angle_x * 2, foundation.angle_y * 2, foundation.angle_z * 2);
            BarricadeManager.dropNonPlantedBarricade(barricade, foundation.point, quarternion, foundation.owner, foundation.group);

            EffectManager.sendEffect(29, EffectManager.MEDIUM, foundation.point);

            player.Message("ammocrate_built");

            regions = new List<RegionCoordinate>();
            Regions.getRegionsInRadius(player.Position, 50, regions);
            barricadeRegions = BarricadeManager.regions.Cast<BarricadeRegion>().ToList();
            barricadeDatas = barricadeRegions.SelectMany(brd => brd.barricades).ToList();

            List<BarricadeDrop> barricadeDrops = barricadeRegions.SelectMany(brd => brd.drops).ToList();

            BarricadeDrop foundationDrop = barricadeDrops.Where(d => d.instanceID == foundation.instanceID).FirstOrDefault();

            if (BarricadeManager.tryGetInfo(foundationDrop.model, out byte x, out byte y, out ushort plant, out ushort index, out BarricadeRegion barricadeRegion))
            {
                BarricadeManager.destroyBarricade(barricadeRegion, x, y, plant, index);
            }

            return true;
        }

        public bool TryBuildRepairStation(BarricadeData foundation, UnturnedPlayer player)
        {
            List<RegionCoordinate> regions = new List<RegionCoordinate>();
            Regions.getRegionsInRadius(player.Position, 400, regions);

            List<BarricadeRegion> barricadeRegions = BarricadeManager.regions.Cast<BarricadeRegion>().ToList();

            List<BarricadeData> barricadeDatas = barricadeRegions.SelectMany(brd => brd.barricades).ToList();

            List<BarricadeData> NearbyBarricades = barricadeDatas.Where(b =>
                    (b.point - player.Position).sqrMagnitude <= Math.Pow(100, 2))
                .OrderBy(b => (b.point - player.Position).magnitude)
                .ToList();

            List<BarricadeData> NearbyFOBs = barricadeDatas.Where(b =>
                Data.TeamManager.IsFriendly(player, b.group) &&
                b.barricade.id == Data.FOBManager.config.FOBID &&
                (b.point - player.Position).sqrMagnitude <= Math.Pow(100, 2)
                ).OrderBy(b => (b.point - player.Position).magnitude)
                .ToList();

            List<BarricadeData> NearbyRepairStations = barricadeDatas.Where(b =>
                Data.TeamManager.IsFriendly(player, b.group) &&
                b.barricade.id == Data.FOBManager.config.RepairStationID &&
                (b.point - NearbyFOBs.FirstOrDefault().point).sqrMagnitude <= Math.Pow(100, 2)
                ).ToList();

            if (NearbyFOBs.Count == 0)
            {
                player.Message("build_error_fobtoofar");
                return false;
            }
            if (NearbyRepairStations.Count != 0)
            {
                player.Message("repairstation_error_alreadyexists");
                return false;
            }

            ushort BuildID = 0;
            if (Data.TeamManager.IsTeam(player, ETeam.TEAM1))
                BuildID = Data.FOBManager.config.Team1BuildID;
            else if (Data.TeamManager.IsTeam(player, ETeam.TEAM2))
                BuildID = Data.FOBManager.config.Team2BuildID;

            List<ItemData> NearbyBuild = new List<ItemData>();

            NearbyBuild = ItemManager.regions.Cast<ItemRegion>().SelectMany(region => region.items).Where(item => ((item.point - NearbyFOBs.FirstOrDefault().point).sqrMagnitude <= Mathf.Pow(100, 2)) && item.item.id == BuildID).ToList();

            if (NearbyBuild.Count < Data.FOBManager.config.RepairStationRequiredBuild)
            {
                player.Message("build_error_notenoughbuild", NearbyBuild.Count, Data.FOBManager.config.RepairStationRequiredBuild);
                return false;
            }

            RemoveNearbyItemsByID(BuildID, Data.FOBManager.config.RepairStationRequiredBuild, player.Position, 400, regions);

            Barricade barricade = new Barricade(Data.FOBManager.config.RepairStationID);

            UnityEngine.Quaternion quarternion = Quaternion.Euler(foundation.angle_x * 2, foundation.angle_y * 2, foundation.angle_z * 2);
            BarricadeManager.dropNonPlantedBarricade(barricade, foundation.point, quarternion, foundation.owner, foundation.group);

            EffectManager.sendEffect(29, EffectManager.MEDIUM, foundation.point);

            player.Message("repairstation_built");

            regions = new List<RegionCoordinate>();
            Regions.getRegionsInRadius(player.Position, 50, regions);
            barricadeRegions = BarricadeManager.regions.Cast<BarricadeRegion>().ToList();
            barricadeDatas = barricadeRegions.SelectMany(brd => brd.barricades).ToList();

            List<BarricadeDrop> barricadeDrops = barricadeRegions.SelectMany(brd => brd.drops).ToList();

            BarricadeDrop foundationDrop = barricadeDrops.Where(d => d.instanceID == foundation.instanceID).FirstOrDefault();

            if (BarricadeManager.tryGetInfo(foundationDrop.model, out byte x, out byte y, out ushort plant, out ushort index, out BarricadeRegion barricadeRegion))
            {
                BarricadeManager.destroyBarricade(barricadeRegion, x, y, plant, index);
            }

            return true;
        }

        public bool TryBuildEmplacement(BarricadeData foundation, UnturnedPlayer player, Emplacement emplacement)
        {
            List<RegionCoordinate> regions = new List<RegionCoordinate>();
            Regions.getRegionsInRadius(player.Position, 400, regions);

            List<BarricadeRegion> barricadeRegions = BarricadeManager.regions.Cast<BarricadeRegion>().ToList();

            List<BarricadeData> barricadeDatas = barricadeRegions.SelectMany(brd => brd.barricades).ToList();

            List<BarricadeData> NearbyBarricades = barricadeDatas.Where(b =>
                    (b.point - player.Position).sqrMagnitude <= Math.Pow(100, 2))
                .OrderBy(b => (b.point - player.Position).magnitude)
                .ToList();

            List<BarricadeData> NearbyFOBs = barricadeDatas.Where(b =>
                Data.TeamManager.IsFriendly(player, b.group) &&
                b.barricade.id == Data.FOBManager.config.FOBID &&
                (b.point - player.Position).sqrMagnitude <= Math.Pow(100, 2)
                ).OrderBy(b => (b.point - player.Position).magnitude).ToList();

            if (NearbyFOBs.Count == 0)
            {
                player.Message("build_error_fobtoofar");
                return false;
            }

            List<InteractableVehicle> vehicles = new List<InteractableVehicle>();
            VehicleManager.getVehiclesInRadius(NearbyFOBs.FirstOrDefault().point, (float)Math.Pow(50, 2), vehicles);
            int similar_vehicles_count = vehicles.Where(v => v.id == emplacement.vehicleID).Count();

            int allowed_vehicles = 2;
            if (emplacement.vehicleID == 38314 || emplacement.vehicleID == 38315)
                allowed_vehicles = 1;
            if (similar_vehicles_count >= allowed_vehicles)
            {
                player.Message("build_error_maxemplacements", allowed_vehicles, vehicles.FirstOrDefault().asset.vehicleName);
                return false;
            }

            ushort BuildID = 0;
            if (Data.TeamManager.IsTeam(player, ETeam.TEAM1))
                BuildID = Data.FOBManager.config.Team1BuildID;
            else if (Data.TeamManager.IsTeam(player, ETeam.TEAM2))
                BuildID = Data.FOBManager.config.Team2BuildID;

            List<ItemData> NearbyBuild = new List<ItemData>();

            NearbyBuild = ItemManager.regions.Cast<ItemRegion>().SelectMany(region => region.items).Where(item => ((item.point - NearbyFOBs.FirstOrDefault().point).sqrMagnitude <= Mathf.Pow(100, 2)) && item.item.id == BuildID).ToList();

            if (NearbyBuild.Count < emplacement.requiredBuild)
            {
                player.Message("build_error_notenoughbuild", NearbyBuild.Count, emplacement.requiredBuild);
                return false;
            }

            RemoveNearbyItemsByID(BuildID, emplacement.requiredBuild, player.Position, 400, regions);

            EffectManager.sendEffect(29, EffectManager.MEDIUM, foundation.point);

            for (int i = 0; i < emplacement.ammoAmount; i++)
                ItemManager.dropItem(new Item(emplacement.ammoID, true), player.Position, true, true, true);

            regions = new List<RegionCoordinate>();
            Regions.getRegionsInRadius(player.Position, 50, regions);
            barricadeRegions = BarricadeManager.regions.Cast<BarricadeRegion>().ToList();
            barricadeDatas = barricadeRegions.SelectMany(brd => brd.barricades).ToList();

            List<BarricadeDrop> barricadeDrops = barricadeRegions.SelectMany(brd => brd.drops).ToList();

            BarricadeDrop foundationDrop = barricadeDrops.Where(d => d.instanceID == foundation.instanceID).FirstOrDefault();

            Quaternion rotation = foundationDrop.model.rotation;
            rotation.eulerAngles = new Vector3(rotation.eulerAngles.x + 90, rotation.eulerAngles.y, rotation.eulerAngles.z);
            InteractableVehicle vehicle = VehicleManager.spawnVehicleV2(emplacement.vehicleID, new Vector3(foundation.point.x, foundation.point.y + 1, foundation.point.z), rotation);

            if (vehicle.asset.canBeLocked)
            {
                vehicle.tellLocked(player.CSteamID, player.Player.quests.groupID, true);

                VehicleManager.ReceiveVehicleLockState(vehicle.instanceID, player.CSteamID, player.Player.quests.groupID, true);
            }

            player.Message("emplacement_built", vehicle.asset.vehicleName);

            vehicle.updateVehicle();
            vehicle.updatePhysics();

            if (BarricadeManager.tryGetInfo(foundationDrop.model, out byte x, out byte y, out ushort plant, out ushort index, out BarricadeRegion barricadeRegion))
            {
                BarricadeManager.destroyBarricade(barricadeRegion, x, y, plant, index);
            }

            return true;
        }

        public bool TryBuildFortification(BarricadeData foundation, UnturnedPlayer player, Fortification fortification)
        {
            List<RegionCoordinate> regions = new List<RegionCoordinate>();
            Regions.getRegionsInRadius(player.Position, 400, regions);

            List<BarricadeRegion> barricadeRegions = BarricadeManager.regions.Cast<BarricadeRegion>().ToList();

            List<BarricadeData> barricadeDatas = barricadeRegions.SelectMany(brd => brd.barricades).ToList();

            List<BarricadeData> NearbyBarricades = barricadeDatas.Where(b =>
                    (b.point - player.Position).sqrMagnitude <= Math.Pow(100, 2))
                .OrderBy(b => (b.point - player.Position).magnitude)
                .ToList();

            List<BarricadeData> NearbyFOBs = barricadeDatas.Where(b =>
                Data.TeamManager.IsFriendly(player, b.group) &&
                b.barricade.id == Data.FOBManager.config.FOBID &&
                (b.point - player.Position).sqrMagnitude <= Math.Pow(100, 2)
                ).ToList();

            if (NearbyFOBs.Count == 0)
            {
                player.Message("build_error_fobtoofar");
                return false;
            }

            ushort BuildID = 0;
            if (Data.TeamManager.IsTeam(player, ETeam.TEAM1))
                BuildID = Data.FOBManager.config.Team1BuildID;
            else if (Data.TeamManager.IsTeam(player, ETeam.TEAM2))
                BuildID = Data.FOBManager.config.Team2BuildID;

            List<ItemData> NearbyBuild = new List<ItemData>();

            NearbyBuild = ItemManager.regions.Cast<ItemRegion>().SelectMany(region => region.items).Where(item => ((item.point - NearbyFOBs.FirstOrDefault().point).sqrMagnitude <= Mathf.Pow(100, 2)) && item.item.id == BuildID).ToList();

            if (NearbyBuild.Count < fortification.required_build)
            {
                player.Message("build_error_notenoughbuild", NearbyBuild.Count, fortification.required_build);
                return false;
            }

            RemoveNearbyItemsByID(BuildID, fortification.required_build, player.Position, 400, regions);

            Barricade barricade = new Barricade(fortification.barricade_id);

            UnityEngine.Quaternion quarternion = Quaternion.Euler(foundation.angle_x * 2, foundation.angle_y * 2, foundation.angle_z * 2);
            BarricadeManager.dropNonPlantedBarricade(barricade, foundation.point, quarternion, foundation.owner, foundation.group);

            EffectManager.sendEffect(29, EffectManager.MEDIUM, foundation.point);

            player.Message("fortification_built", barricade.asset.itemName);

            regions = new List<RegionCoordinate>();
            Regions.getRegionsInRadius(player.Position, 50, regions);
            barricadeRegions = BarricadeManager.regions.Cast<BarricadeRegion>().ToList();
            barricadeDatas = barricadeRegions.SelectMany(brd => brd.barricades).ToList();

            List<BarricadeDrop> barricadeDrops = barricadeRegions.SelectMany(brd => brd.drops).ToList();

            BarricadeDrop foundationDrop = barricadeDrops.Where(d => d.instanceID == foundation.instanceID).FirstOrDefault();

            if (BarricadeManager.tryGetInfo(foundationDrop.model, out byte x, out byte y, out ushort plant, out ushort index, out BarricadeRegion barricadeRegion))
            {
                BarricadeManager.destroyBarricade(barricadeRegion, x, y, plant, index);
            }

            return true;
        }

        public void RemoveNearbyItemsByID(ulong id, int amount, Vector3 center, float sqrRadius, List<RegionCoordinate> search)
        {
            if (ItemManager.regions == null)
            {
                return;
            }

            int removed_count = 0;

            for (int i = 0; i < search.Count; i++)
            {
                RegionCoordinate regionCoordinate = search[i];
                if (ItemManager.regions[regionCoordinate.x, regionCoordinate.y] != null)
                {
                    for (int j = 0; j < ItemManager.regions[regionCoordinate.x, regionCoordinate.y].items.Count; j++)
                    {
                        if (removed_count < amount)
                        {
                            ItemData item = ItemManager.regions[regionCoordinate.x, regionCoordinate.y].items[j];
                            if ((item.point - center).sqrMagnitude <= Mathf.Pow(20, 2) && item.item.id == id)
                            {
                                //indexes_to_remove_at.Add(j);
                                ItemManager.regions[regionCoordinate.x, regionCoordinate.y].items[j] = null;
                                ItemManager.ReceiveTakeItem(regionCoordinate.x, regionCoordinate.y, item.instanceID);
                                removed_count++;
                            }
                        }
                    }
                    ItemManager.regions[regionCoordinate.x, regionCoordinate.y].items.RemoveAll(item => item == null);
                }
            }
        }

        public static BarricadeData GetBarricadeFromLook(UnturnedPlayer player)
        {
            PlayerLook look = player.Player.look;

            Transform barricadeTransform = GetBarricadeTransformFromLook(look);

            if (barricadeTransform == null || !BarricadeManager.tryGetInfo(barricadeTransform, out var x, out var y, out var plant, out var index,
                out var region))
                return null;
            return region.barricades[index];
        }

        public static Transform GetBarricadeTransformFromLook(PlayerLook look)
        {
            return Physics.Raycast(look.aim.position, look.aim.forward, out var collision, 4, RayMasks.BLOCK_COLLISION) &&
                   Physics.Raycast(look.aim.position, look.aim.forward, out var hit, 4, RayMasks.BARRICADE) &&
                   collision.transform == hit.transform
                ? hit.transform
                : null;
        }
        public static T GetInteractableFromLook<T>(PlayerLook look) where T : Interactable
        {
            Transform barricadeTransform = GetBarricadeTransformFromLook(look);
            if (barricadeTransform == null) return null;
            if (barricadeTransform.TryGetComponent(out T interactable))
                return interactable;
            else return null;
        }
    }
}
