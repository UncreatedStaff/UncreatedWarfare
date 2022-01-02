using Rocket.Unturned.Player;
using SDG.Unturned;
using Steamworks;
using System;
using System.Collections.Generic;
using System.Linq;
using Uncreated.Players;
using Uncreated.Warfare.Components;
using Uncreated.Warfare.FOBs;
using Uncreated.Warfare.Gamemodes;
using Uncreated.Warfare.Gamemodes.Flags.TeamCTF;
using Uncreated.Warfare.Gamemodes.Interfaces;
using Uncreated.Warfare.Kits;
using Uncreated.Warfare.Networking;
using Uncreated.Warfare.Point;
using Uncreated.Warfare.Squads;
using Uncreated.Warfare.Teams;
using Uncreated.Warfare.Tickets;
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
            ulong oldteam = oldGroup.GetTeam();
            ulong newteam = newGroup.GetTeam();
            UCPlayer ucplayer = UCPlayer.FromSteamPlayer(player);
            PlayerManager.VerifyTeam(player.player);
            Data.Gamemode?.OnGroupChanged(ucplayer, oldGroup, newGroup, oldteam, newteam);


            SquadManager.OnGroupChanged(player, oldGroup, newGroup);
            TicketManager.OnGroupChanged(player, oldGroup, newGroup);
            FOBManager.SendFOBList(ucplayer);

            RequestSigns.InvokeLangUpdateForAllSigns(player);

            Points.OnGroupChanged(ucplayer, oldGroup, newGroup);
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
        internal static void OnBarricadePlaced(BarricadeRegion region, BarricadeDrop drop)
        {
            SDG.Unturned.BarricadeData data = drop.GetServersideData();

            L.LogDebug($"{data.owner} Placed barricade: {data.barricade.asset.itemName}, {data.point}");
            BarricadeComponent owner = drop.model.gameObject.AddComponent<BarricadeComponent>();
            owner.Owner = data.owner;
            SteamPlayer player = PlayerTool.getSteamPlayer(data.owner);
            owner.Player = player?.player;
            owner.BarricadeGUID = data.barricade.asset.GUID;

            

            RallyManager.OnBarricadePlaced(drop, region);

            RepairManager.OnBarricadePlaced(drop, region);

            bool isFOBRadio = Gamemode.Config.Barricades.FOBRadioGUIDs.Any(g => g == data.barricade.asset.GUID);

            // FOB radio
            if (isFOBRadio)
            {
                if (!FOBManager.AllFOBs.Exists(f => f.Position == drop.model.position))
                    FOBManager.RegisterNewFOB(drop);
            }

            // ammo bag
            if (Gamemode.Config.Barricades.AmmoBagGUID == data.barricade.asset.GUID)
            {
                drop.model.gameObject.AddComponent<AmmoBagComponent>().Initialize(data, drop);
            }

            BuildableData buildable = FOBManager.config.Data.Buildables.Find(b => b.foundationID == drop.asset.GUID);
            if (buildable != null)
            {
                drop.model.gameObject.AddComponent<BuildableComponent>().Initialize(drop, buildable);
            }
            BuildableData repairable = FOBManager.config.Data.Buildables.Find(b => b.structureID == drop.asset.GUID || (b.type == EbuildableType.EMPLACEMENT && b.emplacementData.baseID == drop.asset.GUID));
            if (repairable != null || isFOBRadio)
            {
                drop.model.gameObject.AddComponent<RepairableComponent>();
            }
        }
        internal static void ThrowableSpawned(UseableThrowable useable, GameObject throwable)
        {
            try
            {
                ThrowableOwner t = throwable.AddComponent<ThrowableOwner>();
                PlaytimeComponent c = useable.player.GetPlaytimeComponent(out bool success);
                t.Set(useable, throwable, c);
                L.LogDebug(useable.player.name + " spawned a throwable: " + (useable.equippedThrowableAsset != null ?
                    useable.equippedThrowableAsset.itemName : useable.name), ConsoleColor.DarkGray);
                if (success)
                    c.thrown.Add(t);
            }
            catch (Exception ex)
            {
                L.LogError("Exception in ThrowableSpawned:");
                L.LogError(ex);
            }
        }
        internal static void ProjectileSpawned(UseableGun gun, GameObject projectile)
        {
            Patches.DeathsPatches.lastProjected = projectile;
            if (gun.player.TryGetPlaytimeComponent(out PlaytimeComponent c))
            {
                c.lastProjected = gun.equippedGunAsset.GUID;
            }
        }
        internal static void BulletSpawned(UseableGun gun, BulletInfo bullet)
        {
            PlaytimeComponent c = gun.player.GetPlaytimeComponent(out bool success);
            if (success)
            {
                c.lastShot = gun.equippedGunAsset.GUID;
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
                
                if (Gamemode.Config.Barricades.AmmoBagGUID == barricade.asset.GUID)
                {
                    if (player != null && player.OffDuty() && player.KitClass != EClass.RIFLEMAN)
                    {
                        shouldAllow = false;
                        player.SendChat("ammo_not_rifleman");
                        return;
                    }
                }
                if (Data.Gamemode.UseWhitelist)
                    Data.Gamemode.Whitelister.OnBarricadePlaceRequested(barricade, asset, hit, ref point, ref angle_x, ref angle_y, ref angle_z, ref owner, ref group, ref shouldAllow);
                if (!(shouldAllow && Data.Gamemode is TeamGamemode)) return;
                ulong team = group.GetTeam();
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

                if (Gamemode.Config.Barricades.FOBRadioGUIDs.Any(g => g ==  barricade.asset.GUID))
                {
                    shouldAllow = BuildableComponent.TryPlaceRadio(barricade, player, point);
                    return;
                }

                BuildableData buildable = FOBManager.config.Data.Buildables.Find(b => b.foundationID == barricade.asset.GUID);

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
            }
        }

        internal static void OnPreVehicleDamage(CSteamID instigatorSteamID, InteractableVehicle vehicle, ref ushort pendingTotalDamage, ref bool canRepair, ref bool shouldAllow, EDamageOrigin damageOrigin)
        {
            if (F.IsInMain(vehicle.transform.position))
            {
                shouldAllow = false;
                return;
            }
            if (shouldAllow)
            {
                if (!vehicle.TryGetComponent(out VehicleComponent c))
                {
                    c = vehicle.gameObject.AddComponent<VehicleComponent>();
                    c.owner = vehicle.lockedOwner;
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
                    else if (damageOrigin == EDamageOrigin.Vehicle_Bumper)
                    {
                        if (instigatorSteamID.TryGetPlaytimeComponent(out PlaytimeComponent c2))
                        {
                            c.item = c2.lastExplodedVehicle;
                        }
                    }
                }
                c.lastDamageOrigin = damageOrigin;
                c.lastDamager = instigatorSteamID.m_SteamID;
            }
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
                L.LogError("Error in the main OnPostPlayerConnected loading player into OnlinePlayers:");
                L.LogError(ex);
            }
            try
            {
                // reset the player to spawn if they have joined in a different game as they last played in.

                UCPlayer ucplayer = UCPlayer.FromUnturnedPlayer(player);

                bool g = Data.Is(out ITeams t);
                bool isNewGame = false;
                bool isNewPlayer = true;
                if (PlayerManager.HasSave(player.CSteamID.m_SteamID, out PlayerSave save))
                {
                    isNewPlayer = false;

                    if (save.LastGame != Data.Gamemode.GameID || save.ShouldRespawnOnJoin)
                    {
                        isNewGame = true;

                        save.ShouldRespawnOnJoin = false;

                        PlayerManager.ApplyToOnline();
                    }
                }

                if (player.Player.life.isDead)
                    player.Player.life.ReceiveRespawnRequest(false);
                else
                    player.Player.life.sendRevive();

                if (g && t.UseJoinUI)
                {
                    t.JoinManager.OnPlayerConnected(ucplayer, isNewPlayer, isNewGame);
                }

                FPlayerName names = F.GetPlayerOriginalNames(player);
                if (Data.PlaytimeComponents.ContainsKey(player.Player.channel.owner.playerID.steamID.m_SteamID))
                {
                    UnityEngine.Object.DestroyImmediate(Data.PlaytimeComponents[player.Player.channel.owner.playerID.steamID.m_SteamID]);
                    Data.PlaytimeComponents.Remove(player.Player.channel.owner.playerID.steamID.m_SteamID);
                }
                PlaytimeComponent pt = player.Player.transform.gameObject.AddComponent<PlaytimeComponent>();
                pt.StartTracking(player.Player);
                Data.PlaytimeComponents.Add(player.Player.channel.owner.playerID.steamID.m_SteamID, pt);
                Points.OnPlayerJoined(ucplayer, isNewGame);
                Data.DatabaseManager.CheckUpdateUsernames(names);
                bool FIRST_TIME = !Data.DatabaseManager.HasPlayerJoined(player.Player.channel.owner.playerID.steamID.m_SteamID);
                Data.DatabaseManager.RegisterLogin(player.Player);

                Data.Gamemode.OnPlayerJoined(ucplayer, false);
                for (int i = 0; i < Vehicles.VehicleSpawner.ActiveObjects.Count; i++)
                {
                    Vehicles.VehicleSpawner.ActiveObjects[i].UpdateSign(player.Player.channel.owner);
                }
                if (Data.Gamemode is ITeams)
                {
                    ulong team = player.GetTeam();
                    ToastMessage.QueueMessage(player, new ToastMessage(Translation.Translate(FIRST_TIME ? "welcome_message_first_time" : "welcome_message", player,
                        UCWarfare.GetColorHex("uncreated"), names.CharacterName, TeamManager.GetTeamHexColor(team)), EToastMessageSeverity.INFO));
                } else
                {
                    ToastMessage.QueueMessage(player, new ToastMessage(Translation.Translate(FIRST_TIME ? "welcome_message_first_time" : "welcome_message", player,
                        UCWarfare.GetColorHex("uncreated"), names.CharacterName, UCWarfare.GetColorHex("neutral")), EToastMessageSeverity.INFO));
                }
                Chat.Broadcast("player_connected", names.CharacterName);
                Data.Reporter.OnPlayerJoin(player.Player.channel.owner);
                Invocations.Shared.PlayerJoined.NetInvoke(new FPlayerList
                {
                    Duty = ucplayer.OnDuty(),
                    Name = names.CharacterName,
                    Steam64 = ucplayer.Steam64,
                    Team = player.GetTeamByte()
                });
            }
            catch (Exception ex)
            {
                L.LogError("Error in the main OnPostPlayerConnected:");
                L.LogError(ex);
            }
        }
        internal static void OnRelayVoice(PlayerVoice speaker, bool wantsToUseWalkieTalkie, ref bool shouldAllow,
            ref bool shouldBroadcastOverRadio, ref PlayerVoice.RelayVoiceCullingHandler cullingHandler)
        {
            if (!UCWarfare.Config.RelayMicsDuringEndScreen || Data.Gamemode == null || Data.Gamemode.State == EState.ACTIVE || Data.Gamemode.State == EState.STAGING) return;
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
            if (storage == null || !shouldAllow || Gamemode.Config.Barricades.TimeLimitedStorages == null || Gamemode.Config.Barricades.TimeLimitedStorages.Length == 0 || UCWarfare.Config.MaxTimeInStorages <= 0) return;
            SteamPlayer player = PlayerTool.getSteamPlayer(instigator);
            BarricadeDrop storagedrop = BarricadeManager.FindBarricadeByRootTransform(storage.transform);
            if (player == null || storagedrop == null ||
                !Gamemode.Config.Barricades.TimeLimitedStorages.Contains(storagedrop.asset.GUID)) return;
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
            if (Data.Gamemode is TeamGamemode && TeamManager.IsInAnyMainOrAMCOrLobby(barricadeTransform.position))
            {
                shouldAllow = false;
            }
            else
            {
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

            if (shouldAllow && pendingTotalDamage > 0 && barricadeTransform.TryGetComponent(out BarricadeComponent c2))
            {
                c2.LastDamager = instigatorSteamID.m_SteamID;
            }
        }
        internal static void OnStructureDamaged(CSteamID instigatorSteamID, Transform structureTransform, ref ushort pendingTotalDamage, ref bool shouldAllow, EDamageOrigin damageOrigin)
        {
            if (Data.Gamemode is TeamGamemode && TeamManager.IsInAnyMainOrAMCOrLobby(structureTransform.position))
            {
                shouldAllow = false;
            }
            else
            {
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
                    else if(pl.player.equipment.asset != null)
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
            if (shouldAllow)
            {
                if (vehicle.transform.TryGetComponent(out VehicleComponent component))
                {
                    component.OnPlayerEnteredVehicle(player, vehicle);
                }
            }

            if (Data.Is<IFlagRotation>(out _) && player.IsOnFlag(out Flag flag))
            {
                SendUIParameters p = CTFUI.RefreshStaticUI(player.GetTeam(), flag, true);
                if (p.status != EFlagStatus.BLANK && p.status != EFlagStatus.DONT_DISPLAY)
                    p.SendToPlayer(player.channel.owner);
            }
#if false
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
#endif
        }
        static readonly Dictionary<ulong, long> lastSentMessages = new Dictionary<ulong, long>();
        internal static void RemoveDamageMessageTicks(ulong player)
        {
            lastSentMessages.Remove(player);
        }
        internal static void OnPlayerDamageRequested(ref DamagePlayerParameters parameters, ref bool shouldAllow)
        {
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

            if (shouldAllow && Data.Is(out IRevives rev))
                rev.ReviveManager.OnPlayerDamagedRequested(ref parameters, ref shouldAllow);
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
            if (!(Assets.find(EAssetType.ITEM, jar.item.id) is ItemAsset asset)) return;
            if (!Whitelister.IsWhitelisted(asset.GUID, out _))
            {
                allow = false;
                player.SendChat("cant_store_this_item", asset.itemName);
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
            UCBarricadeManager.GetBarricadeFromInstID(instanceID, out BarricadeDrop drop);
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
                if (vehicle.transform.TryGetComponent(out VehicleComponent component))
                {
                    component.OnPlayerExitedVehicle(player, vehicle);
                }

                Vehicles.VehicleSpawner.OnPlayerLeaveVehicle(player, vehicle);
            }
        }
        internal static void OnVehicleSwapSeatRequested(Player player, InteractableVehicle vehicle, ref bool shouldAllow, byte fromSeatIndex, ref byte toSeatIndex)
        {
            if (shouldAllow)
            {
                if (vehicle.transform.TryGetComponent(out VehicleComponent component))
                {
                    component.OnPlayerSwapSeatRequested(player, vehicle, toSeatIndex);
                }

                Vehicles.VehicleSpawner.OnPlayerLeaveVehicle(player, vehicle);
            }
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
            droppeditems.Remove(player.Player.channel.owner.playerID.steamID.m_SteamID);
            TeamManager.PlayerBaseStatus.Remove(player.Player.channel.owner.playerID.steamID.m_SteamID);
            RemoveDamageMessageTicks(player.Player.channel.owner.playerID.steamID.m_SteamID);
            UCPlayer ucplayer = UCPlayer.FromUnturnedPlayer(player);
            string kit = string.Empty;
            if (ucplayer != null)
            {
                if (Data.Is(out ITeams gm) && gm.UseJoinUI)
                    gm.JoinManager.OnPlayerDisconnected(ucplayer);
                if (Data.Is<IFOBs>(out _)) FOBManager.OnPlayerDisconnect(ucplayer);
                kit = ucplayer.KitName;
            }
            try
            {
                Data.Gamemode.OnPlayerLeft(ucplayer);
                FPlayerName names = F.GetPlayerOriginalNames(player.Player.channel.owner);
                if (player.OnDuty())
                {
                    if (player.IsAdmin())
                        Commands.DutyCommand.AdminOnToOff(player, names);
                    else if (player.IsIntern())
                        Commands.DutyCommand.InternOnToOff(player, names);
                }
                PlaytimeComponent c = player.CSteamID.GetPlaytimeComponent(out bool gotptcomp);
                Data.OriginalNames.Remove(player.Player.channel.owner.playerID.steamID.m_SteamID);
                ulong id = player.Player.channel.owner.playerID.steamID.m_SteamID;
                Chat.Broadcast("player_disconnected", names.CharacterName);
                if (gotptcomp)
                {
                    UnityEngine.Object.Destroy(c);
                    Data.PlaytimeComponents.Remove(player.CSteamID.m_SteamID);
                }
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
                L.LogError("Failed to update kit sign for leaving player:");
                L.LogError(ex);
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
                        explanation = $"You are IP banned on Uncreated Network for{(duration > 0 ? " another " + ((uint)duration).GetTimeFromMinutes(0) : "ever")}, talk to the Directors in discord to appeal at: \"https://discord.gg/" + UCWarfare.Config.DiscordInviteCode + "\"";
                        return;
                    }
                }
                else
                {
                    isValid = false;
                    explanation = "Uncreated Network was unable to check your ban status, try again later or contact a Director if this keeps happening.";
                    return;
                }
                L.LogDebug(player.playerID.playerName, ConsoleColor.DarkGray);
                if (Data.OriginalNames.ContainsKey(player.playerID.steamID.m_SteamID))
                    Data.OriginalNames[player.playerID.steamID.m_SteamID] = new FPlayerName(player.playerID);
                else
                    Data.OriginalNames.Add(player.playerID.steamID.m_SteamID, new FPlayerName(player.playerID));

                bool kick = false;
                string cn = null;
                string nn = null;
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
            if (Data.Is(out IVehicles v))
                v.VehicleSpawner.OnStructureDestroyed(data, drop, instanceID);
            if (drop.model.TryGetComponent(out BarricadeComponent c))
            {
                SteamPlayer damager = PlayerTool.getSteamPlayer(c.LastDamager);
                if (damager != null && data.group.GetTeam() == damager.GetTeam())
                {
                    Data.Reporter.OnDestroyedStructure(c.LastDamager, instanceID);
                }
            }
        }
        internal static void OnBarricadeDestroyed(SDG.Unturned.BarricadeData data, BarricadeDrop drop, uint instanceID, ushort plant)
        {
            if (Data.Is<IFOBs>(out _))
            {
                FOBManager.OnBarricadeDestroyed(data, drop, instanceID, plant);
                RepairManager.OnBarricadeDestroyed(data, drop, instanceID, plant);
            }

            if (drop.model.TryGetComponent(out BuildableComponent buildable))
                buildable.Destroy();
            if (drop.model.TryGetComponent(out BuildableComponent repairable))
                repairable.Destroy();

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
                r.ReviveManager.ClearInjuredMarker(instigator.channel.owner.playerID.steamID.m_SteamID, instigator.GetTeam());
                r.ReviveManager.OnPlayerHealed(instigator, target);
            }
        }
        internal static void OnPluginKeyPressed(Player player, uint simulation, byte key, bool state)
        {
            if (state == false || key != 2 || player == null) return;

            if (Data.Is(out IRevives r))
                r.ReviveManager.GiveUp(player);
        }
    }
#pragma warning restore IDE0060 // Remove unused parameter
}
