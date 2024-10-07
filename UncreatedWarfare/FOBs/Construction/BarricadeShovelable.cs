using System;
using System.Collections.Generic;
using System.Text;
using Uncreated.Warfare.Buildables;
using Uncreated.Warfare.Players;

namespace Uncreated.Warfare.FOBs.Construction;
internal class BarricadeShovelable : PointsShoveable
{
    public IBuildable Buildable { get; private set; }
    public void Init(int hitsRemaining, IBuildable buildable)
    {
        base.Init(hitsRemaining);
        Buildable = buildable;
    }
    public override void Complete(WarfarePlayer shoveler)
    {
        
    }
}
