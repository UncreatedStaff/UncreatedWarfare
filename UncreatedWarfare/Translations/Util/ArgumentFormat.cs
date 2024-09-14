using System;
using Uncreated.Warfare.Translations.Addons;

namespace Uncreated.Warfare.Translations.Util;

public delegate string HandleApplyAddon(string text, in ValueFormatParameters formatParameters);
public struct ArgumentFormat
{
    internal IArgumentAddon[]? FormatAddons;
    internal string? Format;
    internal string? FormatDisplayName;
    internal bool UseForToString;

    public ArgumentFormat(string? fmt)
    {
        FormatAddons = Array.Empty<IArgumentAddon>();
        UseForToString = true;
        Format = fmt;
    }
    
    public ArgumentFormat(in SpecialFormat fmt)
    {
        FormatAddons = Array.Empty<IArgumentAddon>();
        FormatDisplayName = fmt.DisplayName;
        UseForToString = fmt.UseForToString;
        Format = fmt.Format;
    }

    public ArgumentFormat(string fmt, params IArgumentAddon[] addons)
    {
        FormatAddons = addons;
        UseForToString = true;
        Format = fmt;
    }

    public ArgumentFormat(in SpecialFormat fmt, params IArgumentAddon[] addons)
    {
        FormatAddons = addons;
        FormatDisplayName = fmt.DisplayName;
        UseForToString = fmt.UseForToString;
        Format = fmt.Format;
    }

    public ArgumentFormat(params IArgumentAddon[] addons)
    {
        FormatAddons = addons;
        UseForToString = true;
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

    /// <summary>
    /// If this format should be given to the <see cref="IFormattable.ToString(string,IFormatProvider)"/> function, otherwise it's just available to addons.
    /// </summary>
    public bool UseForToString { get; }
    
    public SpecialFormat(string displayName, string format, bool useForToString = true)
    {
        DisplayName = displayName;
        Format = format;
        UseForToString = useForToString;
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