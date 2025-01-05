using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;
using Uncreated.Warfare.Layouts.UI.Leaderboards;
using Uncreated.Warfare.Util;

namespace Uncreated.Warfare.Stats.Leaderboard;
internal class SpecificPlayerValuablePlayerProvider : IValuablePlayerProvider
{
    public ValuablePlayerMatch AggregateMostValuable(IEnumerable<LeaderboardSet> sets, IConfiguration statConfiguration, ILogger logger)
    {
        ulong[] steamIds = statConfiguration.GetSection("Players").Get<ulong[]>() ?? Array.Empty<ulong>();

        double randomMin = statConfiguration.GetValue("RandomValueMinimum", 3d);
        double randomMax = statConfiguration.GetValue("RandomValueMaximum", 24d);
            
        LeaderboardPlayer? match = sets
            .SelectMany(x => x.Players)
            .FirstOrDefault(x => Array.IndexOf(steamIds, x.Player.Steam64.m_SteamID) >= 0);

        return match == null ? default : new ValuablePlayerMatch(match.Player, statConfiguration, RandomUtility.GetDouble(randomMin, randomMax));
    }
}