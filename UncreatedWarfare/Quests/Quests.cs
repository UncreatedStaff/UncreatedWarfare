using SDG.Unturned;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Uncreated.Warfare.Kits;

namespace Uncreated.Warfare.Quests.Types;
/// <summary>Stores information about a <see cref="EQuestType"/> of kit. Isn't necessarily constant, some can have ranges that are used for daily quests.
/// Rank and kit quests should override with a set <see cref="IQuestState{TTracker, TDataNew}"/>.</summary>
public abstract class BaseQuestData
{
    private EQuestType _type;
    private Dictionary<string, string> _translations;
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
}

/// <inheritdoc/>
public abstract class BaseQuestData<TTracker, TState, TDataParent> : BaseQuestData where TTracker : BaseQuestTracker where TState : IQuestState<TTracker, TDataParent>, new() where TDataParent : BaseQuestData<TTracker, TState, TDataParent>
{
    public override IQuestState GetState() => GetNewState();
    public TState GetNewState()
    {
        TState state = new TState();
        state.Init((TDataParent)this);
        return state;
    }
    public abstract TTracker CreateQuestTracker(UCPlayer player, ref TState state);
    public TTracker CreateQuestTracker(UCPlayer player)
    {
        TState state = GetNewState();
        return CreateQuestTracker(player, ref state);
    }
    public override BaseQuestTracker CreateTracker(UCPlayer player) => CreateQuestTracker(player);
    public override BaseQuestTracker GetTracker(UCPlayer player, ref IQuestState state) => state is TState st2 ? CreateQuestTracker(player, ref st2) : null;
}

/// <summary>Base class used to track information about a player's progress in a quest. One per player per quest.
/// <para>Implement children of <see cref="INotifyTracker"/> to listen to events. If <see cref="BaseQuestData.TickFrequencySeconds"/> is > 0, the tick function will run as often as specified.</para></summary>
public abstract class BaseQuestTracker : IDisposable, INotifyTracker
{
    protected readonly UCPlayer _player;
    public UCPlayer Player => _player;
    public BaseQuestData QuestData;
    protected bool isDisposed;
    protected bool _isCompleted;
    public bool IsDailyQuest = false;
    public bool IsCompleted { get; }
    public BaseQuestTracker(UCPlayer target)
    {
        this._player = target;
    }
    public virtual void Tick() { }
    protected virtual void Cleanup() { }
    public virtual void ResetToDefaults() { }
    public abstract string Translate();
    public void OnGameEnd()
    {
        if (QuestData.ResetOnGameEnd)
        {
            _isCompleted = false;
            ResetToDefaults();
        }
    }
    public void TellCompleted()
    {
        TellUpdated();
        _isCompleted = true;
        QuestManager.OnQuestCompleted(this);
    }
    public void TellUpdated()
    {
        QuestManager.OnQuestUpdated(this);
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
}