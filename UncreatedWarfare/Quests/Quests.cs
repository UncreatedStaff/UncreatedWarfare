using Cysharp.Threading.Tasks;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Uncreated.Warfare.Models.Localization;

namespace Uncreated.Warfare.Quests;
/// <summary>Stores information about a <see cref="Quests.QuestType"/> of quest. Isn't necessarily constant, some can have varients that are used for daily quests.
/// Rank and kit quests should overridden with a set <see cref="IQuestState{TTracker, TDataNew}"/>.</summary>
public abstract class BaseQuestData : ITranslationArgument
{
    protected static readonly List<IQuestReward> RewardTempList = new List<IQuestReward>(3);
    private static readonly List<RewardExpression> RewardExpressionTempList = new List<RewardExpression>(3);

    private QuestType _type;
    private RewardExpression[] _rewardExpressions;

    public bool CanBeDailyQuest = true;
    public virtual bool ResetOnGameEnd => false;
    public abstract IEnumerable<IQuestPreset> Presets { get; }
    public abstract int TickFrequencySeconds { get; }
    public QuestType QuestType { get => _type; internal set => _type = value; }
    public Dictionary<string, string> Translations { get; internal set; }
    public IQuestReward[] EvaluateRewards(IQuestState state)
    {
        if (_rewardExpressions is null || _rewardExpressions.Length == 0)
            return Array.Empty<IQuestReward>();
        IQuestReward[] rews = new IQuestReward[_rewardExpressions.Length];
        for (int i = 0; i < _rewardExpressions.Length; ++i)
        {
            IQuestReward rew = QuestRewards.GetQuestReward(_rewardExpressions[i].RewardType)!;
            rew.Init(_rewardExpressions[i].Evaluate(state)!);
            rews[i] = rew;
        }
        return rews;
    }
    public string Translate(bool forAsset, LanguageInfo language, CultureInfo culture, params object[]? formatting)
    {
        if (forAsset)
        {
            if (formatting is not null && formatting.Length > 0) formatting[0] = "{0}";
            else formatting = [ "{0}" ];
        }

        if (Translations == null)
        {
            L.LogWarning("No translations for " + QuestType.ToString() + " quest.");
            return QuestType.ToString() + " - " + string.Join("|", formatting ?? Array.Empty<object>());
        }

        if (Translations.TryGetValue(language.Code, out string v) || (!language.IsDefault && Translations.TryGetValue(L.Default, out v)))
        {
            try
            {
                return formatting == null || formatting.Length == 0 ? v : string.Format(v, formatting);
            }
            catch (FormatException ex)
            {
                L.LogError("Error formatting quest " + _type.ToString());
                L.LogError(ex);
            }
        }

        return string.Join(", ", formatting ?? Array.Empty<object>());
    }
    public string Translate(bool forAsset, UCPlayer? player, params object[]? formatting) =>
        Translate(forAsset, player?.Locale.LanguageInfo ?? Localization.GetDefaultLanguage(), player?.Locale.CultureInfo ?? Data.LocalLocale, formatting);
    public abstract void OnPropertyRead(string propertyname, ref Utf8JsonReader reader);
    public abstract BaseQuestTracker? CreateTracker(UCPlayer player);
    public abstract IQuestState GetState();
    public abstract BaseQuestTracker? GetTracker(UCPlayer? player, IQuestState state);
    public abstract BaseQuestTracker? GetTracker(UCPlayer? player, IQuestPreset preset);
    public abstract IQuestPreset ReadPreset(ref Utf8JsonReader reader);
    public abstract void ReadPresets(ref Utf8JsonReader reader);
    public void ReadRewards(ref Utf8JsonReader reader)
    {
        lock (RewardExpressionTempList)
        {
            while (reader.Read() && reader.TokenType != JsonTokenType.EndArray)
            {
                if (reader.TokenType == JsonTokenType.StartObject)
                {
                    RewardExpression? re = RewardExpression.ReadFromJson(ref reader, QuestType);
                    if (re is not null)
                        RewardExpressionTempList.Add(re);
                }
            }

            _rewardExpressions = RewardExpressionTempList.ToArray();
            RewardExpressionTempList.Clear();
        }
    }
    public abstract IQuestPreset CreateRandomPreset(ushort flag = 0);

    [FormatDisplay("Quest Type (" + nameof(Quests.QuestType) + ")")]
    public const string FormatType = "t";

    /// <summary>For <see cref="QuestAsset"/> formatting.</summary>
    [FormatDisplay(typeof(QuestAsset), "Quest Name")]
    public const string FormatColorQuestAsset = "c";
    public string Translate(LanguageInfo language, string? format, UCPlayer? target, CultureInfo? culture,
        ref TranslationFlags flags)
    {
        if (format is not null)
        {
            if (format.Equals(FormatType, StringComparison.Ordinal))
                return Localization.TranslateEnum(QuestType, language);
        }

        return Localization.TranslateEnum(QuestType, language);
    }
}
/// <inheritdoc/>
public abstract class BaseQuestData<TTracker, TState, TQuestData> : BaseQuestData where TTracker : BaseQuestTracker where TState : struct, IQuestState<TQuestData> where TQuestData : BaseQuestData<TTracker, TState, TQuestData>
{
    private IQuestPreset[] _boxedPresets;
    public Preset[] PresetItems;
    public override IEnumerable<IQuestPreset> Presets => _boxedPresets;
    public sealed override IQuestState GetState() => GetNewState();
    public TState GetNewState()
    {
        TState state = new TState();
        state.CreateFromTemplate((TQuestData)this);
        return state;
    }
    protected abstract TTracker CreateQuestTracker(UCPlayer? player, ref TState state, IQuestPreset? preset);
    public TTracker CreateQuestTracker(UCPlayer? player, in IQuestPreset? preset = null)
    {
        TState state = GetNewState();
        TTracker tracker = CreateQuestTracker(player, ref state, preset);
        return tracker;
    }

    public sealed override string ToString() =>
        $"{QuestType} quest data: {PresetItems?.Length ?? -1} presets, daily quest: {CanBeDailyQuest}, translations: {Translations?.FirstOrDefault().Value ?? "null"}.\n" +
        $"    Presets: {(PresetItems == null ? "null array" : string.Join(", ", PresetItems.Select(x => x.ToString())))}";
    public sealed override BaseQuestTracker CreateTracker(UCPlayer player) => CreateQuestTracker(player);
    public sealed override BaseQuestTracker? GetTracker(UCPlayer? player, IQuestState state)
    {
        if (state is not TState st2)
            return null;

        TTracker tracker = CreateQuestTracker(player, ref st2, null);
        return tracker;
    }
    public sealed override BaseQuestTracker? GetTracker(UCPlayer? player, IQuestPreset preset)
    {
        if (preset.State is not TState st2)
            return null;

        TTracker tracker = CreateQuestTracker(player, ref st2, preset);
        tracker.Flag = preset.Flag;
        return tracker;
    }
    public sealed override IQuestPreset ReadPreset(ref Utf8JsonReader reader) => ReadPresetIntl(ref reader);
    public Preset ReadPresetIntl(ref Utf8JsonReader reader)
    {
        Guid key = default;
        ulong varTeam = default;
        IQuestState<TQuestData>? state = default;
        IQuestReward[]? rewards = null;
        ushort flag = 0;
        while (reader.Read() && reader.TokenType == JsonTokenType.PropertyName)
        {
            string prop = reader.GetString()!;
            if (!reader.Read()) break;
            if (key == default && prop.Equals("key", StringComparison.OrdinalIgnoreCase))
            {
                if (reader.TokenType == JsonTokenType.String)
                {
                    if (!reader.TryGetGuid(out key))
                        L.LogWarning("Failed to parse 'key' GUID from " + QuestType + " preset.");
                }
                else
                    L.LogWarning("Failed to parse 'key' GUID from " + QuestType + " preset.");
            }
            else if (varTeam == default && prop.Equals("varient_team"))
            {
                if (reader.TokenType == JsonTokenType.Number)
                {
                    if (!reader.TryGetUInt64(out varTeam))
                        L.LogWarning("Failed to parse 'varient_team' UInt64 from " + QuestType + " preset.");
                }
                else
                    L.LogWarning("Failed to parse 'varient_team' UInt64 from " + QuestType + " preset.");
            }
            else if (prop.Equals("rewards"))
            {
                if (reader.TokenType != JsonTokenType.Null)
                {
                    if (reader.TokenType == JsonTokenType.StartArray)
                    {
                        lock (RewardTempList)
                        {
                            RewardTempList.Clear();
                            while (reader.Read())
                            {
                                if (reader.TokenType == JsonTokenType.StartObject)
                                {
                                    IQuestReward? reward = null;
                                    QuestRewardType type = QuestRewardType.None;
                                    while (reader.Read())
                                    {
                                        if (reader.TokenType == JsonTokenType.PropertyName)
                                        {
                                            string prop2 = reader.GetString()!;
                                            if (type == QuestRewardType.None && prop2.Equals("type", StringComparison.OrdinalIgnoreCase))
                                            {
                                                if (reader.Read())
                                                {
                                                    if (reader.TokenType == JsonTokenType.String)
                                                    {
                                                        prop2 = reader.GetString()!;
                                                        if (Enum.TryParse(prop2, true, out type))
                                                        {
                                                            reward = QuestRewards.GetQuestReward(type);
                                                        }
                                                    }
                                                    else
                                                        L.LogWarning("Failed to parse 'reward'.'quest_type' IQuestReward object property as a EQuestRewardType with key {" +
                                                                     key.ToString("N") + "} from " + QuestType + " presets.");
                                                }
                                            }
                                            else if (reward != null)
                                            {
                                                reward.ReadJson(ref reader);
                                                if (reader.TokenType == JsonTokenType.EndObject)
                                                    break;
                                            }
                                        }
                                        else if (reader.TokenType == JsonTokenType.EndObject)
                                            break;
                                    }

                                    if (reward != null)
                                        RewardTempList.Add(reward);
                                }
                                else break;
                            }
                            if (RewardTempList.Count > 0)
                            {
                                rewards = RewardTempList.ToArray();
                                RewardTempList.Clear();
                            }
                        }
                    }
                }
            }
            else if (flag == 0 && prop.Equals("flag"))
            {
                if (reader.TokenType == JsonTokenType.Number)
                {
                    if (!reader.TryGetUInt16(out flag))
                    {
                        L.LogWarning("Failed to parse 'flag' UInt16 from " + QuestType + " preset, defaulting to 0");
                        flag = 0;
                    }
                }
                else
                    L.LogWarning("Failed to parse 'flag' UInt16 from " + QuestType + " preset, defaulting to 0");
            }
            else if (prop.Equals("state"))
            {
                if (reader.TokenType == JsonTokenType.StartObject)
                {
                    state = new TState();
                    while (reader.Read())
                    {
                        if (reader.TokenType == JsonTokenType.PropertyName)
                        {
                            string prop2 = reader.GetString()!;
                            try
                            {
                                if (reader.Read())
                                    state.OnPropertyRead(ref reader, prop2);
                            }
                            catch (Exception ex)
                            {
                                L.LogWarning("Failed to parse 'state' IQuestState object property " + prop2 + " with key {" +
                                             key.ToString("N") + "} from " + QuestType + " presets.");
                                L.LogError(ex);
                            }
                        }
                        else if (reader.TokenType == JsonTokenType.EndObject)
                            break;
                    }
                    if (reader.Read() && reader.TokenType == JsonTokenType.EndObject)
                        break;
                }
                else
                    L.LogWarning("Failed to parse 'state' IQuestState object with key {" + key.ToString("N") + "} from " + QuestType + " presets.");
            }
        }

        if (state == null)
        {
            L.LogWarning("Failed to find 'state' IQuestState object from " + QuestType + " presets.");
            state = GetNewState();
        }

        return new Preset(key, (TState)state, rewards, varTeam, flag);
    }
    public sealed override void ReadPresets(ref Utf8JsonReader reader)
    {
        List<Preset> presets = new List<Preset>(4);
        while (reader.Read() && reader.TokenType != JsonTokenType.EndArray)
        {
            if (reader.TokenType == JsonTokenType.StartObject)
            {
                Preset preset = ReadPresetIntl(ref reader);
                for (int i = 0; i < presets.Count; i++)
                {
                    Preset pr = presets[i];
                    if (pr.Key == preset.Key && preset.Team == pr.Team)
                        goto next;
                }
                presets.Add(preset);
            next:
                while (reader.TokenType != JsonTokenType.EndObject && reader.Read()) ;
            }
        }
        PresetItems = presets.ToArray();
        presets.Clear();

        _boxedPresets = new IQuestPreset[PresetItems.Length];

        for (int i = 0; i < PresetItems.Length; ++i)
            _boxedPresets[i] = PresetItems[i];
    }
    public sealed override IQuestPreset CreateRandomPreset(ushort flag = 0)
    {
        return new Preset(Guid.NewGuid(), GetNewState(), null, 0, flag);
    }
    public readonly struct Preset(Guid key, TState state, IQuestReward[]? rewards, ulong team, ushort flag) : IQuestPreset
    {
        public readonly Guid Key = key;
        public readonly ulong Team = team;
        public readonly ushort Flag = flag;
        public readonly TState State = state;
        public readonly IQuestReward[]? Rewards = rewards;

        Guid IQuestPreset.Key => Key;
        IQuestState IQuestPreset.State => State;
        IQuestReward[]? IQuestPreset.RewardOverrides => Rewards;
        ulong IQuestPreset.Team => Team;
        ushort IQuestPreset.Flag => Flag;
        public override string ToString() => $"Preset {Key}. Team: {Team}, Flag: {Flag}, State: {State}" +
                                             (Rewards is null ? ", No reward overrides" : (", " + Rewards.Length + " reward override(s)."));
    }
}

/// <summary>Base class used to track information about a player's progress in a quest. One per player per quest.
/// <para>Implement children of <see cref="INotifyTracker"/> to listen to events. If <see cref="BaseQuestData.TickFrequencySeconds"/> is > 0, the tick function will run as often as specified.</para></summary>
public abstract class BaseQuestTracker : IDisposable, INotifyTracker
{
    public readonly BaseQuestData QuestData;
    public readonly IQuestPreset? Preset;
    public readonly Guid PresetKey;
    public IQuestReward[] Rewards;
    protected bool IsDisposed;
    public bool IsDailyQuest = false;
    public ushort Flag;
    protected abstract bool CompletedCheck { get; }
    public UCPlayer? Player { get; }
    public bool IsTemporary => Player == null;
    public bool IsCompleted { get => CompletedCheck; }
    public virtual short FlagValue => 0;
    protected BaseQuestTracker(BaseQuestData data, UCPlayer? target, IQuestState state, IQuestPreset? preset)
    {
        QuestData = data;
        Preset = preset;
        if (preset is not null)
        {
            PresetKey = preset.Key;
            Rewards = preset.RewardOverrides ?? data.EvaluateRewards(state);
        }
        else
            Rewards = data.EvaluateRewards(state);
        Player = target;
    }
    public virtual void Tick() { }
    protected virtual void Cleanup() { }
    public virtual void ResetToDefaults() { }

    public string GetDisplayString(bool forAsset = false)
    {
        try
        {
            if (forAsset) return Translate(true);
            return Translate(false) ?? QuestData.QuestType.ToString();
        }
        catch (Exception ex)
        {
            L.LogError("Error getting translation for quest " + QuestData.QuestType);
            L.LogError(ex);
            return QuestData.QuestType.ToString();
        }
    }

    protected abstract string Translate(bool forAsset);
    public abstract void WriteQuestProgress(Utf8JsonWriter writer);
    public abstract void OnReadProgressSaveProperty(string property, ref Utf8JsonReader reader);
    public virtual void ManualComplete()
    {
        TellCompleted();
    }
    public void OnGameEnd()
    {
        if (QuestData is not { ResetOnGameEnd: true })
            return;

        ResetToDefaults();
        TellUpdated();
    }
    public void TellCompleted()
    {
        TellUpdated();
        CancellationToken tkn = UCWarfare.UnloadCancel;
        UCWarfare.RunTask(async token =>
        {
            await Task.Delay(500, token).ConfigureAwait(false);
            await UniTask.SwitchToMainThread(token);
            QuestManager.OnQuestCompleted(this);
        }, tkn, ctx: "Complete quest for " + Player + ".");
    }
    public void TellUpdated(bool skipFlagUpdate = false)
    {
        QuestManager.OnQuestUpdated(this, skipFlagUpdate);
    }
    public void Dispose()
    {
        if (!IsDisposed)
        {
            Cleanup();
            IsDisposed = true;
        }
        GC.SuppressFinalize(this);
    }
    internal void TryGiveRewards()
    {
        if (Rewards is null || Rewards.Length == 0)
            return;

        for (int i = 0; i < Rewards.Length; ++i)
        {
            Rewards[i].GiveReward(Player!, this);
        }
    }

    public override string ToString() => GetDisplayString(false);
}