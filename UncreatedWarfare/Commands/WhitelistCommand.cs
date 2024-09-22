using Uncreated.Warfare.Configuration;
using Uncreated.Warfare.Interaction.Commands;
using Uncreated.Warfare.Kits.Whitelists;
using Uncreated.Warfare.Logging;
using Uncreated.Warfare.Models;
using Uncreated.Warfare.Translations;

namespace Uncreated.Warfare.Commands;

[Command("whitelist", "wh")]
public class WhitelistCommand : IExecutableCommand
{
    private readonly WhitelistService _whitelistService;
    private readonly WhitelistTranslations _translations;

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

    public WhitelistCommand(WhitelistService whitelistService, TranslationInjection<WhitelistTranslations> translations)
    {
        _whitelistService = whitelistService;
        _translations = translations.Value;
    }

    /// <inheritdoc />
    public async UniTask ExecuteAsync(CancellationToken token)
    {
        Context.AssertArgs(2);

        if (Context.MatchParameter(0, "add", "whitelist", "create"))
        {
            if (!Context.TryGet(Context.ArgumentCount - 1, out byte amount))
                amount = 255;
            if (!Context.TryGet(1, out ItemAsset? asset, out bool multiple, amount == 255, Context.ArgumentCount - (amount == 255 ? 1 : 2), false))
            {
                if (multiple)
                    throw Context.Reply(_translations.WhitelistMultipleResults, Context.Get(1)!);

                throw Context.Reply(_translations.WhitelistItemNotID, Context.Get(1)!);
            }

            bool whitelisted = await _whitelistService.WhitelistItem(AssetLink.Create(asset), amount, token);
            await UniTask.SwitchToMainThread();
            if (!whitelisted)
                throw Context.Reply(_translations.WhitelistAlreadyAdded, asset);
                
            Context.LogAction(ActionLogType.AddWhitelist, $"{asset.itemName} / {asset.id} / {asset.GUID:N}");
            if (amount != 255)
            {
                Context.LogAction(ActionLogType.SetWhitelistMaxAmount, $"{asset.itemName} / {asset.id} / {asset.GUID:N} set to {amount}");
            }

            throw Context.Reply(_translations.WhitelistAdded, asset);
        }
        
        if (Context.MatchParameter(0, "remove", "delete", "rem"))
        {
            if (!Context.TryGet(1, out ItemAsset? asset, out bool multiple, true))
            {
                if (multiple)
                    throw Context.Reply(_translations.WhitelistMultipleResults, Context.Get(1)!);

                throw Context.Reply(_translations.WhitelistItemNotID, Context.Get(1)!);
            }

            bool didRemove = await _whitelistService.RemoveWhitelistedItem(AssetLink.Create(asset), token);
            await UniTask.SwitchToMainThread();
            if (!didRemove)
                throw Context.Reply(_translations.WhitelistItemNotID, Context.Get(1)!);

            Context.LogAction(ActionLogType.RemoveWhitelist, $"{asset.itemName} / {asset.id} / {asset.GUID:N}");
            throw Context.Reply(_translations.WhitelistRemoved, asset);
        }
        
        if (Context.MatchParameter(0, "set"))
        {
            Context.AssertArgs(4);

            if (!Context.TryGet(Context.ArgumentCount - 1, out byte amount))
                throw Context.SendCorrectUsage("/whitelist set amount <item> <amount>");

            if (!Context.MatchParameter(1, "maxamount", "amount", "amt"))
                throw Context.SendCorrectUsage("/whitelist <add|remove|set amount>");

            if (!Context.TryGet(2, out ItemAsset? asset, out bool multiple, false, Context.ArgumentCount - 2, false))
            {
                if (multiple)
                    throw Context.Reply(_translations.WhitelistMultipleResults, Context.Get(1)!);
                
                throw Context.Reply(_translations.WhitelistItemNotID, Context.Get(1)!);
            }

            IAssetLink<ItemAsset> assetLink = AssetLink.Create(asset);
            ItemWhitelist? existing = await _whitelistService.GetWhitelistAsync(assetLink, token);
            if (existing != null && existing.Amount == amount)
            {
                throw Context.Reply(_translations.WhitelistAlreadyAdded, asset);
            }

            bool whitelistedOrUpdated = await _whitelistService.WhitelistItem(assetLink, amount, token);
            await UniTask.SwitchToMainThread(token);

            if (!whitelistedOrUpdated)
            {
                throw Context.Reply(_translations.WhitelistAlreadyAdded, asset);
            }

            if (whitelistedOrUpdated && existing == null)
            {
                Context.LogAction(ActionLogType.AddWhitelist, $"{asset.itemName} / {asset.id} / {asset.GUID:N}");
                if (amount > 0)
                    Context.LogAction(ActionLogType.SetWhitelistMaxAmount, $"{asset.itemName} / {asset.id} / {asset.GUID:N} set to {amount}");
                Context.Reply(_translations.WhitelistAdded, asset);
                throw Context.Reply(_translations.WhitelistSetAmount, asset, amount);
            }

            Context.LogAction(ActionLogType.SetWhitelistMaxAmount, $"{asset.itemName} / {asset.id} / {asset.GUID:N} set to {(amount <= 0 ? -1 : amount)}");
            throw Context.Reply(_translations.WhitelistSetAmount, asset, amount);
        }
        
        throw Context.SendCorrectUsage("/whitelist <add|remove|set amount>");
    }
}