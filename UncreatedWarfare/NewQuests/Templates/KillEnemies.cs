using Cysharp.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using SDG.Unturned;
using System;
using System.Threading;
using Uncreated.Warfare.Events;
using Uncreated.Warfare.Events.Players;
using Uncreated.Warfare.Exceptions;
using Uncreated.Warfare.NewQuests.Parameters;
using Uncreated.Warfare.Quests;

namespace Uncreated.Warfare.NewQuests.Templates;
public class KillEnemies : QuestTemplate<KillEnemies, KillEnemies.Tracker, KillEnemies.State>
{
    public Int32ParameterTemplate Kills { get; set; }
    public KillEnemies(IConfiguration templateConfig, IServiceProvider serviceProvider) : base(templateConfig, serviceProvider) { }
    public class State : BaseState
    {
        [RewardField("k")]
        public QuestParameterValue<int> Kills { get; set; }
        public override QuestParameterValue<int> FlagValue => Kills;
        public override UniTask CreateFromConfigurationAsync(IConfiguration configuration, CancellationToken token)
        {
            if (!Int32ParameterTemplate.TryParseValue(configuration["Kills"], out QuestParameterValue<int>? kills))
                throw new QuestConfigurationException(typeof(KillEnemies), "Failed to parse integer parameter for \"Kills\".");

            Kills = kills;
            return UniTask.CompletedTask;
        }
        public override async UniTask CreateFromTemplateAsync(KillEnemies data, CancellationToken token)
        {
            Kills = await data.Kills.CreateValue(data.ServiceProvider);
        }
    }
    public class Tracker : QuestTracker, IEventListener<PlayerDied>
    {
        private readonly int _targetKills;
        private int _kills;
        public override bool IsComplete => _kills >= _targetKills;
        public override short FlagValue => (short)_kills;
        public Tracker(UCPlayer player, IServiceProvider serviceProvider, KillEnemies quest, State state, IQuestPreset? preset)
            : base(player, serviceProvider, quest, state, preset)
        {
            _targetKills = state.Kills.GetSingleValueOrMinimum();
        }

        void IEventListener<PlayerDied>.HandleEvent(PlayerDied e)
        {
            if (e.Steam64 != Player.Steam64 || !e.WasEffectiveKill || e.Cause == EDeathCause.SHRED)
                return;

            _kills++;
            InvokeUpdate();
        }
    }
}