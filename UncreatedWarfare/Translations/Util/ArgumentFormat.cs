using System;
using Uncreated.Warfare.Translations.Addons;

namespace Uncreated.Warfare.Translations.Util;

public delegate string HandleApplyAddon(string text, in ValueFormatParameters formatParameters);
public struct ArgumentFormat
{
    internal IArgumentAddon[]? FormatAddons;
    internal string? Format;
    internal string? FormatDisplayName;

    public ArgumentFormat(string? fmt)
    {
        FormatAddons = Array.Empty<IArgumentAddon>();
        Format = fmt;
    }
    
    public ArgumentFormat(SpecialFormat fmt)
    {
        FormatAddons = Array.Empty<IArgumentAddon>();
        Format = fmt.Format;
        FormatDisplayName = fmt.DisplayName;
    }

    public ArgumentFormat(string fmt, params IArgumentAddon[] addons)
    {
        FormatAddons = addons;
        Format = fmt;
    }

    public ArgumentFormat(SpecialFormat fmt, params IArgumentAddon[] addons)
    {
        FormatAddons = addons;
        Format = fmt.Format;
        FormatDisplayName = fmt.DisplayName;
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
    public static implicit operator ArgumentFormat(SpecialFormat fmt)
    {
        return new ArgumentFormat(fmt);
    }
}

/// <summary>
/// Describes a format preset with a display name. Meant to be defined as init-only constants.
/// </summary>
public readonly struct SpecialFormat
{
    /// <summary>
    /// The format type name as displayed in the comments of the translation file.
    /// </summary>
    public string DisplayName { get; }

    /// <summary>
    /// The actual case-sensitive string used to identify the format.
    /// </summary>
    public string Format { get; }
    
    public SpecialFormat(string displayName, string format)
    {
        DisplayName = displayName;
        Format = format;
    }

    /// <summary>
    /// Check if a format matches this format.
    /// </summary>
    public bool Match(string? fmt)
    {
        return fmt != null && fmt.Equals(Format, StringComparison.Ordinal);
    }

    /// <summary>
    /// Check if a format matches this format.
    /// </summary>
    public bool Match(in ValueFormatParameters args)
    {
        return Match(args.Format.Format);
    }
}