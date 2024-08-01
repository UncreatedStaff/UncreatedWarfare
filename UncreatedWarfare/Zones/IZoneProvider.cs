using System.Collections.Generic;

namespace Uncreated.Warfare.Zones;
public interface IZoneProvider
{
    ValueTask<IEnumerable<Zone>> GetZones(CancellationToken token = default);
}