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

            string op = "";
            string property = "";
            string kitName = "";
            string newValue = "";
            string targetPlayer = "";

            if (command.Length == 2)
            {
                // create kit
                if (op.ToLower() == "create" || op.ToLower() == "c")
                {
                    if (!KitManager.KitExists(kitName, out var kit)) // create kit
                    {
                        KitManager.CreateKit(kit.Name, KitManager.ItemsFromInventory(player), KitManager.ClothesFromInventory(player));
                        player.Message("kit_created", kitName);
                        return;
                    }
                    else // overwrite kit
                    {
                        KitManager.OverwriteKitItems(kit.Name, KitManager.ItemsFromInventory(player), KitManager.ClothesFromInventory(player));
                        player.Message("kit_overwritten", kitName);
                        return;
                    }
                }
                // delete kit
                if (op.ToLower() == "delete" || op.ToLower() == "d")
                {
                    if (KitManager.KitExists(kitName, out var kit))
                    {
                        KitManager.DeleteKit(kitName);
                        player.Message("kit_deleted", kitName);
                        return;
                    }
                    else // error
                    {
                        player.Message("kit_e_noexist", kitName);
                        return;
                    }
                }


                return;
            }
            // change kit property
            if (command.Length == 4)
            {
                op = command[0];
                property = command[1];
                kitName = command[2];
                newValue = command[3];

                if (op.ToLower() == "set" || op.ToLower() == "s")
                {
                    if (KitManager.SetProperty(kitName, property, newValue, out bool propertyIsValid, out bool kitExists, out bool argIsValid))
                    {
                        if (!propertyIsValid) // error - invalid property name
                        {
                            player.Message("kit_e_invalidprop", property);
                            return;
                        }
                        if (!kitExists) // error - kit does not exist
                        {
                            player.Message("kit_e_noexist", kitName);
                            return;
                        }
                        if (!argIsValid) // error - invalid argument value
                        {
                            player.Message("kit_e_invalidarg", newValue, property);
                            return;
                        }
                        // success
                        player.Message("kit_setprop", property, kitName, newValue);
                        return;
                    }
                }
            }
            if (command.Length == 3)
            {
                op = command[0];
                targetPlayer = command[1];
                kitName = command[2];

                // give player access to kit
                if (op.ToLower() == "giveaccess" || op.ToLower() == "givea")
                {
                    if (!KitManager.KitExists(kitName, out var kit))
                    {
                        player.Message("kit_e_noexist", kitName);
                        return;
                    }

                    UnturnedPlayer target = UnturnedPlayer.FromName(targetPlayer);

                    // error - no player found
                    if (target == null)
                    {
                        player.Message("kit_e_noplayer", targetPlayer);
                        return;
                    }
                    // error - player already has access
                    if (KitManager.HasAccess(player.CSteamID.m_SteamID, kit.Name))
                    {
                        player.Message("kit_e_alreadyaccess", targetPlayer, kitName);
                        return;
                    }

                    //success
                    player.Message("kit_accessgiven", targetPlayer, kitName);
                    KitManager.GiveAccess(player.CSteamID.m_SteamID, kit.Name);
                    return;
                }
                // remove player access to kit
                if (op.ToLower() == "removeaccess" || op.ToLower() == "removea")
                {
                    if (!KitManager.KitExists(kitName, out var kit))
                    {
                        player.Message("kit_e_noexist", kitName);
                        return;
                    }

                    UnturnedPlayer target = UnturnedPlayer.FromName(targetPlayer);

                    // error - no player found
                    if (target == null)
                    {
                        player.Message("kit_e_noplayer", targetPlayer);
                        return;
                    }
                    // error - player already has no access
                    if (!KitManager.HasAccess(player.CSteamID.m_SteamID, kit.Name))
                    {
                        player.Message("kit_e_noaccess", targetPlayer, kitName);
                        return; 
                    }

                    //success
                    player.Message("kit_accessremoved", targetPlayer, kitName);
                    KitManager.RemoveAccess(player.CSteamID.m_SteamID, kit.Name);
                    return;
                }
            }
        }
    }
}
