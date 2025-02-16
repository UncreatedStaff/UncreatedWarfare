using System;
using System.Collections.Generic;

namespace Uncreated.Warfare.Vehicles.WarfareVehicles.Transport;
public class TranportTracker
{
    public CSteamID? LastKnownDriver { get; private set; }
    public DateTime? LastKnownDriverExitTime { get; private set; }
    private readonly Dictionary<ulong, Vector3> _playerEntryPositions;

    public TranportTracker()
    {
        _playerEntryPositions = new Dictionary<ulong, Vector3>();
    }
    public void RecordPlayerEntry(ulong steam64, Vector3 entryPoint, int seatIndex)
    {
        _playerEntryPositions[steam64] = entryPoint;
        if (seatIndex == 0)
        {
            LastKnownDriver = new CSteamID(steam64);
            LastKnownDriverExitTime = DateTime.UtcNow;
        }
    }
    /// <returns>The total distance travelled by the exiting player from their original recorded entry point.</returns>
    public float RecordPlayerExit(ulong steam64, Vector3 exitPoint)
    {
        if (LastKnownDriver.HasValue && LastKnownDriver.Value.m_SteamID == steam64)
        {
            LastKnownDriver = null;
            LastKnownDriverExitTime = DateTime.UtcNow;
        }

        if (_playerEntryPositions.TryGetValue(steam64, out Vector3 originalEntryPoint))
        {
            _playerEntryPositions.Remove(steam64);
            return Vector3.Distance(originalEntryPoint, exitPoint);
        }

        return 0;
    }
}
