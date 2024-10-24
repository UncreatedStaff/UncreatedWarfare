using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using Uncreated.Warfare.Layouts.Teams;

namespace Uncreated.Warfare.Teams;

/// <summary>
/// Assumes a set pool of points that can be distributed amongst any known number of teams, and a neutral team.
/// </summary>
public class TeamScoreTable
{
    private readonly double[] _scores;
    private double _neutralScore;
    public IReadOnlyList<Team> Teams { get; set; }
    public double TotalScore { get; }
    public TeamScoreTable(IReadOnlyList<Team> teams, double neutralScore)
    {
        TotalScore = neutralScore;

        int maxId = 0;
        Teams = new ReadOnlyCollection<Team>(teams.ToArray());
        foreach (Team team in teams)
        {
            if (team.Id > maxId)
                maxId = team.Id;
        }

        _scores = new double[maxId];
        _neutralScore = neutralScore;
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
    /// Distributes the total score over all teams (except the netural team).
    /// </summary>
    public void DistributeUniformly()
    {
        double amount = TotalScore / _scores.Length;
        lock (_scores)
        {
            for (int i = 0; i < _scores.Length; ++i)
            {
                _scores[i] = amount;
            }

            _neutralScore = 0;
        }
    }
    
    /// <summary>
    /// Distributes the total score over all teams (except the netural team).
    /// </summary>
    public void NeutralizeScores()
    {
        lock (_scores)
        {
            for (int i = 0; i < _scores.Length; ++i)
            {
                _scores[i] = 0;
            }

            _neutralScore = TotalScore;
        }
    }

    public double IncrementPoints(Team? team, double amount)
    {
        lock (_scores)
        {
            int index;
            if (team != null)
            {
                index = team.Id - 1;
                if (index < 0 || index >= _scores.Length)
                    throw new ArgumentOutOfRangeException(nameof(team), "Team not a part of this table.");
            }
            else
            {
                index = -1;
            }

            double removedAmount = 0;
            while (amount > removedAmount)
            {
                // find the minimum value of all scores so we know the max amount that can be evenly removed.
                double min = float.NaN;
                int viableCt = 0;
                for (int i = 0; i < _scores.Length; i++)
                {
                    double score = _scores[i];
                    if (i == index || score <= 0)
                        continue;

                    ++viableCt;

                    if (double.IsNaN(min) || min > score)
                        min = score;
                }

                // no team has any score available, fall back to taking from neutral score
                if (double.IsNaN(min))
                {
                    if (index != -1)
                    {
                        // remove from neutral score if all other scores are 0
                        if (_neutralScore > amount - removedAmount)
                        {
                            _neutralScore -= amount - removedAmount;
                            removedAmount = amount;
                        }
                        else
                        {
                            removedAmount += _neutralScore;
                            _neutralScore = 0;
                        }
                    }

                    break;
                }

                double numToRemovePerTeam = Math.Min(min, (amount - removedAmount) / viableCt);

                // remove the minimum value from all columns
                for (int i = 0; i < _scores.Length; i++)
                {
                    ref double score = ref _scores[i];
                    if (i == index || score <= 0)
                        continue;

                    score -= numToRemovePerTeam;
                    removedAmount += numToRemovePerTeam;
                    if (Math.Abs(score) < 0.0001d)
                        score = 0;
                }
            }

            if (index == -1)
                _neutralScore += removedAmount;
            else
                _scores[index] += removedAmount;
            return removedAmount;
        }
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

                // i = -1 is neutral score
                for (int i = -1; i < state._scores.Length; ++i)
                {
                    double score = i == -1 ? state._neutralScore : state._scores[i];
                    score /= state.TotalScore;
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
