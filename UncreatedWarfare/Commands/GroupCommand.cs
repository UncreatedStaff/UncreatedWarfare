using Rocket.API;
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
        public AllowedCaller AllowedCaller => AllowedCaller.Console;

        public string Name => "group";

        public string Help => "Join a group";

        public string Syntax => "/group <join|create|rename|delete> [name]";

        public List<string> Aliases => new List<string>();

        public List<string> Permissions => new List<string> { "uc.group" }; //.join, .create, .delete, .rename, .current

        public void Execute(IRocketPlayer caller, string[] command)
        {
            UnturnedPlayer player = caller as UnturnedPlayer;
            if (command.Length == 0)
            {
                if (player.HasPermission("uc.group.current"))
                {
                    player.Player.SendChat("current_group", UCWarfare.I.Colors["current_group"],
                        player.Player.quests.groupID.m_SteamID, UCWarfare.I.ColorsHex["current_group_id"],
                        GroupManager.getGroupInfo(player.Player.quests.groupID).name, UCWarfare.I.ColorsHex["current_group_name"]);
                }
                else
                    player.Player.SendChat("no_permissions", UCWarfare.I.Colors["no_permissions"]);
            } else if (command.Length == 3)
            {
                if (command[0].ToLower() == "create")
                {
                    if (player.HasPermission("uc.group.create"))
                    {
                        ulong ID;
                        if (!ulong.TryParse(command[1], out ID))
                        {
                            player.SendChat("joined_group_not_found", UCWarfare.I.Colors["joined_group_not_found"],
                                command[1], UCWarfare.I.ColorsHex["joined_group_not_found_group_id"]);
                            return;
                        }
                        if (!player.Player.quests.hasPermissionToCreateGroup)
                        {
                            player.Player.SendChat("cant_create_group", UCWarfare.I.Colors["cant_create_group"]);
                            return;
                        }
                        GroupManager.addGroup(new CSteamID(ID), command[2]);
                        GroupManager.save();
                        JSONMethods.AddTeam(new TeamData(ID, command[2], new List<ulong>(),
                            player.Player.transform.position.x, player.Player.transform.position.y + 1, player.Player.transform.position.z));
                        player.Player.SendChat("created_group", UCWarfare.I.Colors["created_group"],
                            command[2], UCWarfare.I.ColorsHex["created_group_name"], ID.ToString(), UCWarfare.I.ColorsHex["created_group_id"]);
                        CommandWindow.LogWarning(F.Translate("created_group_console", player.Player.channel.owner.playerID.playerName,
                            player.Player.channel.owner.playerID.steamID.m_SteamID.ToString(), ID.ToString(), command[2]));
                    } else
                        player.Player.SendChat("no_permissions", UCWarfare.I.Colors["no_permissions"]);
                } else if (command[0].ToLower() == "rename")
                {
                    if (player.HasPermission("uc.group.rename"))
                    {
                        ulong ID;
                        if (!ulong.TryParse(command[1], out ID))
                        {
                            player.SendChat("joined_group_not_found", UCWarfare.I.Colors["joined_group_not_found"],
                                command[1], UCWarfare.I.ColorsHex["joined_group_not_found_group_id"]);
                            return;
                        }
                        GroupInfo group = GroupManager.getGroupInfo(new CSteamID(ID));
                        if (group == null)
                        {
                            player.SendChat("joined_group_not_found", UCWarfare.I.Colors["joined_group_not_found"],
                                ID.ToString(), UCWarfare.I.ColorsHex["joined_group_not_found_group_id"]);
                            return;
                        }
                        TeamOld t = UCWarfare.I.TeamManager.Teams.FirstOrDefault(x => x.ID == ID);
                        bool updatedInJSON = t != null && t.Name == command[2];
                        bool updatedInGroupManager = group.name == command[2];
                        if (updatedInJSON && updatedInGroupManager)
                        {
                            player.SendChat("renamed_group_already_named_that", UCWarfare.I.Colors["renamed_group_already_named_that"]);
                            return;
                        }
                        string oldName = string.Empty;
                        if(!updatedInJSON)
                        {
                            UCWarfare.I.TeamManager.RenameTeam(ID, command[2], out oldName);
                        }
                        if(!updatedInGroupManager)
                        {
                            if (oldName == string.Empty) oldName = group.name;
                            group.name = command[2];
                            GroupManager.sendGroupInfo(group);
                            GroupManager.save();
                        }
                        player.SendChat("renamed_group", UCWarfare.I.Colors["renamed_group"],
                            ID.ToString(), UCWarfare.I.ColorsHex["renamed_group_id"],
                            oldName, UCWarfare.I.ColorsHex["renamed_group_old_name"],
                            command[2], UCWarfare.I.ColorsHex["renamed_group_new_name"]);
                        CommandWindow.LogWarning(F.Translate("renamed_group_console",
                            player.Player.channel.owner.playerID.playerName, player.Player.channel.owner.playerID.steamID.m_SteamID.ToString(),
                            ID.ToString(), oldName, command[2]));
                    }
                    else
                        player.Player.SendChat("no_permissions", UCWarfare.I.Colors["no_permissions"]);
                }
            } else if (command.Length == 2) 
            { 
                if (command[0].ToLower() == "join")
                {
                    if (player.HasPermission("uc.group.join"))
                    {
                        ulong ID;
                        if (!ulong.TryParse(command[1], out ID))
                        {
                            player.SendChat("joined_group_not_found", UCWarfare.I.Colors["joined_group_not_found"],
                                command[1], UCWarfare.I.ColorsHex["joined_group_not_found_group_id"]);
                            return;
                        }
                        if (player.Player.quests.groupID.m_SteamID == ID)
                        {
                            player.SendChat("joined_already_in_group", UCWarfare.I.Colors["joined_already_in_group"]);
                            return;
                        }
                        GroupInfo groupInfo = GroupManager.getGroupInfo(new CSteamID(ID));
                        if (groupInfo == null)
                        {
                            player.SendChat("joined_group_not_found", UCWarfare.I.Colors["joined_group_not_found"],
                                ID.ToString(), UCWarfare.I.ColorsHex["joined_group_not_found_group_id"]);
                            return;
                        }
                        ulong OldGroup = player.Player.quests.groupID.m_SteamID;
                        player.Player.quests.ServerAssignToGroup(groupInfo.groupID, EPlayerGroupRank.MEMBER, true);
                        GroupManager.save();
                        ulong team = player.GetTeam();
                        if (team == 0) team = player.Player.quests.groupID.m_SteamID;
                        if (team == 1)
                        {
                            foreach (TeamOld t in UCWarfare.I.TeamManager.Teams)
                                if (t.ID != team)
                                    t.RemovePlayer(player.Player.channel.owner.playerID.steamID.m_SteamID, true);
                            UCWarfare.I.TeamManager.T1.AddPlayer(player.Player.channel.owner.playerID.steamID.m_SteamID, true);
                            player.SendChat("joined_group", UCWarfare.I.Colors["joined_group"],
                                UCWarfare.I.TeamManager.T1.LocalizedName, UCWarfare.I.ColorsHex["team_1_color"],
                                groupInfo.groupID.m_SteamID.ToString(), UCWarfare.I.ColorsHex["joined_group_id"]);
                        } else if (team == 2)
                        {
                            foreach (TeamOld t in UCWarfare.I.TeamManager.Teams)
                                if (t.ID != team)
                                    t.RemovePlayer(player.Player.channel.owner.playerID.steamID.m_SteamID, true);
                            UCWarfare.I.TeamManager.T1.AddPlayer(player.Player.channel.owner.playerID.steamID.m_SteamID, true);
                            player.SendChat("joined_group", UCWarfare.I.Colors["joined_group"],
                                UCWarfare.I.TeamManager.T2.LocalizedName, UCWarfare.I.ColorsHex["team_2_color"],
                                groupInfo.groupID.m_SteamID.ToString(), UCWarfare.I.ColorsHex["joined_group_id"]);
                        } else if (team == 3)
                        {
                            foreach (TeamOld t in UCWarfare.I.TeamManager.Teams)
                                if (t.ID != team)
                                    t.RemovePlayer(player.Player.channel.owner.playerID.steamID.m_SteamID, true);
                            UCWarfare.I.TeamManager.T1.AddPlayer(player.Player.channel.owner.playerID.steamID.m_SteamID, true);
                            player.SendChat("joined_group", UCWarfare.I.Colors["joined_group"],
                                UCWarfare.I.TeamManager.T3.LocalizedName, UCWarfare.I.ColorsHex["team_3_color"],
                                groupInfo.groupID.m_SteamID.ToString(), UCWarfare.I.ColorsHex["joined_group_id"]);
                        } else
                        {
                            TeamOld data = null;
                            foreach (TeamOld t in UCWarfare.I.TeamManager.Teams)
                            {
                                if (t.ID != team)
                                    t.RemovePlayer(player.Player.channel.owner.playerID.steamID.m_SteamID, true);
                                else
                                    data = t;
                            }
                            UCWarfare.I.TeamManager.T1.AddPlayer(player.Player.channel.owner.playerID.steamID.m_SteamID, true);
                            string name = groupInfo.name;
                            string color = UCWarfare.I.ColorsHex["joined_group_name"];
                            if (data != null)
                            {
                                name = data.LocalizedName;
                                color = data.TeamColorHex;
                            }
                            player.SendChat("joined_group", UCWarfare.I.Colors["joined_group"],
                                name, color,
                                groupInfo.groupID.m_SteamID.ToString(), UCWarfare.I.ColorsHex["joined_group_id"]);
                            CommandWindow.LogWarning(F.Translate("joined_group_console", player.Player.channel.owner.playerID.playerName,
                                player.Player.channel.owner.playerID.steamID.m_SteamID.ToString(), name, groupInfo.groupID.m_SteamID.ToString()));
                        }
                    }
                    else
                        player.Player.SendChat("no_permissions", UCWarfare.I.Colors["no_permissions"]);
                } else if (command[0] == "delete")
                {
                    if (player.HasPermission("uc.group.delete"))
                    {
                        ulong ID;
                        if (!ulong.TryParse(command[1], out ID))
                        {
                            player.SendChat("joined_group_not_found", UCWarfare.I.Colors["joined_group_not_found"],
                                command[1], UCWarfare.I.ColorsHex["joined_group_not_found_group_id"]);
                            return;
                        }
                        GroupInfo group = GroupManager.getGroupInfo(new CSteamID(ID));
                        GroupManager.deleteGroup(group.groupID);
                        GroupManager.save();
                        UCWarfare.I.TeamManager.DeleteTeam(group.groupID.m_SteamID, out TeamData deleted);
                        string color = UCWarfare.I.ColorsHex["deleted_group_name"];
                        string name = group.name;
                        TeamOld t = UCWarfare.I.TeamManager.Teams.FirstOrDefault(x => x.ID == group.groupID.m_SteamID || (deleted != null && x.ID == deleted.team_id));
                        if(t != null)
                        {
                            color = t.TeamColorHex;
                            name = t.LocalizedName;
                        } else if (deleted != null)
                            name = deleted.name;
                        player.SendChat("deleted_group", UCWarfare.I.Colors["deleted_group"],
                            name, color, 
                            group.groupID.m_SteamID.ToString(), UCWarfare.I.ColorsHex["joined_group_not_found_group_id"]);
                        CommandWindow.LogWarning(F.Translate("deleted_group_console", player.Player.channel.owner.playerID.playerName, 
                            player.Player.channel.owner.playerID.steamID.m_SteamID.ToString(), name, group.groupID.m_SteamID.ToString()));
                    }
                    else
                        player.Player.SendChat("no_permissions", UCWarfare.I.Colors["no_permissions"]);
                }
            } else 
            {
                player.SendChat("group_usage", UCWarfare.I.Colors["group_usage"]);
            }
        }
    }
}
