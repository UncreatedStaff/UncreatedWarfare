namespace Uncreated.Warfare.Gamemodes.Interfaces
{
    public interface IImplementsLeaderboard<in TStats, TStatTracker> : IImplementsLeaderboard where TStats : BasePlayerStats where TStatTracker : BaseStatTracker<TStats>
    {
        new ILeaderboard<TStats, TStatTracker>? Leaderboard { get; }
        TStatTracker WarstatsTracker { get; internal set; }
    }
    public interface IImplementsLeaderboard : IEndScreen
    {
        ILeaderboard? Leaderboard { get; }
    }
    public interface IEndScreen : IGamemode
    {
        bool IsScreenUp { get; }
    }
}
