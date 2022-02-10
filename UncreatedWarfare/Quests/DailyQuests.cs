using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Uncreated.Warfare.Quests.Types;

namespace Uncreated.Warfare.Quests;

public static class DailyQuests
{
    private static DateTime LastRefresh;
    public const int DAILY_QUEST_COUNT = 3;
    public static BaseQuestData[] DailyQuestDatas = new BaseQuestData[DAILY_QUEST_COUNT];
    public static IQuestState[] States = new IQuestState[DAILY_QUEST_COUNT];
    public static Dictionary<ulong, DailyQuestTracker> DailyTrackers = new Dictionary<ulong, DailyQuestTracker>();
    /// <summary>Checks if a day has passed.</summary>
    public static void Tick()
    {
        DateTime now = DateTime.Now;
        if ((now - LastRefresh).TotalDays > 1d)
        {
            L.Log("Generating new Daily Quests.", ConsoleColor.Magenta);
            LastRefresh = now;
            foreach (KeyValuePair<ulong, DailyQuestTracker> tracker in DailyTrackers)
            {
                for (int i = 0; i < DAILY_QUEST_COUNT; i++)
                    QuestManager.DeregisterTracker(tracker.Value.Trackers[i]);
            }
            DailyTrackers.Clear();
            CreateNewDailyQuests();
            for (int i = 0; i < PlayerManager.OnlinePlayers.Count; i++)
            {
                RegisterDailyTrackers(PlayerManager.OnlinePlayers[i]);
            }
        }
    }
    /// <summary>Should run on player connected.</summary>
    public static void RegisterDailyTrackers(UCPlayer player)
    {
        BaseQuestTracker[] trackers = new BaseQuestTracker[DAILY_QUEST_COUNT];
        for (int i = 0; i < DAILY_QUEST_COUNT; i++)
        {
            BaseQuestTracker tracker = DailyQuestDatas[i].GetTracker(player, ref States[i]);
            if (tracker != null)
            {
                QuestManager.RegisterTracker(tracker);
                tracker.IsDailyQuest = true;
                trackers[i] = tracker;
            }
            else
            {
                L.LogWarning("Failed to create tracker for daily quest " + DailyQuestDatas[i].QuestType);
            }
        }

        DailyQuestTracker tr = new DailyQuestTracker(player, trackers);
        LoadSave(tr);
        DailyTrackers.Add(player.Steam64, tr);
    }
    /// <summary>Should run on player disconnected.</summary>
    public static void DeregisterDailyTrackers(UCPlayer player)
    {
        if (DailyTrackers.TryGetValue(player.Steam64, out DailyQuestTracker tracker))
        {
            for (int i = 0; i < DAILY_QUEST_COUNT; i++)
            {
                QuestManager.DeregisterTracker(tracker.Trackers[i]);
            }
            DailyTrackers.Remove(player.Steam64);
        }
    }
    public static void OnDailyQuestCompleted(BaseQuestTracker tracker)
    {
        L.Log("Daily quest " + tracker.QuestData.QuestType + " completed: \"" + tracker.Translate() + "\"", ConsoleColor.Cyan);
        // todo UI or something, xp reward?
    }

    public static void OnDailyQuestUpdated(BaseQuestTracker tracker)
    {
        if (DailyTrackers.TryGetValue(tracker.Player.Steam64, out DailyQuestTracker t2))
        {
            SaveProgress(t2);
        }
        L.Log("Daily quest " + tracker.QuestData.QuestType + " updated: \"" + tracker.Translate() + "\"");
    }
    /// <summary>Runs every day, creates the daily quests for the day.</summary>
    public static void CreateNewDailyQuests()
    {
        LastRefresh = DateTime.Now;
        if (QuestManager.Quests.Count <= DAILY_QUEST_COUNT)
        {
            L.LogError("Not enough quest types defined to create " + DAILY_QUEST_COUNT + " daily quests.");
            return;
        }
        int[] ints = new int[DAILY_QUEST_COUNT];
        for (int i = 0; i < DAILY_QUEST_COUNT; i++)
        {
            int rndPick;
            // pick DAILY_QUEST_COUNT unique values
            while (true)
            {
                exists:;
                rndPick = UnityEngine.Random.Range(0, QuestManager.Quests.Count);
                for (int p = 0; p < i; p++)
                {
                    if (ints[p] == rndPick)
                        goto exists;
                }
                break;
            }

            ints[i] = rndPick;
            DailyQuestDatas[i] = QuestManager.Quests[rndPick];
            States[i] = DailyQuestDatas[i].GetState();
        }
    }
    private static string GetDailySavePath(ulong steam64) => Path.GetFullPath("\\Players\\" + steam64.ToString(Data.Locale) +
                                                             "_0\\Uncreated_S" + UCWarfare.Version.Major.ToString(Data.Locale) + "\\daily_quest_progress.json");
    public static void SaveProgress(DailyQuestTracker tracker)
    {
        string path = GetDailySavePath(tracker.Player.Steam64);
        
        using (FileStream stream = new FileStream(path, FileMode.Create, FileAccess.Write, FileShare.Read))
        {
            Utf8JsonWriter writer = new Utf8JsonWriter(stream, JsonEx.writerOptions);
            writer.WriteStartObject();
            writer.WritePropertyName("time");
            writer.WriteNumberValue(LastRefresh.Ticks);
            writer.WritePropertyName("daily_challenges");
            writer.WriteStartArray();
            for (int i = 0; i < DAILY_QUEST_COUNT; i++)
            {
                writer.WriteStartObject();
                tracker.Trackers[i].WriteQuestProgress(writer);
                writer.WriteEndObject();
            }
            writer.WriteEndArray();
            writer.WriteEndObject();
            writer.Dispose();
        }
    }
    public static void LoadSave(DailyQuestTracker tracker)
    {
        string path = GetDailySavePath(tracker.Player.Steam64);
        if (!File.Exists(path))
            return;
        using (FileStream stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read))
        {
            if (stream.Length > int.MaxValue)
                return;
            byte[] bytes = new byte[stream.Length];
            stream.Read(bytes, 0, bytes.Length);
            Utf8JsonReader reader = new Utf8JsonReader(bytes, JsonEx.readerOptions);
            if (reader.Read() && reader.TokenType == JsonTokenType.StartObject)
            {
                while (reader.Read() && reader.TokenType == JsonTokenType.PropertyName)
                {
                    if (reader.GetString().Equals("time", StringComparison.OrdinalIgnoreCase))
                    {
                        if (reader.Read() && reader.TokenType == JsonTokenType.Number && reader.TryGetInt64(out long time))
                        {
                            if (time != LastRefresh.Ticks)
                                goto deleteFile; // expired progress file from another day, delete the file and load default values
                        }
                    }
                    else if (reader.GetString().Equals("daily_challenges", StringComparison.OrdinalIgnoreCase))
                    {
                        if (reader.Read() && reader.TokenType == JsonTokenType.StartArray)
                        {
                            int i = -1;
                            while (reader.Read() && reader.TokenType != JsonTokenType.EndArray)
                            {
                                if (i >= DAILY_QUEST_COUNT) return;
                                if (reader.TokenType == JsonTokenType.StartObject)
                                {
                                    i++;
                                }
                                else if (reader.TokenType == JsonTokenType.PropertyName)
                                {
                                    string prop = reader.GetString();
                                    if (reader.Read() && i != -1)
                                    {
                                        try
                                        {
                                            tracker.Trackers[i].OnReadProgressSaveProperty(prop, ref reader);
                                        }
                                        catch (Exception ex)
                                        {
                                            L.LogError("Error reading property " + prop + " in daily quest progress of " + tracker.Player.Steam64 + " in quest " + i);
                                            L.LogError(ex);
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
            }
        }
        for (int i = 0; i < DAILY_QUEST_COUNT; i++)
        {
            if (tracker.Trackers[i].Flag != 0)
            {
                tracker.Trackers[i].Player.Player.quests.sendSetFlag(tracker.Trackers[i].Flag, tracker.Trackers[i].FlagValue);
            }
        }
        return;
        deleteFile:
        File.Delete(path);
    }
}
public class DailyQuestTracker
{
    public UCPlayer Player;
    public BaseQuestTracker[] Trackers;
    public DailyQuestTracker(UCPlayer player, BaseQuestTracker[] trackers)
    {
        if (trackers.Length != DailyQuests.DAILY_QUEST_COUNT)
        {
            throw new ArgumentOutOfRangeException(nameof(trackers), "Trackers should be the same length as amount of daily quests.");
        }
        this.Player = player;
        this.Trackers = trackers;
    }
}