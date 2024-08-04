using System;
using System.Collections.Generic;
using System.Linq;
using Uncreated.Framework.UI;
using Uncreated.Warfare.Commands;
using Uncreated.Warfare.Components;
using Uncreated.Warfare.Configuration;
using Uncreated.Warfare.Database;
using Uncreated.Warfare.Database.Abstractions;
using Uncreated.Warfare.Deaths;
using Uncreated.Warfare.Events;
using Uncreated.Warfare.Events.Components;
using Uncreated.Warfare.Events.Models.Barricades;
using Uncreated.Warfare.Events.Models.Items;
using Uncreated.Warfare.Events.Models.Players;
using Uncreated.Warfare.Events.Models.Structures;
using Uncreated.Warfare.Events.Models.Vehicles;
using Uncreated.Warfare.FOBs;
using Uncreated.Warfare.Interaction;
using Uncreated.Warfare.Kits;
using Uncreated.Warfare.Layouts.UI;
using Uncreated.Warfare.Logging;
using Uncreated.Warfare.Models.Assets;
using Uncreated.Warfare.Models.Kits;
using Uncreated.Warfare.Models.Localization;
using Uncreated.Warfare.Models.Stats.Records;
using Uncreated.Warfare.Moderation;
using Uncreated.Warfare.Moderation.Records;
using Uncreated.Warfare.Players.Management.Legacy;
using Uncreated.Warfare.Players.UI;
using Uncreated.Warfare.Quests;
using Uncreated.Warfare.Squads;
using Uncreated.Warfare.Stats;
using Uncreated.Warfare.Teams;
using Uncreated.Warfare.Traits;
using Uncreated.Warfare.Traits.Buffs;
using Uncreated.Warfare.Util;
using Uncreated.Warfare.Vehicles;
using Uncreated.Warfare.Zones;

#pragma warning disable IDE0060 // Remove unused parameter
namespace Uncreated.Warfare;

public static class EventFunctions
{
    public const float EnemyNearbyRespawnDistance = 100;
    internal static Dictionary<Item, PlayerInventory> ItemsTempBuffer = new Dictionary<Item, PlayerInventory>(256);
    internal static Dictionary<ulong, List<uint>> DroppedItems = new Dictionary<ulong, List<uint>>(96);
    internal static Dictionary<uint, ulong> DroppedItemsOwners = new Dictionary<uint, ulong>(256);
    internal static void SimulateRegisterLastDroppedItem(Vector3 point, ulong steam64)
    {
        if (steam64 == 0 || ItemManager.regions == null) return;
        if (Regions.tryGetCoordinate(point, out byte x, out byte y))
        {
            ItemRegion itemRegion = ItemManager.regions[x, y];
            if (itemRegion.items.Count == 0)
                return;
            ItemData newItem = itemRegion.items.GetTail();
            if (DroppedItems.TryGetValue(steam64, out List<uint> items))
                items.Add(newItem.instanceID);
            else
                DroppedItems.Add(steam64, [ newItem.instanceID ]);

            DroppedItemsOwners[newItem.instanceID] = steam64;
        }
    }
    internal static void OnPunch(PlayerEquipment equipment, EPlayerPunch hand)
    {
        if (hand != EPlayerPunch.RIGHT)
            return;

        UCPlayer? player = UCPlayer.FromPlayer(equipment.player);

        if (player != null && player.JumpOnPunch && player.OnDuty())
        {
            TeleportCommand.Jump(true, -1f, player);
            Vector3 castPt = player.Position;
            player.SendChat(T.TeleportSelfLocationSuccess, $"({castPt.x.ToString("0.##", Data.LocalLocale)}, {castPt.y.ToString("0.##", Data.LocalLocale)}, {castPt.z.ToString("0.##", Data.LocalLocale)})");
        }
    }
    internal static void StopCosmeticsToggleEvent(ref EVisualToggleType type, SteamPlayer player, ref bool allow)
    {
        if (Data.Gamemode is not { AllowCosmetics: true })
            allow = false;
    }
    internal static void OnStructurePlaced(StructureRegion region, StructureDrop drop)
    {
        StructureData data = drop.GetServersideData();
        ActionLog.Add(ActionLogType.PlaceStructure, $"{drop.asset.itemName} / {drop.asset.id} / {drop.asset.GUID:N} - Team: {TeamManager.TranslateName(data.group.GetTeam())}, ID: {drop.instanceID}", data.owner);
    }
    internal static void OnBarricadePlaced(BarricadeRegion region, BarricadeDrop drop)
    {
        BarricadeData data = drop.GetServersideData();

        BarricadeComponent owner = drop.model.gameObject.AddComponent<BarricadeComponent>();
        owner.Owner = data.owner;
        SteamPlayer player = PlayerTool.getSteamPlayer(data.owner);
        owner.Player = player?.player;
        owner.BarricadeGUID = data.barricade.asset.GUID;

        ActionLog.Add(ActionLogType.PlaceBarricade, $"{drop.asset.itemName} / {drop.asset.id} / {drop.asset.GUID:N} - Team: {TeamManager.TranslateName(data.group.GetTeam())}, ID: {drop.instanceID}", data.owner);

        RallyManager.OnBarricadePlaced(drop, region);

        if (Gamemode.Config.BarricadeAmmoBag.MatchGuid(data.barricade.asset.GUID))
            drop.model.gameObject.AddComponent<AmmoBagComponent>().Initialize(drop);
    }
    internal static void ProjectileSpawned(UseableGun gun, GameObject projectile)
    {
        try
        {
            const float radius = 3;
            const float length = 700;
            const int rayMask = RayMasks.VEHICLE | RayMasks.PLAYER | RayMasks.BARRICADE | RayMasks.LARGE |
                                 RayMasks.MEDIUM | RayMasks.GROUND | RayMasks.GROUND2;
            const int rayMaskBackup = RayMasks.VEHICLE | RayMasks.PLAYER | RayMasks.BARRICADE;


            UCPlayer? firer = UCPlayer.FromPlayer(gun.player);
            if (firer is null)
                return;

            if (gun.isAiming && Gamemode.Config.ItemLaserDesignator.MatchGuid(gun.equippedGunAsset.GUID))
            {
                float grndDist = float.NaN;
                if (Physics.Raycast(projectile.transform.position, projectile.transform.up, out RaycastHit hit, length,
                        rayMask))
                {
                    if (hit.transform != null)
                    {
                        if ((ELayerMask)hit.transform.gameObject.layer is ELayerMask.GROUND or ELayerMask.GROUND2
                            or ELayerMask.LARGE or ELayerMask.MEDIUM or ELayerMask.SMALL)
                            grndDist = (projectile.transform.position - hit.transform.position).sqrMagnitude;
                        else
                        {
                            SpottedComponent.MarkTarget(hit.transform, firer);
                            return;
                        }
                    }
                }

                List<RaycastHit> hits = new List<RaycastHit>(Physics.SphereCastAll(projectile.transform.position, radius,
                    projectile.transform.up, length, rayMaskBackup));
                Vector3 strtPos = projectile.transform.position;
                hits.RemoveAll(
                    x =>
                {
                    if (x.transform == null || !x.transform.gameObject.TryGetComponent<SpottedComponent>(out _))
                        return true;
                    float dist = (x.transform.position - strtPos).sqrMagnitude;
                    return dist < radius * radius + 1 || dist > grndDist;
                });
                if (hits.Count == 0) return;
                if (hits.Count == 1)
                {
                    SpottedComponent.MarkTarget(hits[0].transform, firer);
                    return;
                }
                hits.Sort((a, b) => (strtPos - b.point).sqrMagnitude.CompareTo((strtPos - a.point).sqrMagnitude));
                hits.Sort((a, _) => (ELayerMask)a.transform.gameObject.layer is ELayerMask.PLAYER ? -1 : 1);

                SpottedComponent.MarkTarget(hits[0].transform, firer);
                UnityEngine.Object.Destroy(projectile);
                return;
            }

            Rocket[] rockets = projectile.GetComponentsInChildren<Rocket>(true);
            foreach (Rocket rocket in rockets)
            {
                rocket.killer = firer.CSteamID;

                if (firer.CurrentVehicle != null)
                {
                    rocket.ignoreTransform = firer.CurrentVehicle.transform;
                }
                if (VehicleBay.Config.TOWMissileWeapons.HasGuid(gun.equippedGunAsset.GUID))
                    projectile.AddComponent<GuidedMissileComponent>().Initialize(projectile, firer, 90, 0.33f, 800);
                else if (VehicleBay.Config.GroundAAWeapons.HasGuid(gun.equippedGunAsset.GUID))
                    projectile.AddComponent<HeatSeekingMissileComponent>().Initialize(projectile, firer, 190, 8f, 2);
                else if (VehicleBay.Config.AirAAWeapons.HasGuid(gun.equippedGunAsset.GUID))
                    projectile.AddComponent<HeatSeekingMissileComponent>().Initialize(projectile, firer, 190, 6f, 0.5f);
                else if (VehicleBay.Config.LaserGuidedWeapons.HasGuid(gun.equippedGunAsset.GUID))
                    projectile.AddComponent<LaserGuidedMissileComponent>().Initialize(projectile, firer, 150, 1.15f, 150, 15, 0.6f);
            }

            Patches.DeathsPatches.lastProjected = projectile;
            if (!gun.player.TryGetPlayerData(out UCPlayerData c))
                return;

            c.LastRocketShot = gun.equippedGunAsset.GUID;
            c.LastRocketShotVehicleAsset = default;
            c.LastRocketShotVehicle = null;
            InteractableVehicle? veh = gun.player.movement.getVehicle();
            if (veh == null)
                return;

            for (int i = 0; i < veh.turrets.Length; ++i)
            {
                if (veh.turrets[i].turret == null || veh.turrets[i].turret.itemID != gun.equippedGunAsset.id)
                    continue;
                
                c.LastRocketShotVehicleAsset = veh.asset.GUID;
                c.LastRocketShotVehicle = veh;
                break;
            }
        }
        catch (Exception ex)
        {
            L.LogError("Error handling projectile spawn.");
            L.LogError(ex);
        }
    }
    internal static void BulletSpawned(UseableGun gun, BulletInfo bullet)
    {
        if (gun.player.TryGetPlayerData(out UCPlayerData c))
        {
            c.LastGunShot = gun.equippedGunAsset.GUID;
        }
    }
    internal static void ReloadCommand_onTranslationsReloaded()
    {
        foreach (UCPlayer player in PlayerManager.OnlinePlayers)
            UCWarfare.I.UpdateLangs(player, false);
    }

    internal static void OnLandmineExploding(TriggerTrapRequested e)
    {
        if (!e.IsExplosive)
            return;

        if (e.TriggeringPlayer is { VanishMode: true })
        {
            e.Cancel();
            return;
        }

        if (UCWarfare.Config.BlockLandmineFriendlyFire && e.TriggeringTeam == e.ServersideData.group.GetTeam())
        {
            // allow players to trigger their own landmines with throwables
            if (e.TriggeringPlayer == null || e.TriggeringPlayer.Steam64 != e.ServersideData.owner || e.TriggeringThrowable == null)
                e.Cancel();
        }
        else if (!CheckLandminePosition(e.ServersideData.point))
        {
            e.Cancel();
        }
    }
    public static void OnPickedUpItemRequested(Player player, byte x, byte y, uint instanceId, byte toX, byte toY, byte toRotation, byte toPage, ItemData itemData, ref bool shouldAllow)
    {
        UCPlayer? ucplayer = UCPlayer.FromPlayer(player);
        if (ucplayer == null || ucplayer.OnDuty())
            return;

        if (toPage == PlayerInventory.STORAGE && player.inventory.isStorageTrunk)
        {
            if (VehicleUtility.TryGetVehicleFromTrunkStorage(player.inventory.items[PlayerInventory.STORAGE], out InteractableVehicle? vehicle))
            {
                if (!vehicle.TryGetComponent(out VehicleComponent component) || component.NoPickZone == null)
                {
                    return;
                }

                ucplayer.SendChat(T.ProhibitedPickupZone, itemData.item.GetAsset(), FlagGamemode.GetZoneOrFlag(component.NoPickZone));
                shouldAllow = false;
                return;
            }
        }

        if (ucplayer.NoPickZone != null)
        {
            ucplayer.SendChat(T.ProhibitedPickupZone, itemData.item.GetAsset(), FlagGamemode.GetZoneOrFlag(ucplayer.NoPickZone));
            shouldAllow = false;
            // return;
        }
    }
    internal static void OnItemDropRequested(ItemDropRequested e)
    {
        if (e.Item.GetAsset() is not { } itemAsset)
            return;

        if (e.Page == PlayerInventory.STORAGE && e.Player.Player.inventory.isStorageTrunk && !e.Player.Keys.IsKeyDown(Data.Keys.DropSupplyOverride))
        {
            Items? trunk = e.Player.Player.inventory.items[PlayerInventory.STORAGE];
            if (VehicleUtility.TryGetVehicleFromTrunkStorage(trunk, out InteractableVehicle? vehicle))
            {
                VehicleBay? bay = VehicleBay.GetSingletonQuick();
                if (bay?.GetDataSync(vehicle.asset.GUID) is { } data && VehicleData.IsLogistics(data.Type))
                {
                    FactionInfo? faction = TeamManager.GetFactionSafe(e.Player.GetTeam());
                    if (faction is not null && TeamManager.IsInMain(e.Player))
                    {
                        faction.Build.TryGetAsset(out ItemAsset? buildAsset);
                        faction.Ammo.TryGetAsset(out ItemAsset? ammoAsset);
                        bool build = buildAsset is not null && buildAsset.GUID == itemAsset.GUID;
                        if (build || ammoAsset is not null && ammoAsset.GUID == itemAsset.GUID)
                        {
                            trunk.removeItem(e.Index);
                            Item it2 = new Item(build ? ammoAsset : buildAsset, EItemOrigin.WORLD);
                            trunk.addItem(e.X, e.Y, e.ItemJar.rot, it2);
                            e.Cancel();
                            return;
                        }
                    }
                }

                if (!e.Player.OnDuty() && vehicle.TryGetComponent(out VehicleComponent component) && component.NoDropZone != null)
                {
                    e.Player.SendChat(T.ProhibitedDropZone, itemAsset, FlagGamemode.GetZoneOrFlag(component.NoDropZone));
                    e.Cancel();
                }
                return;
            }
        }
        
        if (!e.Player.OnDuty() && e.Player.NoDropZone != null)
        {
            e.Player.SendChat(T.ProhibitedDropZone, itemAsset, FlagGamemode.GetZoneOrFlag(e.Player.NoDropZone));
            e.Break();
        }
    }
    internal static void OnPreVehicleDamage(CSteamID instigatorSteamID, InteractableVehicle vehicle, ref ushort pendingTotalDamage, ref bool canRepair, ref bool shouldAllow, EDamageOrigin damageOrigin)
    {
        if (!shouldAllow)
            return;

        if (F.IsInMain(vehicle.transform.position))
        {
            shouldAllow = false;
            return;
        }

        if (damageOrigin == EDamageOrigin.Vehicle_Collision_Self_Damage && !(vehicle.asset.engine == EEngine.HELICOPTER || vehicle.asset.engine == EEngine.PLANE))
        {
            pendingTotalDamage = (ushort)Mathf.RoundToInt(pendingTotalDamage * 0.13f);
        }

        UCPlayer? instigator = UCPlayer.FromCSteamID(instigatorSteamID);

        // AMC damage multiplier application
        if (instigator != null)
        {
            ulong team = vehicle.lockedGroup.m_SteamID.GetTeam();
            VehicleComponent? component = vehicle.GetComponent<VehicleComponent>();
            float vehicleDamageMultiplier = component != null && component.SafezoneZone != null ? 0f : TeamManager.GetAMCDamageMultiplier(team, vehicle.transform.position);
            float instigatorDamageMultiplier = instigator.GetAMCDamageMultiplier();

            if (vehicleDamageMultiplier > 0f && instigatorDamageMultiplier > 0f)
            {
                float dmgMult = Mathf.Min(vehicleDamageMultiplier, instigatorDamageMultiplier);

                pendingTotalDamage = (ushort)Mathf.RoundToInt(pendingTotalDamage * dmgMult);
            }
            else
            {
                shouldAllow = false;
                return;
            }
        }

        if (!vehicle.TryGetComponent(out VehicleComponent c))
        {
            c = vehicle.gameObject.AddComponent<VehicleComponent>();
            c.Initialize(vehicle);
        }

        c.LastDamageOrigin = damageOrigin;
        c.LastInstigator = instigatorSteamID.m_SteamID;
        c.LastDamagedFromVehicle = null;
        if (instigatorSteamID != CSteamID.Nil)
        {
            c.LastItem = Guid.Empty;
            c.LastItemIsVehicle = false;
            instigatorSteamID.TryGetPlayerData(out UCPlayerData? instigatorData);
            switch (damageOrigin)
            {
                case EDamageOrigin.Grenade_Explosion:
                    if (instigatorData != null)
                    {
                        ThrowableComponent? a = instigatorData.ActiveThrownItems.FirstOrDefault(x => x.IsExplosive);
                        if (a != null)
                            c.LastItem = a.Throwable;
                    }
                    break;
                case EDamageOrigin.Rocket_Explosion:
                    if (instigatorData != null)
                    {
                        c.LastDamagedFromVehicle = instigatorData.LastRocketShotVehicle;
                        c.LastItem = instigatorData.LastRocketShot;
                    }
                    break;
                case EDamageOrigin.Vehicle_Explosion:
                    if (instigatorData != null)
                    {
                        c.LastItemIsVehicle = true;
                        c.LastItem = instigatorData.LastExplodedVehicle;
                    }
                    break;
                case EDamageOrigin.Bullet_Explosion:
                case EDamageOrigin.Useable_Melee:
                case EDamageOrigin.Useable_Gun:
                    if (instigator != null && instigator.Player.equipment.asset != null)
                        c.LastItem = instigator.Player.equipment.asset.GUID;
                    c.LastDamagedFromVehicle = instigator?.CurrentVehicle;
                    break;
                case EDamageOrigin.Food_Explosion:
                    if (instigatorData != null) c.LastItem = instigatorData.LastExplosiveConsumed;
                    break;
                case EDamageOrigin.Trap_Explosion:
                    BarricadeDrop? drop = instigatorData?.ExplodingLandmine;
                    UCPlayer? triggerer = null;
                    if (drop == null)
                    {
                        for (int i = 0; i < PlayerManager.OnlinePlayers.Count; ++i)
                        {
                            UCPlayer pl = PlayerManager.OnlinePlayers[i];
                            if (pl.Player.TryGetPlayerData(out UCPlayerData d) && d.TriggeringLandmine != null &&
                                (d.TriggeringLandmine.model.position - vehicle.transform.position).sqrMagnitude < 225f)
                            {
                                triggerer = pl;
                                c.LastItem = d.TriggeringLandmine.asset.GUID;
                                break;
                            }
                        }
                    }
                    else if (instigatorData != null)
                    {
                        if (instigatorData.TriggeringLandmine == drop)
                            triggerer = instigator;
                    }
                    if (drop != null)
                    {
                        c.LastItem = drop.asset.GUID;
                        if (triggerer == null)
                        {
                            for (int i = 0; i < PlayerManager.OnlinePlayers.Count; ++i)
                            {
                                UCPlayer pl = PlayerManager.OnlinePlayers[i];
                                if (pl.Player.TryGetPlayerData(out UCPlayerData d) && d.TriggeringLandmine == drop)
                                {
                                    triggerer = pl;
                                    break;
                                }
                            }
                        }
                        // if the trap belongs to the triggerer's team, blame the triggerer
                        if (triggerer != null && (instigator == null || triggerer.Steam64 != instigator.Steam64) &&
                            (drop.GetServersideData().group.GetTeam() == triggerer.GetTeam()))
                            c.LastInstigator = triggerer.Steam64;
                    }
                    break;
            }

            if (!c.DamageTable.TryGetValue(instigatorSteamID.m_SteamID, out KeyValuePair<ushort, DateTime> pair))
                c.DamageTable.Add(instigatorSteamID.m_SteamID, new KeyValuePair<ushort, DateTime>(pendingTotalDamage, DateTime.Now));
            else
                c.DamageTable[instigatorSteamID.m_SteamID] = new KeyValuePair<ushort, DateTime>((ushort)(pair.Key + pendingTotalDamage), DateTime.Now);

            if (instigator != null)
            {
                InteractableVehicle attackerVehicle = instigator.Player.movement.getVehicle();
                if (attackerVehicle != null)
                {
                    c.Quota += pendingTotalDamage * 0.015F;
                }
            }
        }

        if (damageOrigin == EDamageOrigin.Useable_Gun)
        {
            if ((vehicle.asset.engine == EEngine.HELICOPTER || vehicle.asset.engine == EEngine.PLANE) && pendingTotalDamage > vehicle.health && pendingTotalDamage > 200)
            {
                canRepair = false;
            }

            VehicleDamageCalculator.ApplyAdvancedDamage(vehicle, ref pendingTotalDamage);
        }
        if (damageOrigin == EDamageOrigin.Rocket_Explosion)
            VehicleDamageCalculator.ApplyAdvancedDamage(vehicle, ref pendingTotalDamage);
    }

    internal static void OnPostPlayerConnected(PlayerJoined e)
    {
        try
        {
            // reset the player to spawn if they have joined in a different game as they last played in.
            UCPlayer ucplayer = e.Player;
            ucplayer.Loading = true;
            bool shouldApplyLastKit = false, forceLastKitRemoval = false;

            if (UCPlayer.LoadingUI.HasAssetOrId)
                UCPlayer.LoadingUI.SendToPlayer(ucplayer.Connection, T.LoadingOnJoin.Translate(ucplayer));

            if (ucplayer.Player.life.isDead)
            {
                ucplayer.Player.life.ServerRespawn(false);
                L.LogDebug("Player " + ucplayer + " was dead, respawning.");
            }

            if (Data.Gamemode.LeaderboardUp(out ILeaderboard lb))
            {
                L.LogDebug("Joining leaderboard...");
                lb.OnPlayerJoined(ucplayer);
                ucplayer.Player.quests.leaveGroup(true);
                TeamManager.TeleportToMain(ucplayer, 0);
                forceLastKitRemoval = true;
            }
            else if (TeamManager.LobbyZone.IsInside(ucplayer.Position) || Data.Gamemode == null || ucplayer.Save.LastGame != Data.Gamemode.GameId || Data.Gamemode.State is not State.Active and not State.Staging)
            {
                ucplayer.Player.life.sendRevive();
                L.LogDebug("Player " + ucplayer + " did not play this game, leaving group.");
                if (Data.Gamemode is ITeams teams && teams.UseTeamSelector && teams.TeamSelector is { IsLoaded: true })
                    teams.TeamSelector.JoinSelectionMenu(ucplayer);
                else
                    ucplayer.Player.quests.leaveGroup(true);
                forceLastKitRemoval = true;
            }
            else if (ucplayer.Save.Team is 1 or 2 && TeamManager.CanJoinTeam(ucplayer, ucplayer.Save.Team))
            {
                shouldApplyLastKit = true;
                L.LogDebug("Player " + ucplayer + " played this game and can rejoin their team, joining back to " + ucplayer.Save.Team + ".");
                ulong other = TeamManager.Other(ucplayer.Save.Team);
                Vector3 pos = ucplayer.Position;
                // if there's teammates nearby then teleport them back to their main base
                bool teleport = PlayerManager.OnlinePlayers.Any(x =>
                    x.GetTeam() == other &&
                    (x.Position - pos).sqrMagnitude <= EnemyNearbyRespawnDistance * EnemyNearbyRespawnDistance);
                TeamManager.JoinTeam(ucplayer, ucplayer.Save.Team, teleport, true);
                if (teleport)
                    ucplayer.Player.life.sendRevive();
            }
            else if (Data.Gamemode is ITeams teams && teams.UseTeamSelector && teams.TeamSelector is { IsLoaded: true })
            {
                forceLastKitRemoval = true;
                ucplayer.Player.life.sendRevive();
                L.LogDebug("Player " + ucplayer + " played this game but can't join " + ucplayer.Save.Team + ", joining lobby.");
                teams.TeamSelector.JoinSelectionMenu(ucplayer);
            }
            else
            {
                forceLastKitRemoval = true;
                L.LogDebug("Player " + ucplayer + " played this game but can't join " + ucplayer.Save.Team + ", leaving group.");
                ucplayer.Player.quests.leaveGroup(true);
            }
            if (Data.Gamemode != null)
            {
                ucplayer.Save.LastGame = Data.Gamemode.GameId;
                PlayerSave.WriteToSaveFile(ucplayer.Save);
            }

            ulong team = ucplayer.GetTeam();
            PlayerNames names = ucplayer.Name;
            
            if (Data.PlaytimeComponents.ContainsKey(ucplayer.Steam64))
            {
                UnityEngine.Object.Destroy(Data.PlaytimeComponents[ucplayer.Steam64]);
                Data.PlaytimeComponents.Remove(ucplayer.Steam64);
            }
            ucplayer.Player.transform.gameObject.AddComponent<SpottedComponent>().Initialize(SpottedComponent.Spotted.Infantry, team);
            if (e.Steam64 == 76561198267927009) ucplayer.Player.channel.owner.isAdmin = true;
            UCPlayerData pt = ucplayer.Player.transform.gameObject.AddComponent<UCPlayerData>();

            pt.StartTracking(ucplayer.Player);
            Data.PlaytimeComponents.Add(ucplayer.Steam64, pt);

            if (Data.Singletons.TryGetComponent(out ZoneList zoneList))
                zoneList.TickZoneFlags(ucplayer, true);

            CancellationToken token = ucplayer.DisconnectToken;
            UCWarfare.RunTask(async tkn =>
            {
                if (Data.Gamemode != null)
                {
                    await ucplayer.PurchaseSync.WaitAsync(tkn).ConfigureAwait(false);
                    try
                    {
                        L.LogDebug($"Initing player with settings: kit id: {ucplayer.Save.KitId}, shouldApplyLastKit: {shouldApplyLastKit}, forceLastKitRemoval: {forceLastKitRemoval}.");
                        if (shouldApplyLastKit && ucplayer.Save.KitId != 0)
                        {
                            Task<Kit?>? kitTask = KitManager.GetSingletonQuick()?.GetKit(ucplayer.Save.KitId, token, x => KitManager.RequestableSet(x, false));
                            if (kitTask != null)
                            {
                                Kit? kit = await kitTask.ConfigureAwait(false);

                                await UniTask.SwitchToMainThread(token);
                                ucplayer.ChangeKit(kit);
                                // create a squad or give unarmed kit
                                if (kit.Class == Class.Squadleader && SquadManager.Loaded)
                                {
                                    L.LogDebug("Player joining with squad leader kit..");
                                    if (SquadManager.MaxSquadsReached(team) || SquadManager.AreSquadLimited(team, out _))
                                    {
                                        KitManager? manager = KitManager.GetSingletonQuick();
                                        if (manager != null)
                                        {
                                            await manager.TryGiveUnarmedKit(ucplayer, manual: false, token);
                                            await UniTask.SwitchToMainThread(token);
                                        }
                                        L.LogDebug("  Tried to give unarmed kit.");
                                    }
                                    else
                                    {
                                        SquadManager.CreateSquad(ucplayer, ucplayer.GetTeam());
                                        L.LogDebug("  Created squad.");
                                    }
                                }
                            }
                            else
                                await UniTask.SwitchToMainThread(token);
                        }
                        else if (forceLastKitRemoval || ucplayer.Save.KitId != 0)
                        {
                            KitManager? manager = KitManager.GetSingletonQuick();

                            if (manager != null)
                            {
                                await manager.Requests.GiveKit(ucplayer, kit: null, manual: false, tip: false, token, psLock: false).ConfigureAwait(false);
                            }

                            await UniTask.SwitchToMainThread(token);
                        }

                        await Data.Gamemode.OnPlayerJoined(ucplayer, tkn).ConfigureAwait(false);

                        // This kicks the player out of the vanilla loading screen. It's first call is cancelled.
                        ThreadQueue.Queue.RunOnMainThread(() => Patches.SendInitialPlayerStateForce(ucplayer.Player.inventory, ucplayer.SteamPlayer));
                    }
                    catch (OperationCanceledException)
                    {
                        L.LogDebug("Player disconnected mid player-init.");
                    }
                    catch (Exception ex)
                    {
                        L.LogError("Error initializing player: " + ucplayer);
                        L.LogError(ex);
                        L.LogError(ex.ToString());

                        ThreadQueue.Queue.RunOnMainThread(() => Provider.kick(ucplayer.CSteamID, $"Error connecting: {ex.GetType().Name} - {ex.Message}. Join 'discord.gg/{UCWarfare.Config.DiscordInviteCode}' for help."));
                    }
                    finally
                    {
                        ucplayer.PurchaseSync.Release();
                    }
                }

                await UCWarfare.ToUpdate(tkn);
                if (ucplayer.IsOnline && UCPlayer.LoadingUI.HasAssetOrId)
                {
                    UCPlayer.LoadingUI.ClearFromPlayer(ucplayer.Connection);
                }
            }, token, ctx: $"Player connecting: {ucplayer.Steam64}.");
            ucplayer.Player.gameObject.AddComponent<ZonePlayerComponent>().Init(ucplayer);
            ActionLog.Add(ActionLogType.Connect, $"Players online: {Provider.clients.Count}", ucplayer);
            if (UCWarfare.Config.EnablePlayerJoinLeaveMessages)
                Chat.Broadcast(T.PlayerConnected, ucplayer);
            Data.Reporter?.OnPlayerJoin(ucplayer.SteamPlayer);
            PlayerManager.NetCalls.SendPlayerJoined.NetInvoke(new ModerationUI.PlayerListEntry
            {
                Duty = ucplayer.OnDuty(),
                Name = names.CharacterName,
                Steam64 = ucplayer.Steam64,
                Team = ucplayer.Player.GetTeamByte()
            });
            if (!UCWarfare.Config.DisableDailyQuests)
                DailyQuests.RegisterDailyTrackers(ucplayer);

            IconManager.DrawNewMarkers(ucplayer, false);
        }
        catch (Exception ex)
        {
            L.LogError("Error in the main OnPostPlayerConnected:");
            L.LogError(ex);
        }
    }
    private static readonly PlayerVoice.RelayVoiceCullingHandler NoComms = (_, _) => false;
    internal static void OnRelayVoice2(PlayerVoice speaker, bool wantsToUseWalkieTalkie, ref bool shouldAllow,
        ref bool shouldBroadcastOverRadio, ref PlayerVoice.RelayVoiceCullingHandler cullingHandler)
    {
        if (Data.Gamemode is null)
        {
            cullingHandler = NoComms;
            shouldBroadcastOverRadio = false;
            return;
        }
        bool isMuted = false;
        UCPlayer? ucplayer = PlayerManager.FromID(speaker.channel.owner.playerID.steamID.m_SteamID);
        if (ucplayer is not null)
        {
            if ((ucplayer.MuteType & MuteType.Voice) == MuteType.Voice && ucplayer.TimeUnmuted > DateTime.Now)
                isMuted = true;

            ucplayer.OnUseVoice(isMuted);
        }
        if (isMuted)
        {
            shouldAllow = false;
            cullingHandler = NoComms;
            shouldBroadcastOverRadio = false;
        }
        else if (Data.Gamemode.State is State.Finished or State.Loading && !UCWarfare.Config.RelayMicsDuringEndScreen)
        {
            shouldAllow = false;
            cullingHandler = NoComms;
            shouldBroadcastOverRadio = false;
        }
    }
    internal static void OnBattleyeKicked(SteamPlayer client, string reason)
    {
        UCPlayer? player = UCPlayer.FromSteamPlayer(client);
        PlayerNames names = player != null ? player.Name : new PlayerNames(client);
        Chat.Broadcast(T.BattlEyeKickBroadcast, names);

        L.Log($"{names.PlayerName} ({client.playerID.steamID.m_SteamID}) was kicked by BattlEye for \"{reason}\".");
        ActionLog.Add(ActionLogType.KickedByBattlEye, "REASON: \"" + reason + "\"", client.playerID.steamID.m_SteamID);

        DateTimeOffset now = DateTimeOffset.UtcNow;

        BattlEyeKick kick = new BattlEyeKick
        {
            Actors = new RelatedActor[] { new RelatedActor(RelatedActor.RolePrimaryAdmin, true, Actors.BattlEye) },
            Message = reason,
            Id = PrimaryKey.NotAssigned,
            RelevantLogsEnd = now,
            RelevantLogsBegin = now.Subtract(TimeSpan.FromSeconds(Time.realtimeSinceStartup - client.joined)),
            StartedTimestamp = now,
            ResolvedTimestamp = now,
            PendingReputation = 0d,
            IsLegacy = false,
            Player = client.playerID.steamID.m_SteamID
        };

        UCWarfare.RunTask(Data.ModerationSql.AddOrUpdate, kick, CancellationToken.None, ctx: "Log BattlEyeKick.");
    }
    internal static void OnConsume(Player instigatingPlayer, ItemConsumeableAsset consumeableAsset)
    {
        UCPlayerData? data = null;
        if (consumeableAsset.FindExplosionEffectAsset() != null)
        {
            if (instigatingPlayer.TryGetPlayerData(out data))
                data.LastExplosiveConsumed = consumeableAsset.GUID;
        }
        if (consumeableAsset.virus > consumeableAsset.disinfectant)
        {
            if (data != null || instigatingPlayer.TryGetPlayerData(out data))
            {
                data.LastInfectableConsumed = consumeableAsset.GUID;
            }
        }
        else if (consumeableAsset.disinfectant > 0 &&
                 instigatingPlayer.life.virus >= Provider.modeConfigData.Players.Virus_Infect)
        {
            if (data != null || instigatingPlayer.TryGetPlayerData(out data))
            {
                data.LastInfectableConsumed = default;
            }
        }
        if (consumeableAsset.bleedingModifier == ItemConsumeableAsset.Bleeding.Heal)
        {
            if (data != null || instigatingPlayer.TryGetPlayerData(out data))
            {
                data.LastBleedingArgs = default;
                data.LastBleedingEvent = null;
            }
        }
        else if (consumeableAsset.bleedingModifier == ItemConsumeableAsset.Bleeding.Cut)
        {
            if (data != null || instigatingPlayer.TryGetPlayerData(out data))
            {
                UCPlayer? pl = UCPlayer.FromPlayer(instigatingPlayer);
                data.LastBleedingArgs = new DeathMessageArgs()
                {
                    DeadPlayerName = pl == null ? instigatingPlayer.channel.owner.playerID.characterName : pl.Name.CharacterName,
                    DeadPlayerTeam = instigatingPlayer.GetTeam(),
                    DeathCause = EDeathCause.INFECTION,
                    ItemName = consumeableAsset.itemName,
                    ItemGuid = consumeableAsset.GUID,
                    Flags = DeathFlags.Item
                };
                data.LastBleedingEvent = new PlayerDied(UCPlayer.FromPlayer(instigatingPlayer)!)
                {
                    Cause = EDeathCause.INFECTION,
                    DeadTeam = data.LastBleedingArgs.DeadPlayerTeam
                };
            }
        }
    }
    internal static void OnEnterStorage(CSteamID instigator, InteractableStorage storage, ref bool shouldAllow)
    {
        if (storage == null ||
            !shouldAllow ||
            Gamemode.Config.TimeLimitedStorages is null ||
            !Gamemode.Config.TimeLimitedStorages.HasValue ||
            Gamemode.Config.TimeLimitedStorages.Value.Length == 0 ||
            UCWarfare.Config.MaxTimeInStorages <= 0)
            return;
        SteamPlayer player = PlayerTool.getSteamPlayer(instigator);
        BarricadeDrop storagedrop = BarricadeManager.FindBarricadeByRootTransform(storage.transform);

        if (player == null || storagedrop == null || !Gamemode.Config.TimeLimitedStorages.Value.Any(x => x.MatchGuid(storagedrop.asset.GUID)))
            return;

        UCPlayer? ucplayer = UCPlayer.FromSteamPlayer(player);
        if (ucplayer == null) return;
        if (ucplayer.StorageCoroutine != null)
            player.player.StopCoroutine(ucplayer.StorageCoroutine);
        ucplayer.StorageCoroutine = player.player.StartCoroutine(WaitToCloseStorage(ucplayer));
    }
    private static IEnumerator WaitToCloseStorage(UCPlayer player)
    {
        yield return new WaitForSecondsRealtime(UCWarfare.Config.MaxTimeInStorages);
        player.Player.inventory.closeStorageAndNotifyClient();
        player.StorageCoroutine = null;
    }
    internal static void OnBarricadeDamaged(CSteamID instigatorSteamID, Transform barricadeTransform, ref ushort pendingTotalDamage, ref bool shouldAllow, EDamageOrigin damageOrigin)
    {
        if (Data.Gamemode is TeamGamemode && TeamManager.IsInAnyMainOrAMCOrLobby(barricadeTransform.position))
        {
            shouldAllow = false;
            return;
        }

        if (Data.Gamemode != null && Data.Gamemode.UseWhitelist)
        {
            Data.Gamemode.Whitelister.OnBarricadeDamageRequested(instigatorSteamID, barricadeTransform, ref pendingTotalDamage, ref shouldAllow, damageOrigin);
            if (!shouldAllow)
                return;
        }
        BarricadeDrop drop = BarricadeManager.FindBarricadeByRootTransform(barricadeTransform);
        if (drop == null) return;

        if (Data.Gamemode is Insurgency { State: not State.Active } && Gamemode.Config.BarricadeInsurgencyCache.TryGetGuid(out Guid guid) && guid == drop.asset.GUID)
        {
            shouldAllow = false;
            return;
        }

        if (Gamemode.Config.BarricadeFOBRadioDamaged.TryGetGuid(out guid) && guid == drop.asset.GUID && instigatorSteamID != CSteamID.Nil)
        {
            shouldAllow = false;
            return;
        }

        StructureSaver? saver = Data.Singletons.GetSingleton<StructureSaver>();
        if (saver != null && saver.IsLoaded && saver.TryGetSaveNoLock(drop, out SavedStructure _))
        {
            shouldAllow = false;
            return;
        }

        if (instigatorSteamID != CSteamID.Nil && instigatorSteamID != Provider.server)
        {
            SteamPlayer? pl = PlayerTool.getSteamPlayer(instigatorSteamID);
            ulong team = drop.GetServersideData().group.GetTeam();
            if (team != 0 && pl != null && pl.GetTeam() != team)
            {
                Guid weapon;
                if (damageOrigin == EDamageOrigin.Rocket_Explosion)
                {
                    if (pl.player.TryGetPlayerData(out UCPlayerData c))
                    {
                        weapon = c.LastRocketShot;
                    }
                    else if (pl.player.equipment.asset != null)
                    {
                        weapon = pl.player.equipment.asset.GUID;
                    }
                    else weapon = Guid.Empty;
                }
                else if (damageOrigin == EDamageOrigin.Useable_Gun)
                {
                    weapon = pl.player.equipment.asset != null ? pl.player.equipment.asset.GUID : Guid.Empty;
                }
                else if (damageOrigin == EDamageOrigin.Grenade_Explosion)
                {
                    if (pl.player.TryGetPlayerData(out UCPlayerData c))
                    {
                        weapon = c.ActiveThrownItems.FirstOrDefault(x => Assets.find<ItemThrowableAsset>(x.Throwable)?.isExplosive ?? false)?.Throwable ?? Guid.Empty;
                    }
                    else if (pl.player.equipment.asset != null)
                    {
                        weapon = pl.player.equipment.asset.GUID;
                    }
                    else weapon = Guid.Empty;
                }
                else if (damageOrigin == EDamageOrigin.Trap_Explosion)
                {
                    if (pl.player.TryGetPlayerData(out UCPlayerData c) && c.ExplodingLandmine != null)
                    {
                        weapon = c.ExplodingLandmine.asset.GUID;
                    }
                    else if (pl.player.equipment.asset != null)
                    {
                        weapon = pl.player.equipment.asset.GUID;
                    }
                    else weapon = Guid.Empty;
                }
                else if (pl.player.equipment.asset != null)
                {
                    weapon = pl.player.equipment.asset.GUID;
                }
                else weapon = Guid.Empty;
                Data.Reporter?.OnDamagedStructure(instigatorSteamID.m_SteamID, new ReportSystem.Reporter.StructureDamageData()
                {
                    broke = false,
                    damage = pendingTotalDamage,
                    instId = drop.instanceID,
                    origin = damageOrigin,
                    structure = drop.asset.GUID,
                    time = Time.realtimeSinceStartup,
                    weapon = weapon
                });
            }
        }

        if (shouldAllow && pendingTotalDamage > 0)
            DestroyerComponent.AddOrUpdate(barricadeTransform.gameObject, instigatorSteamID.m_SteamID, damageOrigin);
    }
    internal static void OnStructureDamaged(CSteamID instigatorSteamID, Transform structureTransform, ref ushort pendingTotalDamage, ref bool shouldAllow, EDamageOrigin damageOrigin)
    {
        if (Data.Gamemode is TeamGamemode && TeamManager.IsInAnyMainOrAMCOrLobby(structureTransform.position))
        {
            shouldAllow = false;
            return;
        }

        if (Data.Gamemode != null && Data.Gamemode.UseWhitelist)
        {
            Data.Gamemode.Whitelister.OnStructureDamageRequested(instigatorSteamID, structureTransform, ref pendingTotalDamage, ref shouldAllow, damageOrigin);
            if (!shouldAllow)
                return;
        }
        StructureDrop drop = StructureManager.FindStructureByRootTransform(structureTransform);
        if (drop == null) return;
        StructureSaver? saver = Data.Singletons.GetSingleton<StructureSaver>();
        if (saver != null && saver.IsLoaded && saver.TryGetSaveNoLock(drop, out SavedStructure _))
        {
            shouldAllow = false;
            return;
        }

        if (instigatorSteamID != CSteamID.Nil && instigatorSteamID != Provider.server)
        {
            Guid weapon;
            SteamPlayer pl = PlayerTool.getSteamPlayer(instigatorSteamID);
            ulong team = drop.GetServersideData().group.GetTeam();
            if (team == 0 || pl == null || pl.GetTeam() != team) return;
            if (damageOrigin == EDamageOrigin.Rocket_Explosion)
            {
                if (pl.player.TryGetPlayerData(out UCPlayerData c))
                {
                    weapon = c.LastRocketShot;
                }
                else if (pl.player.equipment.asset != null)
                {
                    weapon = pl.player.equipment.asset.GUID;
                }
                else weapon = Guid.Empty;
            }
            else if (damageOrigin == EDamageOrigin.Useable_Gun)
            {
                if (pl.player.TryGetPlayerData(out UCPlayerData c))
                {
                    weapon = c.LastRocketShot;
                }
                else if (pl.player.equipment.asset != null)
                {
                    weapon = pl.player.equipment.asset.GUID;
                }
                else weapon = Guid.Empty;
            }
            else if (damageOrigin == EDamageOrigin.Grenade_Explosion)
            {
                if (pl.player.TryGetPlayerData(out UCPlayerData c))
                {
                    weapon = c.ActiveThrownItems.FirstOrDefault(x => Assets.find<ItemThrowableAsset>(x.Throwable)?.isExplosive ?? false)?.Throwable ?? Guid.Empty;
                }
                else if (pl.player.equipment.asset != null)
                {
                    weapon = pl.player.equipment.asset.GUID;
                }
                else weapon = Guid.Empty;
            }
            else if (damageOrigin == EDamageOrigin.Trap_Explosion)
            {
                if (pl.player.TryGetPlayerData(out UCPlayerData c) && c.TriggeringLandmine != null)
                {
                    weapon = c.TriggeringLandmine.asset.GUID;
                }
                else if (pl.player.equipment.asset != null)
                {
                    weapon = pl.player.equipment.asset.GUID;
                }
                else weapon = Guid.Empty;
            }
            else if (pl.player.equipment.asset != null)
            {
                weapon = pl.player.equipment.asset.GUID;
            }
            else weapon = Guid.Empty;
            Data.Reporter?.OnDamagedStructure(instigatorSteamID.m_SteamID, new ReportSystem.Reporter.StructureDamageData()
            {
                broke = false,
                damage = pendingTotalDamage,
                instId = drop.instanceID,
                origin = damageOrigin,
                structure = drop.asset.GUID,
                time = Time.realtimeSinceStartup,
                weapon = weapon
            });
        }
    }
    internal static void OnEnterVehicle(EnterVehicle e)
    {
        if (!e.Vehicle.transform.TryGetComponent(out VehicleComponent component))
        {
            component = e.Vehicle.transform.gameObject.AddComponent<VehicleComponent>();
            component.Initialize(e.Vehicle);
        }
        ActionLog.Add(ActionLogType.EnterVehicleSeat, $"{e.Vehicle.asset.vehicleName} / {e.Vehicle.asset.id} / {e.Vehicle.asset.GUID:N}, Owner: {e.Vehicle.lockedOwner.m_SteamID}, " +
                                                            $"ID: ({e.Vehicle.instanceID}) Seat move: >> " +
                                                            $"{e.PassengerIndex.ToString(Data.AdminLocale)}", e.Player.Steam64);
        component.OnPlayerEnteredVehicle(e);

        if (Data.Is<IFlagRotation>(out _) && e.Player.Player.IsOnFlag(out Flag flag))
        {
            CaptureUI.CaptureUIParameters p = CTFUI.RefreshStaticUI(e.Player.GetTeam(), flag, true);
            CTFUI.CaptureUI.Send(e.Player, in p);
        }
    }
    internal static void OnPlayerDamageRequested(ref DamagePlayerParameters parameters, ref bool shouldAllow)
    {
        if (parameters.player.movement.isSafe || !shouldAllow)
            return;
        if (Data.Gamemode.State != State.Active)
        {
            shouldAllow = false;
            return;
        }
        // Vector3 pos = parameters.player.transform.position;
        if (TeamManager.IsInAnyMainOrLobby(parameters.player))
        {
            shouldAllow = false;
            return;
        }

        UCPlayer? ucplayer = UCPlayer.FromPlayer(parameters.player);
        if (ucplayer != null && ucplayer.OnDuty() && ucplayer.GodMode)
        {
            shouldAllow = false;
            return;
        }

        UCPlayer? killer = UCPlayer.FromCSteamID(parameters.killer);

        // AMC damage multiplier application
        if (ucplayer != null && killer != null)
        {
            float targetDamageMult = ucplayer.GetAMCDamageMultiplier();
            float killerDamageMult = killer.GetAMCDamageMultiplier();
            if (targetDamageMult > 0f && killerDamageMult > 0f)
            {
                float dmgMult = Mathf.Min(targetDamageMult, killerDamageMult);

                parameters.times *= dmgMult;
            }
            else
            {
                shouldAllow = false;
                return;
            }
        }
        else if (ucplayer != null && ucplayer.SafezoneZone != null)
        {
            shouldAllow = false;
            return;
        }

        // prevent damage to crew members
        if (parameters.cause == EDeathCause.GUN)
        {
            InteractableVehicle veh = parameters.player.movement.getVehicle();
            if (veh != null && (!VehicleData.IsFlyingEngine(veh.asset.engine) || veh.speed >= 5))
            {
                VehicleBay? bay = VehicleBay.GetSingletonQuick();
                VehicleData? data = bay?.GetDataSync(veh.asset.GUID);
                if (data != null && (data.PassengersInvincible || data.CrewInvincible && Array.IndexOf(data.CrewSeats, parameters.player.movement.getSeat()) != -1))
                {
                    shouldAllow = false;
                    return;
                }
            }
        }

        StrengthInNumbers.OnPlayerDamageRequested(ref parameters);
        bool wasInjured = false;
        if (Data.Is(out IRevives rev))
        {
            wasInjured = rev.ReviveManager.IsInjured(parameters.player.channel.owner.playerID.steamID.m_SteamID);
            rev.ReviveManager.OnPlayerDamagedRequested(ref parameters, ref shouldAllow);
        }
        bool isInjured = rev != null && rev.ReviveManager.IsInjured(parameters.player.channel.owner.playerID.steamID.m_SteamID);
        byte amount = (byte)Mathf.Min(byte.MaxValue, Mathf.FloorToInt(parameters.damage * parameters.times));
        bool isBleeding;
        if (!shouldAllow)
        {
            isBleeding = false;
            if (isInjured && !wasInjured)
                goto saveRecord;
            return;
        }

        isBleeding = parameters.bleedingModifier switch
        {
            DamagePlayerParameters.Bleeding.Always => true,
            DamagePlayerParameters.Bleeding.Never => false,
            DamagePlayerParameters.Bleeding.Heal => false,
            _ => amount < parameters.player.life.health && Provider.modeConfigData.Players.Can_Start_Bleeding &&
                 amount >= 20
        };

        saveRecord:
        PlayerDied? e = null;
        if (isBleeding)
        {
            DeathTracker.OnWillStartBleeding(ref parameters);
            if (parameters.player.TryGetPlayerData(out UCPlayerData data))
                e = data.LastBleedingEvent;
        }

        if (ucplayer == null)
            return;

        if (e == null)
        {
            DeathMessageArgs args = new DeathMessageArgs();
            e = new PlayerDied(ucplayer);
            DeathTracker.FillArgs(ucplayer, parameters.cause, parameters.limb, parameters.killer, ref args, e);
        }

        bool hasInstigator = e.Instigator.BIndividualAccount() && e.Instigator.m_SteamID != ucplayer.Steam64;
        bool hasPl3 = e.Player3Id.HasValue && new CSteamID(e.Player3Id.Value).BIndividualAccount() && e.Player3Id.Value != ucplayer.Steam64 && e.Player3Id.Value != e.Instigator.m_SteamID;
        DamageRecord dmgRecord = new DamageRecord
        {
            Steam64 = ucplayer.Steam64,
            Position = ucplayer.Position,
            Team = (byte)ucplayer.GetTeam(),
            Damage = amount,
            SessionId = ucplayer.CurrentSession?.SessionId,
            Distance = e.KillDistance,
            Instigator = hasInstigator ? e.Instigator.m_SteamID : null,
            InstigatorSessionId = hasInstigator ? e.KillerSession?.SessionId : null,
            InstigatorPosition = hasInstigator ? e.KillerPoint : null,
            PrimaryAsset = e.PrimaryAsset == Guid.Empty ? null : new UnturnedAssetReference(e.PrimaryAsset),
            SecondaryAsset = e.SecondaryAsset == Guid.Empty ? null : new UnturnedAssetReference(e.SecondaryAsset),
            Vehicle = e.TurretVehicleOwner == Guid.Empty ? null : new UnturnedAssetReference(e.TurretVehicleOwner),
            IsInjure = !wasInjured && isInjured,
            IsInjured = isInjured && wasInjured,
            IsTeamkill = e.WasTeamkill,
            Limb = e.Limb,
            IsSuicide = e.WasSuicide,
            NearestLocation = F.GetClosestLocationName(ucplayer.Position, true, false),
            Cause = e.Cause,
            RelatedPlayer = hasPl3 ? e.Player3Id : null,
            RelatedPlayerPosition = hasPl3 ? e.Player3Point : null,
            RelatedPlayerSessionId = hasPl3 ? e.Player3Session?.SessionId : null,
            TimeDeployedSeconds = e.TimeDeployed,
            Timestamp = DateTimeOffset.UtcNow
        };

        ucplayer.DamageRecords.Add(dmgRecord);
    }
    internal static void OnPlayerMarkedPosOnMap(Player player, ref Vector3 position, ref string overrideText, ref bool isBeingPlaced, ref bool allowed)
    {
        if (player == null) return;
        UCPlayer? ucplayer = UCPlayer.FromPlayer(player);
        if (ucplayer == null || ucplayer.OffDuty() && (ucplayer.Squad == null || !ucplayer.IsSquadLeader()))
        {
            allowed = false;
            ucplayer?.SendChat(T.MarkerNotInSquad);
        }
    }
    internal static void OnPlayerGestureRequested(Player player, EPlayerGesture gesture, ref bool allow)
    {
        if (player == null) return;
        if (gesture == EPlayerGesture.POINT)
        {
            UCPlayer? ucplayer = UCPlayer.FromPlayer(player);
            if (ucplayer == null) return;
            if (!ucplayer.IsSquadLeader())
            {
                ucplayer.SendChat(T.MarkerNotInSquad);
                return;
            }
            if (!Physics.Raycast(new Ray(player.look.aim.transform.position, player.look.aim.transform.forward), out RaycastHit hit, 8192f, RayMasks.BLOCK_COLLISION)) return;
            PlaceMarker(ucplayer, hit.point, true, true);
        }
    }
    private static void PlaceMarker(UCPlayer ucplayer, Vector3 point, bool requireSquad, bool placeMarkerOnMap)
    {
        ThreadUtil.assertIsGameThread();
        if (placeMarkerOnMap)
            ucplayer.Player.quests.replicateSetMarker(true, point);
        EffectAsset marker = ucplayer.GetMarker();
        EffectAsset? lastping = ucplayer.LastPing;
        // todo this clears other squad-member's markers
        if (ucplayer.Squad == null)
        {
            if (requireSquad)
            {
                ucplayer.SendChat(T.MarkerNotInSquad);
                return;
            }
            if (marker == null) return;
            if (lastping != null)
                EffectManager.ClearEffectByGuid(lastping.GUID, ucplayer.Player.channel.owner.transportConnection);
            F.TriggerEffectReliable(marker, ucplayer.Player.channel.owner.transportConnection, point);
            ucplayer.LastPing = marker;
            return;
        }
        if (marker == null) return;
        for (int i = 0; i < ucplayer.Squad.Members.Count; i++)
        {
            if (lastping != null)
                EffectManager.ClearEffectByGuid(lastping.GUID, ucplayer.Squad.Members[i].Player.channel.owner.transportConnection);
            F.TriggerEffectReliable(marker, ucplayer.Squad.Members[i].Player.channel.owner.transportConnection, point);
        }
    }
    public static void ClearPlayerMarkerForSquad(UCPlayer ucplayer)
    {
        if (ucplayer.LastPing != null)
            ClearPlayerMarkerForSquad(ucplayer, ucplayer.LastPing);
    }

    public static void ClearPlayerMarkerForSquad(UCPlayer ucplayer, EffectAsset marker)
    {
        if (marker == null) return;
        ThreadUtil.assertIsGameThread();
        if (ucplayer.Squad == null)
        {
            EffectManager.ClearEffectByGuid(marker.GUID, ucplayer.Player.channel.owner.transportConnection);
            ucplayer.LastPing = null;
            return;
        }
        for (int i = 0; i < ucplayer.Squad.Members.Count; i++)
        {
            EffectManager.ClearEffectByGuid(marker.GUID, ucplayer.Squad.Members[i].Player.channel.owner.transportConnection);
            ucplayer.LastPing = null;
        }
    }

    private static readonly Guid KitRack = new Guid("7ee1d28efe904a369c93c544494fa1ef");
    internal static void OnTryStoreItem(Player player, byte page, ItemJar jar, ref bool allow)
    {
        if (!player.inventory.isStoring || player == null || jar == null || jar.item == null || allow == false) return;
        UCPlayer? ucplayer = UCPlayer.FromPlayer(player);
        if (ucplayer != null && ucplayer.OnDuty())
            return;
        if (Assets.find(EAssetType.ITEM, jar.item.id) is not ItemAsset asset) return;
        if (player.inventory.storage != null)
        {
            BarricadeDrop? drop = BarricadeManager.FindBarricadeByRootTransform(player.inventory.storage.transform);
            if (drop != null && drop.asset.GUID == KitRack)
            {
                allow = false;
                player.SendChat(T.ProhibitedStoring, asset);
                return;
            }
        }
        if (!Whitelister.IsWhitelisted(asset.GUID, out _))
        {
            allow = false;
            player.SendChat(T.ProhibitedStoring, asset);
        }
    }
    internal static void StructureMovedInWorkzone(CSteamID instigator, byte x, byte y, uint instanceID, ref Vector3 point, ref byte angleX, ref byte angleY, ref byte angleZ, ref bool shouldAllow)
    {
        StructureSaver? saver = Data.Singletons.GetSingleton<StructureSaver>();
        Vector3 pt = point, rt = F.BytesToEuler(angleX, angleY, angleZ);
        if (saver != null && saver.TryGetSaveNoLock(instanceID, StructType.Structure, out SqlItem<SavedStructure> item) && item.Item != null)
        {
            Task.Run(async () =>
            {
                await item.Enter().ConfigureAwait(false);
                try
                {
                    item.Item.Position = pt;
                    item.Item.Rotation = rt;
                    await item.SaveItem();
                }
                catch (Exception ex)
                {
                    L.LogError("Error saving structure workzone move.");
                    L.LogError(ex);
                }
                finally
                {
                    item.Release();
                }
            });
        }
    }
    internal static void BarricadeMovedInWorkzone(CSteamID instigator, byte x, byte y, ushort plant, uint instanceID, ref Vector3 point, ref byte angleX, ref byte angleY, ref byte angleZ, ref bool shouldAllow)
    {
        StructureSaver? saver = Data.Singletons.GetSingleton<StructureSaver>();
        Vector3 pt = point, rt = F.BytesToEuler(angleX, angleY, angleZ);
        if (saver != null && saver.TryGetSaveNoLock(instanceID, StructType.Barricade, out SqlItem<SavedStructure> item) && item.Item != null)
        {
            Task.Run(async () =>
            {
                await item.Enter().ConfigureAwait(false);
                try
                {
                    item.Item.Position = pt;
                    item.Item.Rotation = rt;
                    await item.SaveItem();
                }
                catch (Exception ex)
                {
                    L.LogError("Error saving structure workzone move.");
                    L.LogError(ex);
                }
                finally
                {
                    item.Release();
                }
            });
        }
        BarricadeDrop? drop = BarricadeUtility.FindBarricade(instanceID, point).Drop;
        if (drop != null && drop.asset.build is EBuild.SIGN or EBuild.SIGN_WALL)
        {
            TraitSigns.OnBarricadeMoved(drop);
        }
    }
    internal static void OnPlayerLeavesVehicle(ExitVehicle e)
    {
        if (e.Vehicle.transform.TryGetComponent(out VehicleComponent component))
        {
            component.OnPlayerExitedVehicle(e);
        }
        ActionLog.Add(ActionLogType.LeaveVehicleSeat, $"{e.Vehicle.asset.vehicleName} / {e.Vehicle.asset.id} / {e.Vehicle.asset.GUID:N}, Owner: {e.Vehicle.lockedOwner.m_SteamID}, " +
                                                         $"ID: ({e.Vehicle.instanceID})", e.Steam64);
    }
    internal static void OnVehicleSwapSeat(VehicleSwapSeat e)
    {
        InteractableVehicle vehicle = e.Vehicle;
        if (vehicle.transform.TryGetComponent(out VehicleComponent component))
        {
            component.OnPlayerSwapSeatRequested(e);
        }
        ActionLog.Add(ActionLogType.EnterVehicleSeat, $"{vehicle.asset.vehicleName} / {vehicle.asset.id} / {vehicle.asset.GUID:N}, Owner: {vehicle.lockedOwner.m_SteamID}, " +
                                                            $"ID: ({vehicle.instanceID}) Seat move: {e.OldSeat.ToString(Data.AdminLocale)} >> " +
                                                            $"{e.NewSeat.ToString(Data.AdminLocale)}", e.Player.Steam64);
    }
    internal static void BatteryStolen(SteamPlayer theif, ref bool allow)
    {
        if (!UCWarfare.Config.AllowBatteryStealing)
        {
            allow = false;
            theif.SendChat(T.NoStealingBatteries);
        }
    }
    internal static void OnCalculateSpawnDuringRevive(PlayerLife sender, bool wantsToSpawnAtHome, ref Vector3 position, ref float yaw)
    {
        ulong team = sender.player.GetTeam();
        if (team is 1 or 2)
        {
            Zone? zone = TeamManager.GetMain(team);
            if (zone != null)
            {
                position = zone.Center3D;
                yaw = TeamManager.GetMainYaw(team);
                return;
            }
        }
        position = TeamManager.LobbySpawn;
        yaw = TeamManager.LobbySpawnAngle;
    }
    internal static void OnCalculateSpawnDuringJoin(SteamPlayerID playerID, ref Vector3 point, ref float yaw, ref EPlayerStance initialStance, ref bool needsNewSpawnpoint)
    {
        needsNewSpawnpoint = false;
        // leave the player where they logged off if they logged off in the same game.
        if (PlayerSave.TryReadSaveFile(playerID.steamID.m_SteamID, out PlayerSave save))
        {
            if (Data.Gamemode is not null && Data.Gamemode.GameId == save.LastGame && !save.ShouldRespawnOnJoin)
                return;
            if (save.ShouldRespawnOnJoin)
            {
                save.ShouldRespawnOnJoin = false;
                PlayerSave.WriteToSaveFile(save);
            }
        }
        L.LogDebug("Respawning " + playerID.playerName + " in the lobby");

        point = TeamManager.LobbySpawn;
        yaw = TeamManager.LobbySpawnAngle;
        initialStance = EPlayerStance.STAND;
    }
    internal static void OnPlayerDisconnected(PlayerEvent e)
    {
        ulong s64 = e.Steam64;
        DroppedItems.Remove(s64);
        TeamManager.PlayerBaseStatus?.Remove(s64);
        Tips.OnPlayerDisconnected(s64);
        UCPlayer ucplayer = e.Player;
        try
        {
            UnturnedUIDataSource.Instance.RemovePlayer(e.Player.CSteamID);
            if (!UCWarfare.Config.DisableDailyQuests)
                DailyQuests.DeregisterDailyTrackers(ucplayer);
            QuestManager.DeregisterOwnedTrackers(ucplayer);

            EAdminType type = ucplayer.PermissionLevel;
            if ((type & EAdminType.ADMIN_ON_DUTY) == EAdminType.ADMIN_ON_DUTY)
                DutyCommand.AdminOnToOff(ucplayer);
            else if ((type & EAdminType.TRIAL_ADMIN_ON_DUTY) == EAdminType.TRIAL_ADMIN_ON_DUTY)
                DutyCommand.InternOnToOff(ucplayer);

            StatsCoroutine.RemovePlayer(ucplayer.Steam64);
            try
            {
                Data.Gamemode.PlayerLeave(ucplayer);
            }
            catch (Exception ex)
            {
                L.LogError("Error in the " + Data.Gamemode.Name + " OnPlayerLeft:");
                L.LogError(ex);
            }
            UCPlayerData? c = ucplayer.Player.GetPlayerData(out bool gotptcomp);
            if (UCWarfare.Config.EnablePlayerJoinLeaveMessages)
                Chat.Broadcast(T.PlayerDisconnected, ucplayer);
            if (gotptcomp)
            {
                c!.Stats = null!;
                ActionLog.Add(ActionLogType.Disconnect, "PLAYED FOR " + Localization.GetTimeFromSeconds(Mathf.RoundToInt(Time.realtimeSinceStartup - c.JoinTime)).ToUpper(), ucplayer.Steam64);
                Data.PlaytimeComponents.Remove(ucplayer.Steam64);
                UnityEngine.Object.Destroy(c);
            }
            else
                ActionLog.Add(ActionLogType.Disconnect, $"Players online: {Provider.clients.Count - 1}", ucplayer.Steam64);
            PlayerManager.NetCalls.SendPlayerLeft.NetInvoke(ucplayer.Steam64);
            if (e.Player.DamageRecords.Count > 0)
            {
                UCWarfare.RunTask(async () =>
                {
                    await using IStatsDbContext dbContext = new WarfareDbContext();

                    await e.Player.FlushDamages(dbContext).ConfigureAwait(false);
                    await dbContext.SaveChangesAsync();
                }, ctx: "Flushing damage records on leave.");
            }
        }
        catch (Exception ex)
        {
            L.LogError("Error in the main OnPlayerDisconnected:");
            L.LogError(ex);
        }
    }
    internal static void OnLocaleUpdated(UCPlayer player)
        => UCWarfare.I.UpdateLangs(player, false);
    internal static async Task OnPrePlayerConnect(PlayerPending e, CancellationToken token)
    {
        try
        {
            bool kick = false;
            string? cn = null;
            string? nn = null;
            string? match = Data.GetChatFilterViolation(e.PendingPlayer.playerID.playerName);
            if (match != null)
            {
                LanguageInfo langInfo = await Localization.GetLanguage(e.PendingPlayer.playerID.steamID.m_SteamID, token).ConfigureAwait(false);
                ActionLog.Add(ActionLogType.ChatFilterViolation, "PLAYER NAME: " + e.PendingPlayer.playerID.playerName, e.PendingPlayer.playerID.steamID.m_SteamID);
                throw e.Reject(T.NameProfanityPlayerNameKickMessage.Translate(langInfo, match));
            }
            match = Data.GetChatFilterViolation(e.PendingPlayer.playerID.characterName);
            if (match != null)
            {
                LanguageInfo langInfo = await Localization.GetLanguage(e.PendingPlayer.playerID.steamID.m_SteamID, token).ConfigureAwait(false);
                ActionLog.Add(ActionLogType.ChatFilterViolation, "CHARACTER NAME: " + e.PendingPlayer.playerID.characterName, e.PendingPlayer.playerID.steamID.m_SteamID);
                throw e.Reject(T.NameProfanityCharacterNameKickMessage.Translate(langInfo, match));
            }
            match = Data.GetChatFilterViolation(e.PendingPlayer.playerID.nickName);
            if (match != null)
            {
                LanguageInfo langInfo = await Localization.GetLanguage(e.PendingPlayer.playerID.steamID.m_SteamID, token).ConfigureAwait(false);
                ActionLog.Add(ActionLogType.ChatFilterViolation, "NICK NAME: " + e.PendingPlayer.playerID.nickName, e.PendingPlayer.playerID.steamID.m_SteamID);
                throw e.Reject(T.NameProfanityNickNameKickMessage.Translate(langInfo, match));
            }
            if (string.IsNullOrWhiteSpace(e.PendingPlayer.playerID.characterName))
            {
                e.PendingPlayer.playerID.characterName = e.PendingPlayer.playerID.steamID.m_SteamID.ToString(Data.LocalLocale);
                if (e.PendingPlayer.playerID.nickName.Length == 0)
                {
                    e.PendingPlayer.playerID.nickName = e.PendingPlayer.playerID.steamID.m_SteamID.ToString(Data.LocalLocale);
                }
                else
                {
                    kick = F.FilterName(e.PendingPlayer.playerID.characterName, out cn);
                    kick |= F.FilterName(e.PendingPlayer.playerID.nickName, out nn);
                }
            }
            else if (string.IsNullOrWhiteSpace(e.PendingPlayer.playerID.nickName))
            {
                e.PendingPlayer.playerID.nickName = e.PendingPlayer.playerID.steamID.m_SteamID.ToString(Data.LocalLocale);
                if (e.PendingPlayer.playerID.characterName.Length == 0)
                {
                    e.PendingPlayer.playerID.characterName = e.PendingPlayer.playerID.steamID.m_SteamID.ToString(Data.LocalLocale);
                }
                else
                {
                    kick = F.FilterName(e.PendingPlayer.playerID.characterName, out cn);
                    kick |= F.FilterName(e.PendingPlayer.playerID.nickName, out nn);
                }
            }
            else
            {
                kick = F.FilterName(e.PendingPlayer.playerID.characterName, out cn);
                kick |= F.FilterName(e.PendingPlayer.playerID.nickName, out nn);
            }
            if (kick)
            {
                LanguageInfo langInfo = await Localization.GetLanguage(e.PendingPlayer.playerID.steamID.m_SteamID, token).ConfigureAwait(false);
                throw e.Reject(T.NameFilterKickMessage.Translate(langInfo, UCWarfare.Config.MinAlphanumericStringLength));
            }

            e.PendingPlayer.playerID.characterName = Data.NameRichTextReplaceFilter.Replace(cn!, string.Empty);
            e.PendingPlayer.playerID.nickName = Data.NameRichTextReplaceFilter.Replace(nn!, string.Empty);

            if (e.PendingPlayer.playerID.characterName.Length < 3 && e.PendingPlayer.playerID.nickName.Length < 3)
            {
                LanguageInfo langInfo = await Localization.GetLanguage(e.PendingPlayer.playerID.steamID.m_SteamID, token).ConfigureAwait(false);
                throw e.Reject(T.NameFilterKickMessage.Translate(langInfo, UCWarfare.Config.MinAlphanumericStringLength));
            }
            if (e.PendingPlayer.playerID.characterName.Length < 3)
            {
                e.PendingPlayer.playerID.characterName = e.PendingPlayer.playerID.nickName;
            }
            else if (e.PendingPlayer.playerID.nickName.Length < 3)
            {
                e.PendingPlayer.playerID.nickName = e.PendingPlayer.playerID.characterName;
            }

            PlayerNames names = new PlayerNames(e.PendingPlayer.playerID);

            Data.OriginalPlayerNames[e.Steam64] = names;

            L.Log("PN: \"" + e.PendingPlayer.playerID.playerName + "\", CN: \"" + e.PendingPlayer.playerID.characterName + "\", NN: \"" + e.PendingPlayer.playerID.nickName + "\" (" + e.PendingPlayer.playerID.steamID.m_SteamID.ToString(Data.LocalLocale) + ") trying to connect.", ConsoleColor.Cyan);
        }
        catch (ControlException) { throw; }
        catch (Exception ex)
        {
            L.LogError($"Error accepting {e.PendingPlayer.playerID.playerName} in OnPrePlayerConnect:");
            L.LogError(ex);
            e.Reject("Uncreated Network was unable to connect you to to the server, try again later or contact a Director if this keeps happening (discord.gg/" + UCWarfare.Config.DiscordInviteCode + ").");
        }
    }
    internal static void OnStructureDestroyed(StructureDestroyed e)
    {
        if (e.InstigatorId != 0ul)
        {
            SteamPlayer damager = PlayerTool.getSteamPlayer(e.InstigatorId);
            ActionLog.Add(ActionLogType.DestroyStructure, 
                $"{e.Structure.asset.itemName} / {e.Structure.asset.id} / {e.Structure.asset.GUID:N} " +
                $"- Owner: {e.ServersideData.owner}, Team: {TeamManager.TranslateName(e.ServersideData.group.GetTeam())}, ID: {e.Structure.instanceID}, Origin: {e.DamageOrigin}",
                e.InstigatorId);
            if (Data.Reporter is not null && damager != null && e.ServersideData.group.GetTeam() == damager.GetTeam())
                Data.Reporter.OnDestroyedStructure(e.InstigatorId, e.InstanceID);
        }
    }
    internal static void OnBarricadeDestroyed(BarricadeDestroyed e)
    {
        IconRenderer[] iconrenderers = e.Transform.GetComponents<IconRenderer>();
        foreach (IconRenderer iconRenderer in iconrenderers)
            IconManager.DeleteIcon(iconRenderer);
        if (e.InstigatorId != 0ul)
        {
            SteamPlayer damager = PlayerTool.getSteamPlayer(e.InstigatorId);
            ActionLog.Add(ActionLogType.DestroyBarricade, $"{e.Barricade.asset.itemName} / {e.Barricade.asset.id} / {e.Barricade.asset.GUID:N} - Owner: {e.Barricade.GetServersideData().owner}, Team: {TeamManager.TranslateName(e.ServersideData.group.GetTeam())}, ID: {e.Barricade.instanceID}, Origin: {e.DamageOrigin}", e.InstigatorId);
            if (Data.Reporter is not null && damager != null && e.ServersideData.group.GetTeam() == damager.GetTeam())
                Data.Reporter.OnDestroyedStructure(e.InstigatorId, e.InstanceID);
        }
    }
    internal static void OnPostHealedPlayer(Player instigator, Player target)
    {
        UCPlayer? pl = UCPlayer.FromPlayer(target);
        UCPlayer? pl2 = UCPlayer.FromPlayer(instigator);
        if (instigator.equipment.asset is ItemConsumeableAsset asset)
        {
            if (asset.bleedingModifier == ItemConsumeableAsset.Bleeding.Heal)
            {
                if (target.TryGetPlayerData(out UCPlayerData data))
                {
                    data.LastBleedingArgs = default;
                    data.LastBleedingEvent = null;
                }
            }
            else if (asset.bleedingModifier == ItemConsumeableAsset.Bleeding.Cut)
            {
                if (target.TryGetPlayerData(out UCPlayerData data))
                {
                    data.LastBleedingArgs = new DeathMessageArgs
                    {
                        DeadPlayerName = pl == null ? target.channel.owner.playerID.characterName : pl.Name.CharacterName,
                        DeadPlayerTeam = target.GetTeam(),
                        DeathCause = EDeathCause.INFECTION,
                        ItemName = asset.itemName,
                        ItemGuid = asset.GUID,
                        Flags = DeathFlags.Item | DeathFlags.Killer,
                        KillerName = pl2 == null ? instigator.channel.owner.playerID.characterName : pl2.Name.CharacterName,
                        KillerTeam = instigator.GetTeam()
                    };
                    data.LastBleedingArgs.IsTeamkill = data.LastBleedingArgs.DeadPlayerTeam == data.LastBleedingArgs.KillerTeam;
                    data.LastBleedingEvent = new PlayerDied(UCPlayer.FromPlayer(target)!)
                    {
                        Cause = EDeathCause.INFECTION,
                        DeadTeam = data.LastBleedingArgs.DeadPlayerTeam,
                        Instigator = instigator.channel.owner.playerID.steamID,
                        Killer = UCPlayer.FromPlayer(instigator),
                        KillerTeam = data.LastBleedingArgs.KillerTeam,
                        WasTeamkill = data.LastBleedingArgs.IsTeamkill
                    };
                }
            }
        }
        if (Data.Is(out IRevives r))
        {
            if (pl2 is not null && pl is not null)
                r.ReviveManager.OnPlayerHealed(pl2, pl);
        }
    }
    internal static void OnGameUpdateDetected(string newVersion, ref bool shouldShutdown)
    {
        shouldShutdown = false;
        ShutdownCommand.ShutdownAfterGame("Unturned update \"v" + newVersion + "\".", false);
        UCWarfare.I.LastUpdateDetected = Time.realtimeSinceStartup;
    }
    internal static void OnCraftRequested(CraftRequested e)
    {
        if (Data.Gamemode is null)
            goto skip;

        Data.Gamemode.OnCraftRequestedIntl(e);
        if (!e.CanContinue)
            return;

        EBlueprintType type = e.Blueprint.type;
        switch (type)
        {
            case EBlueprintType.AMMO:
                if (!Gamemode.Config.GeneralAllowCraftingAmmo)
                    goto skip;
                break;
            case EBlueprintType.REPAIR:
                if (!Gamemode.Config.GeneralAllowCraftingRepair)
                    goto skip;
                break;
            default:
                if (!Gamemode.Config.GeneralAllowCraftingOthers)
                    goto skip;
                break;
        }
        return;
    skip:
        e.Player.SendChat(T.NoCraftingBlueprint);
        e.Break();
    }
    internal static void ChangeBarrelRequested(PlayerEquipment equipment, UseableGun gun, Item olditem, ItemJar newitem, ref bool shouldallow)
    {
        if (!HolidayUtil.isHolidayActive(ENPCHoliday.APRIL_FOOLS))
            return;

        if (olditem?.GetAsset() is not { } asset || !Gamemode.Config.ItemAprilFoolsBarrel.MatchGuid(asset.GUID))
            return;

        if (UCPlayer.FromPlayer(equipment.player) is { } player && player.OnDuty())
            return;

        shouldallow = false;
    }
    internal static void OnPlayerAided(PlayerAided e)
    {
        AidRecord record = new AidRecord
        {
            Steam64 = e.Player.Steam64,
            SessionId = e.Player.CurrentSession?.SessionId,
            Team = (byte)e.Player.GetTeam(),
            Position = e.Player.Position,
            Health = e.IsRevive ? 0 : e.Player.Player.life.health,
            IsRevive = e.IsRevive,
            Item = new UnturnedAssetReference(e.AidItem.GUID),
            Instigator = e.MedicId,
            InstigatorSessionId = e.Medic.CurrentSession?.SessionId,
            InstigatorPosition = e.Medic.Position,
            NearestLocation = F.GetClosestLocationName(e.Player.Position, true, false),
            Timestamp = DateTimeOffset.UtcNow
        };

        UCWarfare.RunTask(async () =>
        {
            await using IStatsDbContext dbContext = new WarfareDbContext();

            dbContext.AidRecords.Add(record);
            await dbContext.SaveChangesAsync();
        }, ctx: $"Add aid record for {e.MedicId} healing {e.Player.Steam64}.");
    }
    internal static void OnPlayerDied(PlayerDied e)
    {
        bool hasInst = e.Instigator.BIndividualAccount() && e.Instigator.m_SteamID != e.Steam64;
        bool hasPl3 = e.Player3Id.HasValue && new CSteamID(e.Player3Id.Value).BIndividualAccount() && e.Instigator.m_SteamID != e.Player3Id.Value && e.Player3Id.Value != e.Steam64;
        DeathRecord record = new DeathRecord
        {
            Steam64 = e.Steam64,
            Team = (byte)e.DeadTeam,
            Position = e.Point,
            SessionId = e.Session?.SessionId,
            DeathCause = e.Cause,
            DeathMessage = e.DefaultMessage ?? string.Empty,
            Distance = e.KillDistance,
            Instigator = hasInst ? e.Instigator.m_SteamID : null,
            InstigatorPosition = hasInst ? e.KillerPoint : null,
            InstigatorSessionId = hasInst ? e.KillerSession?.SessionId : null,
            IsSuicide = e.WasSuicide,
            IsTeamkill = e.WasTeamkill,
            NearestLocation = F.GetClosestLocationName(e.Point, true, false),
            PrimaryAsset = e.PrimaryAsset == Guid.Empty ? null : new UnturnedAssetReference(e.PrimaryAsset),
            SecondaryAsset = e.SecondaryAsset == Guid.Empty ? null : new UnturnedAssetReference(e.SecondaryAsset),
            Vehicle = e.TurretVehicleOwner == Guid.Empty ? null : new UnturnedAssetReference(e.TurretVehicleOwner),
            RelatedPlayer = hasPl3 ? e.Player3Id : null,
            RelatedPlayerPosition = hasPl3 ? e.Player3Point : null,
            RelatedPlayerSessionId = hasPl3 ? e.Player3Session?.SessionId : null,
            TimeDeployedSeconds = e.TimeDeployed,
            Timestamp = DateTimeOffset.UtcNow
        };

        if (e.Player.DamageRecords.Count > 0)
            record.KillShot = e.Player.DamageRecords[^1];

        UCWarfare.RunTask(async () =>
        {
            await using IStatsDbContext dbContext = new WarfareDbContext();

            dbContext.DeathRecords.Add(record);
            await e.Player.FlushDamages(dbContext).ConfigureAwait(false);
            await dbContext.SaveChangesAsync();
        }, ctx: $"Add death record for {e.Steam64} ({e.Cause}).");
    }
}
#pragma warning restore IDE0060 // Remove unused parameter
