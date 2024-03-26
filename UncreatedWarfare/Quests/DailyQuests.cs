using HarmonyLib;
using SDG.Unturned;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.Json;
using System.Threading.Tasks;
using JetBrains.Annotations;
using Uncreated.Framework;
using Uncreated.Framework.Quests;
using Uncreated.Json;
using Uncreated.Networking;
using Uncreated.Warfare.Players;

namespace Uncreated.Warfare.Quests;

public static class DailyQuests
{
    private const ulong DailyQuestsWorkshopID = 2773379635ul;
    public static BaseQuestData[] DailyQuestDatas = new BaseQuestData[DailyQuest.DAILY_QUEST_CONDITION_LENGTH];
    public static IQuestState[] States = new IQuestState[DailyQuest.DAILY_QUEST_CONDITION_LENGTH];
    public static Dictionary<ulong, DailyQuestTracker> DailyTrackers = new Dictionary<ulong, DailyQuestTracker>();
    private static DailyQuestSave[] _quests = new DailyQuestSave[DailyQuest.DAILY_QUEST_LENGTH];
    private static DailyQuest[] _sendQuests = new DailyQuest[DailyQuest.DAILY_QUEST_LENGTH];
    private static DateTime _nextRefresh;
    private static int _index;
    private static bool _sendHrNotif;
    private static bool _hasRead;
    public static TimeSpan TimeLeftForQuests => DateTime.Now - _nextRefresh;
    public static void OnConnectedToServer()
    {
        if (_hasRead)
            ReplicateQuestChoices();
    }

    private static readonly AssetOrigin QuestModOrigin = new AssetOrigin
    {
        name = "Daily Quests",
        workshopFileId = DailyQuestsWorkshopID
    };
    public static void EarlyLoad()
    {
        MethodInfo? m = typeof(Provider).GetMethod("onDedicatedUGCInstalled", BindingFlags.NonPublic | BindingFlags.Static);
        if (m is not null)
            Harmony.Patches.Patcher.Patch(m,
                prefix: new HarmonyMethod(typeof(DailyQuests).GetMethod(nameof(OnRegisteredWorkshopID),
                    BindingFlags.Static | BindingFlags.NonPublic)));
        else L.LogWarning("Unable to patch Provider.onDedicatedUGCInstalled to register the quest mod!");
        m = typeof(PlayerQuests).GetMethod(nameof(PlayerQuests.ReceiveAbandonQuestRequest), BindingFlags.Instance | BindingFlags.Public);
        if (m is not null)
            Harmony.Patches.Patcher.Patch(m,
                prefix: new HarmonyMethod(typeof(DailyQuests).GetMethod(nameof(OnAbandonedRequestedQuest),
                    BindingFlags.Static | BindingFlags.NonPublic)));
        else L.LogWarning("Unable to patch PlayerQuests.ReceiveAbandonQuest to prevent abandoning daily missions!");
    }
    public static void Load()
    {
        ReadQuests();
        _hasRead = true;

        if (_needsCreate)
        {
            CreateNewModContent();
            _needsCreate = false;
        }
        else
        {
            ReplicateQuestChoices();
        }

        ref DailyQuestSave next = ref _quests[_index + 1];
        _nextRefresh = next.StartDate;
        Guid index2 = _quests[_index].Guid;
        Tick();
        if (index2 == _quests[_index].Guid)
        {
            CreateNewDailyQuests();
            for (int i = 0; i < PlayerManager.OnlinePlayers.Count; i++)
                RegisterDailyTrackers(PlayerManager.OnlinePlayers[i]);
        }

        Teams.TeamSelector.OnPlayerSelected += OnPlayerJoinedTeam;
    }
    private static void PrintQuests()
    {
        L.LogDebug("Daily Quests (current: " + _index + "):");
        using IDisposable indent = L.IndentLog(1);
        for (int i = 0; i < _quests.Length; i++)
        {
            ref DailyQuestSave save = ref _quests[i];
            if (Assets.find(save.Guid) is not QuestAsset asset)
                L.LogWarning("Cannot find asset for day " + i + "'s quest.");
            else
                L.LogDebug("Day " + i + ": " + asset.questName);
        }
    }
    private static void OnRegisteredWorkshopID()
    {
        Provider.registerServerUsingWorkshopFileId(DailyQuestsWorkshopID);
    }

    [UsedImplicitly]
    private static bool OnAbandonedRequestedQuest(Guid assetGuid)
    {
        return false;
    }
    internal static void CheckTrackQuestsOption(UCPlayer player)
    {
        if (player.Save.TrackQuests)
            TrackDailyQuest(player);
        else
            QuestManager.UntrackQuest(player);
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
                _index = 0;
                CreateNewModContent();
            }
            else
            {
                do
                {
                    _index++;
                    if (_index > DailyQuest.DAILY_QUEST_LENGTH / 2)
                    {
                        L.Log("Generating next week's Daily Quests.", ConsoleColor.Magenta);
                        _index = 0;
                        CreateNextModContent();
                    }

                    _nextRefresh = _quests[_index + 1].StartDate;
                } while (_nextRefresh <= now);
            }
            _sendHrNotif = false;
            CreateNewDailyQuests();

            if (_index != 0)
                SaveQuests();

            L.Log("New daily quests put into action: #" + _index + ", to be changed over at: " + _nextRefresh.ToString("f") + " UTC");
            Chat.Broadcast(T.DailyQuestsNewIndex, _nextRefresh);

            ref DailyQuestSave today = ref _quests[_index];
            QuestAsset? tasset = Assets.find<QuestAsset>(today.Guid);
            foreach (KeyValuePair<ulong, DailyQuestTracker> tracker in DailyTrackers)
            {
                for (int i = 0; i < DailyQuest.DAILY_QUEST_CONDITION_LENGTH; i++)
                {
                    QuestManager.DeregisterTracker(tracker.Value.Trackers[i]);
                }

                if (tasset is not null)
                    tracker.Value.Player.Player.quests.ServerRemoveQuest(tasset);
            }
            DailyTrackers.Clear();
            for (int i = 0; i < PlayerManager.OnlinePlayers.Count; i++)
            {
                RegisterDailyTrackers(PlayerManager.OnlinePlayers[i]);
            }
        }
        else if (!_sendHrNotif && (_nextRefresh - now).TotalHours <= 1d)
        {
            _sendHrNotif = true;
            Chat.Broadcast(T.DailyQuestsOneHourRemaining);
        }
    }
    public static void TrackDailyQuest(UCPlayer player)
    {
        ThreadUtil.assertIsGameThread();
        if (player.Save.TrackQuests && Assets.find(_quests[_index].Guid) is QuestAsset qa)
            player.ServerTrackQuest(qa);
    }
    public static void CreateNewModContent()
    {
        if (QuestManager.Quests.Count <= DailyQuest.DAILY_QUEST_CONDITION_LENGTH)
        {
            throw new Exception("Not enough quest types defined to create " + DailyQuest.DAILY_QUEST_CONDITION_LENGTH + " daily quests.");
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
            throw new Exception("Not enough quest types defined to create " + DailyQuest.DAILY_QUEST_CONDITION_LENGTH + " daily quests.");
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
        int xp = 0, cred = 0;
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
            BaseQuestTracker? tempTracker = data.GetTracker(null, state);
            if (tempTracker != null)
            {
                int val = preset.State.FlagValue.InsistValue();
                if (val < short.MinValue || val > short.MaxValue)
                {
                    L.LogError("Invalid flag value from " + tempTracker.GetType().FullDescription() + ": " + val + ".");
                    val = 1;
                }
                pset.IsValid = true;
                pset.PresetObj = preset;
                pset.Type = data.QuestType;
                cond.FlagId = preset.Flag;
                cond.FlagValue = checked((short)val);
                cond.Translation = tempTracker.GetDisplayString(true) ?? string.Empty;
                cond.Key = preset.Key;
                foreach (XPReward reward in tempTracker.Rewards.OfType<XPReward>())
                    xp += reward.XP;
                foreach (CreditsReward reward in tempTracker.Rewards.OfType<CreditsReward>())
                    cred += reward.Credits;
            }
            else
            {
                L.LogWarning("Failed to get daily tracker for " + data.QuestType);
            }
        }

        dq.XPReward = xp;
        dq.CreditsReward = cred;
    }
    private static void OnPlayerJoinedTeam(UCPlayer player)
    {
        TrackDailyQuest(player);
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
            BaseQuestTracker? tracker = DailyQuestDatas[i].GetTracker(player, States[i]);
            if (tracker != null)
            {
                QuestManager.RegisterTracker(tracker);
                tracker.IsDailyQuest = true;
                tracker.Flag = _quests[_index].Presets[i].PresetObj.Flag;
                trackers[i] = tracker;
                L.LogDebug("Registered " + tracker.QuestData.QuestType + " tracker for " + player.CharacterName);
            }
            else
            {
                L.LogWarning("Failed to create tracker for daily quest " + DailyQuestDatas[i].QuestType);
            }
        }

        DailyQuestTracker tr = new DailyQuestTracker(player, trackers);
        ref DailyQuestSave save = ref _quests[_index];
        for (int i = 0; i < _index; ++i)
        {
            ref DailyQuestSave save2 = ref _quests[i];
            if (Assets.find(save2.Guid) is QuestAsset quest2)
            {
                player.Player.quests.ServerRemoveQuest(quest2);
                L.LogDebug("Removing quest: " + quest2.name + " from " + player + ".");
            }
        }
        if (Assets.find(save.Guid) is QuestAsset quest)
        {
            QuestManager.TryAddQuest(player, quest);
            L.LogDebug("Sent quest " + quest.name + " / " + quest.id.ToString(Data.AdminLocale) + " to " + player.CharacterName);
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
            else
                L.LogWarning("Invalid flag or quest " + tr3.QuestData.QuestType + ": " + tr3.Flag);
            if (tr3.IsCompleted)
                QuestManager.DeregisterTracker(tr3);
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
        {
            L.Log($"[{tracker.Player!.Steam64}] Daily quest {tracker.QuestData.QuestType} completed: \"{Util.RemoveRichText(tracker.GetDisplayString())}\"", ConsoleColor.Cyan);
        }
        ToastMessage.QueueMessage(tracker.Player!, new ToastMessage(ToastMessageStyle.Large, new string[] { "Daily Quest Completed!", tracker.GetDisplayString(), string.Empty }));
        tracker.TryGiveRewards();
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
        if (tracker.QuestData.QuestType is not QuestType.TransportPlayers and not QuestType.DriveDistance and not QuestType.XPInGamemode)
            tracker.Player.SendString("<#e4a399>Daily Quest updated: <#cdcec0>" + tracker.GetDisplayString());
    }
    /// <summary>Runs every day, creates the daily quests for the day.</summary>
    public static void CreateNewDailyQuests()
    {
#if DEBUG
        using IDisposable profiler = ProfilingUtils.StartTracking();
#endif
        ref DailyQuestSave save = ref _quests[_index];
        for (int i = 0; i < DailyQuest.DAILY_QUEST_CONDITION_LENGTH; ++i)
        {
            ref DailyQuestSave.Preset preset = ref save.Presets[i];

            QuestType type = preset.Type;
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
        LoadAssets();
        bool needsSend = false;
        for (int i = 0; i < DailyQuest.DAILY_QUEST_LENGTH; ++i)
        {
            if (Assets.find(_quests[i].Guid) is not QuestAsset)
            {
                needsSend = true;
                break;
            }
        }
        if (needsSend)
        {
            if (UCWarfare.CanUseNetCall)
                NetCalls.SendNextQuests.NetInvoke(_sendQuests, _quests[0].StartDate.ToUniversalTime());
            else
                L.Log("Scheduled to send " + _sendQuests.Length + " daily quests once the bot connects.", ConsoleColor.Magenta);
        }
    }
    public static void SaveQuests()
    {
        using FileStream stream = new FileStream(Path.Combine(QuestManager.QUEST_FOLDER, "daily_quests.json"), FileMode.Create, FileAccess.Write, FileShare.Read);
        Utf8JsonWriter writer = new Utf8JsonWriter(stream, JsonEx.writerOptions);
        writer.WriteStartObject();
        writer.WritePropertyName("index");
        writer.WriteNumberValue(_index);
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
    private static bool _needsCreate;
    public static void ReadQuests()
    {
        string p = Path.Combine(QuestManager.QUEST_FOLDER, "daily_quests.json");
        if (!File.Exists(p))
        {
            _needsCreate = true;
            return;
        }

        using FileStream stream = new FileStream(p, FileMode.Open, FileAccess.Read, FileShare.Read);
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
                                reader.TryGetInt32(out _index);
                            }
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
                                                                DateTime.TryParseExact(v, "s", Data.AdminLocale, DateTimeStyles.AssumeUniversal, out save.StartDate);
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
                                                                                    if (v != null && Enum.TryParse(v, true, out QuestType type))
                                                                                    {
                                                                                        preset.Type = type;
                                                                                        BaseQuestData? data = QuestManager.Quests.Find(x => x.QuestType == type);
                                                                                        if (data != null)
                                                                                        {
                                                                                            preset.PresetObj = data.ReadPreset(ref reader);
                                                                                            preset.IsValid = true;
                                                                                            BaseQuestTracker? tempTracker = data.GetTracker(null, preset.PresetObj);
                                                                                            if (tempTracker != null)
                                                                                            {
                                                                                                cond.FlagValue = checked((short)preset.PresetObj.State.FlagValue.InsistValue());
                                                                                                cond.Translation = tempTracker.GetDisplayString(true) ?? string.Empty;
                                                                                                cond.Key = preset.PresetObj.Key;
                                                                                                cond.FlagId = preset.PresetObj.Flag;
                                                                                                L.LogDebug($"SendQuest filled: {cond.FlagValue}, {cond.Translation}, {cond.Key}, {cond.FlagId}");
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
    private static string GetDailySavePath(ulong steam64) => Path.Combine(Environment.CurrentDirectory, "Servers", Provider.serverID, "Players", steam64.ToString(Data.AdminLocale) +
                                                             "_0", "Uncreated_S" + UCWarfare.Version.Major.ToString(Data.AdminLocale));
    public static void SaveProgress(DailyQuestTracker tracker)
    {
#if DEBUG
        using IDisposable profiler = ProfilingUtils.StartTracking();
#endif
        L.LogDebug("Saving trackers for " + tracker.Player.CharacterName);
        string path = GetDailySavePath(tracker.Player.Steam64);
        if (!Directory.Exists(path))
            Directory.CreateDirectory(path);
        path = Path.Combine(path, "daily_quest_progress.json");
        using (FileStream stream = new FileStream(path, FileMode.Create, FileAccess.Write, FileShare.Read))
        {
            Utf8JsonWriter writer = new Utf8JsonWriter(stream, JsonEx.writerOptions);
            writer.WriteStartObject();
            writer.WritePropertyName("guid");
            writer.WriteStringValue(_quests[_index].Guid);
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
                            if (time != _quests[_index].Guid)
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
            string parent = Path.Combine(p, "NPCs", "Quests");
            for (int i = 0; i < DailyQuest.DAILY_QUEST_LENGTH; ++i)
            {
                string path = Path.Combine(parent, "DailyQuest" + i.ToString(CultureInfo.InvariantCulture), "DailyQuest" + i.ToString(CultureInfo.InvariantCulture) + ".dat");
                UCAssetManager.TryLoadAsset(path, QuestModOrigin);
            }

            UCAssetManager.SyncAssetsFromOrigin(QuestModOrigin);
            L.Log("Assets loaded", ConsoleColor.Magenta);
            PrintQuests();
        }
    }

    public static class NetCalls
    {
        public static readonly NetCallRaw<DailyQuest[], DateTime> SendNextQuests = new NetCallRaw<DailyQuest[], DateTime>(KnownNetMessage.SendNextQuests, DailyQuest.ReadMany, null, DailyQuest.WriteMany, null);
        public static readonly NetCallRaw<Folder> AckNextQuestsUploaded = new NetCallRaw<Folder>(ReceiveQuestData, Folder.Read, Folder.Write, 65536);


        [NetCall(NetCallOrigin.ServerOnly, KnownNetMessage.AckNextQuestsUploaded)]
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
        Player = player;
        Trackers = trackers;
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
        public bool IsValid;
        public QuestType Type;
        public IQuestPreset PresetObj;
    }
}