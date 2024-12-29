using System;
using System.Collections.Generic;
using System.Text;
using Uncreated.Warfare.Buildables;
using Uncreated.Warfare.Configuration;
using Uncreated.Warfare.Vehicles.WarfareVehicles;

namespace Uncreated.Warfare.FOBs.Entities;
public class EmplacementEntity : IFobEntity
{
    public IBuildable? AuxilliaryBuildable { get; }
    public WarfareVehicle Vehicle { get; }
    public Vector3 Position => Vehicle.Position;
    public Quaternion Rotation => Vehicle.Rotation;

    public IAssetLink<Asset> IdentifyingAsset { get; }

    public EmplacementEntity(WarfareVehicle emplacementVehicle, IAssetLink<ItemPlaceableAsset> foundationAsset, IBuildable? foundation = null)
    {
        Vehicle = emplacementVehicle;
        AuxilliaryBuildable = foundation;
        IdentifyingAsset = foundationAsset;
    }

    public override bool Equals(object? obj)
    {
        return obj is EmplacementEntity entity && Vehicle.Equals(entity.Vehicle);
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(IdentifyingAsset.Guid, Vehicle);
    }

    public void Dispose()
    {
        // don't need to dispose anything
    }
}
