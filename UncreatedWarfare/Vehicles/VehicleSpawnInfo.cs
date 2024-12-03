using System.Collections.Generic;
using System.Linq;
using Uncreated.Warfare.Buildables;
using Uncreated.Warfare.Components;
using Uncreated.Warfare.Configuration;
using Uncreated.Warfare.Interaction.Requests;
using Uncreated.Warfare.Util;

namespace Uncreated.Warfare.Vehicles;
public class VehicleSpawnInfo : IRequestable<VehicleSpawnInfo>
{
    /// <summary>
    /// A unique name to identify this spawner.
    /// </summary>
    public required string UniqueName { get; set; }
    /// <summary>
    /// The asset used to spawn vehicles.
    /// </summary>
    public required IAssetLink<VehicleAsset> Vehicle { get; set; }

    /// <summary>
    /// Barricade or structure where vehicles are spawned above.
    /// </summary>
    public required IBuildable Spawner { get; set; }

    /// <summary>
    /// List of sign barricades linked to this spawn.
    /// </summary>
    public IList<IBuildable> Signs { get; } = new List<IBuildable>(1);

    public override string ToString()
    {
        return $"VehicleSpawnInfo [" +
               $"UniqueName: {UniqueName}  " +
               $"Vehicle: {Vehicle} " +
               $"Spawner: {Spawner.InstanceId} ({(Spawner.IsStructure ? "STRUCTURE" : "BARRICADE")}) " +
               $"Signs = [{string.Join(", ", Signs.Select(s => s.InstanceId))}]" +
               $"]";
    }

}