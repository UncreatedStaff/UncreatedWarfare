using Microsoft.Extensions.DependencyInjection;
using System;
using System.Linq;
using Uncreated.Warfare.Buildables;
using Uncreated.Warfare.Configuration;
using Uncreated.Warfare.Events;
using Uncreated.Warfare.Events.Models;
using Uncreated.Warfare.Events.Models.Barricades;
using Uncreated.Warfare.Events.Models.Players;
using Uncreated.Warfare.Events.Models.Zones;
using Uncreated.Warfare.Fobs;
using Uncreated.Warfare.Fobs.SupplyCrates;
using Uncreated.Warfare.FOBs.SupplyCrates;
using Uncreated.Warfare.Interaction;
using Uncreated.Warfare.Kits;
using Uncreated.Warfare.Kits.Requests;
using Uncreated.Warfare.Kits.UI;
using Uncreated.Warfare.Players;
using Uncreated.Warfare.Translations;
using Uncreated.Warfare.Util.Containers;
using Uncreated.Warfare.Zones;

namespace Uncreated.Warfare.FOBs.Deployment.Tweaks;

public class ClaimToRearmTweaks :
    IAsyncEventListener<PlayerPunched>,
    IAsyncEventListener<PlayerEnteredZone>,
    IAsyncEventListener<PlayerExitedZone>,
    IAsyncEventListener<ClaimBedRequested>
{
    private readonly KitRequestService _kitRequestService;
    private readonly FobManager _fobManager;
    private readonly KitRearmService _rearmService;
    private readonly ChatService _chatService;
    private readonly AmmoTranslations _translations;
    private readonly ZoneStore? _zoneStore;
    private readonly AssetConfiguration _assetConfiguration;
    private readonly KitSelectionUI? _kitUi;

    public ClaimToRearmTweaks(IServiceProvider serviceProvider)
    {
        _assetConfiguration = serviceProvider.GetRequiredService<AssetConfiguration>();
        _fobManager = serviceProvider.GetRequiredService<FobManager>();
        _kitRequestService = serviceProvider.GetRequiredService<KitRequestService>();
        _rearmService = serviceProvider.GetRequiredService<KitRearmService>();
        _chatService = serviceProvider.GetRequiredService<ChatService>();
        _translations = serviceProvider.GetRequiredService<TranslationInjection<AmmoTranslations>>().Value;
        _zoneStore = serviceProvider.GetService<ZoneStore>();
        _kitUi = serviceProvider.GetService<KitSelectionUI>();
    }

    [EventListener(RequireActiveLayout = true, RequiresMainThread = true)]
    UniTask IAsyncEventListener<PlayerPunched>.HandleEventAsync(PlayerPunched e, IServiceProvider serviceProvider, CancellationToken token = default)
    {
        if (!e.TryGetTargetBuildable(out IBuildable? buildable))
        {
            return UniTask.CompletedTask;
        }

        IAmmoStorage? ammoStorage = TryGetRearmBarricade(e.Player, buildable);
        if (ammoStorage == null)
        {
            return UniTask.CompletedTask;
        }

        return RearmKitFromSupplyCrate(e.Player, ammoStorage, e.Player.DisconnectToken);
    }

    [EventListener(RequireActiveLayout = true, RequiresMainThread = true)]
    UniTask IAsyncEventListener<ClaimBedRequested>.HandleEventAsync(ClaimBedRequested e, IServiceProvider serviceProvider, CancellationToken token)
    {
        IAmmoStorage? ammoStorage = TryGetRearmBarricade(e.Player, e.Buildable);
        if (ammoStorage == null)
        {
            return UniTask.CompletedTask;
        }

        e.Cancel();
        if (!ammoStorage.CanChangeKit || _kitUi == null)
        {
            return RearmKitFromSupplyCrate(e.Player, ammoStorage, e.Player.DisconnectToken);
        }

        return OpenKitUIFromSupplyCrate(e.Player, ammoStorage, e.Player.DisconnectToken);
    }

    private IAmmoStorage? TryGetRearmBarricade(WarfarePlayer player, IBuildable buildable)
    {
        IAmmoStorage? ammoStorage = ContainerHelper.FindComponent<IAmmoStorage>(buildable.Model);
        if (ammoStorage == null)
        {
            SupplyCrate? ammoCrate = _fobManager.Entities.OfType<SupplyCrate>().FirstOrDefault(s =>
                s.Type == SupplyType.Ammo &&
                s.Buildable.Alive &&
                s.Buildable.Equals(buildable)
            );

            ammoStorage = ammoCrate != null
                ? AmmoSupplyCrate.FromSupplyCrate(ammoCrate, _fobManager)
                : null;

            if (ammoStorage == null)
                return null;
        }

        if (player.Team.GroupId == buildable.Group)
            return ammoStorage;
        
        _chatService.Send(player, _translations.AmmoWrongTeam);
        return null;

    }

    private async UniTask RearmKitFromSupplyCrate(WarfarePlayer player, IAmmoStorage ammoStorage, CancellationToken token)
    {
        try
        {
            await _rearmService.RearmAsync(player, ammoStorage, token);
        }
        finally
        {
            (ammoStorage as ITemporaryAmmoStorage)?.Dispose();
        }
    }

    private async UniTask OpenKitUIFromSupplyCrate(WarfarePlayer player, IAmmoStorage ammoStorage, CancellationToken token)
    {
        try
        {
            await _kitUi!.OpenAsync(
                new KitSelectionUI.OpenParameters(player)
                {
                    AmmoStorage = ammoStorage,
                    FactionId = ammoStorage.Team.Faction.PrimaryKey
                },
                token
            );
        }
        catch
        {
            (ammoStorage as ITemporaryAmmoStorage)?.Dispose();
            throw;
        }
    }

    [EventListener(Priority = -1)]
    public async UniTask HandleEventAsync(PlayerEnteredZone e, IServiceProvider serviceProvider, CancellationToken token = default)
    {
        if (e.Zone.Type is not ZoneType.MainBase)
            return;
        
        if (_zoneStore == null || _zoneStore.IsInWarRoom(e.Player) || e.Player.Component<KitPlayerComponent>().HasPreviewKit)
            return;
        
        await _kitRequestService.RestockKitAsync(e.Player, true, token);
    }

    [EventListener(Priority = -1)]
    public async UniTask HandleEventAsync(PlayerExitedZone e, IServiceProvider serviceProvider, CancellationToken token = default)
    {
        if (e.Zone.Type is not ZoneType.WarRoom)
            return;

        if (_zoneStore == null)
            return;

        KitPlayerComponent component = e.Player.Component<KitPlayerComponent>();
        if (!_zoneStore.IsInMainBase(e.Player) || _zoneStore.IsInWarRoom(e.Player))
            return;

        switch (component.ActiveKit)
        {
            case { IsPreview: true }:
                await _kitRequestService.RevertPreviewAsync(e.Player, token);
                return;

            case { Class: > Class.Unarmed }:
                await _kitRequestService.RestockKitAsync(e.Player, true, token);
                break;
        }
    }
}
