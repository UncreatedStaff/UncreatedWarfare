using Uncreated.Warfare.Configuration;

namespace Uncreated.Warfare.FOBs.SupplyCrates.Throwable.Bags.MedicBags;

public class ThrownMedicBagInfo
{
    public required IAssetLink<ItemAsset> ThrowableItemAsset { get; set; }
    public required IAssetLink<ItemBarricadeAsset> MedicBagBarricadeAsset { get; set; }

    public required float StartingHealingPoints { get; set; } = 5;
}