using SDG.Unturned;
using System.Text.Json.Serialization;
using Uncreated.SQL;
using Uncreated.Warfare.Structures;
using UnityEngine;

namespace Uncreated.Warfare.Events.Barricades;
public class SignTextChanged : EventState
{
    private IBuildable? _buildable;
    public Transform Transform => Barricade.model;
    public bool IsOnVehicle => VehicleRegionIndex != ushort.MaxValue;
    public uint InstanceID => Barricade.instanceID;
    public IBuildable Buildable => _buildable ??= new UCBarricade(Barricade);
    public string Text => Sign.text;
    public SqlItem<SavedStructure>? Save { get; }
    public bool IsSaved { get; }
    public byte RegionPosX { get; }
    public byte RegionPosY { get; }
    public ushort VehicleRegionIndex { get; }
    public UCPlayer? Instigator { get; }
    public BarricadeDrop Barricade { get; }
    public BarricadeData ServersideData { get; }
    public BarricadeRegion Region { get; }
    public InteractableSign Sign { get; }
    internal SignTextChanged(UCPlayer? instigator, BarricadeDrop barricade, BarricadeRegion region, byte x, byte y, ushort plant, SqlItem<SavedStructure>? save) : base()
    {
        this.Instigator = instigator;
        this.Barricade = barricade;
        Sign = (InteractableSign)barricade.interactable;
        this.ServersideData = barricade.GetServersideData();
        this.Region = region;
        this.RegionPosX = x;
        this.RegionPosY = y;
        this.VehicleRegionIndex = plant;
        Save = save;
        ListSqlConfig<SavedStructure>? m = save?.Manager;
        if (m is not null)
        {
            m.WriteWait();
            try
            {
                if (save.Item != null)
                {
                    _buildable = save.Item.Buildable;
                    IsSaved = true;
                }
            }
            finally
            {
                m.WriteRelease();
            }
        }
    }
}
