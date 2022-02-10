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
                "6edd15ee-5a0f-4acb-a9a8-f15159205e28", "73e4a6e9-711a-4893-9195-00856676d084", "20567555-a048-4d86-a6f1-b18dff2a5044"),
            new RankData(2, "Private 1st Class", "Pfc", "dd87890eae694685be3c1f48eb9e695e", 
                "da5d6ab0-5ba8-497b-a754-2c2d235bb61e", "4d06677b-7d9d-4929-8d66-2c7340b3ba8e", "6dff912f-5872-4a9e-a38d-dfcfa3abb9db"),
            new RankData(3, "Corporal", "Col", "ed62dbfa66d747dda0c15cca11628239", 
                "7b769793-2b38-4a5f-aa41-1403ce1a16ca", "c0805eed-b25a-4819-803a-30c8a25e7da3", "ef485d62-7d02-4a81-acef-9cc6ac0cc342", "3123710f-955e-45b3-947f-9528845911bb"),
            // TODO: make quests for L4-L10
            new RankData(4, "Specialist", "Spec", ""),
            new RankData(5, "Sergeant", "Sgt", ""),
            new RankData(6, "Staff Sergeant", "Ssg", ""),
            new RankData(7, "Sergeant 1st Class", "Sfc", ""),
            new RankData(8, "Sergeant Major", "S.M", ""),
            new RankData(9, "Warrant Officer", "W.O", ""),
            new RankData(10, "Chief Warrant Officer", "C.W.O", ""),
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