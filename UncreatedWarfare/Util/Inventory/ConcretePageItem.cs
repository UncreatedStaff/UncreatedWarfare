using System;
using System.Globalization;
using Uncreated.Warfare.Configuration;
using Uncreated.Warfare.Kits.Items;
using Uncreated.Warfare.Logging.Formatting;
using Uncreated.Warfare.Translations;

namespace Uncreated.Warfare.Util.Inventory;

public class ConcretePageItem : IConcreteItem, IPageItem
{
    /// <inheritdoc />
    public byte X { get; init; }

    /// <inheritdoc />
    public byte Y { get; init; }

    /// <inheritdoc />
    public Page Page { get; init; }

    /// <inheritdoc />
    public byte Rotation { get; init; }

    /// <inheritdoc />
    public IAssetLink<ItemAsset> Item { get; init; }

    /// <inheritdoc />
    public byte[]? State { get; init; }

    /// <inheritdoc />
    public byte Amount { get; init; }

    /// <inheritdoc />
    public byte Quality { get; init; }

    public ConcretePageItem()
    {
        Item = AssetLink.Empty<ItemAsset>();
        Quality = 100;
        Amount = byte.MaxValue;
    }

    public ConcretePageItem(byte x, byte y, Page page, byte rotation, IAssetLink<ItemAsset> item, byte[]? state, byte amount = byte.MaxValue, byte quality = 100)
    {
        X = x;
        Y = y;
        Page = page;
        Rotation = rotation;
        Item = item ?? AssetLink.Empty<ItemAsset>();
        State = state;
        Amount = amount;
        Quality = quality;
    }

    /// <inheritdoc />
    public virtual bool Equals(IItem? other)
    {
        return other is IConcreteItem c and IPageItem p
               && p.X == X
               && p.Y == Y
               && p.Page == Page
               && p.Rotation == Rotation
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
            X,
            Y,
            Page,
            Rotation,
            Amount,
            Quality,
            Item
        );
        // ReSharper restore NonReadonlyMemberInGetHashCode
    }

    /// <inheritdoc />
    public string Translate(ITranslationValueFormatter formatter, in ValueFormatParameters parameters)
    {
        string b = $"Concrete Page       | {formatter.Colorize(formatter.FormatEnum(Page, parameters.Language), WarfareFormattedLogValues.EnumColor, parameters.Options),9
            } ({formatter.Colorize(formatter.Format(X, in parameters), WarfareFormattedLogValues.NumberColor, parameters.Options)
            }, {formatter.Colorize(formatter.Format(Y, in parameters), WarfareFormattedLogValues.NumberColor, parameters.Options)
            }) @ {formatter.Colorize(formatter.Format(ItemUtility.RotationToDegrees(Rotation), in parameters), WarfareFormattedLogValues.NumberColor, parameters.Options)
        }°";

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
        string b = $"Concrete Page       | {Page,9} ({X.ToString(CultureInfo.InvariantCulture)}, {Y.ToString(CultureInfo.InvariantCulture)}) @ {ItemUtility.RotationToDegrees(Rotation)}°";

        if (Amount != byte.MaxValue)
            b += " amt=" + Amount.ToString(CultureInfo.InvariantCulture);
        if (Quality < 100)
            b += " quality=" + Quality.ToString(CultureInfo.InvariantCulture);

        b += " | " + Item.ToDisplayString();
        return b;
    }
}