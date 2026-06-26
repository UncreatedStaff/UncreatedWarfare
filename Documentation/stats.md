# Leaderboard Stats
The leaderboard stats file (can also be defined in-line in the phase) defines information about stats that are tracked by the gamemode.

This is also used with the Points UI to cycle between all statistics for the game.

## Updating Stats
To update a stat from a custom gamemode, use the following.
```cs
WarfarePlayer player = /* etc */;
player.ComponentOrNull<PlayerGameStatsComponent>()?.AddToStat(KnownStatNames.Deaths, 1);
```

In some situations, you need to update a stat for an offline player. You can do so like this:
```cs
Layout layout = /* inject */;

CSteamID offlinePlayerId = /* etc */;
Team offlinePlayerTeam = /* etc */;

if (layout.Phases.OfType<LeaderboardPhase>().FirstOrDefault() is { } phase)
{
    phase.AddToOfflineStat(
        phase.GetStatIndex(KnownStatNames.Deaths),
        amount: 1.0,
        offlinePlayerId,
        offlinePlayerTeam
    );
}
```

If the given statistic isn't configured it'll just be ignored.

## The Leaderboard Phase

```yml
Phases:
  - Type: LeaderboardPhase
    Duration: 30s
    Invincible: true
    PlayerStats: [ ... ]
    ValuablePlayers: [ ... ] # see below for the format of these
```

## The Stats File

Instead of defining all the stats for each layout, we can define a relative `PlayerStatsPath` to re-use the leaderboard stats. This is similar to variations but a bit simpler.

```yml
Phases:
  - Type: LeaderboardPhase
    Duration: 30s
    PlayerStatsPath: "../../Leaderboard/AAS Stats.yml"
    Invincible: true
```

Usually these are placed in `Warfare/Leaderboard/`.

Built-in Stats:
* Most are [here](https://github.com/UncreatedStaff/UncreatedWarfare/blob/master/UncreatedWarfare/Stats/KnownStatNames.cs)
* Insurgnecy Stats
    * `intel` Intelligence Gathered
    * `los-caches-discovered` Caches Discovered via line-of-sight
    * `caches-destroyed` Caches Destroyed

`Warfare/Leaderboard/AAS Stats.yml`
```yml
# 'PlayerStats' if defined in the phase
Stats:
    # Variable name of the stat. Usually try to keep these short.
  - Name: k
    # Format the statistic will be displayed with.
    # Whole numbers use 'F0' but decimals may use 'F2' or '0.##'.
    # For more info see this page:
    # https://learn.microsoft.com/en-us/dotnet/standard/base-types/standard-numeric-format-strings
    NumberFormat: F0 
    # Text shown on the leaderboard column header.
    ColumnHeader: "K"
    # Display name of the statistic (used for the points UI currently).
    DisplayName:
      en-us: Kills
    # -OR-
    DisplayName: Kills
    # Whether or not the statistic will actually be displayed on the leaderboard.
    # Some stats are only used for global stats or to calculate other stats (like KDR).
    IsLeaderboardColumn: true
    # Disables showing this stat on the points UI (where it cycles through all stats).
    DisablePointsUIDisplay: false
    # Optional (defaults to the value of Name). Defines the variable name as used in other expressions.
    FormulaName: k
    # If provided, indicates that this statistic should be the initial sorted column in the leaderboard.
    # Can either be Descending or Ascending to change the order it's sorted by.
    # Only one statistic can have this
    DefaultLeaderboardSort: Descending

    # Deaths
  - Name: d
    NumberFormat: F0
    ColumnHeader: "D"
    DisplayName: Deaths
    IsLeaderboardColumn: true

    # Objectives Captured
  - Name: obj
    NumberFormat: F0
    # Indicates this stat should be summed up and shown with the team summary.
    IsGlobalStat: true
    DisplayName:
      en-us: "Captures"

    # Kill-Death Ratio
  - Name: kdr
    # Expressions can be used instead to define virtual stats like KDR
    Expression: k/max(1, d)
    DisplayName: "K/D Ratio"
    # Display up to 3 decimal places
    NumberFormat: "0.###"

# Designates categories to choose a specific player that stands out,
# such as highest XP gain or most kills.
ValuablePlayers:
    # Arbitrary name of the valuable player entry
  - Name: xp
    # Either HighestStatistic, LowestStatistic, or Custom.
    # HighestStatistic picks the player with the highest value for a stat.
    # LowestStatistic picks the player with the lowest value for a stat.
    # Custom uses an implementation of IValuablePlayerProvider to choose a player.
    Type: HighestStatistic
    # For HighestStatistic and LowestStatistic, the statistic to use
    Statistic: xp
    # Display name of the category in the leaderboard
    DisplayName: "MVP"
    # Format string to show in the UI under the DisplayName
    Format: "{0:F0} XP"
    
  - Name: longest-shot
    Type: Custom
    MinimumDistance: 100 # meters
    # Implementation of IValuablePlayerProvider
    Provider: "LongestShotValuablePlayerProvider"
    DisplayName:
      en-us: "Sharpshooter"
    Format:
      en-us: "{0} - {1:F1}m"
```

### IValuablePlayerProvider
The following example aggregates the best player depending on an imaginary 'Score' property.
```cs
internal sealed class ScoreValuablePlayerProvider : IValuablePlayerProvider
{
    public ValuablePlayerMatch AggregateMostValuable(
        IEnumerable<LeaderboardSet> sets,
        IConfiguration statConfiguration,
        ILogger logger
    )
    {
        int extremeValue = statConfiguration.GetValue<int>("Minimum");
        LeaderboardPlayer? extremePlayer = null;
        
        // each team
        foreach (LeaderboardSet leaderboardSet in sets)
        {
            // Aggregate throws an error for empty collections
            if (leaderboardSet.Players.Length == 0)
                continue;

            // aggregate the best highest-scored player for each team
            LeaderboardPlayer localExtreme = leaderboardSet.Players.Aggregate((p1, p2) => p1.Score > p2.Score ? p1 : p2);

            int score = localExtreme.Player.Score;

            if (score <= extremeValue)
                continue;

            extremeValue = score;
            extremePlayer = localExtreme;
        }

        if (extremePlayer == null)
            return default;

        // array corresponds to the {n} arguments in the configured Format
        return new ValuablePlayerMatch(extremePlayer.Player, statConfiguration, [ score ]);
    }
}
```
```yml
Name: best-score
Type: Custom
Provider: "ScoreValuablePlayerProvider"
DisplayName: "Highest Scorer"
# Custom formatting for the description.
Format: "Score: {0}"
# Extra properties read from the statConfiguration parameter
Minimum: 50
```