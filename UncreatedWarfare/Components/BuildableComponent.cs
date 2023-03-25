using SDG.Unturned;
using Steamworks;
using System;
using System.Collections.Generic;
using System.Linq;
using Uncreated.Warfare.FOBs;
using Uncreated.Warfare.Gamemodes;
using Uncreated.Warfare.Gamemodes.Interfaces;
using Uncreated.Warfare.Kits;
using Uncreated.Warfare.Levels;
using Uncreated.Warfare.Players;
using Uncreated.Warfare.Quests;
using Uncreated.Warfare.Stats;
using Uncreated.Warfare.Teams;
using Uncreated.Warfare.Vehicles;
using UnityEngine;
using XPReward = Uncreated.Warfare.Levels.XPReward;

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
        L.LogDebug("Initializing buildable: " + foundation.asset.itemName);

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
        // TODO: check if this makes a difference towards fob bugs

        //FOB? fob = FOB.GetNearestFOB(Foundation.model.position, EFobRadius.FULL, Foundation.GetServersideData().group.GetTeam());
        FOB? fob = FOB.GetNearestFOB(Foundation.model.position, EFobRadius.FULL, builder.GetTeam());
        if (fob == null && Buildable.Type != BuildableType.Radio && (builder.KitClass is not Class.CombatEngineer || Buildable.Type is not BuildableType.Fortification))
        {
            builder.SendChat(T.BuildTickNotInRadius);
            return;
        }

        if (Buildable.Type == BuildableType.Bunker && fob!.Bunker != null)
        {
            builder.SendChat(T.BuildTickStructureExists, Buildable);
            return;
        }

        // TODO: add back this validation

        //if (!ValidatePlacementWithFriendlyFOB(Buildable, fob, builder, Foundation.GetServersideData().point, Foundation))
        //    return;

        float amount = builder.KitClass == Class.CombatEngineer ? 2f : 1f;

        amount = Mathf.Max(builder.ShovelSpeedMultiplier, amount);

        L.LogDebug("Incrementing build: " + builder + " (" + Hits + " + " + amount + ").");
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
        L.LogDebug("Building " + Foundation.asset.itemName);
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
                FOB? fob = FOB.GetNearestFOB(structure.model.position, EFobRadius.SHORT, data.group);
                if (fob != null)
                {
                    transform.gameObject.AddComponent<SpottedComponent>().Initialize(SpottedComponent.Spotted.FOB, data.group.GetTeam());

                    fob.UpdateBunker(structure);

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
            Vector3 rotation = Foundation.model.rotation.eulerAngles;
            InteractableVehicle vehicle = SpawnEmplacement(vehicleasset, Foundation.model.transform.position, rotation, data.owner, data.group, guid);
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
                float amount = 0f;
                if (Buildable.Type == BuildableType.Bunker)
                {
                    if (Points.PointsConfig.XPData.TryGetValue(XPReward.BunkerBuilt, out PointsConfig.XPRewardData data2))
                        amount = Mathf.RoundToInt(contribution * data2.Amount);
                }
                else
                {
                    if (Points.PointsConfig.XPData.TryGetValue(XPReward.Shoveling, out PointsConfig.XPRewardData data2))
                        amount = Mathf.RoundToInt(entry.Value * data2.Amount);
                }

                Points.AwardXP(player,
                    Buildable.Type == BuildableType.Bunker ? XPReward.BunkerBuilt : XPReward.Shoveling,
                    structureName.ToUpper() + " BUILT", Mathf.CeilToInt(amount));
                ActionLog.Add(ActionLogType.HelpBuildBuildable, $"{Foundation.asset.itemName} / {Foundation.asset.id} / {Foundation.asset.GUID:N} - {Mathf.RoundToInt(contribution * 100f).ToString(Data.AdminLocale)}%", player);
                if (contribution > 1f / 3f)
                    QuestManager.OnBuildableBuilt(player, Buildable);
            }
        }
        if (Regions.tryGetCoordinate(Foundation.model.position, out byte x, out byte y))
        {
            BarricadeManager.destroyBarricade(Foundation, x, y, ushort.MaxValue);
        }
        L.LogDebug("Done building " + Foundation.asset.itemName);
        Destroy(this);
    }
    internal static InteractableVehicle SpawnEmplacement(VehicleAsset vehicleAsset, Vector3 position, Vector3 rotation, ulong owner, ulong group, Guid baseBarricade = default)
    {
        Quaternion vrotation = Quaternion.Euler(new Vector3(rotation.x + 90, rotation.y, rotation.z));
        Quaternion brotation = Quaternion.Euler(rotation);
        InteractableVehicle vehicle = VehicleManager.spawnVehicleV2(vehicleAsset.id, new Vector3(position.x, position.y + 1, position.z), vrotation);

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
            BarricadeManager.dropNonPlantedBarricade(barricade, position, brotation, owner, group);
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
            L.LogDebug(Foundation.asset.itemName + " destroyed!");
            FOB? fob = FOB.GetNearestFOB(Foundation.GetServersideData().point, EFobRadius.FULL_WITH_BUNKER_CHECK, Foundation.GetServersideData().group);
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
        L.LogDebug("Trying to place radio: " + (placer?.ToString() ?? "null") + " placer. at " + point.ToString("F2", Data.AdminLocale));
#if DEBUG
        using IDisposable profiler = ProfilingUtils.StartTracking();
#endif
        ulong team = placer == null ? 0 : placer.GetTeam();
        float radius = FOBManager.Config.FOBBuildPickupRadius;

        if (FOBManager.Config.RestrictFOBPlacement)
        {
            if (SDG.Framework.Water.WaterUtility.isPointUnderwater(point))
            {
                L.LogDebug(" Underwater.", ConsoleColor.Red);
                placer?.SendChat(T.BuildFOBUnderwater);
                return false;
            }
            if (point.y > F.GetTerrainHeightAt2DPoint(point.x, point.z) + FOBManager.Config.FOBMaxHeightAboveTerrain)
            {
                L.LogDebug(" Too high.", ConsoleColor.Red);
                placer?.SendChat(T.BuildFOBTooHigh, FOBManager.Config.FOBMaxHeightAboveTerrain);
                return false;
            }
            if (Data.Gamemode is TeamGamemode && TeamManager.IsInAnyMainOrAMCOrLobby(point))
            {
                L.LogDebug(" In restricted zone.", ConsoleColor.Red);
                placer?.SendChat(T.BuildFOBTooCloseToMain);
                return false;
            }
        }
        if (FOB.GetFoBs(team).Count >= FOBManager.Config.FobLimit)
        {
            L.LogDebug(" Fob limit reached.");
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
                L.LogDebug(" VB not loaded.", ConsoleColor.Red);
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
                L.LogDebug(" No Logis.", ConsoleColor.Red);
                // no logis nearby
                placer?.SendChat(T.BuildNoLogisticsVehicle);
                return false;
            }
        }

        FOB? nearbyFOB = FOB.GetNearestFOB(point, EFobRadius.FOB_PLACEMENT, team);
        if (nearbyFOB != null)
        {
            L.LogDebug(" FOB too close.", ConsoleColor.Red);
            // another FOB radio is too close
            placer?.SendChat(T.BuildFOBTooClose, nearbyFOB, (nearbyFOB.Position - point).magnitude, radius * 2f);
            return false;
        }

        L.LogDebug(" Placing radio.", ConsoleColor.Green);
        return true;
    }
    public static bool TryPlaceBuildable(Barricade foundation, BuildableData buildable, UCPlayer placer, Vector3 point)
    {
        L.LogDebug("Trying to place buildable " + foundation.asset.itemName + ": " + (placer?.ToString() ?? "null") + " placer. at " + point.ToString("F2", Data.AdminLocale));
#if DEBUG
        using IDisposable profiler = ProfilingUtils.StartTracking();
#endif
        ulong team = placer.GetTeam();

        FOB? fob = FOB.GetNearestFOB(point, EFobRadius.FULL, team);

        if (buildable.Type == BuildableType.Bunker)
        {
            if (FOBManager.Config.RestrictFOBPlacement)
            {
                if (SDG.Framework.Water.WaterUtility.isPointUnderwater(point))
                {
                    L.LogDebug(" Underwater.", ConsoleColor.Red);
                    placer.SendChat(T.BuildFOBUnderwater);
                    return false;
                }
                if (point.y > F.GetTerrainHeightAt2DPoint(point.x, point.z) + FOBManager.Config.FOBMaxHeightAboveTerrain)
                {
                    L.LogDebug(" Too high.", ConsoleColor.Red);
                    placer.SendChat(T.BuildFOBTooHigh, FOBManager.Config.FOBMaxHeightAboveTerrain);
                    return false;
                }
                if (Data.Gamemode is ITeams && TeamManager.IsInAnyMainOrAMCOrLobby(point))
                {
                    L.LogDebug(" In restricted area.", ConsoleColor.Red);
                    placer.SendChat(T.BuildFOBTooCloseToMain);
                    return false;
                }
            }
            if (fob == null || (fob.Position - point).sqrMagnitude > Math.Pow(30, 2))
            {
                L.LogDebug(" No radio.", ConsoleColor.Red);
                // no radio nearby, radio must be within 30m
                placer.SendChat(T.BuildNoRadio, 30);
                return false;
            }
            if (fob.Bunker != null)
            {
                L.LogDebug(" limit reached (bunker already placed).", ConsoleColor.Red);
                // this fob already has a bunker
                placer.SendChat(T.BuildLimitReached, buildable.Limit, buildable);
                return false;
            }
        }
        else
        {
            BarricadeDrop? closeEnemyFOB = Gamemode.Config.BarricadeFOBBunker.ValidReference(out Guid guid)
                ? UCBarricadeManager.GetNearbyBarricades(guid, 5, point, false).FirstOrDefault()
                : null;
            if (buildable.Type == BuildableType.Emplacement)
            {
                if (!buildable.Foundation.Value.Exists || buildable.Emplacement is null || !buildable.Emplacement.EmplacementVehicle.Exists)
                {
                    // invalid GUIDs
                    L.LogDebug(" invalid asset.", ConsoleColor.Red);
                    placer.SendChat(T.BuildInvalidAsset);
                    return false;
                }
            }
            else
            {
                if (!buildable.Foundation.Value.Exists || !buildable.BuildableBarricade.Value.Exists)
                {
                    L.LogDebug(" invalid asset.", ConsoleColor.Red);
                    // invalid GUIDs
                    placer.SendChat(T.BuildInvalidAsset);
                    return false;
                }
            }
            

            if (closeEnemyFOB is not null && closeEnemyFOB.GetServersideData().group != team)
            {
                L.LogDebug(" enemy fob too close.", ConsoleColor.Red);
                // buildable too close to enemy bunker
                placer.SendChat(T.BuildBunkerTooClose, (closeEnemyFOB.model.position - point).magnitude, 5f);
                return false;
            }

            if (!ValidatePlacementWithFriendlyFOB(buildable, fob, placer, point))
                return false;
        }

        if (fob is not null)
        {
            if (fob.Build < buildable.RequiredBuild)
            {
                // not enough build
                placer.SendChat(T.BuildMissingSupplies, fob.Build, buildable.RequiredBuild);
                return false;
            }

            fob.ReduceBuild(buildable.RequiredBuild);
        }
        L.LogDebug(" Placing " + buildable.Type.ToString(), ConsoleColor.Green);
        return true;
    }
    public static int CountExistingBuildables(BuildableData buildable, FOB fob, BarricadeDrop? ignoreFoundation = null, UCPlayer? ownerOnly = null)
    {
        if (ownerOnly == null)
        {
            int existing = 0;
            if (buildable.Type != BuildableType.Emplacement)
            {
                if (buildable.BuildableBarricade.ValidReference(out ItemBarricadeAsset asset))
                    existing += UCBarricadeManager.CountNearbyBarricades(asset.GUID, fob.Radius, fob.Position, fob.Team);
            }
            else if (buildable.Emplacement != null && buildable.Emplacement.EmplacementVehicle.ValidReference(out VehicleAsset vehicle))
            {
                existing += UCVehicleManager.CountNearbyVehicles(vehicle.GUID, fob.Radius, fob.Position, fob.Team);
            }

            if (buildable.Foundation.ValidReference(out ItemBarricadeAsset basset))
            {
                List<BarricadeDrop> nearbyFoundations = UCBarricadeManager.GetNearbyBarricades(basset.GUID, fob.Radius, fob.Position, fob.Team, false).ToListFast();
                if (ignoreFoundation != null) nearbyFoundations.RemoveFast(ignoreFoundation);
                existing += nearbyFoundations.Count;
            }
            return existing;
        }
        else
        {
            int existing = 0;
            Vector3 pos = ownerOnly.Position;
            if (buildable.Type != BuildableType.Emplacement)
            {
                if (buildable.BuildableBarricade.ValidReference(out ItemBarricadeAsset asset))
                {
                    existing = UCBarricadeManager.CountBarricadesWhere(fob.Radius, fob.Position,
                        x => x.GetServersideData().group.GetTeam() == fob.Team &&
                             x.GetServersideData().owner == ownerOnly.Steam64 &&
                             asset.GUID == x.asset.GUID &&
                             (x.model.position - pos).sqrMagnitude < 50f * 50f);
                }
            }
            else if (buildable.Emplacement != null && buildable.Emplacement.EmplacementVehicle.ValidReference(out VehicleAsset vehicle))
            {
                for (int i = 0; i < VehicleManager.vehicles.Count; ++i)
                {
                    InteractableVehicle veh = VehicleManager.vehicles[i];
                    if (veh.lockedOwner.m_SteamID == ownerOnly.Steam64 &&
                        veh.lockedGroup.m_SteamID.GetTeam() == fob.Team &&
                        veh.asset.GUID == vehicle.GUID &&
                        (veh.transform.position - pos).sqrMagnitude < 50f * 50f)
                        ++existing;
                }
            }

            if (buildable.Foundation.ValidReference(out ItemBarricadeAsset basset))
            {
                List<BarricadeDrop> nearbyFoundations = UCBarricadeManager.GetBarricadesWhere(fob.Radius, fob.Position,
                        x => x.GetServersideData().group.GetTeam() == fob.Team &&
                             x.GetServersideData().owner == ownerOnly.Steam64 &&
                             basset.GUID == x.asset.GUID &&
                             (x.model.position - pos).sqrMagnitude < 50f * 50f).ToListFast();

                if (ignoreFoundation != null) nearbyFoundations.RemoveFast(ignoreFoundation);
                existing += nearbyFoundations.Count;
            }
            return existing;
        }
    }
    private static bool ValidatePlacementWithFriendlyFOB(BuildableData buildable, FOB? fob, UCPlayer placer, Vector3 point, BarricadeDrop? ignore = null)
    {
        if (!placer.HasKit || !placer.ActiveKit!.Item!.ContainsItem(buildable.Foundation.Value.Guid)) // normal player, buildable is not in their kit
        {
            if (fob is null)
            {
                L.LogDebug(" not in radius.", ConsoleColor.Red);
                // no fob nearby
                placer.SendChat(T.BuildNotInRadius);
                return false;
            }
            else if ((fob.Position - point).sqrMagnitude > Math.Pow(fob.Radius, 2))
            {
                L.LogDebug(" radius too big.", ConsoleColor.Red);
                // radius is constrained because there is no bunker
                placer.SendChat(T.BuildSmallRadius, fob.Radius);
                return false;
            }
            else
            {
                int existing = CountExistingBuildables(buildable, fob, ignore);
                if (existing >= buildable.Limit)
                {
                    L.LogDebug(" over buildable limit (" + existing + "/" + buildable.Limit + ").", ConsoleColor.Red);
                    // fob buildable limit reached for this type
                    placer.SendChat(T.BuildLimitReached, buildable.Limit, buildable);
                    return false;
                }

            }
        }
        else
        {
            int existing = CountExistingBuildables(buildable, fob, ignore, placer);
            int totalPlaced = UCBarricadeManager.CountBarricadesWhere(b =>
            b.GetServersideData().owner == placer.Steam64 &&
                b.asset.GUID == buildable.BuildableBarricade.Value.Guid &&
                (b.GetServersideData().point - placer.Position).sqrMagnitude < Mathf.Pow(50, 2)); // TODO: check will not work for emplacements - fix?

            int kitCount = placer.ActiveKit!.Item!.CountItems(buildable.Foundation.Value.Guid);
            if (totalPlaced >= kitCount)
            {
                L.LogDebug(" over buildable limit (kit) (" + totalPlaced + "/" + kitCount + ").", ConsoleColor.Red);
                // regional buildable limit reached for this player
                placer.SendChat(T.RegionalBuildLimitReached, kitCount, buildable);
                return false;
            }
        }

        return true;
    }
}

