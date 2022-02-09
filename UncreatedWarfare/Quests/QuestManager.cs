using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Uncreated.Warfare.Quests.Types;

namespace Uncreated.Warfare.Quests;

public static class QuestManager
{
    /// <summary>Complete list of all registered quest data.</summary>
    public static List<BaseQuestData> Quests = new List<BaseQuestData>();
    /// <summary>Complete list of all registered quest trackers (1 per player).</summary>
    public static List<BaseQuestTracker> RegisteredTrackers = new List<BaseQuestTracker>(128);
    public const string QUEST_LOCATION = Data.DATA_DIRECTORY + "Quests\\quest_data.json";
    public const string PROGRESSION_LOCATION = Data.DATA_DIRECTORY + "Quests\\progression.json";
    public static void Init()
    {
        InitTypesReflector();
        ReadQuestDatas();
    }
    /// <summary>Generate and register a random tracker with the provided data to the player.</summary>
    public static BaseQuestTracker CreateTracker(BaseQuestData data, UCPlayer player)
    {
        BaseQuestTracker tracker = data.CreateTracker(player);
        RegisteredTrackers.Add(tracker);
        return tracker;
    }
    /// <summary>Find, generate, and register a tracker using a <paramref name="key"/> and a set <see cref="IQuestPreset"/>.</summary>
    /// <returns>A tracker using a <see cref="IQuestPreset"/> that is matched by <see cref="IQuestPreset.Key"/> and <see cref="IQuestPreset.Team"/> (or 0).</returns>
    public static BaseQuestTracker CreateTracker(UCPlayer player, Guid key)
    {
        ulong team = player.GetTeam();
        // look for one that matches their team first.
        for (int i = 0; i < Quests.Count; i++)
        {
            foreach (IQuestPreset preset in Quests[i].Presets)
            {
                if (preset.Key == key && preset.Team == team)
                {
                    IQuestState state = preset.State;
                    BaseQuestTracker tr = Quests[i].GetTracker(player, ref state);
                    RegisterTracker(tr);
                    return tr;
                }
            }
        }
        for (int i = 0; i < Quests.Count; i++)
        {
            foreach (IQuestPreset preset in Quests[i].Presets)
            {
                if (preset.Key == key && preset.Team == 0)
                {
                    IQuestState state = preset.State;
                    BaseQuestTracker tr = Quests[i].GetTracker(player, ref state);
                    RegisterTracker(tr);
                    return tr;
                }
            }
        }
        return null;
    }
    /// <summary>Deregister a tracker and call <see cref="BaseQuestTracker.Dispose"/> on it.</summary>
    public static void DeregisterTracker(BaseQuestTracker tracker)
    {
        tracker.Dispose();
        RegisteredTrackers.Remove(tracker);
    }
    /// <summary>Register a tracker.</summary>
    public static void RegisterTracker(BaseQuestTracker tracker)
    {
        OnQuestStarted(tracker);
        RegisteredTrackers.Add(tracker);
    }
    /// <summary>Run on disconnect.</summary>
    public static void DeregisterOwnedTrackers(UCPlayer player)
    {
        for (int i = RegisteredTrackers.Count - 1; i >= 0; i--)
        {
            if (RegisteredTrackers[i].Player.Steam64 == player.Steam64)
            {
                RegisteredTrackers[i].Dispose();
                RegisteredTrackers.RemoveAt(i);
            }
        }
    }
    /// <summary>Ticks any <see cref="BaseQuestTracker"/> that's data has overridden <see cref="BaseQuestData.TickFrequencySeconds"/> and has set it > 0.</summary>
    public static void OnGameTick()
    {
        for (int i = 0; i < RegisteredTrackers.Count; i++)
        {
            BaseQuestTracker tracker = RegisteredTrackers[i];
            if (tracker.QuestData.TickFrequencySeconds > 0 && Data.Gamemode.EveryXSeconds(tracker.QuestData.TickFrequencySeconds))
                tracker.Tick();
        }
    }
    public static void OnQuestStarted(BaseQuestTracker tracker)
    {
        if (!tracker.IsDailyQuest && tracker.Flag != 0)
        {
            tracker.Player.Player.quests.sendSetFlag(tracker.Flag, tracker.FlagValue);
            L.LogDebug("Flag quest started: " + tracker.QuestData.QuestType);
        }
    }
    public static void OnQuestCompleted(BaseQuestTracker tracker)
    {
        if (tracker.IsDailyQuest)
            DailyQuests.OnDailyQuestCompleted(tracker);
        else
        {
            // LevelManager.OnQuestCompleted(tracker.key);  (we need something like this)
            
            // TODO: Update a UI and check for giving levels, etc.
        }
    }
    public static void OnQuestUpdated(BaseQuestTracker tracker)
    {
        if (tracker.IsDailyQuest)
            DailyQuests.OnDailyQuestUpdated(tracker);
        else
        {
            if (tracker.Flag != 0)
            {
                tracker.Player.Player.quests.sendSetFlag(tracker.Flag, tracker.FlagValue);
                L.LogDebug("Flag quest updated: " + tracker.FlagValue);
            }
        }
    }

    #region read/write
    public static readonly Dictionary<EQuestType, Type> QuestTypes = new Dictionary<EQuestType, Type>();
    /// <summary>Registers all the <see cref="QuestDataAttribute"/>'s to <see cref="QuestTypes"/>.</summary>
    public static void InitTypesReflector()
    {
        foreach (Type type in Assembly.GetExecutingAssembly().GetTypes().Where(x => x.IsClass && x.IsSubclassOf(typeof(BaseQuestData)) && !x.IsAbstract))
        {
            QuestDataAttribute attribute = type.GetCustomAttributes().OfType<QuestDataAttribute>().FirstOrDefault();
            if (attribute != null && attribute.Type != EQuestType.INVALID && !QuestTypes.ContainsKey(attribute.Type))
            {
                QuestTypes.Add(attribute.Type, type);
            }
        }
    }
    /// <summary>Creates an instance of the provided <paramref name="type"/>. Pulls from <see cref="QuestTypes"/>. <see cref="InitTypesReflector"/> should be ran before use.</summary>
    public static BaseQuestData GetQuestData(EQuestType type)
    {
        if (QuestTypes.TryGetValue(type, out Type result))
        {
            try
            {
                object t = Activator.CreateInstance(result);
                if (t is BaseQuestData data)
                {
                    data.QuestType = type;
                    return data;
                }
            }
            catch (Exception ex)
            {
                L.LogError("Failed to create a quest object of type " + type);
                L.LogError(ex);
            }
        }
        L.LogError("Failed to create a quest object of type " + type);
        return null;
    }

    /// <summary>Read function to parse a quest data with quest type <paramref name="type"/>.</summary>
    public static BaseQuestData ReadQuestData(ref Utf8JsonReader reader)
    {
        BaseQuestData quest = null;
        while (reader.Read())
        {
            if (reader.TokenType == JsonTokenType.EndObject)
            {
                return quest;
            }
            else if (reader.TokenType == JsonTokenType.PropertyName)
            {
                string propertyName = reader.GetString();
                if (propertyName.Equals("quest_type", StringComparison.OrdinalIgnoreCase) && quest == null)
                {
                    if (!reader.Read()) return quest;
                    string typeValue = reader.GetString();
                    if (Enum.TryParse(typeValue, true, out EQuestType type))
                    {
                        quest = GetQuestData(type);
                    }
                    else
                    {
                        L.LogWarning("Unknown quest type \"" + typeValue + "\"");
                    }
                }
                else if (quest != null)
                {
                    if (propertyName.Equals("translations", StringComparison.OrdinalIgnoreCase))
                    {
                        quest.Translations = new Dictionary<string, string>();
                        if (reader.Read() && reader.TokenType == JsonTokenType.StartObject)
                        {
                            while (reader.Read() && reader.TokenType == JsonTokenType.PropertyName)
                            {
                                string key = reader.GetString();
                                if (reader.Read() && reader.TokenType == JsonTokenType.String)
                                {
                                    string value = reader.GetString();
                                    if (!quest.Translations.ContainsKey(key))
                                        quest.Translations.Add(key, value);
                                }
                            }
                        }
                    }
                    else if (propertyName.Equals("team", StringComparison.OrdinalIgnoreCase))
                    {
                        if (reader.Read() && reader.TokenType == JsonTokenType.Number)
                            quest.TeamFilter = reader.GetUInt64();
                    }
                    else if (propertyName.Equals("remove_from_daily_quests", StringComparison.OrdinalIgnoreCase))
                    {
                        if (reader.Read() && (reader.TokenType == JsonTokenType.True || reader.TokenType == JsonTokenType.False))
                            quest.CanBeDailyQuest = !reader.GetBoolean();
                    }
                    else if (propertyName.Equals("presets"))
                    {
                        if (reader.Read() && reader.TokenType == JsonTokenType.StartArray)
                        {
                            quest.ReadPresets(ref reader);
                        }
                    }
                    else if (reader.Read())
                    {
                        quest.OnPropertyRead(propertyName, ref reader);
                    }
                }
                else
                {
                    L.LogWarning("Lost property \"" + propertyName + "\" reading a quest, \"quest_type\" must be the first property.");
                }
            }
        }
        return quest;
    }
    /// <summary>Read all quests.</summary>
    public static void ReadQuestDatas()
    {
        Quests.Clear();
        if (!File.Exists(QUEST_LOCATION))
        {
            using (FileStream stream = new FileStream(QUEST_LOCATION, FileMode.Open, FileAccess.Write, FileShare.None))
            {
                byte[] bytes = Encoding.UTF8.GetBytes("[]");
                stream.Write(bytes, 0, bytes.Length);
            }
            return;
        }
        using (FileStream stream = new FileStream(QUEST_LOCATION, FileMode.Open, FileAccess.Read, FileShare.None))
        {
            if (stream.Length > int.MaxValue)
                return;
            byte[] bytes = new byte[stream.Length];
            stream.Read(bytes, 0, bytes.Length);
            Utf8JsonReader reader = new Utf8JsonReader(bytes, JsonEx.readerOptions);
            while (reader.Read())
            {
                if (reader.TokenType == JsonTokenType.EndArray)
                {
                    break;
                }
                if (reader.TokenType == JsonTokenType.StartObject)
                {
                    BaseQuestData data = ReadQuestData(ref reader);
                    if (data != null)
                        Quests.Add(data);
                }
            }
        }
    }
    private static string GetSavePath(ulong steam64, Guid key, ulong team) => "\\Players\\" + steam64.ToString(Data.Locale) +
                                                    "_0\\Uncreated_S" + UCWarfare.Version.Major.ToString(Data.Locale) + "\\Quests\\" + team + "_" + key.ToString("N") + ".json";
    public static void SaveProgress(BaseQuestTracker t, ulong team)
    {
        if (t.PresetKey == default) return;
        string savePath = GetSavePath(t.Player.Steam64, t.PresetKey, team);
        using (FileStream stream = new FileStream(savePath, FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.Read))
        {
            Utf8JsonWriter writer = new Utf8JsonWriter(stream, JsonEx.writerOptions);
            writer.WriteStartObject();
            writer.WritePropertyName("key");
            writer.WriteStringValue(t.PresetKey.ToString("N"));
            writer.WritePropertyName("team");
            writer.WriteNumberValue(team);
            t.WriteQuestProgress(writer);
            writer.WriteEndObject();
            writer.Dispose();
        }
    }
    public static void ReadProgress(BaseQuestTracker t, ulong team)
    {
        if (t.PresetKey == default) return;
        string savePath = GetSavePath(t.Player.Steam64, t.PresetKey, team);
        using (FileStream stream = new FileStream(savePath, FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.Read))
        {
            if (stream.Length > int.MaxValue)
                return;
            byte[] bytes = new byte[stream.Length];
            stream.Read(bytes, 0, bytes.Length);
            Utf8JsonReader reader = new Utf8JsonReader(bytes, JsonEx.readerOptions);
            while (reader.Read())
            {
                if (reader.TokenType == JsonTokenType.PropertyName)
                {
                    string prop = reader.GetString();
                    if (prop.Equals("key", StringComparison.OrdinalIgnoreCase))
                    {
                        if (!reader.TryGetGuid(out Guid guid) || guid != t.PresetKey)
                        {
                            L.LogWarning("Mis-match between key in file " + savePath);
                        }
                    }
                    else if (prop.Equals("team", StringComparison.OrdinalIgnoreCase))
                    {
                        if (!reader.TryGetUInt64(out ulong team2) || team2 != team)
                        {
                            L.LogWarning("Mis-match between team in file " + savePath);
                        }
                    }
                    else if (reader.Read())
                    {
                        try
                        {
                            t.OnReadProgressSaveProperty(prop, ref reader);
                        }
                        catch (Exception ex)
                        {
                            L.LogError("Failed to read property " + prop + " in progress save for preset " + t.PresetKey + 
                                       " of kit type " + t.QuestData.QuestType + " for player " + t.Player.Steam64 + " in file \"" + savePath + "\".");
                            L.LogError(ex);
                        }
                    }
                }
            }
        }
    }
    #endregion
    #region events

    // put all interface events here

    public static void OnPlayerKilled(UCWarfare.KillEventArgs kill)
    {
        foreach (INotifyOnKill tracker in RegisteredTrackers.OfType<INotifyOnKill>())
            tracker.OnKill(kill);
    }
    public static void OnBuildableBuilt(UCPlayer constructor, FOBs.BuildableData buildable)
    {
        foreach (INotifyBuildableBuilt tracker in RegisteredTrackers.OfType<INotifyBuildableBuilt>())
            tracker.OnBuildableBuilt(constructor, buildable);
    }
    public static void OnFOBBuilt(UCPlayer constructor, Components.FOB fob)
    {
        foreach (INotifyFOBBuilt tracker in RegisteredTrackers.OfType<INotifyFOBBuilt>())
            tracker.OnFOBBuilt(constructor, fob);
    }
    public static void OnSuppliesConsumed(Components.FOB fob, ulong player, int amount)
    {
        foreach (INotifySuppliesConsumed tracker in RegisteredTrackers.OfType<INotifySuppliesConsumed>())
            tracker.OnSuppliesConsumed(fob, player, amount);
    }
    public static void OnObjectiveCaptured(ulong[] participants)
    {
        foreach (INotifyOnObjectiveCaptured tracker in RegisteredTrackers.OfType<INotifyOnObjectiveCaptured>())
            tracker.OnObjectiveCaptured(participants);
    }
    public static void OnRevive(UCPlayer reviver, UCPlayer revived)
    {
        foreach (INotifyOnRevive tracker in RegisteredTrackers.OfType<INotifyOnRevive>())
            tracker.OnPlayerRevived(reviver, revived);
    }
    #endregion
}