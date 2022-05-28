using SDG.Unturned;
using UnityEngine;

namespace Uncreated.Warfare.Events.Barricades;
public class BarricadeDestroyed : EventState
{
    private readonly UCPlayer? instigator;
    private readonly BarricadeDrop drop;
    private readonly BarricadeData data;
    private readonly BarricadeRegion region;
    private readonly byte x;
    private readonly byte y;
    private readonly ushort plant;
    public UCPlayer? Instigator => instigator;
    public BarricadeDrop Barricade => drop;
    public BarricadeData ServersideData => data;
    public BarricadeRegion Region => region;
    public Transform Transform => drop.model;
    public byte RegionPosX => x;
    public byte RegionPosY => y;
    public ushort VehicleRegionIndex => plant;
    public bool IsOnVehicle => plant != ushort.MaxValue;
    public uint InstanceID => drop.instanceID;
    public BarricadeDestroyed(UCPlayer? instigator, BarricadeDrop barricade, BarricadeData barricadeData, BarricadeRegion region, byte x, byte y, ushort plant) : base()
    {
        this.instigator = instigator;
        this.drop = barricade;
        this.data = barricadeData;
        this.region = region;
        this.x = x;
        this.y = y;
        this.plant = plant;
    }
}
