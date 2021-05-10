using Rocket.API;
using Rocket.Unturned.Player;
using SDG.Unturned;
using Steamworks;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using UncreatedWarfare.Teams;

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
            UnturnedPlayer player = (UnturnedPlayer)caller;
            SteamPlayer steamplayer = player.Player.channel.owner;

            // TODO
            if(command.Length == 1)
            {
                if(command[0].ToLower() == "us" || command[0].ToLower() == "usa" || command[0].ToLower() == "ru" || command[0].ToLower() == "russia")
                {
                    if (UCWarfare.I.TeamManager.LobbyZone.IsInside(steamplayer.player.transform.position))
                    {
                        if(command[0].ToLower() == "us" || command[0].ToLower() == "usa")
                        {
                            GroupInfo group = GroupManager.getGroupInfo(new CSteamID(UCWarfare.I.TeamManager.Team1.GroupID));
                            if(group == null)
                            {
                                steamplayer.SendChat("join_group_not_found", UCWarfare.I.Colors["join_group_not_found"],
                                    UCWarfare.I.TeamManager.Team1.LocalizedName, UCWarfare.I.ColorsHex["team_1_color"],
                                    UCWarfare.I.TeamManager.Team1.GroupID.ToString(), UCWarfare.I.ColorsHex["join_group_not_found_group_id"]);
                                return;
                            }
                            Kits.UCInventoryManager.ClearInventory(player);
                            if(!group.hasSpaceForMoreMembersInGroup)
                            {
                                steamplayer.SendChat("join_group_has_no_space", UCWarfare.I.Colors["join_group_has_no_space"],
                                    UCWarfare.I.TeamManager.Team1.LocalizedName, UCWarfare.I.ColorsHex["team_1_color"]);
                                return;
                            }
                            if(!UCWarfare.I.TeamManager.CanJoinTeam(ETeam.TEAM1))
                            {
                                steamplayer.SendChat("join_auto_balance_cant_switch", UCWarfare.I.Colors["join_auto_balance_cant_switch"],
                                    UCWarfare.I.TeamManager.Team1.LocalizedName, UCWarfare.I.ColorsHex["team_1_color"]);
                                return;
                            }
                            if(!steamplayer.playerID.characterName.StartsWith("[US"))
                            {
                                steamplayer.SendChat("joined_team_must_rejoin", UCWarfare.I.Colors["joined_team_must_rejoin"],
                                    UCWarfare.I.TeamManager.Team1.LocalizedName, UCWarfare.I.ColorsHex["team_1_color"]);
                                UCWarfare.I.TeamManager.RemovePlayerFromTeam(steamplayer.playerID.steamID);
                                UCWarfare.I.TeamManager.AddPlayerToTeam(ETeam.TEAM1, steamplayer.playerID.steamID);
                                CommandWindow.LogWarning(F.Translate("player_switched_groups_console_must_rejoin", 0,
                                    steamplayer.playerID.playerName, steamplayer.playerID.steamID.m_SteamID.ToString(), UCWarfare.I.TeamManager.Team1.LocalizedName));
                                return;
                            }
                            steamplayer.player.quests.ServerAssignToGroup(group.groupID, EPlayerGroupRank.MEMBER, true);
                            GroupManager.save();
                            steamplayer.SendChat("joined_team", UCWarfare.I.Colors["joined_team"], 
                                UCWarfare.I.TeamManager.Team1.LocalizedName, UCWarfare.I.ColorsHex["team_1_color"]);
                            CommandWindow.LogWarning(F.Translate("player_switched_groups_console", 0,
                                steamplayer.playerID.playerName, steamplayer.playerID.steamID.m_SteamID.ToString(), UCWarfare.I.TeamManager.Team1.LocalizedName));
                            UCWarfare.I.TeamManager.RemovePlayerFromTeam(steamplayer.playerID.steamID);
                            UCWarfare.I.TeamManager.AddPlayerToTeam(ETeam.TEAM1, steamplayer.playerID.steamID);
                            if (steamplayer.player.TryGetComponent(out TeleportPlayerComponent component))
                            {
                                if(!component.InstantlyTeleportPlayer(steamplayer.GetBaseSpawn(), true))
                                {
                                    steamplayer.SendChat("from_lobby_teleport_failed", UCWarfare.I.Colors["from_lobby_teleport_failed"],
                                        UCWarfare.I.ColorsHex["from_lobby_teleport_failed_command"]);
                                    CommandWindow.LogError("Couldn't teleport " + steamplayer.playerID.playerName + " from lobby.");
                                }
                            } else
                            {
                                steamplayer.SendChat("from_lobby_teleport_failed", UCWarfare.I.Colors["from_lobby_teleport_failed"],
                                    UCWarfare.I.ColorsHex["from_lobby_teleport_failed_command"]);
                                CommandWindow.LogError("Couldn't get the player component of " + steamplayer.playerID.playerName);
                            }
                        } else if (command[0].ToLower() == "ru" || command[0].ToLower() == "russia")
                        {
                            GroupInfo group = GroupManager.getGroupInfo(new CSteamID(UCWarfare.I.TeamManager.Team2.GroupID));
                            if (group == null)
                            {
                                steamplayer.SendChat("join_group_not_found", UCWarfare.I.Colors["join_group_not_found"],
                                    UCWarfare.I.TeamManager.Team2.LocalizedName, UCWarfare.I.ColorsHex["team_2_color"],
                                    UCWarfare.I.TeamManager.Team2.ID.ToString(), UCWarfare.I.ColorsHex["join_group_not_found_group_id"]);
                                return;
                            }
                            Kits.UCInventoryManager.ClearInventory(player);
                            if (!group.hasSpaceForMoreMembersInGroup)
                            {
                                steamplayer.SendChat("join_group_has_no_space", UCWarfare.I.Colors["join_group_has_no_space"],
                                    UCWarfare.I.TeamManager.Team2.LocalizedName, UCWarfare.I.ColorsHex["team_2_color"]);
                                return;
                            }
                            if (!UCWarfare.I.TeamManager.CanJoinTeam(ETeam.TEAM2))
                            {
                                steamplayer.SendChat("join_auto_balance_cant_switch", UCWarfare.I.Colors["join_auto_balance_cant_switch"],
                                    UCWarfare.I.TeamManager.Team2.LocalizedName, UCWarfare.I.ColorsHex["team_2_color"]);
                                return;
                            }
                            if (!steamplayer.playerID.characterName.StartsWith("[US"))
                            {
                                steamplayer.SendChat("joined_team_must_rejoin", UCWarfare.I.Colors["joined_team_must_rejoin"],
                                    UCWarfare.I.TeamManager.Team2.LocalizedName, UCWarfare.I.ColorsHex["team_2_color"]);
                                UCWarfare.I.TeamManager.RemovePlayerFromTeam(steamplayer.playerID.steamID);
                                UCWarfare.I.TeamManager.AddPlayerToTeam(ETeam.TEAM2, steamplayer.playerID.steamID);
                                CommandWindow.LogWarning(F.Translate("player_switched_groups_console_must_rejoin", 0,
                                    steamplayer.playerID.playerName, steamplayer.playerID.steamID.m_SteamID.ToString(), UCWarfare.I.TeamManager.Team2.LocalizedName));
                                return;
                            }
                            steamplayer.player.quests.ServerAssignToGroup(group.groupID, EPlayerGroupRank.MEMBER, true);
                            GroupManager.save();
                            steamplayer.SendChat("joined_team", UCWarfare.I.Colors["joined_team"],
                                UCWarfare.I.TeamManager.Team2.LocalizedName, UCWarfare.I.ColorsHex["team_2_color"]);
                            CommandWindow.LogWarning(F.Translate("player_switched_groups_console", 0,
                                steamplayer.playerID.playerName, steamplayer.playerID.steamID.m_SteamID.ToString(), UCWarfare.I.TeamManager.Team2.LocalizedName)); // player joined T2
                            UCWarfare.I.TeamManager.RemovePlayerFromTeam(steamplayer.playerID.steamID);
                            UCWarfare.I.TeamManager.AddPlayerToTeam(ETeam.TEAM2, steamplayer.playerID.steamID);
                            if (steamplayer.player.TryGetComponent(out TeleportPlayerComponent component))
                            {
                                if (!component.InstantlyTeleportPlayer(steamplayer.GetBaseSpawn(), true))
                                {
                                    steamplayer.SendChat("from_lobby_teleport_failed", UCWarfare.I.Colors["from_lobby_teleport_failed"],
                                        UCWarfare.I.ColorsHex["from_lobby_teleport_failed_command"]);
                                    CommandWindow.LogError("Couldn't teleport " + steamplayer.playerID.playerName + " from lobby.");
                                }
                            }
                            else
                            {
                                steamplayer.SendChat("from_lobby_teleport_failed", UCWarfare.I.Colors["from_lobby_teleport_failed"],
                                    UCWarfare.I.ColorsHex["from_lobby_teleport_failed_command"]);
                                CommandWindow.LogError("Couldn't get the player component of " + steamplayer.playerID.playerName);
                            }
                        }
                    } else
                    {
                        steamplayer.SendChat("join_not_in_lobby", UCWarfare.I.Colors["join_not_in_lobby"], UCWarfare.I.ColorsHex["join_not_in_lobby_command"]);
                    }
                } else
                {
                    steamplayer.SendChat("join_command_no_args_provided", UCWarfare.I.Colors["join_command_no_args_provided"],
                        "us", UCWarfare.I.ColorsHex["team_1_color"], "ru", UCWarfare.I.ColorsHex["team_2_color"]);
                }
            }
        }
    }
}