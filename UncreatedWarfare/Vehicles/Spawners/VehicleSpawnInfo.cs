using System.Collections.Generic;
using Uncreated.Warfare.Configuration;

namespace Uncreated.Warfare.Vehicles.Spawners;

public class VehicleSpawnInfo
{
    public required string UniqueName { get; set; }
    public required uint BuildableInstanceId { get; set; }
    public required IAssetLink<VehicleAsset> VehicleAsset { get; set; }
    public required List<uint> SignInstanceIds { get; set; }
    public bool IsStructure { get; set; } = false;
}