using Uncreated.Warfare.Commands.Dispatch;
using Uncreated.Warfare.Commands.Permissions;
using Uncreated.Warfare.Logging;
using Uncreated.Warfare.Players;
using Uncreated.Warfare.Translations;
using Uncreated.Warfare.Util;
using Uncreated.Warfare.Vehicles;

namespace Uncreated.Warfare.Commands;

[Command("clear")]
[HelpMetadata(nameof(GetHelpMetadata))]
public class ClearCommand : IExecutableCommand
{
    private readonly VehicleService _vehicleService;
    private readonly ClearTranslations _translations;
    private const string Syntax = "/clear <inventory|items|vehicles|structures> [player for inventory]";
    private const string Help = "Either clears a player's inventory or wipes items, vehicles, or structures and barricades from the map.";
    private static readonly PermissionLeaf PermissionClearInventory  = new PermissionLeaf("commands.clear.inventory",  unturned: false, warfare: true);
    private static readonly PermissionLeaf PermissionClearItems      = new PermissionLeaf("commands.clear.items",      unturned: false, warfare: true);
    private static readonly PermissionLeaf PermissionClearVehicles   = new PermissionLeaf("commands.clear.vehicles",   unturned: false, warfare: true);
    private static readonly PermissionLeaf PermissionClearStructures = new PermissionLeaf("commands.clear.structures", unturned: false, warfare: true);

    public ClearCommand(VehicleService vehicleService, TranslationInjection<ClearTranslations> translations)
    {
        _vehicleService = vehicleService;
        _translations = translations.Value;
    }

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

            if (Context.TryGet(1, out _, out WarfarePlayer? pl) || Context.HasArgs(2))
            {
                if (pl is not null)
                {
                    ItemUtility.ClearInventoryAndSlots(pl);

                    Context.LogAction(ActionLogType.ClearInventory, "CLEARED INVENTORY OF " + pl.Steam64.m_SteamID.ToString(Data.AdminLocale));
                    Context.Reply(_translations.ClearInventoryOther, pl);
                }
                else throw Context.Reply(T.PlayerNotFound);
            }
            else if (Context.IsConsole)
                throw Context.Reply(_translations.ClearNoPlayerConsole);
            else
            {
                ItemUtility.ClearInventoryAndSlots(Context.Player);
                Context.LogAction(ActionLogType.ClearInventory, "CLEARED PERSONAL INVENTORY");
                Context.Reply(_translations.ClearInventorySelf);
            }
        }
        else if (Context.MatchParameter(0, "items", "item", "i"))
        {
            await Context.AssertPermissions(PermissionClearItems, token);
            await UniTask.SwitchToMainThread(token);

            if (!Context.TryGet(1, out float range))
            {
                ItemUtility.DestroyAllDroppedItems(false);
                Context.LogAction(ActionLogType.ClearItems);
                throw Context.Reply(_translations.ClearItems);
            }

            Context.AssertRanByPlayer();
            ItemUtility.DestroyDroppedItemsInRange(Context.Player.Position, range, false);
            Context.LogAction(ActionLogType.ClearItems, "RANGE: " + range.ToString("F0") + "m");
            Context.Reply(_translations.ClearItemsInRange, range);
        }
        else if (Context.MatchParameter(0, "vehicles", "vehicle", "v"))
        {
            await Context.AssertPermissions(PermissionClearVehicles, token);
            await UniTask.SwitchToMainThread(token);

            await _vehicleService.DeleteAllVehiclesAsync(token);
            // todo respawn all vehicles
            Context.LogAction(ActionLogType.ClearVehicles);
            Context.Reply(_translations.ClearVehicles);
        }
        else if (Context.MatchParameter(0, "structures", "structure", "struct") ||
                 Context.MatchParameter(0, "barricades", "barricade", "b") || Context.MatchParameter(0, "s"))
        {
            await Context.AssertPermissions(PermissionClearStructures, token);
            await UniTask.SwitchToMainThread(token);

            Data.Gamemode.ReplaceBarricadesAndStructures();
            Context.LogAction(ActionLogType.ClearStructures);
            Context.Reply(_translations.ClearStructures);
        }
        else throw Context.SendCorrectUsage(Syntax);
    }
}

public class ClearTranslations : PropertiesTranslationCollection
{
    protected override string FileName => "Clear Command";

    [TranslationData("Sent when a user tries to clear from console and doesn't provide a player name.", IsPriorityTranslation = false)]
    public readonly Translation ClearNoPlayerConsole = new Translation("Specify a player name when clearing from console.");

    [TranslationData("Sent when a player clears their own inventory.", IsPriorityTranslation = false)]
    public readonly Translation ClearInventorySelf = new Translation("<#e6e3d5>Cleared your inventory.");

    [TranslationData("Sent when a user clears another player's inventory.", "The other player", IsPriorityTranslation = false)]
    public readonly Translation<IPlayer> ClearInventoryOther = new Translation<IPlayer>("<#e6e3d5>Cleared {0}'s inventory.", arg0Fmt: UCPlayer.FormatColoredCharacterName);

    [TranslationData("Sent when a user clears all dropped items.", IsPriorityTranslation = false)]
    public readonly Translation ClearItems = new Translation("<#e6e3d5>Cleared all dropped items.");

    [TranslationData("Sent when a user clears all dropped items within a given range.", "The range in meters", IsPriorityTranslation = false)]
    public readonly Translation<float> ClearItemsInRange = new Translation<float>("<#e6e3d5>Cleared all dropped items in {0}m.", arg0Fmt: "F0");

    [TranslationData("Sent when a user clears all items dropped by another player.", "The player", IsPriorityTranslation = false)]
    public readonly Translation<IPlayer> ClearItemsOther = new Translation<IPlayer>("<#e6e3d5>Cleared {0}'s dropped items.", arg0Fmt: UCPlayer.FormatColoredCharacterName);

    [TranslationData("Sent when a user clears all placed structures and barricades.", IsPriorityTranslation = false)]
    public readonly Translation ClearStructures = new Translation("<#e6e3d5>Cleared all placed structures and barricades.");

    [TranslationData("Sent when a user clears all spawned vehicles.", IsPriorityTranslation = false)]
    public readonly Translation ClearVehicles = new Translation("<#e6e3d5>Cleared all vehicles.");
}