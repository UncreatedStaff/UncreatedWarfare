using System;
using Uncreated.Warfare.Quests;

namespace Uncreated.Warfare.Events.Models.Players;

public class QuestCompleted : CancellablePlayerEvent
{
    public required QuestTracker Tracker { get; init; }
    public Guid PresetKey => Tracker.Preset?.Key ?? Guid.Empty;
    public bool GiveRewards { get; set; } = true;
}
