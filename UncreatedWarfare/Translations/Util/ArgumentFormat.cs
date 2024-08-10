using System;
using Uncreated.Warfare.Translations.Addons;

namespace Uncreated.Warfare.Translations.Util;

public delegate string HandleApplyAddon(string text, in ValueFormatParameters formatParameters);
public struct ArgumentFormat
{
    internal IArgumentAddon[] FormatAddons;
    internal string? Format;

    public ArgumentFormat(string? fmt)
    {
        FormatAddons = Array.Empty<IArgumentAddon>();
        Format = fmt;
    }

    public ArgumentFormat(string fmt, params IArgumentAddon[] addons)
    {
        FormatAddons = addons;
        Format = fmt;
    }

    public ArgumentFormat(params IArgumentAddon[] addons)
    {
        FormatAddons = addons;
        Format = null;
    }

    public static implicit operator ArgumentFormat(string? fmt)
    {
        return new ArgumentFormat(fmt);
    }
}
