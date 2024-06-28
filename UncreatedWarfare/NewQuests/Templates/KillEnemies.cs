using Cysharp.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using System;
using System.Threading;
using Uncreated.Warfare.Exceptions;
using Uncreated.Warfare.NewQuests.Parameters;
using Uncreated.Warfare.Quests;

namespace Uncreated.Warfare.NewQuests.Templates;
public class KillEnemies : QuestTemplate<KillEnemies, KillEnemies.Tracker, KillEnemies.State>
{
    public Int32ParameterTemplate Kills { get; set; }
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
    public class Tracker : QuestTracker
    {

    }
}
