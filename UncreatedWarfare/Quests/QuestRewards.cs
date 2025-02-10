using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Globalization;
using System.Linq;
using System.Text.Json;
using Uncreated.Warfare.Kits;
using Uncreated.Warfare.Players;
using Uncreated.Warfare.Stats;
using Uncreated.Warfare.Translations;

namespace Uncreated.Warfare.Quests;

public interface IQuestReward
{
    /// <summary>
    /// Dispatch the reward to the player.
    /// </summary>
    /// <param name="serviceProvider">The scoped service provider for the current layout.</param>
    UniTask GrantRewardAsync(WarfarePlayer player, QuestTracker tracker, IServiceProvider serviceProvider, CancellationToken token = default);

    void WriteToJson(Utf8JsonWriter writer);
    string CreateQuestRewardString();
}

public class NullReward : IQuestReward
{
    void IQuestReward.WriteToJson(Utf8JsonWriter writer) { }

    UniTask IQuestReward.GrantRewardAsync(WarfarePlayer player, QuestTracker tracker, IServiceProvider serviceProvider, CancellationToken token)
    {
        return UniTask.CompletedTask;
    }

    string IQuestReward.CreateQuestRewardString()
    {
        return "Nothing";
    }
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

    public XPReward(JsonElement configuration)
    {
        XP = configuration.GetProperty("XP").GetInt32();
    }

    /// <inheritdoc />
    public async UniTask GrantRewardAsync(WarfarePlayer player, QuestTracker tracker, IServiceProvider serviceProvider, CancellationToken token = default)
    {
        PointsService pointsService = serviceProvider.GetRequiredService<PointsService>();
        PointsTranslations translations = serviceProvider.GetRequiredService<TranslationInjection<PointsTranslations>>().Value;

        ResolvedEventInfo info = new ResolvedEventInfo(default, XP, null, null)
        {
            Message = translations.XPToastQuestReward.Translate(tracker.Quest.Name)
        };

        await pointsService.ApplyEvent(player.Steam64, player.Team.Faction.PrimaryKey, info, token).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public void WriteToJson(Utf8JsonWriter writer)
    {
        writer.WriteNumber("XP", XP);
    }

    /// <inheritdoc />
    public string CreateQuestRewardString()
    {
        return $"<color=#ffffff>{XP.ToString(CultureInfo.InvariantCulture)}</color> <color=#e3b552>XP</color>";
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

    public CreditsReward(JsonElement configuration)
    {
        Credits = configuration.GetProperty("Credits").GetInt32();
    }

    /// <inheritdoc />
    public async UniTask GrantRewardAsync(WarfarePlayer player, QuestTracker tracker, IServiceProvider serviceProvider, CancellationToken token = default)
    {
        PointsService pointsService = serviceProvider.GetRequiredService<PointsService>();
        PointsTranslations translations = serviceProvider.GetRequiredService<TranslationInjection<PointsTranslations>>().Value;

        ResolvedEventInfo info = new ResolvedEventInfo(default, null, Credits, null)
        {
            Message = translations.XPToastQuestReward.Translate(tracker.Quest.Name)
        };

        await pointsService.ApplyEvent(player.Steam64, player.Team.Faction.PrimaryKey, info, token).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public void WriteToJson(Utf8JsonWriter writer)
    {
        writer.WriteNumber("Credits", Credits);
    }

    /// <inheritdoc />
    public string CreateQuestRewardString()
    {
        return $"<color=#ffffff>{Credits.ToString(CultureInfo.InvariantCulture)}</color> <color=#b8ffc1>C</color>";
    }

    /// <inheritdoc />
    public override string ToString() => "Reward: C " + Credits;
}

public class ReputationReward : IQuestReward
{
    /// <summary>
    /// Amount of reputation to give the recipient.
    /// </summary>
    public int Reputation { get; }

    public ReputationReward(int credits)
    {
        Reputation = credits;
    }

    public ReputationReward(IConfiguration configuration)
    {
        Reputation = configuration.GetValue<int>("Reputation");
    }

    public ReputationReward(JsonElement configuration)
    {
        Reputation = configuration.GetProperty("Reputation").GetInt32();
    }

    /// <inheritdoc />
    public async UniTask GrantRewardAsync(WarfarePlayer player, QuestTracker tracker, IServiceProvider serviceProvider, CancellationToken token = default)
    {
        PointsService pointsService = serviceProvider.GetRequiredService<PointsService>();
        PointsTranslations translations = serviceProvider.GetRequiredService<TranslationInjection<PointsTranslations>>().Value;

        ResolvedEventInfo info = new ResolvedEventInfo(default, null, null, Reputation)
        {
            Message = translations.XPToastQuestReward.Translate(tracker.Quest.Name)
        };

        await pointsService.ApplyEvent(player.Steam64, player.Team.Faction.PrimaryKey, info, token).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public void WriteToJson(Utf8JsonWriter writer)
    {
        writer.WriteNumber("Reputation", Reputation);
    }

    /// <inheritdoc />
    public string CreateQuestRewardString()
    {
        return $"<color=#ffffff>{Reputation.ToString(CultureInfo.InvariantCulture)}</color> <color=#66ff66>Reputation</color>";
    }

    /// <inheritdoc />
    public override string ToString() => "Reward: " + Reputation + " Rep";
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

    public KitAccessReward(JsonElement configuration)
    {
        KitId = configuration.GetProperty("KitId").GetString();
    }

    /// <inheritdoc />
    public async UniTask GrantRewardAsync(WarfarePlayer player, QuestTracker tracker, IServiceProvider serviceProvider, CancellationToken token = default)
    {
        IKitAccessService kitAccessService = serviceProvider.GetRequiredService<IKitAccessService>();
        IKitDataStore kitDataStore = serviceProvider.GetRequiredService<IKitDataStore>();

        uint pk = await kitDataStore
            .QueryFirstAsync(kits => kits
                .Where(x => x.Id == KitId)
                .Select(x => x.PrimaryKey), KitInclude.Base, token)
            .ConfigureAwait(false);

        if (string.IsNullOrEmpty(KitId))
            return;

        if (!await kitAccessService.UpdateAccessAsync(player.Steam64, pk, KitAccessType.QuestReward, token).ConfigureAwait(false))
        {
            serviceProvider.GetRequiredService<ILogger<KitAccessReward>>().LogWarning($"Unknown kit {KitId} when giving access reward to player {player}.");
        }
    }

    /// <inheritdoc />
    public void WriteToJson(Utf8JsonWriter writer)
    {
        writer.WriteString("KitId", KitId);
    }

    /// <inheritdoc />
    public string CreateQuestRewardString()
    {
        return $"<color=#66ffff>Unlock</color> <color=#ffffff>{KitId}</color>";
    }

    /// <inheritdoc />
    public override string ToString() => "Reward: Unlock \"" + KitId + "\"";
}