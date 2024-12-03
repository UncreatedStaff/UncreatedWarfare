using Microsoft.Extensions.Configuration;
using System;
using System.Text.Json;
using Uncreated.Warfare.Events.Models;
using Uncreated.Warfare.Events.Models.Players;
using Uncreated.Warfare.Exceptions;
using Uncreated.Warfare.Kits;
using Uncreated.Warfare.NewQuests.Parameters;
using Uncreated.Warfare.Players;
using Uncreated.Warfare.Players.Extensions;
using Uncreated.Warfare.Quests;
using Uncreated.Warfare.Util;

namespace Uncreated.Warfare.NewQuests.Templates;
public class RevivePlayers : QuestTemplate<RevivePlayers, RevivePlayers.Tracker, RevivePlayers.State>
{
    public Int32ParameterTemplate Revives { get; set; }
    public bool RequireSquad { get; set; }
    public bool RequireFullSquad { get; set; }
    public bool RequireTargetInSameSquad { get; set; }
    public RevivePlayers(IConfiguration templateConfig, IServiceProvider serviceProvider) : base(templateConfig, serviceProvider) { }
    public class State : IQuestState<RevivePlayers>
    {
        [RewardVariable("r")]
        public QuestParameterValue<int> Revives { get; set; }
        public QuestParameterValue<int> FlagValue => Revives;
        public bool RequireSquad { get; set; }
        public bool RequireFullSquad { get; set; }
        public bool RequireTargetInSameSquad { get; set; }
        public UniTask CreateFromConfigurationAsync(IConfiguration configuration, IServiceProvider serviceProvider, CancellationToken token)
        {
            string? revivesStr = configuration["Revives"];

            RequireSquad = configuration.GetValue("RequireSquad", defaultValue: false);
            RequireFullSquad = configuration.GetValue("RequireFullSquad", defaultValue: false);
            RequireTargetInSameSquad = configuration.GetValue("RequireTargetInSameSquad", defaultValue: false);

            if (string.IsNullOrEmpty(revivesStr) || !Int32ParameterTemplate.TryParseValue(revivesStr, out QuestParameterValue<int>? revives))
                throw new QuestConfigurationException(typeof(RevivePlayers), "Failed to parse integer parameter for \"Revives\".");

            Revives = revives;
            return UniTask.CompletedTask;
        }
        public async UniTask CreateFromTemplateAsync(RevivePlayers data, CancellationToken token)
        {
            Revives = await data.Revives.CreateValue(data.ServiceProvider);

            RequireSquad = data.RequireSquad;
            RequireFullSquad = data.RequireFullSquad;
            RequireTargetInSameSquad = data.RequireTargetInSameSquad;
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

        protected override void WriteProgress(Utf8JsonWriter writer)
        {
            writer.WriteNumber("revives", _revives);
        }

        protected override void ReadProgress(ref Utf8JsonReader reader)
        {
            JsonUtility.ReadTopLevelProperties(ref reader, (ref Utf8JsonReader reader, string property, ref object? _) =>
            {
                if (property.Equals("revives", StringComparison.Ordinal))
                {
                    _revives = reader.GetInt32();
                }
            });
        }
    }
}