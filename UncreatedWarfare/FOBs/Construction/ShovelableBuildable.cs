using SDG.Unturned;
using System;
using System.Collections.Generic;
using System.Text;
using Uncreated.Warfare.Buildables;
using Uncreated.Warfare.Configuration;
using Uncreated.Warfare.Players;
using Uncreated.Warfare.Util;

namespace Uncreated.Warfare.FOBs.Construction;
internal class ShovelableBuildable : PointsShoveable
{
    private readonly ShovelableInfo _info;
    private readonly IBuildable _foundation;

    public ShovelableBuildable(ShovelableInfo info, IBuildable foundation, IAssetLink<EffectAsset> shovelEffect) : base(info.RequiredHits, foundation, shovelEffect)
    {
        _info = info;
        _foundation = foundation;
    }

    public override void Complete(WarfarePlayer shoveler)
    {
        ItemPlaceableAsset asset = _info.CompletedStructure.GetAssetOrFail();
        if (asset is not ItemBarricadeAsset barricadeAsset)
            throw new NotSupportedException("Shoveable structures are not yet supported.");


        // drop the barricade
        Transform transform = BarricadeManager.dropNonPlantedBarricade(
            new Barricade(barricadeAsset),
            _foundation.Position,
            _foundation.Rotation,
            _foundation.Owner.m_SteamID,
            _foundation.Group.m_SteamID
        );

        //BarricadeDrop barricade = BarricadeManager.FindBarricadeByRootTransform(transform);

        if (_info.CompletedEffect != null)
        {
            EffectManager.triggerEffect(new TriggerEffectParameters(_info.CompletedEffect.GetAssetOrFail())
            {
                position = _foundation.Position,
                relevantDistance = 70,
                reliable = true
            });
        }

        _foundation.Destroy();

        //_onConvertedToBuildable?.Invoke(new BuildableBarricade(barricade));

    }
}
