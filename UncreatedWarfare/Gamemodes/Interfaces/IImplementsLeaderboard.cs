namespace Uncreated.Warfare.Gamemodes.Interfaces
{
    public interface IImplementsLeaderboard<Stats, StatTracker> : IEndScreen where Stats : BasePlayerStats where StatTracker : BaseStatTracker<Stats>
    {
        Leaderboard<Stats, StatTracker>? Leaderboard { get; }
        StatTracker WarstatsTracker { get; internal set; }
    }
    public interface IEndScreen : IGamemode
    {
        bool IsScreenUp { get; }
    }
}
