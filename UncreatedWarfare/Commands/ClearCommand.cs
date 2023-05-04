using SDG.Unturned;
using System;
using System.Collections.Generic;
using Uncreated.Framework;
using Uncreated.Warfare.Commands.CommandSystem;
using Uncreated.Warfare.Gamemodes.Interfaces;
using Uncreated.Warfare.Vehicles;
using UnityEngine;
using Command = Uncreated.Warfare.Commands.CommandSystem.Command;

namespace Uncreated.Warfare.Commands;

public class ClearCommand : Command
{
    private const string Syntax = "/clear <inventory|items|vehicles|structures> [player for inventory]";
    private const string Help = "Either clears a player's inventory or wipes items, vehicles, or structures and barricades from the map.";
    public ClearCommand() : base("clear", EAdminType.MODERATOR)
    {
        Structure = new CommandStructure
        {
            Description = Help,
            Parameters = new CommandParameter[]
            {
                new CommandParameter("inventory")
                {
                    Aliases = new string[] { "inv" },
                    Description = "Clear you or another player's inventory.",
                    ChainDisplayCount = 1,
                    Parameters = new CommandParameter[]
                    {
                        new CommandParameter("Player", typeof(IPlayer))
                        {
                            Description = "Clear another player's inventory.",
                            IsOptional = true
                        }
                    }
                },
                new CommandParameter("items")
                {
                    Aliases = new string[] { "item", "i" },
                    Description = "Clear all dropped items.",
                    Parameters = new CommandParameter[]
                    {
                        new CommandParameter("Range", typeof(IPlayer))
                        {
                            Description = "Clear all items with a certain range (in meters).",
                            IsOptional = true
                        }
                    }
                },
                new CommandParameter("vehicles")
                {
                    Aliases = new string[] { "vehicle", "v" },
                    Description = "Clear all spawned vehicles.",
                },
                new CommandParameter("structures")
                {
                    Aliases = new string[] { "structure", "struct", "s", "barricades", "barricade", "b" },
                    Description = "Clear all barricades and structures.",
                }
            }
        };
    }
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
                    ctx.LogAction(ActionLogType.ClearInventory, "CLEARED INVENTORY OF " + pl.Steam64.ToString(Data.AdminLocale));
                    ctx.Reply(T.ClearInventoryOther, pl);
                }
                else throw ctx.Reply(T.PlayerNotFound);
            }
            else if (ctx.IsConsole)
                throw ctx.Reply(T.ClearNoPlayerConsole);
            else
            {
                Kits.UCInventoryManager.ClearInventory(ctx.Caller);
                ctx.LogAction(ActionLogType.ClearInventory, "CLEARED PERSONAL INVENTORY");
                ctx.Reply(T.ClearInventorySelf);
            }
        }
        else if (ctx.MatchParameter(0, "items", "item", "i"))
        {
            if (ctx.TryGet(1, out float range))
            {
                ctx.AssertRanByPlayer();
                Vector3 pos = ctx.Caller.Position;
                List<RegionCoordinate> c = new List<RegionCoordinate>(8);
                Regions.getRegionsInRadius(pos, range, c);
                float r2 = range * range;
                for (int i = 0; i < c.Count; ++i)
                {
                    RegionCoordinate c2 = c[i];
                    ItemRegion region = ItemManager.regions[c2.x, c2.y];
                    for (int j = region.items.Count - 1; j >= 0; --j)
                    {
                        ItemData item = region.items[j];
                        if ((item.point - pos).sqrMagnitude <= r2)
                            EventFunctions.OnItemRemoved(item);
                    }
                }
                ItemManager.ServerClearItemsInSphere(pos, range);
                ctx.LogAction(ActionLogType.ClearItems, "RANGE: " + range.ToString("F0") + "m");
                throw ctx.Reply(T.ClearItemsInRange, range);
            }
            ClearItems();
            ctx.LogAction(ActionLogType.ClearItems);
            ctx.Reply(T.ClearItems);
        }
        else if (ctx.MatchParameter(0, "vehicles", "vehicle", "v"))
        {
            WipeVehiclesAndRespawn();
            ctx.LogAction(ActionLogType.ClearVehicles);
            ctx.Reply(T.ClearVehicles);
        }
        else if (ctx.MatchParameter(0, "structures", "structure", "struct") ||
                 ctx.MatchParameter(0, "barricades", "barricade", "b") || ctx.MatchParameter(0, "s"))
        {
            Data.Gamemode.ReplaceBarricadesAndStructures();
            ctx.LogAction(ActionLogType.ClearStructures);
            ctx.Reply(T.ClearStructures);
        }
        else throw ctx.SendCorrectUsage(Syntax);
    }
    public static void WipeVehicles()
    {
        VehicleSpawner.DeleteAllVehiclesFromWorld();
    }
    public static void WipeVehiclesAndRespawn()
    {
        WipeVehicles();
        
        if (Data.Is(out IVehicles vgm))
            UCWarfare.RunTask(vgm.VehicleSpawner.RespawnAllVehicles, ctx: "Wipe and respawn all vehicles.");
    }
    public static void ClearItems()
    {
        ItemManager.askClearAllItems();
        EventFunctions.OnClearAllItems();
    }
}