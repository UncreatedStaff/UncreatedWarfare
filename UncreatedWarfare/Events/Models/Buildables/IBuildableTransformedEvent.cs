using Uncreated.Warfare.Buildables;
using Uncreated.Warfare.Players;

namespace Uncreated.Warfare.Events.Models.Buildables;
public interface IBuildableTransformedEvent
{
    IBuildable Buildable { get; }
    WarfarePlayer? Instigator { get; }
}
