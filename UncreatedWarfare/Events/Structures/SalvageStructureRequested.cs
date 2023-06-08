using SDG.Unturned;
using Uncreated.SQL;
using Uncreated.Warfare.Structures;
using UnityEngine;

namespace Uncreated.Warfare.Events.Structures;
public sealed class SalvageStructureRequested : SalvageRequested
{
    private readonly StructureDrop _drop;
    private readonly StructureData _data;
    public StructureDrop Structure => _drop;
    public StructureData ServersideData => _data;
    public StructureRegion Region => (StructureRegion)RegionObj;
    public override IBuildable Buildable => BuildableCache ??= new UCStructure(Structure);
    internal SalvageStructureRequested(UCPlayer instigator, StructureDrop structure, StructureData structureData, StructureRegion region, byte x, byte y, SqlItem<SavedStructure>? save)
        : base(instigator, region, x, y, structure.instanceID)
    {
        _drop = structure;
        _data = structureData;
        Transform = structure.model;
        StructureSave = save;
        ListSqlConfig<SavedStructure>? m = save?.Manager;
        if (m is not null)
        {
            m.WriteWait();
            try
            {
                if (save!.Item != null)
                {
                    BuildableCache = save.Item.Buildable;
                    IsStructureSaved = true;
                }
            }
            finally
            {
                m.WriteRelease();
            }
        }
    }
}