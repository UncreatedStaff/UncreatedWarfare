using Uncreated.Warfare.Buildables;
using Uncreated.Warfare.Events.Models.Buildables;
using Uncreated.Warfare.Players;

namespace Uncreated.Warfare.Events.Models.Structures;

public class StructureTransformed : IBuildableTransformedEvent
{
    public required IBuildable Buildable { get; init; }

    public required StructureDrop Structure { get; init; }

    public required WarfarePlayer? Instigator { get; init; }
}