using Uncreated.Warfare.Layouts.Teams;

namespace Uncreated.Warfare.Layouts.Flags;

public class SingleLeaderContest
{
    public delegate void PointsChanged(int changeInPoints);
    public delegate void Won(Team winner);
    public delegate void Restarted(Team neutralizer, Team oldLeader);

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
    /// Whether the competition is won by the <see cref="Leader"/>.
    /// The value will become <see langword="true"/> when the Leader manages to reach max number of points possible.
    /// It will then remain <see langword="true"/> until the <see cref="LeaderPoints"/> is reduced to zero by other competing teams.
    /// </summary>
    public bool IsWon { get; private set; }

    /// <summary>
    /// Invoked when the competition is one by a particular team.
    /// </summary>
    public event PointsChanged? OnPointsChanged;

    /// <summary>
    /// Invoked when the competition is one by a particular team.
    /// </summary>
    public event Won? OnWon;

    /// <summary>
    /// Invoked when the competition is restarted after being won, i.e. when a particular team is awarded points causing <see cref="LeaderPoints"/> reach zero and <see cref="IsWon"/> is already <see langword="true"/>.
    /// </summary>
    public event Restarted? OnRestarted;
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

        int change = LeaderPoints - oldPoints;
        if (change > 0)
        {
            if (oldPoints == 0)
                Leader = team;

            // need this to prevent winning every tick when clearing a point
            bool justWon = LeaderPoints == MaxPossiblePoints;
            if (justWon)
                IsWon = true;

            OnPointsChanged?.Invoke(change);

            if (justWon)
                OnWon?.Invoke(Leader);
        }
        else if (change < 0)
        {
            Team oldLeader = Leader;
            // if Points become zero, contest is neutral again
            if (LeaderPoints == 0)
            {
                Leader = Team.NoTeam;
                IsWon = false;
                OnRestarted?.Invoke(team, oldLeader);
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
