using System;
using System.Collections.Generic;
using System.Linq;
using Uncreated.Warfare.Buildables;
using Uncreated.Warfare.Configuration;
using Uncreated.Warfare.Events.Models;
using Uncreated.Warfare.Events.Models.Buildables;
using Uncreated.Warfare.Events.Models.Players;
using Uncreated.Warfare.Fobs;
using Uncreated.Warfare.FOBs.Entities;
using Uncreated.Warfare.Interaction;
using Uncreated.Warfare.Kits;
using Uncreated.Warfare.Kits.Items;
using Uncreated.Warfare.Translations;
using Uncreated.Warfare.Util;

namespace Uncreated.Warfare.FOBs.Construction.Tweaks;

internal class ShovelableTweaks :
    IEventListener<IPlaceBuildableRequestedEvent>,
    IEventListener<PlayerMeleeRequested>
{
    private readonly AssetConfiguration _assetConfiguration;
    private readonly FobTranslations _translations;
    private readonly ChatService _chatService;
    private readonly IKitItemResolver _kitItemResolver;
    private readonly FobManager _fobManager;

    public ShovelableTweaks(
        AssetConfiguration assetConfiguration,
        TranslationInjection<FobTranslations> translations,
        ChatService chatService,
        IKitItemResolver kitItemResolver,
        FobManager fobManager)
    {
        _assetConfiguration = assetConfiguration;
        _translations = translations.Value;
        _chatService = chatService;
        _kitItemResolver = kitItemResolver;
        _fobManager = fobManager;
    }

    void IEventListener<IPlaceBuildableRequestedEvent>.HandleEvent(IPlaceBuildableRequestedEvent e, IServiceProvider serviceProvider)
    {
        if (_assetConfiguration.GetAssetLink<ItemPlaceableAsset>("Buildables:Gameplay:FobUnbuilt").MatchAsset(e.Asset))
            return;

        ShovelableInfo? shovelableInfo = _fobManager.Configuration.Shovelables
            .FirstOrDefault(s => s.Foundation.MatchAsset(e.Asset));

        if (shovelableInfo == null)
            return;

        if (e.IsOnVehicle)
        {
            _chatService.Send(e.OriginalPlacer, _translations.BuildFOBBuildableInvalidPosition);
            e.Cancel();
            return;
        }

        KitPlayerComponent kitComponent = e.OriginalPlacer.Component<KitPlayerComponent>();

        IAssetLink<ItemPlaceableAsset> buildableAsset = AssetLink.Create(e.Asset);

        bool buildableInKit = false;
        Kit? cachedKit = kitComponent.CachedKit;
        if (cachedKit != null)
        {
            int maxAllowedInKit = _kitItemResolver.CountItems(cachedKit, buildableAsset, e.OriginalPlacer.Team);
            buildableInKit = maxAllowedInKit > 0;
        }

        bool placerIsCombatEngineer = kitComponent.ActiveClass == Class.CombatEngineer;

        // removed because this is handled by WhitelistService already
        //List<IBuildableFobEntity> similarPlacedByPlayer = _fobManager.Entities
        //    .OfType<IBuildableFobEntity>()
        //    .Where(en => en.Buildable.Owner == e.OriginalPlacer.Steam64 && en.IdentifyingAsset.MatchAsset(buildableAsset))
        //    .ToList();

        //if (similarPlacedByPlayer.Count >= maxAllowedInKit && similarPlacedByPlayer.Count > 0)
        //{
        //    IBuildableFobEntity oldest = similarPlacedByPlayer
        //        .Aggregate((oldest, next) => BuildableContainer.Get(oldest.Buildable).CreateTime < BuildableContainer.Get(next.Buildable).CreateTime ? oldest : next);

        //    oldest.Buildable.Destroy();
        //}

        if (!shovelableInfo.MaxAllowedPerFob.HasValue || buildableInKit)
            return;

        ResourceFob? nearestFob = _fobManager?.FindNearestResourceFob(e.OriginalPlacer.Team, e.Position);

        if (nearestFob == null && !(placerIsCombatEngineer && shovelableInfo.CombatEngineerCanPlaceAnywhere))
        {
            _chatService.Send(e.OriginalPlacer, _translations.BuildNotInRadius);
            e.Cancel();
            return;
        }

        if (nearestFob == null)
        {
            // combat engineer off FOB
            return;
        }

        if (nearestFob.BuildCount < shovelableInfo.SupplyCost)
        {
            _chatService.Send(e.OriginalPlacer, _translations.BuildMissingSupplies, nearestFob.BuildCount, shovelableInfo.SupplyCost);
            e.Cancel();
            return;
        }

        IEnumerable<IFobEntity> fobEntities = nearestFob.EnumerateEntities();

        int similarEntitiesCount = fobEntities.Count(en => en.IdentifyingAsset.MatchAsset(buildableAsset));
        if (similarEntitiesCount >= shovelableInfo.MaxAllowedPerFob.Value)
        {
            _chatService.Send(e.OriginalPlacer, _translations.BuildLimitReached, shovelableInfo.MaxAllowedPerFob.Value, shovelableInfo);
            e.Cancel();
        }
    }

    // check melee ShovelableBuildable with entrenching tool
    void IEventListener<PlayerMeleeRequested>.HandleEvent(PlayerMeleeRequested e, IServiceProvider serviceProvider)
    {
        if (!_assetConfiguration.GetAssetLink<ItemAsset>("Items:EntrenchingTool").MatchAsset(e.Asset))
            return;

        IBuildable? buildable = BuildableExtensions.GetBuildableFromRootTransform(e.InputInfo.transform);
        if (buildable == null)
            return;

        if (buildable.Group != e.Player.Team.GroupId)
            return;

        ShovelableBuildable? shovelable = _fobManager.GetBuildableFobEntity<ShovelableBuildable>(buildable);

        if (shovelable == null)
        {
            return;
        }

        if (!e.Player.Team.IsFriendly(shovelable.Buildable.Group))
        {
            _chatService.Send(e.Player, _translations.ShovelableNotFriendly);
            return;
        }
        
        shovelable?.Shovel(e.Player, e.InputInfo.point);
    }
}