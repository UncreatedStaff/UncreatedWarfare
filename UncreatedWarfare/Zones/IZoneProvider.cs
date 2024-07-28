using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Uncreated.Warfare.Zones;
public interface IZoneProvider
{
    ValueTask<IEnumerable<Zone>> GetZones(CancellationToken token = default);
}