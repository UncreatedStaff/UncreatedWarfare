using Uncreated.Warfare.Interaction.Commands;
using Uncreated.Warfare.Logging;

namespace Uncreated.Warfare.Commands;

[Command("whitelist", "wh")]
[MetadataFile(nameof(GetHelpMetadata))]
public class WhitelistCommand : IExecutableCommand
{
    /// <inheritdoc />
    public CommandContext Context { get; set; }

    /// <summary>
    /// Get /help metadata about this command.
    /// </summary>
    public static CommandStructure GetHelpMetadata()
    {
        return new CommandStructure
        {
            Description = "Add or remove items from the global whitelist.",
            Parameters =
            [
                new CommandParameter("Add")
                {
                    Aliases = [ "whitelist", "create" ],
                    Description = "Add an item to the global whitelist.",
                    ChainDisplayCount = 3,
                    Parameters =
                    [
                        new CommandParameter("Item", typeof(ItemAsset))
                        {
                            Parameters =
                            [
                                new CommandParameter("Amount", typeof(int))
                                {
                                    IsOptional = true
                                }
                            ]
                        }
                    ]
                },
                new CommandParameter("Remove")
                {
                    Aliases = [ "delete", "rem" ],
                    Description = "Remove an item from the global whitelist.",
                    ChainDisplayCount = 2,
                    Parameters =
                    [
                        new CommandParameter("Item", typeof(ItemAsset))
                        {
                            IsRemainder = true
                        }
                    ]
                },
                new CommandParameter("Set")
                {
                    Description = "Set the whitelist amount for an item.",
                    ChainDisplayCount = 3,
                    Parameters =
                    [
                        new CommandParameter("Amount")
                        {
                            Aliases = [ "maxamount", "amt" ],
                            IsRemainder = true,
                            Parameters =
                            [
                                new CommandParameter("Amount", typeof(byte))
                            ]
                        }
                    ]
                }
            ]
        };
    }

    /// <inheritdoc />
    public UniTask ExecuteAsync(CancellationToken token)
    {
        if (!Data.Gamemode.UseWhitelist || !Whitelister.Loaded)
            throw Context.SendGamemodeError();

        Context.AssertHelpCheck(0, "/whitelist <add|remove|set amount> - Add or remove items from the global whitelist.");

        Context.AssertArgs(2, "/whitelist <add|remove|set amount>");

        if (Context.MatchParameter(0, "add", "whitelist", "create"))
        {
            Context.AssertHelpCheck(1, "/whitelist add <item> [amount]");
            if (!Context.TryGet(Context.ArgumentCount - 1, out byte amount))
                amount = 255;
            if (Context.TryGet(1, out ItemAsset? asset, out bool multiple, amount == 255, Context.ArgumentCount - (amount == 255 ? 1 : 2), false))
            {
                if (!Whitelister.IsWhitelisted(asset.GUID, out _))
                {
                    Whitelister.AddItem(asset.GUID, amount);
                    Context.LogAction(ActionLogType.AddWhitelist, $"{asset.itemName} / {asset.id} / {asset.GUID:N}");
                    if (amount != 255)
                        Context.LogAction(ActionLogType.SetWhitelistMaxAmount, $"{asset.itemName} / {asset.id} / {asset.GUID:N} set to {amount}");
                    Context.Reply(T.WhitelistAdded, asset);
                }
                else
                    throw Context.Reply(T.WhitelistAlreadyAdded, asset);
            }
            else if (multiple)
                throw Context.Reply(T.WhitelistMultipleResults, Context.Get(1)!);
            else
                throw Context.Reply(T.WhitelistItemNotID, Context.Get(1)!);

            return default;
        }
        
        if (Context.MatchParameter(0, "remove", "delete", "rem"))
        {
            Context.AssertHelpCheck(1, "/whitelist remove <item>");
            if (Context.TryGet(1, out ItemAsset? asset, out bool multiple, true, selector: x => Whitelister.IsWhitelistedFast(x.GUID)))
            {
                Whitelister.RemoveItem(asset.GUID);
                Context.LogAction(ActionLogType.RemoveWhitelist, $"{asset.itemName} / {asset.id} / {asset.GUID:N}");
                Context.Reply(T.WhitelistRemoved, asset);
            }
            else if (multiple)
                throw Context.Reply(T.WhitelistMultipleResults, Context.Get(1)!);
            else
                throw Context.Reply(T.WhitelistItemNotID, Context.Get(1)!);

            return default;
        }
        
        if (Context.MatchParameter(0, "set"))
        {
            Context.AssertArgs(4, "/whitelist set amount <item> <amount>");
            Context.AssertHelpCheck(1, "/whitelist set amount <item> <amount>");

            if (!Context.TryGet(Context.ArgumentCount - 1, out byte amount))
                throw Context.SendCorrectUsage("/whitelist set amount <item> <amount>");

            if (!Context.MatchParameter(1, "maxamount", "amount", "amt"))
                throw Context.SendCorrectUsage("/whitelist <add|remove|set amount>");

            Context.AssertHelpCheck(2, "/whitelist set amount <item> <amount>");

            if (Context.TryGet(2, out ItemAsset? asset, out bool multiple, false, Context.ArgumentCount - 2, false))
            {
                if (!Whitelister.IsWhitelisted(asset.GUID, out _))
                {
                    Whitelister.AddItem(asset.GUID, amount);
                    Context.LogAction(ActionLogType.AddWhitelist, $"{asset.itemName} / {asset.id} / {asset.GUID:N}");
                    if (amount != 255)
                        Context.LogAction(ActionLogType.SetWhitelistMaxAmount, $"{asset.itemName} / {asset.id} / {asset.GUID:N} set to {amount}");
                    Context.Reply(T.WhitelistAdded, asset);
                    Context.Reply(T.WhitelistSetAmount, asset, amount);
                }
                else
                {
                    if (amount != 255)
                        Context.LogAction(ActionLogType.SetWhitelistMaxAmount, $"{asset.itemName} / {asset.id} / {asset.GUID:N} set to {amount}");
                    Whitelister.SetAmount(asset.GUID, amount);
                    Context.Reply(T.WhitelistSetAmount, asset, amount);
                }
            }
            else if (multiple)
                throw Context.Reply(T.WhitelistMultipleResults, Context.Get(1)!);
            else
                throw Context.Reply(T.WhitelistItemNotID, Context.Get(1)!);

            return default;
        }
        
        throw Context.SendCorrectUsage("/whitelist <add|remove|set amount>");
    }
}