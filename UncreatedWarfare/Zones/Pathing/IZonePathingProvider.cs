using System.Collections.Generic;

namespace Uncreated.Warfare.Zones.Pathing;

/// <summary>
/// Represents an object that decides a list of zones given all loaded zones.
/// </summary>
public interface IZonePathingProvider
{
    /// <returns>A list of zones, including 2 main bases as the first and last elements to indicate which order the zones are in.</returns>
    UniTask<IList<Zone>> CreateZonePathAsync(CancellationToken token = default);
}
