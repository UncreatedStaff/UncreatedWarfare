using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using SDG.Framework.Water;
using System;
using System.Linq;
using Uncreated.Warfare.Configuration;
using Uncreated.Warfare.Events.Models;
using Uncreated.Warfare.Events.Models.Barricades;
using Uncreated.Warfare.Events.Models.Buildables;
using Uncreated.Warfare.Fobs;
using Uncreated.Warfare.FOBs.SupplyCrates;
using Uncreated.Warfare.Interaction;
using Uncreated.Warfare.Translations;
using Uncreated.Warfare.Util;
using Uncreated.Warfare.Zones;

namespace Uncreated.Warfare.FOBs.Construction.Tweaks;

public class FobPlacementTweaks :
    IEventListener<IPlaceBuildableRequestedEvent>
{
    private readonly AssetConfiguration _assetConfiguration;
    private readonly FobManager _fobManager;
    private readonly FobTranslations _translations;

    public FobPlacementTweaks(AssetConfiguration assetConfiguration, TranslationInjection<FobTranslations> translations, FobManager fobManager)
    {
        _assetConfiguration = assetConfiguration;
        _fobManager = fobManager;
        _translations = translations.Value;
    }

    public void HandleEvent(IPlaceBuildableRequestedEvent e, IServiceProvider serviceProvider)
    {
        if (_assetConfiguration.GetAssetLink<ItemPlaceableAsset>("Buildables:Gameplay:FobUnbuilt").MatchAsset(e.Asset))
            return;

        if (e.OriginalPlacer == null)
            return;

        ChatService chatService = serviceProvider.GetRequiredService<ChatService>();

        NearbySupplyCrates supplyCrates = NearbySupplyCrates.FindNearbyCrates(e.Position, e.OriginalPlacer.Team.GroupId, _fobManager);

        if (supplyCrates.BuildCount == 0)
        {
            chatService.Send(e.OriginalPlacer, _translations.BuildFOBNoSupplyCrate);
            e.Cancel();
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
