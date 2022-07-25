using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Uncreated.Encoding;
using Uncreated.Framework;
using Uncreated.Warfare.Kits;

namespace Uncreated.Warfare.Quests;
public static class QuestRewards
{
}
public enum EQuestRewardType
{
    NONE,
    XP,
    RANK,
    CREDITS,
    KIT_ACCESS
}
[QuestReward(EQuestRewardType.XP)]
public class XPReward : IQuestReward
{
    public EQuestRewardType Type { get; set; }
    public int XP { get; private set; }
    public void GiveReward(UCPlayer player, BaseQuestTracker tracker)
    {
        Point.Points.AwardXP(player.Player, XP,
            Localization.TranslateEnum(tracker.QuestData.QuestType,
                Data.Languages.TryGetValue(player.Steam64, out string lang) 
                    ? lang 
                    : JSONMethods.DEFAULT_LANGUAGE).ToUpper() + " REWARD");
    }
    public void Init(object value)
    {
        if (value is not int i) throw new ArgumentException("Init value must be of type Int32.", nameof(value));
        XP = i;
    }
    public void ReadJson(ref Utf8JsonReader reader)
    {
        do
        {
            if (reader.TokenType == JsonTokenType.EndObject) return;
            if (reader.TokenType == JsonTokenType.PropertyName)
            {
                string? prop = reader.GetString();
                if (reader.Read() && prop is not null && prop.Equals("xp", StringComparison.OrdinalIgnoreCase) &&
                    reader.TokenType == JsonTokenType.Number)
                {
                    if (reader.TryGetInt32(out int xp))
                        XP = xp;
                }
            }
        } while (reader.Read());
    }

    public void WriteJson(Utf8JsonWriter writer)
    {
        writer.WritePropertyName("xp");
        writer.WriteNumberValue(XP);
    }
}
[QuestReward(EQuestRewardType.CREDITS)]
public class CreditsReward : IQuestReward
{
    public EQuestRewardType Type { get; set; }
    public int Credits { get; private set; }
    public void GiveReward(UCPlayer player, BaseQuestTracker tracker)
    {
        Point.Points.AwardCredits(player, Credits,
            Localization.TranslateEnum(tracker.QuestData.QuestType,
                Data.Languages.TryGetValue(player.Steam64, out string lang) 
                    ? lang
                    : JSONMethods.DEFAULT_LANGUAGE).ToUpper() + " REWARD", redmessage: false);
    }
    public void Init(object value)
    {
        if (value is not int i) throw new ArgumentException("Init value must be of type Int32.", nameof(value));
        Credits = i;
    }
    public void ReadJson(ref Utf8JsonReader reader)
    {
        do
        {
            if (reader.TokenType == JsonTokenType.EndObject) return;
            if (reader.TokenType == JsonTokenType.PropertyName)
            {
                string? prop = reader.GetString();
                if (reader.Read() && prop is not null && prop.Equals("credits", StringComparison.OrdinalIgnoreCase) &&
                    reader.TokenType == JsonTokenType.Number)
                {
                    if (reader.TryGetInt32(out int credits))
                        Credits = credits;
                }
            }
        } while (reader.Read());
    }

    public void WriteJson(Utf8JsonWriter writer)
    {
        writer.WritePropertyName("credits");
        writer.WriteNumberValue(Credits);
    }
}
[QuestReward(EQuestRewardType.RANK)]
public class RankReward : IQuestReward
{
    public EQuestRewardType Type { get; set; }
    public int RankOrder { get; private set; }
    public void GiveReward(UCPlayer player, BaseQuestTracker tracker)
    {
        Ranks.RankManager.SkipToRank(player, RankOrder);
    }
    public void Init(object value)
    {
        if (value is not int i) throw new ArgumentException("Init value must be of type Int32.", nameof(value));
        RankOrder = i;
    }
    public void ReadJson(ref Utf8JsonReader reader)
    {
        do
        {
            if (reader.TokenType == JsonTokenType.EndObject) return;
            if (reader.TokenType == JsonTokenType.PropertyName)
            {
                string? prop = reader.GetString();
                if (reader.Read() && prop is not null && prop.Equals("order", StringComparison.OrdinalIgnoreCase) &&
                    reader.TokenType == JsonTokenType.Number)
                {
                    if (reader.TryGetInt32(out int order))
                        RankOrder = order;
                }
            }
        } while (reader.Read());
    }

    public void WriteJson(Utf8JsonWriter writer)
    {
        writer.WritePropertyName("order");
        writer.WriteNumberValue(RankOrder);
    }
}
[QuestReward(EQuestRewardType.KIT_ACCESS)]
public class KitAccessReward : IQuestReward
{
    public EQuestRewardType Type { get; set; }
    public string KitId { get; private set; }
    public void GiveReward(UCPlayer player, BaseQuestTracker tracker)
    {
        if (KitManager.KitExists(KitId, out Kit kit))
            _ = KitManager.GiveAccess(kit, player, EKitAccessType.QUEST_REWARD).ConfigureAwait(false);
    }
    public void Init(object value)
    {
        if (value is not string str) throw new ArgumentException("Init value must be of type String.", nameof(value));
        KitId = str;
    }
    public void ReadJson(ref Utf8JsonReader reader)
    {
        do
        {
            if (reader.TokenType == JsonTokenType.EndObject) return;
            if (reader.TokenType == JsonTokenType.PropertyName)
            {
                string? prop = reader.GetString();
                if (reader.Read() && prop is not null && prop.Equals("kit_id", StringComparison.OrdinalIgnoreCase) &&
                    reader.TokenType == JsonTokenType.String)
                {
                    KitId = reader.GetString()!;
                }
            }
        } while (reader.Read());
    }

    public void WriteJson(Utf8JsonWriter writer)
    {
        writer.WritePropertyName("kit_id");
        writer.WriteStringValue(KitId);
    }
}

public interface IQuestReward : IJsonReadWrite
{
    EQuestRewardType Type { get; internal set; }
    void Init(object value);
    void GiveReward(UCPlayer player, BaseQuestTracker tracker);
}

public class RewardExpression : IJsonReadWrite
{
    public EQuestType QuestType { get; internal set; }
    public EQuestRewardType RewardType { get; private set; }
    private string _expression;
    public void ReadJson(ref Utf8JsonReader reader)
    {
        do
        {
            if (reader.TokenType == JsonTokenType.StartObject) continue;
            if (reader.TokenType == JsonTokenType.EndObject) return;
            if (reader.TokenType == JsonTokenType.PropertyName)
            {
                string? prop = reader.GetString()!;
                if (reader.Read())
                {
                    if (RewardType == EQuestRewardType.NONE && prop.Equals("type", StringComparison.OrdinalIgnoreCase))
                    {
                        prop = reader.GetString()!;
                        if (!Enum.TryParse(prop, true, out EQuestRewardType type) || !QuestManager.QuestRewardTypes.ContainsKey(type))
                            L.LogWarning("Invalid quest type " + prop + " in " + QuestType + " RewardExpression read.");
                        else
                            RewardType = type;
                    }
                    else if (prop.Equals("expression", StringComparison.OrdinalIgnoreCase))
                    {
                        _expression = reader.GetString()!;
                        if (!ValidateExpression())
                            L.LogWarning("Invalid expression: \"" + _expression + "\" in " + QuestType + " RewardExpression read.");
                    }
                }
            }
        } while (reader.Read());
    }
    private static readonly char[] TOKEN_SPLITS = new char[] { ' ', '*', '/', '+', '-', '%', '^' }
    private bool ValidateExpression()
    {
        List<string> tokens = new List<string>(32);

        return true;
    }
    public void WriteJson(Utf8JsonWriter writer)
    {
        throw new NotImplementedException();
    }
}