using SDG.Unturned;
using Steamworks;
using System;
using System.Collections.Generic;
using System.Linq;
using Uncreated.Warfare.FOBs;
using Uncreated.Warfare.Gamemodes;
using Uncreated.Warfare.Point;
using Uncreated.Warfare.Squads;
using Uncreated.Warfare.Stats;
using Uncreated.Warfare.Teams;
using Uncreated.Warfare.Vehicles;
using UnityEngine;

namespace Uncreated.Warfare.Components
{
    public class BuildableComponent : MonoBehaviour
    {
        public BarricadeDrop Foundation { get; private set; }
        public BuildableData Buildable { get; private set; }

        public int Hits { get; private set;}

        public Dictionary<ulong, int> PlayerHits { get; private set; }

        public void Initialize(BarricadeDrop foundation, BuildableData buildable)
        {
            Foundation = foundation;
            Buildable = buildable;
            Hits = 0;
            PlayerHits = new Dictionary<ulong, int>();
        }

        public void IncrementBuildPoints(UCPlayer builder)
        {
            int amount = 1;
            if (builder.KitClass == EClass.COMBAT_ENGINEER)
                amount = 2;

            Hits += amount;

            //player.SendChat("fob_built");

            EffectManager.sendEffect(38405, EffectManager.MEDIUM, builder.Position);

            //XPManager.AddXP(builder.Player, XPManager.config.Data.ShovelXP, Math.Round((float)Hits / Buildable.requiredHits * 100F).ToString() + "%", true);

            if (builder.Player.TryGetPlaytimeComponent(out var component))
            {
                component.QueueMessage(new Players.ToastMessage(Points.GetProgressBar(Hits, Buildable.requiredHits, 25), Players.EToastMessageSeverity.PROGRESS), true);
            }

            if (PlayerHits.ContainsKey(builder.Steam64))
                PlayerHits[builder.Steam64] += amount;
            else
                PlayerHits.Add(builder.Steam64, amount);

            if (Hits >= Buildable.requiredHits)
            {
                Build();
            }
        }
        public void Build()
        {
            SDG.Unturned.BarricadeData data = Foundation.GetServersideData();

            string structureName = "";

            if (Buildable.type != EBuildableType.EMPLACEMENT)
            {
                Barricade barricade = new Barricade(Assets.find<ItemBarricadeAsset>(Buildable.structureID));
                Transform transform = BarricadeManager.dropNonPlantedBarricade(barricade, data.point, Quaternion.Euler(data.angle_x * 2, data.angle_y * 2, data.angle_z * 2), data.owner, data.group);
                BarricadeDrop structure = BarricadeManager.FindBarricadeByRootTransform(transform);

                structureName = Assets.find<ItemBarricadeAsset>(Buildable.foundationID).itemName;

                if (Buildable.type == EBuildableType.FOB_BUNKER)
                {
                    FOB fob = FOB.GetNearestFOB(structure.model.position, EFOBRadius.SHORT, data.group);
                    if (fob != null)
                    {
                        fob.UpdateBunker(structure);

                        FOBManager.SendFOBListToTeam(fob.Team);

                        Orders.OnFOBBunkerBuilt(fob, this);

                        //StatsManager.ModifyStats(player.CSteamID.m_SteamID, s => s.FobsBuilt++, false);
                        StatsManager.ModifyTeam(data.group, t => t.FobsBuilt++, false);
                    }
                }
                else if (Buildable.type == EBuildableType.AMMO_CRATE)
                {
                    FOB fob = FOB.GetNearestFOB(structure.model.position, EFOBRadius.FULL, data.group);
                    fob.Status |= EFOBStatus.AMMO_CRATE;
                    FOBManager.SendFOBEffect(fob.Team, fob.Status, fob.Position);
                }
                else if (Buildable.type == EBuildableType.REPAIR_STATION)
                {
                    FOB fob = FOB.GetNearestFOB(structure.model.position, EFOBRadius.FULL, data.group);
                    fob.Status |= EFOBStatus.REPAIR_STATION;
                    FOBManager.SendFOBEffect(fob.Team, fob.Status, fob.Position);
                }
            }
            else
            {
                if (!(Assets.find(Buildable.emplacementData.ammoID) is ItemAsset ammoasset))
                {
                    L.LogError($"Emplacement {Assets.find(Buildable.structureID)?.name ?? Buildable.structureID.ToString("N")}'s ammo id is not a valid Item.");
                    return;
                }
                if (!(Assets.find(Buildable.structureID) is VehicleAsset vehicleasset))
                {
                    L.LogError($"Emplacement {Assets.find(Buildable.structureID)?.name?.Replace("_Base", "") ?? Buildable.structureID.ToString("N")}'s vehicle id is not a valid vehicle.");
                    return;
                }
                for (int i = 0; i < Buildable.emplacementData.ammoAmount; i++)
                    ItemManager.dropItem(new Item(ammoasset.id, true), data.point, true, true, true);
                Quaternion rotation = Foundation.model.rotation;
                rotation.eulerAngles = new Vector3(rotation.eulerAngles.x + 90, rotation.eulerAngles.y, rotation.eulerAngles.z);
                InteractableVehicle vehicle = VehicleManager.spawnVehicleV2(vehicleasset.id, new Vector3(data.point.x, data.point.y + 1, data.point.z), rotation);

                structureName = vehicle.asset.vehicleName;

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
                        L.LogError($"Emplacement base was not a valid barricade.");
                        return;
                    }
                    Barricade barricade = new Barricade(emplacementBase);
                    BarricadeManager.dropNonPlantedBarricade(barricade, data.point, Quaternion.Euler(data.angle_x * 2, data.angle_y * 2, data.angle_z * 2), data.owner, data.group);
                }
            }

            EffectManager.sendEffect(29, EffectManager.MEDIUM, data.point);

            foreach (var entry in PlayerHits)
            {
                UCPlayer player = UCPlayer.FromID(entry.Key);
                if ((float)entry.Value / Buildable.requiredHits >= 0.1F && player != null)
                    Points.AwardXP(player, entry.Value * Points.XPConfig.ShovelXP, structureName.ToUpper() + " BUILT");
            }

            if (Regions.tryGetCoordinate(Foundation.model.position, out byte x, out byte y))
            {
                BarricadeManager.destroyBarricade(Foundation, x, y, ushort.MaxValue);
                Destroy(this);
            }
        }
        public void Destroy()
        {
            Destroy(this);
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
            if (FOB.GetFOBs(team).Count >= FOBManager.config.Data.FobLimit)
            {
                // fob limit reached
                placer?.Message("build_error_too_many_fobs");
                return false;
            }

            int logis = UCVehicleManager.GetNearbyVehicles(FOBManager.config.Data.LogiTruckIDs.AsEnumerable(), 30, placer.Position).Count(l => l.lockedGroup.m_SteamID == placer.GetTeam());
            //if (logis == 0)
            //{
            //    // no logis nearby
            //    placer?.Message("fob_error_nologi");
            //    return false;
            //}

            FOB nearbyFOB = FOB.GetNearestFOB(point, EFOBRadius.FOB_PLACEMENT, team);
            if (nearbyFOB != null)
            {
                // another FOB radio is too close
                placer?.Message("fob_error_fobtooclose", Math.Round((nearbyFOB.Position - point).magnitude).ToString(), Math.Round(radius * 2).ToString());
                return false;
            }

            return true;
        }
        public static bool TryPlaceBuildable(Barricade foundation, BuildableData buildable, UCPlayer placer, Vector3 point)
        {
            ulong team = placer.GetTeam();

            FOB fob = FOB.GetNearestFOB(point, EFOBRadius.FULL, team);

            if (buildable.type == EBuildableType.FOB_BUNKER)
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

                if (fob == null || (fob.Position - point).sqrMagnitude > Math.Pow(30, 2))
                {
                    // no radio nearby, radio must be within 30m
                    placer?.Message("build_error_noradio", "30");
                    return false;
                }
                if (fob.Bunker != null)
                {
                    // this fob already has a bunker
                    placer?.Message("build_error_structureexists", "a", "FOB Bunker");
                    return false;
                }
            }
            else
            {
                if (fob == null)
                {
                    // no fob nearby
                    placer?.Message("build_error_notinradius");
                    return false;
                }
                else if ((fob.Position - point).sqrMagnitude > Math.Pow(fob.Radius, 2))
                {
                    // radius is constrained because there is no bunker
                    placer?.Message("build_error_radiustoosmall", "30");
                    return false;
                }

                if (buildable.type == EBuildableType.REPAIR_STATION)
                {
                    int existing = UCBarricadeManager.GetNearbyBarricades(Gamemode.Config.Barricades.RepairStationGUID, fob.Radius, fob.Position, team, false).Count();
                    if (existing >= 1)
                    {
                        // repair station already exists
                        placer?.Message("build_error_structureexists", "a", "Repair Station");
                        return false;
                    }
                }
                if (buildable.type == EBuildableType.EMPLACEMENT)
                {
                    int existing = UCVehicleManager.GetNearbyVehicles(buildable.structureID, fob.Radius, fob.Position).Count();
                    if (existing >= buildable.emplacementData.allowedPerFob)
                    {
                        // max emplacements of this type reached
                        placer?.Message("build_error_structureexists", buildable.emplacementData.allowedPerFob.ToString(), foundation.asset.itemName + (buildable.emplacementData.allowedPerFob == 1 ? "" : "s"));
                        return false;
                    }
                }
            }

            if (fob.Build < buildable.requiredBuild)
            {
                // not enough build
                placer?.Message("build_error_notenoughbuild", fob.Build.ToString(), buildable.requiredBuild.ToString());
                return false;
            }

            fob.ReduceBuild(buildable.requiredBuild);

            return true;
        }
    }
    
}
