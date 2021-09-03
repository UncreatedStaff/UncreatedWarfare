using Rocket.API;
using System;
using System.Collections.Generic;
using System.Linq;
using Uncreated.Warfare.Kits;
using Uncreated.Warfare.Officers;
using Uncreated.Warfare.XP;

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
                if (target != null)
                {
                    Rank rank = OfficerManager.config.Data.OfficerRanks.FirstOrDefault(r => r.name.Replace(" ", "").ToLower().Contains(command[2].ToLower()));

                    if (rank is null)
                    {
                        if (int.TryParse(command[2], System.Globalization.NumberStyles.Any, Data.Locale, out var level))
                        {
                            rank = OfficerManager.config.Data.OfficerRanks.FirstOrDefault(r => r.level == level);
                        }
                    }

                    if (rank != null)
                    {
                        if (Enum.TryParse<EBranch>(command[3], out EBranch branch))
                        {
                            OfficerManager.ChangeOfficerRank(target, rank, branch);
                            player.OfficerRank = rank;
                            PlayerManager.Save();
                            XPManager.UpdateUI(target.Player, target.CachedXp, out _);
                        }
                        else
                            player.SendChat("officer_branchnotfound", command[2]);
                    }
                    else
                        player.SendChat("officer_ranknotfound", command[2]);
                }
                else
                    player.SendChat("officer_playernotfound", command[1]);
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
                    if (target.OfficerRank != null)
                    {
                        OfficerManager.DischargeOfficer(target, target.OfficerRank);
                        player.OfficerRank = null;
                        PlayerManager.Save();
                        XPManager.UpdateUI(target.Player, target.CachedXp, out _);
                    }
                    else
                        player.SendChat("officer_notofficer", command[1]);
                }
                else
                    player.SendChat("officer_playernotfound", command[1]);
            }
            else
                player.SendChat("correct_usage", "/officer <setrank|discharge <player name> <level or rank> <branch>");
        }
    }
}
