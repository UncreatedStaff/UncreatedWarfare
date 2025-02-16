using Uncreated.Warfare.Buildables;
using Uncreated.Warfare.Events.Models.Buildables;
using Uncreated.Warfare.Players;

namespace Uncreated.Warfare.Events.Models.Barricades;

public class BarricadeTransformed : IBuildableTransformedEvent
{
    public required IBuildable Buildable { get; init; }

    public required BarricadeDrop Barricade { get; init; }

    public required WarfarePlayer? Instigator { get; init; }
}