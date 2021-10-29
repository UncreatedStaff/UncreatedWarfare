using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Uncreated.Warfare.Gamemodes.Interfaces
{
    public interface IAttackDefence : ITeams
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
