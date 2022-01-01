﻿using Rocket.API;
using System;
using System.Collections.Generic;
using System.Linq;
using Uncreated.Players;
using Uncreated.Warfare.Point;

namespace Uncreated.Warfare.Commands
{
    public class Command_officer : IRocketCommand
    {
        public AllowedCaller AllowedCaller => AllowedCaller.Player;
        public string Name => "officer";
        public string Help => "promotes or demotes a player to an officer rank";
        public string Syntax => "/officer";
        public List<string> Aliases => new List<string>();
        public List<string> Permissions => new List<string>() { "uc.officer" };
        public void Execute(IRocketPlayer caller, string[] command)
        {
            UCPlayer player = UCPlayer.FromIRocketPlayer(caller);

            if (command.Length >= 1 && (command[0].ToLower() == "setrank" || command[0].ToLower() == "set"))
            {
                if (command.Length < 3)
                {
                    player.SendChat("correct_usage", "/officer promote <player name> <level or rank> <branch>");
                    return;
                }

                UCPlayer target = UCPlayer.FromName(command[1]);
                ulong Steam64 = 0;
                string characterName = "";
                if (ulong.TryParse(command[1], out Steam64) && Data.DatabaseManager.PlayerExistsInDatabase(Steam64, out FPlayerName names))
                {
                    characterName = names.CharacterName;
                    goto CheckLevelAndBranch;
                }
                else if (target != null)
                {
                    goto CheckLevelAndBranch;
                }
                else
                    player.SendChat("officer_e_playernotfound", command[1]);

                CheckLevelAndBranch:
                if (int.TryParse(command[2], System.Globalization.NumberStyles.Any, Data.Locale, out var level))
                {
                    if (Enum.TryParse(command[3], out EBranch branch))
                    {
                        if (target != null)
                        {
                            OfficerStorage.ChangeOfficerRank(target.Steam64, level, branch);
                            player.Message("officer_s_changedrank", target.CharacterName, target.Rank.Name, branch.ToString());
                        }
                        else
                        {
                            OfficerStorage.ChangeOfficerRank(Steam64, level, branch);
                            player.Message("officer_s_changedrank", characterName, RankData.GetOfficerRankName(level), branch.ToString());
                        }
                    }
                    else
                        player.SendChat("officer_e_invalidbranch", command[2]);
                }
                else
                    player.SendChat("officer_invalidrank", command[2]);

                
            }
            else if (command.Length >= 1 && (command[0].ToLower() == "discharge" || command[0].ToLower() == "disc"))
            {
                if (command.Length < 2)
                {
                    player.SendChat("correct_usage", "/officer discharge <player name>");
                    return;
                }

                UCPlayer target = UCPlayer.FromName(command[1]);
                if (target != null)
                {
                    if (target.IsOfficer)
                    {
                        OfficerStorage.DischargeOfficer(target);
                    }
                    else
                        player.SendChat("officer_e_notofficer", command[1]);
                }
                else
                    player.SendChat("officer_e_playernotfound", command[1]);
            }
            else
                player.SendChat("correct_usage", "/officer <setrank|discharge <player name> <level or rank> <branch>");


        }
    }
}
