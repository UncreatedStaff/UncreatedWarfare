using SDG.Unturned;
using Uncreated.SQL;
using Uncreated.Warfare.Structures;
using UnityEngine;

namespace Uncreated.Warfare.Events.Structures;
public class SalvageBarricadeRequested : BreakablePlayerEvent, IBuildableDestroyedEvent
{
    private readonly BarricadeDrop _drop;
    private readonly BarricadeData _data;
    private readonly BarricadeRegion _region;
    private readonly byte _x;
    private readonly byte _y;
    private readonly bool _isSaved;
    private readonly ushort _plant;
    private readonly SqlItem<SavedStructure>? _save;
    private IBuildable? _buildable;
    private InteractableVehicle? _vehicle;
    public BarricadeDrop Barricade => _drop;
    public BarricadeData ServersideData => _data;
    public BarricadeRegion Region => _region;
    public Transform Transform => _drop.model;
    public byte RegionPosX => _x;
    public byte RegionPosY => _y;
    public ushort PlantId => _plant;
    public uint InstanceID => _drop.instanceID;
    public bool IsSaved => _isSaved;
    public SqlItem<SavedStructure>? Save => _save;
    public IBuildable Buildable => _buildable ??= new UCBarricade(Barricade);
    object IBuildableDestroyedEvent.Region => Region;
    UCPlayer? IBuildableDestroyedEvent.Instigator => Player;
    public InteractableVehicle? Vehicle => _vehicle ??= Region is VehicleBarricadeRegion r ? r.vehicle : null;
    internal SalvageBarricadeRequested(UCPlayer instigator, BarricadeDrop structure, BarricadeData structureData, BarricadeRegion region, byte x, byte y, ushort plant, SqlItem<SavedStructure>? save) : base(instigator)
    {
        this._drop = structure;
        this._data = structureData;
        this._region = region;
        this._x = x;
        this._y = y;
        this._plant = plant;
        _save = save;
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