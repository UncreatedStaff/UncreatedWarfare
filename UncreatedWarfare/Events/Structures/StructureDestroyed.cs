using SDG.Unturned;
using Uncreated.SQL;
using Uncreated.Warfare.Structures;
using UnityEngine;

namespace Uncreated.Warfare.Events.Structures;
public class StructureDestroyed : EventState, IBuildableDestroyedEvent
{
    private readonly UCPlayer? instigator;
    private readonly ulong instigatorId;
    private readonly StructureDrop drop;
    private readonly StructureData data;
    private readonly StructureRegion region;
    private readonly byte x;
    private readonly byte y;
    private readonly bool _wasSaved;
    private readonly bool _wasPickedUp;
    private readonly Vector3 _ragdoll;
    private readonly SqlItem<SavedStructure>? _save;
    private IBuildable? _buildable;
    public UCPlayer? Instigator => instigator;
    public ulong InstigatorId => instigatorId;
    public StructureDrop Structure => drop;
    public StructureData ServersideData => data;
    public StructureRegion Region => region;
    public Transform Transform => drop.model;
    public byte RegionPosX => x;
    public byte RegionPosY => y;
    public uint InstanceID => drop.instanceID;
    public bool IsSaved => _wasSaved;
    public bool WasPickedUp => _wasPickedUp;
    public Vector3 Ragdoll => _ragdoll;
    public SqlItem<SavedStructure>? Save => _save;
    public IBuildable Buildable => _buildable ??= new UCStructure(Structure);
    object IBuildableDestroyedEvent.Region => Region;
    public EDamageOrigin DamageOrigin { get; }
    internal StructureDestroyed(UCPlayer? instigator, ulong instigatorId, StructureDrop structure, StructureData structureData, StructureRegion region, byte x, byte y, SqlItem<SavedStructure>? save, Vector3 ragoll, bool wasPickedUp, EDamageOrigin damageOrigin) : base()
    {
        this.instigator = instigator;
        this.instigatorId = instigatorId;
        drop = structure;
        data = structureData;
        this.region = region;
        this.x = x;
        this.y = y;
        _ragdoll = ragoll;
        _wasPickedUp = wasPickedUp;
        _save = save;
        ListSqlConfig<SavedStructure>? m = save?.Manager;
        DamageOrigin = damageOrigin;
        if (m is not null)
        {
            m.WriteWait();
            try
            {
                if (save!.Item != null)
                {
                    _buildable = save.Item.Buildable;
                    _wasSaved = true;
                }
            }
            finally
            {
                m.WriteRelease();
            }
        }
    }
}