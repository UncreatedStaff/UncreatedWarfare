using System;
using System.Collections.Generic;
using System.Text;

namespace Uncreated.Warfare.Vehicles.WarfareVehicles.Transport;
public class TranportTracker
{
    public CSteamID? LastKnownDriver { get; private set; }
    private readonly Dictionary<ulong, Vector3> _playerEntryPositions;

    public TranportTracker()
    {
        _playerEntryPositions = new Dictionary<ulong, Vector3>();
    }
    public void RecordPlayerEntry(ulong steam64, Vector3 entryPoint, int seatIndex)
    {
        _playerEntryPositions[steam64] = entryPoint;
        if (seatIndex == 0)
            LastKnownDriver = new CSteamID(steam64);
    }
    /// <returns>The total distance travelled by the exiting player from their original recorded entry point.</returns>
    public float RecordPlayerExit(ulong steam64, Vector3 exitPoint)
    {
        if (_playerEntryPositions.TryGetValue(steam64, out Vector3 originalEntryPoint))
        {
            _playerEntryPositions.Remove(steam64);
            return Vector3.Distance(originalEntryPoint, exitPoint);
        }

        return 0;
    }
}
