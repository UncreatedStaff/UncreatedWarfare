using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Linq;
using System.Text.Json;
using Uncreated.Warfare.Events.Models;
using Uncreated.Warfare.Events.Models.Players;
using Uncreated.Warfare.Exceptions;
using Uncreated.Warfare.NewQuests.Parameters;
using Uncreated.Warfare.Players;
using Uncreated.Warfare.Players.Management;
using Uncreated.Warfare.Quests;
using Uncreated.Warfare.Util;
using Uncreated.Warfare.Util.List;

namespace Uncreated.Warfare.NewQuests.Templates;
public class KingSlayer : QuestTemplate<KingSlayer, KingSlayer.Tracker, KingSlayer.State>
{
    public Int32ParameterTemplate Kills { get; set; }
    public KingSlayer(IConfiguration templateConfig, IServiceProvider serviceProvider) : base(templateConfig, serviceProvider) { }
    public class State : IQuestState<KingSlayer>
    {
        [RewardVariable("k")]
        public QuestParameterValue<int> Kills { get; set; }
        public QuestParameterValue<int> FlagValue => Kills;
        public UniTask CreateFromConfigurationAsync(IConfiguration configuration, IServiceProvider serviceProvider, CancellationToken token)
        {
            string? killsStr = configuration["Kills"];

            if (string.IsNullOrEmpty(killsStr) || !Int32ParameterTemplate.TryParseValue(killsStr, out QuestParameterValue<int>? kills))
                throw new QuestConfigurationException(typeof(KingSlayer), "Failed to parse integer parameter for \"Kills\".");

            Kills = kills;
            return UniTask.CompletedTask;
        }
        public async UniTask CreateFromTemplateAsync(KingSlayer data, CancellationToken token)
        {
            Kills = await data.Kills.CreateValue(data.ServiceProvider);
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