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
        public List<string> Permissions => new List<string>() { "squad" };
        public void Execute(IRocketPlayer caller, string[] command)
        {
            UnturnedPlayer player = (UnturnedPlayer)caller;

            string name = "";
            for (int i = 1; i < command.Length; i++)
            {
                name += command[i];
                if (i < command.Length - 1)
                    name += " ";
            }

            if (command.Length >= 1 && command[0].ToLower() == "create")
            {
                if (command.Length < 2)
                {
                    player.Message("correct_usage", "/squad create <squad name>");
                    return;
                }

                if (!SquadManager.IsInAnySquad(player.CSteamID, out _))
                {
                    if (!SquadManager.FindSquad(name, player.GetTeam(), out var squad))
                    {
                        SquadManager.CreateSquad(name, player, player.GetTeam(), PlayerManager.GetPlayerData(player.CSteamID).Branch);

                        player.Message("squad_created", name);
                    }
                    else
                        player.Message("squad_e_exist", name);
                }
                else
                    player.Message("squad_e_insquad");
            }
            else if (command.Length >= 1 && command[0].ToLower() == "join")
            {
                if (command.Length < 2)
                {
                    player.Message("correct_usage", "/squad join <squad name>");
                    return;
                }

                if (SquadManager.FindSquad(name, player.GetTeam(), out var squad))
                {
                    if (!SquadManager.IsInAnySquad(player.CSteamID, out _))
                    {
                        if (squad.Members.Count < 6)
                        {
                            if (!squad.IsLocked)
                            {
                                SquadManager.JoinSquad(player, ref squad);

                                player.Message("squad_joined", squad.Name);
                            }
                            else
                                player.Message("squad_e_locked");
                        }
                        else
                            player.Message("squad_e_full");
                    }
                    else
                        player.Message("squad_e_insquad", name);
                }
                else
                    player.Message("A squad called '{0}' could not be found.", name);
            }
            else if (command.Length >= 1 && command[0].ToLower() == "promote")
            {
                if (command.Length < 2)
                {
                    player.Message("correct_usage", "/squad promote <player name>");
                    return;
                }
                if (SquadManager.IsInAnySquad(player.CSteamID, out var squad) && squad?.Leader.CSteamID == player.CSteamID)
                {
                    UnturnedPlayer target = UnturnedPlayer.FromName(name);
                    if (target != null)
                    {
                        if (squad.Members.Exists(p => p.CSteamID == target.CSteamID))
                        {
                            SquadManager.PromoteToLeader(ref squad, target);
                        }
                        else
                            player.Message("squad_e_playernotinsquad", name);
                    }
                    else
                        player.Message("squad_e_playernotfound", name);
                }
                else
                    player.Message("squad_e_notsquadleader", name);
            }
            else if (command.Length == 1)
            {
                if (command[0].ToLower() == "leave")
                {
                    if (SquadManager.IsInAnySquad(player.CSteamID, out var squad))
                    {
                        SquadManager.LeaveSquad(player, ref squad);

                        player.Message("squad_left");
                    }
                    else
                        player.Message("squad_e_notinsquad");
                }
                else if (command[0].ToLower() == "disband")
                {
                    if (SquadManager.IsInAnySquad(player.CSteamID, out var squad) && squad?.Leader.CSteamID == player.CSteamID)
                    {
                        SquadManager.DisbandSquad(squad);
                    }
                    else
                        player.Message("squad_e_notsquadleader");
                }
                else if (command[0].ToLower() == "lock" || command[0].ToLower() == "unlock")
                {
                    if (SquadManager.IsInAnySquad(player.CSteamID, out var squad) && squad?.Leader.CSteamID == player.CSteamID)
                    {
                        if (command[0].ToLower() == "lock")
                        {
                            SquadManager.SetLocked(ref squad, true);
                            player.Message("squad_locked");
                        }
                        else if (command[0].ToLower() == "unlock")
                        {
                            SquadManager.SetLocked(ref squad, false);
                            player.Message("squad_unlocked");
                        }
                    }
                    else
                        player.Message("squad_e_notsquadleader");
                }
                else if (command[0].ToLower() == "testui")
                {
                    SquadManager.UpdateUIForTeam(player.GetTeam());

                    player.Message("Squad UI has been reloaded.");
                }
            }
            else
                player.Message("correct_usage", "/squad <join|create|leave>");
        }
    }
}