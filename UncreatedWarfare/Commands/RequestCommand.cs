using Rocket.API;
using Rocket.Unturned.Player;
using SDG.Unturned;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Uncreated.Warfare.FOBs;
using Uncreated.Warfare.Teams;

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
        public void Execute(IRocketPlayer caller, string[] command)
        {
            UnturnedPlayer player = (UnturnedPlayer)caller;
            if(command.Length > 0)
            {
                if(command[0].ToLower() == "save")
                {
                    if(player.HasPermission("uc.request.save"))
                    {
                        InteractableSign sign = BuildManager.GetInteractableFromLook<InteractableSign>(player.Player.look);
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
                } else if (command[0].ToLower() == "remove")
                {
                    if (player.HasPermission("uc.request.remove"))
                    {
                        InteractableSign sign = BuildManager.GetInteractableFromLook<InteractableSign>(player.Player.look);
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
            InteractableSign signlook = BuildManager.GetInteractableFromLook<InteractableSign>(player.Player.look);
            if (signlook == default) player.SendChat("request_not_looking", UCWarfare.GetColor("request_not_looking"));
            else
            {
                if (RequestSigns.SignExists(signlook, out RequestSign requestsign))
                {
                    string teamcolor = TeamManager.NeutralColorHex;
                    if (KitManager.KitExists(requestsign.kit_name, out Kit kit)) teamcolor = F.GetTeamNumberColorHex(kit.Team);

                    if(kit.Cost == 0 && !kit.IsPremium)
                    {
                        player.Player.SendChat("request_kit_given_free", UCWarfare.GetColor("request_kit_given_free"), requestsign.kit_name, teamcolor);
                        KitManager.GiveKit(player, kit);
                    } else if (kit.Cost > 0 && !kit.IsPremium)
                    {
                        uint credits = player.Player.GetCredits();
                        if(credits >= kit.Cost)
                        {
                            player.Player.SendChat("request_kit_given_credits", UCWarfare.GetColor("request_kit_given_credits"), requestsign.kit_name, teamcolor,
                                kit.Cost.ToString(), UCWarfare.GetColorHex("request_kit_given_credits_credits"));
                            KitManager.GiveKit(player, kit);
                            player.Player.ChangeCredits(-kit.Cost);
                        } else
                        {
                            player.Player.SendChat("request_kit_given_credits_cant_afford", UCWarfare.GetColor("request_kit_given_credits_cant_afford"), requestsign.kit_name, teamcolor,
                                kit.Cost.ToString(), UCWarfare.GetColorHex("request_kit_given_credits_cant_afford_credits"));
                        }
                    } else if (kit.IsPremium)
                    {
                        if(kit.AllowedUsers.Contains(player.CSteamID.m_SteamID))
                        {
                            if (kit.Cost == 0)
                            {
                                player.Player.SendChat("request_kit_given_free", UCWarfare.GetColor("request_kit_given_free"), requestsign.kit_name, teamcolor);
                                KitManager.GiveKit(player, kit);
                            }
                            else if (kit.Cost > 0)
                            {
                                uint credits = player.Player.GetCredits();
                                if (credits >= kit.Cost)
                                {
                                    player.Player.SendChat("request_kit_given_credits", UCWarfare.GetColor("request_kit_given_credits"), requestsign.kit_name, teamcolor,
                                        kit.Cost.ToString(), UCWarfare.GetColorHex("request_kit_given_credits_credits"));
                                    KitManager.GiveKit(player, kit);
                                    player.Player.ChangeCredits(-kit.Cost);
                                }
                                else
                                {
                                    player.Player.SendChat("request_kit_given_credits_cant_afford", UCWarfare.GetColor("request_kit_given_credits_cant_afford"), requestsign.kit_name, teamcolor,
                                        kit.Cost.ToString(), UCWarfare.GetColorHex("request_kit_given_credits_cant_afford_credits"));
                                }
                            }
                        } else
                        {
                            player.Player.SendChat("request_kit_given_not_owned", UCWarfare.GetColor("request_kit_given_not_owned"), requestsign.kit_name, teamcolor);
                        }
                    }
                } else player.SendChat("request_not_looking", UCWarfare.GetColor("request_not_looking"));
            }
        }
    }
}
