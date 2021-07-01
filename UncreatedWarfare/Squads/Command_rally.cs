
using Rocket.API;
using Rocket.Unturned.Player;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Uncreated.Warfare.Squads;

namespace Uncreated.Warfare.Commands
{
    public class Command_rally : IRocketCommand
    {
        public AllowedCaller AllowedCaller => AllowedCaller.Player;
        public string Name => "rally";
        public string Help => "Deploys you to a rallypoint";
        public string Syntax => "/rally";
        public List<string> Aliases => new List<string>();
        public List<string> Permissions => new List<string>() { "rally" };
        public void Execute(IRocketPlayer caller, string[] command)
        {
            UCPlayer player = UCPlayer.FromIRocketPlayer(caller);

            if (player.Squad != null)
            {
                if (RallyManager.HasRally(player, out var rallypoint) && rallypoint.IsActive)
                {
                    if (command.Length == 0)
                    {
                        if (rallypoint.timer <= 0)
                        {
                            rallypoint.TeleportPlayer(player);
                        }
                        else
                        {
                            if (!rallypoint.AwaitingPlayers.Exists(p => p.Steam64 == player.Steam64))
                            {
                                rallypoint.AwaitingPlayers.Add(player);
                                player.Message("rally_wait", rallypoint.timer);
                            }
                            else
                            {
                                player.Message("really_e_alreadywaiting");
                            }
                        }
                    }
                    else if (command.Length == 1 && command[0].ToLower() == "cancel" || command[0].ToLower() == "c" || command[0].ToLower() == "abort")
                    {
                        if (rallypoint.AwaitingPlayers.Exists(p => p.Steam64 == player.Steam64))
                        {
                            rallypoint.AwaitingPlayers.RemoveAll(p => p.Steam64 == player.Steam64);
                            player.Message("rally_aborted");
                        }
                        else
                        {
                            player.Message("rally_e_notwaiting");
                        }
                    }
                }
                else
                {
                    player.Message("rally_e_unavailable");
                }
            }
            else
            {
                player.Message("rally_e_notinsquad");
            }
        }
    }
}
