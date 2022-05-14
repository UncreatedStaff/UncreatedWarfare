using Rocket.API;
using System;
using System.Collections.Generic;
using System.Linq;
using Uncreated.Players;
using Uncreated.Warfare.Point;

namespace Uncreated.Warfare.Commands
{
    public class OfficerCommand : IRocketCommand
    {
        public AllowedCaller AllowedCaller => AllowedCaller.Player;
        public string Name => "officer";
        public string Help => "promotes or demotes a player to an officer rank";
        public string Syntax => "/officer";
        private readonly List<string> _aliases = new List<string>(0);
        public List<string> Aliases => _aliases;
        private readonly List<string> _permissions = new List<string>() { "uc.officer" };
		public List<string> Permissions => _permissions;
        public void Execute(IRocketPlayer caller, string[] command)
        {
#if DEBUG
            using IDisposable profiler = ProfilingUtils.StartTracking();
#endif
            UCPlayer? player = UCPlayer.FromIRocketPlayer(caller);
            if (player == null) return;

            if (command.Length >= 1 && (command[0].ToLower() == "setrank" || command[0].ToLower() == "set"))
            {
                if (command.Length < 3)
                {
                    player.SendChat("correct_usage", "/officer promote <player name> <level or rank> <branch>");
                    return;
                }

                UCPlayer? target = UCPlayer.FromName(command[1]);
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
                    if (ulong.TryParse(command[3], out ulong team) && team == 1 || team == 2)
                    {
                        if (target != null)
                        {
                            ActionLog.Add(EActionLogType.SET_OFFICER_RANK, target.Steam64.ToString(Data.Locale) + " to " + level + " on team " + team, player);
                            OfficerStorage.ChangeOfficerRank(target.Steam64, level, team);
                            ref Ranks.RankData data = ref Ranks.RankManager.GetRank(target, out _);
                            player.Message("officer_s_changedrank", target.CharacterName, data.GetName(player.Steam64), Translation.Translate(team.ToString(), player));
                        }
                        else
                        {
                            ActionLog.Add(EActionLogType.SET_OFFICER_RANK, Steam64.ToString(Data.Locale) + " to " + level + " on team " + team, player);
                            OfficerStorage.ChangeOfficerRank(Steam64, level, team);
                            player.Message("officer_s_changedrank", characterName, RankDataOLD.GetOfficerRankName(level), Translation.Translate(team.ToString(), player));
                        }
                    }
                    else
                        player.SendChat("officer_e_team", command[2]);
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

                UCPlayer? target = UCPlayer.FromName(command[1]);
                string characterName = string.Empty;
                if (ulong.TryParse(command[1], out ulong Steam64) && Data.DatabaseManager.PlayerExistsInDatabase(Steam64, out FPlayerName names))
                {
                    characterName = names.CharacterName;
                    goto DischargePlayer;
                }
                else if (target != null)
                {
                    goto DischargePlayer;
                }
                else
                    player.SendChat("officer_e_playernotfound", command[1]);

                DischargePlayer:
                if (target != null)
                {
                    ActionLog.Add(EActionLogType.DISCHARGE_OFFICER, target.Steam64.ToString(Data.Locale), player);
                    OfficerStorage.DischargeOfficer(target.Steam64);
                    player.Message("officer_s_discharged", target.CharacterName);
                }
            }
            else
                player.SendChat("correct_usage", "/officer <setrank|discharge <player name> <level or rank> <branch>");
        }
    }
}
