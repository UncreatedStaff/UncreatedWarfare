using Uncreated.Warfare.Buildables;
using Uncreated.Warfare.Models.Assets;

namespace Uncreated.Warfare.Events.Models.Structures;
public class DamageStructureRequested : CancellableEvent, IBuildableDestroyedEvent
{
    private readonly UCPlayer? _instigator;
    private readonly ulong _instigatorId;
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
    public ulong InstigatorId => _instigatorId;
    public StructureDrop Structure => _drop;
    public StructureData ServersideData => _data;
    public StructureRegion Region => _region;
    public Transform Transform => _drop.model;
    public byte RegionPosX => _x;
    public byte RegionPosY => _y;
    public uint InstanceID => _drop.instanceID;
    public bool IsSaved => _isSaved;
    public EDamageOrigin DamageOrigin => _damageOrigin;
    public UnturnedAssetReference PrimaryAsset { get; }
    public UnturnedAssetReference SecondaryAsset { get; }
    public SqlItem<SavedStructure>? Save => _save;
    public IBuildable Buildable => _buildable ??= new BuildableStructure(Structure);
    object IBuildableDestroyedEvent.Region => Region;
    public ushort PendingDamage { get; set; }
    internal DamageStructureRequested(UCPlayer? instigator, ulong instigatorId, StructureDrop structure, StructureData structureData, StructureRegion region, byte x, byte y, SqlItem<SavedStructure>? save, EDamageOrigin damageOrigin, ushort pendingTotalDamage, UnturnedAssetReference primaryAsset, UnturnedAssetReference secondaryAsset)
    {
        _damageOrigin = damageOrigin;
        _instigator = instigator;
        _instigatorId = instigatorId;
        _drop = structure;
        _data = structureData;
        _region = region;
        _x = x;
        _y = y;
        PendingDamage = pendingTotalDamage;
        _save = save;
        PrimaryAsset = primaryAsset;
        SecondaryAsset = secondaryAsset;
        if (save?.Manager is not null)
        {
            save.Manager.WriteWait();
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
                save.Manager.WriteRelease();
            }
        }
    }
}