using Rocket.API;
using Rocket.Unturned.Player;
using SDG.Unturned;
using Steamworks;
using System;
using System.Collections.Generic;
using System.Linq;
using Uncreated.Warfare.FOBs;
using Uncreated.Warfare.Gamemodes;
using Uncreated.Warfare.Gamemodes.Interfaces;
using Uncreated.Warfare.Kits;
using Uncreated.Warfare.Teams;
using Uncreated.Warfare.Vehicles;
using VehicleSpawn = Uncreated.Warfare.Vehicles.VehicleSpawn;

namespace Uncreated.Warfare.Commands
{
    public class RequestCommand : IRocketCommand
    {
        public AllowedCaller AllowedCaller => AllowedCaller.Player;
        public string Name => "request";
        public string Help => "Request a kit by looking at a sign or request a vehicle by looking at the vehicle, then do /request.";
        public string Syntax => "/request";
        public List<string> Aliases => new List<string>() { };
        public List<string> Permissions => new List<string>() { "uc.request" };
        public void Execute(IRocketPlayer caller, string[] command)
        {
            // dont allow requesting between game end and leaderboard
            if (Data.Gamemode.State != EState.ACTIVE && Data.Gamemode.State != EState.STAGING)
            {
                return;
            }
            UnturnedPlayer player = (UnturnedPlayer)caller;
            if (!Data.Is(out IKitRequests ctf))
            {
                player.Message("command_e_gamemode");
                return;
            }
            UCPlayer ucplayer = UCPlayer.FromIRocketPlayer(caller);
            if (player == null || ucplayer == null) return;
            if (ucplayer.Position == UnityEngine.Vector3.zero) return;
            ulong team = ucplayer.GetTeam();
            if (command.Length > 0)
            {
                if (command[0].ToLower() == "save")
                {
                    if (player.HasPermission("uc.request.save"))
                    {
                        InteractableSign sign = UCBarricadeManager.GetInteractableFromLook<InteractableSign>(player.Player.look);
                        if (sign == default) player.Message("request_not_looking");
                        else
                        {
                            if (RequestSigns.AddRequestSign(sign, out RequestSign signadded))
                            {
                                string teamcolor = TeamManager.NeutralColorHex;
                                if (KitManager.KitExists(signadded.kit_name, out Kit kit)) teamcolor = TeamManager.GetTeamHexColor(kit.Team);
                                player.Message("request_saved_sign", signadded.kit_name, teamcolor);
                            }
                            else player.Message("request_already_saved"); // sign already registered
                        }
                    }
                    else
                        player.Message("no_permissions");
                    return;
                }
                else if (command[0].ToLower() == "remove")
                {
                    if (player.HasPermission("uc.request.remove"))
                    {
                        InteractableSign sign = UCBarricadeManager.GetInteractableFromLook<InteractableSign>(player.Player.look);
                        if (sign == default) player.Message("request_not_looking");
                        else
                        {
                            if (RequestSigns.SignExists(sign, out RequestSign requestsign))
                            {
                                string teamcolor = TeamManager.NeutralColorHex;
                                if (KitManager.KitExists(requestsign.kit_name, out Kit kit)) teamcolor = TeamManager.GetTeamHexColor(kit.Team);
                                player.Message("request_removed_sign", requestsign.kit_name, teamcolor);
                                RequestSigns.RemoveRequestSign(requestsign);
                            }
                            else player.Message("request_already_removed");
                        }
                    }
                    else
                        player.Message("no_permissions");
                    return;
                }
            }
            InteractableVehicle vehicle = UCBarricadeManager.GetVehicleFromLook(player.Player.look);
            InteractableSign signlook = UCBarricadeManager.GetInteractableFromLook<InteractableSign>(player.Player.look);
            if (signlook != null)
            {
                if (!RequestSigns.SignExists(signlook, out RequestSign requestsign))
                {
                    if (!VehicleSigns.SignExists(signlook, out VehicleSign vbsign))
                    {
                        ucplayer.Message("request_kit_e_kitnoexist");
                        return;
                    }
                    if (vbsign.bay != default && vbsign.bay.HasLinkedVehicle(out InteractableVehicle veh))
                    {
                        if (Data.Is<IVehicles>(out _))
                        {
                            if (veh != default)
                                RequestVehicle(ucplayer, veh, team);
                        }
                        else
                        {
                            ucplayer.SendChat("command_e_gamemode");
                        }
                    }
                            return;
                }
                if (!(Data.Gamemode is IKitRequests))
                {
                    ucplayer.SendChat("command_e_gamemode");
                    return;
                }
                if (requestsign.kit_name.StartsWith("loadout_"))
                {
                    if (ushort.TryParse(requestsign.kit_name.Substring(8), out ushort loadoutNumber))
                    {
                        byte bteam = ucplayer.Player.GetTeamByte();
                        List<Kit> loadouts = KitManager.GetKitsWhere(k => k.IsLoadout && k.Team == team && k.AllowedUsers.Contains(ucplayer.Steam64)).ToList();

                        if (loadouts.Count != 0)
                        {
                            if (loadoutNumber > 0 && loadoutNumber <= loadouts.Count)
                            {
                                Kit kit = loadouts[loadoutNumber - 1];

                                if (kit.IsClassLimited(out int currentPlayers, out int allowedPlayers, player.GetTeam()))
                                {
                                    ucplayer.Message("request_kit_e_limited", currentPlayers.ToString(Data.Locale), allowedPlayers.ToString(Data.Locale));
                                    return;
                                }

                                GiveKit(ucplayer, kit);
                                Stats.StatsManager.ModifyKit(kit.Name, x => x.TimesRequested++, true);
                                Stats.StatsManager.ModifyStats(player.CSteamID.m_SteamID, s =>
                                {
                                    Stats.WarfareStats.KitData kitData = s.Kits.Find(k => k.KitID == kit.Name && k.Team == bteam);
                                    if (kitData == default)
                                    {
                                        kitData = new Stats.WarfareStats.KitData() { KitID = kit.Name, Team = bteam, TimesRequested = 1 };
                                        s.Kits.Add(kitData);
                                    }
                                    else
                                    {
                                        kitData.TimesRequested++;
                                    }
                                }, false);
                                return;
                            }
                        }
                    }
                    ucplayer.Message("request_loadout_e_notallowed");
                    return;
                }
                else
                {
                    if (!KitManager.KitExists(requestsign.kit_name, out Kit kit))
                    {
                        ucplayer.Message("request_kit_e_kitnoexist");
                    }
                    else if (ucplayer.KitName == kit.Name)
                    {
                        ucplayer.Message("request_kit_e_alreadyhaskit");
                    }
                    else if (kit.IsPremium && !kit.AllowedUsers.Contains(ucplayer.Steam64) && !UCWarfare.Config.OverrideKitRequirements)
                    {
                        ucplayer.Message("request_kit_e_notallowed");
                    }
                    else if (kit.IsLimited(out int currentPlayers, out int allowedPlayers, player.GetTeam()))
                    {
                        ucplayer.Message("request_kit_e_limited", currentPlayers.ToString(Data.Locale), allowedPlayers.ToString(Data.Locale));
                    }
                    else if (kit.Class == EClass.SQUADLEADER && !ucplayer.IsSquadLeader())
                    {
                        ucplayer.Message("request_kit_e_notsquadleader");
                    }
                    else if (Data.Gamemode.State == Gamemodes.EState.ACTIVE && CooldownManager.HasCooldown(ucplayer, ECooldownType.REQUEST_KIT, out Cooldown requestCooldown) && !ucplayer.OnDutyOrAdmin() && !UCWarfare.Config.OverrideKitRequirements && !(kit.Class == EClass.CREWMAN || kit.Class == EClass.PILOT))
                    {
                        player.Message("kit_e_cooldownglobal", requestCooldown.ToString());
                    }
                    else if (kit.IsPremium && CooldownManager.HasCooldown(ucplayer, ECooldownType.PREMIUM_KIT, out Cooldown premiumCooldown, kit.Name) && !ucplayer.OnDutyOrAdmin() && !UCWarfare.Config.OverrideKitRequirements)
                    {
                        player.Message("kit_e_cooldown", premiumCooldown.ToString());
                    }
                    else
                    {
                        if (ucplayer.Rank.Level < kit.RequiredLevel && !UCWarfare.Config.OverrideKitRequirements)
                        {
                            ucplayer.Message("request_kit_e_wronglevel", kit.RequiredLevel.ToString(Data.Locale));
                        }
                        else
                        {
                            GiveKit(ucplayer, kit);
                        }
                    }
                }
            }
            else if (vehicle != null)
            {
                if (Data.Gamemode is IVehicles)
                {
                    RequestVehicle(ucplayer, vehicle);
                }
                else
                {
                    ucplayer.SendChat("command_e_gamemode");
                    return;
                }
            }
            else
            {
                ucplayer.Message("request_not_looking");
            }
        }
        private void GiveKit(UCPlayer ucplayer, Kit kit)
        {
            bool branchChanged = false;
            if (KitManager.HasKit(ucplayer.CSteamID, out Kit oldkit) && kit.Branch != EBranch.DEFAULT && oldkit.Branch != kit.Branch)
                branchChanged = true;

            Command_ammo.WipeDroppedItems(ucplayer.Player.inventory);
            KitManager.GiveKit(ucplayer, kit);
            Stats.StatsManager.ModifyKit(kit.Name, k => k.TimesRequested++);
            KitManager.AddRequest(kit);
            ucplayer.Message("request_kit_given", kit.DisplayName.ToUpper());

            if (branchChanged)
            {
                ucplayer.Branch = kit.Branch;
                ucplayer.Message("branch_changed", Translation.TranslateBranch(kit.Branch, ucplayer).ToUpper());
            }

            if (kit.IsPremium)
            {
                CooldownManager.StartCooldown(ucplayer, ECooldownType.PREMIUM_KIT, kit.Cooldown, kit.Name);
            }
            CooldownManager.StartCooldown(ucplayer, ECooldownType.REQUEST_KIT, CooldownManager.config.Data.RequestKitCooldown);

            PlayerManager.ApplyToOnline();
        }
        private void RequestVehicle(UCPlayer ucplayer, InteractableVehicle vehicle) => RequestVehicle(ucplayer, vehicle, ucplayer.GetTeam());
        private void RequestVehicle(UCPlayer ucplayer, InteractableVehicle vehicle, ulong team)
        {
            if (!VehicleBay.VehicleExists(vehicle.asset.GUID, out VehicleData data))
            {
                ucplayer.Message("request_vehicle_e_notrequestable");
                return;
            }
            else if (vehicle.lockedOwner != CSteamID.Nil || vehicle.lockedGroup != CSteamID.Nil)
            {
                ucplayer.Message("request_vehicle_e_alreadyrequested");
                return;
            }
            else if (data.Team != team)
            {
                ucplayer.Message("request_vehicle_e_notinteam");
                return;
            }
            else if (data.RequiresSL && ucplayer.Squad == null)
            {
                ucplayer.Message("request_vehicle_e_notinsquad");
                return;
            }
            else if (!KitManager.HasKit(ucplayer.CSteamID, out Kit kit))
            {
                ucplayer.Message("request_vehicle_e_nokit");
                return;
            }
            else if (data.RequiredClass != EClass.NONE && kit.Class != data.RequiredClass)
            {
                Kit requiredKit = KitManager.GetKitsWhere(k => k.Class == data.RequiredClass).FirstOrDefault();
                string @class;
                if (requiredKit != null)
                    @class = requiredKit.DisplayName.ToUpper();
                else @class = data.RequiredClass.ToString().ToUpper();
                ucplayer.Message("request_vehicle_e_wrongkit", requiredKit != null ? requiredKit.DisplayName : data.RequiredClass.ToString().Replace('_', ' ').ToUpper());
                return;
            }
            else if (CooldownManager.HasCooldown(ucplayer, ECooldownType.REQUEST_VEHICLE, out Cooldown cooldown, vehicle.id))
            {
                ucplayer.Message("request_vehicle_e_cooldown", Translation.GetTimeFromSeconds(unchecked((uint)Math.Round(cooldown.Timeleft.TotalSeconds)), ucplayer.Steam64));
                return;
            }
            else
            {
                for (int i = 0; i < VehicleSpawner.ActiveObjects.Count; i++)
                {
                    VehicleSpawn spawn = VehicleSpawner.ActiveObjects[i];
                    if (spawn == null) continue;
                    if (spawn.HasLinkedVehicle(out InteractableVehicle veh))
                    {
                        if (veh == null || veh.isDead) continue;
                        if (veh.lockedOwner.m_SteamID == ucplayer.Steam64 &&
                            (veh.transform.position - vehicle.transform.position).sqrMagnitude < UCWarfare.Config.MaxVehicleAbandonmentDistance * UCWarfare.Config.MaxVehicleAbandonmentDistance)
                        {
                            ucplayer.Message("request_vehicle_e_already_owned");
                            return;
                        }
                    }
                }
            }

            double delay = (DateTime.Now - Tickets.TicketManager.TimeSinceMatchStart).TotalSeconds;
            double timeleft = data.Delay - delay;

            if (data.Delay > 0 && Data.Gamemode.State == Gamemodes.EState.STAGING)
            {
                ucplayer.Message("request_vehicle_e_staging", Translation.GetTimeFromSeconds(unchecked((uint)Math.Round(timeleft)), ucplayer.Steam64));
                return;
            }
            if (delay < data.Delay )
            {
                ucplayer.Message("request_vehicle_e_delay", Translation.GetTimeFromSeconds(unchecked((uint)Math.Round(timeleft)), ucplayer.Steam64));
                return;
            }
            if (ucplayer.Rank.Level < data.RequiredLevel)
            {
                ucplayer.Message("request_vehicle_e_wronglevel", data.RequiredLevel.ToString(Data.Locale));
                return;
            }
            else if (vehicle.asset != default && vehicle.asset.canBeLocked)
            {
                vehicle.tellLocked(ucplayer.CSteamID, ucplayer.Player.quests.groupID, true);

                VehicleManager.ServerSetVehicleLock(vehicle, ucplayer.CSteamID, ucplayer.Player.quests.groupID, true);

                VehicleBay.IncrementRequestCount(vehicle.asset.GUID, true);

                if (VehicleSpawner.HasLinkedSpawn(vehicle.instanceID, out VehicleSpawn spawn))
                {
                    if (vehicle.TryGetComponent(out SpawnedVehicleComponent c))
                    {
                        c.hasBeenRequested = true;
                        c.nextIdleSecond = data.RespawnTime - 10f;
                        c.StartIdleRespawnTimer();
                        c.isIdle = false;
                    }
                    spawn.UpdateSign();
                }
                vehicle.updateVehicle();
                vehicle.updatePhysics();

                EffectManager.sendEffect(8, EffectManager.SMALL, vehicle.transform.position);
                ucplayer.Message("request_vehicle_given", vehicle.asset.vehicleName, UCWarfare.GetColorHex("request_vehicle_given_vehicle_name"));

                if (!FOBManager.config.Data.Buildables.Exists(e => e.type == EbuildableType.EMPLACEMENT && e.structureID == vehicle.asset.GUID))
                {
                    ItemManager.dropItem(new Item(28, true), ucplayer.Position, true, true, true); // gas can
                    ItemManager.dropItem(new Item(277, true), ucplayer.Position, true, true, true); // car jack
                }
                foreach (Guid item in data.Items)
                {
                    if (Assets.find(item) is ItemAsset a)
                        ItemManager.dropItem(new Item(a.id, true), ucplayer.Position, true, true, true);
                }
                Stats.StatsManager.ModifyStats(ucplayer.Steam64, x => x.VehiclesRequested++, false);
                Stats.StatsManager.ModifyTeam(team, t => t.VehiclesRequested++, false);
                Stats.StatsManager.ModifyVehicle(vehicle.id, v => v.TimesRequested++);
            }
            else
            {
                ucplayer.Message("request_vehicle_e_alreadyrequested");
                return;
            }
        }
    }
}
