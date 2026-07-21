using System;
using Uncreated.Warfare.Commands;
using Uncreated.Warfare.Events;
using Uncreated.Warfare.Events.Models;
using Uncreated.Warfare.Events.Models.Zones;
using Uncreated.Warfare.Interaction;
using Uncreated.Warfare.Kits;
using Uncreated.Warfare.Kits.Requests;
using Uncreated.Warfare.Players;
using Uncreated.Warfare.Translations;
using Uncreated.Warfare.Zones;

namespace Uncreated.Warfare.Tweaks;

internal sealed class PreviewKitTweaks : IEventListener<PlayerExitedZone>
{
    private readonly KitRequestService _kitRequestService;
    private readonly ZoneStore _zoneStore;
    private readonly ChatService _chatService;
    private readonly ILogger<PreviewKitTweaks> _logger;
    private readonly KitCommandTranslations _translations;

    public PreviewKitTweaks(
        KitRequestService kitRequestService,
        ZoneStore zoneStore,
        ChatService chatService,
        TranslationInjection<KitCommandTranslations> translations,
        ILogger<PreviewKitTweaks> logger)
    {
        _kitRequestService = kitRequestService;
        _zoneStore = zoneStore;
        _chatService = chatService;
        _logger = logger;
        _translations = translations.Value;
    }

    [EventListener(MustRunInstantly = true)]
    void IEventListener<PlayerExitedZone>.HandleEvent(PlayerExitedZone e, IServiceProvider serviceProvider)
    {
        if (e.Zone.Type is not ZoneType.MainBase)
            return;

        KitPlayerComponent component = e.Player.Component<KitPlayerComponent>();

        if (component.ActiveKit is not { IsPreview: true } || e.Player.IsOnDuty || _zoneStore.IsInMainBase(e.Player))
            return;

        _ = Task.Run(async () =>
        {
            try
            {
                await RevertPreviewAndNotifyAsync(e.Player);
            }
            catch (OperationCanceledException) when (!e.Player.IsOnline) { }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error reverting preview kit for {e.Player}.");
            }
        });
    }

    private async Task RevertPreviewAndNotifyAsync(WarfarePlayer player)
    {
        KitPlayerComponent component = player.Component<KitPlayerComponent>();

        CurrentKitState? activeKit = component.ActiveKit;

        KitRequestService.RevertResult result = await _kitRequestService.RevertPreviewAsync(player);

        switch (result)
        {
            case KitRequestService.RevertResult.RevertedPreview:
            case KitRequestService.RevertResult.RevertedPreviewWithDefaultItems:
                _chatService.Send(player, _translations.KitBackEndedPreview, activeKit?.CachedKit!);
                break;

            case KitRequestService.RevertResult.RevertedLoadoutPreview:
                _chatService.Send(player, _translations.KitBackEndedPreviewLoadout);
                break;

            case KitRequestService.RevertResult.UnknownFallbackKit:
                // await _kitRequestService.GiveAvailableFreeKitAsync(player, isLowAmmo: false);
                _chatService.Send(player, _translations.KitBackPreviewingUnknownKit, activeKit?.CachedKit!);
                break;

            // default case NotPreviewing: break;
        }
    }
}