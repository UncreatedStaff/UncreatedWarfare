using SDG.Unturned;
using Uncreated.SQL;
using Uncreated.Warfare.Structures;
using UnityEngine;

namespace Uncreated.Warfare.Events.Structures;
public class SalvageStructureRequested : BreakablePlayerEvent, IBuildableDestroyedEvent
{
    private readonly StructureDrop drop;
    private readonly StructureData data;
    private readonly StructureRegion region;
    private readonly byte x;
    private readonly byte y;
    private readonly bool _isSaved;
    private readonly SqlItem<SavedStructure>? _save;
    private IBuildable? _buildable;
    public StructureDrop Structure => drop;
    public StructureData ServersideData => data;
    public StructureRegion Region => region;
    public Transform Transform => drop.model;
    public byte RegionPosX => x;
    public byte RegionPosY => y;
    public uint InstanceID => drop.instanceID;
    public bool IsSaved => _isSaved;
    public SqlItem<SavedStructure>? Save => _save;
    public IBuildable Buildable => _buildable ??= new UCStructure(Structure);
    object IBuildableDestroyedEvent.Region => Region;
    UCPlayer? IBuildableDestroyedEvent.Instigator => Player;
    internal SalvageStructureRequested(UCPlayer instigator, StructureDrop structure, StructureData structureData, StructureRegion region, byte x, byte y, SqlItem<SavedStructure>? save) : base(instigator)
    {
        this.drop = structure;
        this.data = structureData;
        this.region = region;
        this.x = x;
        this.y = y;
        if (save is not null)
        {
            save.EnterSync();
            try
            {
                if (save.Item != null)
                {
                    _buildable = save.Item.Buildable;
                    _isSaved = true;
                }
            }
            finally
            {
                save.Release();
            }
        }
    }
}