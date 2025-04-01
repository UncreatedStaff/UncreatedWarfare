using Uncreated.Warfare.Configuration;
using Uncreated.Warfare.Interaction.Commands;
using Uncreated.Warfare.Kits.Whitelists;
using Uncreated.Warfare.Logging;
using Uncreated.Warfare.Translations;

namespace Uncreated.Warfare.Commands;

[Command("add", "whitelist", "create"), SubCommandOf(typeof(WhitelistCommand))]
internal sealed class WhitelistAddCommand : IExecutableCommand
{
    private readonly WhitelistService _whitelistService;
    private readonly WhitelistTranslations _translations;

    /// <inheritdoc />
    public required CommandContext Context { get; init; }

    public WhitelistAddCommand(WhitelistService whitelistService, TranslationInjection<WhitelistTranslations> translations)
    {
        _whitelistService = whitelistService;
        _translations = translations.Value;
    }

    /// <inheritdoc />
    public async UniTask ExecuteAsync(CancellationToken token)
    {
        bool hadAmount;
        if (!(hadAmount = Context.TryGet(Context.ArgumentCount - 1, out int amount)) || amount is < -1 or >= 255)
            amount = -1;

        if (!Context.TryGet(0, out ItemAsset? asset, out bool multiple, !hadAmount, Context.ArgumentCount - (hadAmount ? 1 : 0), false))
        {
            if (multiple)
                throw Context.Reply(_translations.WhitelistMultipleResults, Context.Get(0)!);

            throw Context.Reply(_translations.WhitelistItemNotID, Context.Get(0)!);
        }

        bool whitelisted = await _whitelistService.WhitelistItem(AssetLink.Create(asset), amount, token);
        if (!whitelisted)
        {
            throw Context.Reply(_translations.WhitelistAlreadyAdded, asset);
        }

        await UniTask.SwitchToMainThread();

        // todo: Context.LogAction(ActionLogType.AddWhitelist, $"{asset.itemName} / {asset.id} / {asset.GUID:N}");
        if (amount != -1)
        {
            // todo: Context.LogAction(ActionLogType.SetWhitelistMaxAmount, $"{asset.itemName} / {asset.id} / {asset.GUID:N} set to {amount}");
        }

        Context.Reply(_translations.WhitelistAdded, asset);
    }
}