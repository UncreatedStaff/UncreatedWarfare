using Rocket.API;
using System.Collections.Generic;
using Uncreated.Warfare.Gamemodes.Interfaces;
using UnityEngine;

namespace Uncreated.Warfare.Commands
{
    public class Command_blank : IRocketCommand
    {
        public AllowedCaller AllowedCaller => AllowedCaller.Player;
        public string Name => "range";
        public string Help => "shows you the range to your marker";
        public string Syntax => "/range";
        public List<string> Aliases => new List<string>();
        public List<string> Permissions => new List<string>() { "uc.range" };
        public void Execute(IRocketPlayer caller, string[] command)
        {
            UCPlayer player = UCPlayer.FromIRocketPlayer(caller);
            if (!Data.Is(out ISquads ctf))
            {
                int distance = Mathf.RoundToInt((player.Position - player.Squad.Leader.Player.quests.markerPosition).magnitude / 10) * 10;
                player.Message("range", distance.ToString(Data.Locale));
                return;
            }
            if (player.Squad != null)
            {
                if (player.Squad.Leader.Player.quests.isMarkerPlaced)
                {
                    int distance = Mathf.RoundToInt((player.Position - player.Squad.Leader.Player.quests.markerPosition).magnitude / 10) * 10;
                    player.Message("range", distance.ToString(Data.Locale));
                }
                else
                {
                    player.Message("range_nomarker");
                }
            }
            else
                player.Message("range_notinsquad");
        }
    }
}
