using System;
using System.Collections.Generic;
using System.Text;

namespace Uncreated.Warfare.Lobby;
public class DefaultTeamSelectorBehavior : ITeamSelectorBehavior
{
    public TeamInfo[] Teams { get; }

    public DefaultTeamSelectorBehavior(TeamInfo[] teams)
    {
        Teams = teams;
    }

    public bool CanJoinTeam(int index, int currentTeam = -1)
    {
        if (index >= Teams.Length || index < 0)
            throw new ArgumentOutOfRangeException(nameof(index));

        if (currentTeam >= Teams.Length)
            throw new ArgumentOutOfRangeException(nameof(index));

        return false;
    }
}
