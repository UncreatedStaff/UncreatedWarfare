﻿using Microsoft.Extensions.DependencyInjection;
using SDG.Framework.Water;
using System;
using System.Collections.Generic;
using System.Text;
using Uncreated.Warfare.Configuration;
using Uncreated.Warfare.Events.Models;
using Uncreated.Warfare.Events.Models.Barricades;
using Uncreated.Warfare.FOBs.SupplyCrates;
using Uncreated.Warfare.Fobs;
using Uncreated.Warfare.Interaction;
using Uncreated.Warfare.Translations;
using Uncreated.Warfare.Zones;
using Microsoft.Extensions.Configuration;
using Uncreated.Warfare.Util;
using System.Linq;
using Uncreated.Warfare.Commands;
using Uncreated.Warfare.Kits;
using Uncreated.Warfare.Util.Containers;

namespace Uncreated.Warfare.FOBs.Construction.Tweaks;
public class FobPlacementTweaks :
    IEventListener<PlaceBarricadeRequested>
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger _logger;
    private readonly AssetConfiguration? _assetConfiguration;
    private readonly FobManager? _fobManager;
    private readonly FobTranslations? _translations;

    public FobPlacementTweaks(IServiceProvider serviceProvider, ILogger<FobPlacementTweaks> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
        _assetConfiguration = serviceProvider.GetService<AssetConfiguration>();
        _fobManager = serviceProvider.GetService<FobManager>();
        _translations = serviceProvider.GetService<TranslationInjection<FobTranslations>>()?.Value;
    }
    public void HandleEvent(PlaceBarricadeRequested e, IServiceProvider serviceProvider)
    {
        if (_assetConfiguration == null || _fobManager == null || _translations == null)
            return;

        if (_assetConfiguration.GetAssetLink<ItemBarricadeAsset>("Buildables:Fobs:FobUnbuilt").Guid != e.Barricade.asset.GUID)
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

        int maxNumberOfFobs = _fobManager.Configuration.GetValue("MaxNumberOfFobs", 10);
        bool fobLimitReached = _fobManager.FriendlyBuildableFobs(e.OriginalPlacer.Team).Count() >= maxNumberOfFobs;
        if (fobLimitReached)
        {
            chatService.Send(e.OriginalPlacer, _translations.BuildMaxFOBsHit);
            e.Cancel();
            return;
        }

        float minDistanceBetweenFobs = _fobManager.Configuration.GetValue("MinDistanceBetweenFobs", 150f);
        BuildableFob? tooCloseFob = _fobManager.FriendlyBuildableFobs(e.OriginalPlacer.Team).FirstOrDefault(f =>
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