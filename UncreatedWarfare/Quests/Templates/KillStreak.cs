using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Text.Json;
using System.Text.Json.Serialization;
using Uncreated.Warfare.Configuration;
using Uncreated.Warfare.Events.Models;
using Uncreated.Warfare.Events.Models.Players;
using Uncreated.Warfare.Players;
using Uncreated.Warfare.Quests.Parameters;
using Uncreated.Warfare.Translations;
using Uncreated.Warfare.Util;

namespace Uncreated.Warfare.Quests.Templates;
public class KillStreak : QuestTemplate<KillStreak, KillStreak.Tracker, KillStreak.State>
{
    public Int32ParameterTemplate StreakCount { get; set; }
    public Int32ParameterTemplate StreakLength { get; set; }
    public KillStreak(IConfiguration templateConfig, IServiceProvider serviceProvider) : base(templateConfig, serviceProvider) { }
    public class State : IQuestState<KillStreak>
    {
        [JsonIgnore]
        public string Text { get; set; }

        [RewardVariable("strNum")]
        public QuestParameterValue<int> StreakCount { get; set; }

        [RewardVariable("strLen")]
        public QuestParameterValue<int> StreakLength { get; set; }

        [JsonIgnore]
        public QuestParameterValue<int> FlagValue => StreakCount;
        public UniTask CreateFromConfigurationAsync(IQuestStateConfiguration configuration, KillStreak template, IServiceProvider serviceProvider, CancellationToken token)
        {
            StreakCount = configuration.ParseInt32Value("StreakCount", Int32ParameterTemplate.WildcardInclusive);
            StreakLength = configuration.ParseInt32Value("StreakLength", Int32ParameterTemplate.WildcardInclusive);

            FormatText(template);
            return UniTask.CompletedTask;
        }
        public async UniTask CreateFromTemplateAsync(KillStreak template, CancellationToken token)
        {
            StreakCount = await template.StreakCount.CreateValue(template.ServiceProvider);

            StreakLength = await template.StreakLength.CreateValue(template.ServiceProvider);

            FormatText(template);
        }

        private void FormatText(KillStreak template)
        {
            ITranslationValueFormatter formatter = template.ServiceProvider.GetRequiredService<ITranslationValueFormatter>();

            Text = string.Format(template.Text.Translate(null, template.Type.Name),
                "{0}",
                StreakCount.GetDisplayString(formatter),
                StreakLength.GetDisplayString(formatter)
            );
        }

        /// <inheritdoc />
        public string CreateQuestDescriptiveString()
        {
            return Text;
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
        public Tracker(WarfarePlayer player, IServiceProvider serviceProvider, KillStreak quest, State state, IQuestPreset? preset)
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

            if (e.Instigator.m_SteamID != Player.Steam64.m_SteamID || !e.WasEffectiveKill || e.Cause == EDeathCause.SHRED)
                return;

            _streakProgress++;
            if (_streakProgress < _streakLength)
                return;
            
            ++_streaksCompleted;
            _streakProgress = 0;
            InvokeUpdate();
        }

        public override void WriteProgress(Utf8JsonWriter writer)
        {
            writer.WriteNumber("CurrentStreakProgress", _streakProgress);
            writer.WriteNumber("StreaksCompleted", _streaksCompleted);
        }

        public override void ReadProgress(ref Utf8JsonReader reader)
        {
            JsonUtility.ReadTopLevelProperties(ref reader, (ref Utf8JsonReader reader, string property, ref object? _) =>
            {
                if (property.Equals("CurrentStreakProgress", StringComparison.Ordinal))
                {
                    _streakProgress = reader.GetInt32();
                }
                else if (property.Equals("StreaksCompleted", StringComparison.Ordinal))
                {
                    _streaksCompleted = reader.GetInt32();
                }
            });
        }
    }
}