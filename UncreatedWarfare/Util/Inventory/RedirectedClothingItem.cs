using System;
using Uncreated.Warfare.Logging.Formatting;
using Uncreated.Warfare.Teams;
using Uncreated.Warfare.Translations;

namespace Uncreated.Warfare.Util.Inventory;

public class RedirectedClothingItem : IRedirectedItem, IClothingItem
{
    /// <inheritdoc />
    public ClothingType ClothingType { get; init; }

    /// <inheritdoc />
    public RedirectType Item { get; init; }

    /// <inheritdoc />
    public string? Variant { get; init; }

    public RedirectedClothingItem() { }

    public RedirectedClothingItem(ClothingType clothingType, RedirectType item, string? variant)
    {
        ClothingType = clothingType;
        Item = item;
        Variant = variant;
    }

    /// <inheritdoc />
    public virtual bool Equals(IItem? other)
    {
        return other is IRedirectedItem r and IClothingItem c
               && c.ClothingType == ClothingType
               && r.Item == Item
               && string.Equals(r.Variant, Variant, StringComparison.Ordinal);
    }

    /// <inheritdoc />
    public override bool Equals(object? obj)
    {
        return Equals(obj as IItem);
    }

    /// <inheritdoc />
    public override int GetHashCode()
    {
        return HashCode.Combine(
            ClothingType,
            Item
        );
    }

    /// <inheritdoc />
    public string Translate(ITranslationValueFormatter formatter, in ValueFormatParameters parameters)
    {
        string b = $"Redirected Clothing | {
            formatter.Colorize(formatter.FormatEnum(ClothingType, parameters.Language), WarfareFormattedLogValues.EnumColor, parameters.Options),9} | {
                formatter.Colorize(formatter.FormatEnum(Item, parameters.Language), WarfareFormattedLogValues.EnumColor, parameters.Options)}";

        return b;
    }

    /// <inheritdoc />
    public override string ToString()
    {
        return $"Redirected Clothing | {ClothingType,9} | {Item}";
    }
}