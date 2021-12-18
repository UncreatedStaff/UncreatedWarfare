using SDG.Unturned;
using Steamworks;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Uncreated.Warfare.FOBs;
using Uncreated.Warfare.Gamemodes;
using Uncreated.Warfare.Officers;
using Uncreated.Warfare.Stats;
using Uncreated.Warfare.Teams;
using Uncreated.Warfare.Tickets;
using Uncreated.Warfare.Vehicles;
using Uncreated.Warfare.XP;
using UnityEngine;

namespace Uncreated.Warfare.Components
{
    internal class BuildableComponent : MonoBehaviour
    {
        public BarricadeDrop Foundation { get; private set; }
        public BuildableData Buildable { get; private set; }

        public int Hits { get; private set;}

        public void Initialize(BarricadeDrop foundation, BuildableData buildable)
        {
            Foundation = foundation;
            Buildable = buildable;

            Hits = 0;
        }

        public void IncrementBuildPoints(UCPlayer builder)
        {
            int amount = 1;
            if (builder.KitClass == EClass.COMBAT_ENGINEER)
                amount = 2;

            Hits += amount;

            //player.SendChat("fob_built");

            EffectManager.sendEffect(38405, EffectManager.MEDIUM, builder.Position);

            XPManager.AddXP(builder.Player, XPManager.config.Data.ShovelXP, Math.Round((float)Hits / Buildable.requiredHits * 100F).ToString() + "%", true);

            if (Hits >= Buildable.requiredHits)
            {
                Build();
            }
        }
        public void Build()
        {
            var data = Foundation.GetServersideData();

            if (Buildable.type != EbuildableType.EMPLACEMENT)
            {
                Barricade barricade = new Barricade(UCAssetManager.FindItemBarricadeAsset(Buildable.structureID));
                Transform transform = BarricadeManager.dropNonPlantedBarricade(barricade, data.point, Quaternion.Euler(data.angle_x * 2, data.angle_y * 2, data.angle_z * 2), data.owner, data.group);
                BarricadeDrop structure = BarricadeManager.FindBarricadeByRootTransform(transform);

                if (Buildable.type == EbuildableType.FOB)
                {
                    FOBManager.RegisterNewFOB(structure, UCWarfare.GetColorHex("default_fob_color"));
                    //StatsManager.ModifyStats(player.CSteamID.m_SteamID, s => s.FobsBuilt++, false);
                    StatsManager.ModifyTeam(data.group, t => t.FobsBuilt++, false);
                }
            }
            else
            {
                if (!(Assets.find(Buildable.emplacementData.ammoID) is ItemAsset ammoasset))
                {
                    F.LogError($"Emplacement {Assets.find(Buildable.structureID)?.name ?? Buildable.structureID.ToString("N")}'s ammo id is not a valid Item.");
                    return;
                }
                if (!(Assets.find(Buildable.structureID) is VehicleAsset vehicleasset))
                {
                    F.LogError($"Emplacement {Assets.find(Buildable.structureID)?.name?.Replace("_Base", "") ?? Buildable.structureID.ToString("N")}'s vehicle id is not a valid vehicle.");
                    return;
                }
                for (int i = 0; i < Buildable.emplacementData.ammoAmount; i++)
                    ItemManager.dropItem(new Item(ammoasset.id, true), data.point, true, true, true);
                Quaternion rotation = Foundation.model.rotation;
                rotation.eulerAngles = new Vector3(rotation.eulerAngles.x + 90, rotation.eulerAngles.y, rotation.eulerAngles.z);
                InteractableVehicle vehicle = VehicleManager.spawnVehicleV2(vehicleasset.id, new Vector3(data.point.x, data.point.y + 1, data.point.z), rotation);
                if (vehicle.asset.canBeLocked)
                {
                    CSteamID group = new CSteamID(data.group);
                    vehicle.tellLocked(new CSteamID(data.group), new CSteamID(data.group), true);

                    VehicleManager.ReceiveVehicleLockState(vehicle.instanceID, group, group, true);
                }

                if (Buildable.emplacementData.baseID != Guid.Empty)
                {
                    if (!(Assets.find(Buildable.emplacementData.baseID) is ItemBarricadeAsset emplacementBase))
                    {
                        F.LogError($"Emplacement base was not a valid barricade.");
                        return;
                    }
                    Barricade barricade = new Barricade(emplacementBase);
                    BarricadeManager.dropNonPlantedBarricade(barricade, data.point, Quaternion.Euler(data.angle_x * 2, data.angle_y * 2, data.angle_z * 2), data.owner, data.group);
                }
            }

            EffectManager.sendEffect(29, EffectManager.MEDIUM, data.point);

            if (Regions.tryGetCoordinate(Foundation.model.position, out byte x, out byte y))
            {
                BarricadeManager.destroyBarricade(Foundation, x, y, ushort.MaxValue);
                Destroy(gameObject);
            }
        }
        public static bool TryPlaceRadio(Barricade radio, UCPlayer placer, Vector3 point)
        {
            ulong team = placer.GetTeam();
            float radius = FOBManager.config.Data.FOBBuildPickupRadius;

            if (FOBManager.config.Data.RestrictFOBPlacement)
            {
                if (SDG.Framework.Water.WaterUtility.isPointUnderwater(point))
                {
                    placer?.Message("no_placement_fobs_underwater");
                    return false;
                }
                else if (point.y > F.GetTerrainHeightAt2DPoint(point.x, point.z, point.y, 0) + FOBManager.config.Data.FOBMaxHeightAboveTerrain)
                {
                    placer?.Message("no_placement_fobs_too_high", Mathf.RoundToInt(FOBManager.config.Data.FOBMaxHeightAboveTerrain).ToString(Data.Locale));
                    return false;
                }
                else if (Data.Gamemode is TeamGamemode && TeamManager.IsInAnyMainOrAMCOrLobby(point))
                {
                    placer?.Message("no_placement_fobs_too_near_base");
                    return false;
                }
            }
            if (UCBarricadeManager.GetBarricadesByGUID(radio.asset.GUID).Count(b => b.GetServersideData().group == team) >= FOBManager.config.Data.FobLimit)
            {
                // fob limit reached
                placer?.Message("build_error_too_many_fobs");
                return false;
            }

            int logis = UCVehicleManager.GetNearbyVehicles(FOBManager.config.Data.LogiTruckIDs.AsEnumerable(), 30, placer.Position).Count(l => l.lockedGroup.m_SteamID == placer.GetTeam());
            if (logis == 0)
            {
                // no logis nearby
                placer?.Message("fob_error_nologi");
                return false;
            }

            BarricadeDrop nearbyRadio = UCBarricadeManager.GetNearbyBarricades(Gamemode.Config.Barricades.FOBRadioGUID, radius * 2, point, team, false).FirstOrDefault();
            if (nearbyRadio != null)
            {
                // another FOB radio is too close
                placer?.Message("fob_error_fobtooclose", Math.Round((nearbyRadio.model.position - point).magnitude).ToString(), Math.Round(radius * 2).ToString());
                return false;
            }

            return true;
        }
        public static bool TryPlaceBuildable(Barricade foundation, BuildableData buildable, UCPlayer placer, Vector3 point)
        {
            ulong team = placer.GetTeam();

            BarricadeDrop radio = UCBarricadeManager.GetNearbyBarricades(Gamemode.Config.Barricades.FOBRadioGUID, FOBManager.config.Data.FOBBuildPickupRadius, point, team, false).FirstOrDefault();
            BarricadeDrop fob = radio ?? UCBarricadeManager.GetNearbyBarricades(Gamemode.Config.Barricades.FOBGUID, 30, radio.model.position, team, false).FirstOrDefault();

            Vector3 center = Vector3.zero;
            float radius = 30;
            bool useSmallRadius = true;
            
            if (radio != null)
            {
                center = radio.model.position;

                if (fob != null)
                {
                    radius = FOBManager.config.Data.FOBBuildPickupRadius;
                    useSmallRadius = false;
                }
            }

            Guid BuildID;
            if (team == 1)
                BuildID = Gamemode.Config.Items.T1Build;
            else if (team == 2)
                BuildID = Gamemode.Config.Items.T2Build;
            else return false;

            int NearbyBuild = UCBarricadeManager.GetNearbyItems(BuildID, radius, point).Count();

            if (buildable.type == EbuildableType.FOB)
            {
                if (FOBManager.config.Data.RestrictFOBPlacement)
                {
                    if (SDG.Framework.Water.WaterUtility.isPointUnderwater(point))
                    {
                        placer?.Message("no_placement_fobs_underwater");
                        return false;
                    }
                    else if (point.y > F.GetTerrainHeightAt2DPoint(point.x, point.z, point.y, 0) + FOBManager.config.Data.FOBMaxHeightAboveTerrain)
                    {
                        placer?.Message("no_placement_fobs_too_high", Mathf.RoundToInt(FOBManager.config.Data.FOBMaxHeightAboveTerrain).ToString(Data.Locale));
                        return false;
                    }
                    else if (Data.Gamemode is TeamGamemode && TeamManager.IsInAnyMainOrAMCOrLobby(point))
                    {
                        placer?.Message("no_placement_fobs_too_near_base");
                        return false;
                    }
                }

                var radioClose = UCBarricadeManager.GetNearbyBarricades(Gamemode.Config.Barricades.FOBRadioGUID, 30, point, team, false).FirstOrDefault();

                if (radioClose == null)
                {
                    // no radio nearby, radio must be within 30m
                    placer?.Message("build_error_noradio", "30");
                    return false;
                }

                var fobClose = UCBarricadeManager.GetNearbyBarricades(Gamemode.Config.Barricades.FOBGUID, 30, radioClose.model.position, team, false).FirstOrDefault();

                if (fobClose != null)
                {
                    // this fob already has a bunker
                    placer?.Message("build_error_structureexists", "a", "FOB Bunker");
                    return false;
                }
            }

            if ((radio.model.position - point).sqrMagnitude > radius)
            {
                // not in fob radius
                if (useSmallRadius)
                    placer?.Message("build_error_radiustoosmall", "30");
                else
                    placer?.Message("build_error_notinradius");
                return false;
            }

            if (NearbyBuild < buildable.requiredBuild)
            {
                // not enough build
                placer?.Message("build_error_notenoughbuild", NearbyBuild.ToString(), buildable.requiredBuild.ToString());
                return false;
            }

            if (buildable.type == EbuildableType.REPAIR_STATION)
            {
                int existing = UCBarricadeManager.GetNearbyBarricades(Gamemode.Config.Barricades.RepairStationGUID, radius, center, team, false).Count();
                if (existing >= 1)
                {
                    // repair station already exists
                    placer?.Message("build_error_structureexists", "a", "Repair Station");
                    return false;
                }
            }
            if (buildable.type == EbuildableType.EMPLACEMENT)
            {
                int existing = UCVehicleManager.GetNearbyVehicles(buildable.structureID, 30f, center).Count();
                if (existing >= buildable.emplacementData.allowedPerFob)
                {
                    // max emplacements of this type reached
                    placer?.Message("build_error_structureexists", buildable.emplacementData.allowedPerFob.ToString(), foundation.asset.itemName + (buildable.emplacementData.allowedPerFob == 1 ? "" : "s"));
                    return false;
                }
            }

            RemoveNearbyItemsByID(BuildID, buildable.requiredBuild, center, FOBManager.config.Data.FOBBuildPickupRadius);

            return true;
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
                if (ItemManager.regions[r.x, r.y] != null)
                {
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
    }
    
}
