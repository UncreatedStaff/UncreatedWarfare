using System;
using Uncreated.Warfare.Interaction.Commands;
using Uncreated.Warfare.Kits;
using Uncreated.Warfare.Kits.Requests;
using Uncreated.Warfare.Translations;
using Uncreated.Warfare.Util.Inventory;

namespace Uncreated.Warfare.Commands;

[Command("back", "endpreview", "stoppreview", "stop"), SubCommandOf(typeof(KitCommand))]
internal sealed class KitBackCommand : IExecutableCommand
{
    private readonly KitRequestService _kitRequestService;
    private readonly IItemDistributionService _itemDistributionService;
    private readonly KitCommandTranslations _translations;
    public required CommandContext Context { get; init; }

    public KitBackCommand(
        TranslationInjection<KitCommandTranslations> translations,
        KitRequestService kitRequestService,
        IItemDistributionService itemDistributionService)
    {
        _kitRequestService = kitRequestService;
        _itemDistributionService = itemDistributionService;
        _translations = translations.Value;
    }

    public async UniTask ExecuteAsync(CancellationToken token)
    {
        Context.AssertRanByPlayer();

        KitPlayerComponent playerComponent = Context.Player.Component<KitPlayerComponent>();

        CurrentKitState? activeKit = playerComponent.ActiveKit;
        if (activeKit is not { IsPreview: true })
        {
            throw Context.Reply(_translations.KitBackNotPreviewing);
        }

        CurrentKitState? fallback = activeKit.PreviewFallback;
        if (fallback == null || fallback.IsPreview)
        {
            Context.Logger.LogInformation($"Tried to leave preview of {activeKit.Id}, fallback was: {fallback?.Id}.");
            throw Context.Reply(_translations.KitBackPreviewingUnknownKit);
        }

        if (fallback.ItemsFallback != null)
        {
            Context.Logger.LogTrace($"Giving exact items back for {fallback.Id}.");
            _itemDistributionService.GiveItems(fallback.ItemsFallback, Context.Player);
            playerComponent.UpdateKit(fallback);
            if (string.Equals(activeKit.Id, KitRequestService.DefaultKitId, StringComparison.OrdinalIgnoreCase))
            {
                Context.Logger.LogTrace("Was default kit.");
                throw Context.Reply(_translations.KitBackEndedPreviewLoadout);
            }

            throw Context.Reply(_translations.KitBackEndedPreview, activeKit.CachedKit);
        }

        Context.Logger.LogTrace($"Giving default items back for {fallback.Id}.");
        await _kitRequestService.GiveKitAsync(Context.Player, fallback.CreateBestowData(), Context.Token);
        throw Context.Reply(_translations.KitBackEndedPreview, activeKit.CachedKit);
    }
}