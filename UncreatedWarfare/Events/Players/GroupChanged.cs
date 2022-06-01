using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Uncreated.Warfare.Events.Players;
public class GroupChanged : PlayerEvent
{
    public ulong OldGroup { get; private set; }
    public ulong NewGroup { get; private set; }
    public ulong NewTeam { get; private set; }
    public GroupChanged(UCPlayer player, ulong oldGroup, ulong newGroup) : base(player)
    {
        OldGroup = oldGroup;
        NewGroup = newGroup;
        NewTeam = NewGroup.GetTeam();
    }
}
