using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Uncreated.Warfare.Events.Players;
public class BattlEyeKicked : PlayerEvent
{
    private readonly string _reason;
    public string KickReason => _reason;
    public BattlEyeKicked(UCPlayer player, string reason) : base(player)
    {
        _reason = reason;
    }
}
