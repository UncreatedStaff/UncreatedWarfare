using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Uncreated.Warfare.Commands;
using Uncreated.Warfare.Configuration;
using Uncreated.Warfare.Events.Models;
using Uncreated.Warfare.Events.Models.Barricades;
using Uncreated.Warfare.Events.Models.Items;
using Uncreated.Warfare.Fobs;
using Uncreated.Warfare.FOBs;
using Uncreated.Warfare.FOBs.SupplyCrates;
using Uncreated.Warfare.Interaction;
using Uncreated.Warfare.Kits;
using Uncreated.Warfare.Translations;
using Uncreated.Warfare.Tweaks.BuildablePlacement;
using Uncreated.Warfare.Util.Containers;

namespace Uncreated.Warfare.Tweaks.Bedrolls;
internal class ClaimToDeployTweaks : IEventListener<ClaimBedRequested>
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger _logger;
    private readonly FobManager? _fobManager;

    public ClaimToDeployTweaks(IServiceProvider serviceProvider, ILogger<ClaimToDeployTweaks> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
        _fobManager = serviceProvider.GetService<FobManager>();
    }

    public void HandleEvent(ClaimBedRequested e, IServiceProvider serviceProvider)
    {
        if (_fobManager == null)
            return;

        SupplyCrate? ammoCrate = _fobManager.FloatingItems.OfType<SupplyCrate>().FirstOrDefault(s =>
            s.Type == SupplyType.Ammo &&
            !s.Buildable.IsDead &&
            s.Buildable.Equals(e.Buildable)
        ); // todo: is this an efficient way to do this?

        if (ammoCrate == null)
            return;

        ChatService chatService = serviceProvider.GetRequiredService<ChatService>();
        AmmoCommandTranslations translations = serviceProvider.GetRequiredService<TranslationInjection<AmmoCommandTranslations>>().Value;

        if (!e.Player.TryGetFromContainer(out KitPlayerComponent? kit) || kit?.CachedKit == null)
        {
            chatService.Send(e.Player, translations.AmmoNoKit);
            e.Cancel();
            return;
        }

        int rearmCost = GetKitRearmCost(kit.ActiveClass);

        NearbySupplyCrates supplyCrate = NearbySupplyCrates.FromSingleCrate(ammoCrate, _fobManager);
        if (rearmCost > supplyCrate.AmmoCount)
        {
            chatService.Send(e.Player, translations.AmmoOutOfStock, supplyCrate.AmmoCount, rearmCost);
            e.Cancel();
            return;
        }
        KitManager kitManager = serviceProvider.GetRequiredService<KitManager>();
        _ = kitManager.Requests.GiveKit(e.Player, kit.CachedKit, false, true);
        supplyCrate.SubstractSupplies(1, SupplyType.Ammo, SupplyChangeReason.ConsumeGeneral);

        chatService.Send(e.Player, translations.AmmoResuppliedKit, rearmCost, supplyCrate.AmmoCount);
        e.Cancel();
    }
    private int GetKitRearmCost(Class kitClass)
    {
        switch (kitClass)
        {
            case Class.HAT:
            case Class.CombatEngineer:
                return 3;
            case Class.LAT:
            case Class.MachineGunner:
            case Class.Sniper:
                return 2;
            default:
                return 1;
        }
    }
}
