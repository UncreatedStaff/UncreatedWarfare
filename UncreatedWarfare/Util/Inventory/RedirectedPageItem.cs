using System;
using System.Globalization;
using Uncreated.Warfare.Kits.Items;
using Uncreated.Warfare.Logging.Formatting;
using Uncreated.Warfare.Teams;
using Uncreated.Warfare.Translations;

namespace Uncreated.Warfare.Util.Inventory;

public class RedirectedPageItem : IRedirectedItem, IPageItem
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
    public RedirectType Item { get; init; }

    /// <inheritdoc />
    public string? Variant { get; init; }

    public RedirectedPageItem()
    {
        Item = RedirectType.None;
    }

    public RedirectedPageItem(byte x, byte y, Page page, byte rotation, RedirectType item, string? variant)
    {
        X = x;
        Y = y;
        Page = page;
        Rotation = rotation;
        Item = item;
        Variant = variant;
    }

    /// <inheritdoc />
    public virtual bool Equals(IItem? other)
    {
        return other is IRedirectedItem r and IPageItem p
               && p.X == X
               && p.Y == Y
               && p.Page == Page
               && p.Rotation == Rotation
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
        // ReSharper disable NonReadonlyMemberInGetHashCode
        return HashCode.Combine(
            X,
            Y,
            Page,
            Rotation,
            Item
        );
        // ReSharper restore NonReadonlyMemberInGetHashCode
    }

    /// <inheritdoc />
    public string Translate(ITranslationValueFormatter formatter, in ValueFormatParameters parameters)
    {
        string b = $"Redirected Page     | {
            formatter.Colorize(formatter.FormatEnum(Page, parameters.Language), WarfareFormattedLogValues.EnumColor, parameters.Options)} ({
                formatter.Colorize(formatter.Format(X, in parameters), WarfareFormattedLogValues.NumberColor, parameters.Options)}, {
                    formatter.Colorize(formatter.Format(Y, in parameters), WarfareFormattedLogValues.NumberColor, parameters.Options)}) @ {
                        formatter.Colorize(formatter.Format(ItemUtility.RotationToDegrees(Rotation), in parameters), WarfareFormattedLogValues.NumberColor, parameters.Options)}° | {
                            formatter.Colorize(formatter.FormatEnum(Item, parameters.Language), WarfareFormattedLogValues.EnumColor, parameters.Options)}";

        return b;
    }

    /// <inheritdoc />
    public override string ToString()
    {
        return $"Redirected Page     | {Page} ({X.ToString(CultureInfo.InvariantCulture)}, {Y.ToString(CultureInfo.InvariantCulture)}) @ {ItemUtility.RotationToDegrees(Rotation)}° | {Item}";
    }
}