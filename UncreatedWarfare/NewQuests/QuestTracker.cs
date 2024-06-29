using Cysharp.Threading.Tasks;
using SDG.Unturned;
using System;
using System.Collections.Generic;
using System.Threading;
using Uncreated.Warfare.Quests;

namespace Uncreated.Warfare.NewQuests;
public abstract class QuestTracker
{
    protected readonly IServiceProvider ServiceProvider;
    public UCPlayer Player { get; }

    /// <summary>
    /// If the quest has been completed.
    /// </summary>
    public abstract bool IsComplete { get; }

    /// <summary>
    /// The value sent to the quest translation on the client. This value replaces {0}.
    /// </summary>
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

    protected void InvokeUpdate()
    {
        if (Thread.CurrentThread.IsGameThread())
        {
            HandleUpdated();

            if (IsComplete)
                HandleComplete();
        }
        else
        {
            UniTask.Create(async () =>
            {
                await UniTask.SwitchToMainThread();
                HandleUpdated();

                if (IsComplete)
                    HandleComplete();
            });
        }
    }

    protected virtual void HandleUpdated()
    {
        // todo
    }
    protected virtual void HandleComplete()
    {
        // todo
    }
}
