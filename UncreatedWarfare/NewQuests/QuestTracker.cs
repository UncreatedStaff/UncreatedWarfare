using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Text.Json;
using Uncreated.Framework.UI;
using Uncreated.Warfare.Players;
using Uncreated.Warfare.Quests.Daily;
using Uncreated.Warfare.Util;

namespace Uncreated.Warfare.Quests;
public abstract class QuestTracker
{
    protected readonly IServiceProvider ServiceProvider;
    protected readonly QuestService QuestService;
    public WarfarePlayer Player { get; }

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
    public bool IsDailyQuestTracker { get; }

    public event Action<QuestTracker>? Updated;
    public event Action<QuestTracker>? Completed;

    protected QuestTracker(WarfarePlayer player, IServiceProvider serviceProvider, QuestTemplate quest, IQuestState state, IQuestPreset? preset)
    {
        Player = player;
        ServiceProvider = serviceProvider;
        QuestService = serviceProvider.GetRequiredService<QuestService>();
        State = state;
        Preset = preset;
        Quest = quest;

        IQuestReward[] rewards;
        IQuestReward[]? rewardOverrides = preset?.RewardOverrides;
        if (rewardOverrides == null)
        {
            rewards = new IQuestReward[quest.Rewards.Count];
            int index = 0;
            foreach (QuestRewardExpression expression in quest.Rewards)
            {
                rewards[index] = expression.GetReward(state) ?? new NullReward();
                ++index;
            }
        }
        else
        {
            rewards = new IQuestReward[rewardOverrides.Length];
            Array.Copy(rewardOverrides, rewards, rewards.Length);
        }

        Rewards = rewards;
        IsDailyQuestTracker = Preset is DailyQuestPreset;
    }

    public virtual string CreateDescriptiveStringForPlayer()
    {
        string format = State.CreateQuestDescriptiveString();

        return UnturnedUIUtility.QuickFormat(format, FlagValue, 0);
    }

    public virtual void WriteProgress(Utf8JsonWriter writer) { }
    public virtual void ReadProgress(ref Utf8JsonReader reader) { }
    public void InvokeUpdate()
    {
        if (GameThread.IsCurrent)
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
        QuestService.HandleTrackerUpdated(this);
        Updated?.Invoke(this);
    }

    protected virtual void HandleComplete()
    {
        QuestService.HandleTrackerCompleted(this);
        Completed?.Invoke(this);
    }
}
