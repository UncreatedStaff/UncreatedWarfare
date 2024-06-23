using SDG.Unturned;
using Uncreated.Warfare.Buildables;
using Uncreated.Warfare.Models.Assets;
using Uncreated.Warfare.Models.Buildables;

namespace Uncreated.Warfare.Events.Structures;
public sealed class SalvageStructureRequested : SalvageRequested
{
    private readonly StructureDrop _drop;
    private readonly StructureData _data;
    public StructureDrop Structure => _drop;
    public StructureData ServersideData => _data;
    public StructureRegion Region => (StructureRegion)RegionObj;
    public override IBuildable Buildable => BuildableCache ??= new BuildableStructure(Structure);
    internal SalvageStructureRequested(UCPlayer instigator, StructureDrop structure, StructureData structureData, StructureRegion region, byte x, byte y, BuildableSave? save, UnturnedAssetReference primaryAsset, UnturnedAssetReference secondaryAsset)
        : base(instigator, region, x, y, structure.instanceID, primaryAsset, secondaryAsset)
    {
        _drop = structure;
        _data = structureData;
        Transform = structure.model;
        BuildableSave = save;
        BuildableCache = save?.Buildable;
    }
}