using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Uncreated.Warfare.Gamemodes.Flags.TeamCTF;

namespace Uncreated.Warfare.Gamemodes.Interfaces
{
    public interface IWarstatsGamemode : IGamemode
    {
        EndScreenLeaderboard EndScreen { get; }
        bool isScreenUp { get; }
        WarStatsTracker GameStats { get; }
    }
}
