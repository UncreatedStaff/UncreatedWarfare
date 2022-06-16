using Rocket.API;
using Rocket.Unturned.Player;
using SDG.Unturned;
using System;
using System.Collections.Generic;
using System.Text;
using Uncreated.Players;
using Uncreated.Warfare.Gamemodes.Interfaces;
using Uncreated.Warfare.Vehicles;

namespace Uncreated.Warfare.Commands;

public class ClearCommand : IRocketCommand
{
    private readonly List<string> _permissions = new List<string>(1) { "uc.clear" };
    private readonly List<string> _aliases = new List<string>(0);
    public AllowedCaller AllowedCaller => AllowedCaller.Both;
    public string Name => "clear";
    public string Help => "Either clears a player's inventory or wipes items, vehicles, or structures and barricades from the map.";
    public string Syntax => "/clear <inventory|items|vehicles|structures> [player for inventory]";
    public List<string> Aliases => _aliases;
	public List<string> Permissions => _permissions;
    public void Execute(IRocketPlayer caller, string[] command)
    {
#if DEBUG
        using IDisposable profiler = ProfilingUtils.StartTracking();
#endif
        UCCommandContext ctx = new UCCommandContext(caller, command);
        if (!ctx.HasArgs(1))
        {
            ctx.SendCorrectUsage(Syntax);
            return;
        }

        if (ctx.MatchParameter(0, "help"))
        {
            ctx.SendCorrectUsage(Syntax + " - " + Help);
            return;
        }

        if (ctx.MatchParameter(0, "inventory", "inv"))
        {
            if (ctx.TryGet(1, out _, out UCPlayer? pl) || ctx.HasArgs(2))
            {
                if (pl is not null)
                {
                    Kits.UCInventoryManager.ClearInventory(pl);
                    ctx.LogAction(EActionLogType.CLEAR_INVENTORY, "CLEARED INVENTORY OF " + pl.Steam64.ToString(Data.Locale));
                    FPlayerName names = F.GetPlayerOriginalNames(pl);
                    ctx.Reply("clear_inventory_others", ctx.IsConsole ? names.PlayerName : names.CharacterName);
                }
                else
                {
                    ctx.Reply("clear_inventory_player_not_found");
                }
            }
            else if (ctx.IsConsole)
            {
                ctx.Reply("clear_inventory_console_identity");
            }
            else
            {
                Kits.UCInventoryManager.ClearInventory(ctx.Caller!);
                ctx.LogAction(EActionLogType.CLEAR_INVENTORY, "CLEARED PERSONAL INVENTORY");
                ctx.Reply("clear_inventory_self");
            }
        }
        else if (ctx.MatchParameter(0, "items", "item", "i"))
        {
            ClearItems();
            ctx.LogAction(EActionLogType.CLEAR_ITEMS);
            ctx.Reply("clear_items_cleared");
        }
        else if (ctx.MatchParameter(0, "vehicles", "vehicle", "v"))
        {
            WipeVehiclesAndRespawn();
            ctx.LogAction(EActionLogType.CLEAR_VEHICLES);
            ctx.Reply("clear_vehicles_cleared");
        }
        else if (ctx.MatchParameter(0, "structures", "structure", "struct") ||
                 ctx.MatchParameter(0, "barricades", "barricade", "b") || ctx.MatchParameter(0, "s"))
        {
            Data.Gamemode.ReplaceBarricadesAndStructures();
            ctx.LogAction(EActionLogType.CLEAR_STRUCTURES);
            ctx.Reply("clear_structures_cleared");
        }
        else
            ctx.SendCorrectUsage(Syntax);
    }
    public static void WipeVehicles()
    {
        if (VehicleSpawner.Loaded)
        {
            VehicleBay.DeleteAllVehiclesFromWorld();
        } 
        else
        {
            VehicleManager.askVehicleDestroyAll();
        }
    }
    public static void WipeVehiclesAndRespawn()
    {
        WipeVehicles();
        if (VehicleSpawner.Loaded)
            VehicleSpawner.RespawnAllVehicles();
    }
    public static void ClearItems()
    {
        EventFunctions.itemstemp.Clear();
        ItemManager.askClearAllItems();
    }
}