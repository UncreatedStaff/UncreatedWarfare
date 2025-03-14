using Uncreated.Warfare.Players;

namespace Uncreated.Warfare.FOBs.SupplyCrates.Throwable;

public abstract class ThrownSupplyCrate
{
    protected readonly GameObject Throwable;
    protected readonly WarfarePlayer Thrower;
    protected readonly ItemThrowableAsset ThrownAsset;

    protected ThrownSupplyCrate(GameObject throwable, ItemThrowableAsset thrownAsset, WarfarePlayer thrower)
    {
        Throwable = throwable;
        ThrownAsset = thrownAsset;
        Thrower = thrower;
    }

    protected void RespawnThrowableItem()
    {
        ItemManager.dropItem(new Item(ThrownAsset, EItemOrigin.CRAFT), Throwable.transform.position, false, true, false);
    }
}