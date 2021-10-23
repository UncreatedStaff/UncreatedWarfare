﻿using Rocket.Unturned.Player;
using SDG.Unturned;
using Steamworks;
using System;
using System.Collections.Generic;
using System.Linq;
using Uncreated.Networking;
using Uncreated.Players;
using Uncreated.Warfare.Components;
using Uncreated.Warfare.FOBs;
using Uncreated.Warfare.Gamemodes.Flags.TeamCTF;
using Uncreated.Warfare.Kits;
using Uncreated.Warfare.Networking;
using Uncreated.Warfare.Officers;
using Uncreated.Warfare.Squads;
using Uncreated.Warfare.Stats;
using Uncreated.Warfare.Teams;
using Uncreated.Warfare.Tickets;
using Uncreated.Warfare.XP;
using UnityEngine;
using Flag = Uncreated.Warfare.Gamemodes.Flags.Flag;
using Item = SDG.Unturned.Item;

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
            ulong oldteam = oldGroup.GetTeam();
            ulong newteam = newGroup.GetTeam();

            PlayerManager.VerifyTeam(player.player);
            Data.Gamemode?.OnGroupChanged(player, oldGroup, newGroup, oldteam, newteam);


            SquadManager.ClearUIsquad(player.player);
            SquadManager.UpdateUIMemberCount(newGroup);
            SquadManager.OnGroupChanged(player, oldGroup, newGroup);
            TicketManager.OnGroupChanged(player, oldGroup, newGroup);
            FOBManager.UpdateUI(UCPlayer.FromSteamPlayer(player));

            RequestSigns.InvokeLangUpdateForAllSigns(player);

            XPManager.OnGroupChanged(player, oldGroup, newGroup);
            OfficerManager.OnGroupChanged(player, oldGroup, newGroup);
            Invocations.Shared.TeamChanged.NetInvoke(player.playerID.steamID.m_SteamID, F.GetTeamByte(newGroup));
        }
        internal static void OnStructureDestroyed(SDG.Unturned.StructureData data, StructureDrop drop, uint instanceID)
        {
            Data.VehicleSpawner.OnStructureDestroyed(data, drop, instanceID);
        }
        internal static Dictionary<Item, PlayerInventory> itemstemp = new Dictionary<Item, PlayerInventory>();
        internal static Dictionary<ulong, List<uint>> droppeditems = new Dictionary<ulong, List<uint>>();
        internal static void OnDropItemTry(PlayerInventory inv, Item item, ref bool allow)
        {
            if (!UCWarfare.Config.ClearItemsOnAmmoBoxUse) return;
            if (KitManager.HasKit(inv.player, out Kit kit))
            {
                bool inkit = kit.Items.Exists(k => k.ID == item.id);
                if (inkit)
                {
                    if (!itemstemp.ContainsKey(item))
                        itemstemp.Add(item, inv);
                    else itemstemp[item] = inv;
                }
            }
        }
        internal static void OnDropItemFinal(Item item, ref Vector3 location, ref bool shouldAllow)
        {
            if (!UCWarfare.Config.ClearItemsOnAmmoBoxUse) return;
            if (itemstemp.TryGetValue(item, out PlayerInventory inv))
            {
                uint nextindex;
                try
                {
                    nextindex = (uint)Data.ItemManagerInstanceCount.GetValue(null);
                }
                catch
                {
                    F.LogError("Unable to get ItemManager.instanceCount.");
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
                itemstemp.Remove(item);
            }
        }
        internal static void OnBarricadeDestroyed(SDG.Unturned.BarricadeData data, BarricadeDrop drop, uint instanceID, ushort plant)
        {
            FOBManager.OnBarricadeDestroyed(data, drop, instanceID, plant);
            RallyManager.OnBarricadeDestroyed(data, drop, instanceID, plant);
            RepairManager.OnBarricadeDestroyed(data, drop, instanceID, plant);
            Data.VehicleSpawner.OnBarricadeDestroyed(data, drop, instanceID, plant);
            Data.VehicleSigns.OnBarricadeDestroyed(data, drop, instanceID, plant);
        }
        internal static void StopCosmeticsToggleEvent(ref EVisualToggleType type, SteamPlayer player, ref bool allow)
        {
            if (!UCWarfare.Config.AllowCosmetics) allow = UnturnedPlayer.FromSteamPlayer(player).OnDuty();
        }
        internal static void StopCosmeticsSetStateEvent(ref EVisualToggleType type, SteamPlayer player, ref bool state, ref bool allow)
        {
            if (!UCWarfare.Config.AllowCosmetics && UnturnedPlayer.FromSteamPlayer(player).OffDuty()) state = false;
        }
        internal static void OnBarricadePlaced(BarricadeRegion region, BarricadeDrop drop)
        {
            SDG.Unturned.BarricadeData data = drop.GetServersideData();

            if (UCWarfare.Config.Debug)
                F.Log($"{data.owner} Placed barricade: {data.barricade.asset.itemName}, {data.point}", ConsoleColor.DarkGray);
            BarricadeComponent owner = drop.model.gameObject.AddComponent<BarricadeComponent>();
            owner.Owner = data.owner;
            SteamPlayer player = PlayerTool.getSteamPlayer(data.owner);
            owner.Player = player?.player;
            owner.BarricadeID = data.barricade.id;
            RallyManager.OnBarricadePlaced(drop, region);

            RepairManager.OnBarricadePlaced(drop, region);

            // ammo bag
            if (FOBManager.config.Data.AmmoBagIDs.Contains(data.barricade.id))
            {
                drop.model.gameObject.AddComponent<AmmoBagComponent>().Initialize(data, drop);
            }

            if (data.barricade.id == FOBManager.config.Data.AmmoCrateID)
            {
                if (drop.interactable is InteractableStorage storage)
                {
                    storage.onStateRebuilt = (InteractableStorage s, byte[] state, int size) =>
                    {
                        FOBManager.OnAmmoCrateUpdated(s, drop);
                    };
                }
            }

            if (data.barricade.id == FOBManager.config.Data.FOBBaseID)
            {
                drop.model.gameObject.AddComponent<FOBBaseComponent>().Initialize(drop, data);
            }
        }
        internal static void ThrowableSpawned(UseableThrowable useable, GameObject throwable)
        {
            try
            {
                ThrowableOwner t = throwable.AddComponent<ThrowableOwner>();
                PlaytimeComponent c = F.GetPlaytimeComponent(useable.player, out bool success);
                t.Set(useable, throwable, c);
                if (UCWarfare.Config.Debug)
                    F.Log(useable.player.name + " spawned a throwable: " + (useable.equippedThrowableAsset != null ?
                        useable.equippedThrowableAsset.itemName : useable.name), ConsoleColor.DarkGray);
                if (success)
                    c.thrown.Add(t);
            }
            catch (Exception ex)
            {
                F.LogError("Exception in ThrowableSpawned:");
                F.LogError(ex);
            }
        }
        internal static void ProjectileSpawned(UseableGun gun, GameObject projectile)
        {
            Patches.DeathsPatches.lastProjected = projectile;
            if (F.TryGetPlaytimeComponent(gun.player, out PlaytimeComponent c))
            {
                c.lastProjected = gun.equippedGunAsset.id;
            }
        }
        internal static void BulletSpawned(UseableGun gun, BulletInfo bullet)
        {
            PlaytimeComponent c = F.GetPlaytimeComponent(gun.player, out bool success);
            if (success)
            {
                c.lastShot = gun.equippedGunAsset.id;
            }
        }
        internal static void ReloadCommand_onTranslationsReloaded()
        {
            foreach (SteamPlayer player in Provider.clients)
                UCWarfare.I.UpdateLangs(player);
        }
        internal static void OnBarricadeTryPlaced(Barricade barricade, ItemBarricadeAsset asset, Transform hit, ref Vector3 point, ref float angle_x,
            ref float angle_y, ref float angle_z, ref ulong owner, ref ulong group, ref bool shouldAllow)
        {
            try
            {
                if (!shouldAllow) return;
                UCPlayer player = UCPlayer.FromID(owner);
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
                RallyManager.OnBarricadePlaceRequested(barricade, asset, hit, ref point, ref angle_x, ref angle_y, ref angle_z, ref owner, ref group, ref shouldAllow);
                if (!shouldAllow) return;
                if (barricade.id == FOBManager.config.Data.FOBBaseID && FOBManager.config.Data.RestrictFOBPlacement)
                {
                    if (SDG.Framework.Water.WaterUtility.isPointUnderwater(point))
                    {
                        shouldAllow = false;
                        player?.SendChat("no_placement_fobs_underwater");
                        return;
                    }
                    else if (point.y > F.GetTerrainHeightAt2DPoint(point.x, point.z, point.y, 0) + FOBManager.config.Data.FOBMaxHeightAboveTerrain)
                    {
                        shouldAllow = false;
                        player?.SendChat("no_placement_fobs_too_high", Mathf.RoundToInt(FOBManager.config.Data.FOBMaxHeightAboveTerrain).ToString(Data.Locale));
                        return;
                    }
                    else if (TeamManager.IsInAnyMainOrAMCOrLobby(point))
                    {
                        shouldAllow = false;
                        player?.SendChat("no_placement_fobs_too_near_base");
                        return;
                    }
                }
                else if (FOBManager.config.Data.AmmoBagIDs.Contains(barricade.id))
                {
                    if (player != null && player.OffDuty() && player.KitClass != EClass.RIFLEMAN)
                    {
                        shouldAllow = false;
                        player.SendChat("ammo_not_rifleman");
                        return;
                    }
                }
                ulong team = group.GetTeam();
                Data.Whitelister.OnBarricadePlaceRequested(barricade, asset, hit, ref point, ref angle_x, ref angle_y, ref angle_z, ref owner, ref group, ref shouldAllow);
                if (!shouldAllow) return;
                if (team == 1)
                {
                    if (player != null && !player.OnDuty() && TeamManager.Team2AMC.IsInside(point))
                    {
                        shouldAllow = false;
                        player.Message("whitelist_noplace");
                        return;
                    }
                }
                else if (team == 2)
                {
                    if (player != null && !player.OnDuty() && TeamManager.Team1AMC.IsInside(point))
                    {
                        shouldAllow = false;
                        player.Message("whitelist_noplace");
                        return;
                    }
                }
            }
            catch (Exception ex)
            {
                F.LogError("Error in OnBarricadeTryPlaced:");
                F.LogError(ex);
            }
        }

        internal static void OnPostHealedPlayer(Player instigator, Player target)
        {
            Data.ReviveManager.ClearInjuredMarker(instigator.channel.owner.playerID.steamID.m_SteamID, instigator.GetTeam());
            Data.ReviveManager.OnPlayerHealed(instigator, target);
        }
        internal static void OnPostPlayerConnected(UnturnedPlayer player)
        {
            if (!UCWarfare.Config.UsePatchForPlayerCap && Provider.clients.Count >= 24)
            {
                Provider.maxPlayers = UCWarfare.Config.MaxPlayerCount;
            }
            try
            {
                PlayerManager.InvokePlayerConnected(player); // must always be first
            }
            catch (Exception ex)
            {
                F.LogError("Error in the main OnPostPlayerConnected loading player into OnlinePlayers:");
                F.LogError(ex);
            }
            try
            {
                // reset the player to spawn if they have joined in a different game as they last played in.

                UCPlayer ucplayer = UCPlayer.FromUnturnedPlayer(player);

                if (PlayerManager.HasSave(player.CSteamID.m_SteamID, out PlayerSave save))
                {
                    if (save.LastGame != Data.Gamemode.GameID || save.ShouldRespawnOnJoin)
                    {
                        Data.JoinManager.OnPlayerConnected(ucplayer, true);

                        if (player.Player.life.isDead)
                            player.Player.life.ReceiveRespawnRequest(false);
                        else
                        {
                            player.Player.life.sendRevive();
                            player.Player.teleportToLocation(player.Player.GetBaseSpawn(out ulong t), t.GetBaseAngle());
                        }
                        save.ShouldRespawnOnJoin = false;

                        PlayerManager.ApplyToOnline();
                    }
                    else
                    {
                        Data.JoinManager.OnPlayerConnected(ucplayer, false);
                    }
                }

                if (KitManager.KitExists(ucplayer.KitName, out Kit kit))
                {
                    if (kit.IsLimited(out int currentPlayers, out int allowedPlayers, player.GetTeam()) || (kit.IsLoadout && kit.IsClassLimited(out currentPlayers, out allowedPlayers, player.GetTeam())))
                    {
                        if (!KitManager.TryGiveRiflemanKit(ucplayer))
                            KitManager.TryGiveUnarmedKit(ucplayer);
                    }
                }
                Data.ReviveManager.DownedPlayers.Remove(player.CSteamID.m_SteamID);
                FPlayerName names = F.GetPlayerOriginalNames(player);
                if (Data.PlaytimeComponents.ContainsKey(player.Player.channel.owner.playerID.steamID.m_SteamID))
                {
                    UnityEngine.Object.DestroyImmediate(Data.PlaytimeComponents[player.Player.channel.owner.playerID.steamID.m_SteamID]);
                    Data.PlaytimeComponents.Remove(player.Player.channel.owner.playerID.steamID.m_SteamID);
                }
                PlaytimeComponent pt = player.Player.transform.gameObject.AddComponent<PlaytimeComponent>();
                pt.StartTracking(player.Player);
                Data.PlaytimeComponents.Add(player.Player.channel.owner.playerID.steamID.m_SteamID, pt);
                OfficerManager.OnPlayerJoined(ucplayer);
                XPManager.OnPlayerJoined(ucplayer);
                Data.DatabaseManager.CheckUpdateUsernames(names);
                bool FIRST_TIME = !Data.DatabaseManager.HasPlayerJoined(player.Player.channel.owner.playerID.steamID.m_SteamID);
                Data.DatabaseManager.RegisterLogin(player.Player);
                Data.Gamemode.OnPlayerJoined(player.Player.channel.owner);
                ulong team = player.GetTeam();
                ToastMessage.QueueMessage(player, F.Translate(FIRST_TIME ? "welcome_message_first_time" : "welcome_message", player,
                    UCWarfare.GetColorHex("uncreated"), names.CharacterName, TeamManager.GetTeamHexColor(team)), ToastMessageSeverity.INFO);
                if ((ucplayer.KitName == null || ucplayer.KitName == string.Empty) && team > 0 && team < 3)
                {
                    if (KitManager.KitExists(team == 1 ? TeamManager.Team1UnarmedKit : TeamManager.Team2UnarmedKit, out Kit unarmed))
                        KitManager.GiveKit(ucplayer, unarmed);
                    else if (KitManager.KitExists(TeamManager.DefaultKit, out unarmed)) KitManager.GiveKit(ucplayer, unarmed);
                    else F.LogWarning("Unable to give " + names.PlayerName + " a kit.");
                }
                F.Broadcast("player_connected", names.CharacterName);
                if (!UCWarfare.Config.AllowCosmetics)
                {
                    player.Player.clothing.ServerSetVisualToggleState(EVisualToggleType.COSMETIC, false);
                    player.Player.clothing.ServerSetVisualToggleState(EVisualToggleType.MYTHIC, false);
                    player.Player.clothing.ServerSetVisualToggleState(EVisualToggleType.SKIN, false);
                }
                if (UCWarfare.Config.ModifySkillLevels)
                {
                    player.Player.skills.ServerSetSkillLevel((int)EPlayerSpeciality.OFFENSE, (int)EPlayerOffense.SHARPSHOOTER, 7);
                    player.Player.skills.ServerSetSkillLevel((int)EPlayerSpeciality.OFFENSE, (int)EPlayerOffense.PARKOUR, 2);
                    player.Player.skills.ServerSetSkillLevel((int)EPlayerSpeciality.OFFENSE, (int)EPlayerOffense.EXERCISE, 1);
                    player.Player.skills.ServerSetSkillLevel((int)EPlayerSpeciality.OFFENSE, (int)EPlayerOffense.CARDIO, 5);
                    player.Player.skills.ServerSetSkillLevel((int)EPlayerSpeciality.DEFENSE, (int)EPlayerDefense.VITALITY, 5);
                }
                Data.ReviveManager.OnPlayerConnected(player);
                //PlayerManager.PickGroupAfterJoin(ucplayer);
                TicketManager.OnPlayerJoined(ucplayer);
                Invocations.Shared.PlayerJoined.NetInvoke(new FPlayerList
                {
                    Duty = ucplayer.OnDuty(),
                    Name = names.CharacterName,
                    Steam64 = ucplayer.Steam64,
                    Team = F.GetTeamByte(player)
                });
                StatsManager.RegisterPlayer(player.CSteamID.m_SteamID);
                StatsManager.ModifyStats(player.CSteamID.m_SteamID, s => s.LastOnline = DateTime.Now.Ticks);
            }
            catch (Exception ex)
            {
                F.LogError("Error in the main OnPostPlayerConnected:");
                F.LogError(ex);
            }
        }
        internal static void OnRelayVoice(PlayerVoice speaker, bool wantsToUseWalkieTalkie, ref bool shouldAllow,
            ref bool shouldBroadcastOverRadio, ref PlayerVoice.RelayVoiceCullingHandler cullingHandler)
        {
            if (!UCWarfare.Config.RelayMicsDuringEndScreen || Data.Gamemode == null || Data.Gamemode.State == Gamemodes.EState.ACTIVE) return;
            cullingHandler = new PlayerVoice.RelayVoiceCullingHandler((source, target) =>
            {
                return true;
            });
            shouldBroadcastOverRadio = true;
        }

        internal static void OnBattleyeKicked(SteamPlayer client, string reason)
        {
            FPlayerName names = F.GetPlayerOriginalNames(client.player);
            ulong team = client.GetTeam();
            F.Broadcast("battleye_kick_broadcast", F.ColorizeName(names.CharacterName, team));
            F.Log(F.Translate("battleye_kick_console", 0, out _, names.PlayerName, client.playerID.steamID.m_SteamID.ToString(), reason));
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
            if (storage == null || !shouldAllow || UCWarfare.Config.LimitedStorages == null || UCWarfare.Config.LimitedStorages.Length == 0 || UCWarfare.Config.MaxTimeInStorages <= 0) return;
            SteamPlayer player = PlayerTool.getSteamPlayer(instigator);
            BarricadeDrop storagedrop = BarricadeManager.FindBarricadeByRootTransform(storage.transform);
            if (player == null || storagedrop == null ||
                !UCWarfare.Config.LimitedStorages.Contains(storagedrop.GetServersideData().barricade.id)) return;
            UCPlayer ucplayer = UCPlayer.FromSteamPlayer(player);
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
            if (TeamManager.IsInAnyMainOrAMCOrLobby(barricadeTransform.position))
            {
                shouldAllow = false;
            }
            else
            {
                BarricadeDrop drop = BarricadeManager.FindBarricadeByRootTransform(barricadeTransform);
                if (drop != null && (Structures.StructureSaver.StructureExists(drop.instanceID, Structures.EStructType.BARRICADE, out Structures.Structure s) && s.transform == barricadeTransform))
                {
                    shouldAllow = false;
                }
            }
            if (shouldAllow && pendingTotalDamage > 0 && barricadeTransform.TryGetComponent(out BarricadeComponent c))
            {
                c.LastDamager = instigatorSteamID.m_SteamID;
            }
        }
        internal static void OnStructureDamaged(CSteamID instigatorSteamID, Transform structureTransform, ref ushort pendingTotalDamage, ref bool shouldAllow, EDamageOrigin damageOrigin)
        {
            if (TeamManager.IsInAnyMainOrAMCOrLobby(structureTransform.position))
            {
                shouldAllow = false;
            }
            else
            {
                StructureDrop drop = StructureManager.FindStructureByRootTransform(structureTransform);
                if (drop != null && (Structures.StructureSaver.StructureExists(drop.instanceID, Structures.EStructType.STRUCTURE, out Structures.Structure s) && s.transform == structureTransform))
                {
                    shouldAllow = false;
                }
            }
        }
        internal static void OnPluginKeyPressed(Player player, uint simulation, byte key, bool state)
        {
            if (state == false || key != 2 || player == null) return;
            Data.ReviveManager.GiveUp(player);
        }
        internal static void OnEnterVehicle(Player player, InteractableVehicle vehicle, ref bool shouldAllow)
        {
            if (Data.Gamemode is TeamCTF ctf && player.IsOnFlag(out Flag flag))
            {
                SendUIParameters p = CTFUI.RefreshStaticUI(player.GetTeam(), flag, true);
                if (p.status != F.EFlagStatus.BLANK && p.status != F.EFlagStatus.DONT_DISPLAY)
                    p.SendToPlayer(ctf.Config.PlayerIcon, ctf.Config.UseUI,
                    ctf.Config.CaptureUI, ctf.Config.ShowPointsOnUI, ctf.Config.ProgressChars, player.channel.owner,
                    player.channel.owner.transportConnection);
            }
            if (Vehicles.VehicleSpawner.HasLinkedSpawn(vehicle.instanceID, out Vehicles.VehicleSpawn spawn))
            {
                if (spawn.type == Structures.EStructType.BARRICADE && spawn.BarricadeDrop != null &&
                    spawn.BarricadeDrop.model.TryGetComponent(out Vehicles.SpawnedVehicleComponent c))
                {
                    c.StopIdleRespawnTimer();
                }
                else if
                   (spawn.type == Structures.EStructType.STRUCTURE && spawn.StructureDrop != null &&
                    spawn.StructureDrop.model.TryGetComponent(out c))
                {
                    c.StopIdleRespawnTimer();
                }
            }
        }
        static readonly Dictionary<ulong, long> lastSentMessages = new Dictionary<ulong, long>();
        internal static void RemoveDamageMessageTicks(ulong player)
        {
            lastSentMessages.Remove(player);
        }
        internal static void OnPlayerDamageRequested(ref DamagePlayerParameters parameters, ref bool shouldAllow)
        {
            if (parameters.killer != CSteamID.Nil && parameters.killer != Provider.server && parameters.killer != parameters.player.channel.owner.playerID.steamID) // prevent killer from being null or suicidal
            {
                Player killer = PlayerTool.getPlayer(parameters.killer);
                if (killer != null)
                {
                    ulong killerteam = killer.GetTeam();
                    ulong deadteam = parameters.player.GetTeam();
                    if ((deadteam == 1 && killerteam == 2 && TeamManager.Team1AMC.IsInside(parameters.player.transform.position)) ||
                        (deadteam == 2 && killerteam == 1 && TeamManager.Team2AMC.IsInside(parameters.player.transform.position)))
                    {
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

            if (shouldAllow)
                Data.ReviveManager.OnPlayerDamagedRequested(ref parameters, ref shouldAllow);
        }
        internal static void OnPlayerMarkedPosOnMap(Player player, ref Vector3 position, ref string overrideText, ref bool isBeingPlaced, ref bool allowed)
        {
            if (player == null) return;
            UCPlayer ucplayer = UCPlayer.FromPlayer(player);
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
            Vector3 effectposition = new Vector3(position.x, F.GetTerrainHeightAt2DPoint(position.x, position.z), position.z);
            PlaceMarker(ucplayer, effectposition, false, false);
        }
        internal static void OnPlayerGestureRequested(Player player, EPlayerGesture gesture, ref bool allow)
        {
            if (player == null) return;
            if (gesture == EPlayerGesture.POINT)
            {
                UCPlayer ucplayer = UCPlayer.FromPlayer(player);
                if (ucplayer == null) return;
                if (!Physics.Raycast(new Ray(player.look.aim.transform.position, player.look.aim.transform.forward), out RaycastHit hit, 8192f, RayMasks.BLOCK_COLLISION)) return;
                PlaceMarker(ucplayer, hit.point, true, true);
            }
        }
        private static void PlaceMarker(UCPlayer ucplayer, Vector3 Point, bool sendNoSquadChat, bool placeMarkerOnMap)
        {
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
            if (!player.inventory.isStoring || player == null || jar == null || jar.item == null || allow == false) return;
            UCPlayer utplayer = UCPlayer.FromPlayer(player);
            if (utplayer.OnDuty())
                return;
            if (!Whitelister.IsWhitelisted(jar.item.id, out _))
            {
                allow = false;
                player.SendChat("cant_store_this_item",
                    !(Assets.find(EAssetType.ITEM, jar.item.id) is ItemAsset asset) || asset.itemName == null ? jar.item.id.ToString(Data.Locale) : asset.itemName);
            }
        }
        internal static void StructureMovedInWorkzone(CSteamID instigator, byte x, byte y, uint instanceID, ref Vector3 point, ref byte angle_x, ref byte angle_y, ref byte angle_z, ref bool shouldAllow)
        {
            if (Structures.StructureSaver.StructureExists(instanceID, Structures.EStructType.STRUCTURE, out Structures.Structure found))
            {
                found.transform = new SerializableTransform(new SerializableVector3(point), new SerializableVector3(angle_x * 2f, angle_y * 2f, angle_z * 2f));
                Structures.StructureSaver.Save();
                if (Vehicles.VehicleSpawner.IsRegistered(instanceID, out Vehicles.VehicleSpawn spawn, Structures.EStructType.STRUCTURE))
                {
                    List<Vehicles.VehicleSign> linked = Vehicles.VehicleSigns.GetLinkedSigns(spawn);
                    if (linked.Count > 0)
                    {
                        for (int i = 0; i < linked.Count; i++)
                        {
                            linked[i].bay_transform = found.transform;
                        }
                        Vehicles.VehicleSigns.Save();
                    }
                }
            }
        }
        internal static void BarricadeMovedInWorkzone(CSteamID instigator, byte x, byte y, ushort plant, uint instanceID, ref Vector3 point, ref byte angle_x, ref byte angle_y, ref byte angle_z, ref bool shouldAllow)
        {
            if (Structures.StructureSaver.StructureExists(instanceID, Structures.EStructType.BARRICADE, out Structures.Structure found))
            {
                found.transform = new SerializableTransform(new SerializableVector3(point), new SerializableVector3(angle_x * 2f, angle_y * 2f, angle_z * 2f));
                Structures.StructureSaver.Save();
                if (Vehicles.VehicleSpawner.IsRegistered(instanceID, out Vehicles.VehicleSpawn spawn, Structures.EStructType.BARRICADE))
                {
                    List<Vehicles.VehicleSign> linked = Vehicles.VehicleSigns.GetLinkedSigns(spawn);
                    if (linked.Count > 0)
                    {
                        for (int i = 0; i < linked.Count; i++)
                        {
                            linked[i].bay_transform = found.transform;
                        }
                        Vehicles.VehicleSigns.Save();
                    }
                }
            }
            F.GetBarricadeFromInstID(instanceID, out BarricadeDrop drop);
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
                Vehicles.VehicleSpawner.OnPlayerLeaveVehicle(player, vehicle);
        }
        internal static void BatteryStolen(SteamPlayer theif, ref bool allow)
        {
            if (!UCWarfare.Config.AllowBatteryStealing)
            {
                allow = false;
                theif.SendChat("cant_steal_batteries");
            }
        }
        internal static void OnCalculateSpawnDuringRevive(PlayerLife sender, bool wantsToSpawnAtHome, ref Vector3 position, ref float yaw)
        {
            ulong team = sender.player.GetTeam();
            position = team.GetBaseSpawnFromTeam();
            yaw = team.GetBaseAngle();
        }
        internal static void OnPlayerDisconnected(UnturnedPlayer player)
        {
            if (!UCWarfare.Config.UsePatchForPlayerCap && Provider.clients.Count - 1 < 24)
            {
                Provider.maxPlayers = 24;
            }
            Data.ReviveManager.OnPlayerDisconnected(player.Player.channel.owner);
            droppeditems.Remove(player.Player.channel.owner.playerID.steamID.m_SteamID);
            RemoveDamageMessageTicks(player.Player.channel.owner.playerID.steamID.m_SteamID);
            UCPlayer ucplayer = UCPlayer.FromUnturnedPlayer(player);
            Data.JoinManager.OnPlayerDisconnected(ucplayer);
            string kit = ucplayer.KitName;
            try
            {
                StatsCoroutine.previousPositions.Remove(player.Player.channel.owner.playerID.steamID.m_SteamID);
                FPlayerName names = F.GetPlayerOriginalNames(player.Player.channel.owner);
                if (player.OnDuty())
                {
                    if (player.IsAdmin())
                        Commands.DutyCommand.AdminOnToOff(player, names);
                    else if (player.IsIntern())
                        Commands.DutyCommand.InternOnToOff(player, names);
                }
                PlaytimeComponent c = F.GetPlaytimeComponent(player.CSteamID, out bool gotptcomp);
                Data.OriginalNames.Remove(player.Player.channel.owner.playerID.steamID.m_SteamID);
                ulong id = player.Player.channel.owner.playerID.steamID.m_SteamID;
                //Client.SendPlayerLeft(names);
                Data.Gamemode.OnPlayerLeft(id);
                F.Broadcast("player_disconnected", names.CharacterName);
                if (gotptcomp)
                {
                    UnityEngine.Object.Destroy(c);
                    Data.PlaytimeComponents.Remove(player.CSteamID.m_SteamID);
                }
                Invocations.Shared.PlayerLeft.NetInvoke(player.CSteamID.m_SteamID);
                StatsManager.DeregisterPlayer(player.CSteamID.m_SteamID);
            }
            catch (Exception ex)
            {
                F.LogError("Error in the main OnPlayerDisconnected:");
                F.LogError(ex);
            }
            try
            {
                PlayerManager.InvokePlayerDisconnected(player);
            }
            catch (Exception ex)
            {
                F.LogError("Failed to remove a player from the list:");
                F.LogError(ex);
            }
            try
            {
                if (RequestSigns.SignExists(kit, out RequestSign sign))
                    sign.InvokeUpdate();
            }
            catch (Exception ex)
            {
                F.LogError("Failed to update kit sign for leaving player:");
                F.LogError(ex);
            }
        }
        internal static void LangCommand_OnPlayerChangedLanguage(UnturnedPlayer player, LanguageAliasSet oldSet, LanguageAliasSet newSet)
            => UCWarfare.I.UpdateLangs(player.Player.channel.owner);
        internal static void OnPrePlayerConnect(ValidateAuthTicketResponse_t ticket, ref bool isValid, ref string explanation)
        {
            SteamPending player = Provider.pending.FirstOrDefault(x => x.playerID.steamID.m_SteamID == ticket.m_SteamID.m_SteamID);
            if (player == default(SteamPending)) return;
            try
            {
                if (player.transportConnection.TryGetIPv4Address(out uint address))
                {
                    int duration = Data.DatabaseManager.IPBanCheck(player.playerID.steamID.m_SteamID, address, player.playerID.hwid);
                    if (duration != 0)
                    {
                        isValid = false;
                        explanation = $"You are IP banned on Uncreated Network for{(duration > 0 ? " another " + F.GetTimeFromMinutes((uint)duration, 0) : "ever")}, talk to the Directors in discord to appeal at: \"https://discord.gg/" + UCWarfare.Config.DiscordInviteCode + "\"";
                        return;
                    }
                }
                else
                {
                    isValid = false;
                    explanation = "Uncreated Network was unable to check your ban status, try again later or contact a Director if this keeps happening.";
                    return;
                }
                if (UCWarfare.Config.Debug)
                    F.Log(player.playerID.playerName, ConsoleColor.DarkGray);
                if (Data.OriginalNames.ContainsKey(player.playerID.steamID.m_SteamID))
                    Data.OriginalNames[player.playerID.steamID.m_SteamID] = new FPlayerName(player.playerID);
                else
                    Data.OriginalNames.Add(player.playerID.steamID.m_SteamID, new FPlayerName(player.playerID));
                ulong team = 0;
                if (PlayerManager.HasSave(player.playerID.steamID.m_SteamID, out PlayerSave save))
                {
                    team = save.Team;
                }
                string globalPrefix = "";
                string teamPrefix = "";

                // add team tags to global prefix
                if (team == 1) globalPrefix += $"{TeamManager.Team1Code.ToUpper()}-";
                else if (team == 2) globalPrefix += $"{TeamManager.Team2Code.ToUpper()}-";

                int xp = XPManager.GetXP(player.playerID.steamID.m_SteamID, true);
                // int stars = 0;
                // was not being used so i commented it
                Rank rank = null;
                /*
                if (OfficerManager.IsOfficer(player.playerID.steamID, out var officer))
                {
                    rank = OfficerManager.GetOfficerRank(officer.officerLevel);
                    int officerPoints = OfficerManager.GetOfficerPoints(player.playerID.steamID.m_SteamID, team, true).GetAwaiter().GetResult();
                    stars = OfficerManager.GetStars(officerPoints);
                }
                else
                {*/
                rank = XPManager.GetRank(xp, out _, out _);
                //}

                if (team == 1 || team == 2)
                {
                    globalPrefix += rank.abbreviation;
                    teamPrefix += rank.abbreviation;

                    globalPrefix += " ";
                    teamPrefix += " ";

                    player.playerID.characterName = globalPrefix + (player.playerID.characterName == string.Empty ? player.playerID.steamID.m_SteamID.ToString(Data.Locale) : player.playerID.characterName);
                    player.playerID.nickName = teamPrefix + (player.playerID.nickName == string.Empty ? player.playerID.steamID.m_SteamID.ToString(Data.Locale) : player.playerID.nickName);
                }
            }
            catch (Exception ex)
            {
                F.LogError($"Error accepting {player.playerID.playerName} in OnPrePlayerConnect:");
                F.LogError(ex);
                isValid = false;
                explanation = "Uncreated Network was unable to authenticate your connection, try again later or contact a Director if this keeps happening.";
            }
        }
    }
#pragma warning restore IDE0060 // Remove unused parameter
}
