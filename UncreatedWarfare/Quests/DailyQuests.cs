using HarmonyLib;
using SDG.Unturned;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Uncreated.Framework;
using Uncreated.Framework.Quests;
using Uncreated.Networking;
using Uncreated.Players;
using Uncreated.Warfare.Configuration;
using Uncreated.Warfare.Quests.Types;

namespace Uncreated.Warfare.Quests;

public static class DailyQuests
{
    private const ulong DAILY_QUESTS_WORKSHOP_ID = 2773379635ul;
    public static BaseQuestData[] DailyQuestDatas = new BaseQuestData[DailyQuest.DAILY_QUEST_CONDITION_LENGTH];
    public static IQuestState[] States = new IQuestState[DailyQuest.DAILY_QUEST_CONDITION_LENGTH];
    public static Dictionary<ulong, DailyQuestTracker> DailyTrackers = new Dictionary<ulong, DailyQuestTracker>();
    private static DailyQuestSave[] _quests = new DailyQuestSave[DailyQuest.DAILY_QUEST_LENGTH];
    private static DailyQuest[] _sendQuests = new DailyQuest[DailyQuest.DAILY_QUEST_LENGTH];
    private static DateTime _nextRefresh;
    private static int index = 0;
    private volatile static bool sentCurrent = false;
    private static bool sendHrNotif = false;
    private static bool hasRead = false;
    public static TimeSpan TimeLeftForQuests => DateTime.Now - _nextRefresh;
    public static void OnConnectedToServer()
    {
        if (hasRead && !needsCreate && !sentCurrent)
            ReplicateQuestChoices();
    }

    private static readonly AssetOrigin _questModOrigin = new AssetOrigin()
    {
        name = "Daily Quests",
        workshopFileId = DAILY_QUESTS_WORKSHOP_ID
    };
    public static void EarlyLoad()
    {
        MethodInfo? m = typeof(Provider).GetMethod("onDedicatedUGCInstalled", BindingFlags.NonPublic | BindingFlags.Static);
        if (m is not null)
            Patches.Patcher.Patch(m,
                prefix: new HarmonyMethod(typeof(DailyQuests).GetMethod(nameof(OnRegisteredWorkshopID),
                    BindingFlags.Static | BindingFlags.NonPublic)));
        else L.LogWarning("Unable to patch Provider.onDedicatedUGCInstalled to register the quest mod!");
        m = typeof(PlayerQuests).GetMethod(nameof(PlayerQuests.ReceiveAbandonQuest), BindingFlags.Instance | BindingFlags.Public);
        if (m is not null)
            Patches.Patcher.Patch(m,
                postfix: new HarmonyMethod(typeof(DailyQuests).GetMethod(nameof(OnAbandonedQuest),
                    BindingFlags.Static | BindingFlags.NonPublic)));
        else L.LogWarning("Unable to patch PlayerQuests.ReceiveAbandonQuest to prevent abandoning daily missions!");
    }
    public static void Load()
    {
        ReadQuests();
        hasRead = true;

        if (needsCreate)
        {
            CreateNewModContent();
            needsCreate = false;
        }
        else
            ReplicateQuestChoices();

        ref DailyQuestSave next = ref _quests[index + 1];
        _nextRefresh = next.StartDate;
        Guid index2 = _quests[index].Guid;
        Tick();
        if (index2 == _quests[index].Guid)
        {
            CreateNewDailyQuests();
            for (int i = 0; i < PlayerManager.OnlinePlayers.Count; i++)
                RegisterDailyTrackers(PlayerManager.OnlinePlayers[i]);
        }
        LoadAssets();

        Teams.TeamSelector.OnPlayerSelected += OnPlayerJoinedTeam;
    }
    private static void PrintQuests()
    {
        if (sentCurrent)
        {
            L.LogDebug("Daily Quests (current: " + index + "):");
            using IDisposable indent = L.IndentLog(1);
            for (int i = 0; i < _quests.Length; i++)
            {
                ref DailyQuestSave save = ref _quests[i];
                if (Assets.find(save.Guid) is not QuestAsset asset)
                    L.LogWarning("Cannot find asset for day " + i + "quest.");
                else
                    L.LogDebug("Day " + i + ": " + asset.questName);
            }
        }
    }
    private static void OnRegisteredWorkshopID()
    {
        Provider.registerServerUsingWorkshopFileId(DAILY_QUESTS_WORKSHOP_ID);
    }
    private static void OnAbandonedQuest(PlayerQuests __instance, ushort id)
    {
        __instance.sendAddQuest(id);
    }
    public static void Tick()
    {
#if DEBUG
        using IDisposable profiler = ProfilingUtils.StartTracking();
#endif
        DateTime now = DateTime.Now;
        if (_nextRefresh <= now)
        {
            L.Log("Loading new Daily Quests.", ConsoleColor.Magenta);
            if ((now - _nextRefresh).TotalDays > DailyQuest.DAILY_QUEST_LENGTH / 2)
            {
                L.Log("Generating next week's Daily Quests, as new one's haven't been generated in so long.", ConsoleColor.Magenta);
                index = 0;
                CreateNewModContent();
            }
            else
            {
                do
                {
                    index++;
                    if (index > DailyQuest.DAILY_QUEST_LENGTH / 2)
                    {
                        L.Log("Generating next week's Daily Quests.", ConsoleColor.Magenta);
                        index = 0;
                        CreateNextModContent();
                    }

                    _nextRefresh = _quests[index + 1].StartDate;
                } while (_nextRefresh <= now);
            }
            sendHrNotif = false;
            CreateNewDailyQuests();

            if (index != 0)
                SaveQuests();

            L.Log("New daily quests put into action: #" + index + ", to be changed over at: " + _nextRefresh.ToString("f") + " UTC");
            Chat.Broadcast(T.DailyQuestsNewIndex, _nextRefresh);

            ref DailyQuestSave today = ref _quests[index];
            QuestAsset? tasset = Assets.find<QuestAsset>(today.Guid);
            foreach (KeyValuePair<ulong, DailyQuestTracker> tracker in DailyTrackers)
            {
                for (int i = 0; i < DailyQuest.DAILY_QUEST_CONDITION_LENGTH; i++)
                {
                    QuestManager.DeregisterTracker(tracker.Value.Trackers[i]);
                }

                if (tasset is not null)
                    tracker.Value.Player.Player.quests.sendRemoveQuest(tasset.id);
            }
            DailyTrackers.Clear();
            for (int i = 0; i < PlayerManager.OnlinePlayers.Count; i++)
            {
                RegisterDailyTrackers(PlayerManager.OnlinePlayers[i]);
            }
        }
        else if (!sendHrNotif && (_nextRefresh - now).TotalHours <= 1d)
        {
            sendHrNotif = true;
            Chat.Broadcast(T.DailyQuestsOneHourRemaining);
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
        for (int day = 0; day < DailyQuest.DAILY_QUEST_LENGTH; ++day)
        {
            ref DailyQuest dq = ref _sendQuests[day];
            ref DailyQuestSave save = ref _quests[day];
            GetConditions(ref dq, ref save, day, now, (ushort)(DailyQuest.DAILY_QUEST_START_ID + day));
        }
        SaveQuests();
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
        DailyQuestSave[] quests2 = _quests;
        _sendQuests = new DailyQuest[DailyQuest.DAILY_QUEST_LENGTH];
        _quests = new DailyQuestSave[DailyQuest.DAILY_QUEST_LENGTH];
        for (int i = half; i < DailyQuest.DAILY_QUEST_LENGTH; ++i)
        {
            ref DailyQuest q = ref quests[i];
            _sendQuests[i - half] = q;
            ref DailyQuestSave q2 = ref quests2[i];
            _quests[i - half] = q2;
        }

        DateTime now = DateTime.Today;
        ref DailyQuestSave save = ref _quests[half - 1];
        ushort startId = (ushort)(save.Id - DailyQuest.DAILY_QUEST_FLAG_START_ID);
        if (startId >= half)
            startId -= (ushort)half;
        for (int day = half; day < DailyQuest.DAILY_QUEST_LENGTH; ++day)
        {
            ref DailyQuest dq = ref _sendQuests[day];
            save = ref _quests[day];
            GetConditions(ref dq, ref save, day, now, (ushort)(DailyQuest.DAILY_QUEST_FLAG_START_ID + ++startId));
        }
        SaveQuests();
        ReplicateQuestChoices();
    }
    private static void GetConditions(ref DailyQuest dq, ref DailyQuestSave save, int day, DateTime now, ushort newId)
    {
        dq.Guid = Guid.NewGuid();
        save.Guid = dq.Guid;
        dq.Id = newId;
        save.Id = newId;
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
            BaseQuestTracker? tempTracker = data.GetTracker(null, in state);
            if (tempTracker != null)
            {
                pset.isValid = true;
                pset.PresetObj = preset;
                pset.Type = data.QuestType;
                cond.FlagValue = checked((short)preset.State.FlagValue.InsistValue());
                cond.Translation = tempTracker.GetDisplayString(true);
                cond.Key = preset.Key;
            }
            else
            {
                L.LogWarning("Failed to get daily tracker for " + data.QuestType);
            }
        }
    }
    private static void OnPlayerJoinedTeam(UCPlayer player)
    {
        if (Assets.find(_quests[index].Guid) is QuestAsset qa)
            player.Player.quests.sendTrackQuest(qa.id);
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
            BaseQuestTracker? tracker = DailyQuestDatas[i].GetTracker(player, in States[i]);
            if (tracker != null)
            {
                QuestManager.RegisterTracker(tracker);
                tracker.IsDailyQuest = true;
                trackers[i] = tracker;
                L.LogDebug("Registered " + tracker.QuestData.QuestType.ToString() + " tracker for " + player.CharacterName);
            }
            else
            {
                L.LogWarning("Failed to create tracker for daily quest " + DailyQuestDatas[i].QuestType);
            }
        }

        DailyQuestTracker tr = new DailyQuestTracker(player, trackers);
        ref DailyQuestSave save = ref _quests[index];
        if (Assets.find(save.Guid) is QuestAsset quest)
        {
            player.Player.quests.sendAddQuest(quest.id);
            L.LogDebug("Sent quest " + quest.name + " / " + quest.id.ToString() + " to " + player.CharacterName);
        }
        else
        {
            L.LogWarning("Couldn't find asset for " + save.Guid);
        }
        LoadSave(tr);
        for (int i = 0; i < DailyQuest.DAILY_QUEST_CONDITION_LENGTH; ++i)
        {
            BaseQuestTracker tr3 = tr.Trackers[i];
            if (tr3.Flag != 0)
                player.Player.quests.sendSetFlag(tr3.Flag, tr3.FlagValue);
        }
        if (DailyTrackers.TryGetValue(player.Steam64, out DailyQuestTracker tr2))
        {
            if (tr2 != null)
            {
                for (int i = 0; i < DailyQuest.DAILY_QUEST_CONDITION_LENGTH; i++)
                    QuestManager.DeregisterTracker(tr2.Trackers[i]);
            }
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
        tracker.Player?.SendString("Daily Quest Completed!");
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
        else
            L.LogWarning("Player " + tracker.Player.Steam64 + " is missing their entry in DailyTrackers dictionary!");
        if (tracker.Flag != 0)
            tracker.Player!.Player.quests.sendSetFlag(tracker.Flag, tracker.FlagValue);
        tracker.Player.SendString("<#e4a399>Daily Quest updated: <#cdcec0>" + tracker.GetDisplayString());
    }
    /// <summary>Runs every day, creates the daily quests for the day.</summary>
    public static void CreateNewDailyQuests()
    {
#if DEBUG
        using IDisposable profiler = ProfilingUtils.StartTracking();
#endif
        ref DailyQuestSave save = ref _quests[index];
        for (int i = 0; i < DailyQuest.DAILY_QUEST_CONDITION_LENGTH; ++i)
        {
            ref DailyQuestSave.Preset preset = ref save.Presets[i];

            EQuestType type = preset.Type;
            BaseQuestData? data = QuestManager.Quests.Find(x => x != null && x.QuestType == type);
            if (data != null)
            {
                DailyQuestDatas[i] = data;
                if (preset.PresetObj != null)
                {
                    States[i] = preset.PresetObj.State;
                }
                else
                {
                    States[i] = data.GetState();
                }
            }
        }
    }
    public static void ReplicateQuestChoices()
    {
        if (UCWarfare.CanUseNetCall)
            NetCalls.SendNextQuests.NetInvoke(_sendQuests, _quests[0].StartDate.ToUniversalTime());
        else
            L.Log("Scheduled to send " + _sendQuests.Length + " daily quests once the bot connects.", ConsoleColor.Magenta);
    }
    public static void SaveQuests()
    {
        using (FileStream stream = new FileStream(Path.Combine(QuestManager.QUEST_FOLDER, "daily_quests.json"), FileMode.Create, FileAccess.Write, FileShare.Read))
        {
            Utf8JsonWriter writer = new Utf8JsonWriter(stream, JsonEx.writerOptions);
            writer.WriteStartObject();
            writer.WritePropertyName("index");
            writer.WriteNumberValue(index);
            writer.WritePropertyName("sent_quests");
            writer.WriteBooleanValue(sentCurrent);
            writer.WritePropertyName("quest_schedule");
            writer.WriteStartArray();
            for (int i = 0; i < _quests.Length; ++i)
            {
                ref DailyQuestSave quest = ref _quests[i];
                writer.WriteStartObject();
                writer.WritePropertyName("start_time");
                writer.WriteStringValue(quest.StartDate.ToUniversalTime().ToString("s"));
                writer.WritePropertyName("asset_guid");
                writer.WriteStringValue(quest.Guid);
                writer.WritePropertyName("asset_id");
                writer.WriteNumberValue(quest.Id);
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
                    if (preset.PresetObj.RewardOverrides is not null && preset.PresetObj.RewardOverrides.Length > 0)
                    {
                        writer.WritePropertyName("rewards");
                        writer.WriteStartArray();
                        for (int k = 0; k < preset.PresetObj.RewardOverrides.Length; ++k)
                        {
                            writer.WriteStartObject();
                            preset.PresetObj.RewardOverrides[k].WriteJson(writer);
                            writer.WriteEndObject();
                        }
                        writer.WriteEndArray();
                    }
                    writer.WriteEndObject();
                }
                writer.WriteEndArray();
                writer.WriteEndObject();
            }
            writer.WriteEndArray();
            writer.WriteEndObject();
            writer.Flush();
            writer.Dispose();
        }
    }
    private static bool needsCreate = false;
    public static void ReadQuests()
    {
        string p = Path.Combine(QuestManager.QUEST_FOLDER, "daily_quests.json");
        if (!File.Exists(p))
        {
            needsCreate = true;
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
            Utf8JsonReader reader = new Utf8JsonReader(buffer, JsonEx.readerOptions);
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
                            case "sent_quests":
                                sentCurrent = reader.TokenType == JsonTokenType.True;
                                break;
                            case "quest_schedule":
                                if (reader.TokenType == JsonTokenType.StartArray)
                                {
                                    _quests = new DailyQuestSave[DailyQuest.DAILY_QUEST_LENGTH];
                                    _sendQuests = new DailyQuest[DailyQuest.DAILY_QUEST_LENGTH];
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
                                                    ref DailyQuest send = ref _sendQuests[i];
                                                    switch (prop)
                                                    {
                                                        case "start_time":
                                                            if (reader.TokenType == JsonTokenType.String)
                                                            {
                                                                string? v = reader.GetString();
                                                                if (v != null) 
                                                                    DateTime.TryParseExact(v, "s", Data.Locale, DateTimeStyles.AssumeUniversal, out save.StartDate);
                                                            }
                                                            break;
                                                        case "asset_guid":
                                                            if (reader.TokenType == JsonTokenType.String)
                                                            {
                                                                if (reader.TryGetGuid(out save.Guid))
                                                                    send.Guid = save.Guid;
                                                            }
                                                            break;
                                                        case "asset_id":
                                                            if (reader.TokenType == JsonTokenType.Number && reader.TryGetUInt16(out save.Id))
                                                                send.Id = save.Id;
                                                            break;
                                                        case "presets":
                                                            save.Presets = new DailyQuestSave.Preset[DailyQuest.DAILY_QUEST_CONDITION_LENGTH];
                                                            send.conditions = new DailyQuest.Condition[DailyQuest.DAILY_QUEST_CONDITION_LENGTH];
                                                            int j = -1;
                                                            int xp = 0;
                                                            int cred = 0;
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
                                                                                        ref DailyQuestSave.Preset preset = ref save.Presets[j];
                                                                                        ref DailyQuest.Condition cond = ref send.conditions[j];
                                                                                        if (v != null && Enum.TryParse(v, true, out EQuestType type))
                                                                                        {
                                                                                            preset.Type = type;
                                                                                            BaseQuestData? data = QuestManager.Quests.Find(x => x.QuestType == type);
                                                                                            if (data != null)
                                                                                            {
                                                                                                preset.PresetObj = data.ReadPreset(ref reader);
                                                                                                preset.isValid = true;
                                                                                                if (!sentCurrent)
                                                                                                {
                                                                                                    BaseQuestTracker? tempTracker = data.GetTracker(null, preset.PresetObj);
                                                                                                    if (tempTracker != null)
                                                                                                    {
                                                                                                        cond.FlagValue = checked((short)preset.PresetObj.State.FlagValue.InsistValue());
                                                                                                        cond.Translation = tempTracker.GetDisplayString(true);
                                                                                                        cond.Key = preset.PresetObj.Key;
                                                                                                        cond.FlagId = preset.PresetObj.Flag;
                                                                                                        for (int r = 0; r < tempTracker.Rewards.Length; ++r)
                                                                                                        {
                                                                                                            if (tempTracker.Rewards[r] is XPReward xpr)
                                                                                                                xp += xpr.XP;
                                                                                                            else if (tempTracker.Rewards[r] is CreditsReward cr)
                                                                                                                cred += cr.Credits;
                                                                                                        }
                                                                                                    }
                                                                                                    else
                                                                                                    {
                                                                                                        L.LogWarning("Unable to create tracker for " + preset.PresetObj.State.FlagValue + " (" + type.ToString() + ")");
                                                                                                    }
                                                                                                }
                                                                                            }
                                                                                            else
                                                                                            {
                                                                                                L.LogWarning("Unable to find quest data for type " + type);
                                                                                            }
                                                                                        }
                                                                                        else
                                                                                        {
                                                                                            L.LogWarning("Unknown quest type: " + v);
                                                                                        }
                                                                                        break;
                                                                                }
                                                                            }
                                                                        }
                                                                    }
                                                                }
                                                            }
                                                            send.XPReward = xp;
                                                            send.CreditsReward = cred;
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
    private static string GetDailySavePath(ulong steam64) => Path.Combine(Environment.CurrentDirectory, "Servers", Provider.serverID, "Players", steam64.ToString(Data.Locale) +
                                                             "_0", "Uncreated_S" + UCWarfare.Version.Major.ToString(Data.Locale));
    public static void SaveProgress(DailyQuestTracker tracker)
    {
#if DEBUG
        using IDisposable profiler = ProfilingUtils.StartTracking();
#endif
        L.Log("Saving trackers for " + tracker.Player.CharacterName);
        string path = GetDailySavePath(tracker.Player.Steam64);
        if (!Directory.Exists(path))
            Directory.CreateDirectory(path);
        path = Path.Combine(path, "daily_quest_progress.json");
        using (FileStream stream = new FileStream(path, FileMode.Create, FileAccess.Write, FileShare.Read))
        {
            Utf8JsonWriter writer = new Utf8JsonWriter(stream, JsonEx.writerOptions);
            writer.WriteStartObject();
            writer.WritePropertyName("guid");
            writer.WriteStringValue(_quests[index].Guid);
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
            writer.Flush();
            writer.Dispose();
        }
        L.LogDebug("Saved to " + path);
    }
    public static void LoadSave(DailyQuestTracker tracker)
    {
#if DEBUG
        using IDisposable profiler = ProfilingUtils.StartTracking();
#endif
        string path = Path.Combine(GetDailySavePath(tracker.Player.Steam64), "daily_quest_progress.json");
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
                    string prop = reader.GetString()!;
                    if (prop.Equals("guid", StringComparison.OrdinalIgnoreCase))
                    {
                        if (reader.Read() && reader.TokenType == JsonTokenType.String && reader.TryGetGuid(out Guid time))
                        {
                            if (time != _quests[index].Guid)
                                goto deleteFile; // expired progress file from another day, delete the file and load default values
                        }
                    }
                    else if (prop.Equals("daily_challenges", StringComparison.OrdinalIgnoreCase))
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
                                    prop = reader.GetString()!;
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
    public static void LoadAssets()
    {
        string p = Path.Combine(QuestManager.QUEST_FOLDER, "DailyQuests", DailyQuest.WORKSHOP_FILE_NAME) + Path.DirectorySeparatorChar;
        L.Log("Loading assets from \"" + p + "\"...", ConsoleColor.Magenta);
        if (!Directory.Exists(p))
        {
            L.LogError("Directory doesn't exist!");
        }
        else
        {
            Assets.load(p, _questModOrigin, true);
            L.Log("Assets loaded", ConsoleColor.Magenta);
            PrintQuests();
        }
    }

    public static class NetCalls
    {
        public static readonly NetCallRaw<DailyQuest[], DateTime> SendNextQuests = new NetCallRaw<DailyQuest[], DateTime>(1125, DailyQuest.ReadMany, null, DailyQuest.WriteMany, null);
        public static readonly NetCallRaw<Folder> AckNextQuestsUploaded = new NetCallRaw<Folder>(ReceiveQuestData, Folder.Read, Folder.Write, 65536);


        [NetCall(ENetCall.FROM_SERVER, 1126)]
        public static async Task ReceiveQuestData(MessageContext context, Folder folder)
        {
            try
            {
                string p = Path.Combine(QuestManager.QUEST_FOLDER, "DailyQuests") + Path.DirectorySeparatorChar;
                L.Log("Received mod folder: " + folder.name, ConsoleColor.Magenta);
                if (Directory.Exists(p))
                    Directory.Delete(p, true);
                folder.WriteToDisk(p);
                await UCWarfare.ToUpdate();
                LoadAssets();
                sentCurrent = true;
                PrintQuests();
                SaveQuests();
            }
            catch (Exception ex)
            {
                L.LogError("Error receiving quest mod data.");
                L.LogError(ex);
            }
        }
    }
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
    public Guid Guid;
    public ushort Id;

    public struct Preset
    {
        public bool isValid;
        public EQuestType Type;
        public IQuestPreset PresetObj;
    }
}