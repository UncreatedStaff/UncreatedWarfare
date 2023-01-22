using SDG.Unturned;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using Uncreated.Players;
using Uncreated.Warfare.Configuration;
using Uncreated.Warfare.Events.Players;
using Uncreated.Warfare.Quests;

namespace Uncreated.Warfare.Ranks;
public static class RankManager
{
    public static readonly Config<RankConfig> ConfigSave;
    public static RankConfig Config => ConfigSave.Data;
    private const uint DataVersion = 1;
    public static void Reload() => ConfigSave.Reload();
    static RankManager()
    {
        try
        {
            ConfigSave = new Config<RankConfig>(Data.Paths.PointsStorage, "rank_data.json", RankConfig.Read, RankConfig.Write);
        }
        catch (Exception ex)
        {
            L.LogError(ex);
        }
    }
    private static string GetSavePath(ulong steam64) => Path.DirectorySeparatorChar + Path.Combine("Players", steam64.ToString(Data.AdminLocale) + "_0",
        "Uncreated_S" + UCWarfare.Version.Major.ToString(Data.AdminLocale), "RankProgress.dat");
    public static void WriteRankData(UCPlayer player, RankStatus[] status)
    {
        string path = GetSavePath(player.Steam64);
        Block block = new Block();
        block.writeUInt32(DataVersion);
        block.writeInt32(status.Length);
        for (int i = 0; i < status.Length; i++)
        {
            block.writeInt32(status[i].Order);
            block.writeBoolean(status[i].IsCompelete);
            block.writeBooleanArray(status[i].Completions);
        }
        ServerSavedata.writeBlock(path, block);
    }
    public static bool IsLimited(this UCPlayer player, int order) => GetRankOrder(player) <= order;
    public static bool IsLimited(this UCPlayer player, ref RankData rank) => GetRankOrder(player) <= rank.Order;
    public static RankStatus[] ReadRankData(UCPlayer player)
    {
        string path = GetSavePath(player.Steam64);
        RankStatus[] statuses;
        bool t = !ServerSavedata.fileExists(path);
        int l;
    t:
        if (t)
        {
            l = Config.Ranks.Length;
            statuses = new RankStatus[l];
            l -= 1;
            for (; l >= 0; l--)
            {
                statuses[l] = new RankStatus(Config.Ranks[l].Order, l == 0, new bool[Config.Ranks[l].UnlockRequirements.Length]);
            }
            WriteRankData(player, statuses);
            return statuses;
        }
        Block? block = ServerSavedata.readBlock(path, 0);
        if (block == null)
        {
            L.LogWarning("Failed to read save file " + path);
            t = true;
            goto t;
        }
        if (Config.Ranks == null)
        {
            L.LogError("RANKS WERE NOT SET UP IN THIS CONFIG FILE!");
            return new RankStatus[0];
        }
        /*uint dataVersion =*/
        block.readUInt32();
        int len = block.readInt32();
        l = Config.Ranks.Length;
        statuses = new RankStatus[l];
        int i = 0;
        for (; i < len && i < l; i++)
        {
            int order = block.readInt32();
            bool isCompelete = block.readBoolean();
            bool[] unlkd = block.readBooleanArray();
            if (Config.Ranks[i].UnlockRequirements == null)
            {
                L.LogError("Failed to read unlock requirements of " + Config.Ranks[i].Order.ToString());
                continue;
            }
            int l1 = Config.Ranks[i].UnlockRequirements.Length;
            if (unlkd == null)
            {
                L.LogError("Failed to read boolean array!");
                unlkd = new bool[l1];
            }
            if (unlkd.Length != l1)
            {
                bool[] nba = new bool[l1];
                Buffer.BlockCopy(unlkd, 0, nba, 0, Math.Min(unlkd.Length, l1));
                unlkd = nba;
            }
            statuses[i] = new RankStatus(order, i == 0 || isCompelete, unlkd);
        }
        for (; i < l; i++)
        {
            statuses[i] = new RankStatus(Config.Ranks[i].Order, i == 0, new bool[Config.Ranks[i].UnlockRequirements.Length]);
        }
        WriteRankData(player, statuses);
        return statuses;
    }
    public static ref RankData GetRank(UCPlayer player, out bool success)
    {
        if (player == null || player.RankData == null)
        {
            success = false;
            return ref RankData.Nil;
        }
        for (int i = player.RankData.Length - 1; i >= 0; i--)
        {
            ref RankStatus data = ref player.RankData[i];
            if (data.IsCompelete)
            {
                if (i < Config.Ranks.Length && Config.Ranks[i].Order == data.Order)
                {
                    success = true;
                    return ref Config.Ranks[i];
                }
                else
                {
                    for (int j = 0; j < Config.Ranks.Length; j++)
                    {
                        if (data.Order == Config.Ranks[j].Order)
                        {
                            success = true;
                            return ref Config.Ranks[j];
                        }
                    }
                }
            }
        }
        success = false;
        return ref RankData.Nil;
    }
    public static RankData GetRank(UCPlayer player)
    {
        if (player == null || player.RankData == null) return default;
        for (int i = player.RankData.Length - 1; i >= 0; i--)
        {
            ref RankStatus data = ref player.RankData[i];
            if (data.IsCompelete)
            {
                if (i < Config.Ranks.Length && Config.Ranks[i].Order == data.Order)
                {
                    return Config.Ranks[i];
                }
                else
                {
                    for (int j = 0; j < Config.Ranks.Length; j++)
                    {
                        if (data.Order == Config.Ranks[j].Order)
                        {
                            return Config.Ranks[j];
                        }
                    }
                }
            }
        }
        return RankData.Nil;
    }
    public static ref RankData GetRank(int order, out bool success)
    {
        for (int i = Config.Ranks.Length - 1; i >= 0; i--)
        {
            ref RankData data = ref Config.Ranks[i];
            if (data.Order == order)
            {
                success = true;
                return ref data;
            }
        }
        success = false;
        return ref RankData.Nil;
    }
    public static RankData GetRank(int order)
    {
        for (int i = Config.Ranks.Length - 1; i >= 0; i--)
        {
            ref RankData data = ref Config.Ranks[i];
            if (data.Order == order)
                return data;
        }
        return RankData.Nil;
    }
    public static int GetRankOrder(UCPlayer player)
    {
        if (player == null || player.RankData == null) return -1;
        for (int i = player.RankData.Length - 1; i >= 0; i--)
        {
            ref RankStatus data = ref player.RankData[i];
            if (data.IsCompelete)
            {
                if (i < Config.Ranks.Length && Config.Ranks[i].Order == data.Order)
                {
                    return Config.Ranks[i].Order;
                }
                else
                {
                    for (int j = 0; j < Config.Ranks.Length; j++)
                    {
                        if (data.Order == Config.Ranks[j].Order)
                        {
                            return Config.Ranks[j].Order;
                        }
                    }
                }
            }
        }
        return -1;
    }
    public static int GetRankIndex(UCPlayer player)
    {
        if (player == null || player.RankData == null) return -1;
        for (int i = player.RankData.Length - 1; i >= 0; i--)
        {
            ref RankStatus data = ref player.RankData[i];
            if (data.IsCompelete)
            {
                if (i < Config.Ranks.Length && Config.Ranks[i].Order == data.Order)
                {
                    return i;
                }
                else
                {
                    for (int j = 0; j < Config.Ranks.Length; j++)
                    {
                        if (data.Order == Config.Ranks[j].Order)
                        {
                            return j;
                        }
                    }
                }
            }
        }
        return -1;
    }
    public static void OnPlayerJoin(UCPlayer player)
    {
        player.RankData ??= ReadRankData(player);
        int index = GetRankIndex(player);
        if (index != -1 && index < Config.Ranks.Length - 1)
        {
            ref RankData nextRank = ref Config.Ranks[index + 1];
            if (Assets.find(nextRank.QuestID) is QuestAsset quest)
            {
                player.ServerTrackQuest(quest);
            }
            for (int i = 0; i < nextRank.UnlockRequirements.Length; i++)
            {
                if (!player.RankData[index + 1].Completions[i])
                {
                    BaseQuestTracker? tracker = QuestManager.CreateTracker(player, nextRank.UnlockRequirements[i]);
                    if (tracker == null)
                        L.LogDebug("Failed to create tracker " + (i + 1) + " for " + player.Steam64);
                    else
                        L.LogDebug(tracker.PresetKey + (i + 1).ToString() + " created for " + player.Steam64);
                }
            }
        }
    }
    public static bool SkipToRank(UCPlayer player, int order)
    {
        player.RankData ??= ReadRankData(player);
        int index = GetRankIndex(player);
        if (index < 0)
            index = 0;
        if (index > Config.Ranks.Length - 2)
            return false;
        ref RankData data = ref Config.Ranks[index];
        if (data.Order <= order)
            return false;
        data = ref Config.Ranks[index + 1];
        ref RankStatus status = ref player.RankData[index + 1];

        ClearRank(player, in data);

        for (int i = 0; i < Config.Ranks.Length; ++i)
        {
            data = ref Config.Ranks[i];
            if (data.Order <= order && player.RankData.Length > i)
            {
                status = ref player.RankData[i];
                if (!status.IsCompelete)
                {
                    for (int j = 0; j < status.Completions.Length; ++j)
                        status.Completions[j] = true;
                    status.IsCompelete = true;
                }
            }
        }
        WriteRankData(player, player.RankData);

        OnPlayerJoin(player);
        return true;
    }
    public static void ResetRanks(UCPlayer player)
    {
        int l = Config.Ranks.Length;
        if (player.RankData is not null)
        {
            int index = GetRankIndex(player);
            if (player.RankData.Length - 1 > index)
            {
                ref RankData data = ref Config.Ranks[index + 1];
                ClearRank(player, in data);
            }
        }
        RankStatus[] statuses = new RankStatus[l];
        --l;
        for (; l >= 0; --l)
        {
            statuses[l] = new RankStatus(Config.Ranks[l].Order, l == 0, new bool[Config.Ranks[l].UnlockRequirements.Length]);
        }
        WriteRankData(player, statuses);
        player.RankData = statuses;
        OnPlayerJoin(player);
    }

    private static void ClearRank(UCPlayer player, in RankData data)
    {
        for (int i = 0; i < QuestManager.RegisteredTrackers.Count; ++i)
        {
            if (QuestManager.RegisteredTrackers[i].Player == player)
            {
                BaseQuestTracker tr = QuestManager.RegisteredTrackers[i];
                for (int j = 0; j < data.UnlockRequirements.Length; ++j)
                {
                    if (data.UnlockRequirements[j] == tr.PresetKey)
                    {
                        QuestManager.DeregisterTracker(tr);
                        player.Player.quests.sendRemoveFlag(tr.Flag);
                        goto n;
                    }
                }
            }
            n:;
        }

        if (Assets.find(data.QuestID) is QuestAsset qa)
            player.Player.quests.ServerRemoveQuest(qa);
    }

    public static bool OnQuestCompleted(QuestCompleted e)
    {
        e.Player.RankData ??= ReadRankData(e.Player);
        int index = GetRankIndex(e.Player);
        if (index != -1 && index < Config.Ranks.Length - 1)
        {
            ref RankData nextRank = ref Config.Ranks[index + 1];
            ref RankStatus status = ref e.Player.RankData[index + 1];
            bool write = false;
            for (int i = 0; i < nextRank.UnlockRequirements.Length; i++)
            {
                if (nextRank.UnlockRequirements[i] == e.PresetKey)
                {
                    status.Completions[i] = true;
                    L.LogDebug("Finished rank requirement " + i.ToString());
                    write = true;
                    break;
                }
            }
            if (!write) return false;
            bool value = true;
            for (int i = 0; i < status.Completions.Length; i++)
                value &= status.Completions[i];
            if (value)
            {
                status.IsCompelete = true;
                L.LogDebug("Finished rank " + nextRank.GetName(0));
                WriteRankData(e.Player, e.Player.RankData);
                if (Assets.find(nextRank.QuestID) is QuestAsset quest)
                {
                    e.Player.Player.quests.ServerRemoveQuest(quest);
                    ToastMessage.QueueMessage(e.Player, new ToastMessage("Quest complete: " + quest.questName, ToastMessageSeverity.Big));
                    OnPlayerJoin(e.Player);
                }
            }
            return true;
        }
        return false;
    }
}

public class RankConfig : JSONConfigData
{
    public RankData[] Ranks;
    public override void SetDefaults()
    {
        Ranks = new RankData[]
        {
            new RankData(0, "Recruit", "Rec", "common", Guid.Empty),
            new RankData(1, "Private", "Pvt", "common", "4e9d5956380a4e0a967facfab34b56aa",
                "2452e924-9feb-47c3-9bcc-5429618f424c"),
            new RankData(2, "Private 1st Class", "Pfc", "uncommon", "72474bb9edba4e4daa4214aed6461909",
                "5a9f831f-619d-46f1-8eb6-fc202d5d2c83", "1cb2ac47-b29c-43df-9871-ff8190c1f7be", "38421559-cfcd-4566-bb61-4ebbb1c43c80"),
            new RankData(3, "Corporal", "Col", "uncommon", "b7675410ed0143e58492d474204cc1f3",
                "3e3ffa93-d819-41df-b175-e7f0bd2316ce", "3eb3f8c2-bb01-4ae8-b7b7-a8b26dda5b4b", "72c64d0a-0c2d-4e5e-807f-ec83c84e242c"),
            new RankData(4, "Specialist", "Spec", "uncommon", "52f18bad0f3c40a1b64a4861720fde8f",
                "ebf8632f-b952-4f2c-9a89-f8709ad530a8", "8ac1432b-9d9e-41c3-bf45-e2d2d2662eb9", "d5df8cb1-1b88-471f-9db5-1fbce36cad1f"),
            new RankData(5, "Sergeant", "Sgt", "rare", "52f18bad0f3c40a1b64a4861720fde8f",
                "cec9082f-1eb6-4867-ab7c-569651429121", "cc5bcb0f-081b-4fe3-9f08-464a0b5f458f", "697389bb-428d-42e8-80ae-9dce2d4eebb7"),
            new RankData(6, "Staff Sergeant", "Ssg", "rare", "6ca6d44bc07e4a4d98653dadf30be5a1",
                "39a1fb42-f797-4190-a86c-147675ccd800", "2d23a366-fbcf-4847-a599-96c4f60e530c", "064f08a2-182d-4dae-ab70-bf4f28669c66"),
            new RankData(7, "Sergeant 1st Class", "Sfc", "epic", "0ac0318ac6064a2ab30f22e61769f21e",
                "55c7e483-79f4-4b72-9b16-cd0f24c10844", "b5fc53f9-6184-4233-b683-cd141d14d892", "0600d9aa-9f7c-413f-959c-ab25b2f4c165",
                "8fdf2b79-52a0-4a65-81ef-d3df0b8bf6e3"),
            new RankData(8, "Warrant Officer", "W.O", "epic", "5730fa43425c48759ea31138572e575f",
                "e2607e1f-2781-46fe-b53d-c13dd9921595", "d077f440-29e0-4f91-9406-f3050c44fadf", "edf07ac7-6e04-4167-9cb5-f3240d1e0ab8",
                "d28248e6-3b1c-437b-82eb-bfd1784542d1", "2c3c5834-1947-4436-b986-b1e4e7180087"),
            new RankData(9, "Captain", "C.W.O", "legendary", "8e8d22f179554de49281cb335af40256",
                "408fdcc4-fb1c-4651-a698-8a223558158a"),
            new RankData(10, "Major", "Maj", "legendary", "a90094198a014badbace681a0a0ef296",
                "4d3e027e-1154-409b-84be-a83d72f11be1"),
            new RankData(11, "Lieutenant", "Lt", "legendary", "a202d2fdf2dc4fc28fe66ac8a4bc9bdc",
                "b7ef3c8f-5769-4368-932d-3823bde659a1"),
            new RankData(12, "Colonal", "Col", "legendary", "ebef18e59dc04eb29bcc47e8e9facce0",
                "45c35294-4a44-45a0-b254-a3e7ae5487a6"),
            new RankData(13, "General", "Gen", "mythical", "5d4eae59186a4ff1a2d55836cb5012c7",
                "c9b209e0-2b4f-41d4-8044-3ffe4a234004"),
        };
    }
    public static RankConfig Read(ref Utf8JsonReader reader)
    {
        RankConfig config = new RankConfig();
        while (reader.Read())
        {
            if (reader.TokenType == JsonTokenType.EndObject) return config;
            if (reader.TokenType == JsonTokenType.PropertyName)
            {
                string? prop = reader.GetString();
                if (reader.Read() && prop != null)
                {
                    if (prop.Equals("Ranks", StringComparison.OrdinalIgnoreCase))
                    {
                        List<RankData> datas = new List<RankData>(16);
                        while (reader.Read() && reader.TokenType != JsonTokenType.EndArray)
                        {
                            datas.Add(RankData.Read(ref reader));
                        }
                        config.Ranks = datas.ToArray();
                    }
                }
            }
        }
        return config;
    }
    public static void Write(RankConfig config, Utf8JsonWriter writer)
    {
        writer.WriteStartObject();

        writer.WritePropertyName("Ranks");
        writer.WriteStartArray();
        for (int i = 0; i < config.Ranks.Length; i++)
        {
            ref RankData data = ref config.Ranks[i];
            writer.WriteStartObject();
            data.Write(writer);
            writer.WriteEndObject();
        }
        writer.WriteEndArray();

        writer.WriteEndObject();
    }
}
public record struct RankStatus(int Order, bool IsCompelete, bool[] Completions)
{
    public override string ToString() => $"Rank {Order}, {(IsCompelete ? "COMPLETE" : "INCOMPLETE")}. Quests completed: {Completions.Count(x => x)}/{Completions.Length}.";
}
public readonly struct RankData : IComparable<RankData>, ITranslationArgument
{
    public static RankData Nil = new RankData(-1);
    public readonly Guid QuestID;
    public readonly Guid[] UnlockRequirements;
    public readonly int Order;
    public readonly string Color;
    public readonly Dictionary<string, string> NameTranslations;
    public readonly Dictionary<string, string> AbbreviationTranslations;
    private RankData(int order)
    {
        this.Order = order;
        this.NameTranslations = null!;
        this.AbbreviationTranslations = null!;
        this.Color = UCWarfare.GetColorHex("default");
        this.UnlockRequirements = null!;
        this.QuestID = default;
    }
    public RankData(int order, Dictionary<string, string> names, Dictionary<string, string> abbreviations, string color, Guid questID, params Guid[] unlockRequirements)
    {
        this.Color = F.FilterRarityToHex(color);
        this.Order = order;
        this.NameTranslations = names;
        this.AbbreviationTranslations = abbreviations;
        this.QuestID = questID;
        this.UnlockRequirements = unlockRequirements;
    }
    public RankData(int order, string name, string abbreviation, string color, Guid questID, params Guid[] unlockRequirements) :
        this(order, new Dictionary<string, string>(1) { { L.Default, name } }, new Dictionary<string, string>(1) { { L.Default, abbreviation } },
            color, questID, unlockRequirements)
    { }
    public RankData(int order, string name, string abbreviation, string color, string questID, params string[] unlockRequirements) :
        this(order, name, abbreviation, color, Guid.TryParse(questID, out Guid guid) ? guid : Guid.Empty, ToGuidArray(unlockRequirements))
    { }
    private static Guid[] ToGuidArray(string[] strings)
    {
        Guid[] res = new Guid[strings.Length];
        for (int i = 0; i < strings.Length; i++)
            res[i] = Guid.TryParse(strings[i], out Guid guid) ? guid : Guid.Empty;
        return res;
    }
    public string GetName(ulong player)
    {
        if (!Data.Languages.TryGetValue(player, out string lang))
            lang = L.Default;
        if (NameTranslations == null) return "L" + Order.ToString(Localization.GetLocale(lang));
        if (NameTranslations.TryGetValue(lang, out string rtn) || (!lang.Equals(L.Default, StringComparison.Ordinal) && NameTranslations.TryGetValue(L.Default, out rtn)))
            return rtn;
        return NameTranslations.Values.FirstOrDefault() ?? ("L" + Order.ToString(Localization.GetLocale(lang)));
    }
    public string GetName(string lang)
    {
        lang ??= L.Default;
        if (NameTranslations == null) return "L" + Order.ToString(Localization.GetLocale(lang));
        if (NameTranslations.TryGetValue(lang, out string rtn) || (!lang.Equals(L.Default, StringComparison.Ordinal) && NameTranslations.TryGetValue(L.Default, out rtn)))
            return rtn;
        return NameTranslations.Values.FirstOrDefault() ?? ("L" + Order.ToString(Localization.GetLocale(lang)));
    }
    public string ColorizedName(string lang) => "<color=#" + Color + ">" + GetName(lang) + "</color>";
    public string ColorizedName(ulong player) => "<color=#" + Color + ">" + GetName(player) + "</color>";
    public string ColorizedAbbreviation(string lang) => "<color=#" + Color + ">" + GetAbbreviation(lang) + ".</color>";
    public string ColorizedAbbreviation(ulong player) => "<color=#" + Color + ">" + GetAbbreviation(player) + ".</color>";
    public string GetAbbreviation(ulong player)
    {
        if (!Data.Languages.TryGetValue(player, out string lang))
            lang = L.Default;
        if (AbbreviationTranslations == null) return "L" + Order.ToString(Localization.GetLocale(lang));
        if (AbbreviationTranslations.TryGetValue(lang, out string rtn) || (!lang.Equals(L.Default, StringComparison.Ordinal) && AbbreviationTranslations.TryGetValue(L.Default, out rtn)))
            return rtn;
        return AbbreviationTranslations.Values.FirstOrDefault() ?? ("L" + Order.ToString(Localization.GetLocale(lang)));
    }
    public string GetAbbreviation(string lang)
    {
        lang ??= L.Default;
        if (AbbreviationTranslations == null) return "L" + Order.ToString(Localization.GetLocale(lang));
        if (AbbreviationTranslations.TryGetValue(lang, out string rtn) || (!lang.Equals(L.Default, StringComparison.Ordinal) && AbbreviationTranslations.TryGetValue(L.Default, out rtn)))
            return rtn;
        return AbbreviationTranslations.Values.FirstOrDefault() ?? ("L" + Order.ToString(Localization.GetLocale(lang)));
    }
    public int CompareTo(RankData other) => Order.CompareTo(other.Order);
    public override bool Equals(object? obj) => obj is RankData data && Order == data.Order;
    public bool Equals(ref RankData data) => Order == data.Order;
    public override int GetHashCode() => Order;
    public static bool operator ==(RankData a, RankData b) => a.Order == b.Order;
    public static bool operator !=(RankData a, RankData b) => a.Order != b.Order;
    public static RankData Read(ref Utf8JsonReader reader)
    {
        Guid questid = default;
        List<Guid>? unlockrequirements = null;
        int order = -1;
        string? color = null;
        Dictionary<string, string>? names = null;
        Dictionary<string, string>? abbreviations = null;
        while (reader.TokenType == JsonTokenType.PropertyName || (reader.Read() && reader.TokenType == JsonTokenType.PropertyName))
        {
            string? prop = reader.GetString();
            if (reader.Read() && prop != null)
            {
                switch (prop)
                {
                    case "QuestID":
                        reader.TryGetGuid(out questid);
                        break;
                    case "UnlockRequirements":
                        if (reader.TokenType == JsonTokenType.StartArray)
                        {
                            unlockrequirements = new List<Guid>(5);
                            while (reader.Read() && reader.TokenType != JsonTokenType.EndArray)
                            {
                                if (reader.TokenType == JsonTokenType.String && reader.TryGetGuid(out Guid guid))
                                    unlockrequirements.Add(guid);
                            }
                        }
                        break;
                    case "Order":
                        reader.TryGetInt32(out order);
                        break;
                    case "Color":
                        color = reader.GetString();
                        break;
                    case "NameTranslations":
                        if (reader.TokenType == JsonTokenType.StartObject)
                        {
                            names = new Dictionary<string, string>(1);
                            while (reader.Read() && reader.TokenType == JsonTokenType.PropertyName)
                            {
                                string? key = reader.GetString();
                                if (key != null && reader.Read() && reader.TokenType == JsonTokenType.String)
                                {
                                    string? value = reader.GetString();
                                    if (value != null)
                                    {
                                        if (names.ContainsKey(key))
                                            names[key] = value;
                                        else
                                            names.Add(key, value);
                                    }
                                }
                            }
                        }
                        break;
                    case "AbbreviationTranslations":
                        if (reader.TokenType == JsonTokenType.StartObject)
                        {
                            abbreviations = new Dictionary<string, string>(1);
                            while (reader.Read() && reader.TokenType == JsonTokenType.PropertyName)
                            {
                                string? key = reader.GetString();
                                if (key != null && reader.Read() && reader.TokenType == JsonTokenType.String)
                                {
                                    string? value = reader.GetString();
                                    if (value != null)
                                    {
                                        if (abbreviations.ContainsKey(key))
                                            abbreviations[key] = value;
                                        else
                                            abbreviations.Add(key, value);
                                    }
                                }
                            }
                        }
                        break;
                }
            }
        }
        if (order == -1)
        {
            L.LogWarning("Failed to read rank data, unable to find property \"Order\"");
            return default;
        }
        else
        {
            return new RankData(order, names ?? new Dictionary<string, string>(0), abbreviations ?? new Dictionary<string, string>(0),
                color ?? UCWarfare.GetColorHex("default"), questid, unlockrequirements == null ? Array.Empty<Guid>() : unlockrequirements.ToArray());
        }
    }
    public void Write(Utf8JsonWriter writer)
    {
        writer.WriteString("QuestID", QuestID);
        writer.WritePropertyName("UnlockRequirements");
        if (UnlockRequirements == null)
            writer.WriteNullValue();
        else
        {
            writer.WriteStartArray();
            for (int i = 0; i < UnlockRequirements.Length; i++)
                writer.WriteStringValue(UnlockRequirements[i]);
            writer.WriteEndArray();
        }
        writer.WriteNumber("Order", Order);
        writer.WriteString("Color", Color);
        writer.WritePropertyName("NameTranslations");
        if (NameTranslations == null) writer.WriteNullValue();
        else
        {
            writer.WriteStartObject();
            foreach (KeyValuePair<string, string> translation in NameTranslations)
            {
                writer.WritePropertyName(translation.Key);
                writer.WriteStringValue(translation.Value);
            }
            writer.WriteEndObject();
        }
        writer.WritePropertyName("AbbreviationTranslations");
        if (AbbreviationTranslations == null) writer.WriteNullValue();
        else
        {
            writer.WriteStartObject();
            foreach (KeyValuePair<string, string> translation in AbbreviationTranslations)
            {
                writer.WritePropertyName(translation.Key);
                writer.WriteStringValue(translation.Value);
            }
            writer.WriteEndObject();
        }
    }

    [FormatDisplay("Rank Name")]
    public const string FormatName = "n";
    [FormatDisplay("Colored Rank Name")]
    public const string FormatColorName = "cn";
    [FormatDisplay("Rank Abbreviation")]
    public const string FormatAbbreviation = "a";
    [FormatDisplay("Colored Rank Abbreviation")]
    public const string FormatColorAbbreviation = "ca";
    [FormatDisplay("Order")]
    public const string FormatOrder = "o";
    [FormatDisplay("Colored Order")]
    public const string FormatColorOrder = "co";
    [FormatDisplay("Order with L-Prefix")]
    public const string FormatOrderLevel = "lo";
    [FormatDisplay("Colored Order with L-Prefix")]
    public const string FormatColorOrderLevel = "lco";
    public string Translate(string language, string? format, UCPlayer? target, ref TranslationFlags flags)
    {
        if (format is not null && !format.Equals(FormatName, StringComparison.Ordinal))
        {
            if (format.Equals(FormatColorName, StringComparison.Ordinal))
                return Localization.Colorize(Color, GetName(language), flags);
            if (format.Equals(FormatAbbreviation, StringComparison.Ordinal))
                return GetAbbreviation(language);
            if (format.Equals(FormatColorAbbreviation, StringComparison.Ordinal))
                return Localization.Colorize(Color, GetAbbreviation(language), flags);
            if (format.Equals(FormatOrder, StringComparison.Ordinal))
                return Order.ToString(Localization.GetLocale(language));
            if (format.Equals(FormatOrder, StringComparison.Ordinal))
                return Localization.Colorize(Color, Order.ToString(Localization.GetLocale(language)), flags);
            if (format.Equals(FormatOrderLevel, StringComparison.Ordinal))
                return "L " + Order.ToString(Localization.GetLocale(language));
            if (format.Equals(FormatColorOrderLevel, StringComparison.Ordinal))
                return "L " + Localization.Colorize(Color, Order.ToString(Localization.GetLocale(language)), flags);
        }

        return GetName(language);
    }
}