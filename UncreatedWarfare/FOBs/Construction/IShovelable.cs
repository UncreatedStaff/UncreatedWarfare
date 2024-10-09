using System;
using System.Collections.Generic;
using System.Text;
using Uncreated.Warfare.Buildables;
using Uncreated.Warfare.Players;

namespace Uncreated.Warfare.FOBs.Construction;
public interface IShovelable : IBuildableComponent
{
    TickResponsibilityCollection Builders { get; }
    bool Shovel(WarfarePlayer shoveler, Vector3 point);
    void Complete(WarfarePlayer shoveler);
}