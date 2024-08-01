using Cysharp.Threading.Tasks;
using System;
using System.Collections.Generic;
using System.Text.Json;
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
    protected virtual void WriteProgress(Utf8JsonWriter writer) { }
    protected virtual void ReadProgress(ref Utf8JsonReader reader) { }
    protected void InvokeUpdate()
    {
        if (Thread.CurrentThread.IsGameThread())
        {
            HandleUpdated(false);

            if (IsComplete)
                HandleComplete();
        }
        else
        {
            UniTask.Create(async () =>
            {
                await UniTask.SwitchToMainThread();
                HandleUpdated(false);

                if (IsComplete)
                    HandleComplete();
            });
        }
    }

    protected virtual void HandleUpdated(bool skipFlagUpdate)
    {
        // todo
    }
    protected virtual void HandleComplete()
    {
        // todo
    }
}
