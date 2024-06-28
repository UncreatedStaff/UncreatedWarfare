using System;
using System.Collections.Generic;
using Uncreated.Warfare.Quests;

namespace Uncreated.Warfare.NewQuests;
public abstract class QuestTracker
{
    protected readonly IServiceProvider ServiceProvider;
    public UCPlayer Player { get; }
    public abstract bool IsComplete { get; }
    public abstract short FlagValue { get; }
    public IReadOnlyList<IQuestReward> Rewards { get; }
    public QuestTemplate Quest { get; }
    public IQuestState State { get; }
    public IQuestPreset? Preset { get; }

    public QuestTracker(UCPlayer player, IServiceProvider serviceProvider, QuestTemplate quest, IQuestState state, IQuestPreset? preset)
    {
        Player = player;
        ServiceProvider = serviceProvider;
        State = state;
        Preset = preset;
        Quest = quest;
        // todo rewards
    }
}
