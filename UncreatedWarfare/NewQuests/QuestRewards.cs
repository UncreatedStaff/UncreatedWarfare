using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System;
using Uncreated.Warfare.Kits;
using Uncreated.Warfare.Levels;
using Uncreated.Warfare.Logging;
using Uncreated.Warfare.Players;
using Uncreated.Warfare.Translations;

namespace Uncreated.Warfare.NewQuests;

public interface IQuestReward
{
    /// <summary>
    /// Dispatch the reward to the player.
    /// </summary>
    /// <param name="serviceProvider">The scoped service provider for the current layout.</param>
    UniTask GrantRewardAsync(WarfarePlayer player, QuestTracker tracker, IServiceProvider serviceProvider, CancellationToken token = default);
}

public class XPReward : IQuestReward
{
    /// <summary>
    /// Amount of XP to give the recipient.
    /// </summary>
    public int XP { get; }

    public XPReward(int xp)
    {
        XP = xp;
    }

    public XPReward(IConfiguration configuration)
    {
        XP = configuration.GetValue<int>("XP");
    }

    /// <inheritdoc />
    public async UniTask GrantRewardAsync(WarfarePlayer player, QuestTracker tracker, IServiceProvider serviceProvider, CancellationToken token = default)
    {
        ITranslationValueFormatter formatter = serviceProvider.GetRequiredService<ITranslationValueFormatter>();

        XPParameters parameters = new XPParameters(player, player.Team, XP, tracker.Quest.Name.ToUpper() + " REWARD", false);

        // await Points.AwardXPAsync(parameters, token).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public override string ToString() => "Reward: " + XP + " XP";
}

public class CreditsReward : IQuestReward
{
    /// <summary>
    /// Amount of credits to give the recipient.
    /// </summary>
    public int Credits { get; }

    public CreditsReward(int credits)
    {
        Credits = credits;
    }

    public CreditsReward(IConfiguration configuration)
    {
        Credits = configuration.GetValue<int>("Credits");
    }

    /// <inheritdoc />
    public async UniTask GrantRewardAsync(WarfarePlayer player, QuestTracker tracker, IServiceProvider serviceProvider, CancellationToken token = default)
    {
        ITranslationValueFormatter formatter = serviceProvider.GetRequiredService<ITranslationValueFormatter>();

        CreditsParameters parameters = new CreditsParameters(player, player.Team, Credits, tracker.Quest.Name.ToUpper() + " REWARD");

        // await Points.AwardCreditsAsync(parameters, token).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public override string ToString() => "Reward: C " + Credits;
}

public class RankReward : IQuestReward
{
    /// <summary>
    /// Level number of the rank to give to the recipient.
    /// </summary>
    public int RankOrder { get; }

    public RankReward(int rankOrder)
    {
        RankOrder = rankOrder;
    }

    public RankReward(IConfiguration configuration)
    {
        RankOrder = configuration.GetValue<int>("RankOrder");
    }

    /// <inheritdoc />
    public UniTask GrantRewardAsync(WarfarePlayer player, QuestTracker tracker, IServiceProvider serviceProvider, CancellationToken token = default)
    {
        // Ranks.RankManager.SkipToRank(player, RankOrder);
        return UniTask.CompletedTask;
    }

    /// <inheritdoc />
    public override string ToString()
    {
        // ref Ranks.RankData d = ref Ranks.RankManager.GetRank(RankOrder, out bool success);
        // return "Reward: Unlock " + (success ? d.GetName(null, Data.LocalLocale) : "UNKNOWN RANK") + " (Order #" + RankOrder + ")";
        return "rank reward " + RankOrder;
    }
}

public class KitAccessReward : IQuestReward
{
    /// <summary>
    /// The internal kit name of the kit to give access for the recipient.
    /// </summary>
    public string? KitId { get; }

    public KitAccessReward(string kitId)
    {
        KitId = kitId;
    }

    public KitAccessReward(IConfiguration configuration)
    {
        KitId = configuration["KitId"];
    }

    /// <inheritdoc />
    public async UniTask GrantRewardAsync(WarfarePlayer player, QuestTracker tracker, IServiceProvider serviceProvider, CancellationToken token = default)
    {
        KitManager kitManager = serviceProvider.GetRequiredService<KitManager>();


        if (string.IsNullOrEmpty(KitId))
            return;

        if (!await kitManager.GiveAccess(KitId, player, KitAccessType.QuestReward, token).ConfigureAwait(false))
        {
            L.LogWarning($"Unknown kit {KitId} when giving access reward to player {player}.");
            return;
        }

        // KitSync.OnAccessChanged(player.Steam64.m_SteamID);
    }

    /// <inheritdoc />
    public override string ToString() => "Reward: Unlock \"" + KitId + "\"";
}