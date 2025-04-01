using System;

namespace Uncreated.Warfare.Events.Logging;

public interface IActionLoggableEvent
{
    ActionLogEntry GetActionLogEntry(IServiceProvider serviceProvider, ref ActionLogEntry[]? multipleEntries);
}