using SDG.Unturned;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Uncreated.Networking;
using Uncreated.Players;
using Uncreated.Warfare.Quests.Types;

namespace Uncreated.Warfare.Quests;

public static class DailyQuests
{
    private static DateTime LastRefresh;
    public static BaseQuestData[] DailyQuestDatas = new BaseQuestData[DailyQuest.DAILY_QUEST_CONDITION_LENGTH];
    public static IQuestState[] States = new IQuestState[DailyQuest.DAILY_QUEST_CONDITION_LENGTH];
    public static Dictionary<ulong, DailyQuestTracker> DailyTrackers = new Dictionary<ulong, DailyQuestTracker>();
    private static DailyQuestSave[] _quests;
    private static DailyQuest[] _sendQuests;
    private static int index = 0;
    public static void OnLoad()
    {
        ReadQuests();
        CreateNewDailyQuests();
    }
    /// <summary>Checks if a day has passed.</summary>
    public static void Tick()
    {
#if DEBUG
        using IDisposable profiler = ProfilingUtils.StartTracking();
#endif
        DateTime now = DateTime.Now;
        if ((now - LastRefresh).TotalDays >= 1d)
        {
            L.Log("Generating new Daily Quests.", ConsoleColor.Magenta);
            LastRefresh = now;
            index++;
            if (index > DailyQuest.DAILY_QUEST_LENGTH / 2)
            {
                CreateNextModContent();
                index = 0;
            }
            CreateNewDailyQuests();

            foreach (KeyValuePair<ulong, DailyQuestTracker> tracker in DailyTrackers)
            {
                for (int i = 0; i < DailyQuest.DAILY_QUEST_CONDITION_LENGTH; i++)
                    QuestManager.DeregisterTracker(tracker.Value.Trackers[i]);
            }
            DailyTrackers.Clear();
            for (int i = 0; i < PlayerManager.OnlinePlayers.Count; i++)
            {
                RegisterDailyTrackers(PlayerManager.OnlinePlayers[i]);
            }
        }
    }
    public static void CreateNewModContent()
    {
        if (QuestManager.Quests.Count <= DailyQuest.DAILY_QUEST_CONDITION_LENGTH)
        {
            L.LogError("Not enough quest types defined to create " + DailyQuest.DAILY_QUEST_CONDITION_LENGTH + " daily quests.");
            return;
        }
        DateTime now = DateTime.Today;
        _quests = new DailyQuestSave[DailyQuest.DAILY_QUEST_LENGTH];
        _sendQuests = new DailyQuest[DailyQuest.DAILY_QUEST_LENGTH];
        for (int day = 0; day < DailyQuest.DAILY_QUEST_LENGTH; ++day)
        {
            ref DailyQuest dq = ref _sendQuests[day];
            ref DailyQuestSave save = ref _quests[day];
            GetConditions(ref dq, ref save, day, now);
        }

        ReplicateQuestChoices();
    }
    public static void CreateNextModContent()
    {
        if (QuestManager.Quests.Count <= DailyQuest.DAILY_QUEST_CONDITION_LENGTH)
        {
            L.LogError("Not enough quest types defined to create " + DailyQuest.DAILY_QUEST_CONDITION_LENGTH + " daily quests.");
            return;
        }


        int half = DailyQuest.DAILY_QUEST_LENGTH / 2;
        DailyQuest[] quests = _sendQuests;
        _sendQuests = new DailyQuest[DailyQuest.DAILY_QUEST_LENGTH];
        for (int i = half; i < DailyQuest.DAILY_QUEST_LENGTH; ++i)
        {
            ref DailyQuest q = ref quests[i];
            _sendQuests[i - half] = q;
        }
        DailyQuestSave[] quests2 = _quests;
        _quests = new DailyQuestSave[DailyQuest.DAILY_QUEST_LENGTH];
        for (int i = half; i < DailyQuest.DAILY_QUEST_LENGTH; ++i)
        {
            ref DailyQuestSave q = ref quests2[i];
            _quests[i - half] = q;
        }

        DateTime now = DateTime.Today;
        _quests = new DailyQuestSave[DailyQuest.DAILY_QUEST_LENGTH];
        _sendQuests = new DailyQuest[DailyQuest.DAILY_QUEST_LENGTH];
        for (int day = half; day < DailyQuest.DAILY_QUEST_LENGTH; ++day)
        {
            ref DailyQuest dq = ref _sendQuests[day];
            ref DailyQuestSave save = ref _quests[day];
            GetConditions(ref dq, ref save, day, now);
        }

        ReplicateQuestChoices();
    }
    private static void GetConditions(ref DailyQuest dq, ref DailyQuestSave save, int day, DateTime now)
    {
        dq.conditions = new DailyQuest.Condition[DailyQuest.DAILY_QUEST_CONDITION_LENGTH];
        save.Presets = new DailyQuestSave.Preset[DailyQuest.DAILY_QUEST_CONDITION_LENGTH];
        save.StartDate = day == 0 ? now : now.AddDays(day);
        int[] ints = new int[DailyQuest.DAILY_QUEST_CONDITION_LENGTH];
        for (int i = 0; i < DailyQuest.DAILY_QUEST_CONDITION_LENGTH; i++)
        {
            int rndPick;
            while (true)
            {
                exists:;
                rndPick = UnityEngine.Random.Range(0, QuestManager.Quests.Count);
                for (int p = 0; p < i; ++p)
                {
                    if (ints[p] == rndPick)
                        goto exists;
                }
                if (!QuestManager.Quests[rndPick].CanBeDailyQuest) goto exists;
                break;
            }
            BaseQuestData data = QuestManager.Quests[rndPick];
            ints[i] = rndPick;
            ref DailyQuest.Condition cond = ref dq.conditions[i];
            ref DailyQuestSave.Preset pset = ref save.Presets[i];
            IQuestPreset preset = data.CreateRandomPreset((ushort)(DailyQuest.DAILY_QUEST_START_ID + day * DailyQuest.DAILY_QUEST_CONDITION_LENGTH + i));
            IQuestState state = preset.State;
            BaseQuestTracker? tempTracker = data.GetTracker(null, ref state);
            if (tempTracker != null)
            {
                pset.isValid = true;
                pset.PresetObj = preset;
                pset.Type = data.QuestType;
                cond.FlagValue = preset.State.FlagValue.InsistValue();
                cond.Translation = tempTracker.GetDisplayString(true);
                cond.Key = preset.Key;
            }
            else
            {
                L.LogWarning("Failed to get daily tracker for " + data.QuestType);
            }
        }
    }
    /// <summary>Should run on player connected.</summary>
    public static void RegisterDailyTrackers(UCPlayer player)
    {
#if DEBUG
        using IDisposable profiler = ProfilingUtils.StartTracking();
#endif
        BaseQuestTracker[] trackers = new BaseQuestTracker[DailyQuest.DAILY_QUEST_CONDITION_LENGTH];
        for (int i = 0; i < DailyQuest.DAILY_QUEST_CONDITION_LENGTH; i++)
        {
            BaseQuestTracker? tracker = DailyQuestDatas[i].GetTracker(player, ref States[i]);
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
        if (DailyTrackers.TryGetValue(player.Steam64, out DailyQuestTracker tr2))
        {
            for (int i = 0; i < DailyQuest.DAILY_QUEST_CONDITION_LENGTH; i++)
                QuestManager.DeregisterTracker(tr2.Trackers[i]);
            DailyTrackers[player.Steam64] = tr;
        }
        else
            DailyTrackers.Add(player.Steam64, tr);
    }
    /// <summary>Should run on player disconnected.</summary>
    public static void DeregisterDailyTrackers(UCPlayer player)
    {
#if DEBUG
        using IDisposable profiler = ProfilingUtils.StartTracking();
#endif
        if (DailyTrackers.TryGetValue(player.Steam64, out DailyQuestTracker tracker))
        {
            for (int i = 0; i < DailyQuest.DAILY_QUEST_CONDITION_LENGTH; i++)
            {
                QuestManager.DeregisterTracker(tracker.Trackers[i]);
            }
            DailyTrackers.Remove(player.Steam64);
        }
    }
    public static void OnDailyQuestCompleted(BaseQuestTracker tracker)
    {
#if DEBUG
        using IDisposable profiler = ProfilingUtils.StartTracking();
#endif
        if (tracker.QuestData != null)
            L.Log("Daily quest " + tracker.QuestData.QuestType + " completed: \"" + tracker.GetDisplayString() + "\"", ConsoleColor.Cyan);
        ToastMessage.QueueMessage(tracker.Player!, new ToastMessage("Daily Quest Completed!", tracker.GetDisplayString(), "good job man idk does this need filled?", EToastMessageSeverity.PROGRESS));
        // todo UI or something, xp reward?
        tracker.Player?.SendChat("Daily Quest Completed!");
    }

    public static void OnDailyQuestUpdated(BaseQuestTracker tracker)
    {
#if DEBUG
        using IDisposable profiler = ProfilingUtils.StartTracking();
#endif
        if (DailyTrackers.TryGetValue(tracker.Player!.Steam64, out DailyQuestTracker t2))
        {
            SaveProgress(t2);
        }
        tracker.Player.SendChat("Daily Quest updated: " + tracker.GetDisplayString());
    }
    /// <summary>Runs every day, creates the daily quests for the day.</summary>
    public static void CreateNewDailyQuests()
    {
#if DEBUG
        using IDisposable profiler = ProfilingUtils.StartTracking();
#endif
        LastRefresh = DateTime.Now;

        ref DailyQuestSave save = ref _quests[index];
        for (int i = 0; i < DailyQuest.DAILY_QUEST_CONDITION_LENGTH; ++i)
        {
            ref DailyQuestSave.Preset preset = ref save.Presets[i];

            EQuestType type = preset.Type;
            BaseQuestData? data = QuestManager.Quests.Find(x => x.QuestType == type);
            if (data != null)
            {
                DailyQuestDatas[i] = data;
                States[i] = preset.PresetObj.State;
            }
        }
    }
    public static void ReplicateQuestChoices()
    {
        NetTask task = SendNextQuests.Request(AckNextQuestsUploaded, Data.NetClient.connection, _sendQuests);
        Task.Run(async () =>
        {
            NetTask.Response response = await task;
            if (response.Responded && response.Parameters.Length > 0 && response.Parameters[0] is Folder folder)
            {
                string p = QuestManager.QUEST_FOLDER + "DailyQuests\\";
                folder.WriteToDisk(p);
                await UCWarfare.ToUpdate();
                Assets.load(p, true, EAssetOrigin.WORKSHOP, true, 2773379635ul);
            }
        });
    }
    public static void SaveQuests()
    {
        using (FileStream stream = new FileStream(QuestManager.QUEST_FOLDER + "daily_quests.json", FileMode.Create, FileAccess.Write, FileShare.Read))
        {
            Utf8JsonWriter writer = new Utf8JsonWriter(stream, JsonEx.writerOptions);
            writer.WriteStartObject();
            writer.WritePropertyName("index");
            writer.WriteNumberValue(index);
            writer.WritePropertyName("quest_schedule");
            writer.WriteStartArray();
            for (int i = 0; i < _quests.Length; ++i)
            {
                ref DailyQuestSave quest = ref _quests[i];
                writer.WriteStartObject();
                writer.WritePropertyName("start_time");
                writer.WriteNumberValue(quest.StartDate.Ticks);
                writer.WritePropertyName("presets");
                writer.WriteStartArray();
                for (int j = 0; j < quest.Presets.Length; ++j)
                {
                    ref DailyQuestSave.Preset preset = ref quest.Presets[j];
                    writer.WriteStartObject();
                    writer.WriteString("quest_type", preset.Type.ToString());
                    writer.WriteString("key", preset.PresetObj.Key);
                    writer.WriteNumber("team", preset.PresetObj.Team);
                    writer.WriteNumber("flag", preset.PresetObj.Flag);
                    writer.WritePropertyName("state");
                    writer.WriteStartObject();
                    preset.PresetObj.State.WriteQuestState(writer);
                    writer.WriteEndObject();
                    writer.WriteEndObject();
                }
                writer.WriteEndArray();
                writer.WriteEndObject();
            }
            writer.WriteEndArray();
        }
    }
    public static void ReadQuests()
    {
        string p = QuestManager.QUEST_FOLDER + "daily_quests.json";
        if (!File.Exists(p))
        {
            CreateNewModContent();
            return;
        }
        using (FileStream stream = new FileStream(p, FileMode.Open, FileAccess.Read, FileShare.Read))
        {
            if (stream.Length > int.MaxValue)
            {
                L.LogError(p + " is too long to be read.");
                return;
            }
            byte[] buffer = new byte[stream.Length];
            stream.Read(buffer, 0, buffer.Length);
            Utf8JsonReader reader = new Utf8JsonReader();
            while (reader.Read())
            {
                if (reader.TokenType == JsonTokenType.EndObject) break;
                if (reader.TokenType == JsonTokenType.PropertyName)
                {
                    string? property = reader.GetString();
                    if (reader.Read() && property != null)
                    {
                        switch (property)
                        {
                            case "index":
                                if (reader.TokenType == JsonTokenType.Number)
                                {
                                    reader.TryGetInt32(out index);
                                }
                                break;
                            case "quest_schedule":
                                if (reader.TokenType == JsonTokenType.StartArray)
                                {
                                    _quests = new DailyQuestSave[DailyQuest.DAILY_QUEST_LENGTH];
                                    int i = -1;
                                    while (reader.Read())
                                    {
                                        if (reader.TokenType == JsonTokenType.EndArray) break;
                                        if (reader.TokenType == JsonTokenType.StartObject) ++i;
                                        else if (i < DailyQuest.DAILY_QUEST_LENGTH)
                                        {
                                            if (reader.TokenType == JsonTokenType.PropertyName)
                                            {
                                                string? prop = reader.GetString();
                                                if (reader.Read() && prop != null)
                                                {
                                                    ref DailyQuestSave save = ref _quests[i];
                                                    switch (prop)
                                                    {
                                                        case "start_time":
                                                            if (reader.TokenType == JsonTokenType.Number && reader.TryGetInt64(out long ticks))
                                                                save.StartDate = new DateTime(ticks);
                                                            else if (reader.TokenType == JsonTokenType.String)
                                                            {
                                                                string? v = reader.GetString();
                                                                if (v != null) 
                                                                    DateTime.TryParse(v, Data.Locale, System.Globalization.DateTimeStyles.AssumeLocal, out save.StartDate);
                                                            }
                                                            break;
                                                        case "presets":
                                                            save.Presets = new DailyQuestSave.Preset[DailyQuest.DAILY_QUEST_CONDITION_LENGTH];
                                                            int j = 0;
                                                            if (reader.TokenType == JsonTokenType.StartArray)
                                                            {
                                                                while (reader.Read())
                                                                {
                                                                    if (reader.TokenType == JsonTokenType.EndArray) break;
                                                                    if (reader.TokenType == JsonTokenType.StartObject) ++j;
                                                                    else if (j < DailyQuest.DAILY_QUEST_CONDITION_LENGTH)
                                                                    {
                                                                        if (reader.TokenType == JsonTokenType.PropertyName)
                                                                        {
                                                                            string? prop2 = reader.GetString();
                                                                            if (reader.Read() && prop2 != null)
                                                                            {
                                                                                switch (prop2)
                                                                                {
                                                                                    case "quest_type":
                                                                                        string? v = reader.GetString();
                                                                                        ref DailyQuestSave.Preset preset = ref save.Presets[i];
                                                                                        if (v != null && Enum.TryParse(v, true, out preset.Type))
                                                                                        {
                                                                                            EQuestType type = preset.Type;
                                                                                            BaseQuestData? data = QuestManager.Quests.Find(x => x.QuestType == type);
                                                                                            if (data != null)
                                                                                            {
                                                                                                preset.PresetObj = data.ReadPreset(ref reader);
                                                                                                preset.isValid = true;
                                                                                            }
                                                                                        }
                                                                                        break;
                                                                                }
                                                                            }
                                                                        }
                                                                    }
                                                                }
                                                            }
                                                            break;
                                                    }
                                                }
                                            }
                                        }
                                    }
                                }
                                break;
                        }
                    }
                }
            }
        }
    }
    private static string GetDailySavePath(ulong steam64) => ReadWrite.PATH + ServerSavedata.directory + "\\" + Provider.serverID + "\\Players\\" + steam64.ToString(Data.Locale) +
                                                             "_0\\Uncreated_S" + UCWarfare.Version.Major.ToString(Data.Locale) + "\\daily_quest_progress.json";
    public static void SaveProgress(DailyQuestTracker tracker)
    {
#if DEBUG
        using IDisposable profiler = ProfilingUtils.StartTracking();
#endif
        string path = GetDailySavePath(tracker.Player.Steam64);
        
        using (FileStream stream = new FileStream(path, FileMode.Create, FileAccess.Write, FileShare.Read))
        {
            Utf8JsonWriter writer = new Utf8JsonWriter(stream, JsonEx.writerOptions);
            writer.WriteStartObject();
            writer.WritePropertyName("time");
            writer.WriteNumberValue(LastRefresh.Ticks);
            writer.WritePropertyName("daily_challenges");
            writer.WriteStartArray();
            for (int i = 0; i < DailyQuest.DAILY_QUEST_CONDITION_LENGTH; i++)
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
#if DEBUG
        using IDisposable profiler = ProfilingUtils.StartTracking();
#endif
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
                    if (reader.GetString()!.Equals("time", StringComparison.OrdinalIgnoreCase))
                    {
                        if (reader.Read() && reader.TokenType == JsonTokenType.Number && reader.TryGetInt64(out long time))
                        {
                            if (time != LastRefresh.Ticks)
                                goto deleteFile; // expired progress file from another day, delete the file and load default values
                        }
                    }
                    else if (reader.GetString()!.Equals("daily_challenges", StringComparison.OrdinalIgnoreCase))
                    {
                        if (reader.Read() && reader.TokenType == JsonTokenType.StartArray)
                        {
                            int i = -1;
                            while (reader.Read() && reader.TokenType != JsonTokenType.EndArray)
                            {
                                if (i >= DailyQuest.DAILY_QUEST_CONDITION_LENGTH) return;
                                if (reader.TokenType == JsonTokenType.StartObject)
                                {
                                    i++;
                                }
                                else if (reader.TokenType == JsonTokenType.PropertyName)
                                {
                                    string prop = reader.GetString()!;
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
        for (int i = 0; i < DailyQuest.DAILY_QUEST_CONDITION_LENGTH; i++)
        {
            if (tracker.Trackers[i].Flag != 0)
            {
                tracker.Trackers[i].Player!.Player.quests.sendSetFlag(tracker.Trackers[i].Flag, tracker.Trackers[i].FlagValue);
            }
        }
        return;
        deleteFile:
        File.Delete(path);
    }

    internal static readonly NetCallRaw<DailyQuest[]> SendNextQuests = new NetCallRaw<DailyQuest[]>(1125, DailyQuest.ReadMany, DailyQuest.WriteMany);
    internal static readonly NetCallRaw<Folder> AckNextQuestsUploaded = new NetCallRaw<Folder>(1126, Folder.Read, Folder.Write, 65536, true);
}
public class DailyQuestTracker
{
    public UCPlayer Player;
    public BaseQuestTracker[] Trackers;
    public DailyQuestTracker(UCPlayer player, BaseQuestTracker[] trackers)
    {
        if (trackers.Length != DailyQuest.DAILY_QUEST_CONDITION_LENGTH)
        {
            throw new ArgumentOutOfRangeException(nameof(trackers), "Trackers should be the same length as amount of daily quests.");
        }
        this.Player = player;
        this.Trackers = trackers;
    }
}
public struct DailyQuestSave
{
    public DateTime StartDate;
    public Preset[] Presets;

    public struct Preset
    {
        public bool isValid;
        public EQuestType Type;
        public IQuestPreset PresetObj;
    }
}