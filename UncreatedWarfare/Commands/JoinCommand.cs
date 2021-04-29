using Rocket.API;
using Rocket.Unturned.Player;
using SDG.Unturned;
using Steamworks;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace UncreatedWarfare.Commands
{
    public class JoinCommand : IRocketCommand
    {
        public AllowedCaller AllowedCaller => AllowedCaller.Player;
        public string Name => "join";
        public string Help => "Join US or Russia";
        public string Syntax => "/join <us|ru>";
        public List<string> Aliases => new List<string>();
        public List<string> Permissions => new List<string>() { "uc.join" };
        public void Execute(IRocketPlayer caller, string[] command)
        {
            SteamPlayer player = ((UnturnedPlayer)caller).Player.channel.owner;

            // TODO
            if(command.Length == 1)
            {
                if(command[0].ToLower() == "us" || command[0].ToLower() == "usa" || command[0].ToLower() == "ru" || command[0].ToLower() == "russia")
                {
                    if (UCWarfare.I.TeamManager.LobbyZone.IsInside(player.player.transform.position))
                    {
                        if(command[0].ToLower() == "us" || command[0].ToLower() == "usa")
                        {
                            GroupInfo group = GroupManager.getGroupInfo(new CSteamID(UCWarfare.I.TeamManager.T1.ID));
                            if(group == null)
                            {
                                player.SendChat("join_group_not_found", UCWarfare.I.Colors["join_group_not_found"],
                                    UCWarfare.I.TeamManager.T1.LocalizedName, UCWarfare.I.ColorsHex["team_1_color"],
                                    UCWarfare.I.TeamManager.T1.ID.ToString(), UCWarfare.I.ColorsHex["join_group_not_found_group_id"]);
                                return;
                            }
                            UCWarfare.I.KitManager.ClearInventory(player);
                            if(!group.hasSpaceForMoreMembersInGroup)
                            {
                                player.SendChat("join_group_has_no_space", UCWarfare.I.Colors["join_group_has_no_space"],
                                    UCWarfare.I.TeamManager.T1.LocalizedName, UCWarfare.I.ColorsHex["team_1_color"]);
                                return;
                            }
                            if(!UCWarfare.I.TeamManager.CanAddToTeam1())
                            {
                                player.SendChat("join_auto_balance_cant_switch", UCWarfare.I.Colors["join_auto_balance_cant_switch"],
                                    UCWarfare.I.TeamManager.T1.LocalizedName, UCWarfare.I.ColorsHex["team_1_color"],
                                    "us", UCWarfare.I.ColorsHex["join_auto_balance_cant_switch_queue_command"]);
                                return;
                            }
                        } else if (command[0].ToLower() == "ru" || command[0].ToLower() == "russia")
                        {

                        }
                    } else
                    {
                        player.SendChat("join_not_in_lobby", UCWarfare.I.Colors["join_not_in_lobby"], UCWarfare.I.ColorsHex["join_not_in_lobby_command"]);
                    }
                } else
                {
                    player.SendChat("join_command_no_args_provided", UCWarfare.I.Colors["join_command_no_args_provided"],
                        "us", UCWarfare.I.ColorsHex["team_1_color"], "ru", UCWarfare.I.ColorsHex["team_2_color"]);
                }
            }
        }
    }
}