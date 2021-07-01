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
                        if (sign == default) player.SendChat("request_not_looking", UCWarfare.GetColor("request_not_looking"));
                        else
                        {
                            if (RequestSigns.AddRequestSign(sign, out RequestSign signadded))
                            {
                                string teamcolor = TeamManager.NeutralColorHex;
                                if (KitManager.KitExists(signadded.kit_name, out Kit kit)) teamcolor = F.GetTeamNumberColorHex(kit.Team);
                                player.Player.SendChat("request_saved_sign", UCWarfare.GetColor("request_saved_sign"), signadded.kit_name, teamcolor);
                            }
                        }
                    } else
                        player.Player.SendChat("no_permissions", UCWarfare.GetColor("no_permissions"));
                    return;
                } 
                else if (command[0].ToLower() == "remove")
                {
                    if (player.HasPermission("uc.request.remove"))
                    {
                        InteractableSign sign = UCBarricadeManager.GetInteractableFromLook<InteractableSign>(player.Player.look);
                        if (sign == default) player.SendChat("request_not_looking", UCWarfare.GetColor("request_not_looking"));
                        else
                        {
                            if(RequestSigns.SignExists(sign, out RequestSign requestsign))
                            {
                                string teamcolor = TeamManager.NeutralColorHex;
                                if (KitManager.KitExists(requestsign.kit_name, out Kit kit)) teamcolor = F.GetTeamNumberColorHex(kit.Team);
                                player.Player.SendChat("request_removed_sign", UCWarfare.GetColor("request_removed_sign"), requestsign.kit_name, teamcolor);
                                await RequestSigns.RemoveRequestSign(requestsign);
                            }
                            else player.SendChat("request_not_looking", UCWarfare.GetColor("request_not_looking"));
                        }
                    }
                    else
                        player.Player.SendChat("no_permissions", UCWarfare.GetColor("no_permissions"));
                    return;
                }
            }
            if (signlook != null)
            {
                if (!RequestSigns.SignExists(signlook, out RequestSign requestsign))
                {
                    if (!VehicleSigns.SignExists(signlook, out VehicleSign vbsign))
                    {
                        ucplayer.SendChat("request_kit_e_kitnoexist", UCWarfare.GetColor("request_kit_e_kitnoexist"));
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
                    ucplayer.SendChat("request_kit_e_kitnoexist", UCWarfare.GetColor("request_kit_e_kitnoexist"));
                }
                else if (ucplayer.KitName == kit.Name)
                {
                    ucplayer.SendChat("request_kit_e_alreadyhaskit", UCWarfare.GetColor("request_kit_e_alreadyhaskit"));
                }
                else if (kit.IsPremium && !kit.AllowedUsers.Contains(ucplayer.Steam64))
                {
                    ucplayer.SendChat("request_kit_e_notallowed", UCWarfare.GetColor("request_kit_e_notallowed"));
                }
                else if (kit.IsLimited(out int currentPlayers, out int allowedPlayers))
                {
                    ucplayer.SendChat("request_kit_e_limited", UCWarfare.GetColor("request_kit_e_limited"), currentPlayers, allowedPlayers);
                }
                else if (CooldownManager.HasCooldown(ucplayer, ECooldownType.PREMIUM_KIT, out var cooldown, kit.Name))
                {
                    player.Message("kit_e_cooldown", cooldown.ToString()); return;
                }
                else
                {
                    int xp = await XPManager.GetXP(ucplayer.Player, ucplayer.GetTeam(), true); 
                    Rank rank = XPManager.GetRank(xp, out _, out _);
                    SynchronizationContext rtn = await ThreadTool.SwitchToGameThread();
                    if (rank == default || rank.level < kit.RequiredLevel)
                    {
                        ucplayer.SendChat("request_kit_e_wronglevel", UCWarfare.GetColor("request_kit_e_wronglevel"));
                    }
                    else if (kit.Branch != EBranch.DEFAULT && ucplayer.Branch != kit.Branch)
                    {
                        ucplayer.SendChat("request_kit_e_wrongbranch", UCWarfare.GetColor("request_kit_e_wrongbranch"));
                    }
                    else
                    {
                        KitManager.GiveKit(player, kit);
                        ucplayer.SendChat("request_kit_given", UCWarfare.GetColor("request_kit_given_free"), requestsign.kit_name);
                    }
                    await rtn;
                }
            }
            else if (vehicle != null)
            {
                await RequestVehicle(ucplayer, vehicle);
            }
            else
            {
                ucplayer.SendChat("request_not_looking", UCWarfare.GetColor("request_not_looking"));
            }
        }
        private async Task RequestVehicle(UCPlayer ucplayer, InteractableVehicle vehicle)
        {
            if (!VehicleBay.VehicleExists(vehicle.id, out VehicleData data))
            {
                ucplayer.SendChat("request_vehicle_e_notrequestable", UCWarfare.GetColor("request_vehicle_e_notrequestable"));
                return;
            }
            if (CooldownManager.HasCooldown(ucplayer, ECooldownType.REQUEST_VEHICLE, out Cooldown cooldown, vehicle.id))
            {
                ucplayer.SendChat("request_vehicle_e_cooldown", UCWarfare.GetColor("request_vehicle_e_cooldown"), F.GetTimeFromSeconds(unchecked((uint)Math.Round(cooldown.Timeleft.TotalSeconds))));
                return;
            }
            int xp = await XPManager.GetXP(ucplayer.Player, ucplayer.GetTeam(), true);
            Rank rank = XPManager.GetRank(xp, out _, out _);
            SynchronizationContext rtn = await ThreadTool.SwitchToGameThread();
            if (rank == default || rank.level < data.RequiredLevel)
            {
                ucplayer.SendChat("request_vehicle_e_wronglevel", UCWarfare.GetColor("request_vehicle_e_wronglevel"), rank.level);
            }
            else if (data.RequiredBranch != EBranch.DEFAULT && ucplayer.Branch != data.RequiredBranch)
            {
                ucplayer.SendChat("request_vehicle_e_wrongbranch", UCWarfare.GetColor("request_vehicle_e_wrongbranch"), data.RequiredBranch.ToString().ToLower(), UCWarfare.GetColorHex("request_vehicle_e_wrongbranch_branch"));
            }
            else if (vehicle.lockedOwner != CSteamID.Nil || vehicle.lockedGroup != CSteamID.Nil)
            {
                ucplayer.SendChat("request_vehicle_e_alreadyrequested", UCWarfare.GetColor("request_vehicle_e_alreadyrequested"));
            }
            else if (vehicle.asset != default && vehicle.asset.canBeLocked)
            {
                vehicle.tellLocked(ucplayer.CSteamID, ucplayer.Player.quests.groupID, true);

                VehicleManager.ServerSetVehicleLock(vehicle, ucplayer.CSteamID, ucplayer.Player.quests.groupID, true);

                vehicle.updateVehicle();
                vehicle.updatePhysics();

                EffectManager.sendEffect(8, EffectManager.SMALL, vehicle.transform.position);
                ucplayer.SendChat("request_vehicle_given", UCWarfare.GetColor("request_vehicle_given"), vehicle.asset.vehicleName, UCWarfare.GetColorHex("request_vehicle_given_vehicle_name"));
            } else
            {
                ucplayer.SendChat("request_vehicle_e_alreadyrequested", UCWarfare.GetColor("request_vehicle_e_alreadyrequested"));
            }
            await rtn;
            return;
        }
    }
}
