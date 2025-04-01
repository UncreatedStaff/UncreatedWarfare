using System;
using Uncreated.Warfare.Buildables;
using Uncreated.Warfare.Configuration;
using Uncreated.Warfare.Events.Logging;
using Uncreated.Warfare.Events.Models.Buildables;
using Uncreated.Warfare.Players;

namespace Uncreated.Warfare.Events.Models.Barricades;

public class BarricadeTransformed : IBuildableTransformedEvent
{
    public required IBuildable Buildable { get; init; }

    public required BarricadeDrop Barricade { get; init; }

    public required WarfarePlayer? Instigator { get; init; }

    /// <inheritdoc />
    public ActionLogEntry GetActionLogEntry(IServiceProvider serviceProvider, ref ActionLogEntry[]? multipleEntries)
    {
        BarricadeData serversideData = Barricade.GetServersideData();
        return new ActionLogEntry(ActionLogTypes.BuildableTransformed,
            $"Barricade {AssetLink.ToDisplayString(Barricade.asset)} owned by {serversideData.owner} ({serversideData.group}) # {Barricade.instanceID} " +
            $"to {serversideData.point:F2}, {serversideData.rotation:F2}",
            Instigator
        );
    }
}