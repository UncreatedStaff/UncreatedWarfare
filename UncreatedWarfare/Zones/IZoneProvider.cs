using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Uncreated.Warfare.Proximity;

namespace Uncreated.Warfare.Zones;
public interface IZoneProvider
{
    ValueTask<IEnumerable<IProximity>> GetZones(CancellationToken token = default);
}