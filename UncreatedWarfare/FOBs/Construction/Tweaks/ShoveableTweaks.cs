using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;
using Uncreated.Warfare.Buildables;
using Uncreated.Warfare.Configuration;
using Uncreated.Warfare.Events.Models;
using Uncreated.Warfare.Events.Models.Barricades;
using Uncreated.Warfare.Events.Models.Players;
using Uncreated.Warfare.Fobs;
using Uncreated.Warfare.FOBs.Entities;
using Uncreated.Warfare.Interaction;
using Uncreated.Warfare.Kits;
using Uncreated.Warfare.Kits.Items;
using Uncreated.Warfare.Translations;
using Uncreated.Warfare.Util;

namespace Uncreated.Warfare.FOBs.Construction.Tweaks;

internal class ShoveableTweaks :
    IEventListener<PlaceBarricadeRequested>,
    IEventListener<MeleeHit>
{
    private readonly AssetConfiguration? _assetConfiguration;
    private readonly FobTranslations _translations;
    private readonly ChatService _chatService;
    private readonly IKitItemResolver _kitItemResolver;

    public ShoveableTweaks(IServiceProvider serviceProvider)
    {
        _kitItemResolver = serviceProvider.GetRequiredService<IKitItemResolver>();
        _assetConfiguration = serviceProvider.GetService<AssetConfiguration>();
        _translations = serviceProvider.GetRequiredService<TranslationInjection<FobTranslations>>().Value;
        _chatService = serviceProvider.GetRequiredService<ChatService>();
    }
    public void HandleEvent(PlaceBarricadeRequested e, IServiceProvider serviceProvider)
    {
        if (e.OriginalPlacer == null)
            return;

        FobManager? fobManager = serviceProvider.GetService<FobManager>();

        if (_assetConfiguration?.GetAssetLink<ItemBarricadeAsset>("Buildables:Fobs:FobUnbuilt").Guid == e.Barricade.asset.GUID)
            return;

        ShovelableInfo? shovelableInfo = (fobManager?.Configuration.GetRequiredSection("Shovelables").Get<IEnumerable<ShovelableInfo>>() ?? Array.Empty<ShovelableInfo>())
            .FirstOrDefault(s => s.Foundation != null && s.Foundation.Guid == e.Asset.GUID);

        if (shovelableInfo == null)
            return;

        KitPlayerComponent kitComponent = e.OriginalPlacer.Component<KitPlayerComponent>();

        bool enforcePerFobMax = shovelableInfo.MaxAllowedPerFob != null;

        IAssetLink<ItemBarricadeAsset> barricade = AssetLink.Create(e.Barricade.asset);

        bool barricadeInKit = false;
        Kit? cachedKit = kitComponent.CachedKit;
        if (cachedKit != null)
        {
            barricadeInKit = _kitItemResolver.ContainsItem(cachedKit, barricade, e.OriginalPlacer.Team);
        }

        //bool placerIsCombatEngineer = kitComponent.ActiveClass == Class.CombatEngineer;
        //int maxAllowedInKit = kitComponent.CachedKit?.ItemModels.Count(i => i.Item.GetValueOrDefault().GetAssetLink<Asset>().MatchAsset(e.Barricade.asset)) ?? 0;
        //IEnumerable<IBuildableFobEntity> similarPlacedByPlayer = _fobManager.Entities.OfType<IBuildableFobEntity>().Where(en =>
        //        en.Buildable.Owner == e.OriginalPlacer.Steam64 &&
        //        en.IdentifyingAsset.MatchAsset(e.Barricade.asset));

        //if (similarPlacedByPlayer.Count() >= maxAllowedInKit)
        //{
        //    // todo: remove the oldest barricade
        //    IBuildableFobEntity oldest = similarPlacedByPlayer.OrderBy(f => f.Buildable.Model.GetOrAddComponent<BuildableContainer>().CreateTime).First();
        //    oldest.Buildable.Destroy();
        //}
        if (enforcePerFobMax && !barricadeInKit)
        {
            ResourceFob? nearestFob = fobManager?.FindNearestResourceFob(e.OriginalPlacer.Team, e.Position);

            if (nearestFob == null)
            {
                _chatService.Send(e.OriginalPlacer, _translations.BuildNotInRadius);
                e.Cancel();
                return;
            }

            if (nearestFob.BuildCount < shovelableInfo.SupplyCost)
            {
                _chatService.Send(e.OriginalPlacer, _translations.BuildMissingSupplies, nearestFob.BuildCount, shovelableInfo.SupplyCost);
                e.Cancel();
                return;
            }

            IEnumerable<IFobEntity> fobEntities = nearestFob.GetEntities();

            int similarEntitiesCount = fobEntities.Count(en => en.IdentifyingAsset.MatchAsset(e.Barricade.asset));
            if (similarEntitiesCount >= shovelableInfo.MaxAllowedPerFob)
            {
                _chatService.Send(e.OriginalPlacer, _translations.BuildLimitReached, shovelableInfo.MaxAllowedPerFob.Value, shovelableInfo);
                e.Cancel();
                return;
            }
        }
    }
    public void HandleEvent(MeleeHit e, IServiceProvider serviceProvider)
    {
        if (_assetConfiguration == null)
            return;

        if (e.Equipment?.asset?.GUID == null)
            return;

        IAssetLink<ItemAsset> entrenchingTool = _assetConfiguration.GetAssetLink<ItemAsset>("Items:EntrenchingTool");
        if (entrenchingTool.GetAssetOrFail().GUID != e.Equipment.asset.GUID)
            return;

        RaycastInfo raycast = DamageTool.raycast(new Ray(e.Look.aim.position, e.Look.aim.forward), 2, RayMasks.BARRICADE, e.Player.UnturnedPlayer);
        if (raycast.transform == null)
            return;

        IBuildable? buildable = BuildableExtensions.GetBuildableFromRootTransform(raycast.transform);
        if (buildable == null)
            return;

        if (buildable.Group != e.Player.Team.GroupId)
            return;

        FobManager? fobManager = serviceProvider.GetService<FobManager>();
        if (fobManager == null)
            return;

        ShovelableBuildable? shovelable = fobManager.GetBuildableFobEntity<ShovelableBuildable>(buildable);
        if (shovelable == null)
            return;

        shovelable.Shovel(e.Player, raycast.point);
    }
}
