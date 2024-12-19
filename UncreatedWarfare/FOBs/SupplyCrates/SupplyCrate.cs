using SDG.Unturned;
using System;
using System.Collections.Generic;
using System.Text;
using Uncreated.Warfare.Buildables;
using Uncreated.Warfare.Configuration;
using Uncreated.Warfare.FOBs.Entities;
using Uncreated.Warfare.Util;

namespace Uncreated.Warfare.FOBs.SupplyCrates;
public class SupplyCrate : IBuildableFobEntity
{
    public SupplyType Type { get; }
    public int SupplyCount { get; set; }
    public int MaxSupplyCount { get; }
    public int SupplyRadius { get; set; }

    public IBuildable Buildable {  get; }

    public Vector3 Position => Buildable.Position;

    public Quaternion Rotation => Buildable.Rotation;

    public IAssetLink<Asset> IdentifyingAsset { get; }

    public SupplyCrate(SupplyCrateInfo info, IBuildable buildable)
    {
        Buildable = buildable;
        Type = info.Type;
        SupplyCount = info.StartingSupplies;
        MaxSupplyCount = info.StartingSupplies;
        SupplyRadius = info.SupplyRadius;
        IdentifyingAsset = info.SupplyItemAsset;
    }
    public bool IsWithinRadius(Vector3 point) => MathUtility.WithinRange(Buildable.Position, point, SupplyRadius);
}
