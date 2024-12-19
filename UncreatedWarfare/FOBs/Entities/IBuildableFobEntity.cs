using System;
using System.Collections.Generic;
using System.Text;
using Uncreated.Warfare.Buildables;

namespace Uncreated.Warfare.FOBs.Entities;
public interface IBuildableFobEntity : IFobEntity
{
    public IBuildable Buildable { get; }
}
