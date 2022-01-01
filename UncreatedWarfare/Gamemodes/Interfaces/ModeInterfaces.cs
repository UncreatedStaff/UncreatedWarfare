namespace Uncreated.Warfare.Gamemodes.Interfaces
{
    public interface IAttackDefense : ITeams
    {
        ulong AttackingTeam { get; }
        ulong DefendingTeam { get; }
    }
    public interface ITeamScore : ITeams
    {
        int Team1Score { get; }
        int Team2Score { get; }
    }
}
