using SDG.Unturned;
using Steamworks;
using System;
using System.Collections.Generic;
using System.Linq;
using Uncreated.Warfare.FOBs;
using Uncreated.Warfare.Gamemodes;
using Uncreated.Warfare.Gamemodes.Interfaces;
using Uncreated.Warfare.Kits;
using Uncreated.Warfare.Point;
using Uncreated.Warfare.Quests;
using Uncreated.Warfare.Squads;
using Uncreated.Warfare.Stats;
using Uncreated.Warfare.Teams;
using Uncreated.Warfare.Vehicles;
using UnityEngine;

namespace Uncreated.Warfare.Components;

public class BuildableComponent : MonoBehaviour
{
    public BarricadeDrop Foundation { get; private set; }
    public BuildableData Buildable { get; private set; }

    public int Hits { get; private set;}

    public Dictionary<ulong, int> PlayerHits { get; private set; }

    public bool IsSalvaged;

    public void Initialize(BarricadeDrop foundation, BuildableData buildable)
    {
        Foundation = foundation;
        Buildable = buildable;
        Hits = 0;
        IsSalvaged = false;
        PlayerHits = new Dictionary<ulong, int>();

        SDG.Unturned.BarricadeData data = foundation.GetServersideData();

        UCPlayer? placer = UCPlayer.FromID(data.owner);
        if (placer != null && !(buildable.Type == EBuildableType.FORTIFICATION || buildable.Type == EBuildableType.AMMO_CRATE))
        {
            foreach (UCPlayer player in PlayerManager.OnlinePlayers.Where(p => p != placer && 
            p.GetTeam() == data.group && 
            !F.IsInMain(p.Position) &&
            p.Player.movement.getVehicle() == null &&
            (p.Position - foundation.model.position).sqrMagnitude < Math.Pow(80, 2)))
            {
                Tips.TryGiveTip(player, ETip.HELP_BUILD, placer.CharacterName);
            }
        }
    }

    public void IncrementBuildPoints(UCPlayer builder)
    {
#if DEBUG
        using IDisposable profiler = ProfilingUtils.StartTracking();
#endif
        FOB? fob = FOB.GetNearestFOB(Foundation.model.position, EFOBRadius.FULL, Foundation.GetServersideData().group.GetTeam());
        if (fob == null && Buildable.Type != EBuildableType.RADIO)
        {
            builder.SendChat("build_error_tick_notinradius");
            return;
        }
        switch (Buildable.Type)
        {
            case EBuildableType.FOB_BUNKER:
                if (fob!.Bunker != null)
                {
                    builder.SendChat("build_error_tick_structureexists", Translation.TranslateEnum(EBuildableType.FOB_BUNKER, builder));
                    return;
                }
                break;
            case EBuildableType.REPAIR_STATION:
                if (fob!.RepairStation != null)
                {
                    builder.SendChat("build_error_tick_structureexists", Translation.TranslateEnum(EBuildableType.REPAIR_STATION, builder));
                    return;
                }
                break;
            case EBuildableType.EMPLACEMENT:
                if (Buildable.Emplacement == null ||
                    UCVehicleManager.CountNearbyVehicles(Buildable.Emplacement.EmplacementVehicle.Guid, fob!.Radius, fob!.Position) >= Buildable.Emplacement.MaxFobCapacity)
                {
                    builder.SendChat("build_error_tick_structureexists", 
                        Buildable.Emplacement == null ? Translation.TranslateEnum(EBuildableType.EMPLACEMENT, builder) :
                            (Buildable.Emplacement!.EmplacementVehicle.Asset?.vehicleName ?? Translation.TranslateEnum(EBuildableType.EMPLACEMENT, builder)));
                    return;
                }
                break;
        }
        int amount = 1;
        if (builder.KitClass == EClass.COMBAT_ENGINEER)
            amount = 2;

        Hits += amount;

        //player.SendChat("fob_built");

        EffectManager.sendEffect(38405, EffectManager.MEDIUM, builder.Position);

        //XPManager.AddXP(builder.Player, XPManager.config.Data.ShovelXP, Math.Round((float)Hits / Buildable.requiredHits * 100F).ToString() + "%", true);

        if (builder.Player.TryGetPlayerData(out UCPlayerData component))
        {
            component.QueueMessage(new Players.ToastMessage(Points.GetProgressBar(Hits, Buildable.RequiredHits, 25), Players.EToastMessageSeverity.PROGRESS), true);
        }

        if (PlayerHits.ContainsKey(builder.Steam64))
            PlayerHits[builder.Steam64] += amount;
        else
            PlayerHits.Add(builder.Steam64, amount);

        if (Hits >= Buildable.RequiredHits)
        {
            Build();
        }
    }
    private class CollisionChecker : MonoBehaviour
    {
        public List<GameObject> ActiveColliders = new List<GameObject>();
        public int LayerMask { get; internal set; }
        void OnCollisionEnter(Collision other)
        {
            if (((LayerMask >> other.gameObject.layer) & 1) == 1)
                ActiveColliders.Add(other.gameObject);
        }
    }
    public void Build()
    {
#if DEBUG
        using IDisposable profiler = ProfilingUtils.StartTracking();
#endif
        BarricadeData data = Foundation.GetServersideData();

        string structureName;

        if (Buildable.Type != EBuildableType.EMPLACEMENT)
        {
            Barricade barricade = new Barricade(Assets.find<ItemBarricadeAsset>(Buildable.BuildableBarricade));
            Transform transform = BarricadeManager.dropNonPlantedBarricade(barricade, data.point, Quaternion.Euler(data.angle_x * 2, data.angle_y * 2, data.angle_z * 2), data.owner, data.group);
            BarricadeDrop structure = BarricadeManager.FindBarricadeByRootTransform(transform);

            BuiltBuildableComponent comp = transform.gameObject.AddComponent<BuiltBuildableComponent>();
            comp.Initialize(structure, Buildable, PlayerHits);

            if (Assets.find(Buildable.Foundation) is ItemAsset asset)
                structureName = asset.itemName;
            else
                structureName = Buildable.Foundation.ToString();

            if (Buildable.Type == EBuildableType.FOB_BUNKER)
            {
                FOB? fob = FOB.GetNearestFOB(structure.model.position, EFOBRadius.SHORT, data.group);
                if (fob != null)
                {
                    transform.gameObject.AddComponent<SpottedComponent>().Initialize(SpottedComponent.ESpotted.FOB);

                    fob.UpdateBunker(structure);
                    FOBManager.SendFOBListToTeam(fob.Team);

                    Orders.OnFOBBunkerBuilt(fob, this);

                    //StatsManager.ModifyStats(player.CSteamID.m_SteamID, s => s.FobsBuilt++, false);
                    StatsManager.ModifyTeam(data.group, t => t.FobsBuilt++, false);
                }
            }
        }
        else
        {
            ItemAsset? ammoasset = Buildable.Emplacement == null ? null : Assets.find<ItemAsset>(Buildable.Emplacement.Ammo);

            if (Buildable.Emplacement == null || Assets.find(Buildable.Emplacement.EmplacementVehicle) is not VehicleAsset vehicleasset)
            {
                L.LogError($"Emplacement {(Buildable.Emplacement == null ? "null" : Assets.find(Buildable.Emplacement.EmplacementVehicle)?.name?.Replace("_Base", "") ?? Buildable.Emplacement.EmplacementVehicle.ToString())}'s vehicle id is not a valid vehicle.");
                return;
            }

            if (ammoasset != null)
                for (int i = 0; i < Buildable.Emplacement.AmmoCount; i++)
                    ItemManager.dropItem(new Item(ammoasset.id, true), data.point, true, true, true);
            else
                L.LogWarning($"Emplacement {Assets.find(Buildable.BuildableBarricade)?.name ?? Buildable.BuildableBarricade.ToString()}'s ammo id is not a valid item.");

            Quaternion rotation = Foundation.model.rotation;
            rotation.eulerAngles = new Vector3(rotation.eulerAngles.x + 90, rotation.eulerAngles.y, rotation.eulerAngles.z);
            InteractableVehicle vehicle = VehicleManager.spawnVehicleV2(vehicleasset.id, new Vector3(data.point.x, data.point.y + 1, data.point.z), rotation);

            structureName = vehicle.asset.vehicleName;

            BuiltBuildableComponent comp = transform.gameObject.AddComponent<BuiltBuildableComponent>();
            comp.Initialize(vehicle, Buildable, PlayerHits);

            if (vehicle.asset.canBeLocked)
            {
                CSteamID owner = new CSteamID(data.owner);
                CSteamID group = new CSteamID(data.group);
                vehicle.tellLocked(owner, group, true);

                VehicleManager.ReceiveVehicleLockState(vehicle.instanceID, owner, group, true);
            }

            if (Buildable.Emplacement.BaseBarricade != Guid.Empty)
            {
                if (Assets.find(Buildable.Emplacement.BaseBarricade) is not ItemBarricadeAsset emplacementBase)
                {
                    L.LogWarning($"Emplacement base was not a valid barricade.");
                }
                else
                {
                    Barricade barricade = new Barricade(emplacementBase);
                    BarricadeManager.dropNonPlantedBarricade(barricade, data.point, Quaternion.Euler(data.angle_x * 2, data.angle_y * 2, data.angle_z * 2), data.owner, data.group);
                }
            }
        }

        EffectManager.sendEffect(29, EffectManager.MEDIUM, data.point);

        foreach (KeyValuePair<ulong, int> entry in PlayerHits)
        {
            UCPlayer? player = UCPlayer.FromID(entry.Key);

            float contribution = (float)entry.Value / Buildable.RequiredHits;

            if (contribution >= 0.1F && player != null)
            {
                int amount = 0;
                if (Buildable.Type == EBuildableType.FOB_BUNKER)
                    amount = Mathf.RoundToInt(contribution * Points.XPConfig.BuiltFOBXP);
                else
                    amount = entry.Value * Points.XPConfig.ShovelXP;

                Points.AwardXP(player, amount, structureName.ToUpper() + " BUILT");
                ActionLog.Add(EActionLogType.HELP_BUILD_BUILDABLE, $"{Foundation.asset.itemName} / {Foundation.asset.id} / {Foundation.asset.GUID:N} - {Mathf.RoundToInt(contribution * 100f).ToString(Data.Locale)}%", player);
                if (contribution > 0.3333f)
                    QuestManager.OnBuildableBuilt(player, Buildable);
            }
        }
        if (Regions.tryGetCoordinate(Foundation.model.position, out byte x, out byte y))
        {
            BarricadeManager.destroyBarricade(Foundation, x, y, ushort.MaxValue);
        }
        Destroy(this);
    }
    public void Destroy()
    {
#if DEBUG
        using IDisposable profiler = ProfilingUtils.StartTracking();
#endif
        if (IsSalvaged)
        {
            FOB? fob = FOB.GetNearestFOB(Foundation.model.position, EFOBRadius.FULL_WITH_BUNKER_CHECK, Foundation.GetServersideData().group);
            if (fob is not null)
            {
                fob.AddBuild(Buildable.RequiredBuild);
            }
        }

        Destroy(this);
    }
    public static bool TryPlaceRadio(Barricade radio, UCPlayer placer, Vector3 point)
    {
#if DEBUG
        using IDisposable profiler = ProfilingUtils.StartTracking();
#endif
        ulong team = placer.GetTeam();
        float radius = FOBManager.Config.FOBBuildPickupRadius;

        if (FOBManager.Config.RestrictFOBPlacement)
        {
            if (SDG.Framework.Water.WaterUtility.isPointUnderwater(point))
            {
                placer?.Message("no_placement_fobs_underwater");
                return false;
            }
            else if (point.y > F.GetTerrainHeightAt2DPoint(point.x, point.z) + FOBManager.Config.FOBMaxHeightAboveTerrain)
            {
                placer?.Message("no_placement_fobs_too_high", Mathf.RoundToInt(FOBManager.Config.FOBMaxHeightAboveTerrain).ToString(Data.Locale));
                return false;
            }
            else if (Data.Gamemode is TeamGamemode && TeamManager.IsInAnyMainOrAMCOrLobby(point))
            {
                placer?.Message("no_placement_fobs_too_near_base");
                return false;
            }
        }
        if (FOB.GetFOBs(team).Count >= FOBManager.Config.FobLimit)
        {
            // fob limit reached
            placer?.Message("build_error_too_many_fobs");
            return false;
        }
        if (!placer.OnDuty())
        {
            List<InteractableVehicle> vehicles = new List<InteractableVehicle>();
            VehicleManager.getVehiclesInRadius(point, Mathf.Pow(30, 2), vehicles);
            int logis = vehicles.Where(v => v.lockedGroup.m_SteamID == team &&
            VehicleBay.VehicleExists(v.asset.GUID, out var vehicleData) &&
            (vehicleData.Type == EVehicleType.LOGISTICS || vehicleData.Type == EVehicleType.HELI_TRANSPORT)).Count();
            if (logis == 0)
            {
                // no logis nearby
                placer?.Message("fob_error_nologi");
                return false;
            }
        }

        FOB? nearbyFOB = FOB.GetNearestFOB(point, EFOBRadius.FOB_PLACEMENT, team);
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
#if DEBUG
        using IDisposable profiler = ProfilingUtils.StartTracking();
#endif
        ulong team = placer.GetTeam();

        FOB? fob = FOB.GetNearestFOB(point, EFOBRadius.FULL, team);

        if (buildable.Type == EBuildableType.FOB_BUNKER)
        {
            if (FOBManager.Config.RestrictFOBPlacement)
            {
                if (SDG.Framework.Water.WaterUtility.isPointUnderwater(point))
                {
                    placer?.Message("no_placement_fobs_underwater");
                    return false;
                }
                else if (point.y > F.GetTerrainHeightAt2DPoint(point.x, point.z) + FOBManager.Config.FOBMaxHeightAboveTerrain)
                {
                    placer?.Message("no_placement_fobs_too_high", Mathf.RoundToInt(FOBManager.Config.FOBMaxHeightAboveTerrain).ToString(Data.Locale));
                    return false;
                }
                else if (Data.Gamemode is ITeams && TeamManager.IsInAnyMainOrAMCOrLobby(point))
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
            BarricadeDrop? closeEnemyFOB = UCBarricadeManager.GetNearbyBarricades(Gamemode.Config.Barricades.FOBGUID, 5, point, false).FirstOrDefault();
            if (closeEnemyFOB is not null && closeEnemyFOB.GetServersideData().group != team)
            {
                // buildable too close to enemy bunker
                placer?.Message("build_error_tooclosetoenemybunker");
                return false;
            }

            if (!(placer.KitClass == EClass.COMBAT_ENGINEER && KitManager.KitExists(placer.KitName, out Kit kit) && kit.Items.Exists(i => i.id == buildable.Foundation)))
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
            }

            if (fob is null)
            {
                return true;
            }
            else
            {
                if (buildable.Type == EBuildableType.REPAIR_STATION)
                {
                    int existing = UCBarricadeManager.GetNearbyBarricades(Gamemode.Config.Barricades.RepairStationGUID, fob.Radius, fob.Position, team, false).Count();
                    if (existing >= 1)
                    {
                        // repair station already exists
                        placer?.Message("build_error_structureexists", "a", "Repair Station");
                        return false;
                    }
                }
                if (buildable.Type == EBuildableType.EMPLACEMENT && buildable.Emplacement != null)
                {
                    int existing = UCVehicleManager.GetNearbyVehicles(buildable.Emplacement.EmplacementVehicle, fob.Radius, fob.Position).Count();
                    if (existing >= buildable.Emplacement.MaxFobCapacity)
                    {
                        // max emplacements of this type reached
                        placer?.Message("build_error_structureexists", buildable.Emplacement.MaxFobCapacity.ToString(), foundation.asset.itemName + (buildable.Emplacement.MaxFobCapacity == 1 ? "" : "s"));
                        return false;
                    }
                }
            }
        }

        if (fob.Build < buildable.RequiredBuild)
        {
            // not enough build
            placer?.Message("build_error_notenoughbuild", fob.Build.ToString(), buildable.RequiredBuild.ToString());
            return false;
        }

        fob.ReduceBuild(buildable.RequiredBuild);

        return true;
    }
}

