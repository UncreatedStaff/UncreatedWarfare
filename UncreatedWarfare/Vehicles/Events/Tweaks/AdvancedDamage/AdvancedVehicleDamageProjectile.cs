using Uncreated.Warfare.Vehicles.WarfareVehicles;
using Uncreated.Warfare.Vehicles.WarfareVehicles.Damage;

namespace Uncreated.Warfare.Vehicles.Events.Tweaks.AdvancedDamage;

public class AdvancedVehicleDamageProjectile : MonoBehaviour
{
    private bool _hitOccured;
    
    public Rocket RocketComponent { get; private set; }
    public ItemGunAsset FiringGunAsset {get; private set;}
    public AdvancedVehicleDamageProjectile Init(Rocket rocketComponent, ItemGunAsset firingGunAsset)
    {
        RocketComponent = rocketComponent;
        FiringGunAsset = firingGunAsset;
        _hitOccured = false;
        return this;
    }

    private void OnCollisionEnter(Collision collision)
    {
        Collider other = collision.collider;
        if (_hitOccured)
            return;
        
        bool directHit = !other.isTrigger && other.transform != RocketComponent.ignoreTransform &&
                         !other.transform.IsChildOf(RocketComponent.ignoreTransform);
        if (!directHit)
            return;

        _hitOccured = true;
        
        WarfareVehicleComponent comp = other.GetComponentInParent<WarfareVehicleComponent>();

        if (comp != null)
            comp.WarfareVehicle.AdvancedDamageApplier.RegisterDirectHitDamageMultiplier(AdvancedVehicleDamageApplier.GetComponentDamageMultiplier(other.transform));
    }
}