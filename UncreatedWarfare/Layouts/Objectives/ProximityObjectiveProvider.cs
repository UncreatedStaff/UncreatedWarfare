using Cysharp.Threading.Tasks;
using System;
using System.Collections.Generic;
using System.Threading;

namespace Uncreated.Warfare.Layouts.Objectives;
public class ProximityObjectiveProvider : IObjectiveProvider<ProximityObjective>
{
    private readonly ZoneStore _zoneStore;

    private ProximityObjective[] _objectives = Array.Empty<ProximityObjective>();

    /// <inheritdoc />
    public event PlayerObjectiveHandler? OnPlayerEnteredObjective;

    /// <inheritdoc />
    public event PlayerObjectiveHandler? OnPlayerExitedObjective;

    /// <inheritdoc />
    public IReadOnlyList<ProximityObjective> Objectives { get; private set; }

    public string[] Pathing { get; set; }

    public ProximityObjectiveProvider(ZoneStore zoneStore)
    {
        _zoneStore = zoneStore;
        Objectives = _objectives;
    }

    public UniTask InitializeAsync(CancellationToken token)
    {
        if (Pathing == null || Pathing.Length == 0)
        {
            // todo path using adjacencies.
        }

        _objectives = new ProximityObjective[0]; // todo path
    }
}
