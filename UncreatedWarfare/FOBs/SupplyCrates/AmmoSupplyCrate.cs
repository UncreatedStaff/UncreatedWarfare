using Uncreated.Warfare.Buildables;
using Uncreated.Warfare.Fobs;
using Uncreated.Warfare.Layouts.Teams;
using Uncreated.Warfare.Players;

namespace Uncreated.Warfare.FOBs.SupplyCrates;

public class AmmoSupplyCrate : IAmmoStorage
{
    private readonly NearbySupplyCrates _nearbySupplyCrates;
    public float AmmoCount => _nearbySupplyCrates.AmmoCount;
    public CSteamID Owner { get; }

    private AmmoSupplyCrate(SupplyCrate supplyCrate, FobManager fobManager)
    {
        _nearbySupplyCrates = NearbySupplyCrates.FromSingleCrate(supplyCrate, fobManager);
        Owner = supplyCrate.Buildable.Owner;
    }

    public static AmmoSupplyCrate FromSupplyCrate(SupplyCrate supplyCrate, FobManager fobManager)
    {
        return new AmmoSupplyCrate(supplyCrate, fobManager);
    }
    public void SubtractAmmo(float ammoCount)
    {
        _nearbySupplyCrates.SubstractSupplies(ammoCount, SupplyType.Ammo, SupplyChangeReason.ConsumeGeneral);
    }

}