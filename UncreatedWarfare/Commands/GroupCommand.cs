using Rocket.API;
using Rocket.Unturned.Player;
using SDG.Unturned;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace UncreatedWarfare.Commands
{
    internal class GroupCommand : IRocketCommand
    {
        public AllowedCaller AllowedCaller => AllowedCaller.Console;

        public string Name => "group";

        public string Help => "Join a group";

        public string Syntax => "/group <join|create|rename|delete> [name]";

        public List<string> Aliases => new List<string>();

        public List<string> Permissions => new List<string> { "uc.group" }; //.join, .create, .delete, .rename

        public void Execute(IRocketPlayer caller, string[] command)
        {
            UnturnedPlayer player = caller as UnturnedPlayer;
            if (command.Length == 0)
            {
                player.Player.SendChat("current_group", UCWarfare.I.Colors["current_group"], player.Player.quests.groupID.m_SteamID, UCWarfare.I.ColorsHex["current_group_id"], GroupManager.getGroupInfo(player.quests.groupID).name, UCWarfare.I.ColorsHex["current_group_name"]);
            } else if (command.Length == 1)
            {
                if (command[0].ToLower() == "delete")
                {

                }
            } else if (command.Length == 3)
            {
                if (command[0].ToLower() == "create")
                {
                    if(player.HasPermission("uc.group.create"))
                    {
                        if (!player.Player.quests.hasPermissionToCreateGroup)
                        {
                            player.Player.SendChat("cant_create_group", UCWarfare.I.Colors["cant_create_group"]);
                            return;
                        }
                        player.Player.quests.ReceiveCreateGroupRequest();
                        GroupManager.save();
                        JSONMethods.AddTeam(new TeamData(player.Player.quests.groupID.m_SteamID, command[2], new List<ulong> { player.Player.channel.owner.playerID.steamID.m_SteamID },
                            player.Player.transform.position.x, player.Player.transform.position.y + 1, player.Player.transform.position.z));
                        player.Player.SendChat("created_group", UCWarfare.I.Colors["created_group"],
                            command[2], UCWarfare.I.ColorsHex["created_group_name"], player.Player.quests.groupID.m_SteamID, UCWarfare.I.ColorsHex["created_group_id"]);
                        CommandWindow.LogWarning(F.Translate("created_group_console", player.Player.channel.owner.playerID.playerName,
                            player.Player.channel.owner.playerID.steamID.m_SteamID.ToString(), player.Player.quests.groupID.m_SteamID, command[2]));
                    } else
                        player.Player.SendChat("no_permissions", UCWarfare.I.Colors["no_permissions"]);
                } else if (command[0].ToLower() == "join")
                {
                    if(player.HasPermission("uc.group.join"))
                    {
                        GroupInfo groupInfo = GroupManager.getGroupInfo(player.Player.quests.groupID);
                        if (groupInfo == null)
                            player.SendChat("join_group_not_found", UCWarfare.I.Colors["join_group_not_found"]);
                    }
                }
            }
        }
    }
}
