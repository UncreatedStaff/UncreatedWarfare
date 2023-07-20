using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Uncreated.Framework;
using Uncreated.Warfare.Kits;
using Uncreated.Warfare.Levels;
using Uncreated.Warfare.Sync;

namespace Uncreated.Warfare.Quests;
public static class QuestRewards
{
    public static readonly Dictionary<QuestRewardType, Type> QuestRewardTypes = new Dictionary<QuestRewardType, Type>(8);
    internal static void LoadTypes(List<Type> types)
    {
        foreach (Type type in types.Where(x => x != null && x.IsClass && typeof(IQuestReward).IsAssignableFrom(x)))
        {
            if (Attribute.GetCustomAttribute(type, typeof(QuestRewardAttribute)) is QuestRewardAttribute attr && attr.Type != QuestRewardType.None)
            {
                if (QuestRewardTypes.TryGetValue(attr.Type, out Type first))
                {
                    if (first != type)
                        L.LogWarning("Duplicate reward type: " + attr.Type + " on type " + type.Name + " and " + first.Name + ".");
                }
                else
                    QuestRewardTypes.Add(attr.Type, type);
            }
        }
    }
    public static IQuestReward? GetQuestReward(QuestRewardType type)
    {
        if (QuestRewardTypes.TryGetValue(type, out Type result))
        {
            try
            {
                if (Activator.CreateInstance(result) is IQuestReward data)
                {
                    data.Type = type;
                    return data;
                }
            }
            catch (Exception ex)
            {
                L.LogError("Failed to create a quest reward of type " + type);
                L.LogError(ex);
            }
        }
        L.LogError("Failed to create a quest reward of type " + type);
        return null;
    }
}
public enum QuestRewardType
{
    None,
    XP,
    Rank,
    Credits,
    KitAccess
}
[QuestReward(QuestRewardType.XP, typeof(int))]
public class XPReward : IQuestReward
{
    public QuestRewardType Type { get; set; }
    public int XP { get; private set; }
    public Task GiveReward(UCPlayer player, BaseQuestTracker tracker, CancellationToken token = default)
    {
        XPParameters parameters = new XPParameters(player, player.GetTeam(), XP,
            // todo translation
            Localization.TranslateEnum(tracker.QuestData.QuestType,
                Data.Languages.TryGetValue(player.Steam64, out string lang)
                    ? lang
                    : L.Default).ToUpper() + " REWARD", false);
        return Points.AwardXPAsync(parameters, token);
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

    public override string ToString() => "Reward: " + XP + " XP";
}
[QuestReward(QuestRewardType.Credits, typeof(int))]
public class CreditsReward : IQuestReward
{
    public QuestRewardType Type { get; set; }
    public int Credits { get; private set; }
    public Task GiveReward(UCPlayer player, BaseQuestTracker tracker, CancellationToken token = default)
    {
        CreditsParameters parameters = new CreditsParameters(player, player.GetTeam(), Credits,
            // todo translation
            Localization.TranslateEnum(tracker.QuestData.QuestType,
                Data.Languages.TryGetValue(player.Steam64, out string lang)
                    ? lang
                    : L.Default).ToUpper() + " REWARD");
        return Points.AwardCreditsAsync(parameters, token);
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

    public override string ToString() => "Reward: C " + Credits;
}
[QuestReward(QuestRewardType.Rank, typeof(int))]
public class RankReward : IQuestReward
{
    public QuestRewardType Type { get; set; }
    public int RankOrder { get; private set; }
    public Task GiveReward(UCPlayer player, BaseQuestTracker tracker, CancellationToken token = default)
    {
        Ranks.RankManager.SkipToRank(player, RankOrder);
        return Task.CompletedTask;
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
    public override string ToString()
    {
        ref Ranks.RankData d = ref Ranks.RankManager.GetRank(RankOrder, out bool success);
        return "Reward: Unlock " + (success ? d.GetName(L.Default) : "UNKNOWN RANK") + " (Order #" + RankOrder + ")";
    }
}
[QuestReward(QuestRewardType.KitAccess, typeof(string))]
public class KitAccessReward : IQuestReward
{
    public QuestRewardType Type { get; set; }
    public string KitId { get; private set; }
    public async Task GiveReward(UCPlayer player, BaseQuestTracker tracker, CancellationToken token = default)
    {
        if (!string.IsNullOrEmpty(KitId))
            await KitManager.GiveAccess(KitId, player, KitAccessType.QuestReward, token).ConfigureAwait(false);

        KitSync.OnAccessChanged(player.Steam64);
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
    public override string ToString() => "Reward: Unlock \"" + KitId + "\"";
}

public interface IQuestReward : IJsonReadWrite
{
    QuestRewardType Type { get; set; }
    void Init(object value);
    Task GiveReward(UCPlayer player, BaseQuestTracker tracker, CancellationToken token = default);
}