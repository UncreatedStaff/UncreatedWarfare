namespace Uncreated.Warfare.Gamemodes.Interfaces
{
    public interface IFlagTeamObjectiveGamemode : IFlagRotation
    {
        Flags.Flag ObjectiveTeam1 { get; }
        Flags.Flag ObjectiveTeam2 { get; }
        int ObjectiveT1Index { get; }
        int ObjectiveT2Index { get; }
    }
}
