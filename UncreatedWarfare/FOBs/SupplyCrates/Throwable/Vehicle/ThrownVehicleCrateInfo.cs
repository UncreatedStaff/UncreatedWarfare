using Uncreated.Warfare.Configuration;

namespace Uncreated.Warfare.FOBs.SupplyCrates.Throwable.Vehicle;

public class ThrownVehicleCrateInfo
{
    public required IAssetLink<ItemAsset> ThrowableItemAsset { get; set; }
    public required IAssetLink<EffectAsset> ResupplyEffect { get; set; }
}