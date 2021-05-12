﻿using Rocket.API;
using Rocket.Unturned.Player;
using SDG.Unturned;
using Steamworks;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UncreatedWarfare.Teams;

namespace UncreatedWarfare.Commands
{
    internal class GroupCommand : IRocketCommand
    {
        public AllowedCaller AllowedCaller => AllowedCaller.Player;

        public string Name => "group";

        public string Help => "Join a group";

        public string Syntax => "/group <join [id]|create [name]|rename|delete> [name]";

        public List<string> Aliases => new List<string>();

        public List<string> Permissions => new List<string> { "uc.group" }; //.join, .create, .delete, .rename, .current

        public void Execute(IRocketPlayer caller, string[] command)
        {
            UnturnedPlayer player = caller as UnturnedPlayer;
            if (command.Length == 0)
            {
                if (player.HasPermission("uc.group.current"))
                {
                    GroupInfo info = GroupManager.getGroupInfo(player.Player.quests.groupID);
                    if(info == null)
                    {
                        player.Player.SendChat("not_in_group", UCWarfare.GetColor("not_in_group"));
                    } else
                    {
                        player.Player.SendChat("current_group", UCWarfare.GetColor("current_group"),
                            player.Player.quests.groupID.m_SteamID, UCWarfare.GetColorHex("current_group_id"),
                            info.name, UCWarfare.GetColorHex("current_group_name"));
                    }
                }
                else
                    player.Player.SendChat("no_permissions", UCWarfare.GetColor("no_permissions"));
            }
            else if (command.Length == 2)
            {
                if (command[0].ToLower() == "create")
                {
                    if (player.HasPermission("uc.group.create"))
                    {
                        if (!player.Player.quests.hasPermissionToCreateGroup)
                        {
                            player.Player.SendChat("cant_create_group", UCWarfare.GetColor("cant_create_group"));
                            return;
                        }
                        player.Player.quests.ReceiveCreateGroupRequest();
                        GroupManager.save();
                        player.Player.SendChat("created_group", UCWarfare.GetColor("created_group"),
                            command[1], UCWarfare.GetColorHex("created_group_name"), player.Player.quests.groupID.m_SteamID.ToString(), UCWarfare.GetColorHex("created_group_id"));
                        CommandWindow.LogWarning(F.Translate("created_group_console", 0, player.Player.channel.owner.playerID.playerName,
                            player.Player.channel.owner.playerID.steamID.m_SteamID.ToString(), player.Player.quests.groupID.m_SteamID.ToString(), command[1]));
                    }
                    else
                        player.Player.SendChat("no_permissions", UCWarfare.GetColor("no_permissions"));
                }
                else if (command[0].ToLower() == "join")
                {
                    if (player.HasPermission("uc.group.join"))
                    {
                        ulong ID;
                        if (!ulong.TryParse(command[1], out ID))
                        {
                            player.SendChat("joined_group_not_found", UCWarfare.GetColor("joined_group_not_found"),
                                command[1], UCWarfare.GetColorHex("joined_group_not_found_group_id"));
                            return;
                        }
                        if (player.Player.quests.groupID.m_SteamID == ID)
                        {
                            player.SendChat("joined_already_in_group", UCWarfare.GetColor("joined_already_in_group"));
                            return;
                        }
                        GroupInfo groupInfo = GroupManager.getGroupInfo(new CSteamID(ID));
                        if (groupInfo == null)
                        {
                            player.SendChat("joined_group_not_found", UCWarfare.GetColor("joined_group_not_found"),
                                ID.ToString(), UCWarfare.GetColorHex("joined_group_not_found_group_id"));
                            return;
                        }
                        ulong OldGroup = player.Player.quests.groupID.m_SteamID;
                        if(player.Player.quests.ServerAssignToGroup(groupInfo.groupID, EPlayerGroupRank.MEMBER, true))
                        {
                            GroupManager.save();
                            ulong team = player.GetTeam();
                            CommandWindow.Log(player.Player.channel.owner.playerID.playerName);
                            if (team == 0) team = player.Player.quests.groupID.m_SteamID;
                            if (team == 1)
                            {
                                UCWarfare.I.TeamManager?.RemovePlayerFromTeam(player.Player.channel.owner);
                                UCWarfare.I.TeamManager?.AddPlayerToTeam(ETeam.TEAM1, player.Player.channel.owner);
                                player.SendChat("joined_group", UCWarfare.GetColor("joined_group"),
                                    UCWarfare.I.TeamManager.Team1.LocalizedName, UCWarfare.GetColorHex("team_1_color"),
                                    groupInfo.groupID.m_SteamID.ToString(), UCWarfare.GetColorHex("joined_group_id"));
                                CommandWindow.LogWarning(F.Translate("joined_group_console", 0, player.Player.channel.owner.playerID.playerName,
                                    player.Player.channel.owner.playerID.steamID.m_SteamID.ToString(), UCWarfare.I.TeamManager.Team1.LocalizedName,
                                    groupInfo.groupID.m_SteamID.ToString()));
                            }
                            else if (team == 2)
                            {
                                UCWarfare.I.TeamManager?.RemovePlayerFromTeam(player.Player.channel.owner);
                                UCWarfare.I.TeamManager?.AddPlayerToTeam(ETeam.TEAM2, player.Player.channel.owner);
                                player.SendChat("joined_group", UCWarfare.GetColor("joined_group"),
                                    UCWarfare.I.TeamManager.Team2.LocalizedName, UCWarfare.GetColorHex("team_2_color"),
                                    groupInfo.groupID.m_SteamID.ToString(), UCWarfare.GetColorHex("joined_group_id"));
                                CommandWindow.LogWarning(F.Translate("joined_group_console", 0, player.Player.channel.owner.playerID.playerName,
                                    player.Player.channel.owner.playerID.steamID.m_SteamID.ToString(), UCWarfare.I.TeamManager.Team2.LocalizedName,
                                    groupInfo.groupID.m_SteamID.ToString()));
                            }
                            else if (team == 3)
                            {
                                UCWarfare.I.TeamManager.RemovePlayerFromTeam(player.Player.channel.owner);
                                UCWarfare.I.TeamManager.AddPlayerToTeam(ETeam.NEUTRAL, player.Player.channel.owner);
                                player.SendChat("joined_group", UCWarfare.GetColor("joined_group"),
                                    UCWarfare.I.TeamManager.Team1.LocalizedName, UCWarfare.GetColorHex("team_3_color"),
                                    groupInfo.groupID.m_SteamID.ToString(), UCWarfare.GetColorHex("joined_group_id"));
                                CommandWindow.LogWarning(F.Translate("joined_group_console", 0, player.Player.channel.owner.playerID.playerName,
                                    player.Player.channel.owner.playerID.steamID.m_SteamID.ToString(), UCWarfare.I.TeamManager.Neutral.LocalizedName,
                                    groupInfo.groupID.m_SteamID.ToString()));
                            }
                            else
                            {
                                player.SendChat("joined_group_not_found", UCWarfare.GetColor("joined_group_not_found"),
                                    ID.ToString(), UCWarfare.GetColorHex("joined_group_not_found_group_id"));
                            }
                        }
                        else
                        {
                            player.SendChat("joined_group_not_found", UCWarfare.GetColor("joined_group_not_found"),
                                ID.ToString(), UCWarfare.GetColorHex("joined_group_not_found_group_id"));
                        }
                    }
                    else
                        player.Player.SendChat("no_permissions", UCWarfare.GetColor("no_permissions"));
                }
            }
            else
            {
                player.SendChat("group_usage", UCWarfare.GetColor("group_usage"));
            }
        }
    }
}
