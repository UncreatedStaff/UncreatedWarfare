using System;
using System.Collections.Generic;
using System.Text;
using Uncreated.Warfare.Configuration;

namespace Uncreated.Warfare.FOBs.Construction;
internal class ShovelableInfo
{
    required public IAssetLink<ItemPlaceableAsset> FoundationBuildable { get; set; }
    required public ShovelableType ConstuctionType { get; set; }
    required public int RequiredHits { get; set; }
    public IAssetLink<ItemPlaceableAsset>? CompletedStructure { get; set; }
    public IAssetLink<EffectAsset>? CompletedEffect { get; set; }
    public EmplacementInfo? Emplacement { get; set; }
    public override string ToString()
    {
        return $"ShovelableInfo:\n" +
               $"  FoundationBuildable: {FoundationBuildable}\n" +
               $"  ConstructionType: {ConstuctionType}\n" +
               $"  RequiredHits: {RequiredHits}\n" +
               $"  CompletedStructure: {(CompletedStructure?.ToString() ?? "None")}\n" +
               $"  CompletedEffect: {(CompletedEffect?.ToString() ?? "None")}\n" +
               $"  Emplacement: {(Emplacement?.ToString() ?? "None")}";
    }
}
