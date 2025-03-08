using System;
using System.Collections.Generic;
using Uncreated.Warfare.Layouts.Teams;
using Uncreated.Warfare.Players;
using Uncreated.Warfare.Players.Management;

namespace Uncreated.Warfare.Lobby;
public class DefaultTeamSelectorBehavior : ITeamSelectorBehavior
{
    /// <summary>
    /// Minimum allowed standard deviations of team player counts to allow players to join any team.
    /// </summary>
    // todo config
    private const double MaximumStandardDeviation = 2;

    /// <summary>
    /// Minimum allowed standard deviations from the team the player's trying to join to the mean of all teams.
    /// </summary>
    private const double MaximumScore = 0.70;

    private readonly IPlayerService _playerService;
    public TeamInfo[]? Teams { get; set; }

    public DefaultTeamSelectorBehavior(IPlayerService playerService)
    {
        _playerService = playerService;
    }

    public void UpdateTeams()
    {
        if (Teams == null)
            return;

        for (int i = 0; i < Teams.Length; ++i)
        {
            List<WarfarePlayer>.Enumerator enumerator = _playerService.OnlinePlayers.GetEnumerator();

            ref TeamInfo teamInfo = ref Teams[i];
            Team? team = teamInfo.Team;
            int ct = 0;
            while (enumerator.MoveNext())
            {
                if (enumerator.Current!.Team == team)
                    ++ct;
            }

            teamInfo.PlayerCount = ct;
            enumerator.Dispose();
        }
    }

    public bool CanJoinTeam(int index, int currentTeam = -1)
    {
        if (Teams == null || index >= Teams.Length || index < 0)
            throw new ArgumentOutOfRangeException(nameof(index));

        if (currentTeam >= Teams.Length)
            throw new ArgumentOutOfRangeException(nameof(index));

        CalculateTeamMetrics(currentTeam, out double variance, out _, out double mean);

        int playerCount = Teams[index].PlayerCount;

        if (playerCount <= mean)
            return true;

        if (variance <= MaximumStandardDeviation * MaximumStandardDeviation)
        {
            double stdDev = Math.Sqrt(variance);
            double zScore = (playerCount - mean) / stdDev;
            return zScore <= MaximumScore;
        }

        return false;
    }

    private void CalculateTeamMetrics(int teamIndexToRemoveOneFrom, out double variance, out int minTeam, out double mean)
    {
        // calculate std dev and range of player counts
        int maxTeam = -1; minTeam = -1;
        double total = 0;
        int max = int.MinValue, min = int.MaxValue;

        if (Teams == null)
        {
            throw new InvalidOperationException("Teams not initialized.");
        }

        for (int i = 0; i < Teams.Length; ++i)
        {
            ref TeamInfo teamInfo = ref Teams[i];

            int playerCount = teamInfo.PlayerCount;
            if (i == teamIndexToRemoveOneFrom)
                --playerCount;

            if (playerCount <= min)
            {
                min = playerCount;
                minTeam = i;
            }

            if (playerCount >= max)
            {
                max = playerCount;
                maxTeam = i;
            }

            total += playerCount;
        }

        if (minTeam == maxTeam)
            minTeam = Teams.Length - minTeam - 1;

        mean = total / Teams.Length;
        double varTotalNum = 0;
        for (int i = 0; i < Teams.Length; ++i)
        {
            ref TeamInfo teamInfo = ref Teams[i];

            int playerCount = teamInfo.PlayerCount;
            if (i == teamIndexToRemoveOneFrom)
                --playerCount;

            double difference = playerCount - mean;
            varTotalNum += difference * difference;
        }

        variance = varTotalNum / (Teams.Length - 1);
    }
}