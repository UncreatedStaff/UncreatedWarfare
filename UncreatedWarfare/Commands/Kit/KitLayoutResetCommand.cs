using System;
using Uncreated.Warfare.Interaction.Commands;
using Uncreated.Warfare.Kits;
using Uncreated.Warfare.Translations;

namespace Uncreated.Warfare.Commands;

[Command("reset", "delete", "cancel", "remove"), SubCommandOf(typeof(KitLayoutCommand))]
internal sealed class KitLayoutResetCommand : IExecutableCommand
{
    private readonly KitLayoutService _layoutService;
    private readonly KitCommandTranslations _translations;

    public required CommandContext Context { get; init; }

    public KitLayoutResetCommand(TranslationInjection<KitCommandTranslations> translations, KitLayoutService layoutService)
    {
        _layoutService = layoutService;
        _translations = translations.Value;
    }

    public async UniTask ExecuteAsync(CancellationToken token)
    {
        Context.AssertRanByPlayer();

        Kit? kit = await Context.Player.Component<KitPlayerComponent>().GetActiveKitAsync(KitInclude.Items | KitInclude.Translations, token).ConfigureAwait(false);
        await UniTask.SwitchToMainThread(token);

        if (kit == null)
        {
            throw Context.Reply(_translations.KitLayoutNoKit);
        }

        await UniTask.Delay(TimeSpan.FromSeconds(0.5f), cancellationToken: token);
        await UniTask.SwitchToMainThread(token);

        if (kit.Items != null)
        {
            _layoutService.TryReverseLayoutTransformations(Context.Player, kit);
        }

        await _layoutService.ResetLayoutAsync(Context.Player.Steam64, kit.Key, token).ConfigureAwait(false);
        Context.Reply(_translations.KitLayoutReset, kit);
    }
}