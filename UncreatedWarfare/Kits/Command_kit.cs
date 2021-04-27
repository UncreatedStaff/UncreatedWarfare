using Rocket.API;
using Rocket.Unturned.Player;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace UncreatedWarfare.Kits
{
    public class Command_kit : IRocketCommand
    {
        public AllowedCaller AllowedCaller => AllowedCaller.Player;
        public string Name => "kit";
        public string Help => "creates, renames or deletes a kit";
        public string Syntax => "/kit";
        public List<string> Aliases => new List<string>() { "kit" };
        public List<string> Permissions => new List<string>() { "kit" };
        public KitManager KitManager => UCWarfare.I.KitManager;
        public void Execute(IRocketPlayer caller, string[] command)
        {
            UnturnedPlayer player = (UnturnedPlayer)caller;

            if (command.Length == 2)
            {
                // create kit
                if (command[0].ToLower() == "create" || command[0].ToLower() == "c")
                {
                    if (!KitManager.KitExists(command[1].ToLower(), out var kit))
                    {
                        KitManager.CreateKit(player, command[1]);
                        return;
                    }
                    else // overwrite kit
                    {
                        KitManager.OverwriteKitItems(kit.Name, KitManager.ItemsFromInventory(player), KitManager.ClothesFromInventory(player));

                        return;
                    }
                }
            }


            if (command.Length == 0)
            {

            }
            if (command.Length == 1)
            {

            }
            
        }
    }
}
