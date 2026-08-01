using System;
using Uncreated.Warfare.Configuration;
using Uncreated.Warfare.Fobs;
using Uncreated.Warfare.Layouts.Teams;
using Uncreated.Warfare.Util;

namespace Uncreated.Warfare.FOBs.SupplyCrates;

public class AmmoCrate : ITemporaryAmmoStorage
{
    private int _hasAmmoCountSub;
    private event Action? AmmoCountUpdatedIntl;
    
    private readonly SupplyCrate _supplyCrate;
    public bool CanChangeKit => _supplyCrate.Info is { CanChangeKit: true };

    public float AmmoCount
    {
        get
        {
            return _supplyCrate.SupplyCount;
        }
        private set
        {
            _supplyCrate.SupplyCount = value;
        }
    }

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
                    throw new ObjectDisposedException(nameof(AmmoCrate));
            }

            AmmoCountUpdatedIntl += value;
        }
        remove => AmmoCountUpdatedIntl -= value;
    }

    private AmmoCrate(SupplyCrate supplyCrate, FobManager fobManager)
    {
        _supplyCrate = supplyCrate;
        Owner = supplyCrate.Buildable.Owner;
    }

    private void HandleSupplyCountUpdated()
    {
        AmmoCountUpdatedIntl?.Invoke();
    }

    public static AmmoCrate FromSupplyCrate(SupplyCrate supplyCrate, FobManager fobManager)
    {
        return new AmmoCrate(supplyCrate, fobManager);
    }
    public void SubtractAmmo(float ammoCount)
    {
        AmmoCount = Mathf.Max(AmmoCount - ammoCount, 0);
        if (AmmoCount == 0)
        {
            _supplyCrate.Buildable.Destroy();
        }
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