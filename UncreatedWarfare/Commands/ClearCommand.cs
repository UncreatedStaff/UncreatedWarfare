using Rocket.API;
using Rocket.Unturned.Player;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using SDG.Unturned;
using Uncreated.Warfare.Vehicles;

namespace Uncreated.Warfare.Commands
{
    public class ClearCommand : IRocketCommand
    {
        public AllowedCaller AllowedCaller => AllowedCaller.Both;
        public string Name => "clear";
        public string Help => "Either clears a player's inventory or wipes items, vehicles, or structures and barricades from the map.";
        public string Syntax => "/clear <inventory|items|vehicles|structures> [player for inventory]";
        public List<string> Aliases => new List<string>();
        public List<string> Permissions => new List<string>() { "uc.clear" };
        public async void Execute(IRocketPlayer caller, string[] command)
        {
            UnturnedPlayer player = caller as UnturnedPlayer;
            bool isConsole = caller.DisplayName == "Console";
            if (command.Length < 1)
            {
                if (isConsole) F.LogError(F.Translate("clear_not_enough_args", 0, out _)); 
                else player.SendChat("clear_not_enough_args");
                return;
            }
            string operation = command[0].ToLower();
            if (operation == "inv" || operation == "inventory")
            {
                if (command.Length == 1)
                {
                    if (isConsole)
                    {
                        if (isConsole) F.LogError(F.Translate("clear_inventory_console_identity", 0, out _));
                        else player.SendChat("clear_inventory_console_identity");
                        return;
                    } else
                    {
                        Kits.UCInventoryManager.ClearInventory(player);
                        if (isConsole) F.LogError(F.Translate("clear_inventory_self", 0, out _));
                        else player.SendChat("clear_inventory_self");
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
                        n = isConsole ? F.GetPlayerOriginalNames(splayer).PlayerName : F.GetPlayerOriginalNames(splayer).CharacterName;
                        if (isConsole) F.LogError(F.Translate("clear_inventory_others", 0, out _, n));
                        else player.SendChat("clear_inventory_others", n);
                    } else
                    {
                        if (isConsole) F.LogError(F.Translate("clear_inventory_player_not_found", 0, out _, n));
                        else player.SendChat("clear_inventory_player_not_found", n);
                    }
                }
            } else if (operation == "i" || operation == "items" || operation == "item")
            {
                ItemManager.askClearAllItems();
                if (isConsole) F.LogError(F.Translate("clear_items_cleared", 0, out _));
                else player.SendChat("clear_items_cleared");
            }
            else if (operation == "v" || operation == "vehicles" || operation == "vehicle")
            {
                WipeVehiclesAndRespawn();
                if (isConsole) F.LogError(F.Translate("clear_vehicles_cleared", 0, out _));
                else player.SendChat("clear_vehicles_cleared");
            }
            else if (operation == "s" || operation == "b" || operation == "structures" || operation == "structure" || 
                operation == "struct" || operation == "barricades" || operation == "barricade")
            {
                await UCWarfare.ReplaceBarricadesAndStructures();
                if (isConsole) F.LogError(F.Translate("clear_structures_cleared", 0, out _));
                else player.SendChat("clear_structures_cleared");
            } else
            {
                if (isConsole) F.LogError(F.Translate("correct_usage", 0, out _, Syntax));
                else player.SendChat("correct_usage", Syntax);
                return;
            }
        }
        public static void WipeVehiclesAndRespawn()
        {
            List<Vehicles.VehicleSpawn> spawnsToReset = new List<Vehicles.VehicleSpawn>();
            for (int i = 0; i < VehicleSpawner.ActiveObjects.Count; i++)
                if (VehicleSpawner.ActiveObjects[i].HasLinkedVehicle(out _)) spawnsToReset.Add(VehicleSpawner.ActiveObjects[i]);
            VehicleManager.askVehicleDestroyAll();
            for (int i = 0; i < spawnsToReset.Count; i++)
                spawnsToReset[i].SpawnVehicle();
        }
    }
}