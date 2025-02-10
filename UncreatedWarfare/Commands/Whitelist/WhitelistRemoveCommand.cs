using Uncreated.Warfare.Configuration;
using Uncreated.Warfare.Interaction.Commands;
using Uncreated.Warfare.Kits.Whitelists;
using Uncreated.Warfare.Logging;
using Uncreated.Warfare.Translations;

namespace Uncreated.Warfare.Commands;

[Command("remove", "delete", "rem"), SubCommandOf(typeof(WhitelistCommand))]
internal sealed class WhitelistRemoveCommand : IExecutableCommand
{
    private readonly WhitelistService _whitelistService;
    private readonly WhitelistTranslations _translations;

    /// <inheritdoc />
    public required CommandContext Context { get; init; }

    public WhitelistRemoveCommand(WhitelistService whitelistService, TranslationInjection<WhitelistTranslations> translations)
    {
        _whitelistService = whitelistService;
        _translations = translations.Value;
    }

    /// <inheritdoc />
    public async UniTask ExecuteAsync(CancellationToken token)
    {
        if (!Context.TryGet(0, out ItemAsset? asset, out bool multiple, true))
        {
            if (multiple)
                throw Context.Reply(_translations.WhitelistMultipleResults, Context.Get(0)!);

            throw Context.Reply(_translations.WhitelistItemNotID, Context.Get(0)!);
        }

        bool didRemove = await _whitelistService.RemoveWhitelistedItem(AssetLink.Create(asset), token);
        if (!didRemove)
        {
            throw Context.Reply(_translations.WhitelistItemNotID, Context.Get(0)!);
        }

        await UniTask.SwitchToMainThread();
        Context.LogAction(ActionLogType.RemoveWhitelist, $"{asset.itemName} / {asset.id} / {asset.GUID:N}");
        Context.Reply(_translations.WhitelistRemoved, asset);
    }
}