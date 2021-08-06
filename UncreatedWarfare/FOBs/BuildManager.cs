using Rocket.API;
using Rocket.Unturned.Player;
using SDG.Unturned;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Uncreated.Warfare.Officers;
using Uncreated.Warfare.Teams;
using Uncreated.Warfare.Tickets;
using Uncreated.Warfare.XP;
using UnityEngine;
using static Uncreated.Warfare.FOBs.FOBConfig;

namespace Uncreated.Warfare.FOBs
{
    public class BuildManager
    {

        public BuildManager() { }

        public static bool TryBuildFOB(BarricadeData foundation, UnturnedPlayer player)
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
                TeamManager.IsFriendly(player, b.group) &&                         // All barricades that are friendly
                b.barricade.id == FOBManager.config.Data.FOBID                          // All barricades that are FOB structures
                ).ToList();

            if (TotalFOBs.Count >= FOBManager.config.Data.FobLimit)
            {
                player.SendChat("fob_error_limitreached");
                return false;
            }

            List<BarricadeData> NearbyFOBs = TotalFOBs.Where(b =>
                (b.point - player.Position).sqrMagnitude <= Math.Pow(300, 2)            // All fobs in a 300m radius
                ).OrderBy(b => (b.point - player.Position).magnitude).ToList();

            if (NearbyFOBs.Count != 0)
            {
                player.SendChat("fob_error_fobtooclose");
                return false;
            }

            ushort BuildID = 0;
            if (TeamManager.IsTeam1(player))
                BuildID = FOBManager.config.Data.Team1BuildID;
            else if (TeamManager.IsTeam2(player))
                BuildID = FOBManager.config.Data.Team2BuildID;

            List<ItemData> NearbyBuild = new List<ItemData>();

            NearbyBuild = ItemManager.regions.Cast<ItemRegion>().SelectMany(region => region.items).Where(item => ((item.point - player.Position).sqrMagnitude <= Mathf.Pow(50, 2)) && item.item.id == BuildID).ToList();

            List<InteractableVehicle> logitrucks = vehicles.Where(v => FOBManager.config.Data.LogiTruckIDs.Contains(v.id)).ToList();

            if (logitrucks == null || logitrucks.Count == 0)
            {
                player.SendChat("fob_error_nologi");
                return false;
            }

            if (NearbyBuild.Count < FOBManager.config.Data.FOBRequiredBuild)
            {
                player.SendChat("build_error_notenoughbuild", NearbyBuild.Count.ToString(Data.Locale), FOBManager.config.Data.FOBRequiredBuild.ToString(Data.Locale));
                return false;
            }

            RemoveNearbyItemsByID(BuildID, FOBManager.config.Data.FOBRequiredBuild, player.Position, 400, regions);

            Barricade barricade = new Barricade(FOBManager.config.Data.FOBID);

            UnityEngine.Quaternion quarternion = Quaternion.Euler(foundation.angle_x * 2, foundation.angle_y * 2, foundation.angle_z * 2);
            BarricadeManager.dropNonPlantedBarricade(barricade, foundation.point, quarternion, foundation.owner, foundation.group);

            EffectManager.sendEffect(29, EffectManager.MEDIUM, foundation.point);

            player.SendChat("fob_built");

            var ucplayer = UCPlayer.FromUnturnedPlayer(player);
            Task.Run(async () =>
            {
                await TicketManager.AwardSquadXP(ucplayer,
                    60,
                    XPManager.config.Data.BuiltFOBXP,
                    OfficerManager.config.Data.BuiltFOBPoints,
                    "xp_built_fob",
                    "ofp_squad_built_fob",
                    0.4F
                    );
            });


            regions = new List<RegionCoordinate>();
            Regions.getRegionsInRadius(player.Position, 50, regions);
            barricadeRegions = BarricadeManager.regions.Cast<BarricadeRegion>().ToList();
            barricadeDatas = barricadeRegions.SelectMany(brd => brd.barricades).ToList();

            List<BarricadeData> FOBstructures = barricadeDatas.Where(b =>
                (b.point - player.Position).sqrMagnitude <= Math.Pow(20, 2) &&
                b.barricade.id == FOBManager.config.Data.FOBID &&
                TeamManager.IsFriendly(player, b.group)
            ).ToList();

            FOBManager.RegisterNewFOB(FOBstructures.FirstOrDefault());

            List<BarricadeDrop> barricadeDrops = barricadeRegions.SelectMany(brd => brd.drops).ToList();

            BarricadeDrop foundationDrop = barricadeDrops.Where(d => d.instanceID == foundation.instanceID).FirstOrDefault();

            if (BarricadeManager.tryGetInfo(foundationDrop.model, out byte x, out byte y, out ushort plant, out ushort index, out BarricadeRegion barricadeRegion))
            {
                BarricadeManager.destroyBarricade(barricadeRegion, x, y, plant, index);
            }

            return true;
        }

        public static bool TryBuildAmmoCrate(BarricadeData foundation, UnturnedPlayer player)
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
                TeamManager.IsFriendly(player, b.group) &&
                b.barricade.id == FOBManager.config.Data.FOBID &&
                (b.point - player.Position).sqrMagnitude <= Math.Pow(100, 2)
                ).OrderBy(b => (b.point - player.Position).magnitude)
                .ToList();

            List<BarricadeData> NearbyAmmoCrates = barricadeDatas.Where(b =>
                TeamManager.IsFriendly(player, b.group) &&
                b.barricade.id == FOBManager.config.Data.AmmoCrateID &&
                (b.point - NearbyFOBs.FirstOrDefault().point).sqrMagnitude <= Math.Pow(100, 2)
                ).ToList();

            if (NearbyFOBs.Count == 0)
            {
                player.SendChat("build_error_fobtoofar");
                return false;
            }
            if (NearbyAmmoCrates.Count != 0)
            {
                player.SendChat("ammocrate_error_alreadyexists");
                return false;
            }

            ushort BuildID = 0;
            if (TeamManager.IsTeam1(player))
                BuildID = FOBManager.config.Data.Team1BuildID;
            else if (TeamManager.IsTeam2(player))
                BuildID = FOBManager.config.Data.Team2BuildID;

            List<ItemData> NearbyBuild = new List<ItemData>();

            NearbyBuild = ItemManager.regions.Cast<ItemRegion>().SelectMany(region => region.items).Where(item => ((item.point - NearbyFOBs.FirstOrDefault().point).sqrMagnitude <= Mathf.Pow(100, 2)) && item.item.id == BuildID).ToList();

            if (NearbyBuild.Count < FOBManager.config.Data.AmmoCrateRequiredBuild)
            {
                player.SendChat("build_error_notenoughbuild", NearbyBuild.Count.ToString(Data.Locale), FOBManager.config.Data.AmmoCrateRequiredBuild.ToString(Data.Locale));
                return false;
            }

            RemoveNearbyItemsByID(BuildID, FOBManager.config.Data.AmmoCrateRequiredBuild, player.Position, 400, regions);

            Barricade barricade = new Barricade(FOBManager.config.Data.AmmoCrateID);

            Quaternion quarternion = Quaternion.Euler(foundation.angle_x * 2, foundation.angle_y * 2, foundation.angle_z * 2);
            BarricadeManager.dropNonPlantedBarricade(barricade, foundation.point, quarternion, foundation.owner, foundation.group);

            EffectManager.sendEffect(29, EffectManager.MEDIUM, foundation.point);

            player.SendChat("ammocrate_built");

            var ucplayer = UCPlayer.FromUnturnedPlayer(player);
            Task.Run(async () =>
            {
                await TicketManager.AwardSquadXP(ucplayer,
                    60,
                    XPManager.config.Data.BuiltAmmoCrateXP,
                    OfficerManager.config.Data.BuiltAmmoCratePoints,
                    "xp_built_ammo_crate",
                    "ofp_squad_built_ammo_crate",
                    0.4F
                    );
            });

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

        public static bool TryBuildRepairStation(BarricadeData foundation, UnturnedPlayer player)
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
                TeamManager.IsFriendly(player, b.group) &&
                b.barricade.id == FOBManager.config.Data.FOBID &&
                (b.point - player.Position).sqrMagnitude <= Math.Pow(100, 2)
                ).OrderBy(b => (b.point - player.Position).magnitude)
                .ToList();

            List<BarricadeData> NearbyRepairStations = barricadeDatas.Where(b =>
                TeamManager.IsFriendly(player, b.group) &&
                b.barricade.id == FOBManager.config.Data.RepairStationID &&
                (b.point - NearbyFOBs.FirstOrDefault().point).sqrMagnitude <= Math.Pow(100, 2)
                ).ToList();

            if (NearbyFOBs.Count == 0)
            {
                player.SendChat("build_error_fobtoofar");
                return false;
            }
            if (NearbyRepairStations.Count != 0)
            {
                player.SendChat("repairstation_error_alreadyexists");
                return false;
            }

            ushort BuildID = 0;
            if (TeamManager.IsTeam1(player))
                BuildID = FOBManager.config.Data.Team1BuildID;
            else if (TeamManager.IsTeam2(player))
                BuildID = FOBManager.config.Data.Team2BuildID;

            List<ItemData> NearbyBuild = new List<ItemData>();

            NearbyBuild = ItemManager.regions.Cast<ItemRegion>().SelectMany(region => region.items).Where(item => ((item.point - NearbyFOBs.FirstOrDefault().point).sqrMagnitude <= Mathf.Pow(100, 2)) && item.item.id == BuildID).ToList();

            if (NearbyBuild.Count < FOBManager.config.Data.RepairStationRequiredBuild)
            {
                player.SendChat("build_error_notenoughbuild", NearbyBuild.Count.ToString(Data.Locale), FOBManager.config.Data.RepairStationRequiredBuild.ToString(Data.Locale));
            }

            RemoveNearbyItemsByID(BuildID, FOBManager.config.Data.RepairStationRequiredBuild, player.Position, 400, regions);

            Barricade barricade = new Barricade(FOBManager.config.Data.RepairStationID);

            Quaternion quarternion = Quaternion.Euler(foundation.angle_x * 2, foundation.angle_y * 2, foundation.angle_z * 2);
            BarricadeManager.dropNonPlantedBarricade(barricade, foundation.point, quarternion, foundation.owner, foundation.group);

            EffectManager.sendEffect(29, EffectManager.MEDIUM, foundation.point);

            player.SendChat("repairstation_built");

            var ucplayer = UCPlayer.FromUnturnedPlayer(player);

            Task.Run(async () =>
            {
                await TicketManager.AwardSquadXP(ucplayer,
                    60,
                    XPManager.config.Data.BuiltRepairStationXP,
                    OfficerManager.config.Data.BuiltRepairStationPoints,
                    "xp_built_repair_station",
                    "ofp_squad_built_repair_station",
                    0.4F
                    );
            });

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

        public static bool TryBuildEmplacement(BarricadeData foundation, UnturnedPlayer player, Emplacement emplacement)
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
                TeamManager.IsFriendly(player, b.group) &&
                b.barricade.id == FOBManager.config.Data.FOBID &&
                (b.point - player.Position).sqrMagnitude <= Math.Pow(100, 2)
                ).OrderBy(b => (b.point - player.Position).magnitude).ToList();

            if (NearbyFOBs.Count == 0)
            {
                player.SendChat("build_error_fobtoofar");
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
                player.SendChat("build_error_maxemplacements", allowed_vehicles.ToString(Data.Locale), vehicles.FirstOrDefault().asset.vehicleName);
                return false;
            }

            ushort BuildID = 0;
            if (TeamManager.IsTeam1(player))
                BuildID = FOBManager.config.Data.Team1BuildID;
            else if (TeamManager.IsTeam2(player))
                BuildID = FOBManager.config.Data.Team2BuildID;

            List<ItemData> NearbyBuild = new List<ItemData>();

            NearbyBuild = ItemManager.regions.Cast<ItemRegion>().SelectMany(region => region.items).Where(item => ((item.point - NearbyFOBs.FirstOrDefault().point).sqrMagnitude <= Mathf.Pow(100, 2)) && item.item.id == BuildID).ToList();

            if (NearbyBuild.Count < emplacement.requiredBuild)
            {
                player.SendChat("build_error_notenoughbuild", NearbyBuild.Count.ToString(Data.Locale), emplacement.requiredBuild.ToString(Data.Locale));
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

            player.SendChat("emplacement_built", vehicle.asset.vehicleName);

            var ucplayer = UCPlayer.FromUnturnedPlayer(player);
            Task.Run(async () =>
            {
                await TicketManager.AwardSquadXP(ucplayer,
                    60,
                    XPManager.config.Data.BuiltEmplacementXP,
                    OfficerManager.config.Data.BuiltEmplacementPoints,
                    "xp_built_emplacement",
                    "ofp_squad_built_emplacement",
                    0.4F
                    );
            });

            vehicle.updateVehicle();
            vehicle.updatePhysics();

            if (BarricadeManager.tryGetInfo(foundationDrop.model, out byte x, out byte y, out ushort plant, out ushort index, out BarricadeRegion barricadeRegion))
            {
                BarricadeManager.destroyBarricade(barricadeRegion, x, y, plant, index);
            }

            return true;
        }

        public static bool TryBuildFortification(BarricadeData foundation, UnturnedPlayer player, Fortification fortification)
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
                TeamManager.IsFriendly(player, b.group) &&
                b.barricade.id == FOBManager.config.Data.FOBID &&
                (b.point - player.Position).sqrMagnitude <= Math.Pow(100, 2)
                ).ToList();

            if (NearbyFOBs.Count == 0)
            {
                player.SendChat("build_error_fobtoofar");
                return false;
            }

            ushort BuildID = 0;
            if (TeamManager.IsTeam1(player))
                BuildID = FOBManager.config.Data.Team1BuildID;
            else if (TeamManager.IsTeam2(player))
                BuildID = FOBManager.config.Data.Team2BuildID;

            List<ItemData> NearbyBuild = new List<ItemData>();

            NearbyBuild = ItemManager.regions.Cast<ItemRegion>().SelectMany(region => region.items).Where(item => ((item.point - NearbyFOBs.FirstOrDefault().point).sqrMagnitude <= Mathf.Pow(100, 2)) && item.item.id == BuildID).ToList();

            if (NearbyBuild.Count < fortification.required_build)
            {
                player.SendChat("build_error_notenoughbuild", NearbyBuild.Count.ToString(Data.Locale), fortification.required_build.ToString(Data.Locale));
                return false;
            }

            RemoveNearbyItemsByID(BuildID, fortification.required_build, player.Position, 400, regions);

            Barricade barricade = new Barricade(fortification.barricade_id);

            Quaternion quarternion = Quaternion.Euler(foundation.angle_x * 2, foundation.angle_y * 2, foundation.angle_z * 2);
            BarricadeManager.dropNonPlantedBarricade(barricade, foundation.point, quarternion, foundation.owner, foundation.group);

            EffectManager.sendEffect(29, EffectManager.MEDIUM, foundation.point);

            player.SendChat("fortification_built", barricade.asset.itemName);

            var ucplayer = UCPlayer.FromUnturnedPlayer(player);
            Task.Run(async () =>
            {
                await TicketManager.AwardSquadXP(ucplayer,
                    60,
                    XPManager.config.Data.BuiltBarricadeXP,
                    OfficerManager.config.Data.BuiltBarricadePoints,
                    "xp_built_fortification",
                    "ofp_squad_built_fortification",
                    0.4F
                    );
            });

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

        public static void RemoveNearbyItemsByID(ulong id, int amount, Vector3 center, float sqrRadius, List<RegionCoordinate> search)
        {
            if (ItemManager.regions == null || sqrRadius == 0)
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
                                //Data.SendTakeItem.Invoke(SDG.NetTransport.ENetReliability.Reliable, Regions.EnumerateClients(x, y, ItemManager.ITEM_REGIONS), x, y, instances[i]);
                                removed_count++;
                            }
                        }
                    }
                    ItemManager.regions[regionCoordinate.x, regionCoordinate.y].items.RemoveAll(item => item == null);
                }
            }
        }
    }
}
