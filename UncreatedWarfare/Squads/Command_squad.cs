using Rocket.API;
using System;
using System.Collections.Generic;
using System.Linq;
using Uncreated.Warfare.Gamemodes.Interfaces;

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
#if DEBUG
            using IDisposable profiler = ProfilingUtils.StartTracking();
#endif
            UCPlayer? player = UCPlayer.FromIRocketPlayer(caller);
            if (player == null) return;
            if (!Data.Is(out ISquads ctf))
            {
                player.SendChat("command_e_gamemode");
                return;
            }
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
                if (SquadManager.Squads.Count(x => x.Team == team) >= 8)
                {
                    player.SendChat("squad_too_many");
                    return;
                }
                if (player.Squad == null)
                {
                    Squad squad = SquadManager.CreateSquad(player, team, player.Branch);

                    player.SendChat("squad_created", squad.Name);
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
                    if (player.Squad == null)
                    {
                        if (squad.Members.Count < 6)
                        {
                            if (!squad.IsLocked)
                            {
                                SquadManager.JoinSquad(player, squad);
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
                if (player.Squad != null && player.Squad.Leader.CSteamID.m_SteamID == player.CSteamID.m_SteamID)
                {
                    UCPlayer? target = UCPlayer.FromName(name, true);
                    if (target != null)
                    {
                        if (target.Squad == player.Squad)
                        {
                            SquadManager.PromoteToLeader(player.Squad, target);
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
                if (player.Squad != null && player.Squad.Leader.CSteamID.m_SteamID == player.CSteamID.m_SteamID)
                {
                    UCPlayer? target = UCPlayer.FromName(name);
                    if (target != null)
                    {
                        if (target.Squad == player.Squad)
                        {
                            SquadManager.KickPlayerFromSquad(target, player.Squad);
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
                    if (player.Squad != null)
                    {
                        SquadManager.LeaveSquad(player, player.Squad);
                    }
                    else
                        player.SendChat("squad_e_notinsquad");
                }
                else if (op == "disband")
                {
                    if (player.Squad != null && player.Squad.Leader.CSteamID.m_SteamID == player.CSteamID.m_SteamID)
                    {
                        SquadManager.DisbandSquad(player.Squad);
                    }
                    else
                        player.SendChat("squad_e_notsquadleader");
                }
                else if (op == "lock" || op == "unlock")
                {
                    if (player.Squad != null && player.Squad.Leader.CSteamID.m_SteamID == player.CSteamID.m_SteamID)
                    {
                        if (op == "lock")
                        {
                            SquadManager.SetLocked(player.Squad, true);
                            player.SendChat("squad_locked");
                        }
                        else if (op == "unlock")
                        {
                            SquadManager.SetLocked(player.Squad, false);
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