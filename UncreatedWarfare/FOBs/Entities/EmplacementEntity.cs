using System;
using System.Collections.Generic;
using System.Text;
using Uncreated.Warfare.Buildables;
using Uncreated.Warfare.Configuration;
using Uncreated.Warfare.Vehicles.Vehicle;

namespace Uncreated.Warfare.FOBs.Entities;
public class EmplacementEntity : IFobEntity
{
    public IBuildable? AuxilliaryBuildable { get; }
    public WarfareVehicle Vehicle { get; }
    public Vector3 Position => Vehicle.Position;
    public Quaternion Rotation => Vehicle.Rotation;

    public IAssetLink<Asset> IdentifyingAsset { get; }

    public EmplacementEntity(WarfareVehicle emplacementVehicle, IBuildable? foundation = null)
    {
        Vehicle = emplacementVehicle;
        AuxilliaryBuildable = foundation;
        IdentifyingAsset = AssetLink.Create(Vehicle.Vehicle.asset);
    }
}
