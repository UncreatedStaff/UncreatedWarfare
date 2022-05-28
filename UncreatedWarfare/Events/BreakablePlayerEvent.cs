using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Uncreated.Warfare.Events;
public class BreakablePlayerEvent : BreakableEvent
{
    private readonly UCPlayer _player;
    public UCPlayer Player => _player;
    public ulong Steam64 => _player.Steam64;
    public BreakablePlayerEvent(UCPlayer player, bool shouldAllow)
    {
        _player = player;
        if (!shouldAllow) Break();
    }
    public BreakablePlayerEvent(UCPlayer player)
    {
        _player = player;
    }
}
