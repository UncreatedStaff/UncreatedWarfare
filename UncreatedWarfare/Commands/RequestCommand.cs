using Rocket.API;
using Rocket.Unturned.Player;
using SDG.Unturned;
using Steamworks;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Uncreated.Warfare.FOBs;
using Uncreated.Warfare.Teams;
using Uncreated.Warfare.Vehicles;
using Uncreated.Warfare.XP;
using Uncreated.Warfare.Kits;
using System.Threading;

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
        public async void Execute(IRocketPlayer caller, string[] command)
        {
            UnturnedPlayer player = (UnturnedPlayer)caller;
            UCPlayer ucplayer = UCPlayer.FromIRocketPlayer(caller);

            InteractableVehicle vehicle = UCBarricadeManager.GetVehicleFromLook(player.Player.look);
            InteractableSign signlook = UCBarricadeManager.GetInteractableFromLook<InteractableSign>(player.Player.look);

            if (command.Length > 0)
            {
                if(command[0].ToLower() == "save")
                {
                    if(player.HasPermission("uc.request.save"))
                    {
                        InteractableSign sign = UCBarricadeManager.GetInteractableFromLook<InteractableSign>(player.Player.look);
                        if (sign == default) player.Message("request_not_looking");
                        else
                        {
                            if (RequestSigns.AddRequestSign(sign, out RequestSign signadded))
                            {
                                string teamcolor = TeamManager.NeutralColorHex;
                                if (KitManager.KitExists(signadded.kit_name, out Kit kit)) teamcolor = F.GetTeamNumberColorHex(kit.Team);
                                player.Message("request_saved_sign", signadded.kit_name, teamcolor);
                            }
                        }
                    } else
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
                            if(RequestSigns.SignExists(sign, out RequestSign requestsign))
                            {
                                string teamcolor = TeamManager.NeutralColorHex;
                                if (KitManager.KitExists(requestsign.kit_name, out Kit kit)) teamcolor = F.GetTeamNumberColorHex(kit.Team);
                                    player.Message("request_removed_sign", requestsign.kit_name, teamcolor);
                                await RequestSigns.RemoveRequestSign(requestsign);
                            }
                            else player.Message("request_not_looking");
                        }
                    }
                    else
                        player.Message("no_permissions");
                    return;
                }
            }
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
                        if(veh != default)
                            await RequestVehicle(ucplayer, veh);
                    }
                }
                else if (!KitManager.KitExists(requestsign.kit_name, out Kit kit))
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
                else if (kit.Class == Kit.EClass.SQUADLEADER && !ucplayer.IsSquadLeader())
                {
                    ucplayer.Message("request_kit_e_notsquadleader");
                }
                else if (CooldownManager.HasCooldown(ucplayer, ECooldownType.REQUEST_KIT, out var requestCooldown) && !ucplayer.OnDutyOrAdmin() && !UCWarfare.Config.OverrideKitRequirements)
                {
                    player.Message("kit_e_cooldownglobal", requestCooldown.ToString());
                }
                else if (kit.IsPremium && CooldownManager.HasCooldown(ucplayer, ECooldownType.PREMIUM_KIT, out var premiumCooldown, kit.Name) && !ucplayer.OnDutyOrAdmin() && !UCWarfare.Config.OverrideKitRequirements)
                {
                    player.Message("kit_e_cooldown", premiumCooldown.ToString());
                }
                else
                {
                    int xp = await XPManager.GetXP(ucplayer.Player, ucplayer.GetTeam(), true);
                    Rank rank = XPManager.GetRank(xp, out _, out _);
                    if ((rank == default || rank.level < kit.RequiredLevel) && !UCWarfare.Config.OverrideKitRequirements)
                    {
                        ucplayer.Message("request_kit_e_wronglevel", kit.RequiredLevel.ToString(Data.Locale));
                    }
                    else
                    {
                        bool branchChanged = false;
                        if (KitManager.HasKit(player.CSteamID, out Kit oldkit) && kit.Branch != EBranch.DEFAULT && oldkit.Branch != kit.Branch)
                            branchChanged = true;

                        await KitManager.GiveKit(ucplayer, kit);
                        ucplayer.Message("request_kit_given", kit.DisplayName.ToUpper());

                        if (branchChanged)
                        {
                            ucplayer.Branch = kit.Branch;
                            ucplayer.Message("branch_changed", F.TranslateBranch(kit.Branch, ucplayer).ToUpper());
                        }

                        if (kit.IsPremium)
                        {
                            CooldownManager.StartCooldown(ucplayer, ECooldownType.PREMIUM_KIT, kit.Cooldown, kit.Name);
                        }
                        CooldownManager.StartCooldown(ucplayer, ECooldownType.REQUEST_KIT, CooldownManager.config.Data.RequestKitCooldown);

                        PlayerManager.Save();
                    }
                }
            }
            else if (vehicle != null)
            {
                await RequestVehicle(ucplayer, vehicle);
            }
            else
            {
                ucplayer.Message("request_not_looking");
            }
        }
        private async Task RequestVehicle(UCPlayer ucplayer, InteractableVehicle vehicle)
        {
            if (!VehicleBay.VehicleExists(vehicle.id, out VehicleData data))
            {
                ucplayer.Message("request_vehicle_e_notrequestable");
                return;
            }
            else if (vehicle.lockedOwner != CSteamID.Nil || vehicle.lockedGroup != CSteamID.Nil)
            {
                ucplayer.Message("request_vehicle_e_alreadyrequested");
                return;
            }
            else if (data.RequiresSL && ucplayer.Squad == null)
            {
                ucplayer.Message("request_vehicle_e_notinsquad");
                return;
            }
            else if (!KitManager.HasKit(ucplayer.CSteamID, out var kit))
            {
                ucplayer.Message("request_vehicle_e_nokit");
                return;
            }
            else if (data.RequiredClass != Kit.EClass.NONE && kit.Class != data.RequiredClass)
            {
                var requiredKit = KitManager.GetKitsWhere(k => k.Class == data.RequiredClass).FirstOrDefault();

                ucplayer.Message("request_vehicle_e_wrongkit", requiredKit != null ? requiredKit.DisplayName.ToUpper() : "UNKNOWN");
                return;
            }
            else if (CooldownManager.HasCooldown(ucplayer, ECooldownType.REQUEST_VEHICLE, out Cooldown cooldown, vehicle.id))
            {
                ucplayer.Message("request_vehicle_e_cooldown", F.GetTimeFromSeconds(unchecked((uint)Math.Round(cooldown.Timeleft.TotalSeconds)), ucplayer.Steam64));
                return;
            }
            int xp = await XPManager.GetXP(ucplayer.Player, ucplayer.GetTeam(), true);
            Rank rank = XPManager.GetRank(xp, out _, out _);
            SynchronizationContext rtn = await ThreadTool.SwitchToGameThread();
            if (rank == default || rank.level < data.RequiredLevel)
            {
                ucplayer.Message("request_vehicle_e_wronglevel", data.RequiredLevel.ToString(Data.Locale));
                return;
            }
            else if (vehicle.asset != default && vehicle.asset.canBeLocked)
            {
                vehicle.tellLocked(ucplayer.CSteamID, ucplayer.Player.quests.groupID, true);

                VehicleManager.ServerSetVehicleLock(vehicle, ucplayer.CSteamID, ucplayer.Player.quests.groupID, true);

                vehicle.updateVehicle();
                vehicle.updatePhysics();

                EffectManager.sendEffect(8, EffectManager.SMALL, vehicle.transform.position);
                ucplayer.Message("request_vehicle_given", vehicle.asset.vehicleName, UCWarfare.GetColorHex("request_vehicle_given_vehicle_name"));

                if (!FOBManager.config.Data.Emplacements.Exists(e => e.vehicleID == vehicle.id))
                {
                    ItemManager.dropItem(new Item(28, true), ucplayer.Position, true, true, true);
                    ItemManager.dropItem(new Item(277, true), ucplayer.Position, true, true, true);
                }
                
                foreach (ushort item in data.Items)
                {
                    ItemManager.dropItem(new Item(item, true), ucplayer.Position, true, true, true);
                }
            }
            else
            {
                ucplayer.Message("request_vehicle_e_alreadyrequested");
                return;
            }
            await rtn;
            return;
        }
    }
}
