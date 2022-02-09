using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Uncreated.Warfare.Gamemodes;
using Uncreated.Warfare.Gamemodes.Interfaces;

namespace Uncreated.Warfare.Quests.Types;


[QuestData(EQuestType.CAPTURE_OBJECTIVES)]
public class CaptureObjectivesQuest : BaseQuestData<CaptureObjectivesQuest.Tracker, CaptureObjectivesQuest.State, CaptureObjectivesQuest>
{
    public DynamicIntegerValue ObjectiveCount;
    public override int TickFrequencySeconds => 0;
    public override Tracker CreateQuestTracker(UCPlayer player, ref State state) => new Tracker(player, ref state);
    public override void OnPropertyRead(string propertyname, ref Utf8JsonReader reader)
    {
        if (propertyname.Equals("objective_count", StringComparison.Ordinal))
        {
            if (!reader.TryReadIntegralValue(out ObjectiveCount))
                ObjectiveCount = new DynamicIntegerValue(10);
        }
    }
    public struct State : IQuestState<Tracker, CaptureObjectivesQuest>
    {
        public IDynamicValue<int>.IChoice ObjectiveCount;
        public void Init(CaptureObjectivesQuest data)
        {
            this.ObjectiveCount = data.ObjectiveCount.GetValue();
        }
        public void OnPropertyRead(ref Utf8JsonReader reader, string prop)
        {
            if (prop.Equals("objective_count", StringComparison.Ordinal))
                ObjectiveCount = DynamicIntegerValue.ReadChoice(ref reader);
        }
        public void WriteQuestState(Utf8JsonWriter writer)
        {
            writer.WriteProperty("objective_count", ObjectiveCount);
        }
    }
    public class Tracker : BaseQuestTracker, INotifyOnObjectiveCaptured
    {
        private readonly int ObjectiveCount = 0;
        private int _captures;
        public Tracker(UCPlayer target, ref State questState) : base(target)
        {
            ObjectiveCount = questState.ObjectiveCount.InsistValue();
        }
        public override void OnReadProgressSaveProperty(string prop, ref Utf8JsonReader reader)
        {
            if (reader.TokenType == JsonTokenType.Number && prop.Equals("objectives_captured", StringComparison.Ordinal))
                _captures = reader.GetInt32();
        }
        public override void WriteQuestProgress(Utf8JsonWriter writer)
        {
            writer.WriteProperty("objectives_captured", _captures);
        }
        public override void ResetToDefaults() => _captures = 0;
        public void OnObjectiveCaptured(ulong[] participants)
        {
            for (int i = 0; i < participants.Length; i++)
            {
                if (participants[i] == _player.Steam64)
                {
                    _captures++;
                    if (_captures >= ObjectiveCount)
                        TellCompleted();
                    else
                        TellUpdated();
                    return;
                }
            }
        }
        public override string Translate() => QuestData.Translate(_player, _captures, ObjectiveCount);
    }
}

[QuestData(EQuestType.XP_IN_GAMEMODE)]
public class XPInGamemodeQuest : BaseQuestData<XPInGamemodeQuest.Tracker, XPInGamemodeQuest.State, XPInGamemodeQuest>
{
    public DynamicIntegerValue XPCount;
    public DynamicEnumValue<EGamemode> Gamemode;
    public override int TickFrequencySeconds => 0;
    public override Tracker CreateQuestTracker(UCPlayer player, ref State state) => new Tracker(player, ref state);
    public override void OnPropertyRead(string propertyname, ref Utf8JsonReader reader)
    {
        if (propertyname.Equals("xp_required", StringComparison.Ordinal))
        {
            if (!reader.TryReadIntegralValue(out XPCount))
                XPCount = new DynamicIntegerValue(10);
        }
        else if (propertyname.Equals("gamemode", StringComparison.Ordinal))
        {
            if (!reader.TryReadEnumValue(out Gamemode))
                Gamemode = new DynamicEnumValue<EGamemode>(new EnumRange<EGamemode>(EGamemode.TEAM_CTF, EGamemode.INSURGENCY), EChoiceBehavior.ALLOW_ONE);
        }
    }
    public struct State : IQuestState<Tracker, XPInGamemodeQuest>
    {
        public IDynamicValue<int>.IChoice XPCount;
        public IDynamicValue<EGamemode>.IChoice Gamemode;
        public void Init(XPInGamemodeQuest data)
        {
            this.XPCount = data.XPCount.GetValue();
            this.Gamemode = data.Gamemode.GetValue();
        }
        public void OnPropertyRead(ref Utf8JsonReader reader, string prop)
        {
            if (prop.Equals("xp_required", StringComparison.Ordinal))
                XPCount = DynamicIntegerValue.ReadChoice(ref reader);
            else if (prop.Equals("gamemode", StringComparison.Ordinal))
                Gamemode = DynamicEnumValue<EGamemode>.ReadChoice(ref reader);
        }
        public void WriteQuestState(Utf8JsonWriter writer)
        {
            writer.WriteProperty("xp_required", XPCount);
            writer.WriteProperty("gamemode", Gamemode);
        }
    }
    public class Tracker : BaseQuestTracker, INotifyGameOver, INotifyGainedXP
    {
        private readonly int ObjectiveCount = 0;
        public IDynamicValue<EGamemode>.IChoice Gamemode;
        private int _currentXp;
        public Tracker(UCPlayer target, ref State questState) : base(target)
        {
            ObjectiveCount = questState.XPCount.InsistValue();
            Gamemode = questState.Gamemode;
        }
        public override void OnReadProgressSaveProperty(string prop, ref Utf8JsonReader reader) { }
        public override void WriteQuestProgress(Utf8JsonWriter writer) { }
        public override void ResetToDefaults() => _currentXp = 0;
        public void OnGameOver(ulong winner)
        {
            if (Gamemode.IsMatch(Data.Gamemode.GamemodeType))
            {
                if (Data.Is(out IGameStats st) && st.GameStats is BaseStatTracker<BasePlayerStats> st2 && st2.stats.TryGetValue(_player.Steam64, out BasePlayerStats st3) && st3 is IExperienceStats exp4)
                    _currentXp = exp4.XPGained;
                if (_currentXp >= ObjectiveCount)
                    TellCompleted();
                else
                    TellUpdated();
            }
        }
        public void OnGainedXP(UCPlayer player, int amtGained, int total, int gameTotal, EBranch branch)
        {
            if (Gamemode.IsMatch(Data.Gamemode.GamemodeType))
            {
                if (_player.Steam64 == player.Steam64)
                    _currentXp = gameTotal;
            }
        }
        public override string Translate() => QuestData.Translate(_player, _currentXp, ObjectiveCount);
    }
}
