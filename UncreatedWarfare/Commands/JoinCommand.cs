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
using Uncreated.Warfare.Teams;

namespace Uncreated.Warfare.Commands
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
                string t1name = TeamManager.TranslateName(TeamManager.Team1ID, player);
                string t2name = TeamManager.TranslateName(TeamManager.Team2ID, player);
                if (command[0].ToLower() == TeamManager.Team1Code.ToLower() || command[0].ToLower() == t1name.ToLower() || command[0].ToLower() == "ru" || command[0].ToLower() == t2name.ToLower())
                {
                    if (TeamManager.LobbyZone.IsInside(steamplayer.player.transform.position))
                    {
                        if(command[0].ToLower() == TeamManager.Team1Code.ToLower() || command[0].ToLower() == t1name.ToLower())
                        {
                            GroupInfo group = GroupManager.getGroupInfo(new CSteamID(TeamManager.Team1ID));
                            if(group == default)
                            {
                                steamplayer.SendChat("join_group_not_found", UCWarfare.GetColor("join_group_not_found"),
                                    t1name, TeamManager.Team1ColorHex,
                                    TeamManager.Team1ID.ToString(), UCWarfare.GetColorHex("join_group_not_found_group_id"));
                                return;
                            }
                            Kits.UCInventoryManager.ClearInventory(player);
                            if(!group.hasSpaceForMoreMembersInGroup)
                            {
                                steamplayer.SendChat("join_group_has_no_space", UCWarfare.GetColor("join_group_has_no_space"),
                                    t1name, TeamManager.Team1ColorHex);
                                return;
                            }
                            if(!TeamManager.CanJoinTeam(1))
                            {
                                steamplayer.SendChat("join_auto_balance_cant_switch", UCWarfare.GetColor("join_auto_balance_cant_switch"),
                                    t1name, TeamManager.Team1ColorHex);
                                return;
                            }
                            if(!steamplayer.playerID.characterName.StartsWith("[RU"))
                            {
                                steamplayer.SendChat("joined_team_must_rejoin", UCWarfare.GetColor("joined_team_must_rejoin"),
                                    t1name, TeamManager.Team1ColorHex);
                                F.Log(F.Translate("player_switched_groups_console_must_rejoin", 0,
                                    steamplayer.playerID.playerName, steamplayer.playerID.steamID.m_SteamID.ToString(), t1name), ConsoleColor.Cyan);
                                return;
                            }
                            steamplayer.player.quests.ServerAssignToGroup(group.groupID, EPlayerGroupRank.MEMBER, true);
                            GroupManager.save();
                            steamplayer.SendChat("joined_team", UCWarfare.GetColor("joined_team"),
                                t1name, TeamManager.Team1ColorHex);
                            F.Log(F.Translate("player_switched_groups_console", 0,
                                steamplayer.playerID.playerName, steamplayer.playerID.steamID.m_SteamID.ToString(), t1name), ConsoleColor.Cyan);
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
                        } else if (command[0].ToLower() == TeamManager.Team2Code || command[0].ToLower() == t2name.ToLower())
                        {
                            GroupInfo group = GroupManager.getGroupInfo(new CSteamID(TeamManager.Team2ID));
                            if (group == default)
                            {
                                steamplayer.SendChat("join_group_not_found", UCWarfare.GetColor("join_group_not_found"),
                                    t2name, TeamManager.Team2ColorHex,
                                    TeamManager.Team2ID.ToString(), UCWarfare.GetColorHex("join_group_not_found_group_id"));
                                return;
                            }
                            Kits.UCInventoryManager.ClearInventory(player);
                            if (!group.hasSpaceForMoreMembersInGroup)
                            {
                                steamplayer.SendChat("join_group_has_no_space", UCWarfare.GetColor("join_group_has_no_space"),
                                    t2name, TeamManager.Team2ColorHex);
                                return;
                            }
                            if (!TeamManager.CanJoinTeam(2))
                            {
                                steamplayer.SendChat("join_auto_balance_cant_switch", UCWarfare.GetColor("join_auto_balance_cant_switch"),
                                    t2name, TeamManager.Team2ColorHex);
                                return;
                            }
                            if (!steamplayer.playerID.characterName.StartsWith("[US"))
                            {
                                steamplayer.SendChat("joined_team_must_rejoin", UCWarfare.GetColor("joined_team_must_rejoin"),
                                    t2name, TeamManager.Team2ColorHex);
                                F.Log(F.Translate("player_switched_groups_console_must_rejoin", 0,
                                    steamplayer.playerID.playerName, steamplayer.playerID.steamID.m_SteamID.ToString(), t2name), ConsoleColor.Cyan);
                                return;
                            }
                            steamplayer.player.quests.ServerAssignToGroup(group.groupID, EPlayerGroupRank.MEMBER, true);
                            GroupManager.save();
                            steamplayer.SendChat("joined_team", UCWarfare.GetColor("joined_team"),
                                t2name, TeamManager.Team2ColorHex);
                            F.Log(F.Translate("player_switched_groups_console", 0,
                                steamplayer.playerID.playerName, steamplayer.playerID.steamID.m_SteamID.ToString(), t2name), ConsoleColor.Cyan); // player joined T2
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
                        TeamManager.Team1Code, TeamManager.Team1ColorHex, TeamManager.Team2Code, TeamManager.Team2ColorHex);
                }
            }
        }
    }
}