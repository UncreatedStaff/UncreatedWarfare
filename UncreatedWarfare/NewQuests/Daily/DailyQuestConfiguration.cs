using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using Uncreated.Warfare.Configuration;
using Uncreated.Warfare.Exceptions;
using Uncreated.Warfare.Util;

namespace Uncreated.Warfare.Quests.Daily;

public class DailyQuestConfiguration
{
    private readonly string _filePath;
    private readonly ILogger _logger;
    private readonly QuestService _questService;
    private readonly object _sync;

    public bool HasData { get; internal set; }

    public DailyQuestDay?[]? Days { get; set; }

    public DailyQuestConfiguration(string filePath, ILogger logger, QuestService questService)
    {
        _sync = new object();
        
        _filePath = filePath;
        _logger = logger;
        _questService = questService;
    }

    public void Write()
    {
        lock (_sync)
        {
            WriteIntl();
        }
    }

    public async UniTask Read(CancellationToken token = default)
    {
        try
        {
            await ReadIntl(token);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Exception reading \"{0}\".", _filePath);
            try
            {
                string backupPath = Path.Combine(Path.GetDirectoryName(_filePath)!, Path.GetFileNameWithoutExtension(_filePath) + "_backup.json");
                if (File.Exists(backupPath))
                    File.Delete(backupPath);

                File.Move(_filePath, backupPath);
            }
            catch { /* ignored */ }

            HasData = false;
            Days = new DailyQuestDay[DailyQuestService.DayLength];
        }
    }

    private async UniTask ReadIntl(CancellationToken token)
    {
        token.ThrowIfCancellationRequested();

        if (!File.Exists(_filePath))
        {
            HasData = false;
            Days = new DailyQuestDay[DailyQuestService.DayLength];
            return;
        }

        JsonDocument document;
        lock (_sync)
        {
            using FileStream fs = new FileStream(_filePath, FileMode.Open, FileAccess.Read, FileShare.Read, 4096, FileOptions.SequentialScan);
            document = JsonDocument.Parse(fs, ConfigurationSettings.JsonDocumentOptions);
        }

        DailyQuestDay[]? days = document.Deserialize<DailyQuestDay[]>(ConfigurationSettings.JsonSerializerSettingsQuests);
        if (days == null)
        {
            HasData = false;
            Array.Clear(Days, 0, Days.Length);
            return;
        }

        List<IQuestReward> rewards = new List<IQuestReward>();
        int index = 0;
        foreach (JsonElement dayElement in document.RootElement.EnumerateArray())
        {
            DailyQuestDay? day = days[index];
            ++index;
            if (day?.Presets == null)
                throw new QuestConfigurationException($"Missing day or preset in day index {index} in daily quest config.");

            day.StartTime = DateTime.SpecifyKind(day.StartTime, DateTimeKind.Utc);

            int presetIndex = 0;
            foreach (JsonElement presetElement in dayElement.GetProperty("Presets").EnumerateArray())
            {
                DailyQuestPreset? preset = day.Presets[presetIndex];
                if (preset == null)
                    throw new QuestConfigurationException($"Missing preset in day {index} preset {presetIndex} in daily quest config.");

                preset.Day = day;

                ++presetIndex;

                if (preset.TemplateName == null || _questService.Templates.FirstOrDefault(x => x.Name.Equals(preset.TemplateName, StringComparison.Ordinal)) is not { } template)
                    throw new QuestConfigurationException($"Missing or unknown quest preset name in day {index} preset {presetIndex} in daily quest config.");

                JsonElement stateElement = presetElement.GetProperty("State");
                await template.ReadStateToPreset(preset, new QuestJsonElementStateConfiguration(stateElement, template.Type), token);

                if (presetElement.TryGetProperty("RewardOverrides", out JsonElement element))
                {
                    foreach (JsonElement rewardElement in element.EnumerateArray())
                    {
                        string? typeStr = rewardElement.GetProperty("Type").GetString();
                        if (typeStr == null)
                            throw new QuestConfigurationException($"Missing type in quest preset reward in day {index} preset {presetIndex} reward {rewards.Count} in daily quest config.");

                        Type? type = ContextualTypeResolver.ResolveType(typeStr, typeof(IQuestReward));
                        if (type == null)
                            throw new QuestConfigurationException($"Unknown type {typeStr} in quest preset reward in day {index} preset {presetIndex} reward {rewards.Count} in daily quest config.");

                        IQuestReward reward = (IQuestReward)Activator.CreateInstance(type, rewardElement);
                        rewards.Add(reward);
                    }

                    if (rewards.Count == 0)
                        preset.RewardOverrides = Array.Empty<IQuestReward>();
                    else
                        preset.RewardOverrides = rewards.ToArray();

                    rewards.Clear();
                }
                else
                {
                    preset.RewardOverrides = null;
                }
            }
        }

        Days = days;

        HasData = true;
    }

    private void WriteIntl()
    {
        using FileStream fs = new FileStream(_filePath, FileMode.Create, FileAccess.Write, FileShare.Write);
        
        Utf8JsonWriter writer = new Utf8JsonWriter(fs, ConfigurationSettings.JsonWriterOptions);
        writer.WriteStartArray();
        foreach (DailyQuestDay? day in Days)
        {
            if (day == null)
                continue;

            writer.WriteStartObject();

            writer.WriteString("Asset", day.Asset);
            writer.WriteNumber("Id", day.Id);
            writer.WriteString("StartTime", day.StartTime);
            writer.WritePropertyName("Presets");
            writer.WriteStartArray();

            if (day.Presets != null)
            {
                foreach (DailyQuestPreset? preset in day.Presets)
                {
                    if (preset == null)
                        continue;

                    writer.WriteStartObject();

                    writer.WriteString("TemplateName", preset.TemplateName);
                    writer.WriteString("Key", preset.Key);
                    writer.WriteNumber("Flag", preset.Flag);

                    writer.WritePropertyName("State");
                    IQuestState state = preset.State;
                    JsonSerializer.Serialize(writer, state, state.GetType(), ConfigurationSettings.JsonSerializerSettingsQuests);

                    if (preset.RewardOverrides == null)
                        continue;

                    writer.WritePropertyName("RewardOverrides");
                    writer.WriteStartArray();

                    foreach (IQuestReward reward in preset.RewardOverrides)
                    {
                        writer.WriteStartObject();
                        writer.WriteString("Type", reward.GetType().AssemblyQualifiedName);
                        reward.WriteToJson(writer);
                        writer.WriteEndObject();
                    }

                    writer.WriteEndArray();

                    writer.WriteEndObject();
                }
            }

            writer.WriteEndArray();

            writer.WriteEndObject();
        }
        writer.WriteEndArray();

        writer.Flush();
        writer.Dispose();
    }
}

public class DailyQuestDay
{
    public DateTime StartTime { get; set; }
    public Guid Asset { get; set; }
    public ushort Id { get; set; }
    public DailyQuestPreset?[]? Presets { get; set; }
}