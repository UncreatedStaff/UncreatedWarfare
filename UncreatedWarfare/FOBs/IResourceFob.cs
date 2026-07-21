using Uncreated.Warfare.Fobs;
using Uncreated.Warfare.FOBs.Deployment;
using Uncreated.Warfare.FOBs.SupplyCrates;
using Uncreated.Warfare.Players;

namespace Uncreated.Warfare.FOBs;

/// <summary>
/// A FOB that can store resources.
/// </summary>
public interface IResourceFob : IFob
{
    float BuildCount { get; }
    float AmmoCount { get; }
    void ChangeSupplies(SupplyType supplyType, float amount, SupplyChangeReason reason, WarfarePlayer? instigator = null);
    bool IDeployable.AllowUnarmedDeploy => true;
}
