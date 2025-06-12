using Uncreated.Warfare.Buildables;
using Uncreated.Warfare.Fobs;
using Uncreated.Warfare.Util;

namespace Uncreated.Warfare.FOBs;

/// <summary>
/// A physical FOB that's represented by an <see cref="IBuildable"/>.
/// </summary>
public interface IBuildableFob : IFob, ITransformObject
{
    IBuildable Buildable { get; }
}
