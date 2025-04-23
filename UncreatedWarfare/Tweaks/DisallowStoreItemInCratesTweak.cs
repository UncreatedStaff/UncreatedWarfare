using System;
using System.Linq;
using Uncreated.Warfare.Configuration;
using Uncreated.Warfare.Events;
using Uncreated.Warfare.Events.Models;
using Uncreated.Warfare.Events.Models.Items;
using Uncreated.Warfare.Fobs;
using Uncreated.Warfare.FOBs;
using Uncreated.Warfare.Interaction;
using Uncreated.Warfare.Kits.Items;
using Uncreated.Warfare.Translations;

namespace Uncreated.Warfare.Tweaks;

public class DisallowPickUpSupplyCrate : IEventListener<ItemMoveRequested>
{
    private readonly FobConfiguration _fobConfig;
    private readonly ChatService _chatService;
    private readonly TranslationInjection<FobTranslations> _translations;

    public DisallowPickUpSupplyCrate(FobConfiguration fobConfig, ChatService chatService, TranslationInjection<FobTranslations> translations)
    {
        _fobConfig = fobConfig;
        _chatService = chatService;
        _translations = translations;
    }

    [EventListener(RequiresMainThread = false)]
    public void HandleEvent(ItemMoveRequested e, IServiceProvider serviceProvider)
    {
        if (e.OldPage != Page.Storage || e.Player.IsOnDuty)
        {
            return;
        }

        // check if the item is a supply crate
        IAssetLink<ItemAsset> asset = e.Asset;
        if (!_fobConfig.SupplyCrates.Any(x => x.SupplyItemAsset.MatchAsset(asset)))
            return;

        e.Cancel();
        _chatService.Send(e.Player, _translations.Value.CantTakeSupplyCrate);
    }
}