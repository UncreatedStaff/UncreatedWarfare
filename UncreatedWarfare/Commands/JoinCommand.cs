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
        public async void Execute(IRocketPlayer caller, string[] command)
        {
            UCPlayer player = UCPlayer.FromIRocketPlayer(caller);

            // TODO
            if(command.Length == 1)
            {
                string team1name = TeamManager.TranslateName(1, player.CSteamID, true);
                string team2name = TeamManager.TranslateName(2, player.CSteamID, true);
                if (command[0].ToLower() == TeamManager.Team1Code.ToLower() || command[0].ToLower() == TeamManager.Team1Name.ToLower() || command[0].ToLower() == TeamManager.Team2Code.ToLower() || command[0].ToLower() == TeamManager.Team2Name.ToLower())
                {
                    ulong newTeam = 0;
                    string restrictedNamePrefix = "";
                    if (command[0].ToLower() == TeamManager.Team1Code.ToLower() || command[0].ToLower() == team1name.ToLower())
                    {
                        newTeam = TeamManager.Team1ID;
                        restrictedNamePrefix = "[US";
                    }
                    else if (command[0].ToLower() == TeamManager.Team2Code.ToLower() || command[0].ToLower() == team2name.ToLower())
                    {
                        newTeam = TeamManager.Team2ID;
                        restrictedNamePrefix = "[RU";
                    }

                    string teamName = TeamManager.TranslateName(newTeam, player.CSteamID);

                    if (TeamManager.LobbyZone.IsInside(player.Position))
                    {
                        GroupInfo group = GroupManager.getGroupInfo(new CSteamID(newTeam));
                        if (group == default)
                        {
                            player.Message("join_e_groupnoexist", TeamManager.TranslateName(newTeam, player.CSteamID, true));
                            return;
                        }
                        Kits.UCInventoryManager.ClearInventory(player);
                        if (!group.hasSpaceForMoreMembersInGroup)
                        {
                            player.Message("join_e_teamfull", teamName);
                            return;
                        }
                        if (!TeamManager.CanJoinTeam(newTeam))
                        {
                            player.Message("join_e_autobalance", teamName);
                            return;
                        }
                        if (player.CharacterName.StartsWith(restrictedNamePrefix))
                        {
                            player.Player.quests.sendLeaveGroup();
                            PlayerManager.Save();
                            player.Message("join_e_badname", restrictedNamePrefix);
                            return;
                        }
                        player.Message("joined_standby");
                        await Task.Delay(3000);

                        ulong oldgroup = player.GetTeam();
                        player.Player.quests.ServerAssignToGroup(group.groupID, EPlayerGroupRank.MEMBER, true);
                        GroupManager.save();
                        await EventFunctions.OnGroupChangedInvoke(player.Player.channel.owner, oldgroup, player.GetTeam());
                        

                        F.Log($"Player {player.CharacterName} switched to {teamName}", ConsoleColor.Cyan);

                        player.Player.teleportToLocation(newTeam.GetBaseSpawnFromTeam(), newTeam.GetBaseAngle());

                        player.Message("join_s", TeamManager.TranslateName(newTeam, player.CSteamID, true));

                        PlayerManager.Save();
                    }
                    else
                        player.Message("join_correctusage");
                }
                else
                    player.Message("join_correctusage");
            }
            else
                player.Message("join_correctusage");
        }
    }
}