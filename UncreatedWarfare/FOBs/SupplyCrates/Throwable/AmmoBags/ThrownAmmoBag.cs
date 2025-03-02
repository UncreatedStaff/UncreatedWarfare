using Uncreated.Warfare.Configuration;
using Uncreated.Warfare.Players;
using Uncreated.Warfare.Util;
using Uncreated.Warfare.Util.Timing;

namespace Uncreated.Warfare.FOBs.SupplyCrates.Throwable;

public class ThrownAmmoBag : ThrownSupplyCrate
{
    private static readonly Collider[] TempHitColliders = new Collider[4];
    private readonly ItemBarricadeAsset _completedAmmoCrateAsset;
    private readonly ThrownComponent _thrownComponent;

    public ThrownAmmoBag(GameObject throwable, WarfarePlayer thrower, ItemThrowableAsset thrownAsset, ItemBarricadeAsset completedAmmoCrateAsset)
        : base(throwable, thrownAsset, thrower)
    {
        _completedAmmoCrateAsset = completedAmmoCrateAsset;
        _thrownComponent = _throwable.AddComponent<ThrownComponent>();
        _thrownComponent.OnThrowableDestroyed = OnThrowableDestroyed;
    }

    private void OnThrowableDestroyed()
    {
        int resultsCount = Physics.OverlapSphereNonAlloc(_throwable.transform.position, 0.5f, TempHitColliders, 
            LayerMasks.LARGE | LayerMasks.MEDIUM | LayerMasks.BARRICADE | LayerMasks.STRUCTURE | LayerMasks.GROUND | LayerMasks.GROUND2);

        if (resultsCount <= 0) // check that this ammo bag isn't being destroyed while midair
        {
            RespawnThrowableItem();
            return;
        }
            
        BuildableExtensions.DropBuildable(
            _completedAmmoCrateAsset,
            _throwable.transform.position,
            Quaternion.Euler(-90, _throwable.transform.eulerAngles.y, 0),
            _thrower.Steam64,
            _thrower.GroupId
        );
    }
}