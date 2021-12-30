﻿using Rocket.API;
using SDG.Unturned;
using Steamworks;
using System;
using System.Collections.Generic;
using Uncreated.Warfare.Gamemodes.Interfaces;
using Uncreated.Warfare.Kits;
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
            UCPlayer player = UCPlayer.FromIRocketPlayer(caller);
            if (!Data.Is(out ITeams gm))
            {
                player.Message("command_e_gamemode");
                return;
            }
            if (command.Length == 1)
            {
                string imput = command[0].ToLower();
                string t1code = TeamManager.Team1Code.ToLower();
                string t1name = TeamManager.Team1Name.ToLower();
                string t2code = TeamManager.Team2Code.ToLower();
                string t2name = TeamManager.Team2Name.ToLower();
                if (imput == t1code || imput == t1name || imput == t2code || imput == t2name)
                {
                    ulong newTeam = 0;
                    string restrictedNamePrefix = "";
                    if (imput == t1code || imput == t1name)
                    {
                        newTeam = TeamManager.Team1ID;
                        restrictedNamePrefix = TeamManager.Team2Code;
                    }
                    else if (imput == t2code || imput == t2name)
                    {
                        newTeam = TeamManager.Team2ID;
                        restrictedNamePrefix = TeamManager.Team1Code;
                    }

                    string teamName = TeamManager.TranslateName(newTeam, player.CSteamID);

                    if (!TeamManager.LobbyZone.IsInside(player.Position))
                    {
                        player.SendChat("join_e_notinlobby");
                        return;
                    }
                    if (player.GetTeam() == newTeam)
                    {
                        player.SendChat("join_e_alreadyonteam");
                        return;
                    }

                    L.Log("Team ID: " + newTeam);

                    GroupInfo group = GroupManager.getGroupInfo(new CSteamID(newTeam));
                    if (group == null)
                    {
                        player.SendChat("join_e_groupnoexist", TeamManager.TranslateName(newTeam, player.CSteamID, true));
                        return;
                    }
                    Kits.UCInventoryManager.ClearInventory(player);
                    if (!group.hasSpaceForMoreMembersInGroup)
                    {
                        player.SendChat("join_e_teamfull", teamName);
                        return;
                    }
                    if (!TeamManager.CanJoinTeam(newTeam))
                    {
                        player.SendChat("join_e_autobalance", teamName);
                        return;
                    }
                    if (player.CharacterName.StartsWith(restrictedNamePrefix, StringComparison.OrdinalIgnoreCase))
                    {
                        player.Player.quests.leaveGroup(true);
                        PlayerManager.ApplyToOnline();
                        player.SendChat("join_e_badname", restrictedNamePrefix);
                        UCInventoryManager.ClearInventory(player);
                        return;
                    }
                    //player.SendChat("joined_standby");
                    //Task.Delay(3000);

                    ulong oldgroup = player.GetTeam();
                    player.Player.quests.ServerAssignToGroup(group.groupID, EPlayerGroupRank.MEMBER, true);
                    GroupManager.save();
                    EventFunctions.OnGroupChangedInvoke(player.Player.channel.owner, oldgroup, newTeam);

                    Players.FPlayerName names = F.GetPlayerOriginalNames(player);
                    L.Log(Translation.Translate("join_player_joined_console", 0, out _,
                        names.PlayerName, player.Steam64.ToString(), newTeam.ToString(Data.Locale), oldgroup.ToString(Data.Locale)),
                        ConsoleColor.Cyan);

                    player.Player.teleportToLocation(newTeam.GetBaseSpawnFromTeam(), newTeam.GetBaseAngle());

                    if (KitManager.KitExists(TeamManager.GetUnarmedFromS64ID(player.Steam64), out var kit))
                        KitManager.GiveKit(player, kit);

                    player.SendChat("join_s", TeamManager.TranslateName(newTeam, player.CSteamID, true));

                    new List<CSteamID>(1) { player.CSteamID }.BroadcastToAllExcept("join_announce", names.CharacterName, teamName);

                    if (player.Squad != null)
                        Squads.SquadManager.LeaveSquad(player, player.Squad);
                    PlayerManager.ApplyToOnline();
                }
                else
                    player.SendChat("join_correctusage");
            }
            else
                player.SendChat("join_correctusage");
        }
    }
}