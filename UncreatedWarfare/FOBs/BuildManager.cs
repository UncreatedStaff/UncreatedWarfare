using Rocket.API;
using Rocket.Core.Steam;
using Rocket.Unturned.Player;
using SDG.Unturned;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Uncreated.Warfare.Officers;
using Uncreated.Warfare.Stats;
using Uncreated.Warfare.Teams;
using Uncreated.Warfare.Tickets;
using Uncreated.Warfare.Vehicles;
using Uncreated.Warfare.XP;
using UnityEngine;
using static Uncreated.Warfare.FOBs.FOBConfig;
using Item = SDG.Unturned.Item;

namespace Uncreated.Warfare.FOBs
{
    public class BuildManager
    {
        public static bool TryBuildFOB(BarricadeData foundation, UnturnedPlayer player)
        {
            if (foundation == null || foundation.barricade == null || player == null || player.Player == null) return false;
            ulong team = player.GetTeam();
            // all friendly fobs on the map
            IEnumerable<BarricadeDrop> TotalFOBs = UCBarricadeManager.GetAllFobs(team);
            if (TotalFOBs.Count() >= FOBManager.config.Data.FobLimit)
            {
                player.SendChat("fob_error_limitreached");
                return false;
            }
            IEnumerable<BarricadeDrop> NearbyFOBs = UCBarricadeManager.GetNearbyBarricades(TotalFOBs, 200, player.Position, true); // get from both teams to check distance from enemy fobs too.
            if (NearbyFOBs.Count() != 0)
            {
                player.SendChat("fob_error_fobtooclose");
                return false;
            }
            ushort BuildID = 0;
            if (team == 1)
                BuildID = FOBManager.config.Data.Team1BuildID;
            else if (team == 2)
                BuildID = FOBManager.config.Data.Team2BuildID;
            List<ItemData> NearbyBuild = UCBarricadeManager.GetNearbyItems(BuildID, 50, player.Position);
            int nearbybuild = NearbyBuild.Count();
            IEnumerable<InteractableVehicle> logitrucks = UCVehicleManager.GetNearbyVehicles(FOBManager.config.Data.LogiTruckIDs, 50, player.Position);
            if (logitrucks == null || logitrucks.Count() == 0)
            {
                player.SendChat("fob_error_nologi");
                return false;
            }
            if (nearbybuild < FOBManager.config.Data.FOBRequiredBuild)
            {
                player.SendChat("build_error_notenoughbuild", nearbybuild.ToString(Data.Locale), FOBManager.config.Data.FOBRequiredBuild.ToString(Data.Locale));
                return false;
            }

            RemoveNearbyItemsByID(BuildID, FOBManager.config.Data.FOBRequiredBuild, player.Position, FOBManager.config.Data.FOBBuildPickupRadius);
            Barricade barricade = new Barricade(FOBManager.config.Data.FOBID);
            BarricadeManager.dropNonPlantedBarricade(barricade, foundation.point, Quaternion.Euler(foundation.angle_x * 2, foundation.angle_y * 2, foundation.angle_z * 2), foundation.owner, foundation.group);
            EffectManager.sendEffect(29, EffectManager.MEDIUM, foundation.point);
            player.SendChat("fob_built");
            UCPlayer ucplayer = UCPlayer.FromUnturnedPlayer(player);
            TicketManager.AwardSquadXP(ucplayer,
                60,
                XPManager.config.Data.BuiltFOBXP,
                OfficerManager.config.Data.BuiltFOBPoints,
                "xp_built_fob",
                "ofp_squad_built_fob",
                0.4F
                );

            IEnumerable<BarricadeDrop> FOBstructures = UCBarricadeManager.GetNearbyBarricades(FOBManager.config.Data.FOBID, FOBManager.config.Data.FOBBuildPickupRadius, player.Position, player.GetTeam(), true);
            FOBManager.RegisterNewFOB(FOBstructures.FirstOrDefault());
            StatsManager.ModifyStats(player.CSteamID.m_SteamID, s => s.FobsBuilt++, false);
            StatsManager.ModifyTeam(team, t => t.FobsBuilt++, false);
            BarricadeDrop foundationDrop = F.GetBarricadeFromInstID(foundation.instanceID);
            if (foundationDrop != null && Regions.tryGetCoordinate(foundationDrop.model.position, out byte x, out byte y))
            {
                BarricadeManager.destroyBarricade(foundationDrop, x, y, ushort.MaxValue);
            }
            return true;
        }

        public static bool TryBuildAmmoCrate(BarricadeData foundation, UnturnedPlayer player)
        {
            if (foundation == null || foundation.barricade == null || player == null || player.Player == null) return false;
            ulong team = player.GetTeam();
            IEnumerable<BarricadeDrop> NearbyFOBs = UCBarricadeManager.GetNearbyFOBs(player.Position, team);
            if (NearbyFOBs.Count() == 0)
            {
                player.SendChat("build_error_fobtoofar");
                return false;
            }
            BarricadeDrop nearestFOB = NearbyFOBs.FirstOrDefault();
            IEnumerable<BarricadeDrop> NearbyAmmoCrates = UCBarricadeManager.GetNearbyBarricades(FOBManager.config.Data.AmmoCrateID, 100, nearestFOB.model.position, team, true);
            if (NearbyAmmoCrates.Count() != 0)
            {
                player.SendChat("ammocrate_error_alreadyexists");
                return false;
            }
            ushort BuildID = 0;
            if (team == 1)
                BuildID = FOBManager.config.Data.Team1BuildID;
            else if (team == 2)
                BuildID = FOBManager.config.Data.Team2BuildID;
            List<ItemData> NearbyBuild = UCBarricadeManager.GetNearbyItems(BuildID, FOBManager.config.Data.FOBBuildPickupRadius, foundation.point);
            if (NearbyBuild.Count < FOBManager.config.Data.AmmoCrateRequiredBuild)
            {
                player.SendChat("build_error_notenoughbuild", NearbyBuild.Count.ToString(Data.Locale), FOBManager.config.Data.AmmoCrateRequiredBuild.ToString(Data.Locale));
                return false;
            }
            RemoveNearbyItemsByID(BuildID, FOBManager.config.Data.AmmoCrateRequiredBuild, player.Position, FOBManager.config.Data.FOBBuildPickupRadius);
            Barricade barricade = new Barricade(FOBManager.config.Data.AmmoCrateID);
            BarricadeManager.dropNonPlantedBarricade(barricade, foundation.point, Quaternion.Euler(foundation.angle_x * 2, foundation.angle_y * 2, foundation.angle_z * 2), foundation.owner, foundation.group);
            EffectManager.sendEffect(29, EffectManager.MEDIUM, foundation.point);
            player.SendChat("ammocrate_built");
            UCPlayer ucplayer = UCPlayer.FromUnturnedPlayer(player);
            TicketManager.AwardSquadXP(ucplayer,
                60,
                XPManager.config.Data.BuiltAmmoCrateXP,
                OfficerManager.config.Data.BuiltAmmoCratePoints,
                "xp_built_ammo_crate",
                "ofp_squad_built_ammo_crate",
                0.4F
                );
            BarricadeDrop foundationDrop = F.GetBarricadeFromInstID(foundation.instanceID);
            if (foundationDrop != null && Regions.tryGetCoordinate(foundationDrop.model.position, out byte x, out byte y))
            {
                BarricadeManager.destroyBarricade(foundationDrop, x, y, ushort.MaxValue);
            }
            return true;
        }

        public static bool TryBuildRepairStation(BarricadeData foundation, UnturnedPlayer player)
        {
            if (foundation == null || foundation.barricade == null || player == null || player.Player == null) return false;
            ulong team = player.GetTeam();
            IEnumerable<BarricadeDrop> NearbyFOBs = UCBarricadeManager.GetNearbyFOBs(player.Position, team);
            if (NearbyFOBs.Count() == 0)
            {
                player.SendChat("build_error_fobtoofar");
                return false;
            }
            IEnumerable<BarricadeDrop> NearbyRepairStations = UCBarricadeManager.GetNearbyBarricades(FOBManager.config.Data.RepairStationID, 100, player.Position, team, true);
            if (NearbyRepairStations.Count() != 0)
            {
                player.SendChat("repairstation_error_alreadyexists");
                return false;
            }
            ushort BuildID = 0;
            if (team == 1)
                BuildID = FOBManager.config.Data.Team1BuildID;
            else if (team == 2)
                BuildID = FOBManager.config.Data.Team2BuildID;
            List<ItemData> NearbyBuild = UCBarricadeManager.GetNearbyItems(BuildID, FOBManager.config.Data.FOBBuildPickupRadius, foundation.point);
            if (NearbyBuild.Count < FOBManager.config.Data.RepairStationRequiredBuild)
            {
                player.SendChat("build_error_notenoughbuild", NearbyBuild.Count.ToString(Data.Locale), FOBManager.config.Data.RepairStationRequiredBuild.ToString(Data.Locale));
            }
            RemoveNearbyItemsByID(BuildID, FOBManager.config.Data.RepairStationRequiredBuild, player.Position, FOBManager.config.Data.FOBBuildPickupRadius);
            Barricade barricade = new Barricade(FOBManager.config.Data.RepairStationID);
            Quaternion quarternion = Quaternion.Euler(foundation.angle_x * 2, foundation.angle_y * 2, foundation.angle_z * 2);
            BarricadeManager.dropNonPlantedBarricade(barricade, foundation.point, quarternion, foundation.owner, foundation.group);
            EffectManager.sendEffect(29, EffectManager.MEDIUM, foundation.point);
            player.SendChat("repairstation_built");
            UCPlayer ucplayer = UCPlayer.FromUnturnedPlayer(player);
            TicketManager.AwardSquadXP(ucplayer,
                60,
                XPManager.config.Data.BuiltRepairStationXP,
                OfficerManager.config.Data.BuiltRepairStationPoints,
                "xp_built_repair_station",
                "ofp_squad_built_repair_station",
                0.4F
                );
            BarricadeDrop foundationDrop = F.GetBarricadeFromInstID(foundation.instanceID);
            if (foundationDrop != null && Regions.tryGetCoordinate(foundationDrop.model.position, out byte x, out byte y))
            {
                BarricadeManager.destroyBarricade(foundationDrop, x, y, ushort.MaxValue);
            }
            return true;
        }

        public static bool TryBuildEmplacement(BarricadeData foundation, UnturnedPlayer player, Emplacement emplacement)
        {
            if (foundation == null || foundation.barricade == null || player == null || player.Player == null || emplacement == null) return false;
            ulong team = player.GetTeam();
            IEnumerable<BarricadeDrop> NearbyFOBs = UCBarricadeManager.GetNearbyFOBs(player.Position, team);
            if (NearbyFOBs.Count() == 0)
            {
                player.SendChat("build_error_fobtoofar");
                return false;
            }
            BarricadeDrop nearestFOB = NearbyFOBs.FirstOrDefault();
            List<InteractableVehicle> vehicles = new List<InteractableVehicle>();
            VehicleManager.getVehiclesInRadius(nearestFOB.model.position, 2500f, vehicles);
            int similar_vehicles_count = vehicles.Count(v => v.id == emplacement.vehicleID);
            int allowed_vehicles = 2;
            if (emplacement.vehicleID == 38314 || emplacement.vehicleID == 38315)
                allowed_vehicles = 1;
            if (similar_vehicles_count >= allowed_vehicles)
            {
                player.SendChat("build_error_maxemplacements", allowed_vehicles.ToString(Data.Locale), vehicles.FirstOrDefault().asset.vehicleName);
                return false;
            }
            ushort BuildID = 0;
            if (team == 1)
                BuildID = FOBManager.config.Data.Team1BuildID;
            else if (team == 2)
                BuildID = FOBManager.config.Data.Team2BuildID;
            List<ItemData> NearbyBuild = UCBarricadeManager.GetNearbyItems(BuildID, FOBManager.config.Data.FOBBuildPickupRadius, foundation.point);
            if (NearbyBuild.Count < emplacement.requiredBuild)
            {
                player.SendChat("build_error_notenoughbuild", NearbyBuild.Count.ToString(Data.Locale), emplacement.requiredBuild.ToString(Data.Locale));
                return false;
            }
            RemoveNearbyItemsByID(BuildID, emplacement.requiredBuild, player.Position, FOBManager.config.Data.FOBBuildPickupRadius);
            EffectManager.sendEffect(29, EffectManager.MEDIUM, foundation.point);
            for (int i = 0; i < emplacement.ammoAmount; i++)
                ItemManager.dropItem(new Item(emplacement.ammoID, true), player.Position, true, true, true);
            BarricadeDrop foundationDrop = F.GetBarricadeFromInstID(foundation.instanceID);
            Quaternion rotation = foundationDrop.model.rotation;
            rotation.eulerAngles = new Vector3(rotation.eulerAngles.x + 90, rotation.eulerAngles.y, rotation.eulerAngles.z);
            InteractableVehicle vehicle = VehicleManager.spawnVehicleV2(emplacement.vehicleID, new Vector3(foundation.point.x, foundation.point.y + 1, foundation.point.z), rotation);
            if (vehicle.asset.canBeLocked)
            {
                vehicle.tellLocked(player.CSteamID, player.Player.quests.groupID, true);

                VehicleManager.ReceiveVehicleLockState(vehicle.instanceID, player.CSteamID, player.Player.quests.groupID, true);
            }
            player.SendChat("emplacement_built", vehicle.asset.vehicleName);
            UCPlayer ucplayer = UCPlayer.FromUnturnedPlayer(player);
            TicketManager.AwardSquadXP(ucplayer,
                60,
                XPManager.config.Data.BuiltEmplacementXP,
                OfficerManager.config.Data.BuiltEmplacementPoints,
                "xp_built_emplacement",
                "ofp_squad_built_emplacement",
                0.4F
                );
            vehicle.updateVehicle();
            vehicle.updatePhysics();
            StatsManager.ModifyStats(player.CSteamID.m_SteamID, s => s.EmplacementsBuilt++, false);
            StatsManager.ModifyTeam(team, t => t.EmplacementsBuilt++, false);
            if (foundationDrop != null && Regions.tryGetCoordinate(foundationDrop.model.position, out byte x, out byte y))
            {
                BarricadeManager.destroyBarricade(foundationDrop, x, y, ushort.MaxValue);
            }
            return true;
        }

        public static bool TryBuildFortification(BarricadeData foundation, UnturnedPlayer player, Fortification fortification)
        {
            if (foundation == null || foundation.barricade == null || player == null || player.Player == null) return false;
            ulong team = player.GetTeam();
            IEnumerable<BarricadeDrop> NearbyFOBs = UCBarricadeManager.GetNearbyFOBs(player.Position, team);
            if (NearbyFOBs.Count() == 0)
            {
                player.SendChat("build_error_fobtoofar");
                return false;
            }
            BarricadeDrop nearestFOB = NearbyFOBs.FirstOrDefault();
            ushort BuildID = 0;
            if (team == 1)
                BuildID = FOBManager.config.Data.Team1BuildID;
            else if (team == 2)
                BuildID = FOBManager.config.Data.Team2BuildID;
            List<ItemData> NearbyBuild = UCBarricadeManager.GetNearbyItems(BuildID, FOBManager.config.Data.FOBBuildPickupRadius, foundation.point);
            if (NearbyBuild.Count < fortification.required_build)
            {
                player.SendChat("build_error_notenoughbuild", NearbyBuild.Count.ToString(Data.Locale), fortification.required_build.ToString(Data.Locale));
                return false;
            }
            RemoveNearbyItemsByID(BuildID, fortification.required_build, player.Position, FOBManager.config.Data.FOBBuildPickupRadius);
            Barricade barricade = new Barricade(fortification.barricade_id);
            BarricadeManager.dropNonPlantedBarricade(barricade, foundation.point, Quaternion.Euler(foundation.angle_x * 2, foundation.angle_y * 2, foundation.angle_z * 2), foundation.owner, foundation.group);
            EffectManager.sendEffect(29, EffectManager.MEDIUM, foundation.point);
            player.SendChat("fortification_built", barricade.asset.itemName);
            UCPlayer ucplayer = UCPlayer.FromUnturnedPlayer(player);
            TicketManager.AwardSquadXP(ucplayer,
                60,
                XPManager.config.Data.BuiltBarricadeXP,
                OfficerManager.config.Data.BuiltBarricadePoints,
                "xp_built_fortification",
                "ofp_squad_built_fortification",
                0.4F
                );
            StatsManager.ModifyStats(player.CSteamID.m_SteamID, s => s.FortificationsBuilt++, false);
            StatsManager.ModifyTeam(team, t => t.FortificationsBuilt++, false);
            BarricadeDrop foundationDrop = F.GetBarricadeFromInstID(foundation.instanceID);
            if (foundationDrop != null && Regions.tryGetCoordinate(foundationDrop.model.position, out byte x, out byte y))
            {
                BarricadeManager.destroyBarricade(foundationDrop, x, y, ushort.MaxValue);
            }
            return true;
        }
        public static bool RemoveNearbyItemsByID(ulong id, int amount, Vector3 center, float radius)
        {
            List<RegionCoordinate> regions = new List<RegionCoordinate>();
            Regions.getRegionsInRadius(center, radius, regions);
            return RemoveNearbyItemsByID(id, amount, center, radius, regions);
        }
        public static bool RemoveNearbyItemsByID(ulong id, int amount, Vector3 center, float radius, List<RegionCoordinate> search)
        {
            float sqrRadius = radius * radius;
            if (ItemManager.regions == null || sqrRadius == 0 || sqrRadius < 0) return true;
            int removed_count = 0;
            for (int i = 0; i < search.Count; i++)
            {
                RegionCoordinate r = search[i];
                if (ItemManager.regions[r.x, r.y] != null)
                {
                    for (int j = ItemManager.regions[r.x, r.y].items.Count - 1; j >= 0; j--)
                    {
                        if (removed_count < amount)
                        {
                            ItemData item = ItemManager.regions[r.x, r.y].items[j];
                            if ((item.point - center).sqrMagnitude <= 20 * 20 && item.item.id == id)
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
    }
}
