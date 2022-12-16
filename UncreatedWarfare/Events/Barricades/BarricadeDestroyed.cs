using SDG.Unturned;
using System.Text.Json.Serialization;
using Uncreated.SQL;
using Uncreated.Warfare.Structures;
using UnityEngine;

namespace Uncreated.Warfare.Events.Barricades;
public class BarricadeDestroyed : EventState, IBuildableDestroyedEvent
{
    private readonly UCPlayer? instigator;
    private readonly BarricadeDrop drop;
    private readonly BarricadeData data;
    private readonly BarricadeRegion region;
    private readonly byte x;
    private readonly byte y;
    private readonly ushort plant;
    private IBuildable? _buildable;
    private SqlItem<SavedStructure>? _save;
    private readonly bool _wasSaved;
    public UCPlayer? Instigator => instigator;
    public BarricadeDrop Barricade => drop;
    public BarricadeData ServersideData => data;
    public BarricadeRegion Region => region;
    public Transform Transform => drop.model;
    public byte RegionPosX => x;
    public byte RegionPosY => y;
    public ushort VehicleRegionIndex => plant;
    public bool IsOnVehicle => plant != ushort.MaxValue;
    public bool IsSaved => _wasSaved;
    public uint InstanceID => drop.instanceID;
    public IBuildable Buildable => _buildable ??= new UCBarricade(Barricade);
    public SqlItem<SavedStructure>? Save => _save;
    object IBuildableDestroyedEvent.Region => Region;
    internal BarricadeDestroyed(UCPlayer? instigator, BarricadeDrop barricade, BarricadeData barricadeData, BarricadeRegion region, byte x, byte y, ushort plant, SqlItem<SavedStructure>? save) : base()
    {
        this.instigator = instigator;
        this.drop = barricade;
        this.data = barricadeData;
        this.region = region;
        this.x = x;
        this.y = y;
        this.plant = plant;
        if (save is not null)
        {
            _save = save;
            save.Manager.WriteWait();
            try
            {
                if (save.Item != null)
                {
                    _buildable = save.Item.Buildable;
                    _wasSaved = true;
                }
            }
            finally
            {
                save.Manager.WriteRelease();
            }
        }
    }
}
