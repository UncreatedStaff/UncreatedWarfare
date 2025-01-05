using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;
using Uncreated.Warfare.Layouts.UI.Leaderboards;
using Uncreated.Warfare.Players;
using Uncreated.Warfare.Translations;

namespace Uncreated.Warfare.Stats.Leaderboard;

public interface IValuablePlayerProvider
{
    ValuablePlayerMatch AggregateMostValuable(IEnumerable<LeaderboardSet> sets, IConfiguration statConfiguration, ILogger logger);
}

public readonly struct ValuablePlayerMatch
{
    public readonly WarfarePlayer? Player;
    public readonly double StatValue;
    public readonly object[]? StatValues;
    public readonly TranslationList? Title;
    public readonly TranslationList? ValueFormatString;

    public ValuablePlayerMatch(WarfarePlayer? player, IConfiguration configuration, double statValue)
    {
        Player = player;
        Title = configuration.GetSection("DisplayName").Get<TranslationList>();
        ValueFormatString = configuration.GetSection("Format").Get<TranslationList>();
        StatValue = statValue;
    }

    public ValuablePlayerMatch(WarfarePlayer? player, IConfiguration configuration, object[] statValues)
    {
        Player = player;
        Title = configuration.GetSection("DisplayName").Get<TranslationList>();
        ValueFormatString = configuration.GetSection("Format").Get<TranslationList>();
        StatValues = statValues;
    }

    public string GetTitle(in LanguageSet set)
    {
        return Title is not { Count: > 0 } ? "Valuable Player" : Title.Translate(set.Language)!;
    }

    public string FormatValue(in LanguageSet set)
    {
        if (ValueFormatString is not { Count: > 0 })
        {
            LanguageSet s = set;
            return StatValues == null
                ? StatValue.ToString(s.Culture)
                : string.Join(", ", StatValues.Select(x => x is IFormattable f ? f.ToString(null, s.Culture) : x.ToString()));
        }

        string fmtString = ValueFormatString.Translate(set.Language)!;

        if (StatValues == null)
        {
            return string.Format(set.Culture, fmtString, StatValue);
        }

        return StatValues.Length == 0 ? fmtString : string.Format(set.Culture, fmtString, StatValues);
    }
}