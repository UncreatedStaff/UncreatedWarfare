using System;
using Uncreated.Warfare.Configuration;
using Uncreated.Warfare.Fobs;
using Uncreated.Warfare.Layouts.Teams;

namespace Uncreated.Warfare.FOBs.SupplyCrates;

public class AmmoSupplyCrate : ITemporaryAmmoStorage
{
    private int _hasAmmoCountSub;
    private event Action? AmmoCountUpdatedIntl;

    private readonly NearbySupplyCrates _nearbySupplyCrates;
    private readonly SupplyCrate _supplyCrate;
    public bool CanChangeKit => _supplyCrate.Info is { CanChangeKit: true };
    public float AmmoCount => _nearbySupplyCrates.AmmoCount;
    public CSteamID Owner { get; }
    public Team Team => _supplyCrate.Team;

    public event Action? AmmoCountUpdated
    {
        add
        {
            switch (Interlocked.Exchange(ref _hasAmmoCountSub, 1))
            {
                case 0:
                    _supplyCrate.OnSupplyCountUpdated += HandleSupplyCountUpdated;
                    break;

                //   1: already subscribed
                
                case 2:
                    throw new ObjectDisposedException(nameof(AmmoSupplyCrate));
            }

            AmmoCountUpdatedIntl += value;
        }
        remove => AmmoCountUpdatedIntl -= value;
    }

    private AmmoSupplyCrate(SupplyCrate supplyCrate, FobManager fobManager)
    {
        _nearbySupplyCrates = NearbySupplyCrates.FromSingleCrate(supplyCrate, fobManager);
        Owner = supplyCrate.Buildable.Owner;
        _supplyCrate = supplyCrate;
    }

    private void HandleSupplyCountUpdated()
    {
        AmmoCountUpdatedIntl?.Invoke();
    }

    public static AmmoSupplyCrate FromSupplyCrate(SupplyCrate supplyCrate, FobManager fobManager)
    {
        return new AmmoSupplyCrate(supplyCrate, fobManager);
    }
    public void SubtractAmmo(float ammoCount)
    {
        _nearbySupplyCrates.SubtractSupplies(ammoCount, SupplyType.Ammo, SupplyChangeReason.ConsumeGeneral);
    }

    public override string ToString()
    {
        return AssetLink.ToDisplayString(_supplyCrate.Buildable.Asset) + $" ({AmmoCount:F2} ammo)";
    }

    public void Dispose()
    {
        if (Interlocked.Exchange(ref _hasAmmoCountSub, 2) == 1)
            _supplyCrate.OnSupplyCountUpdated -= HandleSupplyCountUpdated;
    }

    Vector3 IAmmoStorage.Point => _supplyCrate.Position;
    float IAmmoStorage.InteractRange => 8;
}