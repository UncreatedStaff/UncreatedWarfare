using System;
using System.Text.Json;
using Uncreated.Warfare.Gamemodes;
using Uncreated.Warfare.Gamemodes.Interfaces;
using Uncreated.Warfare.Squads;

namespace Uncreated.Warfare.Quests.Types;


[QuestData(EQuestType.CAPTURE_OBJECTIVES)]
public class CaptureObjectivesQuest : BaseQuestData<CaptureObjectivesQuest.Tracker, CaptureObjectivesQuest.State, CaptureObjectivesQuest>
{
    public DynamicIntegerValue ObjectiveCount;
    public override int TickFrequencySeconds => 0;
    protected override Tracker CreateQuestTracker(UCPlayer? player, in State state, in IQuestPreset? preset) => new Tracker(this, player, in state, preset);
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
        public IDynamicValue<int>.IChoice FlagValue => ObjectiveCount;
        public bool IsEligable(UCPlayer player) => true;
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
        public override short FlagValue => (short)_captures;
        protected override bool CompletedCheck => _captures >= ObjectiveCount;
        public Tracker(BaseQuestData data, UCPlayer? target, in State questState, in IQuestPreset? preset) : base(data, target, questState, in preset)
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
        protected override string Translate(bool forAsset) => QuestData!.Translate(forAsset, _player, _captures, ObjectiveCount);
        public override void ManualComplete()
        {
            _captures = ObjectiveCount;
            base.ManualComplete();
        }
    }
}

[QuestData(EQuestType.XP_IN_GAMEMODE)]
public class XPInGamemodeQuest : BaseQuestData<XPInGamemodeQuest.Tracker, XPInGamemodeQuest.State, XPInGamemodeQuest>
{
    public DynamicIntegerValue XPCount;
    public DynamicEnumValue<EGamemode> Gamemode;
    public DynamicIntegerValue NumberOfGames;
    public override int TickFrequencySeconds => 0;
    protected override Tracker CreateQuestTracker(UCPlayer? player, in State state, in IQuestPreset? preset) => new Tracker(this, player, in state, preset);
    public override void OnPropertyRead(string propertyname, ref Utf8JsonReader reader)
    {
        if (propertyname.Equals("xp_goal", StringComparison.Ordinal))
        {
            if (!reader.TryReadIntegralValue(out XPCount))
                XPCount = new DynamicIntegerValue(10);
        }
        else if (propertyname.Equals("gamemode", StringComparison.Ordinal))
        {
            if (!reader.TryReadEnumValue(out Gamemode))
                Gamemode = new DynamicEnumValue<EGamemode>(new EnumRange<EGamemode>(EGamemode.TEAM_CTF, EGamemode.INSURGENCY), EChoiceBehavior.ALLOW_ONE);
        }
        else if (propertyname.Equals("game_count", StringComparison.Ordinal))
        {
            if (!reader.TryReadIntegralValue(out XPCount))
                XPCount = new DynamicIntegerValue(1);
        }
    }
    public struct State : IQuestState<Tracker, XPInGamemodeQuest>
    {
        public IDynamicValue<int>.IChoice XPCount;
        internal DynamicEnumValue<EGamemode>.Choice Gamemode;
        public IDynamicValue<int>.IChoice GameCount;
        public IDynamicValue<int>.IChoice FlagValue => GameCount;
        public bool IsEligable(UCPlayer player) => true;
        public void Init(XPInGamemodeQuest data)
        {
            this.XPCount = data.XPCount.GetValue();
            this.Gamemode = data.Gamemode.GetValueIntl();
            this.GameCount = data.NumberOfGames.GetValue();
        }
        public void OnPropertyRead(ref Utf8JsonReader reader, string prop)
        {
            if (prop.Equals("xp_goal", StringComparison.Ordinal))
                XPCount = DynamicIntegerValue.ReadChoice(ref reader);
            else if (prop.Equals("gamemode", StringComparison.Ordinal))
                Gamemode = DynamicEnumValue<EGamemode>.ReadChoiceIntl(ref reader);
            else if (prop.Equals("game_count", StringComparison.Ordinal))
                GameCount = DynamicIntegerValue.ReadChoice(ref reader);
        }
        public void WriteQuestState(Utf8JsonWriter writer)
        {
            writer.WriteProperty("xp_goal", XPCount);
            writer.WriteProperty("gamemode", Gamemode);
            writer.WriteProperty("game_count", GameCount);
        }
    }
    public class Tracker : BaseQuestTracker, INotifyGameOver, INotifyGainedXP
    {
        private readonly int XPCount = 0;
        private readonly DynamicEnumValue<EGamemode>.Choice Gamemode;
        private readonly int GameCount = 0;
        private int _currentXp;
        private int _gamesCompleted;
        public override short FlagValue => (short)_gamesCompleted;
        public Tracker(BaseQuestData data, UCPlayer? target, in State questState, in IQuestPreset? preset) : base(data, target, questState, in preset)
        {
            XPCount = questState.XPCount.InsistValue();
            Gamemode = questState.Gamemode;
            GameCount = questState.GameCount.InsistValue();
        }
        protected override bool CompletedCheck => _gamesCompleted >= GameCount;
        public override void OnReadProgressSaveProperty(string prop, ref Utf8JsonReader reader)
        {
            if (reader.TokenType == JsonTokenType.Number && prop.Equals("games_met_goal", StringComparison.Ordinal))
                _gamesCompleted = reader.GetInt32();
        }
        public override void WriteQuestProgress(Utf8JsonWriter writer)
        {
            writer.WriteProperty("games_met_goal", _gamesCompleted);
        }
        public override void ResetToDefaults()
        {
            _currentXp = 0;
            _gamesCompleted = 0;
        }
        public void OnGameOver(ulong winner)
        {
            if (Gamemode.IsMatch(Data.Gamemode.GamemodeType))
            {
                if (Data.Is(out IGameStats st) && st.GameStats is BaseStatTracker<BasePlayerStats> st2 && 
                    st2.stats.TryGetValue(_player.Steam64, out BasePlayerStats st3) && st3 is IExperienceStats exp4)
                    _currentXp = exp4.XPGained;
                if (_currentXp >= XPCount)
                {
                    _gamesCompleted++;
                    if (_gamesCompleted >= GameCount)
                        TellCompleted();
                    else
                        TellUpdated();
                }
            }
        }
        public void OnGainedXP(UCPlayer player, int amtGained, int total, int gameTotal)
        {
            if (Gamemode.IsMatch(Data.Gamemode.GamemodeType))
            {
                if (_player.Steam64 == player.Steam64)
                    _currentXp = gameTotal;
            }
        }
        protected override string Translate(bool forAsset) => QuestData!.Translate(forAsset, _player, _currentXp, XPCount);
        public override void ManualComplete()
        {
            _currentXp = 0;
            _gamesCompleted = GameCount;
            base.ManualComplete();
        }
    }
}
[QuestData(EQuestType.TEAMMATES_DEPLOY_ON_RALLY)]
public class RallyUseQuest : BaseQuestData<RallyUseQuest.Tracker, RallyUseQuest.State, RallyUseQuest>
{
    public DynamicIntegerValue UseCount;
    public override int TickFrequencySeconds => 0;
    protected override Tracker CreateQuestTracker(UCPlayer? player, in State state, in IQuestPreset? preset) => new Tracker(this, player, in state, preset);
    public override void OnPropertyRead(string propertyname, ref Utf8JsonReader reader)
    {
        if (propertyname.Equals("deployments", StringComparison.Ordinal))
        {
            if (!reader.TryReadIntegralValue(out UseCount))
                UseCount = new DynamicIntegerValue(10);
        }
    }
    public struct State : IQuestState<Tracker, RallyUseQuest>
    {
        public IDynamicValue<int>.IChoice UseCount;
        public IDynamicValue<int>.IChoice FlagValue => UseCount;
        public void Init(RallyUseQuest data)
        {
            this.UseCount = data.UseCount.GetValue();
        }
        public bool IsEligable(UCPlayer player) => true;

        public void OnPropertyRead(ref Utf8JsonReader reader, string prop)
        {
            if (prop.Equals("deployments", StringComparison.Ordinal))
                UseCount = DynamicIntegerValue.ReadChoice(ref reader);
        }
        public void WriteQuestState(Utf8JsonWriter writer)
        {
            writer.WriteProperty("deployments", UseCount);
        }
    }
    public class Tracker : BaseQuestTracker, INotifyRallyActive
    {
        private readonly int UseCount = 0;
        private int _rallyUses;
        protected override bool CompletedCheck => _rallyUses >= UseCount;
        public override short FlagValue => (short)_rallyUses;
        public Tracker(BaseQuestData data, UCPlayer? target, in State questState, in IQuestPreset? preset) : base(data, target, questState, in preset)
        {
            UseCount = questState.UseCount.InsistValue();
        }
        public override void OnReadProgressSaveProperty(string prop, ref Utf8JsonReader reader)
        {
            if (reader.TokenType == JsonTokenType.Number && prop.Equals("deployments", StringComparison.Ordinal))
                _rallyUses = reader.GetInt32();
        }
        public override void WriteQuestProgress(Utf8JsonWriter writer)
        {
            writer.WriteProperty("deployments", _rallyUses);
        }
        public override void ResetToDefaults() => _rallyUses = 0;
        public void OnRallyActivated(RallyPoint rally)
        {
            if (rally.squad.Leader?.Steam64 == _player.Steam64)
            {
                _rallyUses += rally.AwaitingPlayers.Count;
                if (_rallyUses >= UseCount)
                    TellCompleted();
                else
                    TellUpdated();
            }
        }
        protected override string Translate(bool forAsset) => QuestData!.Translate(forAsset, _player, _rallyUses, UseCount);
        public override void ManualComplete()
        {
            _rallyUses = UseCount;
            base.ManualComplete();
        }
    }
}
[QuestData(EQuestType.WIN_GAMEMODE)]
public class WinGamemodeQuest : BaseQuestData<WinGamemodeQuest.Tracker, WinGamemodeQuest.State, WinGamemodeQuest>
{
    private const EGamemode MAX_GAMEMODE = EGamemode.INSURGENCY;
    public DynamicIntegerValue WinCount;
    public DynamicEnumValue<EGamemode> Gamemode = new DynamicEnumValue<EGamemode>(new EnumRange<EGamemode>(EGamemode.TEAM_CTF, MAX_GAMEMODE), EChoiceBehavior.ALLOW_ONE);
    public override int TickFrequencySeconds => 0;
    protected override Tracker CreateQuestTracker(UCPlayer? player, in State state, in IQuestPreset? preset) => new Tracker(this, player, in state, preset);
    public override void OnPropertyRead(string propertyname, ref Utf8JsonReader reader)
    {
        if (propertyname.Equals("gamemode", StringComparison.Ordinal))
        {
            if (!reader.TryReadEnumValue(out Gamemode))
                Gamemode = new DynamicEnumValue<EGamemode>(new EnumRange<EGamemode>(EGamemode.TEAM_CTF, MAX_GAMEMODE), EChoiceBehavior.ALLOW_ONE);
        }
        else if (propertyname.Equals("wins", StringComparison.Ordinal))
        {
            if (!reader.TryReadIntegralValue(out WinCount))
                WinCount = new DynamicIntegerValue(10);
        }
    }
    public struct State : IQuestState<Tracker, WinGamemodeQuest>
    {
        [RewardField("w")]
        public IDynamicValue<int>.IChoice Wins;
        public IDynamicValue<EGamemode>.IChoice Gamemode;
        public IDynamicValue<int>.IChoice FlagValue => Wins;
        public void Init(WinGamemodeQuest data)
        {
            this.Gamemode = data.Gamemode.GetValue();
            this.Wins = data.WinCount.GetValue();
        }
        public bool IsEligable(UCPlayer player) => true;

        public void OnPropertyRead(ref Utf8JsonReader reader, string prop)
        {
            if (prop.Equals("gamemode", StringComparison.Ordinal))
                Gamemode = DynamicEnumValue<EGamemode>.ReadChoice(ref reader);
            else if (prop.Equals("wins", StringComparison.Ordinal))
                Wins = DynamicIntegerValue.ReadChoice(ref reader);
        }
        public void WriteQuestState(Utf8JsonWriter writer)
        {
            writer.WriteProperty("gamemode", Gamemode);
            writer.WriteProperty("wins", Wins);
        }
    }
    public class Tracker : BaseQuestTracker, INotifyGameOver
    {
        public readonly IDynamicValue<EGamemode>.IChoice Gamemode;
        public readonly int WinCount;
        private int _wins;
        protected override bool CompletedCheck => _wins >= WinCount;
        public override short FlagValue => (short)_wins;
        public Tracker(BaseQuestData data, UCPlayer? target, in State questState, in IQuestPreset? preset) : base(data, target, questState, in preset)
        {
            Gamemode = questState.Gamemode;
            WinCount = questState.Wins.InsistValue();
        }
        public override void OnReadProgressSaveProperty(string prop, ref Utf8JsonReader reader)
        {
            if (reader.TokenType == JsonTokenType.Number && prop.Equals("wins", StringComparison.Ordinal))
                _wins = reader.GetInt32();
        }
        public override void WriteQuestProgress(Utf8JsonWriter writer)
        {
            writer.WriteProperty("wins", _wins);
        }

        public override void ResetToDefaults() => _wins = 0;
        public void OnGameOver(ulong winner)
        {
            ulong team = _player.GetTeam();
            if (winner == team && Gamemode.IsMatch(Data.Gamemode.GamemodeType))
            {
                if (Data.Is(out IGameStats st) && st.GameStats is BaseStatTracker<BasePlayerStats> st2 &&
                    st2.stats.TryGetValue(_player.Steam64, out BasePlayerStats st3) && st3 is TeamPlayerStats teamstats &&
                    st2.GetPresence(teamstats, winner) > Gamemodes.Gamemode.MATCH_PRESENT_THRESHOLD)
                {
                    _wins++;
                    if (_wins >= WinCount)
                        TellCompleted();
                    else
                        TellUpdated();
                }
            }
        }
        protected override string Translate(bool forAsset) => QuestData!.Translate(forAsset, _player, _wins, WinCount, Gamemode.ToString());
        public override void ManualComplete()
        {
            _wins = WinCount;
            base.ManualComplete();
        }
    }
}

[QuestData(EQuestType.NEUTRALIZE_FLAGS)]
public class NeutralizeFlagsQuest : BaseQuestData<NeutralizeFlagsQuest.Tracker, NeutralizeFlagsQuest.State, NeutralizeFlagsQuest>
{
    public DynamicIntegerValue Neutralizations;
    public override int TickFrequencySeconds => 0;
    protected override Tracker CreateQuestTracker(UCPlayer? player, in State state, in IQuestPreset? preset) => new Tracker(this, player, in state, preset);
    public override void OnPropertyRead(string propertyname, ref Utf8JsonReader reader)
    {
        if (propertyname.Equals("neutralizations", StringComparison.Ordinal))
        {
            if (!reader.TryReadIntegralValue(out Neutralizations))
                Neutralizations = new DynamicIntegerValue(10);
        }
    }
    public struct State : IQuestState<Tracker, NeutralizeFlagsQuest>
    {
        [RewardField("n")]
        public IDynamicValue<int>.IChoice Neutralizations;
        public IDynamicValue<int>.IChoice FlagValue => Neutralizations;
        public void Init(NeutralizeFlagsQuest data)
        {
            this.Neutralizations = data.Neutralizations.GetValue();
        }
        public bool IsEligable(UCPlayer player) => true;

        public void OnPropertyRead(ref Utf8JsonReader reader, string prop)
        {
            if (prop.Equals("neutralizations", StringComparison.Ordinal))
                Neutralizations = DynamicIntegerValue.ReadChoice(ref reader);
        }
        public void WriteQuestState(Utf8JsonWriter writer)
        {
            writer.WriteProperty("neutralizations", Neutralizations);
        }
    }
    public class Tracker : BaseQuestTracker, INotifyOnFlagNeutralized
    {
        public readonly int Neutralizations;
        private int _neutralizations;
        protected override bool CompletedCheck => _neutralizations >= Neutralizations;
        public override short FlagValue => (short)_neutralizations;
        public Tracker(BaseQuestData data, UCPlayer? target, in State questState, in IQuestPreset? preset) : base(data, target, questState, in preset)
        {
            Neutralizations = questState.Neutralizations.InsistValue();
        }
        public override void OnReadProgressSaveProperty(string prop, ref Utf8JsonReader reader)
        {
            if (reader.TokenType == JsonTokenType.Number && prop.Equals("neutralizations", StringComparison.Ordinal))
                _neutralizations = reader.GetInt32();
        }
        public override void WriteQuestProgress(Utf8JsonWriter writer)
        {
            writer.WriteProperty("neutralizations", _neutralizations);
        }

        public override void ResetToDefaults() => _neutralizations = 0;
        public void OnFlagNeutralized(ulong[] participants, ulong neutralizer)
        {
            if (_player.GetTeam() == neutralizer)
            {
                for (int i = 0; i < participants.Length; i++)
                {
                    if (participants[i] == _player.Steam64)
                    {
                        _neutralizations++;
                        if (_neutralizations >= Neutralizations)
                            TellCompleted();
                        else
                            TellUpdated();
                        return;
                    }
                }
            }
        }
        protected override string Translate(bool forAsset) => QuestData!.Translate(forAsset, _player, _neutralizations, Neutralizations);
        public override void ManualComplete()
        {
            _neutralizations = Neutralizations;
            base.ManualComplete();
        }
    }
}
