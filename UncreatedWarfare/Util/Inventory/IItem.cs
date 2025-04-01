using System;
using Uncreated.Warfare.Configuration;
using Uncreated.Warfare.Kits.Items;
using Uncreated.Warfare.Teams;
using Uncreated.Warfare.Translations.ValueFormatters;

namespace Uncreated.Warfare.Util.Inventory;

public interface IItem : IEquatable<IItem>, ITranslationArgument;

public interface IClothingItem : IItem
{
    ClothingType ClothingType { get; }
}

public interface IPageItem : IItem
{
    byte X { get; }
    byte Y { get; }
    Page Page { get; }
    byte Rotation { get; }
}

public interface IConcreteItem : IItem
{
    IAssetLink<ItemAsset> Item { get; }
    byte[]? State { get; }
    byte Amount { get; }
    byte Quality { get; }
}

public interface IRedirectedItem : IItem
{
    RedirectType Item { get; }
    string? Variant { get; }
}