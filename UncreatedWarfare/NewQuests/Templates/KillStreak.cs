using Cysharp.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using System;
using System.Text.Json;
using System.Threading;
using Uncreated.Warfare.Events;
using Uncreated.Warfare.Events.Players;
using Uncreated.Warfare.Exceptions;
using Uncreated.Warfare.NewQuests.Parameters;
using Uncreated.Warfare.Quests;
using Uncreated.Warfare.Util;

namespace Uncreated.Warfare.NewQuests.Templates;
public class KillStreak : QuestTemplate<KillStreak, KillStreak.Tracker, KillStreak.State>
{
    public Int32ParameterTemplate StreakCount { get; set; }
    public Int32ParameterTemplate StreakLength { get; set; }
    public KillStreak(IConfiguration templateConfig, IServiceProvider serviceProvider) : base(templateConfig, serviceProvider) { }
    public class State : BaseState
    {
        [RewardField("strNum")]
        public QuestParameterValue<int> StreakCount { get; set; }

        [RewardField("strLen")]
        public QuestParameterValue<int> StreakLength { get; set; }
        public override QuestParameterValue<int> FlagValue => StreakCount;
        public override UniTask CreateFromConfigurationAsync(IConfiguration configuration, IServiceProvider serviceProvider, CancellationToken token)
        {
            string? streakCountStr = configuration["StreakCount"],
                    streakLenStr = configuration["StreakLength"];

            if (string.IsNullOrEmpty(streakCountStr) || !Int32ParameterTemplate.TryParseValue(streakCountStr, out QuestParameterValue<int>? streakCount))
                throw new QuestConfigurationException(typeof(KillStreak), "Failed to parse integer parameter for \"StreakCount\".");
            
            if (string.IsNullOrEmpty(streakLenStr) || !Int32ParameterTemplate.TryParseValue(streakLenStr, out QuestParameterValue<int>? streakLength))
                throw new QuestConfigurationException(typeof(KillStreak), "Failed to parse integer parameter for \"StreakLength\".");

            StreakCount = streakCount;
            StreakLength = streakLength;
            return UniTask.CompletedTask;
        }
        public override async UniTask CreateFromTemplateAsync(KillStreak data, CancellationToken token)
        {
            StreakCount = await data.StreakCount.CreateValue(data.ServiceProvider);

            StreakLength = await data.StreakLength.CreateValue(data.ServiceProvider);
        }
    }
    public class Tracker : QuestTracker, IEventListener<PlayerDied>
    {
        private readonly int _targetStreaksCompleted;
        private readonly int _streakLength;
        private int _streakProgress;

        private int _streaksCompleted;
        public override bool IsComplete => _streaksCompleted >= _targetStreaksCompleted;
        public override short FlagValue => (short)_streaksCompleted;
        public Tracker(UCPlayer player, IServiceProvider serviceProvider, KillStreak quest, State state, IQuestPreset? preset)
            : base(player, serviceProvider, quest, state, preset)
        {
            _targetStreaksCompleted = state.StreakCount.GetSingleValueOrMinimum();
            _streakLength = state.StreakLength.GetSingleValueOrMinimum();
        }

        void IEventListener<PlayerDied>.HandleEvent(PlayerDied e, IServiceProvider serviceProvider)
        {
            // reset streak
            if (e.MatchPlayer(Player))
            {
                _streakProgress = 0;
                return;
            }

            if (e.Instigator.m_SteamID != Player.Steam64 || !e.WasEffectiveKill || e.Cause == EDeathCause.SHRED)
                return;

            _streakProgress++;
            if (_streakProgress < _streakLength)
                return;
            
            ++_streaksCompleted;
            _streakProgress = 0;
            InvokeUpdate();
        }

        protected override void WriteProgress(Utf8JsonWriter writer)
        {
            writer.WriteNumber("current_streak_progress", _streakProgress);
            writer.WriteNumber("streaks_completed", _streaksCompleted);
        }

        protected override void ReadProgress(ref Utf8JsonReader reader)
        {
            JsonUtility.ReadTopLevelProperties(ref reader, (ref Utf8JsonReader reader, string property, ref object? _) =>
            {
                if (property.Equals("current_streak_progress", StringComparison.Ordinal))
                {
                    _streakProgress = reader.GetInt32();
                }
                else if (property.Equals("streaks_completed", StringComparison.Ordinal))
                {
                    _streaksCompleted = reader.GetInt32();
                }
            });
        }
    }
}