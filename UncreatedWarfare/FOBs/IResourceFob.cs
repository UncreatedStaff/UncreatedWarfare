using System;
using System.Collections.Generic;
using System.Text;
using Uncreated.Warfare.Fobs;
using Uncreated.Warfare.FOBs.SupplyCrates;

namespace Uncreated.Warfare.FOBs;

/// <summary>
/// A FOB that can store resources.
/// </summary>
public interface IResourceFob : IFob
{
    float BuildCount { get; }
    float AmmoCount { get; }
    void ChangeSupplies(SupplyType supplyType, float amount);
}
