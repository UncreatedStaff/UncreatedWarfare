using SDG.Unturned;
using Uncreated.SQL;
using Uncreated.Warfare.Structures;
using UnityEngine;

namespace Uncreated.Warfare.Events.Structures;
public class DamageStructureRequested : BreakableEvent, IBuildableDestroyedEvent
{
    private readonly UCPlayer? _instigator;
    private readonly StructureDrop _drop;
    private readonly StructureData _data;
    private readonly StructureRegion _region;
    private readonly byte _x;
    private readonly byte _y;
    private readonly bool _isSaved;
    private readonly SqlItem<SavedStructure>? _save;
    private readonly EDamageOrigin _damageOrigin;
    private IBuildable? _buildable;
    public UCPlayer? Instigator => _instigator;
    public StructureDrop Structure => _drop;
    public StructureData ServersideData => _data;
    public StructureRegion Region => _region;
    public Transform Transform => _drop.model;
    public byte RegionPosX => _x;
    public byte RegionPosY => _y;
    public uint InstanceID => _drop.instanceID;
    public bool IsSaved => _isSaved;
    public EDamageOrigin DamageOrigin => _damageOrigin;
    public SqlItem<SavedStructure>? Save => _save;
    public IBuildable Buildable => _buildable ??= new UCStructure(Structure);
    object IBuildableDestroyedEvent.Region => Region;
    public ushort PendingDamage { get; set; }
    internal DamageStructureRequested(UCPlayer? instigator, StructureDrop structure, StructureData structureData, StructureRegion region, byte x, byte y, SqlItem<SavedStructure>? save, EDamageOrigin damageOrigin, ushort pendingTotalDamage)
    {
        this._damageOrigin = damageOrigin;
        this._instigator = instigator;
        this._drop = structure;
        this._data = structureData;
        this._region = region;
        this._x = x;
        this._y = y;
        PendingDamage = pendingTotalDamage;
        if (save is not null)
        {
            _save = save;
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