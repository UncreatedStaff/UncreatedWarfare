using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using SDG.Framework.Water;
using System;
using System.Linq;
using Uncreated.Warfare.Configuration;
using Uncreated.Warfare.Events;
using Uncreated.Warfare.Events.Models;
using Uncreated.Warfare.Events.Models.Barricades;
using Uncreated.Warfare.Events.Models.Buildables;
using Uncreated.Warfare.Fobs;
using Uncreated.Warfare.FOBs.SupplyCrates;
using Uncreated.Warfare.Interaction;
using Uncreated.Warfare.Kits;
using Uncreated.Warfare.Kits.Items;
using Uncreated.Warfare.Kits.Whitelists;
using Uncreated.Warfare.Players.Extensions;
using Uncreated.Warfare.Players.Permissions;
using Uncreated.Warfare.Translations;
using Uncreated.Warfare.Util;
using Uncreated.Warfare.Zones;

namespace Uncreated.Warfare.FOBs.Construction.Tweaks;

public class FobPlacementTweaks :
    IAsyncEventListener<IPlaceBuildableRequestedEvent>
{
    private readonly AssetConfiguration _assetConfiguration;
    private readonly FobManager _fobManager;
    private readonly UserPermissionStore _userPermissionStore;
    private readonly FobTranslations _translations;
    private readonly IKitItemResolver _kitItemResolver;

    public FobPlacementTweaks(AssetConfiguration assetConfiguration, TranslationInjection<FobTranslations> translations, FobManager fobManager, UserPermissionStore userPermissionStore, IKitItemResolver kitItemResolver)
    {
        _assetConfiguration = assetConfiguration;
        _fobManager = fobManager;
        _userPermissionStore = userPermissionStore;
        _kitItemResolver = kitItemResolver;
        _translations = translations.Value;
    }

    [EventListener(RequiresMainThread = true, Priority = 1 /* before WhitelistService */)]
    public async UniTask HandleEventAsync(IPlaceBuildableRequestedEvent e, IServiceProvider serviceProvider, CancellationToken token = default)
    {
        if (!_assetConfiguration.GetAssetLink<ItemPlaceableAsset>("Buildables:Gameplay:FobUnbuilt").MatchAsset(e.Asset))
        {
            return;
        }

        if (e.OriginalPlacer == null)
            return;
        
        ChatService chatService = serviceProvider.GetRequiredService<ChatService>();

        NearbySupplyCrates supplyCrates = NearbySupplyCrates.FindNearbyCrates(e.Position, e.OriginalPlacer.Team.GroupId, _fobManager);

        if (supplyCrates.BuildCount == 0)
        {
            if (!await _userPermissionStore.HasPermissionAsync(e.OriginalPlacer, WhitelistService.PermissionPlaceBuildable, token))
            {
                chatService.Send(e.OriginalPlacer, _translations.BuildFOBNoSupplyCrate);
                e.Cancel();
            }
            return;
        }

        ShovelableInfo? shovelableInfo = _fobManager.Configuration.Shovelables
            .FirstOrDefault(s => s.Foundation != null && s.Foundation.Guid == e.Asset.GUID);
        if (shovelableInfo != null && supplyCrates.BuildCount < shovelableInfo.SupplyCost)
        {
            chatService.Send(e.OriginalPlacer, _translations.BuildMissingSupplies, supplyCrates.BuildCount, shovelableInfo.SupplyCost);
            e.Cancel();
            return;
        }

        int maxNumberOfFobs = _fobManager.Configuration.GetValue("MaxNumberOfFobs", 10);
        bool fobLimitReached = _fobManager.FriendlyBunkerFobs(e.OriginalPlacer.Team).Count() >= maxNumberOfFobs;
        if (fobLimitReached)
        {
            chatService.Send(e.OriginalPlacer, _translations.BuildMaxFOBsHit);
            e.Cancel();
            return;
        }

        float minDistanceBetweenFobs = _fobManager.Configuration.GetValue("MinDistanceBetweenFobs", 150f);
        BunkerFob? tooCloseFob = _fobManager.FriendlyBunkerFobs(e.OriginalPlacer.Team).FirstOrDefault(f =>
            MathUtility.WithinRange(e.Position, f.Position, minDistanceBetweenFobs)
        );

        if (tooCloseFob != null)
        {
            chatService.Send(e.OriginalPlacer, _translations.BuildFOBTooClose, tooCloseFob, Vector3.Distance(tooCloseFob.Position, e.Position), minDistanceBetweenFobs);
            e.Cancel();
            return;
        }

        float minFobDistanceFromMain = _fobManager.Configuration.GetValue<float>("MinFobDistanceFromMain", 300);

        ZoneStore? zoneStore = serviceProvider.GetService<ZoneStore>();
        if (zoneStore != null)
        {
            Zone? mainBase = zoneStore.FindClosestZone(e.Position, ZoneType.MainBase);

            if (mainBase != null && MathUtility.WithinRange(mainBase.Center, e.Position, minFobDistanceFromMain))
            {
                chatService.Send(e.OriginalPlacer, _translations.BuildFOBTooCloseToMain);
                e.Cancel();
                return;
            }
        }

        if (WaterUtility.isPointUnderwater(e.Position))
        {
            chatService.Send(e.OriginalPlacer, _translations.BuildFOBUnderwater);
            e.Cancel();
            return;
        }
    }
}
