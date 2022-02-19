using SDG.Unturned;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Uncreated.Warfare.Quests;

namespace Uncreated.Warfare.Ranks;
public static class RankManager
{
    public static Config<RankConfig> ConfigSave = new Config<RankConfig>(Data.PointsStorage, "rank_data.json");
    public static RankConfig Config => ConfigSave.data;
    private const uint DATA_VERSION = 1;
    public static void Reload() => ConfigSave.Reload();

    private static string GetSavePath(ulong steam64) => "\\Players\\" + steam64.ToString(Data.Locale) + "_0\\Uncreated_S" + 
                                                        UCWarfare.Version.Major.ToString(Data.Locale) + "\\RankProgress.dat";
    public static void WriteRankData(UCPlayer player, RankStatus[] status)
    {
        string path = GetSavePath(player.Steam64);
        Block block = new Block();
        block.writeUInt32(DATA_VERSION);
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
        int l;
        if (!ServerSavedata.fileExists(path))
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
        Block block = ServerSavedata.readBlock(path, 0);
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
            int l1 = Config.Ranks[i].UnlockRequirements.Length;
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
    public static int GetRankOrder(UCPlayer player)
    {
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
        player.RankData = ReadRankData(player);
        int index = GetRankIndex(player);
        if (index != -1 && index < Config.Ranks.Length - 1)
        {
            ref RankData nextRank = ref Config.Ranks[index + 1];
            if (Assets.find(nextRank.QuestID) is QuestAsset quest)
            {
                player.Player.quests.sendAddQuest(quest.id);
                //player.Player.quests.sendTrackQuest(quest.id);
            }
            for (int i = 0; i < nextRank.UnlockRequirements.Length; i++)
            {
                if (!player.RankData[index + 1].Completions[i])
                    QuestManager.CreateTracker(player, nextRank.UnlockRequirements[i]);
            }
        }
    }
    public static bool OnQuestCompleted(UCPlayer player, Guid key)
    {
        player.RankData = ReadRankData(player);
        int index = GetRankIndex(player);
        if (index != -1 && index < Config.Ranks.Length - 1)
        {
            ref RankData nextRank = ref Config.Ranks[index + 1];
            ref RankStatus status = ref player.RankData[index + 1];
            bool write = false;
            for (int i = 0; i < nextRank.UnlockRequirements.Length; i++)
            {
                if (nextRank.UnlockRequirements[i] == key)
                {
                    status.Completions[i] = true;
                    // TODO: popup
                    L.Log("Finished rank requirement " + i.ToString());
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
                L.Log("Finished rank " + nextRank.GetName(0));
                if (Assets.find(nextRank.QuestID) is QuestAsset quest)
                {
                    player.Player.quests.sendRemoveQuest(quest.id);
                    OnPlayerJoin(player);
                }
            }
            WriteRankData(player, player.RankData);
            return true;
        }
        return false;
    }
}

public class RankConfig : ConfigData
{
    public RankData[] Ranks;
    public override void SetDefaults()
    {
        Ranks = new RankData[]
        {
            new RankData(0, "Recruit", "Rec", Guid.Empty),
            new RankData(1, "Private", "Pvt", "4e9d5956380a4e0a967facfab34b56aa",
                "2452e924-9feb-47c3-9bcc-5429618f424c"),
            new RankData(2, "Private 1st Class", "Pfc", "72474bb9edba4e4daa4214aed6461909",
                "5a9f831f-619d-46f1-8eb6-fc202d5d2c83", "1cb2ac47-b29c-43df-9871-ff8190c1f7be", "38421559-cfcd-4566-bb61-4ebbb1c43c80"),
            new RankData(3, "Corporal", "Col", "b7675410ed0143e58492d474204cc1f3",
                "3e3ffa93-d819-41df-b175-e7f0bd2316ce", "3eb3f8c2-bb01-4ae8-b7b7-a8b26dda5b4b", "72c64d0a-0c2d-4e5e-807f-ec83c84e242c"),
            new RankData(4, "Specialist", "Spec", "52f18bad0f3c40a1b64a4861720fde8f",
                "ebf8632f-b952-4f2c-9a89-f8709ad530a8", "8ac1432b-9d9e-41c3-bf45-e2d2d2662eb9", "d5df8cb1-1b88-471f-9db5-1fbce36cad1f"),
            new RankData(5, "Sergeant", "Sgt", "52f18bad0f3c40a1b64a4861720fde8f",
                "cec9082f-1eb6-4867-ab7c-569651429121", "cc5bcb0f-081b-4fe3-9f08-464a0b5f458f", "697389bb-428d-42e8-80ae-9dce2d4eebb7"),
            new RankData(6, "Staff Sergeant", "Ssg", "6ca6d44bc07e4a4d98653dadf30be5a1",
                "39a1fb42-f797-4190-a86c-147675ccd800", "2d23a366-fbcf-4847-a599-96c4f60e530c", "064f08a2-182d-4dae-ab70-bf4f28669c66"),
            new RankData(7, "Sergeant 1st Class", "Sfc", "0ac0318ac6064a2ab30f22e61769f21e",
                "55c7e483-79f4-4b72-9b16-cd0f24c10844", "b5fc53f9-6184-4233-b683-cd141d14d892", "0600d9aa-9f7c-413f-959c-ab25b2f4c165", 
                "8fdf2b79-52a0-4a65-81ef-d3df0b8bf6e3"),
            new RankData(8, "Warrant Officer", "W.O", "5730fa43425c48759ea31138572e575f",
                "e2607e1f-2781-46fe-b53d-c13dd9921595", "d077f440-29e0-4f91-9406-f3050c44fadf", "edf07ac7-6e04-4167-9cb5-f3240d1e0ab8",
                "d28248e6-3b1c-437b-82eb-bfd1784542d1", "2c3c5834-1947-4436-b986-b1e4e7180087"),
            new RankData(9, "Captain", "C.W.O", "8e8d22f179554de49281cb335af40256",
                "408fdcc4-fb1c-4651-a698-8a223558158a"),
            new RankData(10, "Major", "Maj", "a90094198a014badbace681a0a0ef296",
                "4d3e027e-1154-409b-84be-a83d72f11be1"),
            new RankData(11, "Lieutenant", "Lt", "a202d2fdf2dc4fc28fe66ac8a4bc9bdc",
                "b7ef3c8f-5769-4368-932d-3823bde659a1"),
            new RankData(12, "Colonal", "Col", "ebef18e59dc04eb29bcc47e8e9facce0",
                "45c35294-4a44-45a0-b254-a3e7ae5487a6"),
            new RankData(13, "General", "Gen", "5d4eae59186a4ff1a2d55836cb5012c7",
                "c9b209e0-2b4f-41d4-8044-3ffe4a234004"),
        };
    }
}
public record struct RankStatus(int Order, bool IsCompelete, bool[] Completions)
{
    public override string ToString() => $"Rank {Order}, {(IsCompelete ? "COMPLETE" : "INCOMPLETE")}. Quests completed: {Completions.Count(x => x)}/{Completions.Length}.";
}

public struct RankData
{
    public static RankData Nil = new RankData()
    {
        Order = -1
    };
    public Guid QuestID;
    public Guid[] UnlockRequirements;
    public int Order;
    public Dictionary<string, string> NameTranslations;
    public Dictionary<string, string> AbbreviationTranslations;
    public RankData(int order, string name, string abbreviation, Guid questID, params Guid[] unlockRequirements)
    {
        this.Order = order;
        this.NameTranslations = new Dictionary<string, string>(1)
        {
            { JSONMethods.DEFAULT_LANGUAGE, name }
        };
        this.AbbreviationTranslations = new Dictionary<string, string>(1)
        {
            { JSONMethods.DEFAULT_LANGUAGE, abbreviation }
        };
        this.QuestID = questID;
        this.UnlockRequirements = unlockRequirements;
    }
    public RankData(int order, string name, string abbreviation, string questID, params string[] unlockRequirements) : 
        this(order, name, abbreviation, Guid.TryParse(questID, out Guid guid) ? guid : Guid.Empty, ToGuidArray(unlockRequirements)) { }
    private static Guid[] ToGuidArray(string[] strings)
    {
        Guid[] res = new Guid[strings.Length];
        for (int i = 0; i < strings.Length; i++)
            res[i] = Guid.TryParse(strings[i], out Guid guid) ? guid : Guid.Empty;
        return res;
    }
    public string GetName(ulong player)
    {
        if (NameTranslations == null) return "L" + Order.ToString(Data.Locale);
        if (!Data.Languages.TryGetValue(player, out string lang))
            lang = JSONMethods.DEFAULT_LANGUAGE;
        if (NameTranslations.TryGetValue(lang, out string rtn) || (!lang.Equals(JSONMethods.DEFAULT_LANGUAGE, StringComparison.Ordinal) && NameTranslations.TryGetValue(JSONMethods.DEFAULT_LANGUAGE, out rtn)))
            return rtn;
        return NameTranslations.Values.FirstOrDefault() ?? ("L" + Order.ToString(Data.Locale));
    }
    public string GetAbbreviation(ulong player)
    {
        if (AbbreviationTranslations == null) return "L" + Order.ToString(Data.Locale);
        if (!Data.Languages.TryGetValue(player, out string lang))
            lang = JSONMethods.DEFAULT_LANGUAGE;
        if (AbbreviationTranslations.TryGetValue(lang, out string rtn) || (!lang.Equals(JSONMethods.DEFAULT_LANGUAGE, StringComparison.Ordinal) && AbbreviationTranslations.TryGetValue(JSONMethods.DEFAULT_LANGUAGE, out rtn)))
            return rtn;
        return AbbreviationTranslations.Values.FirstOrDefault() ?? ("L" + Order.ToString(Data.Locale));
    }
}