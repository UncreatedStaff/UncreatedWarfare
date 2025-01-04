using System;
using Uncreated.Warfare.Configuration;
using Uncreated.Warfare.Logging.Formatting;
using Uncreated.Warfare.Translations.Addons;

namespace Uncreated.Warfare.Translations.ValueFormatters;
public class AssetValueFormatter : IValueFormatter<Asset>
{
    public static string Format(Asset asset, ITranslationValueFormatter formatter, in ValueFormatParameters parameters)
    {
        bool rarity = (parameters.Options & TranslationOptions.NoRichText) != 0
                      && (AssetLink.AssetLinkFriendly.Match(in parameters) || AssetLink.AssetLinkDescriptive.Match(in parameters));

        string name = rarity ? RarityColorAddon.Apply(asset.name, asset, formatter, in parameters) : $"\"{asset.name}\"";

        Guid guid = asset.GUID;
        ushort id = asset.id;
        string? idStr;
        if (id != 0)
        {
            Span<char> numSpan = stackalloc char[5];
            id.TryFormat(numSpan, out int len, "F0", parameters.Culture);
            idStr = formatter.Colorize(numSpan[..len], WarfareFormattedLogValues.NumberColor, parameters.Options);
        }
        else idStr = null;

        if (guid != Guid.Empty)
        {
            Span<char> span = stackalloc char[32];
            guid.TryFormat(span, out _, "N");
            string guidStr = formatter.Colorize(span, WarfareFormattedLogValues.StructColor, parameters.Options);

            return id == 0
                ? $"{name}{{{guidStr}}}"
                : $"{name}{{{guidStr}}} ({formatter.FormatEnum(asset.assetCategory, parameters.Language)}/{idStr!})";
        }

        return id == 0 ? name : $"{name}({formatter.FormatEnum(asset.assetCategory, parameters.Language)}/{idStr})";
    }

    public string Format(ITranslationValueFormatter formatter, Asset value, in ValueFormatParameters parameters)
    {
        return Format(value, formatter, in parameters);
    }

    public string Format(ITranslationValueFormatter formatter, object value, in ValueFormatParameters parameters)
    {
        return Format((Asset)value, formatter, in parameters);
    }
}
