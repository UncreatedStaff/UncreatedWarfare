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
    public Dictionary<ulong, float> PlayerHits { get; private set; }
    public float Hits { get; private set; }

    public bool IsSalvaged;

    public void Initialize(BarricadeDrop foundation, BuildableData buildable)
    {
        Foundation = foundation;
        Buildable = buildable;
        Hits = 0;
        IsSalvaged = false;
        PlayerHits = new Dictionary<ulong, float>();

        BarricadeData data = foundation.GetServersideData();

        UCPlayer? placer = UCPlayer.FromID(data.owner);
        if (placer != null && !(buildable.Type == BuildableType.Fortification || buildable.Type == BuildableType.AmmoCrate))
        {
            foreach (UCPlayer player in PlayerManager.OnlinePlayers.Where(p => p != placer &&
            p.GetTeam() == data.group &&
            !F.IsInMain(p.Position) &&
            p.Player.movement.getVehicle() == null &&
            (p.Position - foundation.model.position).sqrMagnitude < Math.Pow(80, 2)))
            {
                Tips.TryGiveTip(player, 120, T.TipHelpBuild, placer);
            }
        }
    }

    public void IncrementBuildPoints(UCPlayer builder)
    {
#if DEBUG
        using IDisposable profiler = ProfilingUtils.StartTracking();
#endif
        FOB? fob = FOB.GetNearestFOB(Foundation.model.position, EfobRadius.FULL, Foundation.GetServersideData().group.GetTeam());
        if (fob == null && Buildable.Type != BuildableType.Radio && (builder.KitClass is not Class.CombatEngineer || Buildable.Type is not BuildableType.Fortification))
        {
            builder.SendChat(T.BuildTickNotInRadius);
            return;
        }
        switch (Buildable.Type)
        {
            case BuildableType.Bunker:
                if (fob!.Bunker != null)
                {
                    builder.SendChat(T.BuildTickStructureExists, Buildable);
                    return;
                }
                break;
            case BuildableType.RepairStation:
                if (fob!.RepairStation != null)
                {
                    builder.SendChat(T.BuildTickStructureExists, Buildable);
                    return;
                }
                break;
            case BuildableType.Emplacement:
                if (Buildable.Emplacement == null ||
                    UCVehicleManager.CountNearbyVehicles(Buildable.Emplacement.EmplacementVehicle.Guid, fob!.Radius, fob.Position) >= Buildable.Emplacement.MaxFobCapacity)
                {
                    builder.SendChat(T.BuildTickStructureExists, Buildable);
                    return;
                }
                break;
        }
        float amount = builder.KitClass == Class.CombatEngineer ? 2f : 1f;

        amount = Mathf.Max(builder.ShovelSpeedMultiplier, amount);

        Hits += amount;

        //player.SendChat("fob_built");
        if (Gamemode.Config.EffectDig.ValidReference(out EffectAsset effect))
            F.TriggerEffectReliable(effect, EffectManager.MEDIUM, builder.Position);

        //XPManager.AddXP(builder.Player, XPManager.config.Data.ShovelXP, Math.Round((float)Hits / Buildable.requiredHits * 100F).ToString() + "%", true);

        if (builder.Player.TryGetPlayerData(out UCPlayerData component))
        {
            component.QueueMessage(new ToastMessage(Points.GetProgressBar(Hits, Buildable.RequiredHits, 25), ToastMessageSeverity.Progress), true);
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
    public void Build()
    {
#if DEBUG
        using IDisposable profiler = ProfilingUtils.StartTracking();
#endif
        BarricadeData data = Foundation.GetServersideData();

        string structureName;
        if (Buildable.Type != BuildableType.Emplacement)
        {
            if (!Buildable.BuildableBarricade.ValidReference(out ItemBarricadeAsset asset))
            {
                L.LogError((Buildable.Foundation.ValidReference(out asset) ? asset.FriendlyName : "<unknown>") + " does not have a valid BuildableBarricade in FOB config.");
                return;
            }
            Barricade barricade = new Barricade(asset);
            Transform transform = BarricadeManager.dropNonPlantedBarricade(barricade, data.point, Quaternion.Euler(data.angle_x * 2, data.angle_y * 2, data.angle_z * 2), data.owner, data.group);
            BarricadeDrop structure = BarricadeManager.FindBarricadeByRootTransform(transform);

            BuiltBuildableComponent comp = transform.gameObject.AddComponent<BuiltBuildableComponent>();
            comp.Initialize(structure, Buildable, PlayerHits);

            structureName = Buildable.Foundation.ValidReference(out ItemBarricadeAsset fndAsset) ? fndAsset.itemName : "<unknown>";

            if (Buildable.Type == BuildableType.Bunker)
            {
                FOB? fob = FOB.GetNearestFOB(structure.model.position, EfobRadius.SHORT, data.group);
                if (fob != null)
                {
                    transform.gameObject.AddComponent<SpottedComponent>().Initialize(SpottedComponent.Spotted.FOB, data.group.GetTeam());

                    fob.UpdateBunker(structure);

                    Orders.OnFOBBunkerBuilt(fob, this);

                    //StatsManager.ModifyStats(player.CSteamID.m_SteamID, s => s.FobsBuilt++, false);
                    StatsManager.ModifyTeam(data.group, t => t.FobsBuilt++, false);
                }
            }
        }
        else
        {
            if (Buildable.Emplacement == null || !Buildable.Emplacement.EmplacementVehicle.ValidReference(out VehicleAsset vehicleasset))
            {
                L.LogError((Buildable.Foundation.ValidReference(out ItemBarricadeAsset asset) ? asset.FriendlyName : "<unknown>") + " does not have a valid Emplacement > EmplacementVehicle in FOB config.");
                return;
            }
            Buildable.Emplacement.Ammo.ValidReference(out ItemAsset ammoasset);

            if (ammoasset != null)
                for (int i = 0; i < Buildable.Emplacement.AmmoCount; i++)
                    ItemManager.dropItem(new Item(ammoasset.id, true), data.point, true, true, true);
            else
                L.LogWarning($"Emplacement {vehicleasset.FriendlyName}'s ammo id is not a valid item.");

            Buildable.Emplacement.BaseBarricade.ValidReference(out Guid guid);
            InteractableVehicle vehicle = SpawnImplacement(vehicleasset, Foundation.model.transform.position, Foundation.model.rotation.eulerAngles, data.owner, data.group, guid);
            structureName = vehicle.asset.vehicleName;

            BuiltBuildableComponent comp = vehicle.transform.gameObject.AddComponent<BuiltBuildableComponent>();
            comp.Initialize(vehicle, Buildable, PlayerHits);
        }
        if (Gamemode.Config.EffectBuildSuccess.ValidReference(out EffectAsset effect))
            F.TriggerEffectReliable(effect, EffectManager.MEDIUM, data.point);

        foreach (KeyValuePair<ulong, float> entry in PlayerHits)
        {
            UCPlayer? player = UCPlayer.FromID(entry.Key);

            float contribution = entry.Value / Buildable.RequiredHits;

            if (contribution >= 0.1F && player != null)
            {
                float amount = Buildable.Type == BuildableType.Bunker
                    ? Mathf.RoundToInt(contribution * Points.XPConfig.BuiltFOBXP)
                    : entry.Value * Points.XPConfig.ShovelXP;

                Points.AwardXP(player, Mathf.CeilToInt(amount), structureName.ToUpper() + " BUILT");
                ActionLog.Add(ActionLogType.HelpBuildBuildable, $"{Foundation.asset.itemName} / {Foundation.asset.id} / {Foundation.asset.GUID:N} - {Mathf.RoundToInt(contribution * 100f).ToString(Data.AdminLocale)}%", player);
                if (contribution > 1f / 3f)
                    QuestManager.OnBuildableBuilt(player, Buildable);
            }
        }
        if (Regions.tryGetCoordinate(Foundation.model.position, out byte x, out byte y))
        {
            BarricadeManager.destroyBarricade(Foundation, x, y, ushort.MaxValue);
        }
        Destroy(this);
    }
    internal static InteractableVehicle SpawnImplacement(VehicleAsset vehicleAsset, Vector3 position, Vector3 rotation, ulong owner, ulong group, Guid baseBarricade = default)
    {
        Quaternion qrotation = Quaternion.Euler(new Vector3(rotation.x, rotation.y, rotation.z));
        InteractableVehicle vehicle = VehicleManager.spawnVehicleV2(vehicleAsset.id, new Vector3(position.x, position.y + 1, position.z), qrotation);

        if (vehicle.asset.canBeLocked)
        {
            CSteamID cowner = new CSteamID(owner);
            CSteamID cgroup = new CSteamID(group);
            vehicle.tellLocked(cowner, cgroup, true);

            VehicleManager.ReceiveVehicleLockState(vehicle.instanceID, cowner, cgroup, true);
        }

        if (baseBarricade != default && Assets.find(baseBarricade) is ItemBarricadeAsset emplacementBase)
        {
            Barricade barricade = new Barricade(emplacementBase);
            BarricadeManager.dropNonPlantedBarricade(barricade, position, qrotation, owner, group);
        }
        return vehicle;
    }
    public void Destroy()
    {
#if DEBUG
        using IDisposable profiler = ProfilingUtils.StartTracking();
#endif
        if (IsSalvaged)
        {
            FOB? fob = FOB.GetNearestFOB(Foundation.GetServersideData().point, EfobRadius.FULL_WITH_BUNKER_CHECK, Foundation.GetServersideData().group);
            if (fob is not null)
            {
                fob.AddBuild(Buildable.RequiredBuild);
            }
        }

        Destroy(this);
    }

    private static readonly List<InteractableVehicle> RadioNearbyVehiclesCache = new List<InteractableVehicle>(16);
    public static bool TryPlaceRadio(Barricade radio, UCPlayer? placer, Vector3 point)
    {
#if DEBUG
        using IDisposable profiler = ProfilingUtils.StartTracking();
#endif
        ulong team = placer == null ? 0 : placer.GetTeam();
        float radius = FOBManager.Config.FOBBuildPickupRadius;

        if (FOBManager.Config.RestrictFOBPlacement)
        {
            if (SDG.Framework.Water.WaterUtility.isPointUnderwater(point))
            {
                placer?.SendChat(T.BuildFOBUnderwater);
                return false;
            }
            if (point.y > F.GetTerrainHeightAt2DPoint(point.x, point.z) + FOBManager.Config.FOBMaxHeightAboveTerrain)
            {
                placer?.SendChat(T.BuildFOBTooHigh, FOBManager.Config.FOBMaxHeightAboveTerrain);
                return false;
            }
            if (Data.Gamemode is TeamGamemode && TeamManager.IsInAnyMainOrAMCOrLobby(point))
            {
                placer?.SendChat(T.BuildFOBTooCloseToMain);
                return false;
            }
        }
        if (FOB.GetFoBs(team).Count >= FOBManager.Config.FobLimit)
        {
            // fob limit reached
            placer?.SendChat(T.BuildMaxFOBsHit);
            return false;
        }
        if (placer == null || !placer.OnDuty())
        {
            VehicleBay? bay = Data.Singletons.GetSingleton<VehicleBay>();
            if (bay == null || !bay.IsLoaded)
            {
                placer?.SendChat(T.BuildNoLogisticsVehicle);
                return false;
            }
            bool any;
            lock (RadioNearbyVehiclesCache)
            {
                try
                {
                    ulong grp = TeamManager.GetGroupID(team);
                    VehicleManager.getVehiclesInRadius(point, 30 * 30, RadioNearbyVehiclesCache);
                    any = RadioNearbyVehiclesCache.Any(v =>
                    {
                        if (v.lockedGroup.m_SteamID != grp)
                            return false;
                        VehicleData? data = bay.Items.FirstOrDefault(x => x.Item != null && x.Item.VehicleID == v.asset.GUID)?.Item;
                        return data != null && VehicleData.IsLogistics(data.Type);
                    });
                }
                finally
                {
                    RadioNearbyVehiclesCache.Clear();
                }
            }
            if (!any)
            {
                // no logis nearby
                placer?.SendChat(T.BuildNoLogisticsVehicle);
                return false;
            }
        }

        FOB? nearbyFOB = FOB.GetNearestFOB(point, EfobRadius.FOB_PLACEMENT, team);
        if (nearbyFOB != null)
        {
            // another FOB radio is too close
            placer?.SendChat(T.BuildFOBTooClose, nearbyFOB, (nearbyFOB.Position - point).magnitude, radius * 2f);
            return false;
        }

        return true;
    }
    public static bool TryPlaceBuildable(Barricade foundation, BuildableData buildable, UCPlayer? placer, Vector3 point)
    {
#if DEBUG
        using IDisposable profiler = ProfilingUtils.StartTracking();
#endif
        ulong team = placer == null ? 0ul : placer.GetTeam();

        FOB? fob = FOB.GetNearestFOB(point, EfobRadius.FULL, team);

        if (buildable.Type == BuildableType.Bunker)
        {
            if (FOBManager.Config.RestrictFOBPlacement)
            {
                if (SDG.Framework.Water.WaterUtility.isPointUnderwater(point))
                {
                    placer?.SendChat(T.BuildFOBUnderwater);
                    return false;
                }
                if (point.y > F.GetTerrainHeightAt2DPoint(point.x, point.z) + FOBManager.Config.FOBMaxHeightAboveTerrain)
                {
                    placer?.SendChat(T.BuildFOBTooHigh, FOBManager.Config.FOBMaxHeightAboveTerrain);
                    return false;
                }
                if (Data.Gamemode is ITeams && TeamManager.IsInAnyMainOrAMCOrLobby(point))
                {
                    placer?.SendChat(T.BuildFOBTooCloseToMain);
                    return false;
                }
            }
            if (fob == null || (fob.Position - point).sqrMagnitude > Math.Pow(30, 2))
            {
                // no radio nearby, radio must be within 30m
                placer?.SendChat(T.BuildNoRadio, 30);
                return false;
            }
            if (fob.Bunker != null)
            {
                // this fob already has a bunker
                placer?.SendChat(T.BuildStructureExists, buildable);
                return false;
            }
        }
        else
        {
            BarricadeDrop? closeEnemyFOB = Gamemode.Config.BarricadeFOBBunker.ValidReference(out Guid guid)
                ? UCBarricadeManager.GetNearbyBarricades(guid, 5, point, false).FirstOrDefault()
                : null;
            if (closeEnemyFOB is not null && closeEnemyFOB.GetServersideData().group != team)
            {
                // buildable too close to enemy bunker
                placer?.SendChat(T.BuildBunkerTooClose, (closeEnemyFOB.model.position - point).magnitude, 5f);
                return false;
            }

            if (placer is not { KitClass: Class.CombatEngineer } ||
                !buildable.Foundation.ValidReference(out Guid foundGuid) ||
                !placer.HasKit ||
                !placer.ActiveKit!.Item!.ContainsItem(foundGuid))
            {
                if (fob == null)
                {
                    // no fob nearby
                    placer?.SendChat(T.BuildNotInRadius);
                    return false;
                }
                if ((fob.Position - point).sqrMagnitude > Math.Pow(fob.Radius, 2))
                {
                    // radius is constrained because there is no bunker
                    placer?.SendChat(T.BuildSmallRadius, fob.Radius);
                    return false;
                }
            }

            if (fob is null)
                return true;

            if (buildable.Type == BuildableType.RepairStation)
            {
                int existing = Gamemode.Config.BarricadeRepairStation.ValidReference(out guid) ? UCBarricadeManager.CountNearbyBarricades(guid, fob.Radius, fob.Position, team) : 0;
                if (existing >= 1)
                {
                    // repair station already exists
                    placer?.SendChat(T.BuildStructureExists, buildable);
                    return false;
                }
            }
            if (buildable.Type == BuildableType.Emplacement && buildable.Emplacement != null)
            {
                int existing = UCVehicleManager.GetNearbyVehicles(buildable.Emplacement.EmplacementVehicle, fob.Radius, fob.Position).Count();
                if (existing >= buildable.Emplacement.MaxFobCapacity)
                {
                    // max emplacements of this type reached
                    placer?.SendChat(T.BuildStructureExists, buildable);
                    return false;
                }
            }
        }

        if (fob.Build < buildable.RequiredBuild)
        {
            // not enough build
            placer?.SendChat(T.BuildMissingSupplies, fob.Build, buildable.RequiredBuild);
            return false;
        }

        fob.ReduceBuild(buildable.RequiredBuild);
        return true;
    }
}

