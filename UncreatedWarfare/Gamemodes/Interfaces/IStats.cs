using SDG.Unturned;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Uncreated.Warfare.Gamemodes.Interfaces
{
    public interface IStats
    {
        Player Player { get; set; }
        ulong Steam64 { get; }
        void Update(float dt);
        void CheckGame();
    }
    public interface IPVPModeStats : IStats
    {
        int Kills { get; }
        int Deaths { get; }
        int DamageDone { get; }
        float KDR { get; }
        void AddKill();
        void AddDeath();
        void AddDamage(int amount);
    }
    public interface ITeamPVPModeStats : IPVPModeStats
    {
        int Teamkills { get; }
        void AddTeamkill();
    }
    public interface IRevivesStats : IStats
    {
        int Revives { get; }
        void AddRevive();
    }
    public interface IExperienceStats : IStats
    {
        int XPGained { get; }
        int OFPGained { get; }
        void AddXP(int amount);
        void AddOfficerPoints(int amount);
    }
    public interface IFlagStats : IStats
    {
        int Captures { get; }
        void AddCapture();
    }
    public interface IFOBStats : IStats
    {
        int FOBsDestroyed { get; }
        int FOBsPlaced { get; }
        void AddFOBDestroyed();
        void AddFOBPlaced();
    }
}
