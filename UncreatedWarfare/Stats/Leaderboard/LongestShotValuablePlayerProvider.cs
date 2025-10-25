using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Uncreated.Warfare.Configuration;
using Uncreated.Warfare.Layouts.UI.Leaderboards;

namespace Uncreated.Warfare.Stats.Leaderboard;
internal class LongestShotValuablePlayerProvider : IValuablePlayerProvider
{
    public ValuablePlayerMatch AggregateMostValuable(IEnumerable<LeaderboardSet> sets, IConfiguration statConfiguration, ILogger logger)
    {
        string? minimum = statConfiguration["MinimumDistance"];
        double.TryParse(minimum, NumberStyles.Number, CultureInfo.InvariantCulture, out double minValue);
        minValue *= minValue;

        LongestShot extremeValue = default;
        LeaderboardPlayer? extremePlayer = null;

        foreach (LeaderboardSet leaderboardSet in sets)
        {
            if (leaderboardSet.Players.Length == 0)
                continue;

            LeaderboardPlayer localExtreme = leaderboardSet.Players.Aggregate((p1, p2) =>
            {
                LongestShot p1Value = p1.Player.Component<PlayerGameStatsComponent>().LongestShot;
                LongestShot p2Value = p2.Player.Component<PlayerGameStatsComponent>().LongestShot;

                return p2Value.Gun is not { Exists: true } || p1Value.SquaredDistance >= p2Value.SquaredDistance ? p1 : p2;
            });

            LongestShot localExtremeValue = localExtreme.Player.Component<PlayerGameStatsComponent>().LongestShot;

            if (localExtremeValue.SquaredDistance <= extremeValue.SquaredDistance)
                continue;

            extremeValue = localExtremeValue;
            extremePlayer = localExtreme;
        }

        if (extremeValue.SquaredDistance < minValue || extremePlayer == null || !extremeValue.Gun.TryGetAsset(out ItemGunAsset? asset))
            return default;

        return new ValuablePlayerMatch(extremePlayer.Player, statConfiguration, [ asset.itemName, MathF.Sqrt(extremeValue.SquaredDistance) ]);
    }
}