using Rocket.API;
using Rocket.Unturned.Player;
using SDG.Unturned;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Uncreated.Warfare.Kits;

namespace Uncreated.Warfare.Squads
{
    public class Command_squad : IRocketCommand
    {
        public AllowedCaller AllowedCaller => AllowedCaller.Player;
        public string Name => "squad";
        public string Help => "Creates or disbands a squad";
        public string Syntax => "/squad";
        public List<string> Aliases => new List<string>();
        public List<string> Permissions => new List<string>() { "uc.squad" };
        public void Execute(IRocketPlayer caller, string[] command)
        {
            UCPlayer player = UCPlayer.FromIRocketPlayer(caller);
            if (!UCWarfare.Config.EnableSquads)
            {
                player.SendChat("squads_disabled");
                return;
            }

            string name = "";
            for (int i = 1; i < command.Length; i++)
            {
                name += command[i];
                if (i < command.Length - 1)
                    name += " ";
            }
            ulong team = player.GetTeam();
            string op = command.Length > 0 ? command[0].ToLower() : string.Empty;
            if (command.Length >= 1 && op == "create")
            {
                if (command.Length < 2)
                {
                    player.SendChat("correct_usage", "/squad create <squad name>");
                    return;
                }
                if (SquadManager.Squads.Count(x => x.Team == team) >= 8)
                {
                    player.SendChat("squad_too_many");
                    return;
                }
                if (!SquadManager.IsInAnySquad(player.CSteamID, out _, out _))
                {
                    string newname = name;
                    ProfanityFilter.filterOutCurseWords(ref newname, '*');
                    if (name != newname || name.Length > SquadManager.config.Data.MaxSquadNameLength)
                    {
                        player.SendChat("squad_no_no_words", name);
                    } else if (!SquadManager.FindSquad(name, team, out Squad squad))
                    {
                        squad = SquadManager.CreateSquad(name, player, team, player.Branch);

                        player.SendChat("squad_created", squad.Name);
                    }
                    else
                        player.SendChat("squad_e_exist", squad.Name);
                }
                else
                    player.SendChat("squad_e_insquad");
            }
            else if (command.Length >= 1 && op == "join")
            {
                if (command.Length < 2)
                {
                    player.SendChat("correct_usage", "/squad join <squad name>");
                    return;
                }

                if (SquadManager.FindSquad(name, team, out Squad squad))
                {
                    if (!SquadManager.IsInAnySquad(player.CSteamID, out _, out _))
                    {
                        if (squad.Members.Count < 6)
                        {
                            if (!squad.IsLocked)
                            {
                                SquadManager.JoinSquad(player, ref squad);
                            }
                            else
                                player.SendChat("squad_e_locked");
                        }
                        else
                            player.SendChat("squad_e_full");
                    }
                    else
                        player.SendChat("squad_e_insquad", name);
                }
                else
                    player.SendChat("squad_e_noexist", name);
            }
            else if (command.Length >= 1 && op == "promote")
            {
                if (command.Length < 2)
                {
                    player.SendChat("correct_usage", "/squad promote <player name>");
                    return;
                }
                if (SquadManager.IsInAnySquad(player.CSteamID, out var squad, out _) && squad?.Leader.CSteamID == player.CSteamID)
                {
                    UCPlayer target = UCPlayer.FromName(name);
                    if (target != null)
                    {
                        if (SquadManager.IsInSquad(target.CSteamID, squad))
                        {
                            SquadManager.PromoteToLeader(squad, target);
                        }
                        else
                            player.SendChat("squad_e_playernotinsquad", name);
                    }
                    else
                        player.SendChat("squad_e_playernotfound", name);
                }
                else 
                    player.SendChat("squad_e_notsquadleader", name);
            }
            else if (command.Length >= 1 && op == "kick")
            {
                if (command.Length < 2)
                {
                    player.SendChat("correct_usage", "/squad kick <player name>");
                    return;
                }
                if (SquadManager.IsInAnySquad(player.CSteamID, out var squad, out _) && squad?.Leader.CSteamID == player.CSteamID)
                {
                    UCPlayer target = UCPlayer.FromName(name);
                    if (target != null)
                    {
                        if (SquadManager.IsInSquad(target.CSteamID, squad))
                        {
                            SquadManager.KickPlayerFromSquad(target, ref squad);
                        }
                        else
                            player.SendChat("squad_e_playernotinsquad", name);
                    }
                    else
                        player.SendChat("squad_e_playernotfound", name);
                }
                else
                    player.SendChat("squad_e_notsquadleader", name);
            }
            else if (command.Length == 1)
            {
                if (op == "leave")
                {
                    if (SquadManager.IsInAnySquad(player.CSteamID, out var squad, out _))
                    {
                        SquadManager.LeaveSquad(player, squad);
                    }
                    else
                        player.SendChat("squad_e_notinsquad");
                }
                else if (op == "disband")
                {
                    if (SquadManager.IsInAnySquad(player.CSteamID, out var squad, out _) && squad?.Leader.CSteamID == player.CSteamID)
                    {
                        SquadManager.DisbandSquad(squad);
                    }
                    else
                        player.SendChat("squad_e_notsquadleader");
                }
                else if (op == "lock" || op == "unlock")
                {
                    if (SquadManager.IsInAnySquad(player.CSteamID, out var squad, out _) && squad?.Leader.CSteamID == player.CSteamID)
                    {
                        if (op == "lock")
                        {
                            SquadManager.SetLocked(ref squad, true);
                            player.SendChat("squad_locked");
                        }
                        else if (op == "unlock")
                        {
                            SquadManager.SetLocked(ref squad, false);
                            player.SendChat("squad_unlocked");
                        }
                    }
                    else
                        player.SendChat("squad_e_notsquadleader");
                }
                else if (op == "testui")
                {
                    SquadManager.UpdateUIMemberCount(player.GetTeam());

                    player.SendChat("squad_ui_reloaded");
                }
            }
            else
                player.SendChat("correct_usage", "/squad <join|create|leave>");
        }
    }
}