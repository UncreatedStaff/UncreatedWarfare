using SDG.Unturned;
using Steamworks;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Uncreated.Framework;
using Uncreated.Players;
using Uncreated.Warfare.Components;
using Uncreated.Warfare.Deaths;
using Uncreated.Warfare.Events;
using Uncreated.Warfare.Events.Barricades;
using Uncreated.Warfare.Events.Components;
using Uncreated.Warfare.Events.Players;
using Uncreated.Warfare.Events.Vehicles;
using Uncreated.Warfare.FOBs;
using Uncreated.Warfare.Gamemodes;
using Uncreated.Warfare.Gamemodes.Flags;
using Uncreated.Warfare.Gamemodes.Flags.TeamCTF;
using Uncreated.Warfare.Gamemodes.Interfaces;
using Uncreated.Warfare.Kits;
using Uncreated.Warfare.Point;
using Uncreated.Warfare.Squads;
using Uncreated.Warfare.Teams;
using Uncreated.Warfare.Vehicles;
using UnityEngine;
using static Uncreated.Warfare.Gamemodes.Flags.UI.CaptureUI;
using Flag = Uncreated.Warfare.Gamemodes.Flags.Flag;

#pragma warning disable IDE0060 // Remove unused parameter
namespace Uncreated.Warfare;

public static class EventFunctions
{
    internal static Dictionary<Item, PlayerInventory> itemstemp = new Dictionary<Item, PlayerInventory>();
    internal static Dictionary<ulong, List<uint>> droppeditems = new Dictionary<ulong, List<uint>>();
    internal static Dictionary<uint, ulong> droppeditemsInverse = new Dictionary<uint, ulong>();
    internal static void OnDropItemTry(PlayerInventory inv, Item item, ref bool allow)
    {
        if (!itemstemp.ContainsKey(item))
            itemstemp.Add(item, inv);
        else itemstemp[item] = inv;
    }
    internal static void OnDropItemFinal(Item item, ref Vector3 location, ref bool shouldAllow)
    {
#if DEBUG
        using IDisposable profiler = ProfilingUtils.StartTracking();
#endif
        if (itemstemp.TryGetValue(item, out PlayerInventory inv))
        {
            uint nextindex;
            try
            {
                nextindex = (uint)Data.ItemManagerInstanceCount.GetValue(null);
            }
            catch
            {
                L.LogError("Unable to get ItemManager.instanceCount.");
                itemstemp.Remove(item);
                return;
            }
            nextindex++;
            if (droppeditems.TryGetValue(inv.player.channel.owner.playerID.steamID.m_SteamID, out List<uint> instanceids))
            {
                if (instanceids == null) droppeditems[inv.player.channel.owner.playerID.steamID.m_SteamID] = new List<uint>() { nextindex };
                else instanceids.Add(nextindex);
            }
            else
            {
                droppeditems.Add(inv.player.channel.owner.playerID.steamID.m_SteamID, new List<uint>() { nextindex });
            }

            if (!droppeditemsInverse.ContainsKey(nextindex))
                droppeditemsInverse.Add(nextindex, inv.player.channel.owner.playerID.steamID.m_SteamID);
            else
                droppeditemsInverse[nextindex] = inv.player.channel.owner.playerID.steamID.m_SteamID;

            itemstemp.Remove(item);
        }
    }
    internal static void StopCosmeticsToggleEvent(ref EVisualToggleType type, SteamPlayer player, ref bool allow)
    {
        if (!UCWarfare.Config.AllowCosmetics) allow = false;
    }
    internal static void StopCosmeticsSetStateEvent(ref EVisualToggleType type, SteamPlayer player, ref bool state, ref bool allow)
    {
        if (!UCWarfare.Config.AllowCosmetics) state = false;
    }
    internal static void OnStructurePlaced(StructureRegion region, StructureDrop drop)
    {
        SDG.Unturned.StructureData data = drop.GetServersideData();
        ActionLogger.Add(EActionLogType.PLACE_STRUCTURE, $"{drop.asset.itemName} / {drop.asset.id} / {drop.asset.GUID:N} - Team: {TeamManager.TranslateName(data.group.GetTeam(), 0)}, ID: {drop.instanceID}", data.owner);
    }
    internal static void OnBarricadePlaced(BarricadeRegion region, BarricadeDrop drop)
    {
#if DEBUG
        using IDisposable profiler = ProfilingUtils.StartTracking();
#endif
        SDG.Unturned.BarricadeData data = drop.GetServersideData();

        BarricadeComponent owner = drop.model.gameObject.AddComponent<BarricadeComponent>();
        owner.Owner = data.owner;
        SteamPlayer player = PlayerTool.getSteamPlayer(data.owner);
        owner.Player = player?.player;
        owner.BarricadeGUID = data.barricade.asset.GUID;

        ActionLogger.Add(EActionLogType.PLACE_BARRICADE, $"{drop.asset.itemName} / {drop.asset.id} / {drop.asset.GUID:N} - Team: {TeamManager.TranslateName(data.group.GetTeam(), 0)}, ID: {drop.instanceID}", data.owner);

        RallyManager.OnBarricadePlaced(drop, region);

        RepairManager.OnBarricadePlaced(drop, region);

        if (Gamemode.Config.Barricades.FOBRadioGUIDs == null) return;

        // ammo bag
        if (Gamemode.Config.Barricades.AmmoBagGUID.ValidReference(out Guid guid) && guid == data.barricade.asset.GUID)
            drop.model.gameObject.AddComponent<AmmoBagComponent>().Initialize(data, drop);

    }
    internal static void ProjectileSpawned(UseableGun gun, GameObject projectile)
    {
#if DEBUG
        using IDisposable profiler = ProfilingUtils.StartTracking();
#endif
        const float RADIUS = 3;
        const float LENGTH = 700;
        const int RAY_MASK = RayMasks.VEHICLE | RayMasks.PLAYER | RayMasks.BARRICADE | RayMasks.LARGE |
                             RayMasks.MEDIUM | RayMasks.GROUND | RayMasks.GROUND2;
        const int RAY_MASK_BACKUP = RayMasks.VEHICLE | RayMasks.PLAYER | RayMasks.BARRICADE;
        if (gun.isAiming && gun.equippedGunAsset.GUID == SpottedComponent.LaserDesignatorGUID)
        {
            float grndDist = float.NaN;
            if (Physics.Raycast(projectile.transform.position, projectile.transform.up, out RaycastHit hit, LENGTH,
                    RAY_MASK))
            {
                if (hit.transform != null)
                {
                    if ((ELayerMask)hit.transform.gameObject.layer is ELayerMask.GROUND or ELayerMask.GROUND2
                        or ELayerMask.LARGE or ELayerMask.MEDIUM or ELayerMask.SMALL)
                        grndDist = (projectile.transform.position - hit.transform.position).sqrMagnitude;
                    else
                    {
                        SpottedComponent.MarkTarget(hit.transform, gun.player);
                        return;
                    }
                }
            }

            List<RaycastHit> hits = new List<RaycastHit>(Physics.SphereCastAll(projectile.transform.position, RADIUS,
                projectile.transform.up, LENGTH, RAY_MASK_BACKUP));
            Vector3 strtPos = projectile.transform.position;
            hits.RemoveAll(
                x =>
            {
                if (x.transform == null || !x.transform.gameObject.TryGetComponent<SpottedComponent>(out _))
                    return true;
                float dist = (x.transform.position - strtPos).sqrMagnitude;
                return dist < RADIUS * RADIUS + 1 || dist > grndDist;
            });
            if (hits.Count == 0) return;
            if (hits.Count == 1)
            {
                SpottedComponent.MarkTarget(hits[0].transform, gun.player);
                return;
            }
            hits.Sort((a, b) =>
            {
                return (strtPos - b.point).sqrMagnitude.CompareTo((strtPos - a.point).sqrMagnitude);
            });
            hits.Sort((a, b) =>
            {
                return (ELayerMask)a.transform.gameObject.layer is ELayerMask.PLAYER ? -1 : 1;
            });

            SpottedComponent.MarkTarget(hits[0].transform, gun.player);
            return;
        }

        Rocket[] rockets = projectile.GetComponentsInChildren<Rocket>(true);
        foreach (Rocket rocket in rockets)
        {
            rocket.killer = gun.player.channel.owner.playerID.steamID;
        }

        if (VehicleBay.Config.TOWMissileWeapons.HasGUID(gun.equippedGunAsset.GUID))
            projectile.AddComponent<GuidedMissileComponent>().Initialize(projectile, gun.player, 90, 0.33f, 800);
        else if (VehicleBay.Config.GroundAAWeapons.HasGUID(gun.equippedGunAsset.GUID))
            projectile.AddComponent<HeatSeakingMissileComponent>().Initialize(projectile, gun.player, 150, 5f, 1000, 4, 0.33f);
        else if (VehicleBay.Config.AirAAWeapons.HasGUID(gun.equippedGunAsset.GUID))
            projectile.AddComponent<HeatSeakingMissileComponent>().Initialize(projectile, gun.player, 150, 5f, 1000, 10, 0f);
        else if (VehicleBay.Config.LaserGuidedWeapons.HasGUID(gun.equippedGunAsset.GUID))
            projectile.AddComponent<LaserGuidedMissileComponent>().Initialize(projectile, gun.player, 120, 1.15f, 150, 15, 0.6f);

        Patches.DeathsPatches.lastProjected = projectile;
        if (gun.player.TryGetPlayerData(out UCPlayerData c))
        {
            c.LastRocketShot = gun.equippedGunAsset.GUID;
            c.LastRocketShotVehicle = default;
            InteractableVehicle? veh = gun.player.movement.getVehicle();
            if (veh != null)
            {
                for (int i = 0; i < veh.turrets.Length; ++i)
                {
                    if (veh.turrets[i].turret != null && veh.turrets[i].turret.itemID == gun.equippedGunAsset.id)
                    {
                        c.LastRocketShotVehicle = veh.asset.GUID;
                        break;
                    }
                }
            }
        }
    }
    internal static void BulletSpawned(UseableGun gun, BulletInfo bullet)
    {
#if DEBUG
        using IDisposable profiler = ProfilingUtils.StartTracking();
#endif
        UCPlayerData? c = gun.player.GetPlayerData(out bool success);
        if (c != null)
        {
            c.LastGunShot = gun.equippedGunAsset.GUID;
        }
    }
    internal static void ReloadCommand_onTranslationsReloaded()
    {
#if DEBUG
        using IDisposable profiler = ProfilingUtils.StartTracking();
#endif
        foreach (UCPlayer player in PlayerManager.OnlinePlayers)
            UCWarfare.I.UpdateLangs(player);
    }

    internal static void OnLandmineExploding(LandmineExploding e)
    {
        if (UCWarfare.Config.BlockLandmineFriendlyFire && e.Triggerer.GetTeam() == e.TrapBarricade.GetServersideData().group.GetTeam())
        {
            e.Break();
        }
        else
        {
            if (!CheckLandminePosition(e.TrapBarricade.model.transform.position))
                e.Break();
        }
    }
    private static bool CheckLandminePosition(Vector3 position)
    {
        return !(TeamManager.IsInAnyMainOrAMCOrLobby(position) || FOBManager.Loaded && FOBManager.IsPointInFOB(position, out _, out _));
    }
    internal static void OnBarricadeTryPlaced(Barricade barricade, ItemBarricadeAsset asset, Transform hit, ref Vector3 point, ref float angle_x,
        ref float angle_y, ref float angle_z, ref ulong owner, ref ulong group, ref bool shouldAllow)
    {
#if DEBUG
        using IDisposable profiler = ProfilingUtils.StartTracking();
#endif
        try
        {
            if (!shouldAllow) return;
            UCPlayer? player = UCPlayer.FromID(owner);
            if (player is null)
            {
                shouldAllow = false;
                return;
            }

            bool perms = player.OnDuty();
            if (asset.build == EBuild.SPIKE || asset.build == EBuild.WIRE)
            {
                if (!perms && !CheckLandminePosition(point))
                {
                    shouldAllow = false;
                    player.SendChat("no_place_trap", asset.itemName, asset.itemName.An());
                    return;
                }
            }

            if (hit != null && hit.TryGetComponent<InteractableVehicle>(out _))
            {
                if (!UCWarfare.Config.ModerationSettings.AllowedBarricadesOnVehicles.Contains(asset.id))
                {
                    if (!perms)
                    {
                        shouldAllow = false;
                        player.SendChat("no_placement_on_vehicle", asset.itemName, asset.itemName.An());
                        return;
                    }
                }
            }
            RallyManager.OnBarricadePlaceRequested(barricade, asset, hit, ref point, ref angle_x, ref angle_y, ref angle_z, ref owner, ref group, ref shouldAllow);
            if (!shouldAllow) return;

            if (Gamemode.Config.Barricades.AmmoBagGUID.ValidReference(out Guid guid) && guid == barricade.asset.GUID)
            {
                if (!perms && player.KitClass != EClass.RIFLEMAN)
                {
                    shouldAllow = false;
                    player.SendChat("ammo_not_rifleman");
                    return;
                }
            }
            if (Data.Gamemode.UseWhitelist && hit != null)
                Data.Gamemode.Whitelister.OnBarricadePlaceRequested(barricade, asset, hit, ref point, ref angle_x, ref angle_y, ref angle_z, ref owner, ref group, ref shouldAllow);
            if (!(shouldAllow && Data.Gamemode is TeamGamemode)) return;
            ulong team = group.GetTeam();
            if (!perms && TeamManager.IsInAnyMainOrAMCOrLobby(point))
            {
                shouldAllow = false;
                player.Message("whitelist_noplace");
                return;
            }

            if (Gamemode.Config.Barricades.FOBRadioGUIDs.Value.Any(g => g == barricade.asset.GUID))
            {
                shouldAllow = BuildableComponent.TryPlaceRadio(barricade, player, point);
                return;
            }

            BuildableData buildable = FOBManager.Config.Buildables.Find(b => b.Foundation == barricade.asset.GUID);

            if (buildable != null)
            {
                shouldAllow = BuildableComponent.TryPlaceBuildable(barricade, buildable, player, point);
                return;
            }
        }
        catch (Exception ex)
        {
            L.LogError("Error in OnBarricadeTryPlaced:");
            L.LogError(ex);
            shouldAllow = false;
        }
    }
    internal static void OnPreVehicleDamage(CSteamID instigatorSteamID, InteractableVehicle vehicle, ref ushort pendingTotalDamage, ref bool canRepair, ref bool shouldAllow, EDamageOrigin damageOrigin)
    {
#if DEBUG
        using IDisposable profiler = ProfilingUtils.StartTracking();
#endif
        if (F.IsInMain(vehicle.transform.position))
        {
            shouldAllow = false;
            return;
        }

        if (shouldAllow)
        {
            if (damageOrigin == EDamageOrigin.Vehicle_Collision_Self_Damage && !(vehicle.asset.engine == EEngine.HELICOPTER || vehicle.asset.engine == EEngine.PLANE))
            {
                pendingTotalDamage = (ushort)Mathf.RoundToInt(pendingTotalDamage * 0.13f);
            }

            if (damageOrigin == EDamageOrigin.Useable_Gun)
            {
                if ((vehicle.asset.engine == EEngine.HELICOPTER || vehicle.asset.engine == EEngine.PLANE) && pendingTotalDamage > vehicle.health && pendingTotalDamage > 200)
                {
                    canRepair = false;
                }

                VehicleDamageCalculator.ApplyAdvancedDamage(vehicle, ref pendingTotalDamage);
            }
            //if (damageOrigin == EDamageOrigin.Rocket_Explosion)
            //    VehicleDamageCalculator.ApplyAdvancedDamage(vehicle, ref pendingTotalDamage);

            if (!vehicle.TryGetComponent(out VehicleComponent c))
            {
                c = vehicle.gameObject.AddComponent<VehicleComponent>();
                c.Initialize(vehicle);
            }
            UCPlayer? instigator = UCPlayer.FromCSteamID(instigatorSteamID);
            c.LastDamageOrigin = damageOrigin;
            c.LastInstigator = instigatorSteamID.m_SteamID;
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
                            ThrowableComponent a = instigatorData.ActiveThrownItems.FirstOrDefault(x => x.IsExplosive);
                            if (a != null)
                                c.LastItem = a.Throwable;
                        }
                        break;
                    case EDamageOrigin.Rocket_Explosion:
                        if (instigatorData != null) c.LastItem = instigatorData.LastRocketShot;
                        break;
                    case EDamageOrigin.Vehicle_Explosion:
                        if (instigatorData != null)
                        {
                            c.LastItemIsVehicle = true;
                            c.LastItem = instigatorData.lastExplodedVehicle;
                        }
                        break;
                    case EDamageOrigin.Bullet_Explosion:
                    case EDamageOrigin.Useable_Melee:
                    case EDamageOrigin.Useable_Gun:
                        if (instigator != null && instigator.Player.equipment.asset != null) c.LastItem = instigator.Player.equipment.asset.GUID;
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
        }
    }

    internal static void OnPostPlayerConnected(PlayerJoined e)
    {
#if DEBUG
        using IDisposable profiler = ProfilingUtils.StartTracking();
#endif
        try
        {
            // reset the player to spawn if they have joined in a different game as they last played in.
            UCPlayer ucplayer = e.Player;

            bool g = Data.Is(out ITeams t);
            bool shouldRespawn = false;
            bool isNewPlayer = e.IsNewPlayer;
            if (!isNewPlayer && (e.SaveData!.LastGame != Data.Gamemode.GameID || e.SaveData.ShouldRespawnOnJoin))
            {
                shouldRespawn = true;

                e.SaveData.ShouldRespawnOnJoin = false;
            }

            if (e.SaveData is not null)
                e.SaveData.LastGame = Data.Gamemode.GameID;

            if (ucplayer.Player.life.isDead)
                ucplayer.Player.life.ServerRespawn(false);
            else if (shouldRespawn)
            {
                ucplayer.Player.life.sendRevive();
                ucplayer.Player.teleportToLocation(F.GetBaseSpawn(ucplayer.Player, out ulong team2), team2.GetBaseAngle());
            }

            PlayerManager.ApplyTo(ucplayer);

            ulong team = ucplayer.GetTeam();
            FPlayerName names = F.GetPlayerOriginalNames(ucplayer);
            if (Data.Is<ITeams>())
            {
                ToastMessage.QueueMessage(ucplayer, new ToastMessage(Localization.Translate(isNewPlayer ? "welcome_message_first_time" : "welcome_message", ucplayer,
                    UCWarfare.GetColorHex("uncreated"), names.CharacterName, TeamManager.GetTeamHexColor(team)), EToastMessageSeverity.INFO));
            }
            else
            {
                ToastMessage.QueueMessage(ucplayer, new ToastMessage(Localization.Translate(isNewPlayer ? "welcome_message_first_time" : "welcome_message", ucplayer,
                    UCWarfare.GetColorHex("uncreated"), names.CharacterName, UCWarfare.GetColorHex("neutral")), EToastMessageSeverity.INFO));
            }
            if (Data.PlaytimeComponents.ContainsKey(ucplayer.Steam64))
            {
                UnityEngine.Object.DestroyImmediate(Data.PlaytimeComponents[ucplayer.Steam64]);
                Data.PlaytimeComponents.Remove(ucplayer.Steam64);
            }
            ucplayer.Player.transform.gameObject.AddComponent<SpottedComponent>().Initialize(SpottedComponent.ESpotted.INFANTRY);
            if (e.Steam64 == 76561198267927009) ucplayer.Player.channel.owner.isAdmin = true;
            UCPlayerData pt = ucplayer.Player.transform.gameObject.AddComponent<UCPlayerData>();
            pt.StartTracking(ucplayer.Player);
            Data.PlaytimeComponents.Add(ucplayer.Steam64, pt);
            Task.Run(async () =>
            {
                Task t1 = Data.DatabaseManager.CheckUpdateUsernames(names);
                Task<int> t2 = Data.DatabaseManager.GetXP(ucplayer.Steam64, team);
                Task<int> t3 = Data.DatabaseManager.GetCredits(ucplayer.Steam64, team);
                Task t4 = ucplayer.DownloadKits();
                await Data.DatabaseManager.RegisterLogin(ucplayer.Player);
                await t1;
                ucplayer.CachedXP = await t2;
                ucplayer.CachedCredits = await t3;
                await t4;
                await OffenseManager.ApplyMuteSettings(ucplayer);
                await UCWarfare.ToUpdate();
                RequestSigns.UpdateAllSigns(ucplayer.Player.channel.owner);
                VehicleSpawner.UpdateSigns(ucplayer);
                Points.UpdateCreditsUI(ucplayer);
                Points.UpdateXPUI(ucplayer);
                try
                {
                    Data.Gamemode.InternalOnAsyncInitComplete(ucplayer);
                }
                catch (Exception ex)
                {
                    L.LogError("Error in the " + Data.Gamemode.Name + " OnAsyncInitComplete:");
                    L.LogError(ex);
                }
            }).ConfigureAwait(false);

            try
            {
                Data.Gamemode.OnPlayerJoined(ucplayer);
            }
            catch (Exception ex)
            {
                L.LogError("Error in the " + Data.Gamemode.Name + " OnPlayerJoined:");
                L.LogError(ex);
            }
            ucplayer.Player.gameObject.AddComponent<ZonePlayerComponent>().Init(ucplayer);
            ActionLogger.Add(EActionLogType.CONNECT, $"Players online: {Provider.clients.Count}", ucplayer);
            Chat.Broadcast("player_connected", names.CharacterName);
            Data.Reporter?.OnPlayerJoin(ucplayer.SteamPlayer);
            if (ucplayer != null)
            {
                PlayerManager.NetCalls.SendPlayerJoined.NetInvoke(new PlayerListEntry
                {
                    Duty = ucplayer.OnDuty(),
                    Name = names.CharacterName,
                    Steam64 = ucplayer.Steam64,
                    Team = ucplayer.Player.GetTeamByte()
                });
                //Quests.DailyQuests.RegisterDailyTrackers(ucplayer);
                //KitManager.OnPlayerJoinedQuestHandling(ucplayer);
                //VehicleBay.OnPlayerJoinedQuestHandling(ucplayer);
                //Ranks.RankManager.OnPlayerJoin(ucplayer);
                IconManager.DrawNewMarkers(ucplayer, false);
            }
        }
        catch (Exception ex)
        {
            L.LogError("Error in the main OnPostPlayerConnected:");
            L.LogError(ex);
        }
    }
    private static readonly PlayerVoice.RelayVoiceCullingHandler NO_COMMS = (a, b) => false;
    internal static void OnRelayVoice2(PlayerVoice speaker, bool wantsToUseWalkieTalkie, ref bool shouldAllow,
        ref bool shouldBroadcastOverRadio, ref PlayerVoice.RelayVoiceCullingHandler cullingHandler)
    {
        if (Data.Gamemode is null)
        {
            cullingHandler = NO_COMMS;
            shouldBroadcastOverRadio = false;
            return;
        }
        bool isMuted = false;
        UCPlayer? ucplayer = PlayerManager.FromID(speaker.channel.owner.playerID.steamID.m_SteamID);
        if (ucplayer is not null)
        {
            if (ucplayer.MuteType != Commands.EMuteType.NONE && ucplayer.TimeUnmuted > DateTime.Now)
                isMuted = true;

            ucplayer.OnUseVoice(isMuted);
        }
        if (isMuted)
        {
            shouldAllow = false;
            cullingHandler = NO_COMMS;
            shouldBroadcastOverRadio = false;
        }
        else if (Data.Gamemode.State is EState.FINISHED or EState.LOADING)
        {
            if (!UCWarfare.Config.RelayMicsDuringEndScreen)
            {
                shouldAllow = false;
                cullingHandler = NO_COMMS;
                shouldBroadcastOverRadio = false;
                return;
            }
        }
    }
    internal static void OnRelayVoice(PlayerVoice speaker, bool wantsToUseWalkieTalkie, ref bool shouldAllow,
        ref bool shouldBroadcastOverRadio, ref PlayerVoice.RelayVoiceCullingHandler cullingHandler)
    {
#if DEBUG
        using IDisposable profiler = ProfilingUtils.StartTracking();
#endif
        if (Data.Gamemode is null)
        {
            cullingHandler = NO_COMMS;
            shouldBroadcastOverRadio = false;
            shouldAllow = false;
            return;
        }
        bool isMuted = false;
        bool squad = false;
        ulong team = 0;
        UCPlayer? ucplayer = PlayerManager.FromID(speaker.channel.owner.playerID.steamID.m_SteamID);
        if (ucplayer is not null)
        {
            ucplayer.LastSpoken = Time.realtimeSinceStartup;
            team = ucplayer.GetTeam();
            if (ucplayer.MuteType != Commands.EMuteType.NONE && ucplayer.TimeUnmuted > DateTime.Now)
                isMuted = true;
            else if (ucplayer.Squad is not null && ucplayer.Squad.Members.Count > 1)
            {
                shouldBroadcastOverRadio = true;
                squad = true;
            }
        }
        if (isMuted)
        {
            cullingHandler = NO_COMMS;
            shouldBroadcastOverRadio = false;
            shouldAllow = false;
            return;
        }
        switch (Data.Gamemode.State)
        {
            case EState.STAGING:
            case EState.ACTIVE:
                if (squad)
                {
                    bool CullingHandler(PlayerVoice source, PlayerVoice target)
                    {
                        if (ucplayer != null)
                        {
                            UCPlayer? targetPl = PlayerManager.FromID(target.channel.owner.playerID.steamID.m_SteamID);
                            if (targetPl != null && targetPl.Squad == ucplayer.Squad)
                            {
                                return targetPl.GetTeam() == team || PlayerVoice.handleRelayVoiceCulling_Proximity(source, target);
                            }
                        }
                        return PlayerVoice.handleRelayVoiceCulling_Proximity(source, target);
                    }
                    cullingHandler = CullingHandler;
                    shouldBroadcastOverRadio = true;
                    return;
                }
                return;
            case EState.LOADING:
            case EState.FINISHED:
                if (!UCWarfare.Config.RelayMicsDuringEndScreen)
                {
                    cullingHandler = NO_COMMS;
                    shouldBroadcastOverRadio = false;
                    shouldAllow = false;
                    return;
                }
                break;
        }


        shouldBroadcastOverRadio = false;
    }

    internal static void OnBattleyeKicked(SteamPlayer client, string reason)
    {
#if DEBUG
        using IDisposable profiler = ProfilingUtils.StartTracking();
#endif
        FPlayerName names = F.GetPlayerOriginalNames(client.player);
        ulong team = client.GetTeam();
        Chat.Broadcast("battleye_kick_broadcast", F.ColorizeName(names.CharacterName, team));
        L.Log(Localization.Translate("battleye_kick_console", 0, out _, names.PlayerName, client.playerID.steamID.m_SteamID.ToString(), reason));
        if (UCWarfare.Config.ModerationSettings.BattleyeExclusions != null &&
            !UCWarfare.Config.ModerationSettings.BattleyeExclusions.Contains(reason))
        {
            ulong id = client.playerID.steamID.m_SteamID;
            OffenseManager.LogBattlEyeKicksPlayer(id, reason, DateTime.Now);
        }
    }
    internal static void OnConsume(Player instigatingPlayer, ItemConsumeableAsset consumeableAsset)
    {
        UCPlayerData? data = null;
        if (consumeableAsset.explosion != 0)
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
                data.LastBleedingArgs = new DeathMessageArgs()
                {
                    DeadPlayerName = F.GetPlayerOriginalNames(instigatingPlayer).CharacterName,
                    DeadPlayerTeam = instigatingPlayer.GetTeam(),
                    DeathCause = EDeathCause.INFECTION,
                    ItemName = consumeableAsset.itemName,
                    ItemGuid = consumeableAsset.GUID,
                    Flags = EDeathFlags.ITEM
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
#if DEBUG
        using IDisposable profiler = ProfilingUtils.StartTracking();
#endif
        if (storage == null || !shouldAllow || Gamemode.Config.Barricades.TimeLimitedStorages == null || Gamemode.Config.Barricades.TimeLimitedStorages.Value.Length == 0 || UCWarfare.Config.MaxTimeInStorages <= 0) return;
        SteamPlayer player = PlayerTool.getSteamPlayer(instigator);
        BarricadeDrop storagedrop = BarricadeManager.FindBarricadeByRootTransform(storage.transform);
        if (player == null || storagedrop == null ||
            !Gamemode.Config.Barricades.TimeLimitedStorages.Value.Any(x => x.Guid == storagedrop.asset.GUID)) return;
        UCPlayer? ucplayer = UCPlayer.FromSteamPlayer(player);
        if (ucplayer == null) return;
        if (ucplayer.StorageCoroutine != null)
            player.player.StopCoroutine(ucplayer.StorageCoroutine);
        ucplayer.StorageCoroutine = player.player.StartCoroutine(WaitToCloseStorage(ucplayer));
    }
    private static IEnumerator<WaitForSeconds> WaitToCloseStorage(UCPlayer player)
    {
        yield return new WaitForSeconds(UCWarfare.Config.MaxTimeInStorages);
        player.Player.inventory.closeStorageAndNotifyClient();
        player.StorageCoroutine = null;
    }
    internal static void OnBarricadeDamaged(CSteamID instigatorSteamID, Transform barricadeTransform, ref ushort pendingTotalDamage, ref bool shouldAllow, EDamageOrigin damageOrigin)
    {
#if DEBUG
        using IDisposable profiler = ProfilingUtils.StartTracking();
#endif
        if (Data.Gamemode is TeamGamemode && TeamManager.IsInAnyMainOrAMCOrLobby(barricadeTransform.position))
        {
            shouldAllow = false;
        }
        else
        {
            if (Data.Gamemode != null && Data.Gamemode.UseWhitelist)
            {
                Data.Gamemode.Whitelister.OnBarricadeDamageRequested(instigatorSteamID, barricadeTransform, ref pendingTotalDamage, ref shouldAllow, damageOrigin);
                if (!shouldAllow)
                    return;
            }
            BarricadeDrop drop = BarricadeManager.FindBarricadeByRootTransform(barricadeTransform);
            if (drop == null) return;
            if (Gamemode.Config.Barricades.FOBRadioDamagedGUID.ValidReference(out Guid guid) && guid == drop.asset.GUID && instigatorSteamID != CSteamID.Nil)
            {
                shouldAllow = false;
            }

            if (Structures.StructureSaver.StructureExists(drop.instanceID, Structures.EStructType.BARRICADE, out Structures.Structure s) && s.transform == barricadeTransform)
            {
                shouldAllow = false;
            }
            else if (instigatorSteamID != CSteamID.Nil && instigatorSteamID != Provider.server)
            {
                Guid weapon;
                SteamPlayer? pl = PlayerTool.getSteamPlayer(instigatorSteamID);
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
                    if (pl.player.equipment.asset != null)
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
            if (shouldAllow && pendingTotalDamage > 0 && barricadeTransform.TryGetComponent(out BarricadeComponent c2))
            {
                c2.LastDamager = instigatorSteamID.m_SteamID;
                L.LogDebug(instigatorSteamID.m_SteamID + " damaged " + drop.asset.FriendlyName);
            }
        }
    }
    internal static void OnStructureDamaged(CSteamID instigatorSteamID, Transform structureTransform, ref ushort pendingTotalDamage, ref bool shouldAllow, EDamageOrigin damageOrigin)
    {
#if DEBUG
        using IDisposable profiler = ProfilingUtils.StartTracking();
#endif
        if (Data.Gamemode is TeamGamemode && TeamManager.IsInAnyMainOrAMCOrLobby(structureTransform.position))
        {
            shouldAllow = false;
        }
        else
        {
            if (Data.Gamemode != null && Data.Gamemode.UseWhitelist)
            {
                Data.Gamemode.Whitelister.OnStructureDamageRequested(instigatorSteamID, structureTransform, ref pendingTotalDamage, ref shouldAllow, damageOrigin);
                if (!shouldAllow)
                    return;
            }
            StructureDrop drop = StructureManager.FindStructureByRootTransform(structureTransform);
            if (drop == null) return;
            if (Structures.StructureSaver.StructureExists(drop.instanceID, Structures.EStructType.STRUCTURE, out Structures.Structure s) && s.transform == structureTransform)
            {
                shouldAllow = false;
            }
            else if (instigatorSteamID != CSteamID.Nil && instigatorSteamID != Provider.server)
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
    }
    internal static void OnEnterVehicle(EnterVehicle e)
    {
#if DEBUG
        using IDisposable profiler = ProfilingUtils.StartTracking();
#endif
        if (!e.Vehicle.transform.TryGetComponent(out VehicleComponent component))
        {
            component = e.Vehicle.transform.gameObject.AddComponent<VehicleComponent>();
            component.Initialize(e.Vehicle);
        }
        component.OnPlayerEnteredVehicle(e);

        if (Data.Is<IFlagRotation>(out _) && e.Player.Player.IsOnFlag(out Flag flag))
        {
            CaptureUIParameters p = CTFUI.RefreshStaticUI(e.Player.GetTeam(), flag, true);
            CTFUI.CaptureUI.Send(e.Player, ref p);
        }
    }
    static readonly Dictionary<ulong, long> lastSentMessages = new Dictionary<ulong, long>();
    internal static void RemoveDamageMessageTicks(ulong player)
    {
        lastSentMessages.Remove(player);
    }
    internal static void OnPlayerDamageRequested(ref DamagePlayerParameters parameters, ref bool shouldAllow)
    {
#if DEBUG
        using IDisposable profiler = ProfilingUtils.StartTracking();
#endif
        if (Data.Gamemode.State != EState.ACTIVE)
        {
            shouldAllow = false;
            return;
        }
        Vector3 pos = parameters.player.transform.position;
        if (TeamManager.IsInAnyMainOrLobby(parameters.player))
        {
            if (TeamManager.LobbyZone != null && TeamManager.LobbyZone.IsInside(pos))
            {
                shouldAllow = false;
                return;
            }
        }

        UCPlayer? ucplayer = UCPlayer.FromPlayer(parameters.player);
        if (ucplayer != null && ucplayer.OnDuty())
        {
            if (ucplayer.GodMode)
            {
                shouldAllow = false;
                return;
            }
        }
        if (parameters.cause < DeathTracker.MAIN_CAMP_OFFSET && Data.Gamemode is TeamGamemode gm && gm.EnableAMC && OffenseManager.IsValidSteam64ID(parameters.killer) && parameters.killer != parameters.player.channel.owner.playerID.steamID) // prevent killer from being null or suicidal
        {
            Player killer = PlayerTool.getPlayer(parameters.killer);
            if (killer != null)
            {
                ulong killerteam = killer.GetTeam();
                ulong deadteam = parameters.player.GetTeam();
                if ((deadteam == 1 && killerteam == 2 && TeamManager.Team1AMC.IsInside(pos)) ||
                    (deadteam == 2 && killerteam == 1 && TeamManager.Team2AMC.IsInside(pos)))
                {
                    // if the player has shot since they died
                    if (!killer.TryGetPlayerData(out UCPlayerData comp) || comp.LastGunShot != default)
                        goto next;
                    shouldAllow = false;
                    byte newdamage = (byte)Math.Min(byte.MaxValue, Mathf.RoundToInt(parameters.damage * parameters.times * UCWarfare.Config.AMCDamageMultiplier));
                    killer.life.askDamage(newdamage, parameters.direction * newdamage, (EDeathCause)((int)DeathTracker.MAIN_CAMP_OFFSET + (int)parameters.cause),
                    parameters.limb, parameters.player.channel.owner.playerID.steamID, out _, true, ERagdollEffect.NONE, false, true);
                    if (!lastSentMessages.TryGetValue(parameters.player.channel.owner.playerID.steamID.m_SteamID, out long lasttime) || new TimeSpan(DateTime.Now.Ticks - lasttime).TotalSeconds > 5)
                    {
                        killer.SendChat("amc_reverse_damage");
                        if (lasttime == default)
                            lastSentMessages.Add(parameters.player.channel.owner.playerID.steamID.m_SteamID, DateTime.Now.Ticks);
                        else
                            lastSentMessages[parameters.player.channel.owner.playerID.steamID.m_SteamID] = DateTime.Now.Ticks;
                    }
                }
            }
        }
        next:
        if (shouldAllow && Data.Is(out IRevives rev))
            rev.ReviveManager.OnPlayerDamagedRequested(ref parameters, ref shouldAllow);
        if (shouldAllow)
        {
            bool isBleeding;
            switch (parameters.bleedingModifier)
            {
                case DamagePlayerParameters.Bleeding.Always:
                    isBleeding = true;
                    break;
                case DamagePlayerParameters.Bleeding.Never:
                    isBleeding = false;
                    break;
                case DamagePlayerParameters.Bleeding.Heal:
                    isBleeding = false;
                    break;
                default:
                    byte amount = (byte)Mathf.Min(byte.MaxValue, Mathf.FloorToInt(parameters.damage * parameters.times));
                    isBleeding = amount < parameters.player.life.health && Provider.modeConfigData.Players.Can_Start_Bleeding && amount >= 20;
                    break;
            }
            if (isBleeding)
                DeathTracker.OnWillStartBleeding(ref parameters);
        }
    }
    internal static void OnPlayerMarkedPosOnMap(Player player, ref Vector3 position, ref string overrideText, ref bool isBeingPlaced, ref bool allowed)
    {
#if DEBUG
        using IDisposable profiler = ProfilingUtils.StartTracking();
#endif
        if (player == null) return;
        UCPlayer? ucplayer = UCPlayer.FromPlayer(player);
        if (ucplayer == null || ucplayer.Squad == null)
        {
            allowed = false;
            return;
        }
        if (!isBeingPlaced)
        {
            ClearPlayerMarkerForSquad(ucplayer);
            return;
        }
        if (!ucplayer.IsSquadLeader())
        {
            allowed = false;
            ucplayer.Message("range_notsquadleader");
            return;
        }
        overrideText = ucplayer.Squad.Name.ToUpper();
        //Vector3 effectposition = new Vector3(position.x, F.GetTerrainHeightAt2DPoint(position.x, position.z), position.z);
        //PlaceMarker(ucplayer, effectposition, false, false);
    }
    internal static void OnPlayerGestureRequested(Player player, EPlayerGesture gesture, ref bool allow)
    {
#if DEBUG
        using IDisposable profiler = ProfilingUtils.StartTracking();
#endif
        if (player == null) return;
        if (gesture == EPlayerGesture.POINT)
        {
            UCPlayer? ucplayer = UCPlayer.FromPlayer(player);
            if (ucplayer == null) return;
            if (!Physics.Raycast(new Ray(player.look.aim.transform.position, player.look.aim.transform.forward), out RaycastHit hit, 8192f, RayMasks.BLOCK_COLLISION)) return;
            PlaceMarker(ucplayer, hit.point, true, true);
        }
    }
    private static void PlaceMarker(UCPlayer ucplayer, Vector3 Point, bool sendNoSquadChat, bool placeMarkerOnMap)
    {
#if DEBUG
        using IDisposable profiler = ProfilingUtils.StartTracking();
#endif
        ThreadUtil.assertIsGameThread();
        if (placeMarkerOnMap)
            ucplayer.Player.quests.ReceiveSetMarkerRequest(true, Point);
        ushort markerid = ucplayer.GetMarkerID();
        ushort lastping = ucplayer.LastPingID == 0 ? markerid : ucplayer.LastPingID;
        if (ucplayer.Squad == null)
        {
            if (sendNoSquadChat)
                ucplayer.SendChat("marker_not_in_squad");
            if (markerid == 0) return;
            EffectManager.askEffectClearByID(lastping, ucplayer.Player.channel.owner.transportConnection);
            EffectManager.sendEffectReliable(markerid, ucplayer.Player.channel.owner.transportConnection, Point);
            ucplayer.LastPingID = markerid;
            return;
        }
        if (markerid == 0) return;
        for (int i = 0; i < ucplayer.Squad.Members.Count; i++)
        {
            EffectManager.askEffectClearByID(lastping, ucplayer.Squad.Members[i].Player.channel.owner.transportConnection);
            EffectManager.sendEffectReliable(markerid, ucplayer.Squad.Members[i].Player.channel.owner.transportConnection, Point);
            ucplayer.LastPingID = markerid;
        }
    }
    public static void ClearPlayerMarkerForSquad(UCPlayer ucplayer) => ClearPlayerMarkerForSquad(ucplayer, ucplayer.LastPingID == 0 ? ucplayer.GetMarkerID() : ucplayer.LastPingID);
    public static void ClearPlayerMarkerForSquad(UCPlayer ucplayer, ushort markerid)
    {
#if DEBUG
        using IDisposable profiler = ProfilingUtils.StartTracking();
#endif
        ThreadUtil.assertIsGameThread();
        if (markerid == 0) return;
        if (ucplayer.Squad == null)
        {
            EffectManager.askEffectClearByID(markerid, ucplayer.Player.channel.owner.transportConnection);
            ucplayer.LastPingID = 0;
            return;
        }
        for (int i = 0; i < ucplayer.Squad.Members.Count; i++)
        {
            EffectManager.askEffectClearByID(markerid, ucplayer.Squad.Members[i].Player.channel.owner.transportConnection);
            ucplayer.LastPingID = 0;
        }
    }
    internal static void OnTryStoreItem(Player player, byte page, ItemJar jar, ref bool allow)
    {
#if DEBUG
        using IDisposable profiler = ProfilingUtils.StartTracking();
#endif
        if (!player.inventory.isStoring || player == null || jar == null || jar.item == null || allow == false) return;
        UCPlayer? ucplayer = UCPlayer.FromPlayer(player);
        if (ucplayer != null && ucplayer.OnDuty())
            return;
        if (Assets.find(EAssetType.ITEM, jar.item.id) is not ItemAsset asset) return;
        if (!Whitelister.IsWhitelisted(asset.GUID, out _))
        {
            allow = false;
            player.SendChat("cant_store_this_item", asset.itemName);
        }
    }
    internal static void StructureMovedInWorkzone(CSteamID instigator, byte x, byte y, uint instanceID, ref Vector3 point, ref byte angle_x, ref byte angle_y, ref byte angle_z, ref bool shouldAllow)
    {
#if DEBUG
        using IDisposable profiler = ProfilingUtils.StartTracking();
#endif
        if (Structures.StructureSaver.StructureExists(instanceID, Structures.EStructType.STRUCTURE, out Structures.Structure found))
        {
            found.transform = new SerializableTransform(new SerializableVector3(point), new SerializableVector3(angle_x * 2f, angle_y * 2f, angle_z * 2f));
            Structures.StructureSaver.SaveSingleton();
            if (VehicleSpawner.IsRegistered(instanceID, out Vehicles.VehicleSpawn spawn, Structures.EStructType.STRUCTURE))
            {
                IEnumerable<VehicleSign> linked = VehicleSigns.GetLinkedSigns(spawn);
                int i = 0;
                foreach (VehicleSign sign in linked)
                {
                    i++;
                    sign.bay_transform = found.transform;
                }
                if (i > 0) VehicleSigns.SaveSingleton();
            }
        }
    }
    internal static void BarricadeMovedInWorkzone(CSteamID instigator, byte x, byte y, ushort plant, uint instanceID, ref Vector3 point, ref byte angle_x, ref byte angle_y, ref byte angle_z, ref bool shouldAllow)
    {
#if DEBUG
        using IDisposable profiler = ProfilingUtils.StartTracking();
#endif
        if (Structures.StructureSaver.StructureExists(instanceID, Structures.EStructType.BARRICADE, out Structures.Structure found))
        {
            found.transform = new SerializableTransform(new SerializableVector3(point), new SerializableVector3(angle_x * 2f, angle_y * 2f, angle_z * 2f));
            Structures.StructureSaver.SaveSingleton();
            if (VehicleSpawner.IsRegistered(instanceID, out Vehicles.VehicleSpawn spawn, Structures.EStructType.BARRICADE))
            {
                IEnumerable<VehicleSign> linked = VehicleSigns.GetLinkedSigns(spawn);
                int i = 0;
                foreach (VehicleSign sign in linked)
                {
                    i++;
                    sign.bay_transform = found.transform;
                }
                if (i > 0) VehicleSigns.SaveSingleton();
            }
        }
        UCBarricadeManager.GetBarricadeFromInstID(instanceID, out BarricadeDrop? drop);
        if (drop != default)
        {
            if (drop.model.TryGetComponent(out InteractableSign sign))
            {
                if (RequestSigns.SignExists(sign, out RequestSign rsign))
                {
                    rsign.transform = new SerializableTransform(new SerializableVector3(point), new SerializableVector3(angle_x * 2f, angle_y * 2f, angle_z * 2f));
                    RequestSigns.SaveSingleton();
                }
                else if (VehicleSigns.SignExists(sign, out VehicleSign vbsign))
                {
                    vbsign.sign_transform = new SerializableTransform(new SerializableVector3(point), new SerializableVector3(angle_x * 2f, angle_y * 2f, angle_z * 2f));
                    VehicleSigns.SaveSingleton();
                }
            }
        }
    }
    internal static void OnPlayerLeavesVehicle(ExitVehicle e)
    {
#if DEBUG
        using IDisposable profiler = ProfilingUtils.StartTracking();
#endif
        if (e.Vehicle.transform.TryGetComponent(out VehicleComponent component))
        {
            component.OnPlayerExitedVehicle(e);
        }
        ActionLogger.Add(EActionLogType.LEAVE_VEHICLE_SEAT, $"{e.Vehicle.asset.vehicleName} / {e.Vehicle.asset.id} / {e.Vehicle.asset.GUID:N}, Owner: {e.Vehicle.lockedOwner.m_SteamID}, " +
                                                         $"ID: ({e.Vehicle.instanceID})", e.Steam64);
    }
    internal static void OnVehicleSwapSeat(VehicleSwapSeat e)
    {
#if DEBUG
        using IDisposable profiler = ProfilingUtils.StartTracking();
#endif
        InteractableVehicle vehicle = e.Vehicle;
        if (vehicle.transform.TryGetComponent(out VehicleComponent component))
        {
            component.OnPlayerSwapSeatRequested(e);
        }
        ActionLogger.Add(EActionLogType.ENTER_VEHICLE_SEAT, $"{vehicle.asset.vehicleName} / {vehicle.asset.id} / {vehicle.asset.GUID:N}, Owner: {vehicle.lockedOwner.m_SteamID}, " +
                                                            $"ID: ({vehicle.instanceID}) Seat move: {e.OldSeat.ToString(Data.Locale)} >> " +
                                                            $"{e.NewSeat.ToString(Data.Locale)}", e.Player.Steam64);
    }
    internal static void BatteryStolen(SteamPlayer theif, ref bool allow)
    {
        if (!UCWarfare.Config.AllowBatteryStealing)
        {
#if DEBUG
            using IDisposable profiler = ProfilingUtils.StartTracking();
#endif
            allow = false;
            theif.SendChat("cant_steal_batteries");
        }
    }
    internal static void OnCalculateSpawnDuringRevive(PlayerLife sender, bool wantsToSpawnAtHome, ref Vector3 position, ref float yaw)
    {
#if DEBUG
        using IDisposable profiler = ProfilingUtils.StartTracking();
#endif
        ulong team = sender.player.GetTeam();
        position = team.GetBaseSpawnFromTeam();
        yaw = team.GetBaseAngle();
    }
    internal static void OnCalculateSpawnDuringJoin(SteamPlayerID playerID, ref Vector3 point, ref float yaw, ref EPlayerStance initialStance, ref bool needsNewSpawnpoint)
    {
#if DEBUG
        using IDisposable profiler = ProfilingUtils.StartTracking();
#endif
        // leave the player where they logged off if they logged off in the same game.
        if (PlayerSave.TryReadSaveFile(playerID.steamID.m_SteamID, out PlayerSave save) && 
            Data.Gamemode is not null && Data.Gamemode.GameID == save.LastGame && !save.ShouldRespawnOnJoin)
            return;
        
        point = TeamManager.LobbySpawn;
        yaw = TeamManager.LobbySpawnAngle;
        initialStance = EPlayerStance.STAND;
        needsNewSpawnpoint = false;
    }
    internal static void OnPlayerDisconnected(PlayerEvent e)
    {
#if DEBUG
        using IDisposable profiler = ProfilingUtils.StartTracking();
#endif
        ulong s64 = e.Steam64;
        droppeditems.Remove(s64);
        TeamManager.PlayerBaseStatus.Remove(s64);
        RemoveDamageMessageTicks(s64);
        Tips.OnPlayerDisconnected(s64);
        UCPlayer ucplayer = e.Player;
        string kit = string.Empty;
        FPlayerName names = F.GetPlayerOriginalNames(ucplayer.Player.channel.owner);
        try
        {
            Quests.DailyQuests.DeregisterDailyTrackers(ucplayer);
            Quests.QuestManager.DeregisterOwnedTrackers(ucplayer);
            kit = ucplayer.KitName;

            EAdminType type = ucplayer.PermissionLevel;
            if ((type & EAdminType.ADMIN_ON_DUTY) == EAdminType.ADMIN_ON_DUTY)
                Commands.DutyCommand.AdminOnToOff(ucplayer, names);
            else if ((type & EAdminType.TRIAL_ADMIN_ON_DUTY) == EAdminType.TRIAL_ADMIN_ON_DUTY)
                Commands.DutyCommand.InternOnToOff(ucplayer, names);

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
            Data.OriginalNames.Remove(s64);
            ulong id = s64;
            Chat.Broadcast("player_disconnected", names.CharacterName);
            if (c != null)
            {
                ActionLogger.Add(EActionLogType.DISCONNECT, "PLAYED FOR " + ((uint)Mathf.RoundToInt(Time.realtimeSinceStartup - c.JoinTime)).GetTimeFromSeconds(0).ToUpper(), ucplayer.Steam64);
                UnityEngine.Object.Destroy(c);
                Data.PlaytimeComponents.Remove(ucplayer.Steam64);
            }
            else
                ActionLogger.Add(EActionLogType.DISCONNECT, $"Players online: {Provider.clients.Count - 1}", ucplayer.Steam64);
            PlayerManager.NetCalls.SendPlayerLeft.NetInvoke(ucplayer.Steam64);
        }
        catch (Exception ex)
        {
            L.LogError("Error in the main OnPlayerDisconnected:");
            L.LogError(ex);
        }
        try
        {
            if (RequestSigns.SignExists(kit, out RequestSign sign))
                sign.InvokeUpdate();
        }
        catch (Exception ex)
        {
            L.LogError("Failed to update kit sign for other players after leaving:");
            L.LogError(ex);
        }
    }
    internal static void LangCommand_OnPlayerChangedLanguage(UCPlayer player, LanguageAliasSet oldSet, LanguageAliasSet newSet)
        => UCWarfare.I.UpdateLangs(player);
    internal static void OnPrePlayerConnect(ValidateAuthTicketResponse_t ticket, ref bool isValid, ref string explanation)
    {
#if DEBUG
        using IDisposable profiler = ProfilingUtils.StartTracking();
#endif
        SteamPending player = Provider.pending.FirstOrDefault(x => x.playerID.steamID.m_SteamID == ticket.m_SteamID.m_SteamID);
        if (player == null) return;
        try
        {
            ActionLogger.Add(EActionLogType.TRY_CONNECT, $"Steam Name: {player.playerID.playerName}, Public Name: {player.playerID.characterName}, Private Name: {player.playerID.nickName}, Character ID: {player.playerID.characterID}.", ticket.m_SteamID.m_SteamID);
            bool kick = false;
            string? cn = null;
            string? nn = null;
            if (player.playerID.characterName.Length == 0)
            {
                player.playerID.characterName = player.playerID.steamID.m_SteamID.ToString(Data.Locale);
                if (player.playerID.nickName.Length == 0)
                {
                    player.playerID.nickName = player.playerID.steamID.m_SteamID.ToString(Data.Locale);
                }
                else
                {
                    kick = F.FilterName(player.playerID.characterName, out cn);
                    kick |= F.FilterName(player.playerID.nickName, out nn);
                }
            }
            else if (player.playerID.nickName.Length == 0)
            {
                player.playerID.nickName = player.playerID.steamID.m_SteamID.ToString(Data.Locale);
                if (player.playerID.characterName.Length == 0)
                {
                    player.playerID.characterName = player.playerID.steamID.m_SteamID.ToString(Data.Locale);
                }
                else
                {
                    kick = F.FilterName(player.playerID.characterName, out cn);
                    kick |= F.FilterName(player.playerID.nickName, out nn);
                }
            }
            else
            {
                kick = F.FilterName(player.playerID.characterName, out cn);
                kick |= F.FilterName(player.playerID.nickName, out nn);
            }
            if (kick)
            {
                isValid = false;
                explanation = Localization.Translate("kick_autokick_namefilter", player.playerID.steamID.m_SteamID);
                return;
            }
            else
            {
                player.playerID.characterName = cn;
                player.playerID.nickName = cn;
            }

            player.playerID.characterName = Regex.Replace(player.playerID.characterName, "<.*>", string.Empty);
            player.playerID.nickName      = Regex.Replace(player.playerID.nickName,      "<.*>", string.Empty);

            if (player.playerID.characterName.Length < 3 && player.playerID.nickName.Length < 3)
            {
                isValid = false;
                explanation = Localization.Translate("kick_autokick_namefilter", player.playerID.steamID.m_SteamID);
                return;
            }
            else if (player.playerID.characterName.Length < 3)
            {
                player.playerID.characterName = player.playerID.nickName;
            }
            else if (player.playerID.nickName.Length < 3)
            {
                player.playerID.nickName = player.playerID.characterName;
            }

            FPlayerName names = new FPlayerName(player.playerID);

            if (Data.OriginalNames.ContainsKey(player.playerID.steamID.m_SteamID))
                Data.OriginalNames[player.playerID.steamID.m_SteamID] = names;
            else
                Data.OriginalNames.Add(player.playerID.steamID.m_SteamID, names);

            L.Log("PN: \"" + player.playerID.playerName + "\", CN: \"" + player.playerID.characterName + "\", NN: \"" + player.playerID.nickName + "\" (" + player.playerID.steamID.m_SteamID.ToString(Data.Locale) + ") trying to connect.", ConsoleColor.Cyan);
        }
        catch (Exception ex)
        {
            L.LogError($"Error accepting {player.playerID.playerName} in OnPrePlayerConnect:");
            L.LogError(ex);
            isValid = false;
            explanation = "Uncreated Network was unable to authenticate your connection, try again later or contact a Director if this keeps happening.";
        }
    }
    internal static void OnStructureDestroyed(SDG.Unturned.StructureData data, StructureDrop drop, uint instanceID)
    {
#if DEBUG
        using IDisposable profiler = ProfilingUtils.StartTracking();
#endif
        if (Data.Is(out IVehicles v))
            v.VehicleSpawner.OnStructureDestroyed(data, drop, instanceID);
        if (drop.model.TryGetComponent(out BarricadeComponent c))
        {
            SteamPlayer damager = PlayerTool.getSteamPlayer(c.LastDamager);
            ActionLogger.Add(EActionLogType.DESTROY_STRUCTURE, $"{drop.asset.itemName} / {drop.asset.id} / {drop.asset.GUID:N} - Owner: {c.Owner}, Team: {TeamManager.TranslateName(data.group.GetTeam(), 0)}, ID: {drop.instanceID}", c.LastDamager);
            if (Data.Reporter is not null && damager != null && data.group.GetTeam() == damager.GetTeam())
            {
                Data.Reporter.OnDestroyedStructure(c.LastDamager, instanceID);
            }
        }
    }
    internal static void OnBarricadeDestroyed(BarricadeDestroyed e)
    {
#if DEBUG
        using IDisposable profiler = ProfilingUtils.StartTracking();
#endif
        if (Data.Is<IFOBs>(out _))
        {
            RepairManager.OnBarricadeDestroyed(e.ServersideData, e.Barricade, e.InstanceID, e.VehicleRegionIndex);
        }

        if (e.Transform.TryGetComponent(out BuildableComponent buildable))
            buildable.Destroy();
        if (e.Transform.TryGetComponent(out RepairableComponent repairable))
            repairable.Destroy(e);

        IconRenderer[] iconrenderers = e.Barricade.model.GetComponents<IconRenderer>();
        foreach (IconRenderer iconRenderer in iconrenderers)
            IconManager.DeleteIcon(iconRenderer);

        if (Data.Is<ISquads>(out _))
            RallyManager.OnBarricadeDestroyed(e.ServersideData, e.Barricade, e.InstanceID, e.VehicleRegionIndex);
        if (Data.Is(out IVehicles v))
        {
            v.VehicleSpawner.OnBarricadeDestroyed(e.ServersideData, e.Barricade, e.InstanceID, e.VehicleRegionIndex);
            v.VehicleSigns.OnBarricadeDestroyed(e.ServersideData, e.Barricade, e.InstanceID, e.VehicleRegionIndex);
        }
        if (e.Transform.TryGetComponent(out BarricadeComponent c))
        {
            SteamPlayer damager = PlayerTool.getSteamPlayer(c.LastDamager);
            ActionLogger.Add(EActionLogType.DESTROY_BARRICADE, $"{e.Barricade.asset.itemName} / {e.Barricade.asset.id} / {e.Barricade.asset.GUID:N} - Owner: {c.Owner}, Team: {TeamManager.TranslateName(e.ServersideData.group.GetTeam(), 0)}, ID: {e.Barricade.instanceID}", c.LastDamager);
            if (Data.Reporter is not null && damager != null && e.ServersideData.group.GetTeam() == damager.GetTeam())
            {
                Data.Reporter.OnDestroyedStructure(c.LastDamager, e.InstanceID);
            }
        }
    }
    internal static void OnPostHealedPlayer(Player instigator, Player target)
    {
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
                    data.LastBleedingArgs = new DeathMessageArgs()
                    {
                        DeadPlayerName = F.GetPlayerOriginalNames(target).CharacterName,
                        DeadPlayerTeam = target.GetTeam(),
                        DeathCause = EDeathCause.INFECTION,
                        ItemName = asset.itemName,
                        ItemGuid = asset.GUID,
                        Flags = EDeathFlags.ITEM | EDeathFlags.KILLER,
                        KillerName = F.GetPlayerOriginalNames(instigator).CharacterName,
                        KillerTeam = instigator.GetTeam()
                    };
                    data.LastBleedingArgs.isTeamkill = data.LastBleedingArgs.DeadPlayerTeam == data.LastBleedingArgs.KillerTeam;
                    data.LastBleedingEvent = new PlayerDied(UCPlayer.FromPlayer(target)!)
                    {
                        Cause = EDeathCause.INFECTION,
                        DeadTeam = data.LastBleedingArgs.DeadPlayerTeam,
                        Instigator = instigator.channel.owner.playerID.steamID,
                        Killer = UCPlayer.FromPlayer(instigator),
                        KillerTeam = data.LastBleedingArgs.KillerTeam,
                        WasTeamkill = data.LastBleedingArgs.isTeamkill
                    };
                }
            }
        }
        if (Data.Is(out IRevives r))
        {
#if DEBUG
            using IDisposable profiler = ProfilingUtils.StartTracking();
#endif
            r.ReviveManager.ClearInjuredMarker(instigator.channel.owner.playerID.steamID.m_SteamID, instigator.GetTeam());
            r.ReviveManager.OnPlayerHealed(instigator, target);
        }
    }
    internal static void OnPluginKeyPressed(Player player, uint simulation, byte key, bool state)
    {
        if (!state || key != 2 || player == null) return;

#if DEBUG
        using IDisposable profiler = ProfilingUtils.StartTracking();
#endif
        if (Data.Is(out IRevives r))
        {
            r.ReviveManager.GiveUp(player);
        }

        InteractableVehicle? vehicle = player.movement.getVehicle();
        if (vehicle != null && player.movement.getSeat() == 0 && (vehicle.asset.engine == EEngine.HELICOPTER || vehicle.asset.engine == EEngine.PLANE) && vehicle.transform.TryGetComponent(out VehicleComponent component))
        {
            component.TrySpawnCountermeasures();
        }
    }
}
#pragma warning restore IDE0060 // Remove unused parameter
