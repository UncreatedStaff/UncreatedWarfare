using Uncreated.Warfare.Buildables;
using Uncreated.Warfare.Events.Logging;
using Uncreated.Warfare.Players;

namespace Uncreated.Warfare.Events.Models.Buildables;
public interface IBuildableTransformedEvent : IActionLoggableEvent
{
    IBuildable Buildable { get; }
    WarfarePlayer? Instigator { get; }
}
