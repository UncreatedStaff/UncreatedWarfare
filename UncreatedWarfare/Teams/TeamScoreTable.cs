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
    public double MaxScore { get; }
    public TeamScoreTable(IReadOnlyList<Team> teams, double neutralScore)
    {
        MaxScore = neutralScore;

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
                if (team == null || !team.IsValid)
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
        double amount = MaxScore / _scores.Length;
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
    /// Distributes the total score given a set ration (where the first index is the neutral team).
    /// </summary>
    public void Distribute(params double[] ratio)
    {
        if (ratio.Length < 1 + _scores.Length)
            throw new ArgumentException($"Incorrect number of ratio values. Expected {1 + _scores.Length}.", nameof(ratio));

        double ttl = 0;
        for (int i = 0; i < ratio.Length; ++i)
        {
            double rt = ratio[i];
            if (rt < 0)
                throw new ArgumentException($"Ratio contained negative number at index {i}.", nameof(ratio));
            ttl += ratio[i];
        }

        if (ttl == 0)
            throw new ArgumentException("Ratio contains only zeros.", nameof(ratio));

        ttl /= MaxScore;

        lock (_scores)
        {
            for (int i = 0; i < _scores.Length; ++i)
            {
                _scores[i] = ratio[i + 1] / ttl;
            }

            _neutralScore = ratio[0] / ttl;
        }
    }
    
    /// <summary>
    /// Distributes the entire score to the neutral team.
    /// </summary>
    public void NeutralizeScores()
    {
        lock (_scores)
        {
            for (int i = 0; i < _scores.Length; ++i)
            {
                _scores[i] = 0;
            }

            _neutralScore = MaxScore;
        }
    }
    
    /// <summary>
    /// Distributes the entire score to the neutral team.
    /// </summary>
    public void MaximizeTeam(Team? team)
    {
        if (team == null || !team.IsValid)
        {
            NeutralizeScores();
            return;
        }

        lock (_scores)
        {
            for (int i = 0; i < _scores.Length; ++i)
            {
                if (i == team.Id - 1)
                    _scores[i] = MaxScore;
                else
                    _scores[i] = 0;
            }

            _neutralScore = 0;
        }
    }
    
    /// <summary>
    /// Distributes the entire score to the neutral team.
    /// </summary>
    public void MinimizeTeam(Team? team)
    {
        IncrementPoints(team, -this[team]);
    }

    /// <summary>
    /// Decrease the amount of points for a team by <paramref name="amount"/>.
    /// </summary>
    /// <exception cref="ArgumentOutOfRangeException">Unknown team.</exception>
    public double DecrementPoints(Team? team, double amount, bool toNeutral)
    {
        if (amount < 0)
        {
            return -IncrementPoints(team, -amount);
        }

        lock (_scores)
        {
            int index;
            if (team != null && team.IsValid)
            {
                index = team.Id - 1;
                if (index < 0 || index >= _scores.Length)
                    throw new ArgumentOutOfRangeException(nameof(team), "Team not a part of this table.");
            }
            else
            {
                toNeutral = false;
                index = -1;
            }

            double currentScore = index == -1 ? _neutralScore : _scores[index];
            if (currentScore <= 0)
                return 0;

            if (currentScore <= amount)
            {
                amount = currentScore;
                if (index == -1)
                    _neutralScore = 0;
                else
                    _scores[index] = 0;
            }
            else
            {
                if (index == -1)
                    _neutralScore -= amount;
                else
                    _scores[index] -= amount;
            }

            if (toNeutral)
            {
                _neutralScore += amount;
                return amount;
            }

            double addedAmount = 0;
            while (amount > addedAmount)
            {
                // find the minimum value and second minimum value, then add the difference distributed to all the minimum columns, and so on until all columns are even.
                double min = double.NaN;
                int numMinimums = 0;
                for (int i = 0; i < _scores.Length; i++)
                {
                    double score = _scores[i];
                    if (i == index)
                        continue;

                    if (double.IsNaN(min) || min > score)
                    {
                        min = score;
                        numMinimums = 1;
                    }
                    else if (min == score)
                    {
                        ++numMinimums;
                    }
                }

                double secondMinimum = double.NaN;
                for (int i = 0; i < _scores.Length; i++)
                {
                    double score = _scores[i];
                    if (i == index || score <= 0)
                        continue;

                    if ((double.IsNaN(secondMinimum) || secondMinimum > score) && score > min)
                        secondMinimum = score;
                }

                // all teams are even, distribute evenly
                if (double.IsNaN(secondMinimum))
                {
                    double perTeam = (amount - addedAmount) / (_scores.Length - (index == -1 ? 0 : 1));
                    for (int i = 0; i < _scores.Length; ++i)
                    {
                        if (i == index)
                            continue;

                        _scores[i] += perTeam;
                    }

                    return amount;
                }

                double perMinimum = Math.Min((amount - addedAmount) / numMinimums, secondMinimum - min);
                for (int i = 0; i < _scores.Length; ++i)
                {
                    double score = _scores[i];
                    if (i == index || min != score)
                        continue;

                    _scores[i] += perMinimum;
                }

                addedAmount += perMinimum * numMinimums;
            }

            return amount;
        }
    }

    /// <summary>
    /// Increase the amount of points for a team by <paramref name="amount"/>.
    /// </summary>
    /// <exception cref="ArgumentOutOfRangeException">Unknown team.</exception>
    public double IncrementPoints(Team? team, double amount)
    {
        if (amount < 0)
        {
            return -DecrementPoints(team, -amount, true);
        }

        lock (_scores)
        {
            int index;
            if (team != null && team.IsValid)
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
                double min = double.NaN;
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
    public string ToGraph(bool scaleToMaximum = false)
    {
        /*
           Creates a graph like this for testing and visualization:
           |                       
           |                       
           |                       
           |                       
           |                     █ 
           |                 █ █ █  
           |             █ █ █ █ █ 
           |   █ █ █ █ █ █ █ █ █ █ 
            -----------------------
             N 0 1 2 3 4 5 6 7 8 9 
         */
        GraphState state = default;
        state.Table = this;
        state.ScaleToMaximum = scaleToMaximum;
        lock (_scores)
        {
            return string.Create(((_scores.Length + 1) * 2 + 2 + Environment.NewLine.Length) * 10 - Environment.NewLine.Length, state, static (span, state) =>
            {
                span.Fill(' ');
                ReadOnlySpan<char> newLine = Environment.NewLine;

                TeamScoreTable table = state.Table;

                int totalCharWidth = (table._scores.Length + 1) * 2 + 2 + newLine.Length;
                const int totalCharHeight = 10;
                int tableCharWidth = (table._scores.Length + 1) * 2;
                const int tableCharHeight = totalCharHeight - 2;

                double yTop = table.MaxScore;
                if (state.ScaleToMaximum)
                {
                    yTop = table._neutralScore;
                    for (int i = 0; i < table._scores.Length; ++i)
                    {
                        double score = table._scores[i];
                        if (score > yTop)
                            yTop = score;
                    }
                }

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
                for (int i = -1; i < table._scores.Length; ++i)
                {
                    double score = i == -1 ? table._neutralScore : table._scores[i];
                    score /= yTop;
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
    public override string ToString()
    {
        var teamScores = string.Join(", ", Teams.Select((team, index) => $"{team.Id}: {_scores[index]}"));
        return $"Team Scores: [{teamScores}], Neutral Score: {_neutralScore}, Max Score: {MaxScore}";
    }
    private struct GraphState
    {
        public TeamScoreTable Table;
        public bool ScaleToMaximum;
    }
}
