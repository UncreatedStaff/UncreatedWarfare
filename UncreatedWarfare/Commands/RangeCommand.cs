using Rocket.API;
using System.Collections.Generic;
using Uncreated.Warfare.Gamemodes.Interfaces;
using UnityEngine;

namespace Uncreated.Warfare.Commands
{
    public class RangeCommand : IRocketCommand
    {
        public AllowedCaller AllowedCaller => AllowedCaller.Player;
        public string Name => "range";
        public string Help => "shows you the range to your marker";
        public string Syntax => "/range";
        private readonly List<string> _aliases = new List<string>(0);
        public List<string> Aliases => _aliases;
        private readonly List<string> _permissions = new List<string>(1) { "uc.range" };
		public List<string> Permissions => _permissions;
        public void Execute(IRocketPlayer caller, string[] command)
        {
            UCPlayer? player = UCPlayer.FromIRocketPlayer(caller);
            if (player == null) return;
            if (!Data.Is<ISquads>(out _))
            {
                int distance = Mathf.RoundToInt((player.Position - player.Player.quests.markerPosition).magnitude / 10) * 10;
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
