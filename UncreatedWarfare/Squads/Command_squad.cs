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

            if (command.Length >= 2)
            {
                string squadName = "";
                for (int i = 1; i < command.Length; i++)
                {
                    squadName += command[i];
                    if (i < command.Length - 1)
                        squadName += " ";
                }

                if (command[0].ToLower() == "create")
                {
                    if (!SquadManager.IsInAnySquad(player.CSteamID, out _))
                    {
                        if (!SquadManager.FindSquad(squadName, player.GetTeam(), out var squad))
                        {
                            SquadManager.CreateSquad(squadName, player, player.GetTeam(), LogoutSaver.GetSave(player.CSteamID).Branch);

                            player.Message("You created the squad: {0}", squadName);
                        }
                        else
                            player.Message("A squad with a similar name to {0} already exists.", squadName);
                    }
                    else
                        player.Message("You are already in a squad! Leave it before you create a new one.", squadName);
                }
                if (command[0].ToLower() == "join")
                {
                    if (SquadManager.FindSquad(squadName, player.GetTeam(), out var squad))
                    {
                        if (!SquadManager.IsInAnySquad(player.CSteamID, out _))
                        {
                            if (squad.Members.Count < 6)
                            {
                                if (!squad.IsLocked)
                                {
                                    SquadManager.JoinSquad(player, ref squad);

                                    player.Message("You joined the squad: {0}", squad.Name);
                                }
                                else
                                    player.Message("That squad is locked. Try again later.");
                            }
                            else
                                player.Message("That squad is full. Try again later.");
                        }
                        else
                            player.Message("You are already in a squad! Leave it before you join a new one.", squadName);
                    }
                    else
                        player.Message("A squad called '{0}' could not be found.", squadName);
                }
                if (command[0].ToLower() == "promote")
                {
                    if (SquadManager.IsInAnySquad(player.CSteamID, out var squad) && squad?.Leader.CSteamID == player.CSteamID)
                    {
                        UnturnedPlayer target = UnturnedPlayer.FromName(squadName);
                        if (target != null)
                        {
                            if (squad.Members.Exists(p => p.CSteamID == target.CSteamID))
                            {
                                SquadManager.PromoteToLeader(ref squad, target);
                            }
                            else
                                player.Message("That player is not in you squad.", squadName);
                        }
                        else
                            player.Message("Could not find player: {0}", squadName);
                    }
                    else
                        player.Message("You are not the leader of any squad!", squadName);
                }
                else
                {

                }
            }
            if (command.Length == 1)
            {
                if (command[0].ToLower() == "leave")
                {
                    if (SquadManager.IsInAnySquad(player.CSteamID, out var squad))
                    {
                        SquadManager.LeaveSquad(player, ref squad);

                        player.Message("You left your squad.");
                    }
                    else
                        player.Message("You are not in any squad!");
                }
                else if (command[0].ToLower() == "disband")
                {
                    if (SquadManager.IsInAnySquad(player.CSteamID, out var squad) && squad?.Leader.CSteamID == player.CSteamID)
                    {
                        SquadManager.DisbandSquad(squad);
                    }
                    else
                        player.Message("You are not the leader of a squad!");
                }
                else if (command[0].ToLower() == "lock" || command[0].ToLower() == "unlock")
                {
                    if (SquadManager.IsInAnySquad(player.CSteamID, out var squad) && squad?.Leader.CSteamID == player.CSteamID)
                    {
                        if (command[0].ToLower() == "lock")
                        {
                            SquadManager.SetLocked(ref squad, true);
                            player.Message("You locked your squad.");
                        }
                        if (command[0].ToLower() == "unlock")
                        {
                            SquadManager.SetLocked(ref squad, false);
                            player.Message("You unlocked your squad.");
                        }
                    }
                    else
                        player.Message("You are not the leader of a squad!");
                }
                else if (command[0].ToLower() == "testui")
                {
                    SquadManager.UpdateUIForTeam(player.GetTeam());

                    player.Message("Squad UI has been reload.");
                }
            }
        }
    }
}