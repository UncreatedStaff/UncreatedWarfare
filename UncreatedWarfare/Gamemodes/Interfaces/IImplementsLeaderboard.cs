using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Uncreated.Warfare.Gamemodes.Interfaces
{
    public interface IImplementsLeaderboard<Stats, StatTracker>
    {
        bool isScreenUp { get; }
        Leaderboard<Stats, StatTracker> Leaderboard { get; }
    }
}
