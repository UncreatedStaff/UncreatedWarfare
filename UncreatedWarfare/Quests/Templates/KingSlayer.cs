using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;
using Uncreated.Warfare.Configuration;
using Uncreated.Warfare.Events.Models;
using Uncreated.Warfare.Events.Models.Players;
using Uncreated.Warfare.Players;
using Uncreated.Warfare.Players.Management;
using Uncreated.Warfare.Quests.Parameters;
using Uncreated.Warfare.Translations;
using Uncreated.Warfare.Util;

namespace Uncreated.Warfare.Quests.Templates;
public class KingSlayer : QuestTemplate<KingSlayer, KingSlayer.Tracker, KingSlayer.State>
{
    public Int32ParameterTemplate Kills { get; set; }
    public KingSlayer(IConfiguration templateConfig, IServiceProvider serviceProvider) : base(templateConfig, serviceProvider) { }
    public class State : IQuestState<KingSlayer>
    {
        [JsonIgnore]
        public string Text { get; set; }

        [RewardVariable("k")]
        public QuestParameterValue<int> Kills { get; set; }

        [JsonIgnore]
        public QuestParameterValue<int> FlagValue => Kills;
        public UniTask CreateFromConfigurationAsync(IQuestStateConfiguration configuration, KingSlayer template, IServiceProvider serviceProvider, CancellationToken token)
        {
            Kills = configuration.ParseInt32Value("Kills", Int32ParameterTemplate.WildcardInclusive);

            FormatText(template);

            return UniTask.CompletedTask;
        }
        public async UniTask CreateFromTemplateAsync(KingSlayer template, CancellationToken token)
        {
            Kills = await template.Kills.CreateValue(template.ServiceProvider);

            FormatText(template);
        }

        private void FormatText(KingSlayer template)
        {
            ITranslationValueFormatter formatter = template.ServiceProvider.GetRequiredService<ITranslationValueFormatter>();

            Text = string.Format(template.Text.Translate(null, template.Type.Name),
                "{0}",
                Kills.GetDisplayString(formatter)
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
        private readonly int _targetKills;
        private WarfarePlayer? _kingSlayer;

        private int _kills;
        public override bool IsComplete => _kills >= _targetKills;
        public override short FlagValue => (short)_kills;
        public Tracker(WarfarePlayer player, IServiceProvider serviceProvider, KingSlayer quest, State state, IQuestPreset? preset)
            : base(player, serviceProvider, quest, state, preset)
        {
            _targetKills = state.Kills.GetSingleValueOrMinimum();
        }

        void IEventListener<PlayerDied>.HandleEvent(PlayerDied e, IServiceProvider serviceProvider)
        {
            if (e.Instigator.m_SteamID != Player.Steam64.m_SteamID || !e.WasEffectiveKill || e.Cause == EDeathCause.SHRED || !e.DeadTeam.IsValid)
                return;

            WarfarePlayer? kingSlayer = serviceProvider.GetRequiredService<IPlayerService>().OnlinePlayers
                .Where(player => player.Team == e.DeadTeam)
                .Aggregate((kingSlayer, next) => next.CachedPoints.XP > kingSlayer.CachedPoints.XP ? next : kingSlayer);
            
            if (kingSlayer.Steam64 == e.Player.Steam64)
            {
                _kingSlayer = kingSlayer;
                _kills++;
                InvokeUpdate();
                return;
            }

            if (Equals(_kingSlayer, kingSlayer))
                return;

            _kingSlayer = kingSlayer;
            InvokeUpdate();
        }

        public override void WriteProgress(Utf8JsonWriter writer)
        {
            writer.WriteNumber("Kills", _kills);
        }

        public override void ReadProgress(ref Utf8JsonReader reader)
        {
            JsonUtility.ReadTopLevelProperties(ref reader, (ref Utf8JsonReader reader, string property, ref object? _) =>
            {
                if (property.Equals("Kills", StringComparison.Ordinal))
                {
                    _kills = reader.GetInt32();
                }
            });
        }
    }
}