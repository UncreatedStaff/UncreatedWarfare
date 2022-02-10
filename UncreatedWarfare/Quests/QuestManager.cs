using SDG.Unturned;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Uncreated.Warfare.Kits;
using Uncreated.Warfare.Quests.Types;
using Uncreated.Warfare.Ranks;
using Uncreated.Warfare.Squads;
using Uncreated.Warfare.Vehicles;

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
        OnQuestStarted(tracker);
        RegisteredTrackers.Add(tracker);
        return tracker;
    }
    /// <summary>Find, generate, and register a tracker using a <paramref name="key"/> and a set <see cref="IQuestPreset"/>.</summary>
    /// <returns>A tracker using a <see cref="IQuestPreset"/> that is matched by <see cref="IQuestPreset.Key"/> and <see cref="IQuestPreset.Team"/> (or 0), or <see langword="null"/> if no preset is found.</returns>
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
                    if (tr == null)
                    {
                        L.LogWarning("Failed to get a tracker from key " + key.ToString("N"));
                        return null;
                    }
                    ReadProgress(tr, team);
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
                    if (tr == null)
                    {
                        L.LogWarning("Failed to get a tracker from key " + key.ToString("N"));
                        return null;
                    }
                    ReadProgress(tr, team);
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
    public static void PrintAllQuests(UCPlayer player)
    {
        L.Log("All quests:");
        for (int i = 0; i < Quests.Count; i++)
        {
            L.Log("\n    " + Quests[i].ToString());
            foreach (IQuestPreset preset in Quests[i].Presets)
            {
                L.Log("    Preset " + preset.Key);
                FieldInfo[] fields = preset.GetType().GetFields(BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Public);
                for (int f = 0; f < fields.Length; f++)
                {
                    if (fields[f].Name != "_state")
                        L.Log("        " + fields[f].Name + ": " + (fields[f].GetValue(preset)?.ToString() ?? "null"));
                }
                L.Log("        State:");
                FieldInfo[] fields2 = preset.State.GetType().GetFields(BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Public);
                for (int f = 0; f < fields2.Length; f++)
                {
                    L.Log("            " + fields2[f].Name + ": " + (fields2[f].GetValue(preset.State)?.ToString() ?? "null"));
                }
            }
        }
        if (player == null) return;
        L.Log("Player Quests: " + player.Steam64.ToString());
        L.Log("  Rank progress:");
        for (int i = 0; i < player.RankData.Length; i++)
        {
            L.Log("    " + player.RankData[i].ToString());
        }
        L.Log("  All trackers:");
        for (int i = 0; i < RegisteredTrackers.Count; i++)
        {
            if (RegisteredTrackers[i].Player.Steam64 == player.Steam64)
                L.Log("    Tracker type " + RegisteredTrackers[i].QuestData.QuestType + " - \"" + RegisteredTrackers[i].Translate() + "\".");
        }
    }
    /// <summary>Ticks any <see cref="BaseQuestTracker"/> that's data has overridden <see cref="BaseQuestData.TickFrequencySeconds"/> and has set it > 0.</summary>
    public static void OnGameTick()
    {
        DailyQuests.Tick();
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
            if (tracker.PresetKey != default)
            {
                if (!RankManager.OnQuestCompleted(tracker.Player, tracker.PresetKey))
                    if (!KitManager.OnQuestCompleted(tracker.Player, tracker.PresetKey))
                        VehicleBay.OnQuestCompleted(tracker.Player, tracker.PresetKey);
            }
            // LevelManager.OnQuestCompleted(tracker.key);  (we need something like this)
            
            // TODO: Update a UI and check for giving levels, etc.
        }
    }
    public static void OnQuestUpdated(BaseQuestTracker tracker, bool skipFlagUpdate = false)
    {
        if (tracker.IsDailyQuest)
            DailyQuests.OnDailyQuestUpdated(tracker);
        else
        {
            SaveProgress(tracker, tracker.Player.GetTeam());
            if (tracker.Flag != 0 && !skipFlagUpdate)
            {
                tracker.Player.Player.quests.sendSetFlag(tracker.Flag, tracker.FlagValue);
                L.LogDebug("Flag quest updated: " + tracker.FlagValue);
            }
        }
        L.LogDebug("Quest updated: " + tracker.Translate());
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
                    if (reader.Read())
                    {
                        if (propertyName.Equals("translations", StringComparison.OrdinalIgnoreCase))
                        {
                            quest.Translations = new Dictionary<string, string>();
                            if (reader.TokenType == JsonTokenType.StartObject)
                            {
                                while (reader.Read() && reader.TokenType != JsonTokenType.EndObject)
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
                        else if (propertyName.Equals("remove_from_daily_quests", StringComparison.OrdinalIgnoreCase))
                        {
                            if (reader.TokenType == JsonTokenType.True || reader.TokenType == JsonTokenType.False)
                                quest.CanBeDailyQuest = !reader.GetBoolean();
                        }
                        else if (propertyName.Equals("presets"))
                        {
                            if (reader.TokenType == JsonTokenType.StartArray)
                            {
                                quest.ReadPresets(ref reader);
                            }
                            else
                                L.LogWarning("Failed to read \"presets\" property correctly.");
                        }
                        else
                        {
                            quest.OnPropertyRead(propertyName, ref reader);
                        }
                    }
                    else
                        L.LogWarning("Lost property \"" + propertyName + "\" reading a quest, \"quest_type\", failed to read value.");
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
                    try
                    {
                        BaseQuestData data = ReadQuestData(ref reader);
                        if (data != null)
                            Quests.Add(data);
                    }
                    catch (Exception ex)
                    {
                        L.LogError("Error reading data for a quest.");
                        L.LogError(ex);
                    }
                }
            }
        }
    }
    private static string GetSavePath(ulong steam64, Guid key, ulong team) => Path.GetFullPath("\\Players\\" + steam64.ToString(Data.Locale) +
                                                    "_0\\Uncreated_S" + UCWarfare.Version.Major.ToString(Data.Locale) + "\\Quests\\" + team + "_" + key.ToString("N") + ".json");
    public static void SaveProgress(BaseQuestTracker t, ulong team)
    {
        if (t.PresetKey == default) return;
        string savePath = GetSavePath(t.Player.Steam64, t.PresetKey, team);
        string dir = Path.GetDirectoryName(savePath);
        if (!Directory.Exists(dir)) Directory.CreateDirectory(dir);
        using (FileStream stream = new FileStream(savePath, FileMode.Create, FileAccess.ReadWrite, FileShare.Read))
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
        if (!File.Exists(savePath)) return;
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
        if (t.Flag != 0)
        {
            t.Player.Player.quests.sendSetFlag(t.Flag, t.FlagValue);
        }
    }
    #endregion
    #region events

    // put all interface events here

    public static void OnKill(UCWarfare.KillEventArgs kill)
    {
        foreach (INotifyOnKill tracker in RegisteredTrackers.OfType<INotifyOnKill>())
            tracker.OnKill(kill);
    }
    public static void OnDeath(UCWarfare.DeathEventArgs death)
    {
        foreach (INotifyOnDeath tracker in RegisteredTrackers.OfType<INotifyOnDeath>())
            tracker.OnDeath(death);
    }
    public static void OnDeath(UCWarfare.SuicideEventArgs death)
    {
        foreach (INotifyOnDeath tracker in RegisteredTrackers.OfType<INotifyOnDeath>())
            tracker.OnSuicide(death);
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
    public static void OnGameOver(ulong winner)
    {
        foreach (INotifyGameOver tracker in RegisteredTrackers.OfType<INotifyGameOver>())
            tracker.OnGameOver(winner);
    }
    public static void OnGainedXP(UCPlayer player, int amtGained, int total, int gameTotal, EBranch branch)
    {
        foreach (INotifyGainedXP tracker in RegisteredTrackers.OfType<INotifyGainedXP>())
            tracker.OnGainedXP(player, amtGained, total, gameTotal, branch);
    }
    public static void OnRallyActivated(RallyPoint rally)
    {
        foreach (INotifyRallyActive tracker in RegisteredTrackers.OfType<INotifyRallyActive>())
            tracker.OnRallyActivated(rally);
    }
    public static void OnVehicleDestroyed(UCPlayer owner, UCPlayer destroyer, VehicleData data, Components.VehicleComponent component)
    {
        foreach (INotifyVehicleDestroyed tracker in RegisteredTrackers.OfType<INotifyVehicleDestroyed>())
            tracker.OnVehicleDestroyed(owner, destroyer, data, component);
    }
    public static void OnDistanceUpdated(ulong lastDriver, float totalDistance, float newDistance, Components.VehicleComponent vehicle)
    {
        foreach (INotifyVehicleDistanceUpdates tracker in RegisteredTrackers.OfType<INotifyVehicleDistanceUpdates>())
            tracker.OnDistanceUpdated(lastDriver, totalDistance, newDistance, vehicle);
    }
    #endregion
}