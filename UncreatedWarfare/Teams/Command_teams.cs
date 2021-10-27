﻿using Rocket.API;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Uncreated.Warfare.Gamemodes;

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

            if (!Data.TryMode(out TeamGamemode teamgm))
            {
                player.SendChat("command_e_gamemode");
                return;
            }
            if (!player.Player.IsInMain())
            {
                player.SendChat("teams_e_notinlobby");
                return;
            }

            teamgm.JoinManager.ShowUI(player, true);
            
        }
    }
}
