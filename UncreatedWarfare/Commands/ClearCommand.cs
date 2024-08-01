using Cysharp.Threading.Tasks;
using System.Collections.Generic;
using System.Threading;
using Uncreated.Warfare.Commands.Dispatch;
using Uncreated.Warfare.Commands.Permissions;
using Uncreated.Warfare.Util;
using Uncreated.Warfare.Vehicles;

namespace Uncreated.Warfare.Commands;

[Command("clear")]
[HelpMetadata(nameof(GetHelpMetadata))]
public class ClearCommand : IExecutableCommand
{
    private const string Syntax = "/clear <inventory|items|vehicles|structures> [player for inventory]";
    private const string Help = "Either clears a player's inventory or wipes items, vehicles, or structures and barricades from the map.";
    private static readonly PermissionLeaf PermissionClearInventory  = new PermissionLeaf("commands.clear.inventory",  unturned: false, warfare: true);
    private static readonly PermissionLeaf PermissionClearItems      = new PermissionLeaf("commands.clear.items",      unturned: false, warfare: true);
    private static readonly PermissionLeaf PermissionClearVehicles   = new PermissionLeaf("commands.clear.vehicles",   unturned: false, warfare: true);
    private static readonly PermissionLeaf PermissionClearStructures = new PermissionLeaf("commands.clear.structures", unturned: false, warfare: true);

    /// <inheritdoc />
    public CommandContext Context { get; set; }

    /// <summary>
    /// Get /help metadata about this command.
    /// </summary>
    public static CommandStructure GetHelpMetadata()
    {
        return new CommandStructure
        {
            Description = Help,
            Parameters =
            [
                new CommandParameter("inventory")
                {
                    Aliases = [ "inv" ],
                    Description = "Clear you or another player's inventory.",
                    ChainDisplayCount = 1,
                    Permission = PermissionClearInventory,
                    Parameters =
                    [
                        new CommandParameter("Player", typeof(IPlayer))
                        {
                            Description = "Clear another player's inventory.",
                            IsOptional = true
                        }
                    ]
                },
                new CommandParameter("items")
                {
                    Aliases = [ "item", "i" ],
                    Description = "Clear all dropped items.",
                    Permission = PermissionClearItems,
                    Parameters =
                    [
                        new CommandParameter("Range", typeof(IPlayer))
                        {
                            Description = "Clear all items with a certain range (in meters).",
                            IsOptional = true
                        }
                    ]
                },
                new CommandParameter("vehicles")
                {
                    Aliases = [ "vehicle", "v" ],
                    Description = "Clear all spawned vehicles.",
                    Permission = PermissionClearVehicles
                },
                new CommandParameter("structures")
                {
                    Aliases = [ "structure", "struct", "s", "barricades", "barricade", "b" ],
                    Description = "Clear all barricades and structures.",
                    Permission = PermissionClearStructures
                }
            ]
        };
    }

    /// <inheritdoc />
    public async UniTask ExecuteAsync(CancellationToken token)
    {
        Context.AssertArgs(1, Syntax);

        if (Context.MatchParameter(0, "help"))
            throw Context.SendCorrectUsage(Syntax + " - " + Help);

        if (Context.MatchParameter(0, "inventory", "inv"))
        {
            await Context.AssertPermissions(PermissionClearInventory, token);
            await UniTask.SwitchToMainThread(token);

            if (Context.TryGet(1, out _, out UCPlayer? pl) || Context.HasArgs(2))
            {
                if (pl is not null)
                {
                    ItemUtility.ClearInventoryAndSlots(pl);

                    Context.LogAction(ActionLogType.ClearInventory, "CLEARED INVENTORY OF " + pl.Steam64.ToString(Data.AdminLocale));
                    Context.Reply(T.ClearInventoryOther, pl);
                }
                else throw Context.Reply(T.PlayerNotFound);
            }
            else if (Context.IsConsole)
                throw Context.Reply(T.ClearNoPlayerConsole);
            else
            {
                ItemUtility.ClearInventoryAndSlots(Context.Player);
                Context.LogAction(ActionLogType.ClearInventory, "CLEARED PERSONAL INVENTORY");
                Context.Reply(T.ClearInventorySelf);
            }
        }
        else if (Context.MatchParameter(0, "items", "item", "i"))
        {
            await Context.AssertPermissions(PermissionClearItems, token);
            await UniTask.SwitchToMainThread(token);

            if (!Context.TryGet(1, out float range))
            {
                ClearItems();
                Context.LogAction(ActionLogType.ClearItems);
                throw Context.Reply(T.ClearItems);
            }

            Context.AssertRanByPlayer();
            Vector3 pos = Context.Player.Position;
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
            Context.LogAction(ActionLogType.ClearItems, "RANGE: " + range.ToString("F0") + "m");
            Context.Reply(T.ClearItemsInRange, range);
        }
        else if (Context.MatchParameter(0, "vehicles", "vehicle", "v"))
        {
            await Context.AssertPermissions(PermissionClearVehicles, token);
            await UniTask.SwitchToMainThread(token);

            WipeVehiclesAndRespawn();
            Context.LogAction(ActionLogType.ClearVehicles);
            Context.Reply(T.ClearVehicles);
        }
        else if (Context.MatchParameter(0, "structures", "structure", "struct") ||
                 Context.MatchParameter(0, "barricades", "barricade", "b") || Context.MatchParameter(0, "s"))
        {
            await Context.AssertPermissions(PermissionClearStructures, token);
            await UniTask.SwitchToMainThread(token);

            Data.Gamemode.ReplaceBarricadesAndStructures();
            Context.LogAction(ActionLogType.ClearStructures);
            Context.Reply(T.ClearStructures);
        }
        else throw Context.SendCorrectUsage(Syntax);
    }
    public static void WipeVehicles()
    {
        VehicleSpawner.DeleteAllVehiclesFromWorld();
    }
    public static void WipeVehiclesAndRespawn()
    {
        WipeVehicles();
        
        if (Data.Is(out IVehicles vgm))
        {
            UCWarfare.RunTask(vgm.VehicleSpawner.RespawnAllVehicles, ctx: "Wipe and respawn all vehicles.");
        }
    }
    public static void ClearItems()
    {
        ItemManager.askClearAllItems();
        EventFunctions.OnClearAllItems();
    }
}