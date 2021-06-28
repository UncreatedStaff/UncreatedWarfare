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

namespace Uncreated.Warfare.Kits
{
    public class Command_blank : IRocketCommand
    {
        public AllowedCaller AllowedCaller => AllowedCaller.Player;
        public string Name => "request";
        public string Help => "Request a kit by looking at a sign and doing /request.";
        public string Syntax => "/request";
        public List<string> Aliases => new List<string>() { };
        public List<string> Permissions => new List<string>() { "uc.request" };
        public async void Execute(IRocketPlayer caller, string[] command)
        {
            UnturnedPlayer player = (UnturnedPlayer)caller;
            UCPlayer ucplayer = UCPlayer.FromIRocketPlayer(caller);

            var vehicle = UCBarricadeManager.GetVehicleFromLook(player.Player.look);
            var signlook = UCBarricadeManager.GetInteractableFromLook<InteractableSign>(player.Player.look);

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
                                RequestSigns.RemoveRequestSign(requestsign);
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
                    ucplayer.Message("request_kit_e_signnoexist");
                    return;
                }
                if (!KitManager.KitExists(requestsign.kit_name, out Kit kit))
                {
                    ucplayer.Message("request_kit_e_kitnoexist");
                    return;
                }
                if (ucplayer.KitName == kit.Name)
                {
                    ucplayer.Message("request_kit_e_alreadyhaskit");
                    return;
                }
                if (kit.IsPremium && !kit.AllowedUsers.Contains(ucplayer.Steam64))
                {
                    ucplayer.Message("request_kit_e_notallowed");
                    return;
                }
                if (kit.IsLimited(out int currentPlayers, out int allowedPlayers))
                {
                    ucplayer.Message("request_kit_e_limited", currentPlayers, allowedPlayers);
                    return;
                }

                var xp = await XPManager.GetXP(ucplayer.Player, ucplayer.GetTeam());
                var rank = XPManager.GetRank(xp, out _, out _);

                if (rank?.level < kit.RequiredLevel)
                {
                    ucplayer.Message("request_kit_e_wronglevel");
                    return;
                }
                if (kit.Branch != EBranch.DEFAULT && ucplayer.Branch != kit.Branch)
                {
                    ucplayer.Message("request_kit_e_wrongbranch");
                    return;
                }

                ucplayer.Message("request_kit_given", requestsign.kit_name);
                KitManager.GiveKit(player, kit);
                return;
            }
            else if (vehicle != null)
            {
                if (!VehicleBay.VehicleExists(vehicle.id, out var data))
                {
                    ucplayer.Message("request_vehicle_e_notrequestable");
                    return;
                }
                if (CooldownManager.HasCooldown(ucplayer, ECooldownType.REQUEST_VEHICLE, out var cooldown, vehicle.id))
                {
                    ucplayer.Message("request_vehicle_e_cooldown", cooldown.Timeleft.ToString());
                    return;
                }
                var xp = await XPManager.GetXP(ucplayer.Player, ucplayer.GetTeam());
                var rank = XPManager.GetRank(xp, out _, out _);

                if (rank?.level < data.RequiredLevel)
                {
                    ucplayer.Message("request_vehicle_e_wronglevel", rank.level);
                    return;
                }
                if (data.RequiredBranch != EBranch.DEFAULT && ucplayer.Branch != data.RequiredBranch)
                {
                    ucplayer.Message("request_vehicle_e_wrongbranch", data.RequiredBranch);
                    return;
                }
                if (!(vehicle.lockedOwner == CSteamID.Nil && vehicle.lockedGroup == CSteamID.Nil))
                {
                    ucplayer.Message("request_vehicle_e_alreadyrequested");
                    return;
                }
                if (vehicle.asset.canBeLocked)
                {
                    vehicle.tellLocked(player.CSteamID, player.Player.quests.groupID, true);

                    VehicleManager.ServerSetVehicleLock(vehicle, player.CSteamID, player.Player.quests.groupID, true);

                    vehicle.updateVehicle();
                    vehicle.updatePhysics();

                    EffectManager.sendEffect((ushort)8, EffectManager.SMALL, vehicle.transform.position);
                }

                ucplayer.Message("request_vehicle_given", vehicle.asset.vehicleName);
                return;
            }
            else
            {
                ucplayer.Message("request_e_notlooking");
            }
        }
    }
}
