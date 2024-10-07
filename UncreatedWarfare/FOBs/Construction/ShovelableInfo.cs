using System;
using System.Collections.Generic;
using System.Text;
using Uncreated.Warfare.Configuration;

namespace Uncreated.Warfare.FOBs.Construction;
internal class ShovelableInfo
{
    required public IAssetLink<ItemBarricadeAsset> FoundationBuildable;
    required public ShovelableType ConstuctionType;
    public IAssetLink<ItemBarricadeAsset>? CompletedStructure;
    public IAssetLink<EffectAsset>? CompletedEffect;
    public EmplacementInfo? Emplacement;
    
}
