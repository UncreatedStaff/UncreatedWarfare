using System;
using SDG.Unturned;
using System.Threading.Tasks;
using Uncreated.Framework;
using Uncreated.Warfare.Commands.CommandSystem;
using Uncreated.Warfare.Vehicles;
using Command = Uncreated.Warfare.Commands.CommandSystem.Command;

namespace Uncreated.Warfare.Commands;

public class ClearCommand : Command
{
    private const string Syntax = "/clear <inventory|items|vehicles|structures> [player for inventory]";
    private const string Help = "Either clears a player's inventory or wipes items, vehicles, or structures and barricades from the map.";
    public ClearCommand() : base("clear", EAdminType.MODERATOR) { }
    public override void Execute(CommandInteraction ctx)
    {
#if DEBUG
        using IDisposable profiler = ProfilingUtils.StartTracking();
#endif
        ctx.AssertArgs(1, Syntax);

        if (ctx.MatchParameter(0, "help"))
            throw ctx.SendCorrectUsage(Syntax + " - " + Help);

        if (ctx.MatchParameter(0, "inventory", "inv"))
        {
            if (ctx.TryGet(1, out _, out UCPlayer? pl) || ctx.HasArgs(2))
            {
                if (pl is not null)
                {
                    Kits.UCInventoryManager.ClearInventory(pl);
                    ctx.LogAction(EActionLogType.CLEAR_INVENTORY, "CLEARED INVENTORY OF " + pl.Steam64.ToString(Data.AdminLocale));
                    ctx.Reply(T.ClearInventoryOther, pl);
                }
                else throw ctx.Reply(T.PlayerNotFound);
            }
            else if (ctx.IsConsole)
                throw ctx.Reply(T.ClearNoPlayerConsole);
            else
            {
                Kits.UCInventoryManager.ClearInventory(ctx.Caller);
                ctx.LogAction(EActionLogType.CLEAR_INVENTORY, "CLEARED PERSONAL INVENTORY");
                ctx.Reply(T.ClearInventorySelf);
            }
        }
        else if (ctx.MatchParameter(0, "items", "item", "i"))
        {
            ClearItems();
            ctx.LogAction(EActionLogType.CLEAR_ITEMS);
            ctx.Reply(T.ClearItems);
        }
        else if (ctx.MatchParameter(0, "vehicles", "vehicle", "v"))
        {
            WipeVehiclesAndRespawn();
            ctx.LogAction(EActionLogType.CLEAR_VEHICLES);
            ctx.Reply(T.ClearVehicles);
        }
        else if (ctx.MatchParameter(0, "structures", "structure", "struct") ||
                 ctx.MatchParameter(0, "barricades", "barricade", "b") || ctx.MatchParameter(0, "s"))
        {
            Data.Gamemode.ReplaceBarricadesAndStructures();
            ctx.LogAction(EActionLogType.CLEAR_STRUCTURES);
            ctx.Reply(T.ClearStructures);
        }
        else throw ctx.SendCorrectUsage(Syntax);
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
            Task.Run(() => Util.TryWrap(VehicleSpawner.RespawnAllVehicles()));
    }
    public static void ClearItems()
    {
        EventFunctions.itemstemp.Clear();
        ItemManager.askClearAllItems();
    }
}