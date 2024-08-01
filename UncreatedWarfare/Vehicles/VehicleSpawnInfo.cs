using System.Collections.Generic;
using Uncreated.Warfare.Buildables;
using Uncreated.Warfare.Configuration;

namespace Uncreated.Warfare.Vehicles;
public class VehicleSpawnInfo
{
    /// <summary>
    /// The asset used to spawn vehicles.
    /// </summary>
    public IAssetLink<VehicleAsset> Vehicle { get; set; }

    /// <summary>
    /// Barricade or structure where vehicles are spawned above.
    /// </summary>
    public IBuildable Spawner { get; set; }

    /// <summary>
    /// List of sign barricades linked to this spawn.
    /// </summary>
    public IList<IBuildable> SignInstanceIds { get; } = new List<IBuildable>(1);
}
