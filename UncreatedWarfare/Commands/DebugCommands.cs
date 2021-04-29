using Rocket.API;
using Rocket.Unturned.Player;
using SDG.Unturned;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UncreatedWarfare.Flags;
using Flag = UncreatedWarfare.Flags.Flag;

namespace UncreatedWarfare.Commands
{
    internal class ZoneCommand : IRocketCommand
    {
        public AllowedCaller AllowedCaller => AllowedCaller.Player;

        public string Name => "zone";

        public string Help => "Get the current zone the player is in if any";

        public string Syntax => "/zone";

        public List<string> Aliases => new List<string>();

        public List<string> Permissions => new List<string> { "uc.zone" };

        public void Execute(IRocketPlayer caller, string[] command)
        {
            Player player = (caller as UnturnedPlayer).Player;
            Flag flag = UCWarfare.I.FlagManager.FlagRotation.FirstOrDefault(f => f.PlayerInRange(player));
            if(flag == null)
            {
                player.SendChat("not_in_zone", UCWarfare.I.Colors["default"], player.transform.position.x, player.transform.position.y, player.transform.position.z, UCWarfare.I.FlagManager.FlagRotation.Count);
            } else
            {
                player.SendChat("current_zone", UCWarfare.I.Colors["default"], flag.Name, player.transform.position.x, player.transform.position.y, player.transform.position.z);
            }
        }
    }
}
