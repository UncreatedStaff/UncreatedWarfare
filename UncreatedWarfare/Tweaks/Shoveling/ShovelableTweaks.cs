using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Text;
using Uncreated.Warfare.Buildables;
using Uncreated.Warfare.Configuration;
using Uncreated.Warfare.Events.Models;
using Uncreated.Warfare.Events.Models.Players;
using Uncreated.Warfare.Fobs;
using Uncreated.Warfare.FOBs;
using Uncreated.Warfare.FOBs.Construction;
using Uncreated.Warfare.Translations;
using Uncreated.Warfare.Tweaks.BuildablePlacement;
using Uncreated.Warfare.Util.Containers;

namespace Uncreated.Warfare.Tweaks.Shoveling;
internal class ShovelableTweaks : IEventListener<MeleeHit>
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger _logger;
    private readonly AssetConfiguration? _assetConfiguration;

    public ShovelableTweaks(IServiceProvider serviceProvider, ILogger<ShovelableTweaks> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
        _assetConfiguration = serviceProvider.GetService<AssetConfiguration>();
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

        if (!raycast.transform.TryGetComponent(out BuildableContainer container))
            return;

        if (container.Buildable.Group != e.Player.Team.GroupId)
            return;

        if (!container.TryGetFromContainer(out IShovelable? shovelable))
            return;

        shovelable!.Shovel(e.Player, raycast.point);
    }
}
