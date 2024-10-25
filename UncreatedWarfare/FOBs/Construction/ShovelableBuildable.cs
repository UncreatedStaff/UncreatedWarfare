using Microsoft.Extensions.DependencyInjection;
using SDG.Unturned;
using System;
using System.Collections.Generic;
using System.Text;
using Uncreated.Warfare.Buildables;
using Uncreated.Warfare.Configuration;
using Uncreated.Warfare.Players;
using Uncreated.Warfare.Util;
using Uncreated.Warfare.Vehicles;
using UnityEngine;

namespace Uncreated.Warfare.FOBs.Construction;
internal class ShovelableBuildable : PointsShoveable
{
    private readonly ShovelableInfo _info;
    private readonly IBuildable _foundation;
    private readonly IServiceProvider _serviceProvider;

    public Action<IBuildable?>? OnComplete { get; set; }

    public ShovelableBuildable(ShovelableInfo info, IBuildable foundation, IAssetLink<EffectAsset> shovelEffect, IServiceProvider serviceProvider) : base(info, foundation, shovelEffect)
    {
        _info = info;
        _foundation = foundation;
        _serviceProvider = serviceProvider;
    }

    public override void Complete(WarfarePlayer shoveler)
    {
        IBuildable? completedBuildable = null;
        if (_info.CompletedStructure.TryGetAsset(out ItemPlaceableAsset? completedAsset))
        {
            if (completedAsset is not ItemBarricadeAsset barricadeAsset)
                throw new NotSupportedException("Shoveable structures are not yet supported.");

            // drop the barricade
            Transform transform = BarricadeManager.dropNonPlantedBarricade(
                new Barricade(barricadeAsset),
                _foundation.Position,
                _foundation.Rotation,
                _foundation.Owner.m_SteamID,
                _foundation.Group.m_SteamID
            );
            completedBuildable = new BuildableBarricade(BarricadeManager.FindBarricadeByRootTransform(transform));
        }

        if (Info.Emplacement != null)
            DropEmplacement(Info.Emplacement);

        if (_info.CompletedEffect != null)
        {
            EffectManager.triggerEffect(new TriggerEffectParameters(_info.CompletedEffect.GetAssetOrFail())
            {
                position = _foundation.Position,
                relevantDistance = 70,
                reliable = true
            });
        }

        OnComplete?.Invoke(completedBuildable);
        _foundation.Destroy(); // make sure to only destroy the foundation events are invoked
    }
    private void DropEmplacement(EmplacementInfo emplacementInfo)
    {
         _ = _serviceProvider.GetRequiredService<VehicleService>().SpawnVehicleAsync(
            emplacementInfo.Vehicle,
            new Vector3(_foundation.Position.x, _foundation.Position.y + 2, _foundation.Position.z),
            // rotate x + 90 degrees because nelson sucks
            Quaternion.Euler(_foundation.Rotation.eulerAngles.x + 90, _foundation.Rotation.eulerAngles.y, _foundation.Rotation.eulerAngles.z),
            _foundation.Owner,
            _foundation.Group);
    }
}
