using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Text.Json;
using System.Text.Json.Serialization;
using Uncreated.Warfare.Configuration;
using Uncreated.Warfare.Events.Models;
using Uncreated.Warfare.Events.Models.Players;
using Uncreated.Warfare.Players;
using Uncreated.Warfare.Players.Extensions;
using Uncreated.Warfare.Quests.Parameters;
using Uncreated.Warfare.Translations;
using Uncreated.Warfare.Util;

namespace Uncreated.Warfare.Quests.Templates;
public class RevivePlayers : QuestTemplate<RevivePlayers, RevivePlayers.Tracker, RevivePlayers.State>
{
    public Int32ParameterTemplate Revives { get; set; }
    public bool RequireSquad { get; set; }
    public bool RequireFullSquad { get; set; }
    public bool RequireTargetInSameSquad { get; set; }
    public RevivePlayers(IConfiguration templateConfig, IServiceProvider serviceProvider) : base(templateConfig, serviceProvider) { }
    public class State : IQuestState<RevivePlayers>
    {
        [JsonIgnore]
        public string Text { get; set; }

        [RewardVariable("r")]
        public QuestParameterValue<int> Revives { get; set; }

        [JsonIgnore]
        public QuestParameterValue<int> FlagValue => Revives;
        public bool RequireSquad { get; set; }
        public bool RequireFullSquad { get; set; }
        public bool RequireTargetInSameSquad { get; set; }
        public UniTask CreateFromConfigurationAsync(IQuestStateConfiguration configuration, RevivePlayers template, IServiceProvider serviceProvider, CancellationToken token)
        {
            RequireSquad = configuration.ParseBooleanValue("RequireSquad");
            RequireFullSquad = configuration.ParseBooleanValue("RequireFullSquad");
            RequireTargetInSameSquad = configuration.ParseBooleanValue("RequireTargetInSameSquad");
            Revives = configuration.ParseInt32Value("Revives", Int32ParameterTemplate.WildcardInclusive);

            FormatText(template);

            return UniTask.CompletedTask;
        }
        public async UniTask CreateFromTemplateAsync(RevivePlayers template, CancellationToken token)
        {
            Revives = await template.Revives.CreateValue(template.ServiceProvider);

            RequireSquad = template.RequireSquad;
            RequireFullSquad = template.RequireFullSquad;
            RequireTargetInSameSquad = template.RequireTargetInSameSquad;

            FormatText(template);
        }

        private void FormatText(RevivePlayers template)
        {
            ITranslationValueFormatter formatter = template.ServiceProvider.GetRequiredService<ITranslationValueFormatter>();

            Text = string.Format(template.Text.Translate(null, template.Type.Name),
                "{0}",
                Revives.GetDisplayString(formatter)
            );
        }

        /// <inheritdoc />
        public string CreateQuestDescriptiveString()
        {
            return Text;
        }
    }
    public class Tracker : QuestTracker, IEventListener<PlayerAided>
    {
        private readonly int _targetRevives;
        private readonly bool _needsSquad;
        private readonly bool _squadMustBeFull;
        private readonly bool _needsTargetInSameSquad;

        private int _revives;
        public override bool IsComplete => _revives >= _targetRevives;
        public override short FlagValue => (short)_revives;
        public Tracker(WarfarePlayer player, IServiceProvider serviceProvider, RevivePlayers quest, State state, IQuestPreset? preset)
            : base(player, serviceProvider, quest, state, preset)
        {
            _targetRevives = state.Revives.GetSingleValueOrMinimum();

            _needsSquad = state.RequireSquad || state.RequireFullSquad || state.RequireTargetInSameSquad;
            _squadMustBeFull = state.RequireFullSquad;
            _needsTargetInSameSquad = state.RequireTargetInSameSquad;
        }

        void IEventListener<PlayerAided>.HandleEvent(PlayerAided e, IServiceProvider serviceProvider)
        {
            if (!Player.Equals(e.Medic) || !e.IsRevive || Player.Team != e.Player.Team)
                return;

            // squads
            if (_needsSquad && !Player.IsInSquad())
            {
                return;
            }

            if (_squadMustBeFull && !Player.IsInFullSquad())
            {
                return;
            }

            if (_needsTargetInSameSquad && !Player.IsInSquadWith(e.Player))
            {
                return;
            }

            Interlocked.Increment(ref _revives);
            InvokeUpdate();
        }

        public override void WriteProgress(Utf8JsonWriter writer)
        {
            writer.WriteNumber("Revives", _revives);
        }

        public override void ReadProgress(ref Utf8JsonReader reader)
        {
            JsonUtility.ReadTopLevelProperties(ref reader, (ref Utf8JsonReader reader, string property, ref object? _) =>
            {
                if (property.Equals("Revives", StringComparison.Ordinal))
                {
                    _revives = reader.GetInt32();
                }
            });
        }
    }
}