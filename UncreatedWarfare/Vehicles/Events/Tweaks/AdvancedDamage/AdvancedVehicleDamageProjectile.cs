using Uncreated.Warfare.Vehicles.WarfareVehicles;
using Uncreated.Warfare.Vehicles.WarfareVehicles.Damage;

namespace Uncreated.Warfare.Vehicles.Events.Tweaks.AdvancedDamage;

public class AdvancedVehicleDamageProjectile : MonoBehaviour
{
    private bool _hitOccured;
    
    public required Rocket RocketComponent { get; set; }

    private void Awake()
    {
        RocketComponent = GetComponent<Rocket>() ?? throw new MissingComponentException("AdvancedVehicleDamageProjectile must used on a game object with a Rocket component.");
        
        _hitOccured = false;
    }

    private void OnCollisionEnter(Collision collision)
    {
        Collider other = collision.collider;
        if (_hitOccured || other.isTrigger)
            return;
        
        if (RocketComponent.ignoreTransform != null && (other.transform == RocketComponent.ignoreTransform || other.transform.IsChildOf(RocketComponent.ignoreTransform)))
            return;

        _hitOccured = true;
        
        WarfareVehicleComponent comp = other.GetComponentInParent<WarfareVehicleComponent>();

        if (comp != null)
            comp.WarfareVehicle.AdvancedDamageApplier.RegisterPendingDamageForNextEvent(AdvancedVehicleDamageApplier.GetComponentDamageMultiplier(other.transform));
    }
}