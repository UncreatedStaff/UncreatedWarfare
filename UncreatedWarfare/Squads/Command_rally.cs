
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
                            player.Message("You have rallied with your squad.");
                        }
                        else
                        {
                            if (!rallypoint.AwaitingPlayers.Exists(p => p.Steam64 == player.Steam64))
                            {
                                rallypoint.AwaitingPlayers.Add(player);
                                player.Message($"<color=#89917e>Standby for rally in <color=#5eff87>{rallypoint.timer}</color> seconds. Do '<color=#bfbfbf>/rally cancel</color>' to abort.</color>");
                            }
                            else
                            {
                                player.Message("You are already awaiting <color=#5eff87>rally</color> deployment. Do '/rally cancel' to abort.");
                            }
                        }
                    }
                    else if (command.Length == 1 && command[0].ToLower() == "cancel" || command[0].ToLower() == "c" || command[0].ToLower() == "abort")
                    {
                        if (rallypoint.AwaitingPlayers.Exists(p => p.Steam64 == player.Steam64))
                        {
                            rallypoint.AwaitingPlayers.RemoveAll(p => p.Steam64 == player.Steam64);
                            player.Message($"<color=#89917e>Cancelled rally deployment.</color>");
                        }
                        else
                        {
                            player.Message("You are not awaiting deployment.");
                        }
                    }
                }
                else
                {
                    player.Message("Rallypoint is unavailable right now");
                }
            }
            else
            {
                player.Message("You are not in squad.");
            }
        }
    }
}
