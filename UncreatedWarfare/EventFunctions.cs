using Rocket.Unturned.Player;
using SDG.Unturned;
using Steamworks;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Uncreated.Players;
using Uncreated.Warfare.Components;
using Uncreated.Warfare.FOBs;
using Uncreated.Warfare.Gamemodes;
using Uncreated.Warfare.Gamemodes.Flags;
using Uncreated.Warfare.Gamemodes.Flags.TeamCTF;
using Uncreated.Warfare.Gamemodes.Interfaces;
using Uncreated.Warfare.Kits;
using Uncreated.Warfare.Networking;
using Uncreated.Warfare.Point;
using Uncreated.Warfare.Squads;
using Uncreated.Warfare.Teams;
using Uncreated.Warfare.Tickets;
using Uncreated.Warfare.Vehicles;
using UnityEngine;
using Flag = Uncreated.Warfare.Gamemodes.Flags.Flag;

#pragma warning disable IDE0060 // Remove unused parameter
namespace Uncreated.Warfare
{
    public static class EventFunctions
    {
        public delegate void GroupChanged(SteamPlayer player, ulong oldGroup, ulong newGroup);
        public static event GroupChanged OnGroupChanged;
        internal static void OnGroupChangedInvoke(SteamPlayer player, ulong oldGroup, ulong newGroup) => OnGroupChanged?.Invoke(player, oldGroup, newGroup);
        internal static void GroupChangedAction(SteamPlayer player, ulong oldGroup, ulong newGroup)
        {
#if DEBUG
            using IDisposable profiler = ProfilingUtils.StartTracking();
#endif
            ulong oldteam = oldGroup.GetTeam();
            ulong newteam = newGroup.GetTeam();
            UCPlayer? ucplayer = UCPlayer.FromSteamPlayer(player);
            if (ucplayer != null)
            {
                PlayerManager.ApplyTo(ucplayer);
                Data.Gamemode?.OnGroupChanged(ucplayer, oldGroup, newGroup, oldteam, newteam);

                if (newteam == 1 || newteam == 2)
                    FOBManager.SendFOBList(ucplayer);
                Points.OnGroupChanged(ucplayer, oldGroup, newGroup);
                IconManager.DrawNewMarkers(ucplayer, true);
            }
            SquadManager.OnGroupChanged(player, oldGroup, newGroup);
            TicketManager.OnGroupChanged(player, oldGroup, newGroup);

            RequestSigns.InvokeLangUpdateForAllSigns(player);

            Invocations.Shared.TeamChanged.NetInvoke(player.playerID.steamID.m_SteamID, F.GetTeamByte(newGroup));

        }
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
        internal static void OnCommandExecuted(Rocket.API.IRocketPlayer player, Rocket.API.IRocketCommand command, ref bool cancel)
        {
            if (cancel) return;
            CommandWaitTask.OnCommandExecuted(player, command);
        }
        internal static void OnStructurePlaced(StructureRegion region, StructureDrop drop)
        {
            SDG.Unturned.StructureData data = drop.GetServersideData();
            ActionLog.Add(EActionLogType.PLACE_STRUCTURE, $"{drop.asset.itemName} / {drop.asset.id} / {drop.asset.GUID:N} - Team: {TeamManager.TranslateName(data.group.GetTeam(), 0)}, ID: {drop.instanceID}", data.owner);
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

            ActionLog.Add(EActionLogType.PLACE_BARRICADE, $"{drop.asset.itemName} / {drop.asset.id} / {drop.asset.GUID:N} - Team: {TeamManager.TranslateName(data.group.GetTeam(), 0)}, ID: {drop.instanceID}", data.owner);

            RallyManager.OnBarricadePlaced(drop, region);

            RepairManager.OnBarricadePlaced(drop, region);

            if (Gamemode.Config.Barricades.FOBRadioGUIDs == null) return;

            bool isFOBRadio = Gamemode.Config.Barricades.FOBRadioGUIDs.Any(g => g == data.barricade.asset.GUID);

            if (FOBManager.AllFOBs == null) return;

            // FOB radio
            if (isFOBRadio)
            {
                if (!FOBManager.AllFOBs.Exists(f => f.Position == drop.model.position))
                    FOBManager.RegisterNewFOB(drop);
            }

            // ammo bag
            if (Gamemode.Config.Barricades.AmmoBagGUID == data.barricade.asset.GUID)
                drop.model.gameObject.AddComponent<AmmoBagComponent>().Initialize(data, drop);

            if (FOBManager.config.data.Buildables == null) return;
            BuildableData buildable = FOBManager.config.data.Buildables.Find(b => b.foundationID == drop.asset.GUID);
            if (buildable != null)
            {
                drop.model.gameObject.AddComponent<BuildableComponent>().Initialize(drop, buildable);
            }

            IconManager.OnBarricadePlaced(drop, isFOBRadio);

            BuildableData? repairable = isFOBRadio ? null : FOBManager.config.data.Buildables.Find(b => b.structureID == drop.asset.GUID || (b.type == EBuildableType.EMPLACEMENT && b.emplacementData != null && b.emplacementData.baseID == drop.asset.GUID));
            if (repairable != null || isFOBRadio)
            {
                drop.model.gameObject.AddComponent<RepairableComponent>();
            }
        }
        internal static void ThrowableSpawned(UseableThrowable useable, GameObject throwable)
        {
#if DEBUG
            using IDisposable profiler = ProfilingUtils.StartTracking();
#endif
            try
            {
                ThrowableOwner t = throwable.AddComponent<ThrowableOwner>();
                PlaytimeComponent? c = useable.player.GetPlaytimeComponent(out bool success);
                if (c != null)
                {
                    t.Set(useable, throwable, c);
                    c.thrown.Add(t);
                }
                L.LogDebug(useable.player.name + " spawned a throwable: " + (useable.equippedThrowableAsset != null ?
                    useable.equippedThrowableAsset.itemName : useable.name), ConsoleColor.DarkGray);
            }
            catch (Exception ex)
            {
                L.LogError("Exception in ThrowableSpawned:");
                L.LogError(ex);
            }
        }
        internal static void ProjectileSpawned(UseableGun gun, GameObject projectile)
        {
#if DEBUG
            using IDisposable profiler = ProfilingUtils.StartTracking();
#endif
            SDG.Unturned.Rocket[] rockets = projectile.GetComponentsInChildren<SDG.Unturned.Rocket>(true);
            foreach (SDG.Unturned.Rocket rocket in rockets)
            {
                rocket.killer = gun.player.channel.owner.playerID.steamID;
            }

            if (VehicleBay.Config.TOWMissileWeapons.Contains(gun.equippedGunAsset.GUID))
                projectile.AddComponent<GuidedMissileComponent>().Initialize(projectile, gun.player, 90, 0.33f, 800);
            else if (VehicleBay.Config.GroundAAWeapons.Contains(gun.equippedGunAsset.GUID))
                projectile.AddComponent<HeatSeakingMissileComponent>().Initialize(projectile, gun.player, 150, 5f, 1000, 4, 0.33f);
            else if (VehicleBay.Config.AirAAWeapons.Contains(gun.equippedGunAsset.GUID))
                projectile.AddComponent<HeatSeakingMissileComponent>().Initialize(projectile, gun.player, 150, 5f, 1000, 10, 0f);


            Patches.DeathsPatches.lastProjected = projectile;
            if (gun.player.TryGetPlaytimeComponent(out PlaytimeComponent c))
            {
                c.lastProjected = gun.equippedGunAsset.GUID;
            }
        }
        internal static void BulletSpawned(UseableGun gun, BulletInfo bullet)
        {
#if DEBUG
            using IDisposable profiler = ProfilingUtils.StartTracking();
#endif
            PlaytimeComponent? c = gun.player.GetPlaytimeComponent(out bool success);
            if (c != null)
            {
                c.lastShot = gun.equippedGunAsset.GUID;
            }
        }
        internal static void ReloadCommand_onTranslationsReloaded()
        {
#if DEBUG
            using IDisposable profiler = ProfilingUtils.StartTracking();
#endif
            foreach (SteamPlayer player in Provider.clients)
                UCWarfare.I.UpdateLangs(player);
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
                if (hit != null && hit.TryGetComponent<InteractableVehicle>(out _))
                {
                    if (!UCWarfare.Config.AdminLoggerSettings.AllowedBarricadesOnVehicles.Contains(asset.id))
                    {
                        if (player != null && player.OffDuty())
                        {
                            shouldAllow = false;
                            player.SendChat("no_placement_on_vehicle", asset.itemName, asset.itemName.An());
                            return;
                        }
                    }
                }
                if (hit != null)
                    RallyManager.OnBarricadePlaceRequested(barricade, asset, hit, ref point, ref angle_x, ref angle_y, ref angle_z, ref owner, ref group, ref shouldAllow);
                if (!shouldAllow) return;

                if (Gamemode.Config.Barricades.AmmoBagGUID == barricade.asset.GUID)
                {
                    if (player != null && player.OffDuty() && player.KitClass != EClass.RIFLEMAN)
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
                if (player != null)
                {
                    if (!player.OnDuty() && TeamManager.IsInAnyMainOrAMCOrLobby(point))
                    {
                        shouldAllow = false;
                        player.Message("whitelist_noplace");
                        return;
                    }

                if (Gamemode.Config.Barricades.FOBRadioGUIDs.Any(g => g == barricade.asset.GUID))
                {
                    shouldAllow = BuildableComponent.TryPlaceRadio(barricade, player, point);
                    return;
                }

                    BuildableData buildable = FOBManager.config.data.Buildables.Find(b => b.foundationID == barricade.asset.GUID);

                    if (buildable != null)
                    {
                        shouldAllow = BuildableComponent.TryPlaceBuildable(barricade, buildable, player, point);
                        return;
                    }
                }
                else
                {
                    L.LogError("Error in OnBarricadeTryPlaced: Player is null.");
                    shouldAllow = false;
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

                if (damageOrigin == EDamageOrigin.Useable_Gun && (vehicle.asset.engine == EEngine.HELICOPTER || vehicle.asset.engine == EEngine.PLANE))
                {
                    if (pendingTotalDamage > vehicle.health && pendingTotalDamage > 200)
                    {
                        canRepair = false;
                    }
                }

                if (!vehicle.TryGetComponent(out VehicleComponent c))
                {
                    c = vehicle.gameObject.AddComponent<VehicleComponent>();
                    c.Initialize(vehicle);
                }

                if (instigatorSteamID != CSteamID.Nil)
                {
                    c.item = Guid.Empty;
                    if (damageOrigin == EDamageOrigin.Grenade_Explosion)
                    {
                        if (instigatorSteamID.TryGetPlaytimeComponent(out PlaytimeComponent c2))
                        {
                            ThrowableOwner a = c2.thrown.FirstOrDefault(x =>
                                Assets.find(x.ThrowableID) is ItemThrowableAsset asset && asset.isExplosive);
                            if (a != null)
                                c.item = a.ThrowableID;
                        }
                    }
                    else if (damageOrigin == EDamageOrigin.Rocket_Explosion)
                    {
                        if (instigatorSteamID.TryGetPlaytimeComponent(out PlaytimeComponent c2))
                        {
                            c.item = c2.lastProjected;
                        }
                    }
                    else if (damageOrigin == EDamageOrigin.Vehicle_Explosion)
                    {
                        if (instigatorSteamID.TryGetPlaytimeComponent(out PlaytimeComponent c2))
                        {
                            c.item = c2.lastExplodedVehicle;
                        }
                    }
                    else if (damageOrigin == EDamageOrigin.Useable_Gun || damageOrigin == EDamageOrigin.Bullet_Explosion)
                    {
                        if (instigatorSteamID.TryGetPlaytimeComponent(out PlaytimeComponent c2))
                        {
                            c.item = c2.lastShot;
                        }
                    }
                    else if (damageOrigin == EDamageOrigin.Trap_Explosion)
                    {
                        if (instigatorSteamID.TryGetPlaytimeComponent(out PlaytimeComponent c2))
                        {
                            c.item = c2.LastLandmineExploded.barricadeGUID;
                        }
                    }

                    if (!c.DamageTable.TryGetValue(instigatorSteamID.m_SteamID, out var pair))
                        c.DamageTable.Add(instigatorSteamID.m_SteamID, new KeyValuePair<ushort, DateTime>(pendingTotalDamage, DateTime.Now));
                    else
                        c.DamageTable[instigatorSteamID.m_SteamID] = new KeyValuePair<ushort, DateTime>((ushort)(pair.Key + pendingTotalDamage), DateTime.Now);

                    UCPlayer? gunner = UCPlayer.FromCSteamID(instigatorSteamID);
                    if (gunner != null)
                    {
                        InteractableVehicle attackerVehicle = gunner.Player.movement.getVehicle();
                        if (attackerVehicle != null)
                        {
                            c.Quota += pendingTotalDamage * 0.015F;
                        }
                    }
                    c.lastDamageOrigin = damageOrigin;
                    c.lastDamager = instigatorSteamID.m_SteamID;
                }
            }
        }

        internal static void OnPostPlayerConnected(UnturnedPlayer player)
        {
#if DEBUG
            using IDisposable profiler = ProfilingUtils.StartTracking();
#endif
            try
            {
                PlayerManager.InvokePlayerConnected(player); // must always be first
            }
            catch (Exception ex)
            {
                L.LogError("Error in the main OnPostPlayerConnected loading player into OnlinePlayers:");
                L.LogError(ex);
                Provider.kick(player.CSteamID, "There was a fatal error connecting you to the server.");
            }
            try
            {
                // reset the player to spawn if they have joined in a different game as they last played in.
                UCPlayer? ucplayer = UCPlayer.FromUnturnedPlayer(player);

                bool g = Data.Is(out ITeams t);
                bool shouldRespawn = false;
                bool isNewPlayer = !PlayerManager.HasSave(player.CSteamID.m_SteamID, out PlayerSave save);
                if (!isNewPlayer)
                {
                    if (save.LastGame != Data.Gamemode.GameID || save.ShouldRespawnOnJoin)
                    {
                        shouldRespawn = true;

                        save.ShouldRespawnOnJoin = false;
                    }
                }


                save.LastGame = Data.Gamemode.GameID;

                if (player.Player.life.isDead)
                    player.Player.life.ReceiveRespawnRequest(false);
                else if (shouldRespawn)
                {
                    player.Player.life.sendRevive();
                    player.Player.teleportToLocation(F.GetBaseSpawn(player.Player, out ulong team), team.GetBaseAngle());
                }

                if (ucplayer != null)
                    PlayerManager.ApplyTo(ucplayer);

                FPlayerName names = F.GetPlayerOriginalNames(player);
                if (Data.PlaytimeComponents.ContainsKey(player.Player.channel.owner.playerID.steamID.m_SteamID))
                {
                    UnityEngine.Object.DestroyImmediate(Data.PlaytimeComponents[player.Player.channel.owner.playerID.steamID.m_SteamID]);
                    Data.PlaytimeComponents.Remove(player.Player.channel.owner.playerID.steamID.m_SteamID);
                }
                PlaytimeComponent pt = player.Player.transform.gameObject.AddComponent<PlaytimeComponent>();
                pt.StartTracking(player.Player);
                Data.PlaytimeComponents.Add(player.Player.channel.owner.playerID.steamID.m_SteamID, pt);
                Task.Run(async () =>
                {
                    bool FIRST_TIME = !await Data.DatabaseManager.HasPlayerJoined(player.Player.channel.owner.playerID.steamID.m_SteamID);
                    Task t1 = Data.DatabaseManager.CheckUpdateUsernames(names);
                    Task<int> t2 = Data.DatabaseManager.GetXP(player.Player.channel.owner.playerID.steamID.m_SteamID, player.GetTeam());
                    Task<int> t3 = Data.DatabaseManager.GetCredits(player.Player.channel.owner.playerID.steamID.m_SteamID, player.GetTeam());
                    Task<List<Kit>> t4 = Data.DatabaseManager.GetAccessibleKits(player.Player.channel.owner.playerID.steamID.m_SteamID);
                    await UCWarfare.ToUpdate();
                    if (Data.Gamemode is ITeams)
                    {
                        ulong team = player.GetTeam();
                        ToastMessage.QueueMessage(player, new ToastMessage(Translation.Translate(FIRST_TIME ? "welcome_message_first_time" : "welcome_message", player,
                            UCWarfare.GetColorHex("uncreated"), names.CharacterName, TeamManager.GetTeamHexColor(team)), EToastMessageSeverity.INFO));
                    }
                    else
                    {
                        ToastMessage.QueueMessage(player, new ToastMessage(Translation.Translate(FIRST_TIME ? "welcome_message_first_time" : "welcome_message", player,
                            UCWarfare.GetColorHex("uncreated"), names.CharacterName, UCWarfare.GetColorHex("neutral")), EToastMessageSeverity.INFO));
                    }
                    await UCWarfare.ToPool();
                    await Data.DatabaseManager.RegisterLogin(player.Player);
                    await t1;
                    if (ucplayer != null)
                    {
                        ucplayer.CachedXP = await t2;
                        ucplayer.CachedCredits = await t3;
                        ucplayer.AccessibleKits = await t4;
                        await UCWarfare.ToUpdate();
                        RequestSigns.InvokeLangUpdateForAllSigns(ucplayer.Player.channel.owner);
                        foreach (Vehicles.VehicleSpawn spawn in VehicleSpawner.ActiveObjects)
                            spawn.UpdateSign(ucplayer.Player.channel.owner);
                        Points.UpdateCreditsUI(ucplayer);
                        Points.UpdateXPUI(ucplayer);
                        player.Player.gameObject.AddComponent<ZonePlayerComponent>().Init(ucplayer);
                    }
                }).ConfigureAwait(false);

                if (ucplayer != null)
                {
                    Data.Gamemode.OnPlayerJoined(ucplayer, false, shouldRespawn);
                    ActionLog.Add(EActionLogType.CONNECT, null, ucplayer);
                }
                for (int i = 0; i < VehicleSpawner.ActiveObjects.Count; i++)
                {
                    VehicleSpawner.ActiveObjects[i].UpdateSign(player.Player.channel.owner);
                }
                Chat.Broadcast("player_connected", names.CharacterName);
                Data.Reporter.OnPlayerJoin(player.Player.channel.owner);
                if (ucplayer != null)
                {
                    Invocations.Shared.PlayerJoined.NetInvoke(new FPlayerList
                    {
                        Duty = ucplayer.OnDuty(),
                        Name = names.CharacterName,
                        Steam64 = ucplayer.Steam64,
                        Team = player.GetTeamByte()
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

        private static void VoiceMutedUseTick()
        {
            // send ui or something
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
                {
                    VoiceMutedUseTick();
                    isMuted = true;
                }
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
                return;
            }
            bool isMuted = false;
            bool squad = false;
            ulong team = 0;
            UCPlayer? ucplayer = PlayerManager.FromID(speaker.channel.owner.playerID.steamID.m_SteamID);
            if (ucplayer is not null)
            {
                team = ucplayer.GetTeam();
                if (ucplayer.MuteType != Commands.EMuteType.NONE && ucplayer.TimeUnmuted > DateTime.Now)
                {
                    VoiceMutedUseTick();
                    isMuted = true;
                }
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
                                ucplayer.LastSpoken = Time.realtimeSinceStartup;
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
            L.Log(Translation.Translate("battleye_kick_console", 0, out _, names.PlayerName, client.playerID.steamID.m_SteamID.ToString(), reason));
            if (UCWarfare.Config.AdminLoggerSettings.LogBattleyeKick &&
                UCWarfare.Config.AdminLoggerSettings.BattleyeExclusions != null &&
                !UCWarfare.Config.AdminLoggerSettings.BattleyeExclusions.Contains(reason))
            {
                ulong id = client.playerID.steamID.m_SteamID;
                Data.DatabaseManager.AddBattleyeKick(id, reason);
                Invocations.Shared.LogBattleyeKicked.NetInvoke(id, reason, DateTime.Now);
            }
        }
        internal static void OnEnterStorage(CSteamID instigator, InteractableStorage storage, ref bool shouldAllow)
        {
#if DEBUG
            using IDisposable profiler = ProfilingUtils.StartTracking();
#endif
            if (storage == null || !shouldAllow || Gamemode.Config.Barricades.TimeLimitedStorages == null || Gamemode.Config.Barricades.TimeLimitedStorages.Length == 0 || UCWarfare.Config.MaxTimeInStorages <= 0) return;
            SteamPlayer player = PlayerTool.getSteamPlayer(instigator);
            BarricadeDrop storagedrop = BarricadeManager.FindBarricadeByRootTransform(storage.transform);
            if (player == null || storagedrop == null ||
                !Gamemode.Config.Barricades.TimeLimitedStorages.Contains(storagedrop.asset.GUID)) return;
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
                if (drop.asset.GUID == Gamemode.Config.Barricades.FOBRadioDamagedGUID && instigatorSteamID != CSteamID.Nil)
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
                    SteamPlayer pl = PlayerTool.getSteamPlayer(instigatorSteamID);
                    ulong team = drop.GetServersideData().group.GetTeam();
                    if (team == 0 || pl == null || pl.GetTeam() != team) return;
                    if (damageOrigin == EDamageOrigin.Rocket_Explosion)
                    {
                        if (pl.player.TryGetPlaytimeComponent(out PlaytimeComponent c))
                        {
                            weapon = c.lastProjected;
                        }
                        else if (pl.player.equipment.asset != null)
                        {
                            weapon = pl.player.equipment.asset.GUID;
                        }
                        else weapon = Guid.Empty;
                    }
                    else if (damageOrigin == EDamageOrigin.Useable_Gun)
                    {
                        if (pl.player.TryGetPlaytimeComponent(out PlaytimeComponent c))
                        {
                            weapon = c.lastShot;
                        }
                        else if (pl.player.equipment.asset != null)
                        {
                            weapon = pl.player.equipment.asset.GUID;
                        }
                        else weapon = Guid.Empty;
                    }
                    else if (damageOrigin == EDamageOrigin.Grenade_Explosion)
                    {
                        if (pl.player.TryGetPlaytimeComponent(out PlaytimeComponent c))
                        {
                            weapon = c.thrown.FirstOrDefault(x => Assets.find<ItemThrowableAsset>(x.ThrowableID)?.isExplosive ?? false)?.ThrowableID ?? Guid.Empty;
                        }
                        else if (pl.player.equipment.asset != null)
                        {
                            weapon = pl.player.equipment.asset.GUID;
                        }
                        else weapon = Guid.Empty;
                    }
                    else if (damageOrigin == EDamageOrigin.Trap_Explosion)
                    {
                        if (pl.player.TryGetPlaytimeComponent(out PlaytimeComponent c))
                        {
                            weapon = c.LastLandmineTriggered.barricadeGUID;
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
                    Data.Reporter.OnDamagedStructure(instigatorSteamID.m_SteamID, new ReportSystem.Reporter.StructureDamageData()
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
                        if (pl.player.TryGetPlaytimeComponent(out PlaytimeComponent c))
                        {
                            weapon = c.lastProjected;
                        }
                        else if (pl.player.equipment.asset != null)
                        {
                            weapon = pl.player.equipment.asset.GUID;
                        }
                        else weapon = Guid.Empty;
                    }
                    else if (damageOrigin == EDamageOrigin.Useable_Gun)
                    {
                        if (pl.player.TryGetPlaytimeComponent(out PlaytimeComponent c))
                        {
                            weapon = c.lastProjected;
                        }
                        else if (pl.player.equipment.asset != null)
                        {
                            weapon = pl.player.equipment.asset.GUID;
                        }
                        else weapon = Guid.Empty;
                    }
                    else if (damageOrigin == EDamageOrigin.Grenade_Explosion)
                    {
                        if (pl.player.TryGetPlaytimeComponent(out PlaytimeComponent c))
                        {
                            weapon = c.thrown.FirstOrDefault(x => Assets.find<ItemThrowableAsset>(x.ThrowableID)?.isExplosive ?? false)?.ThrowableID ?? Guid.Empty;
                        }
                        else if (pl.player.equipment.asset != null)
                        {
                            weapon = pl.player.equipment.asset.GUID;
                        }
                        else weapon = Guid.Empty;
                    }
                    else if (damageOrigin == EDamageOrigin.Trap_Explosion)
                    {
                        if (pl.player.TryGetPlaytimeComponent(out PlaytimeComponent c))
                        {
                            weapon = c.LastLandmineTriggered.barricadeGUID;
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
                    Data.Reporter.OnDamagedStructure(instigatorSteamID.m_SteamID, new ReportSystem.Reporter.StructureDamageData()
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
        internal static void OnEnterVehicle(Player player, InteractableVehicle vehicle, ref bool shouldAllow)
        {
#if DEBUG
            using IDisposable profiler = ProfilingUtils.StartTracking();
#endif
            if (shouldAllow)
            {
                if (vehicle != null)
                {
                    if (!vehicle.transform.TryGetComponent(out VehicleComponent component))
                    {
                        component = vehicle.transform.gameObject.AddComponent<VehicleComponent>();
                        component.Initialize(vehicle);
                    }

                    component.OnPlayerEnteredVehicle(player, vehicle);
                }
            }

            if (Data.Is<IFlagRotation>(out _) && player.IsOnFlag(out Flag flag))
            {
                SendUIParameters p = CTFUI.RefreshStaticUI(player.GetTeam(), flag, true);
                if (p.status != EFlagStatus.BLANK && p.status != EFlagStatus.DONT_DISPLAY)
                    p.SendToPlayer(player.channel.owner);
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
            if (Data.Gamemode is TeamGamemode gm && gm.EnableAMC && parameters.killer != CSteamID.Nil && parameters.killer != Provider.server && parameters.killer != parameters.player.channel.owner.playerID.steamID) // prevent killer from being null or suicidal
            {
                Player killer = PlayerTool.getPlayer(parameters.killer);
                if (killer != null)
                {
                    ulong killerteam = killer.GetTeam();
                    ulong deadteam = parameters.player.GetTeam();
                    if ((deadteam == 1 && killerteam == 2 && TeamManager.Team1AMC.IsInside(parameters.player.transform.position)) ||
                        (deadteam == 2 && killerteam == 1 && TeamManager.Team2AMC.IsInside(parameters.player.transform.position)))
                    {
                        // if the player has shot since they died
                        if (!killer.TryGetPlaytimeComponent(out PlaytimeComponent comp) || comp.lastShot != default)
                            goto next;
                        shouldAllow = false;
                        byte newdamage = (byte)Math.Min(byte.MaxValue, Mathf.RoundToInt(parameters.damage * parameters.times * UCWarfare.Config.AMCDamageMultiplier));
                        killer.life.askDamage(newdamage, parameters.direction * newdamage, EDeathCause.ARENA,
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
                Structures.StructureSaver.Save();
                if (Vehicles.VehicleSpawner.IsRegistered(instanceID, out Vehicles.VehicleSpawn spawn, Structures.EStructType.STRUCTURE))
                {
                    IEnumerable<Vehicles.VehicleSign> linked = Vehicles.VehicleSigns.GetLinkedSigns(spawn);
                    int i = 0;
                    foreach (Vehicles.VehicleSign sign in linked)
                    {
                        i++;
                        sign.bay_transform = found.transform;
                    }
                    if (i > 0) Vehicles.VehicleSigns.Save();
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
                Structures.StructureSaver.Save();
                if (Vehicles.VehicleSpawner.IsRegistered(instanceID, out Vehicles.VehicleSpawn spawn, Structures.EStructType.BARRICADE))
                {
                    IEnumerable<Vehicles.VehicleSign> linked = Vehicles.VehicleSigns.GetLinkedSigns(spawn);
                    int i = 0;
                    foreach (Vehicles.VehicleSign sign in linked)
                    {
                        i++;
                        sign.bay_transform = found.transform;
                    }
                    if (i > 0) Vehicles.VehicleSigns.Save();
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
                        RequestSigns.Save();
                    }
                    else if (Vehicles.VehicleSigns.SignExists(sign, out Vehicles.VehicleSign vbsign))
                    {
                        vbsign.sign_transform = new SerializableTransform(new SerializableVector3(point), new SerializableVector3(angle_x * 2f, angle_y * 2f, angle_z * 2f));
                        Vehicles.VehicleSigns.Save();
                    }
                }
            }
        }
        internal static void OnPlayerLeavesVehicle(Player player, InteractableVehicle vehicle, ref bool shouldAllow, ref Vector3 pendingLocation, ref float pendingYaw)
        {
            if (shouldAllow)
            {
#if DEBUG
                using IDisposable profiler = ProfilingUtils.StartTracking();
#endif
                if (vehicle.transform.TryGetComponent(out VehicleComponent component))
                {
                    component.OnPlayerExitedVehicle(player, vehicle);
                }

                VehicleSpawner.OnPlayerLeaveVehicle(player, vehicle);
                ActionLog.Add(EActionLogType.LEAVE_VEHICLE_SEAT, $"{vehicle.asset.vehicleName} / {vehicle.asset.id} / {vehicle.asset.GUID:N}, Owner: {vehicle.lockedOwner.m_SteamID}, " +
                                                                 $"ID: ({vehicle.instanceID})", player.channel.owner.playerID.steamID.m_SteamID);
            }
        }
        internal static void OnVehicleSwapSeatRequested(Player player, InteractableVehicle vehicle, ref bool shouldAllow, byte fromSeatIndex, ref byte toSeatIndex)
        {
            if (shouldAllow)
            {
#if DEBUG
                using IDisposable profiler = ProfilingUtils.StartTracking();
#endif
                if (vehicle.transform.TryGetComponent(out VehicleComponent component))
                {
                    component.OnPlayerSwapSeatRequested(player, vehicle, toSeatIndex);
                }

                VehicleSpawner.OnPlayerLeaveVehicle(player, vehicle);
                ActionLog.Add(EActionLogType.ENTER_VEHICLE_SEAT, $"{vehicle.asset.vehicleName} / {vehicle.asset.id} / {vehicle.asset.GUID:N}, Owner: {vehicle.lockedOwner.m_SteamID}, " +
                                                                 $"ID: ({vehicle.instanceID}) Seat move: {fromSeatIndex.ToString(Data.Locale)} >> " +
                                                                 $"{toSeatIndex.ToString(Data.Locale)}", player.channel.owner.playerID.steamID.m_SteamID);
            }
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
        internal static void OnPlayerDisconnected(UnturnedPlayer player)
        {
#if DEBUG
            using IDisposable profiler = ProfilingUtils.StartTracking();
#endif
            ulong s64 = player.Player.channel.owner.playerID.steamID.m_SteamID;
            droppeditems.Remove(s64);
            TeamManager.PlayerBaseStatus.Remove(s64);
            RemoveDamageMessageTicks(s64);
            Tips.OnPlayerDisconnected(s64);
            UCPlayer? ucplayer = UCPlayer.FromUnturnedPlayer(player);
            string kit = string.Empty;
            try
            {
                if (ucplayer != null)
                {
                    Quests.DailyQuests.DeregisterDailyTrackers(ucplayer);
                    Quests.QuestManager.DeregisterOwnedTrackers(ucplayer);
                    if (Data.Is(out ITeams gm) && gm.UseJoinUI)
                        gm.JoinManager.OnPlayerDisconnected(ucplayer);
                    if (Data.Is<IFOBs>(out _)) FOBManager.OnPlayerDisconnect(ucplayer);
                    kit = ucplayer.KitName;
                    try
                    {
                        Data.Gamemode.OnPlayerLeft(ucplayer);
                    }
                    catch (Exception ex)
                    {
                        L.LogError("Error in the " + Data.Gamemode.Name + " OnPlayerLeft:");
                        L.LogError(ex);
                    }
                }
                FPlayerName names = F.GetPlayerOriginalNames(player.Player.channel.owner);
                if (player.OnDuty())
                {
                    if (player.IsAdmin())
                        Commands.DutyCommand.AdminOnToOff(player, names);
                    else if (player.IsIntern())
                        Commands.DutyCommand.InternOnToOff(player, names);
                }
                PlaytimeComponent? c = player.CSteamID.GetPlaytimeComponent(out bool gotptcomp);
                Data.OriginalNames.Remove(s64);
                ulong id = s64;
                Chat.Broadcast("player_disconnected", names.CharacterName);
                if (c != null)
                {
                    ActionLog.Add(EActionLogType.DISCONNECT, "PLAYED FOR " + ((uint)Mathf.RoundToInt(Time.realtimeSinceStartup - c.JoinTime)).GetTimeFromSeconds(0).ToUpper(), player.CSteamID.m_SteamID);
                    UnityEngine.Object.Destroy(c);
                    Data.PlaytimeComponents.Remove(player.CSteamID.m_SteamID);
                }
                else
                    ActionLog.Add(EActionLogType.DISCONNECT, null, player.CSteamID.m_SteamID);
                Invocations.Shared.PlayerLeft.NetInvoke(player.CSteamID.m_SteamID);
            }
            catch (Exception ex)
            {
                L.LogError("Error in the main OnPlayerDisconnected:");
                L.LogError(ex);
            }
            try
            {
                PlayerManager.InvokePlayerDisconnected(player);
            }
            catch (Exception ex)
            {
                L.LogError("Failed to remove a player from the list:");
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
        internal static void LangCommand_OnPlayerChangedLanguage(UnturnedPlayer player, LanguageAliasSet oldSet, LanguageAliasSet newSet)
            => UCWarfare.I.UpdateLangs(player.Player.channel.owner);
        internal static void OnPrePlayerConnect(ValidateAuthTicketResponse_t ticket, ref bool isValid, ref string explanation)
        {
#if DEBUG
            using IDisposable profiler = ProfilingUtils.StartTracking();
#endif
            SteamPending player = Provider.pending.FirstOrDefault(x => x.playerID.steamID.m_SteamID == ticket.m_SteamID.m_SteamID);
            if (player == null) return;
            try
            {
                ActionLog.Add(EActionLogType.TRY_CONNECT, $"Steam Name: {player.playerID.playerName}, Public Name: {player.playerID.characterName}, Private Name: {player.playerID.nickName}, Character ID: {player.playerID.characterID}.", ticket.m_SteamID.m_SteamID);
                int remainingDuration = 0;
                OffenseManager.EBanResponse response = OffenseManager.VerifyJoin(player, ref explanation, ref remainingDuration);
                if (response > OffenseManager.EBanResponse.ALL_GOOD)
                {
                    isValid = false;
                    L.Log("Rejecting " + player.playerID.playerName + " (" + player.playerID.steamID.m_SteamID.ToString(Data.Locale) + ") because " + response.ToString());
                    return;
                }

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
                    explanation = Translation.Translate("kick_autokick_namefilter", player.playerID.steamID.m_SteamID);
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
                    explanation = Translation.Translate("kick_autokick_namefilter", player.playerID.steamID.m_SteamID);
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
                ActionLog.Add(EActionLogType.DESTROY_STRUCTURE, $"{drop.asset.itemName} / {drop.asset.id} / {drop.asset.GUID:N} - Owner: {c.Owner}, Team: {TeamManager.TranslateName(data.group.GetTeam(), 0)}, ID: {drop.instanceID}", c.LastDamager);
                if (damager != null && data.group.GetTeam() == damager.GetTeam())
                {
                    Data.Reporter.OnDestroyedStructure(c.LastDamager, instanceID);
                }
            }
        }
        internal static void OnBarricadeDestroyed(SDG.Unturned.BarricadeData data, BarricadeDrop drop, uint instanceID, ushort plant)
        {
#if DEBUG
            using IDisposable profiler = ProfilingUtils.StartTracking();
#endif
            if (Data.Is<IFOBs>(out _))
            {
                FOBManager.OnBarricadeDestroyed(data, drop, instanceID, plant);
                RepairManager.OnBarricadeDestroyed(data, drop, instanceID, plant);
            }

            if (drop.model.TryGetComponent(out BuildableComponent buildable))
                buildable.Destroy();
            if (drop.model.TryGetComponent(out RepairableComponent repairable))
                repairable.Destroy();

            IconRenderer[] iconrenderers = drop.model.GetComponents<IconRenderer>();
            foreach (IconRenderer iconRenderer in iconrenderers)
                IconManager.DeleteIcon(iconRenderer);

            if (Data.Is<ISquads>(out _))
                RallyManager.OnBarricadeDestroyed(data, drop, instanceID, plant);
            if (Data.Is(out IVehicles v))
            {
                v.VehicleSpawner.OnBarricadeDestroyed(data, drop, instanceID, plant);
                v.VehicleSigns.OnBarricadeDestroyed(data, drop, instanceID, plant);
            }
            if (drop.model.TryGetComponent(out BarricadeComponent c))
            {
                SteamPlayer damager = PlayerTool.getSteamPlayer(c.LastDamager);
                ActionLog.Add(EActionLogType.DESTROY_BARRICADE, $"{drop.asset.itemName} / {drop.asset.id} / {drop.asset.GUID:N} - Owner: {c.Owner}, Team: {TeamManager.TranslateName(data.group.GetTeam(), 0)}, ID: {drop.instanceID}", c.LastDamager);
                if (damager != null && data.group.GetTeam() == damager.GetTeam())
                {
                    Data.Reporter.OnDestroyedStructure(c.LastDamager, instanceID);
                }
            }
        }
        internal static void OnPostHealedPlayer(Player instigator, Player target)
        {
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
}
