using Cysharp.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using SDG.Unturned;
using System;
using System.Linq;
using System.Text.Json;
using System.Threading;
using Uncreated.Warfare.Events;
using Uncreated.Warfare.Events.Players;
using Uncreated.Warfare.Exceptions;
using Uncreated.Warfare.NewQuests.Parameters;
using Uncreated.Warfare.Quests;
using Uncreated.Warfare.Util;

namespace Uncreated.Warfare.NewQuests.Templates;
public class KingSlayer : QuestTemplate<KingSlayer, KingSlayer.Tracker, KingSlayer.State>
{
    public Int32ParameterTemplate Kills { get; set; }
    public KingSlayer(IConfiguration templateConfig, IServiceProvider serviceProvider) : base(templateConfig, serviceProvider) { }
    public class State : BaseState
    {
        [RewardField("k")]
        public QuestParameterValue<int> Kills { get; set; }
        public override QuestParameterValue<int> FlagValue => Kills;
        public override UniTask CreateFromConfigurationAsync(IConfiguration configuration, IServiceProvider serviceProvider, CancellationToken token)
        {
            string? killsStr = configuration["Kills"];

            if (string.IsNullOrEmpty(killsStr) || !Int32ParameterTemplate.TryParseValue(killsStr, out QuestParameterValue<int>? kills))
                throw new QuestConfigurationException(typeof(KingSlayer), "Failed to parse integer parameter for \"Kills\".");

            Kills = kills;
            return UniTask.CompletedTask;
        }
        public override async UniTask CreateFromTemplateAsync(KingSlayer data, CancellationToken token)
        {
            Kills = await data.Kills.CreateValue(data.ServiceProvider);
        }
    }
    public class Tracker : QuestTracker, IEventListener<PlayerDied>
    {
        private readonly int _targetKills;
        private UCPlayer? _kingSlayer;

        private int _kills;
        public override bool IsComplete => _kills >= _targetKills;
        public override short FlagValue => (short)_kills;
        public Tracker(UCPlayer player, IServiceProvider serviceProvider, KingSlayer quest, State state, IQuestPreset? preset)
            : base(player, serviceProvider, quest, state, preset)
        {
            _targetKills = state.Kills.GetSingleValueOrMinimum();
        }

        void IEventListener<PlayerDied>.HandleEvent(PlayerDied e, IServiceProvider serviceProvider)
        {
            if (e.Instigator.m_SteamID != Player.Steam64 || !e.WasEffectiveKill || e.Cause == EDeathCause.SHRED || e.DeadTeam is not 1 and not 2)
                return;

            UCPlayer? kingSlayer = PlayerManager.OnlinePlayers
                .Where(player => player.GetTeam() == e.DeadTeam)
                .Aggregate((kingSlayer, next) => next.CachedXP > kingSlayer.CachedXP ? next : kingSlayer);
            
            if (kingSlayer.Steam64 == e.Player.Steam64)
            {
                _kingSlayer = kingSlayer;
                _kills++;
                InvokeUpdate();
                return;
            }

            if (_kingSlayer == kingSlayer)
                return;

            HandleUpdated(true);
            _kingSlayer = kingSlayer;
        }

        protected override void WriteProgress(Utf8JsonWriter writer)
        {
            writer.WriteNumber("kills", _kills);
        }

        protected override void ReadProgress(ref Utf8JsonReader reader)
        {
            JsonUtility.ReadTopLevelProperties(ref reader, (ref Utf8JsonReader reader, string property, ref object? _) =>
            {
                if (property.Equals("kills", StringComparison.Ordinal))
                {
                    _kills = reader.GetInt32();
                }
            });
        }
    }
}