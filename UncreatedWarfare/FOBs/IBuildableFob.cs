using System;
using System.Collections.Generic;
using System.Text;
using Uncreated.Warfare.Buildables;
using Uncreated.Warfare.Fobs;

namespace Uncreated.Warfare.FOBs;
/// <summary>
/// A physical FOB that's represented by an <see cref="IBuildable"/>.
/// </summary>
public interface IBuildableFob : IFob
{
    IBuildable Buildable { get; }
}
