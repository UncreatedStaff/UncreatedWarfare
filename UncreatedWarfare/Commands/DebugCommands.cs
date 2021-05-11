using Rocket.API;
using Rocket.Unturned.Player;
using SDG.Unturned;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UncreatedWarfare.Flags;
using UncreatedWarfare.FOBs;
using Flag = UncreatedWarfare.Flags.Flag;

namespace UncreatedWarfare.Commands
{
    internal class ZoneCommand : IRocketCommand
    {
        public AllowedCaller AllowedCaller => AllowedCaller.Player;

        public string Name => "test";

        public string Help => "Get the current zone the player is in if any";

        public string Syntax => "/test <mode>";

        public List<string> Aliases => new List<string>();

        public List<string> Permissions => new List<string> { "uc.test" };

        public void Execute(IRocketPlayer caller, string[] command)
        {
            Player player = (caller as UnturnedPlayer).Player;
            if(command.Length > 0)
            {
                if(command[0] == "zone")
                {
                    Flag flag = UCWarfare.I.FlagManager.FlagRotation.FirstOrDefault(f => f.PlayerInRange(player));
                    if (flag == null)
                    {
                        player.SendChat("not_in_zone", UCWarfare.GetColor("default"), player.transform.position.x, player.transform.position.y, player.transform.position.z, UCWarfare.I.FlagManager.FlagRotation.Count);
                    }
                    else
                    {
                        player.SendChat("current_zone", UCWarfare.GetColor("default"), flag.Name, player.transform.position.x, player.transform.position.y, player.transform.position.z);
                    }
                } else if (command[0] == "sign")
                {
                    InteractableSign sign = BuildManager.GetInteractableFromLook<InteractableSign>(player.look);
                    if (sign == null) player.SendChat("No sign found.", UCWarfare.GetColor("default"));
                    else
                    {
                        player.SendChat("Sign text: \"" + sign.text + '\"', UCWarfare.GetColor("default"));
                        CommandWindow.Log("Sign text: \"" + sign.text + '\"');
                    }
                }
            }
        }
    }
}
