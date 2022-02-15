using SDG.Unturned;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Uncreated.Warfare.Kits;

namespace Uncreated.Warfare.Quests;
/// <summary>Stores information about a <see cref="EQuestType"/> of quest. Isn't necessarily constant, some can have varients that are used for daily quests.
/// Rank and kit quests should overridden with a set <see cref="IQuestState{TTracker, TDataNew}"/>.</summary>
public abstract class BaseQuestData
{
    private EQuestType _type;
    private Dictionary<string, string> _translations;
    public bool CanBeDailyQuest = true;
    public abstract IEnumerable<IQuestPreset> Presets { get; }
    public virtual int XPReward { get => 1; } // remove ?
    public abstract int TickFrequencySeconds { get; }
    public EQuestType QuestType { get => _type; internal set => _type = value; }
    public Dictionary<string, string> Translations { get => _translations; internal set => _translations = value; }
    public virtual bool ResetOnGameEnd => false;
    public string Translate(string language, params object[] formatting)
    {
        if (Translations.TryGetValue(language, out string v) || (!language.Equals(JSONMethods.DEFAULT_LANGUAGE, StringComparison.Ordinal) && Translations.TryGetValue(JSONMethods.DEFAULT_LANGUAGE, out v)))
        {
            try
            {
                return string.Format(v, formatting);
            }
            catch (FormatException ex)
            {
                L.LogError("Error formatting quest " + _type.ToString());
                L.LogError(ex);
            }
        }
        return string.Join(", ", formatting);
    }
    public string Translate(UCPlayer player, params object[] formatting) =>
        Translate(Data.Languages.TryGetValue(player.Steam64, out string language) ? language : JSONMethods.DEFAULT_LANGUAGE, formatting);
    public abstract void OnPropertyRead(string propertyname, ref Utf8JsonReader reader);
    public abstract BaseQuestTracker CreateTracker(UCPlayer player);
    public abstract IQuestState GetState();
    public abstract BaseQuestTracker GetTracker(UCPlayer player, ref IQuestState state);
    public abstract BaseQuestTracker GetTracker(UCPlayer player, IQuestPreset preset);
    public abstract void ReadPresets(ref Utf8JsonReader reader);
}


/// <inheritdoc/>
public abstract class BaseQuestData<TTracker, TState, TDataParent> : BaseQuestData where TTracker : BaseQuestTracker where TState : IQuestState<TTracker, TDataParent>, new() where TDataParent : BaseQuestData<TTracker, TState, TDataParent>
{
    public override IEnumerable<IQuestPreset> Presets => _presets.Cast<IQuestPreset>();
    public Preset[] _presets;
    public readonly struct Preset : IQuestPreset
    {
        public readonly Guid _key;
        public readonly int _reqLevel;
        public readonly ulong _team;
        public readonly ushort _flag;
        public readonly TState _state;
        public Preset(Guid key, int requiredLevel, TState state, ulong team, ushort flag)
        {
            this._key = key;
            this._reqLevel = requiredLevel;
            this._state = state;
            this._team = team;
            this._flag = flag;
        }
        public Guid Key => _key;
        public int RequiredLevel => _reqLevel;
        public IQuestState State => _state;
        public ulong Team => _team;
        public ushort Flag => _flag;

        public override string ToString() =>
            $"Preset {_key}. Required level: {_reqLevel}, Team: {_team}, Flag: {_flag}, State: {_state}";
    }
    public override IQuestState GetState() => GetNewState();
    public TState GetNewState()
    {
        TState state = new TState();
        state.Init((TDataParent)this);
        return state;
    }
    protected abstract TTracker CreateQuestTracker(UCPlayer player, ref TState state);
    public TTracker CreateQuestTracker(UCPlayer player)
    {
        TState state = GetNewState();
        TTracker tracker = CreateQuestTracker(player, ref state);
        tracker.QuestData = this;
        return tracker;
    }

    public override string ToString() =>
        $"{QuestType} quest data: {_presets?.Length ?? -1} presets, daily quest: {CanBeDailyQuest}, translations: {Translations?.FirstOrDefault().Value ?? "null"}.\n" +
        $"    Presets: {(_presets == null ? "null array" : string.Join(", ", _presets.Select(x => x.ToString())))}";
    public override BaseQuestTracker CreateTracker(UCPlayer player) => CreateQuestTracker(player);
    public override BaseQuestTracker GetTracker(UCPlayer player, ref IQuestState state)
    {
        if (state is TState st2)
        {
            TTracker tracker = CreateQuestTracker(player, ref st2);
            tracker.QuestData = this;
            return tracker;
        }
        return null;
    }
    public override BaseQuestTracker GetTracker(UCPlayer player, IQuestPreset preset)
    {
        if (preset.State is TState st2)
        {
            TTracker tracker = CreateQuestTracker(player, ref st2);
            tracker.QuestData = this;
            tracker.PresetKey = preset.Key;
            tracker.Flag = preset.Flag;
            tracker.Preset = preset;
            return tracker;
        }
        return null;
    }
    public override void ReadPresets(ref Utf8JsonReader reader)
    {
        List<Preset> presets = new List<Preset>(4);
        while (reader.Read() && reader.TokenType != JsonTokenType.EndArray)
        {
            if (reader.TokenType == JsonTokenType.StartObject)
            {
                Guid key = default;
                ulong varTeam = default;
                int reqLvl = default;
                TState state = default;
                ushort flag = 0;
                while (reader.Read() && reader.TokenType == JsonTokenType.PropertyName)
                {
                    string prop = reader.GetString();
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
                    else if (reqLvl == default && prop.Equals("required_level"))
                    {
                        if (reader.TokenType == JsonTokenType.Number)
                        {
                            if (!reader.TryGetInt32(out reqLvl))
                                L.LogWarning("Failed to parse 'required_level' Int32 from " + QuestType + " preset.");
                        }
                        else
                            L.LogWarning("Failed to parse 'required_level' Int32 from " + QuestType + " preset.");
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
                                    string prop2 = reader.GetString();
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
                for (int i = 0; i < presets.Count; i++)
                {
                    Preset pr = presets[i];
                    if (pr.Key == key && varTeam == pr.Team)
                        goto next;
                }
                presets.Add(new Preset(key, reqLvl, state, varTeam, flag));
                next:
                while (reader.TokenType != JsonTokenType.EndObject && reader.Read()) ;
            }
        }
        _presets = presets.ToArray();
        presets.Clear();
    }
}

/// <summary>Base class used to track information about a player's progress in a quest. One per player per quest.
/// <para>Implement children of <see cref="INotifyTracker"/> to listen to events. If <see cref="BaseQuestData.TickFrequencySeconds"/> is > 0, the tick function will run as often as specified.</para></summary>
public abstract class BaseQuestTracker : IDisposable, INotifyTracker
{
    protected readonly UCPlayer _player;
    public UCPlayer Player => _player;
    public BaseQuestData? QuestData;
    public IQuestPreset? Preset;
    protected bool isDisposed;
    private bool _isComplete;
    protected abstract bool CompletedCheck { get; }
    public bool IsDailyQuest = false;
    public ushort Flag = 0;
    public bool IsCompleted { get => _isComplete || CompletedCheck; }
    public virtual short FlagValue => 0;
    public Guid PresetKey;
    public BaseQuestTracker(UCPlayer target)
    {
        this._player = target;
    }
    public virtual void Tick() { }
    protected virtual void Cleanup() { }
    public virtual void ResetToDefaults() { }
    public abstract string Translate();
    public abstract void WriteQuestProgress(Utf8JsonWriter writer);
    public abstract void OnReadProgressSaveProperty(string property, ref Utf8JsonReader reader);
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
        QuestManager.OnQuestCompleted(this);
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

    public void SaveProgresss() => QuestManager.SaveProgress(this, Preset.Team);
}