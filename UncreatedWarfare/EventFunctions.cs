using Rocket.Unturned.Enumerations;
using Rocket.Unturned.Player;
using SDG.Unturned;
using Steamworks;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Uncreated.Networking;
using Uncreated.Players;
using Uncreated.Warfare.Components;
using Uncreated.Warfare.FOBs;
using Uncreated.Warfare.Gamemodes.Flags.TeamCTF;
using Uncreated.Warfare.Kits;
using Uncreated.Warfare.Officers;
using Uncreated.Warfare.Squads;
using Uncreated.Warfare.Stats;
using Uncreated.Warfare.Teams;
using Uncreated.Warfare.Tickets;
using Uncreated.Warfare.XP;
using UnityEngine;
using Flag = Uncreated.Warfare.Gamemodes.Flags.Flag;
using Kit = Uncreated.Warfare.Kits.Kit;

#pragma warning disable IDE0060 // Remove unused parameter
namespace Uncreated.Warfare
{
    public static class EventFunctions
    {
        public delegate Task GroupChanged(SteamPlayer player, ulong oldGroup, ulong newGroup);
        public static event GroupChanged OnGroupChanged;
        internal static async Task OnGroupChangedInvoke(SteamPlayer player, ulong oldGroup, ulong newGroup) => await OnGroupChanged?.Invoke(player, oldGroup, newGroup);
        internal static async Task GroupChangedAction(SteamPlayer player, ulong oldGroup, ulong newGroup)
        {
            ulong oldteam = oldGroup.GetTeam();
            ulong newteam = newGroup.GetTeam();

            PlayerManager.VerifyTeam(player.player);
            await Data.Gamemode?.OnGroupChanged(player, oldGroup, newGroup, oldteam, newteam);


            SquadManager.ClearUIsquad(player.player);
            SquadManager.UpdateUIMemberCount(newGroup);
            SquadManager.OnGroupChanged(player, oldGroup, newGroup);
            TicketManager.OnGroupChanged(player, oldGroup, newGroup);
            FOBManager.UpdateUI(UCPlayer.FromSteamPlayer(player));

            await RequestSigns.InvokeLangUpdateForAllSigns(player);

            await XPManager.OnGroupChanged(player, oldGroup, newGroup);
            await OfficerManager.OnGroupChanged(player, oldGroup, newGroup);
        }
        internal static void OnStructureDestroyed(StructureRegion region, StructureData data, StructureDrop drop, uint instanceID)
        {
            Data.VehicleSpawner.OnStructureDestroyed(region, data, drop, instanceID);
        }
        internal static void OnBarricadeDestroyed(BarricadeRegion region, BarricadeData data, BarricadeDrop drop, uint instanceID, ushort plant, ushort index)
        {
            if (Data.OwnerComponents != null)
            {
                int c = Data.OwnerComponents.FindIndex(x => x != null && x.transform != default && data != default && x.transform.position == data.point);
                if (c != -1)
                {
                    UnityEngine.Object.Destroy(Data.OwnerComponents[c]);
                    Data.OwnerComponents.RemoveAt(c);
                }
            }
            FOBManager.OnBarricadeDestroyed(region, data, drop, instanceID, plant, index);
            RallyManager.OnBarricadeDestroyed(region, data, drop, instanceID, plant, index);
            RepairManager.OnBarricadeDestroyed(region, data, drop, instanceID, plant, index);
            Data.VehicleSpawner.OnBarricadeDestroyed(region, data, drop, instanceID, plant, index);
            Data.VehicleSigns.OnBarricadeDestroyed(region, data, drop, instanceID, plant, index);
        }
        internal static void StopCosmeticsToggleEvent(ref EVisualToggleType type, SteamPlayer player, ref bool allow)
        {
            if (!UCWarfare.Config.AllowCosmetics) allow = UnturnedPlayer.FromSteamPlayer(player).OnDuty();
        }
        internal static void StopCosmeticsSetStateEvent(ref EVisualToggleType type, SteamPlayer player, ref bool state, ref bool allow)
        {
            if (!UCWarfare.Config.AllowCosmetics && UnturnedPlayer.FromSteamPlayer(player).OffDuty()) state = false;
        }
        internal static void OnBarricadePlaced(BarricadeRegion region, BarricadeData data, ref Transform location)
        {
            F.Log("Placed barricade: " + data.barricade.asset.itemName + ", " + location.position.ToString(), ConsoleColor.DarkGray);
            BarricadeOwnerDataComponent c = location.gameObject.AddComponent<BarricadeOwnerDataComponent>();
            c.SetData(data, region, location);
            Data.OwnerComponents.Add(c);
            RallyManager.OnBarricadePlaced(region, data, ref location);
            RepairManager.OnBarricadePlaced(region, data, ref location);
        }
        internal static async void OnLandmineExploded(InteractableTrap trap, Collider collider, BarricadeOwnerDataComponent owner)
        {
            try
            {
                if (owner == default || owner.owner == default)
                {
                    if (owner == default || owner.ownerID == 0) return;
                    FPlayerName usernames = await Data.DatabaseManager.GetUsernames(owner.ownerID);
                    if (UCWarfare.Config.Debug)
                        F.Log(usernames.PlayerName + "'s landmine exploded", ConsoleColor.DarkGray);
                }
                else
                {
                    if (F.TryGetPlaytimeComponent(owner.owner.player, out PlaytimeComponent c))
                        c.LastLandmineExploded = new LandmineDataForPostAccess(trap, owner);
                    if (UCWarfare.Config.Debug)
                        F.Log(F.GetPlayerOriginalNames(owner.owner).PlayerName + "'s landmine exploded", ConsoleColor.DarkGray);
                }
            }
            catch (Exception ex)
            {
                F.LogError("Exception in LandmineExploded:");
                F.LogError(ex);
            }
        }
        internal static void ThrowableSpawned(UseableThrowable useable, GameObject throwable)
        {
            try
            {
                ThrowableOwnerDataComponent t = throwable.AddComponent<ThrowableOwnerDataComponent>();
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
            Patches.InternalPatches.lastProjected = projectile;
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
        internal static async Task ReloadCommand_onTranslationsReloaded()
        {
            foreach (SteamPlayer player in Provider.clients)
                await UCWarfare.I.UpdateLangs(player);
        }
        internal static void OnBarricadeTryPlaced(Barricade barricade, ItemBarricadeAsset asset, Transform hit, ref Vector3 point, ref float angle_x,
            ref float angle_y, ref float angle_z, ref ulong owner, ref ulong group, ref bool shouldAllow)
        {
            if (hit != null && hit.TryGetComponent<InteractableVehicle>(out _))
            {
                if (!UCWarfare.Config.AdminLoggerSettings.AllowedBarricadesOnVehicles.Contains(asset.id))
                {
                    UCPlayer player = UCPlayer.FromID(owner);
                    if (player != null && player.OffDuty())
                    {
                        shouldAllow = false;
                        player.SendChat("no_placement_on_vehicle", asset.itemName, asset.itemName.An());
                        return;
                    }
                }
            }
            if (shouldAllow)
                RallyManager.OnBarricadePlaceRequested(barricade, asset, hit, ref point, ref angle_x, ref angle_y, ref angle_z, ref owner, ref group, ref shouldAllow);
        }
        internal static void OnPostHealedPlayer(Player instigator, Player target)
        {
            Data.ReviveManager.ClearInjuredMarker(instigator.channel.owner.playerID.steamID.m_SteamID, instigator.GetTeam());
            Task.Run(async() => await Data.ReviveManager.OnPlayerHealedAsync(instigator, target));
        }
        internal static void OnPostPlayerConnected(UnturnedPlayer player)
        {
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
                if (PlayerManager.HasSave(player.CSteamID.m_SteamID, out PlayerSave save))
                {
                    if (save.LastGame != Data.Gamemode.GameID || save.ShouldRespawnOnJoin)
                    {
                        if (player.Player.life.isDead)
                            player.Player.life.ReceiveRespawnRequest(false);
                        else
                        {
                            player.Player.life.sendRevive();
                            player.Player.teleportToLocation(player.Player.GetBaseSpawn(out ulong t), t.GetBaseAngle());
                        }
                        save.ShouldRespawnOnJoin = false;
                        PlayerManager.Save();
                    }
                }
                Data.ReviveManager.DownedPlayers.Remove(player.CSteamID.m_SteamID);
                FPlayerName names = F.GetPlayerOriginalNames(player);
                UCPlayer ucplayer = UCPlayer.FromUnturnedPlayer(player);
                if (Data.PlaytimeComponents.ContainsKey(player.Player.channel.owner.playerID.steamID.m_SteamID))
                {
                    UnityEngine.Object.DestroyImmediate(Data.PlaytimeComponents[player.Player.channel.owner.playerID.steamID.m_SteamID]);
                    Data.PlaytimeComponents.Remove(player.Player.channel.owner.playerID.steamID.m_SteamID);
                }
                PlaytimeComponent pt = player.Player.transform.gameObject.AddComponent<PlaytimeComponent>();
                Task.Run(async () =>
                {
                    await pt.StartTracking(player.Player);
                    Data.PlaytimeComponents.Add(player.Player.channel.owner.playerID.steamID.m_SteamID, pt);
                    await OfficerManager.OnPlayerJoined(ucplayer);
                    await XPManager.OnPlayerJoined(ucplayer);
                    await Client.SendPlayerJoined(names);
                    await Data.DatabaseManager.CheckUpdateUsernames(names);
                    bool FIRST_TIME = !await Data.DatabaseManager.HasPlayerJoined(player.Player.channel.owner.playerID.steamID.m_SteamID);
                    await Data.DatabaseManager.RegisterLogin(player.Player);
                    await Data.Gamemode.OnPlayerJoined(player.Player.channel.owner);
                    ulong team = player.GetTeam();
                    ToastMessage.QueueMessage(player, F.Translate(FIRST_TIME ? "welcome_message_first_time" : "welcome_message", player,
                        UCWarfare.GetColorHex("uncreated"), names.CharacterName, TeamManager.GetTeamHexColor(team)), ToastMessageSeverity.INFO);
                    if ((ucplayer.KitName == null || ucplayer.KitName == string.Empty) && team > 0 && team < 3)
                    {
                        if (KitManager.KitExists(team == 1 ? TeamManager.Team1UnarmedKit : TeamManager.Team2UnarmedKit, out Kit unarmed))
                            await KitManager.GiveKit(ucplayer, unarmed);
                        else if (KitManager.KitExists(TeamManager.DefaultKit, out unarmed)) await KitManager.GiveKit(ucplayer, unarmed);
                        else F.LogWarning("Unable to give " + names.PlayerName + " a kit.");
                    }
                    pt.UCPlayerStats.LogIn(player.Player.channel.owner, names, WarfareStats.WarfareName);
                });
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
                    player.Player.skills.ServerSetSkillLevel((int)EPlayerSpeciality.OFFENSE, (int)EPlayerOffense.PARKOUR, 3);
                    player.Player.skills.ServerSetSkillLevel((int)EPlayerSpeciality.OFFENSE, (int)EPlayerOffense.EXERCISE, 4);
                    player.Player.skills.ServerSetSkillLevel((int)EPlayerSpeciality.OFFENSE, (int)EPlayerOffense.CARDIO, 5);
                    player.Player.skills.ServerSetSkillLevel((int)EPlayerSpeciality.DEFENSE, (int)EPlayerDefense.VITALITY, 5);
                }
                Data.ReviveManager.OnPlayerConnected(player);

                TicketManager.OnPlayerJoined(ucplayer);

            }
            catch (Exception ex)
            {
                F.LogError("Error in the main OnPostPlayerConnected:");
                F.LogError(ex);
            }
            
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
                Task.Run(async () =>
                {
                    await Data.DatabaseManager.AddBattleyeKick(id, reason);
                    await Client.LogPlayerBattleyeKicked(id, reason, DateTime.Now);
                });
            }
        }
        internal static void OnEnterStorage(CSteamID instigator, InteractableStorage storage, ref bool shouldAllow)
        {
            if (storage == null || !shouldAllow || UCWarfare.Config.LimitedStorages == null || UCWarfare.Config.LimitedStorages.Length == 0 || UCWarfare.Config.MaxTimeInStorages <= 0) return;
            SteamPlayer player = PlayerTool.getSteamPlayer(instigator);
            if (player == null || !BarricadeManager.tryGetInfo(storage.transform, out _, out _, out _, out ushort index, out BarricadeRegion region) || 
                region == null || !UCWarfare.Config.LimitedStorages.Contains(region.barricades[index].barricade.id)) return;
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
            if (shouldAllow && pendingTotalDamage > 0 && barricadeTransform.TryGetComponent(out BarricadeOwnerDataComponent t))
            {
                t.lastDamaged = instigatorSteamID.m_SteamID;
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
                CTFUI.RefreshStaticUI(player.GetTeam(), flag, true).SendToPlayer(ctf.Config.PlayerIcon, ctf.Config.UseUI, 
                    ctf.Config.CaptureUI, ctf.Config.ShowPointsOnUI, ctf.Config.ProgressChars, player.channel.owner, 
                    player.channel.owner.transportConnection);
            }
        }
        static Dictionary<ulong, long> lastSentMessages = new Dictionary<ulong, long>();
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

            if(shouldAllow)
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
            if (ucplayer.KitClass != Kit.EClass.NONE)
                overrideText = F.GetPlayerOriginalNames(player).NickName;
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
                if(sendNoSquadChat)
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
            Data.ReviveManager.OnPlayerDisconnected(player.Player.channel.owner);
            RemoveDamageMessageTicks(player.Player.channel.owner.playerID.steamID.m_SteamID);
            UCPlayer ucplayer = UCPlayer.FromUnturnedPlayer(player);
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
                Task.Run(
                    async () =>
                    {
                        if (gotptcomp)
                            c.UCPlayerStats.UpdateSession(WarfareStats.WarfareName);
                        Task s = Client.SendPlayerLeft(names);
                        await Data.Gamemode?.OnPlayerLeft(id);
                        await s;
                    });
                F.Broadcast("player_disconnected", names.CharacterName);
                if (UCWarfare.Config.RemoveLandminesOnDisconnect)
                {
                    IEnumerator<BarricadeOwnerDataComponent> ownedTraps = Data.OwnerComponents.Where(x => x != null && x.ownerID == player.CSteamID.m_SteamID
                   && x.barricade?.asset?.type == EItemType.TRAP).GetEnumerator();
                    while (ownedTraps.MoveNext())
                    {
                        BarricadeOwnerDataComponent comp = ownedTraps.Current;
                        if (comp == null) continue;
                        if (BarricadeManager.tryGetInfo(comp.barricadeTransform, out byte x, out byte y, out ushort plant, out ushort index, out BarricadeRegion region))
                        {
                            BarricadeManager.destroyBarricade(region, x, y, plant, index);
                            F.Log($"Removed {player.DisplayName}'s {comp.barricade.asset.itemName} at {x}, {y}", ConsoleColor.Green);
                        }
                        Data.OwnerComponents.Remove(comp);
                        UnityEngine.Object.Destroy(comp);
                    }
                    ownedTraps.Dispose();
                }
                if (gotptcomp)
                {
                    UnityEngine.Object.Destroy(c);
                    Data.PlaytimeComponents.Remove(player.CSteamID.m_SteamID);
                }
                //Data.ReviveManager.OnPlayerDisconnected(player); (now called from Provider event)
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
                    sign.InvokeUpdate().GetAwaiter().GetResult();
            }
            catch (Exception ex)
            {
                F.LogError("Failed to update kit sign for leaving player:");
                F.LogError(ex);
            }
        }
        internal static async Task LangCommand_OnPlayerChangedLanguage(UnturnedPlayer player, LanguageAliasSet oldSet, LanguageAliasSet newSet)
            => await UCWarfare.I.UpdateLangs(player.Player.channel.owner);
        internal static void OnPrePlayerConnect(ValidateAuthTicketResponse_t ticket, ref bool isValid, ref string explanation)
        {
            SteamPending player = Provider.pending.FirstOrDefault(x => x.playerID.steamID.m_SteamID == ticket.m_SteamID.m_SteamID);
            if (player == default(SteamPending)) return;
            F.Log(player.playerID.playerName);
            if (Data.OriginalNames.ContainsKey(player.playerID.steamID.m_SteamID))
                Data.OriginalNames[player.playerID.steamID.m_SteamID] = new FPlayerName(player.playerID);
            else
                Data.OriginalNames.Add(player.playerID.steamID.m_SteamID, new FPlayerName(player.playerID));
            ulong team = 0;
            if (PlayerManager.HasSave(player.playerID.steamID.m_SteamID, out var save))
            {
                team = save.Team;
            }
            string globalPrefix = "";
            string teamPrefix = "";

            // add team tags to global prefix
            if (TeamManager.IsTeam1(team)) globalPrefix += $"{TeamManager.Team1Code.ToUpper()}-";
            else if (TeamManager.IsTeam2(team)) globalPrefix += $"{TeamManager.Team2Code.ToUpper()}-";

            int xp = XPManager.GetXP(player.playerID.steamID.m_SteamID, team, true).GetAwaiter().GetResult();
            //int stars = 0;
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

            if (TeamManager.IsTeam1(team) || TeamManager.IsTeam2(team))
            {
                globalPrefix += rank.abbreviation;
                teamPrefix += rank.abbreviation;

                globalPrefix += " ";
                teamPrefix += " ";

                player.playerID.characterName = globalPrefix + (player.playerID.characterName == string.Empty ? player.playerID.steamID.m_SteamID.ToString(Data.Locale) : player.playerID.characterName);
                player.playerID.nickName = teamPrefix + (player.playerID.nickName == string.Empty ? player.playerID.steamID.m_SteamID.ToString(Data.Locale) : player.playerID.nickName);
            }
        }
    }
#pragma warning restore IDE0060 // Remove unused parameter
}
