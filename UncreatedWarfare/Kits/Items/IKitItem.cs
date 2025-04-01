using Uncreated.Warfare.Configuration;
using Uncreated.Warfare.Teams;
using Uncreated.Warfare.Util.Inventory;

namespace Uncreated.Warfare.Kits.Items;

public interface IKitItem : IItem
{
    uint PrimaryKey { get; }

    uint KitId { get; }
}

public class ConcreteClothingKitItem : ConcreteClothingItem, IKitItem
{
    /// <inheritdoc />
    public uint PrimaryKey { get; init; }

    /// <inheritdoc />
    public uint KitId { get; init; }

    public ConcreteClothingKitItem() { }

    public ConcreteClothingKitItem(uint pk, uint kitId, ClothingType clothingType, IAssetLink<ItemClothingAsset> item, byte[]? state, byte amount = byte.MaxValue, byte quality = 100)
        : base(clothingType, item, state, amount, quality)
    {
        PrimaryKey = pk;
        KitId = kitId;
    }
}

public class RedirectedClothingKitItem : RedirectedClothingItem, IKitItem
{
    /// <inheritdoc />
    public uint PrimaryKey { get; init; }

    /// <inheritdoc />
    public uint KitId { get; init; }

    public RedirectedClothingKitItem() { }

    public RedirectedClothingKitItem(uint pk, uint kitId, ClothingType clothingType, RedirectType item, string? variant)
        : base(clothingType, item, variant)
    {
        PrimaryKey = pk;
        KitId = kitId;
    }
}

public class ConcretePageKitItem : ConcretePageItem, IKitItem
{
    /// <inheritdoc />
    public uint PrimaryKey { get; init; }

    /// <inheritdoc />
    public uint KitId { get; init; }

    public ConcretePageKitItem() { }

    public ConcretePageKitItem(uint pk, uint kitId, byte x, byte y, Page page, byte rotation, IAssetLink<ItemAsset> item, byte[]? state, byte amount = byte.MaxValue, byte quality = 100)
        : base(x, y, page, rotation, item, state, amount, quality)
    {
        PrimaryKey = pk;
        KitId = kitId;
    }
}

public class RedirectedPageKitItem : RedirectedPageItem, IKitItem
{
    /// <inheritdoc />
    public uint PrimaryKey { get; init; }

    /// <inheritdoc />
    public uint KitId { get; init; }

    public RedirectedPageKitItem() { }

    public RedirectedPageKitItem(uint pk, uint kitId, byte x, byte y, Page page, byte rotation, RedirectType item, string? variant)
        : base(x, y, page, rotation, item, variant)
    {
        PrimaryKey = pk;
        KitId = kitId;
    }
}