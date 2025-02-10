using Uncreated.Warfare.Configuration;
using Uncreated.Warfare.Interaction.Commands;
using Uncreated.Warfare.Kits.Whitelists;
using Uncreated.Warfare.Logging;
using Uncreated.Warfare.Models;
using Uncreated.Warfare.Translations;

namespace Uncreated.Warfare.Commands;

[Command("maxamount", "amount", "amt"), SubCommandOf(typeof(WhitelistSetCommand))]
internal sealed class WhitelistSetMaxAmountCommand : IExecutableCommand
{
    private readonly WhitelistService _whitelistService;
    private readonly WhitelistTranslations _translations;

    /// <inheritdoc />
    public required CommandContext Context { get; init; }

    public WhitelistSetMaxAmountCommand(WhitelistService whitelistService, TranslationInjection<WhitelistTranslations> translations)
    {
        _whitelistService = whitelistService;
        _translations = translations.Value;
    }

    /// <inheritdoc />
    public async UniTask ExecuteAsync(CancellationToken token)
    {
        Context.AssertArgs(2);

        if (!Context.TryGet(Context.ArgumentCount - 1, out int amount))
            throw Context.SendHelp();

        if (amount is < -1 or >= 255)
            amount = -1;

        if (!Context.TryGet(0, out ItemAsset? asset, out bool multiple, false, Context.ArgumentCount - 2, false))
        {
            if (multiple)
                throw Context.Reply(_translations.WhitelistMultipleResults, Context.Get(0)!);

            throw Context.Reply(_translations.WhitelistItemNotID, Context.Get(0)!);
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
        Context.Reply(_translations.WhitelistSetAmount, asset, amount);
    }
}