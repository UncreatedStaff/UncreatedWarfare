using Rocket.API;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Uncreated.Warfare.Teams
{
    class Command_teams : IRocketCommand
    {
        public AllowedCaller AllowedCaller => AllowedCaller.Player;
        public string Name => "teams";
        public string Help => "Pull up the Teams UI";
        public string Syntax => "/teams";
        public List<string> Aliases => new List<string>();
        public List<string> Permissions => new List<string>() { "uc.teams" };
        public void Execute(IRocketPlayer caller, string[] command)
        {
            UCPlayer player = UCPlayer.FromIRocketPlayer(caller);

            if (!(player.GetTeam() == 1 || player.GetTeam() == 2) && !player.Player.IsInMain())
            {
                player.SendChat("teams_e_notinmain");
                return;
            }

            Data.JoinManager.JoinLobby(player, true);
            
        }
    }
}
