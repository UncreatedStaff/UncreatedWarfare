using System;
using System.Globalization;
using Uncreated.Warfare.Configuration;
using Uncreated.Warfare.Logging.Formatting;
using Uncreated.Warfare.Translations;

namespace Uncreated.Warfare.Util.Inventory;

public class ConcreteClothingItem : IConcreteItem, IClothingItem
{
    public ClothingType ClothingType { get; init; }

    public IAssetLink<ItemClothingAsset> Item { get; init; }

    public byte[]? State { get; init; }

    public byte Amount { get; init; }

    public byte Quality { get; init; }

    public ConcreteClothingItem()
    {
        Item = AssetLink.Empty<ItemClothingAsset>();
        Quality = 100;
        Amount = byte.MaxValue;
    }

    public ConcreteClothingItem(ClothingType clothingType, IAssetLink<ItemClothingAsset> item, byte[]? state, byte amount = byte.MaxValue, byte quality = 100)
    {
        ClothingType = clothingType;
        Item = item ?? AssetLink.Empty<ItemClothingAsset>();
        State = state;
        Amount = amount;
        Quality = quality;
    }

    /// <inheritdoc />
    public virtual bool Equals(IItem? other)
    {
        return other is IConcreteItem c and IClothingItem p
               && p.ClothingType == ClothingType
               && c.Amount == Amount
               && c.Quality == Quality
               && c.Item.MatchAsset(Item)
               && CollectionUtility.CompareBytes(c.State, State);
    }

    /// <inheritdoc />
    public override bool Equals(object? obj)
    {
        return Equals(obj as IItem);
    }

    /// <inheritdoc />
    public override int GetHashCode()
    {
        // ReSharper disable NonReadonlyMemberInGetHashCode
        return HashCode.Combine(
            ClothingType,
            Amount,
            Quality,
            Item
        );
        // ReSharper restore NonReadonlyMemberInGetHashCode
    }

    /// <inheritdoc />
    public string Translate(ITranslationValueFormatter formatter, in ValueFormatParameters parameters)
    {
        string b = $"Concrete Clothing   | {
            formatter.Colorize(formatter.FormatEnum(ClothingType, parameters.Language), WarfareFormattedLogValues.EnumColor, parameters.Options)}";

        if (Amount != byte.MaxValue)
            b += " amt=" + formatter.Colorize(formatter.Format(Amount, in parameters), WarfareFormattedLogValues.NumberColor, parameters.Options);
        if (Quality < 100)
            b += " quality=" + formatter.Colorize(formatter.Format(Quality, in parameters), WarfareFormattedLogValues.NumberColor, parameters.Options);

        b += " | " + Item.Translate(formatter, in parameters);
        return b;
    }

    /// <inheritdoc />
    public override string ToString()
    {
        string b = $"Concrete Clothing   | {ClothingType}";

        if (Amount != byte.MaxValue)
            b += " amt=" + Amount.ToString(CultureInfo.InvariantCulture);
        if (Quality < 100)
            b += " quality=" + Quality.ToString(CultureInfo.InvariantCulture);

        b += " | " + Item.ToDisplayString();
        return b;
    }


    IAssetLink<ItemAsset> IConcreteItem.Item => Item;
}