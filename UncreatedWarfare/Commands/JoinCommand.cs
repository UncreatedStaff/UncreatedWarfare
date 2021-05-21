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
                    if (Data.TeamManager.LobbyZone.IsInside(steamplayer.player.transform.position))
                    {
                        if(command[0].ToLower() == "us" || command[0].ToLower() == "usa")
                        {
                            GroupInfo group = GroupManager.getGroupInfo(new CSteamID(Data.TeamManager.Team1.GroupID));
                            if(group == null)
                            {
                                steamplayer.SendChat("join_group_not_found", UCWarfare.GetColor("join_group_not_found"),
                                    Data.TeamManager.Team1.LocalizedName, UCWarfare.GetColorHex("team_1_color"),
                                    Data.TeamManager.Team1.GroupID.ToString(), UCWarfare.GetColorHex("join_group_not_found_group_id"));
                                return;
                            }
                            Kits.UCInventoryManager.ClearInventory(player);
                            if(!group.hasSpaceForMoreMembersInGroup)
                            {
                                steamplayer.SendChat("join_group_has_no_space", UCWarfare.GetColor("join_group_has_no_space"),
                                    Data.TeamManager.Team1.LocalizedName, UCWarfare.GetColorHex("team_1_color"));
                                return;
                            }
                            if(!Data.TeamManager.CanJoinTeam(ETeam.TEAM1))
                            {
                                steamplayer.SendChat("join_auto_balance_cant_switch", UCWarfare.GetColor("join_auto_balance_cant_switch"),
                                    Data.TeamManager.Team1.LocalizedName, UCWarfare.GetColorHex("team_1_color"));
                                return;
                            }
                            if(!steamplayer.playerID.characterName.StartsWith("[RU"))
                            {
                                steamplayer.SendChat("joined_team_must_rejoin", UCWarfare.GetColor("joined_team_must_rejoin"),
                                    Data.TeamManager.Team1.LocalizedName, UCWarfare.GetColorHex("team_1_color"));
                                Data.TeamManager.RemovePlayerFromTeam(steamplayer);
                                Data.TeamManager.AddPlayerToTeam(ETeam.TEAM1, steamplayer);
                                F.Log(F.Translate("player_switched_groups_console_must_rejoin", 0,
                                    steamplayer.playerID.playerName, steamplayer.playerID.steamID.m_SteamID.ToString(), Data.TeamManager.Team1.LocalizedName), ConsoleColor.Cyan);
                                return;
                            }
                            steamplayer.player.quests.ServerAssignToGroup(group.groupID, EPlayerGroupRank.MEMBER, true);
                            GroupManager.save();
                            steamplayer.SendChat("joined_team", UCWarfare.GetColor("joined_team"), 
                                Data.TeamManager.Team1.LocalizedName, UCWarfare.GetColorHex("team_1_color"));
                            F.Log(F.Translate("player_switched_groups_console", 0,
                                steamplayer.playerID.playerName, steamplayer.playerID.steamID.m_SteamID.ToString(), Data.TeamManager.Team1.LocalizedName), ConsoleColor.Cyan);
                            Data.TeamManager.RemovePlayerFromTeam(steamplayer);
                            Data.TeamManager.AddPlayerToTeam(ETeam.TEAM1, steamplayer);
                            if (steamplayer.player.TryGetComponent(out TeleportPlayerComponent component))
                            {
                                if(!component.InstantlyTeleportPlayer(steamplayer.GetBaseSpawn(), true))
                                {
                                    steamplayer.SendChat("from_lobby_teleport_failed", UCWarfare.GetColor("from_lobby_teleport_failed"),
                                        UCWarfare.GetColorHex("from_lobby_teleport_failed_command"));
                                    F.LogError("Couldn't teleport " + steamplayer.playerID.playerName + " from lobby.");
                                }
                            } else
                            {
                                steamplayer.SendChat("from_lobby_teleport_failed", UCWarfare.GetColor("from_lobby_teleport_failed"),
                                    UCWarfare.GetColorHex("from_lobby_teleport_failed_command"));
                                F.LogError("Couldn't get the player component of " + steamplayer.playerID.playerName);
                            }
                        } else if (command[0].ToLower() == "ru" || command[0].ToLower() == "russia")
                        {
                            GroupInfo group = GroupManager.getGroupInfo(new CSteamID(Data.TeamManager.Team2.GroupID));
                            if (group == null)
                            {
                                steamplayer.SendChat("join_group_not_found", UCWarfare.GetColor("join_group_not_found"),
                                    Data.TeamManager.Team2.LocalizedName, UCWarfare.GetColorHex("team_2_color"),
                                    Data.TeamManager.Team2.ID.ToString(), UCWarfare.GetColorHex("join_group_not_found_group_id"));
                                return;
                            }
                            Kits.UCInventoryManager.ClearInventory(player);
                            if (!group.hasSpaceForMoreMembersInGroup)
                            {
                                steamplayer.SendChat("join_group_has_no_space", UCWarfare.GetColor("join_group_has_no_space"),
                                    Data.TeamManager.Team2.LocalizedName, UCWarfare.GetColorHex("team_2_color"));
                                return;
                            }
                            if (!Data.TeamManager.CanJoinTeam(ETeam.TEAM2))
                            {
                                steamplayer.SendChat("join_auto_balance_cant_switch", UCWarfare.GetColor("join_auto_balance_cant_switch"),
                                    Data.TeamManager.Team2.LocalizedName, UCWarfare.GetColorHex("team_2_color"));
                                return;
                            }
                            if (!steamplayer.playerID.characterName.StartsWith("[US"))
                            {
                                steamplayer.SendChat("joined_team_must_rejoin", UCWarfare.GetColor("joined_team_must_rejoin"),
                                    Data.TeamManager.Team2.LocalizedName, UCWarfare.GetColorHex("team_2_color"));
                                Data.TeamManager.RemovePlayerFromTeam(steamplayer);
                                Data.TeamManager.AddPlayerToTeam(ETeam.TEAM2, steamplayer);
                                F.Log(F.Translate("player_switched_groups_console_must_rejoin", 0,
                                    steamplayer.playerID.playerName, steamplayer.playerID.steamID.m_SteamID.ToString(), Data.TeamManager.Team2.LocalizedName), ConsoleColor.Cyan);
                                return;
                            }
                            steamplayer.player.quests.ServerAssignToGroup(group.groupID, EPlayerGroupRank.MEMBER, true);
                            GroupManager.save();
                            steamplayer.SendChat("joined_team", UCWarfare.GetColor("joined_team"),
                                Data.TeamManager.Team2.LocalizedName, UCWarfare.GetColorHex("team_2_color"));
                            F.Log(F.Translate("player_switched_groups_console", 0,
                                steamplayer.playerID.playerName, steamplayer.playerID.steamID.m_SteamID.ToString(), Data.TeamManager.Team2.LocalizedName), ConsoleColor.Cyan); // player joined T2
                            Data.TeamManager.RemovePlayerFromTeam(steamplayer);
                            Data.TeamManager.AddPlayerToTeam(ETeam.TEAM2, steamplayer);
                            if (steamplayer.player.TryGetComponent(out TeleportPlayerComponent component))
                            {
                                if (!component.InstantlyTeleportPlayer(steamplayer.GetBaseSpawn(), true))
                                {
                                    steamplayer.SendChat("from_lobby_teleport_failed", UCWarfare.GetColor("from_lobby_teleport_failed"),
                                        UCWarfare.GetColorHex("from_lobby_teleport_failed_command"));
                                    F.LogError("Couldn't teleport " + steamplayer.playerID.playerName + " from lobby.");
                                }
                            }
                            else
                            {
                                steamplayer.SendChat("from_lobby_teleport_failed", UCWarfare.GetColor("from_lobby_teleport_failed"),
                                    UCWarfare.GetColorHex("from_lobby_teleport_failed_command"));
                                F.LogError("Couldn't get the player component of " + steamplayer.playerID.playerName);
                            }
                        }
                    } else
                    {
                        steamplayer.SendChat("join_not_in_lobby", UCWarfare.GetColor("join_not_in_lobby"), UCWarfare.GetColorHex("join_not_in_lobby_command"));
                    }
                } else
                {
                    steamplayer.SendChat("join_command_no_args_provided", UCWarfare.GetColor("join_command_no_args_provided"),
                        "us", UCWarfare.GetColorHex("team_1_color"), "ru", UCWarfare.GetColorHex("team_2_color"));
                }
            }
        }
    }
}