using Rocket.API;
using Rocket.Unturned.Player;
using System.Collections.Generic;
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
        public List<string> Aliases => new List<string>(0);
        public List<string> Permissions => new List<string>(1) { "uc.unstuck" };
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
                    t.JoinManager.OnPlayerConnected(ucplayer, false);
                }
            }
        }
    }
}
