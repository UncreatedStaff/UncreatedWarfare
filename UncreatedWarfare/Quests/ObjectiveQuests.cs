using System;
using System.Linq;
using System.Text.Json;
using Uncreated.Warfare.Squads;

namespace Uncreated.Warfare.Quests.Types;


[QuestData(QuestType.CaptureObjectives)]
public class CaptureObjectivesQuest : BaseQuestData<CaptureObjectivesQuest.Tracker, CaptureObjectivesQuest.State, CaptureObjectivesQuest>
{
    public DynamicIntegerValue ObjectiveCount;
    public override int TickFrequencySeconds => 0;
    protected override Tracker CreateQuestTracker(UCPlayer? player, ref State state, IQuestPreset? preset) => new Tracker(this, player, ref state, preset);
    public override void OnPropertyRead(string propertyname, ref Utf8JsonReader reader)
    {
        if (propertyname.Equals("objective_count", StringComparison.Ordinal))
        {
            if (!reader.TryReadIntegralValue(out ObjectiveCount))
                ObjectiveCount = new DynamicIntegerValue(10);
        }
    }
    public struct State : IQuestState<CaptureObjectivesQuest>
    {
        [RewardField("a")]
        public DynamicIntegerValue.Choice ObjectiveCount;

        public readonly DynamicIntegerValue.Choice FlagValue => ObjectiveCount;
        public readonly bool IsEligable(UCPlayer player) => true;
        public void CreateFromTemplate(CaptureObjectivesQuest data)
        {
            ObjectiveCount = data.ObjectiveCount.GetValue();
        }
        public void OnPropertyRead(ref Utf8JsonReader reader, string prop)
        {
            if (prop.Equals("objective_count", StringComparison.Ordinal))
                ObjectiveCount = DynamicIntegerValue.ReadChoice(ref reader);
        }
        public readonly void WriteQuestState(Utf8JsonWriter writer)
        {
            writer.WriteProperty("objective_count", ObjectiveCount);
        }
    }
    public class Tracker(BaseQuestData data, UCPlayer? target, ref State questState, IQuestPreset? preset) : BaseQuestTracker(data, target, questState, preset), INotifyOnObjectiveCaptured
    {
        private readonly int _objectiveCount = questState.ObjectiveCount.InsistValue();
        private int _captures;
        public override short FlagValue => (short)_captures;
        protected override bool CompletedCheck => _captures >= _objectiveCount;

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
                if (participants[i] == Player!.Steam64)
                {
                    _captures++;
                    if (_captures >= _objectiveCount)
                        TellCompleted();
                    else
                        TellUpdated();
                    return;
                }
            }
        }
        protected override string Translate(bool forAsset) => QuestData.Translate(forAsset, Player!, _captures, _objectiveCount);
        public override void ManualComplete()
        {
            _captures = _objectiveCount;
            base.ManualComplete();
        }
    }
}

[QuestData(QuestType.XPInGamemode)]
public class XPInGamemodeQuest : BaseQuestData<XPInGamemodeQuest.Tracker, XPInGamemodeQuest.State, XPInGamemodeQuest>
{
    public DynamicIntegerValue XPCount;
    public DynamicEnumValue<GamemodeType> Gamemode;
    public DynamicIntegerValue NumberOfGames;
    public override int TickFrequencySeconds => 0;
    protected override Tracker CreateQuestTracker(UCPlayer? player, ref State state, IQuestPreset? preset) => new Tracker(this, player, ref state, preset);
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
                Gamemode = new DynamicEnumValue<GamemodeType>(new EnumRange<GamemodeType>(GamemodeType.TeamCTF, GamemodeType.Insurgency), ChoiceBehavior.Selective);
        }
        else if (propertyname.Equals("game_count", StringComparison.Ordinal))
        {
            if (!reader.TryReadIntegralValue(out XPCount))
                XPCount = new DynamicIntegerValue(1);
        }
    }
    public struct State : IQuestState<XPInGamemodeQuest>
    {
        internal DynamicEnumValue<GamemodeType>.Choice Gamemode;

        [RewardField("xp")]
        public DynamicIntegerValue.Choice XPCount;

        [RewardField("games")]
        public DynamicIntegerValue.Choice GameCount;

        public readonly DynamicIntegerValue.Choice FlagValue => GameCount;
        public readonly bool IsEligable(UCPlayer player) => true;
        public void CreateFromTemplate(XPInGamemodeQuest data)
        {
            XPCount = data.XPCount.GetValue();
            Gamemode = data.Gamemode.GetValue();
            GameCount = data.NumberOfGames.GetValue();
        }
        public void OnPropertyRead(ref Utf8JsonReader reader, string prop)
        {
            if (prop.Equals("xp_goal", StringComparison.Ordinal))
                XPCount = DynamicIntegerValue.ReadChoice(ref reader);
            else if (prop.Equals("gamemode", StringComparison.Ordinal))
                Gamemode = DynamicEnumValue<GamemodeType>.ReadChoice(ref reader);
            else if (prop.Equals("game_count", StringComparison.Ordinal))
                GameCount = DynamicIntegerValue.ReadChoice(ref reader);
        }
        public readonly void WriteQuestState(Utf8JsonWriter writer)
        {
            writer.WriteProperty("xp_goal", XPCount);
            writer.WriteProperty("gamemode", Gamemode);
            writer.WriteProperty("game_count", GameCount);
        }
    }
    public class Tracker(BaseQuestData data, UCPlayer? target, ref State questState, IQuestPreset? preset) : BaseQuestTracker(data, target, questState, preset), INotifyGameOver, INotifyGainedXP
    {
        private readonly int _xpCount = questState.XPCount.InsistValue();
        private readonly DynamicEnumValue<GamemodeType>.Choice _gamemode = questState.Gamemode;
        private readonly int _gameCount = questState.GameCount.InsistValue();
        private int _currentXp;
        private int _gamesCompleted;
        public override short FlagValue => (short)_gamesCompleted;
        protected override bool CompletedCheck => _gamesCompleted >= _gameCount;
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
            if (_gamemode.IsMatch(Data.Gamemode.GamemodeType))
            {
                if (Data.Is(out IGameStats st) && st.GameStats is BaseStatTracker<BasePlayerStats> st2)
                {
                    for (int i = 0; i < st2.stats.Count; i++)
                    {
                        if (st2.stats[i].Steam64 == Player!.Steam64 && st2.stats[i] is IExperienceStats exp4)
                        {
                            _currentXp = exp4.XPGained;
                            break;
                        }
                    }
                }
                if (_currentXp >= _xpCount)
                {
                    _gamesCompleted++;
                    if (_gamesCompleted >= _gameCount)
                        TellCompleted();
                    else
                        TellUpdated();
                }
            }
        }
        public void OnGainedXP(UCPlayer player, int amtGained, int total, int gameTotal)
        {
            if (_gamemode.IsMatch(Data.Gamemode.GamemodeType))
            {
                if (Player!.Steam64 == player.Steam64)
                    _currentXp = gameTotal;
            }
        }
        protected override string Translate(bool forAsset) => QuestData.Translate(forAsset, Player!, _currentXp, _xpCount);
        public override void ManualComplete()
        {
            _currentXp = 0;
            _gamesCompleted = _gameCount;
            base.ManualComplete();
        }
    }
}
[QuestData(QuestType.TeammatesDeployOnRally)]
public class RallyUseQuest : BaseQuestData<RallyUseQuest.Tracker, RallyUseQuest.State, RallyUseQuest>
{
    public DynamicIntegerValue UseCount;
    public override int TickFrequencySeconds => 0;
    protected override Tracker CreateQuestTracker(UCPlayer? player, ref State state, IQuestPreset? preset) => new Tracker(this, player, ref state, preset);
    public override void OnPropertyRead(string propertyname, ref Utf8JsonReader reader)
    {
        if (propertyname.Equals("deployments", StringComparison.Ordinal))
        {
            if (!reader.TryReadIntegralValue(out UseCount))
                UseCount = new DynamicIntegerValue(10);
        }
    }
    public struct State : IQuestState<RallyUseQuest>
    {
        [RewardField("a")]
        public DynamicIntegerValue.Choice UseCount;

        public readonly DynamicIntegerValue.Choice FlagValue => UseCount;
        public void CreateFromTemplate(RallyUseQuest data)
        {
            UseCount = data.UseCount.GetValue();
        }
        public readonly bool IsEligable(UCPlayer player) => true;

        public void OnPropertyRead(ref Utf8JsonReader reader, string prop)
        {
            if (prop.Equals("deployments", StringComparison.Ordinal))
                UseCount = DynamicIntegerValue.ReadChoice(ref reader);
        }
        public readonly void WriteQuestState(Utf8JsonWriter writer)
        {
            writer.WriteProperty("deployments", UseCount);
        }
    }
    public class Tracker(BaseQuestData data, UCPlayer? target, ref State questState, IQuestPreset? preset) : BaseQuestTracker(data, target, questState, preset), INotifyRallyActive
    {
        private readonly int _useCount = questState.UseCount.InsistValue();
        private int _rallyUses;
        protected override bool CompletedCheck => _rallyUses >= _useCount;
        public override short FlagValue => (short)_rallyUses;

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
            if (rally.Squad.Leader?.Steam64 == Player!.Steam64)
            {
                _rallyUses += rally.AwaitingPlayers.Count;
                if (_rallyUses >= _useCount)
                    TellCompleted();
                else
                    TellUpdated();
            }
        }
        protected override string Translate(bool forAsset) => QuestData.Translate(forAsset, Player!, _rallyUses, _useCount);
        public override void ManualComplete()
        {
            _rallyUses = _useCount;
            base.ManualComplete();
        }
    }
}
[QuestData(QuestType.WinGamemode)]
public class WinGamemodeQuest : BaseQuestData<WinGamemodeQuest.Tracker, WinGamemodeQuest.State, WinGamemodeQuest>
{
    private const GamemodeType MaxGamemode = GamemodeType.Deathmatch;

    public DynamicIntegerValue WinCount;

    public DynamicEnumValue<GamemodeType> Gamemode = new DynamicEnumValue<GamemodeType>(new EnumRange<GamemodeType>(GamemodeType.TeamCTF, MaxGamemode), ChoiceBehavior.Selective);
    public override int TickFrequencySeconds => 0;
    protected override Tracker CreateQuestTracker(UCPlayer? player, ref State state, IQuestPreset? preset) => new Tracker(this, player, ref state, preset);
    public override void OnPropertyRead(string propertyname, ref Utf8JsonReader reader)
    {
        if (propertyname.Equals("gamemode", StringComparison.Ordinal))
        {
            if (!reader.TryReadEnumValue(out Gamemode))
                Gamemode = new DynamicEnumValue<GamemodeType>(new EnumRange<GamemodeType>(GamemodeType.TeamCTF, MaxGamemode), ChoiceBehavior.Selective);
        }
        else if (propertyname.Equals("wins", StringComparison.Ordinal))
        {
            if (!reader.TryReadIntegralValue(out WinCount))
                WinCount = new DynamicIntegerValue(10);
        }
    }
    public struct State : IQuestState<WinGamemodeQuest>
    {
        [RewardField("w")]
        public DynamicIntegerValue.Choice Wins;

        public DynamicEnumValue<GamemodeType>.Choice Gamemode;
        
        public readonly DynamicIntegerValue.Choice FlagValue => Wins;
        public void CreateFromTemplate(WinGamemodeQuest data)
        {
            // get enabled gamemode based on weight.
            Type gamemodeType = Gamemodes.Gamemode.GetNextGamemode() ?? typeof(TeamCTF);
            GamemodeType type = Gamemodes.Gamemode.Gamemodes.FirstOrDefault(x => x.Value == gamemodeType).Key;
            if (type == GamemodeType.Undefined)
                type = GamemodeType.TeamCTF;

            Gamemode = new DynamicEnumValue<GamemodeType>.Choice(new DynamicEnumValue<GamemodeType>(type));
            Wins = data.WinCount.GetValue();
        }
        public readonly bool IsEligable(UCPlayer player) => true;

        public void OnPropertyRead(ref Utf8JsonReader reader, string prop)
        {
            if (prop.Equals("gamemode", StringComparison.Ordinal))
                Gamemode = DynamicEnumValue<GamemodeType>.ReadChoice(ref reader);
            else if (prop.Equals("wins", StringComparison.Ordinal))
                Wins = DynamicIntegerValue.ReadChoice(ref reader);
        }
        public readonly void WriteQuestState(Utf8JsonWriter writer)
        {
            writer.WriteProperty("gamemode", Gamemode);
            writer.WriteProperty("wins", Wins);
        }
    }
    public class Tracker(BaseQuestData data, UCPlayer? target, ref State questState, IQuestPreset? preset) : BaseQuestTracker(data, target, questState, preset), INotifyGameOver
    {
        internal readonly DynamicEnumValue<GamemodeType>.Choice Gamemode = questState.Gamemode;
        public readonly int WinCount = questState.Wins.InsistValue();
        private int _wins;
        protected override bool CompletedCheck => _wins >= WinCount;
        public override short FlagValue => (short)_wins;

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
            ulong team = Player!.GetTeam();
            if ((winner == 0 || winner == team) && Gamemode.IsMatch(Data.Gamemode.GamemodeType))
            {
                if (Data.Is(out IGameStats st) && st.GameStats != null)
                {
                    bool award = false;
                    object? stats = st.GameStats.GetPlayerStats(Player!.Steam64);
                    if (winner != 0 && stats is ITeamPresenceStats tps)
                    {
                        if (st.GameStats.GetPresence(tps, winner) > Gamemodes.Gamemode.MatchPresentThreshold)
                            award = true;
                    }
                    else if (stats is IPresenceStats ps)
                    {
                        if (st.GameStats.GetPresence(ps) > Gamemodes.Gamemode.MatchPresentThreshold && Data.Gamemode.IsWinner(Player!))
                            award = true;
                    }
                    if (award)
                    {
                        _wins++;
                        if (_wins >= WinCount)
                            TellCompleted();
                        else
                            TellUpdated();
                    }
                }
            }
        }
        protected override string Translate(bool forAsset) => QuestData.Translate(forAsset, Player!, _wins, WinCount, Gamemode.ToString());
        public override void ManualComplete()
        {
            _wins = WinCount;
            base.ManualComplete();
        }
    }
}

[QuestData(QuestType.NeutralizeFlags)]
public class NeutralizeFlagsQuest : BaseQuestData<NeutralizeFlagsQuest.Tracker, NeutralizeFlagsQuest.State, NeutralizeFlagsQuest>
{
    public DynamicIntegerValue Neutralizations;
    public override int TickFrequencySeconds => 0;
    protected override Tracker CreateQuestTracker(UCPlayer? player, ref State state, IQuestPreset? preset) => new Tracker(this, player, ref state, preset);
    public override void OnPropertyRead(string propertyname, ref Utf8JsonReader reader)
    {
        if (propertyname.Equals("neutralizations", StringComparison.Ordinal))
        {
            if (!reader.TryReadIntegralValue(out Neutralizations))
                Neutralizations = new DynamicIntegerValue(10);
        }
    }
    public struct State : IQuestState<NeutralizeFlagsQuest>
    {
        [RewardField("n")]
        public DynamicIntegerValue.Choice Neutralizations;

        public readonly DynamicIntegerValue.Choice FlagValue => Neutralizations;
        public void CreateFromTemplate(NeutralizeFlagsQuest data)
        {
            Neutralizations = data.Neutralizations.GetValue();
        }
        public readonly bool IsEligable(UCPlayer player) => true;

        public void OnPropertyRead(ref Utf8JsonReader reader, string prop)
        {
            if (prop.Equals("neutralizations", StringComparison.Ordinal))
                Neutralizations = DynamicIntegerValue.ReadChoice(ref reader);
        }
        public readonly void WriteQuestState(Utf8JsonWriter writer)
        {
            writer.WriteProperty("neutralizations", Neutralizations);
        }
    }
    public class Tracker(BaseQuestData data, UCPlayer? target, ref State questState, IQuestPreset? preset) : BaseQuestTracker(data, target, questState, preset), INotifyOnFlagNeutralized
    {
        public readonly int Neutralizations = questState.Neutralizations.InsistValue();
        private int _neutralizations;
        protected override bool CompletedCheck => _neutralizations >= Neutralizations;
        public override short FlagValue => (short)_neutralizations;

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
            if (Player!.GetTeam() != neutralizer)
                return;
            
            for (int i = 0; i < participants.Length; i++)
            {
                if (participants[i] != Player!.Steam64)
                    continue;

                _neutralizations++;
                if (_neutralizations >= Neutralizations)
                    TellCompleted();
                else
                    TellUpdated();
                return;
            }
        }
        protected override string Translate(bool forAsset) => QuestData.Translate(forAsset, Player!, _neutralizations, Neutralizations);
        public override void ManualComplete()
        {
            _neutralizations = Neutralizations;
            base.ManualComplete();
        }
    }
}
