using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Uncreated.Warfare.Squads.Commander;
public class Commanders
{
    private UCPlayer? _commanderT1;
    private UCPlayer? _commanderT2;

    public UCPlayer? ActiveCommanderTeam1
    {
        get => _commanderT1;
        internal set
        {
            if (value == _commanderT1)
                return;
            UCPlayer? old = _commanderT1;
            _commanderT1 = value;
            OnCommanderChanged(old, value, 1ul);
        }
    }
    public UCPlayer? ActiveCommanderTeam2
    {
        get => _commanderT2;
        internal set
        {
            if (value == _commanderT2)
                return;
            UCPlayer? old = _commanderT2;
            _commanderT2 = value;
            OnCommanderChanged(old, value, 2ul);
        }
    }
    public UCPlayer? GetCommander(ulong team)
    {
        if (team == 1)
            return _commanderT1;
        else if (team == 2)
            return _commanderT2;
        return null;
    }

    public bool IsCommander(UCPlayer player)
    {
        UCPlayer? cmd = GetCommander(player.GetTeam());
        return cmd != null && cmd.Steam64 == player.Steam64;
    }

    private void OnCommanderChanged(UCPlayer? old, UCPlayer? commander, ulong team)
    {

    }
}
