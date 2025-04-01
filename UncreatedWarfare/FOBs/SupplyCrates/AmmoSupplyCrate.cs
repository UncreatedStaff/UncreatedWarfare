using Uncreated.Warfare.Configuration;
using Uncreated.Warfare.Fobs;

namespace Uncreated.Warfare.FOBs.SupplyCrates;

public class AmmoSupplyCrate : IAmmoStorage
{
    private readonly NearbySupplyCrates _nearbySupplyCrates;
    private readonly SupplyCrate _supplyCrate;
    public float AmmoCount => _nearbySupplyCrates.AmmoCount;
    public CSteamID Owner { get; }

    private AmmoSupplyCrate(SupplyCrate supplyCrate, FobManager fobManager)
    {
        _nearbySupplyCrates = NearbySupplyCrates.FromSingleCrate(supplyCrate, fobManager);
        Owner = supplyCrate.Buildable.Owner;
        _supplyCrate = supplyCrate;
    }

    public static AmmoSupplyCrate FromSupplyCrate(SupplyCrate supplyCrate, FobManager fobManager)
    {
        return new AmmoSupplyCrate(supplyCrate, fobManager);
    }
    public void SubtractAmmo(float ammoCount)
    {
        _nearbySupplyCrates.SubstractSupplies(ammoCount, SupplyType.Ammo, SupplyChangeReason.ConsumeGeneral);
    }

    /// <inheritdoc />
    public override string ToString()
    {
        return AssetLink.ToDisplayString(_supplyCrate.Buildable.Asset) + $" ({AmmoCount:F2} ammo)";
    }
}