using Rocket.API;
using Rocket.Unturned.Player;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using SDG.Unturned;

namespace Uncreated.Warfare.Commands
{
    public class ClearCommand : IRocketCommand
    {
        public AllowedCaller AllowedCaller => AllowedCaller.Both;
        public string Name => "clear";
        public string Help => "Either clears a player's inventory or wipes items, vehicles, or structures/barricades from the map.";
        public string Syntax => "/clear <inventory|items|vehicles|structures> [player for inventory]";
        public List<string> Aliases => new List<string>();
        public List<string> Permissions => new List<string>() { "uc.clear" };
        public void Execute(IRocketPlayer caller, string[] command)
        {
            UnturnedPlayer player = caller as UnturnedPlayer;
            bool isConsole = caller.DisplayName == "Console" || player == default;
            Action<string, Color, object[]> Reply = isConsole ?
                new Action<string, Color, object[]>((msg, color, formatting) =>
                    F.Log(F.Translate(msg, 0, formatting)))
            :
                new Action<string, Color, object[]>((msg, color, formatting) =>
                    player.SendChat(msg, color, formatting));
            if (command.Length < 1)
            {
                Reply.Invoke("clear_not_enough_args", UCWarfare.GetColor("clear_not_enough_args"), new object[0]);
                return;
            }
            string operation = command[0].ToLower();
            if (operation == "inv" || operation == "inventory")
            {
                if (command.Length == 1)
                {
                    if (isConsole)
                    {
                        Reply.Invoke("clear_inventory_console_identity", 
                            UCWarfare.GetColor("clear_inventory_console_identity"), new object[0]);
                        return;
                    } else
                    {
                        Kits.UCInventoryManager.ClearInventory(player);
                        Reply.Invoke("clear_inventory_self", UCWarfare.GetColor("clear_inventory_self"), new object[0]);
                    }
                }
                else 
                {
                    StringBuilder name = new StringBuilder();
                    for (int i = 1; i < command.Length; i++)
                        name.Append((i == 1 ? '\0' : ' ') + command[i]);
                    string n = name.ToString();
                    if (PlayerTool.tryGetSteamPlayer(n, out SteamPlayer splayer))
                    {
                        Kits.UCInventoryManager.ClearInventory(splayer);
                        Reply.Invoke("clear_inventory_others", UCWarfare.GetColor("clear_inventory_others"),
                            new object[2] {
                                isConsole ? F.GetPlayerOriginalNames(splayer).PlayerName : F.GetPlayerOriginalNames(splayer).CharacterName,
                                UCWarfare.GetColorHex("clear_inventory_others_name")
                            });
                    } else
                    {
                        Reply.Invoke("clear_inventory_player_not_found", UCWarfare.GetColor("clear_inventory_player_not_found"),
                            new object[2] { n, UCWarfare.GetColorHex("clear_inventory_player_not_found_player") });
                    }
                }
            } else if (operation == "i" || operation == "items" || operation == "item")
            {
                ItemManager.askClearAllItems();
                Reply.Invoke("clear_items_cleared", UCWarfare.GetColor("clear_items_cleared"), new object[0]);
            }
            else if (operation == "v" || operation == "vehicles" || operation == "vehicle")
            {
                VehicleManager.askVehicleDestroyAll();
                Reply.Invoke("clear_vehicles_cleared", UCWarfare.GetColor("clear_vehicles_cleared"), new object[0]);
            }
            else if (operation == "s" || operation == "b" || operation == "structures" || operation == "structure" || 
                operation == "struct" || operation == "barricades" || operation == "barricade")
            {
                UCWarfare.ReplaceBarricadesAndStructures();
                Reply.Invoke("clear_structures_cleared", UCWarfare.GetColor("clear_structures_cleared"), new object[0]);
            } else
            {
                Reply.Invoke("correct_usage_c", UCWarfare.GetColor("correct_usage_c"), new object[1] { Syntax });
                return;
            }
        }
    }
}