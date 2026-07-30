namespace Uncreated.Warfare.FOBs.SupplyCrates;

public interface IAmmoStorage : ICombatSupply
{
    void SubtractAmmo(float ammoCount);
    float AmmoCount { get; }
}