using Rocket.API;
using Rocket.Unturned.Player;
using System;
using System.Collections.Generic;

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
            if (!Data.Gamemode.UseWhitelist)
            {
                player.SendChat("command_e_gamemode");
                return;
            }
            if (arguments.Length == 2)
            {
                if (arguments[0].ToLower() == "add")
                {
                    if (UInt16.TryParse(arguments[1], System.Globalization.NumberStyles.Any, Data.Locale, out var itemID))
                    {
                        if (!Whitelister.IsWhitelisted(itemID, out _))
                        {
                            Whitelister.AddItem(itemID);
                            player.SendChat("whitelist_added", arguments[1]);
                        }
                        else
                            player.SendChat("whitelist_e_exist", arguments[1]);
                    }
                    else
                        player.SendChat("whitelist_e_invalidid", arguments[1]);
                }
                else if (arguments[0].ToLower() == "remove")
                {
                    if (UInt16.TryParse(arguments[1], System.Globalization.NumberStyles.Any, Data.Locale, out var itemID))
                    {
                        if (Whitelister.IsWhitelisted(itemID, out _))
                        {
                            Whitelister.RemoveItem(itemID);
                            player.SendChat("whitelist_removed", arguments[1]);
                        }
                        else
                            player.SendChat("whitelist_e_noexist", arguments[1]);
                    }
                    else
                        player.SendChat("whitelist_e_invalidid", arguments[1]);
                }
                else
                    player.SendChat("correct_usage", "/whitelist <add|remove|set>");
            }
            else if (arguments.Length == 4)
            {
                if (arguments[0].ToLower() == "set")
                {
                    if (arguments[1].ToLower() == "maxamount" || arguments[1].ToLower() == "a")
                    {
                        if (UInt16.TryParse(arguments[2], System.Globalization.NumberStyles.Any, Data.Locale, out var itemID))
                        {
                            if (UInt16.TryParse(arguments[3], System.Globalization.NumberStyles.Any, Data.Locale, out var amount))
                            {
                                if (Whitelister.IsWhitelisted(itemID, out _))
                                {
                                    Whitelister.SetAmount(itemID, amount);
                                    player.SendChat("whitelist_removed", arguments[2]);
                                }
                                else
                                    player.SendChat("whitelist_e_noexist", arguments[2]);
                            }
                            else
                                player.SendChat("whitelist_e_invalidamount", arguments[3]);
                        }
                        else
                            player.SendChat("whitelist_e_invalidid", arguments[2]);
                    }
                    else
                        player.SendChat("correct_usage", "/whitelist set <amount|salvagable> <value>");
                }
                else
                    player.SendChat("correct_usage", "/whitelist <add|remove|set>");
            }
            else
                player.SendChat("correct_usage", "/whitelist <add|remove|set>");
        }
    }
}