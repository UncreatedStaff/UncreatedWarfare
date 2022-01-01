namespace Uncreated.Warfare.Gamemodes.Interfaces
{
    public interface IImplementsLeaderboard<Stats, StatTracker> : IEndScreen
    {
        Leaderboard<Stats, StatTracker> Leaderboard { get; }
    }
    public interface IEndScreen
    {
        bool isScreenUp { get; }
    }
}
