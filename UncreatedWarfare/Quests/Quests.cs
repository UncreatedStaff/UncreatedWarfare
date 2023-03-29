using SDG.Unturned;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace Uncreated.Warfare.Quests;
/// <summary>Stores information about a <see cref="Quests.QuestType"/> of quest. Isn't necessarily constant, some can have varients that are used for daily quests.
/// Rank and kit quests should overridden with a set <see cref="IQuestState{TTracker, TDataNew}"/>.</summary>
public abstract class BaseQuestData : ITranslationArgument
{
    private QuestType _type;
    private Dictionary<string, string> _translations;
    private RewardExpression[] _rewardExpressions;
    public bool CanBeDailyQuest = true;
    public abstract IEnumerable<IQuestPreset> Presets { get; }
    public abstract int TickFrequencySeconds { get; }
    public QuestType QuestType { get => _type; internal set => _type = value; }
    public Dictionary<string, string> Translations { get => _translations; internal set => _translations = value; }
    public virtual bool ResetOnGameEnd => false;
    public IQuestReward[] EvaluateRewards(in IQuestState state)
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
    public string Translate(bool forAsset, string language, params object[]? formatting)
    {
#if DEBUG
        using IDisposable profiler = ProfilingUtils.StartTracking();
#endif
        if (forAsset)
        {
            if (formatting is not null && formatting.Length > 0) formatting[0] = "{0}";
            else formatting = new object[1] { "{0}" };
        }
        if (Translations == null)
        {
            L.LogWarning("No translations for " + QuestType.ToString() + " quest.");
            return QuestType.ToString() + " - " + string.Join("|", formatting);
        }
        if (Translations.TryGetValue(language, out string v) || (!language.Equals(L.Default, StringComparison.Ordinal) && Translations.TryGetValue(L.Default, out v)))
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
        return string.Join(", ", formatting);
    }
    public string Translate(bool forAsset, UCPlayer? player, params object[]? formatting) =>
        Translate(forAsset, player is not null && Data.Languages.TryGetValue(player.Steam64, out string language) ? language : L.Default, formatting);
    public abstract void OnPropertyRead(string propertyname, ref Utf8JsonReader reader);
    public abstract BaseQuestTracker? CreateTracker(UCPlayer player);
    public abstract IQuestState GetState();
    protected static readonly List<IQuestReward> rewardTempList = new List<IQuestReward>(3);
    public abstract BaseQuestTracker? GetTracker(UCPlayer? player, in IQuestState state);
    public abstract BaseQuestTracker? GetTracker(UCPlayer? player, in IQuestPreset preset);
    public abstract IQuestPreset ReadPreset(ref Utf8JsonReader reader);
    public abstract void ReadPresets(ref Utf8JsonReader reader);
    private static readonly List<RewardExpression> _rewardTemp = new List<RewardExpression>(3);
    public void ReadRewards(ref Utf8JsonReader reader)
    {
#if DEBUG
        using IDisposable profiler = ProfilingUtils.StartTracking();
#endif
        lock (_rewardTemp)
        {
            while (reader.Read() && reader.TokenType != JsonTokenType.EndArray)
            {
                if (reader.TokenType == JsonTokenType.StartObject)
                {
                    RewardExpression? re = RewardExpression.ReadFromJson(ref reader, QuestType);
                    if (re is not null)
                        _rewardTemp.Add(re);
                }
            }

            this._rewardExpressions = _rewardTemp.ToArray();
            _rewardTemp.Clear();
        }
    }
    public abstract IQuestPreset CreateRandomPreset(ushort flag = 0);
    [FormatDisplay("Quest Type (" + nameof(Quests.QuestType) + ")")]
    public const string TYPE_FORMAT = "t";

    [FormatDisplay(typeof(QuestAsset), "Quest Name")]
    /// <summary>For <see cref="QuestAsset"/> formatting.</summary>
    public const string COLOR_QUEST_ASSET_FORMAT = "c";
    public string Translate(string language, string? format, UCPlayer? target, CultureInfo? culture,
        ref TranslationFlags flags)
    {
        if (format is not null)
        {
            if (format.Equals(TYPE_FORMAT, StringComparison.Ordinal))
                return Localization.TranslateEnum(QuestType, language);
        }

        return Localization.TranslateEnum(QuestType, language);
    }
}
/// <inheritdoc/>
public abstract class BaseQuestData<TTracker, TState, TDataParent> : BaseQuestData where TTracker : BaseQuestTracker where TState : IQuestState<TTracker, TDataParent>, new() where TDataParent : BaseQuestData<TTracker, TState, TDataParent>
{
    public override IEnumerable<IQuestPreset> Presets => _presets.Cast<IQuestPreset>();
    public Preset[] _presets;
    public readonly struct Preset : IQuestPreset
    {
        public readonly Guid _key;
        public readonly ulong _team;
        public readonly ushort _flag;
        public readonly TState _state;
        public readonly IQuestReward[]? _rewards;
        public Preset(Guid key, TState state, IQuestReward[]? rewards, ulong team, ushort flag)
        {
            this._key = key;
            this._state = state;
            this._team = team;
            this._flag = flag;
            this._rewards = rewards;
        }
        public Guid Key => _key;
        public IQuestState State => _state;
        public IQuestReward[]? RewardOverrides => _rewards;
        public ulong Team => _team;
        public ushort Flag => _flag;

        public override string ToString() =>
            $"Preset {_key}. Team: {_team}, Flag: {_flag}, State: {_state}" + (_rewards is null ? ", No reward overrides" : (", " + _rewards.Length + " reward override(s)."));
    }
    public override sealed IQuestState GetState() => GetNewState();
    public TState GetNewState()
    {
#if DEBUG
        using IDisposable profiler = ProfilingUtils.StartTracking();
#endif
        TState state = new TState();
        state.Init((TDataParent)this);
        return state;
    }
    protected abstract TTracker CreateQuestTracker(UCPlayer? player, in TState state, in IQuestPreset? preset);
    public TTracker CreateQuestTracker(UCPlayer? player, in IQuestPreset? preset = null)
    {
#if DEBUG
        using IDisposable profiler = ProfilingUtils.StartTracking();
#endif
        TState state = GetNewState();
        TTracker tracker = CreateQuestTracker(player, in state, in preset);
        return tracker;
    }

    public override sealed string ToString() =>
        $"{QuestType} quest data: {_presets?.Length ?? -1} presets, daily quest: {CanBeDailyQuest}, translations: {Translations?.FirstOrDefault().Value ?? "null"}.\n" +
        $"    Presets: {(_presets == null ? "null array" : string.Join(", ", _presets.Select(x => x.ToString())))}";
    public override sealed BaseQuestTracker? CreateTracker(UCPlayer player) => CreateQuestTracker(player);
    public override sealed BaseQuestTracker? GetTracker(UCPlayer? player, in IQuestState state)
    {
#if DEBUG
        using IDisposable profiler = ProfilingUtils.StartTracking();
#endif
        if (state is TState st2)
        {
            TTracker tracker = CreateQuestTracker(player, in st2, null);
            return tracker;
        }
        return null;
    }
    public override sealed BaseQuestTracker? GetTracker(UCPlayer? player, in IQuestPreset preset)
    {
#if DEBUG
        using IDisposable profiler = ProfilingUtils.StartTracking();
#endif
        if (preset.State is TState st2)
        {
            TTracker tracker = CreateQuestTracker(player, in st2, in preset);
            tracker.Flag = preset.Flag;
            return tracker;
        }
        return null;
    }
    public override sealed IQuestPreset ReadPreset(ref Utf8JsonReader reader) => ReadPresetIntl(ref reader);
    public Preset ReadPresetIntl(ref Utf8JsonReader reader)
    {
        Guid key = default;
        ulong varTeam = default;
        TState? state = default;
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
                        lock (rewardTempList)
                        {
                            rewardTempList.Clear();
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
                                        rewardTempList.Add(reward);
                                }
                                else break;
                            }
                            if (rewardTempList.Count > 0)
                            {
                                rewards = rewardTempList.ToArray();
                                rewardTempList.Clear();
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
        return new Preset(key, state!, rewards, varTeam, flag);
    }
    public override sealed void ReadPresets(ref Utf8JsonReader reader)
    {
#if DEBUG
        using IDisposable profiler = ProfilingUtils.StartTracking();
#endif
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
        _presets = presets.ToArray();
        presets.Clear();
    }
    public sealed override IQuestPreset CreateRandomPreset(ushort flag = 0)
    {
        return new Preset(Guid.NewGuid(), GetNewState(), null, 0, flag);
    }
}

/// <summary>Base class used to track information about a player's progress in a quest. One per player per quest.
/// <para>Implement children of <see cref="INotifyTracker"/> to listen to events. If <see cref="BaseQuestData.TickFrequencySeconds"/> is > 0, the tick function will run as often as specified.</para></summary>
public abstract class BaseQuestTracker : IDisposable, INotifyTracker
{
    protected readonly UCPlayer _player;
    public readonly BaseQuestData QuestData;
    public readonly IQuestPreset? Preset;
    public readonly Guid PresetKey;
    public IQuestReward[] Rewards;
    protected bool isDisposed;
    public bool IsDailyQuest = false;
    public ushort Flag = 0;
    protected abstract bool CompletedCheck { get; }
    public UCPlayer? Player => _player;
    public bool IsTemporary => _player == null;
    public bool IsCompleted { get => CompletedCheck; }
    public virtual short FlagValue => 0;
    public BaseQuestTracker(BaseQuestData data, UCPlayer? target, in IQuestState state, in IQuestPreset? preset)
    {
        this.QuestData = data;
        this.Preset = preset;
        if (preset is not null)
        {
            this.PresetKey = preset.Key;
            this.Rewards = preset.RewardOverrides ?? data.EvaluateRewards(in state);
        }
        else
            this.Rewards = data.EvaluateRewards(in state);
        this._player = target!;
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
        if (QuestData != null && QuestData.ResetOnGameEnd)
        {
            ResetToDefaults();
            TellUpdated();
        }
    }
    public void TellCompleted()
    {
        TellUpdated();
        CancellationToken tkn = UCWarfare.UnloadCancel;
        UCWarfare.RunTask(async token =>
        {
            await Task.Delay(500, token).ConfigureAwait(false);
            await UCWarfare.ToUpdate(token);
            QuestManager.OnQuestCompleted(this);
        }, tkn, ctx: "Compelte quest for " + Player + ".");
    }
    public void TellUpdated(bool skipFlagUpdate = false)
    {
        QuestManager.OnQuestUpdated(this, skipFlagUpdate);
    }
    public void Dispose()
    {
        if (!isDisposed)
        {
            Cleanup();
            isDisposed = true;
        }
        GC.SuppressFinalize(this);
    }
    internal void TryGiveRewards()
    {
        if (Rewards is null || Rewards.Length == 0)
            return;

        for (int i = 0; i < Rewards.Length; ++i)
        {
            Rewards[i].GiveReward(_player, this);
        }
    }

    public override string ToString() => GetDisplayString(false);
}