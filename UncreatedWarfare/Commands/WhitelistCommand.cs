using Rocket.API;
using Rocket.Unturned.Player;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Uncreated.Warfare.Commands
{
    public class Command_whitelist : IRocketCommand
    {
        public AllowedCaller AllowedCaller => AllowedCaller.Player;
        public string Name => "whitelist";
        public string Help => "Whitelists items";
        public string Syntax => "/whitelist";
        public List<string> Aliases => new List<string>() { "wh" };
        public List<string> Permissions => new List<string>() { "uc.whitelist" };
        public void Execute(IRocketPlayer caller, string[] arguments)
        {
            UnturnedPlayer player = (UnturnedPlayer)caller;
            if (arguments.Length == 2)
            {
                if (arguments[0].ToLower() == "add")
                {
                    if (UInt16.TryParse(arguments[1], out var itemID))
                    {
                        if (!Whitelister.IsWhitelisted(itemID, out _))
                        {
                            Whitelister.AddItem(itemID);
                            player.Message("whitelist_added", arguments[1]);
                        }
                        else
                            player.Message("whitelist_e_exist", arguments[1]);
                    }
                    else
                        player.Message("whitelist_e_invalidid", arguments[1]);
                }
                if (arguments[0].ToLower() == "remove")
                {
                    if (UInt16.TryParse(arguments[1], out var itemID))
                    {
                        if (Whitelister.IsWhitelisted(itemID, out _))
                        {
                            Whitelister.RemoveItem(itemID);
                            player.Message("whitelist_removed", arguments[1]);
                        }
                        else
                            player.Message("whitelist_e_noexist", arguments[1]);
                    }
                    else
                        player.Message("whitelist_e_invalidid", arguments[1]);
                }
                else
                    player.Message("correct_usage", "/whitelist <add|remove|set>");
            }
            else if (arguments.Length == 4)
            {
                if (arguments[0].ToLower() == "set")
                {
                    if (arguments[1].ToLower() == "maxamount" || arguments[1].ToLower() == "a")
                    {
                        if (UInt16.TryParse(arguments[2], out var itemID))
                        {
                            if (UInt16.TryParse(arguments[3], out var amount))
                            {
                                if (Whitelister.IsWhitelisted(itemID, out _))
                                {
                                    Whitelister.SetAmount(itemID, amount);
                                    player.Message("whitelist_removed", arguments[2]);
                                }
                                else
                                    player.Message("whitelist_e_noexist", arguments[2]);
                            }
                            else
                                player.Message("whitelist_e_invalidamount", arguments[3]);
                        }
                        else
                            player.Message("whitelist_e_invalidid", arguments[2]);
                    }
                    else
                        player.Message("correct_usage", "/whitelist set <amount|salvagable> <value>");
                }
                else
                    player.Message("correct_usage", "/whitelist <add|remove|set>");
            }
            else
                player.Message("correct_usage", "/whitelist <add|remove|set>");
        }
    }
}