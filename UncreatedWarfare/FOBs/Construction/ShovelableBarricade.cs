using SDG.Unturned;
using System;
using System.Collections.Generic;
using System.Text;
using Uncreated.Warfare.Buildables;
using Uncreated.Warfare.Configuration;
using Uncreated.Warfare.Players;
using Uncreated.Warfare.Util;

namespace Uncreated.Warfare.FOBs.Construction;
internal class ShovelableBarricade : PointsShoveable
{
    private readonly ShovelableInfo _info;
    private readonly BarricadeDrop _foundation;

    public ShovelableBarricade(ShovelableInfo info, BarricadeDrop foundation, int hitsRemaining) : base(hitsRemaining)
    {
        _info = info;
        _foundation = foundation;
    }

    public override void Complete(WarfarePlayer shoveler)
    {
        // drop the barricade
        Transform transform = BarricadeManager.dropNonPlantedBarricade(
            new Barricade(_info.CompletedStructure.GetAssetOrFail()),
            _foundation.model.position,
            _foundation.model.rotation,
            _foundation.GetServersideData().owner,
            _foundation.GetServersideData().group
        );

        BarricadeDrop barricade = BarricadeManager.FindBarricadeByRootTransform(transform);

        if (_info.CompletedEffect != null)
        {
            EffectManager.triggerEffect(new TriggerEffectParameters(_info.CompletedEffect.GetAssetOrFail())
            {
                position = _foundation.model.position,
                relevantDistance = 70,
                reliable = true
            });
        }

        //_onConvertedToBuildable?.Invoke(new BuildableBarricade(barricade));
        
    }
}
