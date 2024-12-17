using Microsoft.Extensions.DependencyInjection;
using SDG.Framework.Water;
using System;
using System.Collections.Generic;
using System.Text;
using Uncreated.Warfare.Configuration;
using Uncreated.Warfare.Events.Models;
using Uncreated.Warfare.Events.Models.Barricades;
using Uncreated.Warfare.FOBs.SupplyCrates;
using Uncreated.Warfare.Fobs;
using Uncreated.Warfare.FOBs;
using Uncreated.Warfare.Interaction;
using Uncreated.Warfare.Translations;
using Uncreated.Warfare.Zones;
using Microsoft.Extensions.Configuration;
using Uncreated.Warfare.Util;
using System.Linq;
using Uncreated.Warfare.Commands;
using Uncreated.Warfare.Kits;
using Uncreated.Warfare.Util.Containers;
using Uncreated.Warfare.Buildables;
using Uncreated.Warfare.FOBs.Construction;

namespace Uncreated.Warfare.Tweaks.BuildablePlacement;
internal class ShoveablePlacementTweaks :
    IEventListener<PlaceBarricadeRequested>
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger _logger;
    private readonly AssetConfiguration? _assetConfiguration;
    private readonly FobManager? _fobManager;
    private readonly FobTranslations? _translations;

    public ShoveablePlacementTweaks(IServiceProvider serviceProvider, ILogger<GuidedMissileLaunchTweaks> logger)
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

        if (e.OriginalPlacer == null)
            return;

        ShovelableInfo? shovelableInfo = (_fobManager.Configuration.GetRequiredSection("Shovelables").Get<IEnumerable<ShovelableInfo>>() ?? Array.Empty<ShovelableInfo>())
            .FirstOrDefault(s => s.Foundation != null && s.Foundation.Guid == e.Asset.GUID);

        if (shovelableInfo == null)
            return;

        ChatService chatService = serviceProvider.GetRequiredService<ChatService>();

        BuildableFob? nearestFob = _fobManager.FindNearestBuildableFob(e.OriginalPlacer.Team, e.Position);

        if (nearestFob == null)
        {
            chatService.Send(e.OriginalPlacer, _translations.BuildNotInRadius);
            return;
        }

        if (nearestFob.BuildCount < shovelableInfo.SupplyCost)
        {
            chatService.Send(e.OriginalPlacer, _translations.BuildMissingSupplies, nearestFob.BuildCount, shovelableInfo.SupplyCost);
            e.Cancel();
            return;
        }

        //int existingCount = nearestFob.Items.Collection.Count(i => 
        //    shovelableInfo.CompletedStructure.MatchAsset(i.Buildable.Asset));
        //if ()
        //{
        //    chatService.Send(e.OriginalPlacer, _translations.BuildMissingSupplies, nearestFob.BuildCount, shovelableInfo.SupplyCost);
        //    e.Cancel();
        //    return;
        //}
    }
}
