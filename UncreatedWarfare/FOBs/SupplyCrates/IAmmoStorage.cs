using Uncreated.Warfare.Players;

namespace Uncreated.Warfare.FOBs.SupplyCrates;

public interface IAmmoStorage
{
    void SubtractAmmo(float ammoCount);
    float AmmoCount { get; }
    CSteamID Owner { get; }
}