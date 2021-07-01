using Rocket.API;
using Rocket.Unturned.Player;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Uncreated.Warfare.Commands
{
    public class Command_blank : IRocketCommand
    {
        public AllowedCaller AllowedCaller => AllowedCaller.Player;
        public string Name => "range";
        public string Help => "shows you the range to your marker";
        public string Syntax => "/range";
        public List<string> Aliases => new List<string>();
        public List<string> Permissions => new List<string>() { "range" };
        public void Execute(IRocketPlayer caller, string[] command)
        {
            UCPlayer player = UCPlayer.FromIRocketPlayer(caller);

            if (player.Squad != null && player.Squad.Leader.Steam64 == player.Steam64)
            {
                if (player.Player.quests.isMarkerPlaced)
                {
                    int distance = (int)Math.Round((player.Position - player.Player.quests.markerPosition).magnitude / 10) * 10;

                    player.Message("range", distance);
                }
                else
                {
                    player.Message("range_nomarker");
                }
            }
            else
                player.Message("range_notsquadleader");
        }
    }
}
