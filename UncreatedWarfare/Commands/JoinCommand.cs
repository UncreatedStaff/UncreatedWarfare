using Rocket.API;
using Rocket.Unturned.Player;
using SDG.Unturned;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace UncreatedWarfare.Commands
{
    public class JoinCommand : IRocketCommand
    {
        public AllowedCaller AllowedCaller => AllowedCaller.Player;
        public string Name => "join";
        public string Help => "Join US or Russia";
        public string Syntax => "/join <us|russia>";
        public List<string> Aliases => new List<string>();
        public List<string> Permissions => new List<string>() { "uc.join" };
        public void Execute(IRocketPlayer caller, string[] command)
        {
            SteamPlayer player = ((UnturnedPlayer)caller).Player.channel.owner;

            // TODO
            if(command.Length == 1)
            {
                if(command[0].ToLower() == "us" || command[0].ToLower() == "usa" || command[0].ToLower() == "ru" || command[0].ToLower() == "russia")
                {
                    if (UCWarfare.I.TeamManager.LobbyZone.IsInside(player.player.transform.position))
                    {

                    } else
                    {

                    }
                }
            }
        }
    }
}