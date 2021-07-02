using Rocket.API;
using Rocket.Unturned.Player;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Uncreated.Warfare.Components;
using Uncreated.Warfare.FOBs;
using Uncreated.Warfare.Teams;
using UnityEngine;

namespace Uncreated.Warfare.Commands
{
    public class Command_deploy : IRocketCommand
    {
        public AllowedCaller AllowedCaller => AllowedCaller.Player;
        public string Name => "deploy";
        public string Help => "deploys you to a nearby FOB";
        public string Syntax => "/deploy";
        public List<string> Aliases => new List<string>() { "deploy" };
        public List<string> Permissions => new List<string>() { "deploy" };
        public void Execute(IRocketPlayer caller, string[] command)
        {
            UCPlayer player = UCPlayer.FromIRocketPlayer(caller);

            if (command.Length == 1)
            {
                PlaytimeComponent c = F.GetPlaytimeComponent(player.Player, out _);

                bool shouldCancelOnMove = !player.Player.IsInMain();
                bool shouldCancelOnDamage = !player.Player.IsInMain();

                ulong team = player.GetTeam();

                if (command[0].ToLower() == "main")
                {
                    c.TeleportDelayed(team.GetBaseSpawn(), team.GetBaseAngle(), FOBManager.config.data.DeloyMainDelay, shouldCancelOnMove, shouldCancelOnDamage, true, "<color=#d1b780>main</color>");
                }
                else if (command[0].ToLower() == "lobby")
                {
                    c.TeleportDelayed(TeamManager.LobbySpawn, TeamManager.LobbySpawnAngle, FOBManager.config.data.DeloyMainDelay, shouldCancelOnMove, shouldCancelOnDamage, true, "<color=#bb80d1>lobby</color>");
                }
                else
                {
                    if (FOBManager.FindFOBByName(command[0], player.GetTeam(), out var FOB))
                    {
                        c.TeleportDelayed(FOB.Structure.point, 0, FOBManager.config.data.DeloyMainDelay, shouldCancelOnMove, shouldCancelOnDamage, true, $"<color=#54e3ff>{FOB.Name}</color>", FOB);
                    }
                    else
                    {
                        player.Message("deploy_e_fobnoexist", command[0]);
                    }
                }
            }
            else
            {
                player.Message("correct_usage", "/deploy main -OR- /deploy <fob name>");
            }
        }
    }
}