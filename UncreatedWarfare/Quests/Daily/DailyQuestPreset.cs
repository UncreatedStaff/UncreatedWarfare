using System;
using System.Text.Json.Serialization;

namespace Uncreated.Warfare.Quests.Daily;

public class DailyQuestPreset : IAssetQuestPreset
{
    /// <summary>
    /// Name of the quest template mapping to <see cref="QuestTemplate.Name"/>.
    /// </summary>
    public string? TemplateName { get; set; }

    public Guid Key { get; set; }

    public ushort Flag { get; set; }

    [JsonIgnore]
    public IQuestState State { get; private set; }

    [JsonIgnore]
    public IQuestReward[]? RewardOverrides { get; set; }

    [JsonIgnore]
    public DailyQuestDay Day { get; set; }

    public void UpdateState(IQuestState state)
    {
        State = state;
    }

    public Guid Asset => Day.Asset;
}