using System;
using Uncreated.Warfare.Buildables;
using Uncreated.Warfare.Configuration;
using Uncreated.Warfare.Events.Logging;
using Uncreated.Warfare.Events.Models.Buildables;
using Uncreated.Warfare.Players;

namespace Uncreated.Warfare.Events.Models.Structures;

public class StructureTransformed : IBuildableTransformedEvent
{
    public required IBuildable Buildable { get; init; }

    public required StructureDrop Structure { get; init; }

    public required WarfarePlayer? Instigator { get; init; }

    /// <inheritdoc />
    public ActionLogEntry GetActionLogEntry(IServiceProvider serviceProvider, ref ActionLogEntry[]? multipleEntries)
    {
        StructureData serversideData = Structure.GetServersideData();
        return new ActionLogEntry(ActionLogTypes.BuildableTransformed,
            $"Structure {AssetLink.ToDisplayString(Structure.asset)} owned by {serversideData.owner} ({serversideData.group}) # {Structure.instanceID} " +
            $"to {serversideData.point:F2}, {serversideData.rotation:F2}",
            Instigator
        );
    }
}