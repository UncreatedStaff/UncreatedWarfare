using SDG.Unturned;

namespace Uncreated.Warfare.Gamemodes.Interfaces
{
    public interface IStats
    {
        Player Player { get; set; }
        ulong Steam64 { get; }
    }
    public interface IPVPModeStats : IStats
    {
        int Kills { get; }
        int Deaths { get; }
        float DamageDone { get; }
        float KDR { get; }
        void AddKill();
        void AddDeath();
        void AddDamage(float amount);
    }
    public interface ITeamPVPModeStats : IPVPModeStats
    {
        int Teamkills { get; }
        void AddTeamkill();
    }
    public interface ILongestShotTracker
    {
        Flags.LongestShot LongestShot { get; set; }
    }
    public interface IFobsTracker
    {
        int FOBsPlacedT1 { get; set; }
        int FOBsPlacedT2 { get; set; }
        int FOBsDestroyedT1 { get; set; }
        int FOBsDestroyedT2 { get; set; }
    }
    public interface IRevivesStats : IStats
    {
        int Revives { get; }
        void AddRevive();
    }
    public interface IExperienceStats : IStats
    {
        int XPGained { get; }
        int Credits { get; }
        void AddXP(int amount);
        void AddCredits(int amount);
    }
    public interface IFlagStats : IStats
    {
        int Captures { get; }
        void AddCapture();
        void AddCaptures(int amount);
    }
    public interface IFOBStats : IStats
    {
        int FOBsDestroyed { get; }
        int FOBsPlaced { get; }
        void AddFOBDestroyed();
        void AddFOBPlaced();
    }
}
