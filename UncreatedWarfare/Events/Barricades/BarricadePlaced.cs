using SDG.Unturned;
using UnityEngine;

namespace Uncreated.Warfare.Events.Barricades;
public class BarricadePlaced : EventState
{
    private readonly UCPlayer? owner;
    private readonly BarricadeDrop drop;
    private readonly BarricadeData data;
    private readonly BarricadeRegion region;
    public UCPlayer? Owner => owner;
    public BarricadeDrop Barricade => drop;
    public BarricadeData ServersideData => data;
    public BarricadeRegion Region => region;
    public ulong GroupID => data.group;
    public Transform Transform => drop.model;
    public BarricadePlaced(UCPlayer? owner, BarricadeDrop drop, BarricadeData data, BarricadeRegion region)
    {
        this.owner = owner;
        this.drop = drop;
        this.data = data;
        this.region = region;
    }
}
