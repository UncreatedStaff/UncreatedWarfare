using Uncreated.Warfare.Events.Models.Barricades;
using Uncreated.Warfare.Events.Models.Structures;

namespace Uncreated.Warfare.Events.Models.Buildables;

/// <summary>
/// Invoked when a player is about to salvage a buildable (<see cref="SalvageBarricadeRequested"/> and <see cref="SalvageStructureRequested"/>).
/// </summary>
public interface ISalvageBuildableRequestedEvent : ICancellable, IBaseBuildableDestroyedEvent, IPlayerEvent;