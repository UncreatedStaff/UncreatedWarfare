using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;
using Uncreated.Warfare.Layouts.UI.Leaderboards;
using Uncreated.Warfare.Util;

namespace Uncreated.Warfare.Stats.Leaderboard;
public class ValuablePlayerInfo
{
    internal IConfiguration? Configuration;

    #nullable disable
    [UsedImplicitly]
    public string Name { get; set; }

    [UsedImplicitly]
    public ValuablePlayerInfoType Type { get; set; }

    [UsedImplicitly]
    public float Chance { get; set; } = 1f;
    #nullable restore

    /// <summary>
    /// Chooses a player which is best fit for this role.
    /// </summary>
    public ValuablePlayerMatch AggregateMostValuablePlayer(IEnumerable<LeaderboardSet> sets, ILogger logger)
    {
        if (Configuration == null)
        {
            logger.LogWarning("No configuration provided for valuable player {0}.", Name);
            return default;
        }

        ValuablePlayerMatch match = Type switch
        {
            ValuablePlayerInfoType.Custom
                => ExecuteCustomValuablePlayerProvider(sets, logger),
            ValuablePlayerInfoType.HighestStatistic or ValuablePlayerInfoType.LowestStatistic
                => AggregateValuablePlayerByStatistic(sets, logger),
            _ => default
        };

        if (match.Player == null || Chance <= 0 || Chance < 1 && RandomUtility.GetDouble() > Chance)
            return default;

        return match;
    }

    private ValuablePlayerMatch AggregateValuablePlayerByStatistic(IEnumerable<LeaderboardSet> sets, ILogger logger)
    {
        bool isFindingHighest = Type == ValuablePlayerInfoType.HighestStatistic;

        string? statName = Configuration!["Statistic"];
        if (statName == null)
        {
            logger.LogWarning("Missing statistic for valuable player {0}.", Name);
            return default;
        }

        double extremeValue = double.MinValue;
        LeaderboardPlayer? extremePlayer = null;

        foreach (LeaderboardSet leaderboardSet in sets)
        {
            int statIndex = leaderboardSet.GetStatisticIndex(statName);
            if (statIndex == -1)
            {
                logger.LogWarning("Unavailable statistic for valuable player {0}: {1}.", Name, statName);
                return default;
            }

            if (leaderboardSet.Players.Length == 0)
                continue;

            Func<LeaderboardPlayer, LeaderboardPlayer, LeaderboardPlayer> aggregator;
            if (isFindingHighest)
            {
                aggregator = (p1, p2) =>
                {
                    double p1Value = leaderboardSet.GetStatisticValue(statIndex, p1.Player.Steam64);
                    double p2Value = leaderboardSet.GetStatisticValue(statIndex, p2.Player.Steam64);

                    return p1Value >= p2Value ? p1 : p2;
                };
            }
            else
            {
                aggregator = (p1, p2) =>
                {
                    double p1Value = leaderboardSet.GetStatisticValue(statIndex, p1.Player.Steam64);
                    double p2Value = leaderboardSet.GetStatisticValue(statIndex, p2.Player.Steam64);

                    return p1Value <= p2Value ? p1 : p2;
                };
            }

            LeaderboardPlayer localExtreme = leaderboardSet.Players.Aggregate(aggregator);

            double localExtremeValue = leaderboardSet.GetStatisticValue(statIndex, localExtreme.Player.Steam64);

            if (isFindingHighest ? localExtremeValue <= extremeValue : localExtremeValue >= extremeValue)
                continue;

            extremeValue = localExtremeValue;
            extremePlayer = localExtreme;
        }

        return extremePlayer == null ? default : new ValuablePlayerMatch(extremePlayer.Player, Configuration, extremeValue);
    }

    private ValuablePlayerMatch ExecuteCustomValuablePlayerProvider(IEnumerable<LeaderboardSet> sets, ILogger logger)
    {
        string? typeProviderName = Configuration!["Provider"];
        Type? type = ContextualTypeResolver.ResolveType(typeProviderName, typeof(IValuablePlayerProvider));
        if (type == null)
        {
            logger.LogWarning("Missing or unknown provider type for stat {0}: {1}", Name, typeProviderName);
            return default;
        }

        try
        {
            IValuablePlayerProvider provider = (IValuablePlayerProvider)Activator.CreateInstance(type);
            return provider.AggregateMostValuable(sets, Configuration, logger);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error aggregating valuable player from stats.");
        }

        return default;
    }
}

public enum ValuablePlayerInfoType
{
    /// <summary>
    /// Aggregates the highest value of a statistic.
    /// </summary>
    HighestStatistic,

    /// <summary>
    /// Aggregates the lowest value of a statistic.
    /// </summary>
    LowestStatistic,

    /// <summary>
    /// Invokes a <see cref="IValuablePlayerProvider"/> implementation.
    /// </summary>
    Custom
}