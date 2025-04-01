using System;
using Uncreated.Warfare.Layouts.Teams;

namespace Uncreated.Warfare.Layouts.Flags;
public class SingleLeaderContest
{
    public int MaxPossiblePoints { get; }
    /// <summary>
    /// The <see cref="Team"/> currently leading the contest.
    /// </summary>
    public Team Leader { get; private set; }
    /// <summary>
    /// The current points the <see cref="Leader"/> of the contest has.
    /// </summary>
    public int LeaderPoints { get; private set; }
    /// <summary>
    /// Whether the compeition is won by the <see cref="Leader"/>.
    /// The value will become <see langword="true"/> when the Leader manages to reach max number of points possible.
    /// It will then remain <see langword="true"/> until the <see cref="LeaderPoints"/> is reduced to zero by other competing teams.
    /// </summary>
    public bool IsWon { get; private set; }
    /// <summary>
    /// Invoked when the compeition is one by a particular team.
    /// </summary>
    public event Action<int>? OnPointsChanged;
    /// <summary>
    /// Invoked when the compeition is one by a particular team.
    /// </summary>
    public event Action<Team>? OnWon;
    /// <summary>
    /// Invoked when the compeition is restarted after being won, i.e. when a particular team is awarded points causing <see cref="LeaderPoints"/> reach zero and <see cref="IsWon"/> is already <see langword="true"/>.
    /// </summary>
    public event Action<Team>? OnRestarted;
    public SingleLeaderContest(int maxPossiblePoints)
    {
        Leader = Team.NoTeam;
        MaxPossiblePoints = maxPossiblePoints;
        LeaderPoints = 0;
        IsWon = false;
    }
    public SingleLeaderContest(Team startingWinner, int maxPossiblePoints)
    {
        Leader = startingWinner;
        MaxPossiblePoints = maxPossiblePoints;
        if (startingWinner != Team.NoTeam)
        {
            LeaderPoints = maxPossiblePoints;
            IsWon = true;
        }
        else
        {
            LeaderPoints = 0;
            IsWon = false;
        }
    }
    private void IncrementPointsClamp(int points)
    {
        LeaderPoints = Mathf.Clamp(LeaderPoints + points, 0, MaxPossiblePoints);
    }

    /// <summary>
    /// Award points to specific team.
    /// All teams compete to be the only team reaching the max amount of points possible.
    /// If a certain team is currently leading, awarding this team poinst will increase <see cref="LeaderPoints"/>, 
    /// and all points awarded to other teams will reduce <see cref="LeaderPoints"/> until it reaches zero, thus allowing for a new leader to take charge.
    /// </summary>
    public void AwardPoints(Team team, int points)
    {
        int oldPoints = LeaderPoints;

        if (points == 0)
            return;

        if (team == Leader && LeaderPoints == MaxPossiblePoints)
            return;

        if (Leader == team || Leader == Team.NoTeam)
            IncrementPointsClamp(points);
        else
            IncrementPointsClamp(-points);

        //int change = Mathf.Abs(LeaderPoints - oldPoints);
        //if (change >= 0)
        //{
        //    // if Points become zero
        //    if (LeaderPoints == 0)
        //    {
        //        Leader = Team.NoTeam;
        //        IsWon = false;
        //        OnRestarted?.Invoke(team);
        //    }
        //    else if (Leader == Team.NoTeam)
        //        Leader = team;
        //
        //    if (LeaderPoints == MaxPossiblePoints)
        //        IsWon = true;
        //
        //    OnPointsChanged?.Invoke(LeaderPoints - oldPoints);
        //
        //    if (IsWon)
        //        OnWon?.Invoke(Leader);
        //}

        int change = LeaderPoints - oldPoints;
        if (change > 0)
        {
            if (oldPoints == 0)
                Leader = team;
            if (LeaderPoints == MaxPossiblePoints)
                IsWon = true;

            OnPointsChanged?.Invoke(change);

            if (IsWon)
                OnWon?.Invoke(Leader);
        }
        else if (change < 0)
        {
            // if Points become zero, contest is neutral again
            if (LeaderPoints == 0)
            {
                Leader = Team.NoTeam;
                IsWon = false;
                OnRestarted?.Invoke(team);
            }
            // otherwise, if there is no leader yet, set the new leader
            else if (Leader == Team.NoTeam)
                Leader = team;

            OnPointsChanged?.Invoke(change);
        }
    }
    public override string ToString()
    {
        return $"SingleLeaderContest: " +
               $"Leader = {Leader?.ToString() ?? "None"}, " +
               $"LeaderPoints = {LeaderPoints}/{MaxPossiblePoints}, " +
               $"IsWon = {IsWon}";
    }

}
