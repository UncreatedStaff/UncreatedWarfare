using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using Uncreated.Warfare.Layouts.Teams;

namespace Uncreated.Warfare.Teams;

/// <summary>
/// Assumes an unlimited pool of points that can be given to any known number of teams, and a neutral team.
/// </summary>
public class TeamCountTable
{
    private readonly double[] _scores;
    private readonly Team?[] _orderedTeams;
    private double _neutralScore;
    public IReadOnlyList<Team> Teams { get; set; }
    public TeamCountTable(IReadOnlyList<Team> teams)
    {
        int maxId = 0;
        Team[] tArr = teams.ToArray();
        Teams = new ReadOnlyCollection<Team>(tArr);
        foreach (Team team in tArr)
        {
            if (team.Id > maxId)
                maxId = team.Id;
        }

        _scores = new double[maxId];
        Team?[] orderedTeams = new Team?[maxId];
        foreach (Team team in tArr)
        {
            if (team.Id <= 0)
                continue;

            orderedTeams[team.Id - 1] = team;
        }

        _orderedTeams = orderedTeams;
    }

    /// <summary>
    /// Get the score for a team, or the neutral score if <paramref name="team"/> is <see langword="null"/>.
    /// </summary>
    public double this[Team? team]
    {
        get
        {
            lock (_scores)
            {
                if (team == null)
                    return _neutralScore;

                int id = team.Id - 1;
                return id >= 0 && id < _scores.Length ? _scores[id] : 0d;
            }
        }
    }

    /// <summary>
    /// Increase the amount of points for a team by <paramref name="amount"/>.
    /// </summary>
    /// <exception cref="ArgumentOutOfRangeException">Unknown team.</exception>
    public double IncrementPoints(Team? team, double amount)
    {
        lock (_scores)
        {
            if (team != null)
            {
                int index = team.Id - 1;
                if (index < 0 || index >= _scores.Length)
                    throw new ArgumentOutOfRangeException(nameof(team), "Team not a part of this table.");

                ref double score = ref _scores[index];
                if (score + amount < 0)
                {
                    amount = -score;
                    score = 0;
                }
                else
                {
                    score += amount;
                }
            }
            else
            {
                if (_neutralScore + amount < 0)
                {
                    amount = -_neutralScore;
                    _neutralScore = 0;
                }
                else
                {
                    _neutralScore += amount;
                }
            }

            return amount;
        }
    }

    /// <summary>
    /// Set all scores to zero.
    /// </summary>
    public void ClearScores() => SetAllScores(0d, false);

    /// <summary>
    /// Set all scores to a value.
    /// </summary>
    public void SetAllScores(double value, bool ignoreNeutral)
    {
        lock (_scores)
        {
            if (!ignoreNeutral)
                _neutralScore = value;

            for (int i = 0; i < _scores.Length; ++i)
            {
                _scores[i] = value;
            }
        }
    }

    /// <summary>
    /// Calculate the mean of all teams' scores. This may include the neutral team if <paramref name="ignoreNeutral"/> is not <see langword="true"/>.
    /// </summary>
    public double CalculateAverageScore(bool ignoreNeutral)
    {
        lock (_scores)
        {
            return CalculateTotalIntl(ignoreNeutral) / (_scores.Length + (!ignoreNeutral ? 1 : 0));
        }
    }

    /// <summary>
    /// Calculate the sum of all teams' scores. This may include the neutral team if <paramref name="ignoreNeutral"/> is not <see langword="true"/>.
    /// </summary>
    public double CalculateTotalScore(bool ignoreNeutral)
    {
        lock (_scores)
        {
            return CalculateTotalIntl(ignoreNeutral);
        }
    }

    /// <summary>
    /// Calculate highest score of all teams. This may include the neutral team if <paramref name="ignoreNeutral"/> is not <see langword="true"/>.
    /// </summary>
    public double CalcualateMaxScore(bool ignoreNeutral) => CalcualateMaxScore(ignoreNeutral, out _);

    /// <summary>
    /// Calculate the team with the highest score. This may be the neutral team if <paramref name="ignoreNeutral"/> is not <see langword="true"/>, in which case <paramref name="maximumTeam"/> will be <see langword="null"/>.
    /// </summary>
    public double CalcualateMaxScore(bool ignoreNeutral, out Team? maximumTeam)
    {
        lock (_scores)
        {
            double max = ignoreNeutral ? double.NaN : _neutralScore;
            maximumTeam = null;
            for (int i = 0; i < _scores.Length; ++i)
            {
                double score = _scores[i];
                if (!double.IsNaN(max) && score <= max)
                    continue;

                max = score;
                maximumTeam = _orderedTeams[i];
            }

            return max;
        }
    }

    /// <summary>
    /// Calculate lowest score of all teams. This may include the neutral team if <paramref name="ignoreNeutral"/> is not <see langword="true"/>.
    /// </summary>
    public double CalcualateMinScore(bool ignoreNeutral) => CalcualateMinScore(ignoreNeutral, out _);

    /// <summary>
    /// Calculate the team with the lowest score. This may be the neutral team if <paramref name="ignoreNeutral"/> is not <see langword="true"/>, in which case <paramref name="minimumTeam"/> will be <see langword="null"/>.
    /// </summary>
    public double CalcualateMinScore(bool ignoreNeutral, out Team? minimumTeam)
    {
        lock (_scores)
        {
            double min = ignoreNeutral ? double.NaN : _neutralScore;
            minimumTeam = null;
            for (int i = 0; i < _scores.Length; ++i)
            {
                double score = _scores[i];
                if (!double.IsNaN(min) && score >= min)
                    continue;

                min = score;
                minimumTeam = _orderedTeams[i];
            }

            return min;
        }
    }

    /// <summary>
    /// Get the score for a team relative to the total score of all teams.
    /// </summary>
    public double GetRelativeScore(bool ignoreNeutral, Team? team)
    {
        if (ignoreNeutral && (team == null || !team.IsValid))
            throw new InvalidOperationException("Wants to ignore neutral when no team was inputted.");

        lock (_scores)
        {
            double total = CalculateTotalIntl(ignoreNeutral);
            if (team == null)
                return _neutralScore / total;

            int id = team.Id - 1;
            return id >= 0 && id < _scores.Length ? _scores[id] / total : 0d;
        }
    }

    private double CalculateTotalIntl(bool ignoreNeutral)
    {
        double ttl = ignoreNeutral ? 0 : _neutralScore;
        for (int i = 0; i < _scores.Length; ++i)
        {
            ttl += _scores[i];
        }

        return ttl;
    }

    /// <summary>
    /// Creates a graph for visualizing the different scores.
    /// </summary>
    public string ToGraph()
    {
        /*
           Creates a graph like this for testing and visualization:
           |                       
           |                       
           |                       
           |                       
           |                       
           |                       
           |                       
           |   █ █ █ █ █ █ █ █ █ █ 
            -----------------------
             N 0 1 2 3 4 5 6 7 8 9 
         */
        lock (_scores)
        {
            return string.Create(((_scores.Length + 1) * 2 + 2 + Environment.NewLine.Length) * 10 - Environment.NewLine.Length, this, static (span, state) =>
            {
                span.Fill(' ');
                ReadOnlySpan<char> newLine = Environment.NewLine;

                int totalCharWidth = (state._scores.Length + 1) * 2 + 2 + newLine.Length;
                const int totalCharHeight = 10;
                int tableCharWidth = (state._scores.Length + 1) * 2;
                const int tableCharHeight = totalCharHeight - 2;

                // Y axis
                for (int y = 0; y < totalCharHeight - 1; ++y)
                {
                    if (y < tableCharHeight)
                    {
                        span[y * totalCharWidth] = '|';
                    }

                    // newline
                    newLine.CopyTo(span.Slice((y + 1) * totalCharWidth - newLine.Length));
                }

                // X axis
                for (int x = tableCharWidth + 1; x > 0; --x)
                {
                    span[tableCharHeight * totalCharWidth + x] = '-';
                }

                double ttl = state.CalculateTotalIntl(false);

                // i = -1 is neutral score
                for (int i = -1; i < state._scores.Length; ++i)
                {
                    double score = i == -1 ? state._neutralScore : state._scores[i];
                    score /= ttl;
                    int height = (int)Math.Round(score * tableCharHeight);
                    int x = (i + 2) * 2;

                    // bar
                    for (int y = 0; y < height; ++y)
                    {
                        span[(tableCharHeight - 1 - y) * totalCharWidth + x] = '█';
                    }

                    // label
                    span[(tableCharHeight + 1) * totalCharWidth + x] = i == -1 ? 'N' : (char)(i + 48);
                }
            });
        }
    }
}
