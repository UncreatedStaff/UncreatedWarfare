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

        public string Syntax => throw new NotImplementedException();

        public List<string> Aliases => throw new NotImplementedException();

        public List<string> Permissions => throw new NotImplementedException();

        public void Execute(IRocketPlayer caller, string[] command)
        {
            Player player = ((UnturnedPlayer)caller).Player;
            if (command.Length == 0)
            {
                player.SendChat("current_group", UCWarfare.I.Colors["current_group"], player.quests.groupID.m_SteamID, UCWarfare.I.ColorsHex["current_group_id"], GroupManager.getGroupInfo(player.quests.groupID).name, UCWarfare.I.ColorsHex["current_group_name"]);
            } else if (command.Length == 1)
            {
                if (command[0].ToLower() == "create")
                {

                }
            } else if (command.Length == 3)
            {
                if (command[0].ToLower() == "create")
                {
                    if (!player.quests.hasPermissionToCreateGroup)
                    {
                        player.SendChat("cant_create_group", UCWarfare.I.Colors["cant_create_group"]);
                        return;
                    }
                    player.quests.ReceiveCreateGroupRequest();
                    GroupManager.save();
                    JSONMethods.AddTeam(new TeamData(player.quests.groupID.m_SteamID, command[2], new List<ulong> { player.channel.owner.playerID.steamID.m_SteamID }));
                }
            }
        }
    }
}
