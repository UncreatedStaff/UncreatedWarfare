using System;
using Uncreated.Warfare.Buildables;
using Uncreated.Warfare.Configuration;
using Uncreated.Warfare.Fobs;
using Uncreated.Warfare.Layouts.Teams;
using Uncreated.Warfare.Util;
using Uncreated.Warfare.Vehicles.WarfareVehicles;

namespace Uncreated.Warfare.FOBs.Entities;

public class EmplacementEntity : IFobEntity
{
    public IBuildable? AuxilliaryBuildable { get; }
    public WarfareVehicle Vehicle { get; }
    public Vector3 Position { get => Vehicle.Position; set => SetPosition(value); }
    public Quaternion Rotation { get => Vehicle.Rotation; set => SetRotation(value); }
    public IAssetLink<Asset> IdentifyingAsset { get; }

    public Team Team { get; }

    public EmplacementEntity(WarfareVehicle emplacementVehicle, Team team, IAssetLink<ItemPlaceableAsset> foundationAsset, IBuildable? foundation = null)
    {
        Vehicle = emplacementVehicle;
        AuxilliaryBuildable = foundation;
        IdentifyingAsset = foundationAsset;
        Team = team;
    }

    public override bool Equals(object? obj)
    {
        return obj is EmplacementEntity entity && Vehicle.Equals(entity.Vehicle);
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(IdentifyingAsset, Vehicle);
    }

    private void SetPosition(Vector3 position)
    {
        if (AuxilliaryBuildable == null)
        {
            Vehicle.Position = position;
            return;
        }

        Vector3 offset = Vehicle.Position - AuxilliaryBuildable.Position;
        Vehicle.Position = position;
        AuxilliaryBuildable.Position = position - offset;
    }

    private void SetRotation(Quaternion rotation)
    {
        Vehicle.Rotation = rotation;
        if (AuxilliaryBuildable != null)
            AuxilliaryBuildable.Rotation = rotation * BarricadeUtility.DefaultBarricadeRotation;
    }

    public void SetPositionAndRotation(Vector3 position, Quaternion rotation)
    {
        Vehicle.SetPositionAndRotation(position, rotation);
        AuxilliaryBuildable?.SetPositionAndRotation(position, rotation);
    }

    public void UpdateConfiguration(FobConfiguration configuration) { }

    bool ITransformObject.Alive => Vehicle.Alive;
}