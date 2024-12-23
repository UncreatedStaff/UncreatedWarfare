using System;
using System.Collections.Generic;
using System.Text;
using Uncreated.Warfare.Fobs;

namespace Uncreated.Warfare.FOBs;

/// <summary>
/// A FOB that can store resources.
/// </summary>
public interface IResourceFob : IFob
{
    /// <summary>
    /// Number of Supplies on the FOB.
    /// </summary>
    float BuildCount { get; }
    float AmmoCount { get; }
}
