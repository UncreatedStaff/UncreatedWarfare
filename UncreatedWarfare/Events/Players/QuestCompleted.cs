using System;
using Uncreated.Warfare.Quests;

namespace Uncreated.Warfare.Events.Players;

public class QuestCompleted : CancellablePlayerEvent
{
    private readonly BaseQuestTracker _tracker;
    public BaseQuestTracker Tracker => _tracker;
    public Guid PresetKey => _tracker.PresetKey;
    public bool GiveRewards { get; set; } = true;

    public QuestCompleted(BaseQuestTracker tracker) : base(tracker.Player ?? throw new ArgumentException("Tracker must belong to a player."), true)
    {
        _tracker = tracker;
    }
}
