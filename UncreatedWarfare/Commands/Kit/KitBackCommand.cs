using Uncreated.Warfare.Interaction.Commands;
using Uncreated.Warfare.Kits;
using Uncreated.Warfare.Kits.Requests;
using Uncreated.Warfare.Translations;

namespace Uncreated.Warfare.Commands;

[Command("back", "endpreview", "stoppreview", "stop"), SubCommandOf(typeof(KitCommand))]
internal sealed class KitBackCommand : IExecutableCommand
{
    private readonly KitRequestService _kitRequestService;
    private readonly KitCommandTranslations _translations;
    public required CommandContext Context { get; init; }

    public KitBackCommand(
        TranslationInjection<KitCommandTranslations> translations,
        KitRequestService kitRequestService)
    {
        _kitRequestService = kitRequestService;
        _translations = translations.Value;
    }

    public async UniTask ExecuteAsync(CancellationToken token)
    {
        Context.AssertRanByPlayer();

        KitPlayerComponent playerComponent = Context.Player.Component<KitPlayerComponent>();

        CurrentKitState? activeKit = playerComponent.ActiveKit;

        KitRequestService.RevertResult result
            = await _kitRequestService.RevertPreviewAsync(Context.Player, Context.Token);

        switch (result)
        {
            case KitRequestService.RevertResult.RevertedPreview:
                throw Context.Reply(_translations.KitBackEndedPreview, activeKit?.CachedKit!);

            case KitRequestService.RevertResult.RevertedLoadoutPreview:
                throw Context.Reply(_translations.KitBackEndedPreviewLoadout);

            case KitRequestService.RevertResult.UnknownFallbackKit:
                throw Context.Reply(_translations.KitBackPreviewingUnknownKit);

            // case NotPreviewing:
            default:
                throw Context.Reply(_translations.KitBackNotPreviewing);
        }
    }
}