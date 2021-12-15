﻿using Rocket.API;
using Rocket.Unturned.Player;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Uncreated.Warfare.Gamemodes.Interfaces;
using Uncreated.Warfare.Teams;

namespace Uncreated.Warfare.Commands
{
    public class UnstuckCommand : IRocketCommand
    {
        public AllowedCaller AllowedCaller => AllowedCaller.Player;
        public string Name => "unstuck";
        public string Help => "Unstucks you from lobby.";
        public string Syntax => "/unstuck";
        public List<string> Aliases => new List<string>();
        public List<string> Permissions => new List<string>() { "uc.unstuck" };
        public void Execute(IRocketPlayer caller, string[] command)
        {
            if (caller is UnturnedPlayer player)
            {
                UCPlayer ucplayer = UCPlayer.FromUnturnedPlayer(player);
                if (ucplayer == null) return;

                if (Data.Is(out ITeams t) && TeamManager.LobbyZone.IsInside(ucplayer.Position))
                {
                    t.JoinManager.CloseUI(ucplayer);
                    t.JoinManager.OnPlayerDisconnected(ucplayer);
                    t.JoinManager.OnPlayerConnected(ucplayer, false, false);
                }
            }
        }
    }
}