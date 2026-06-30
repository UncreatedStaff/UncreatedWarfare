using System;

namespace Uncreated.Warfare.Translations;

/// <summary>
/// Indicates that an enum type can be translated.
/// </summary>
[AttributeUsage(AttributeTargets.Enum)]
public sealed class TranslatableAttribute : Attribute
{
    /// <summary>
    /// The name of the file, overriding the CLR type's full name.
    /// </summary>
    public string? FileName { get; }

    /// <summary>
    /// Description of this enum.
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// Whether or not this entire enum will be included in translation packs.
    /// </summary>
    public bool IsPrioritizedTranslation { get; set; } = true;

    public TranslatableAttribute() { }

    public TranslatableAttribute(string? fileName)
    {
        FileName = fileName;
    }

}

/// <summary>
/// Indicates the default value for the default language for this enum field.
/// </summary>
[AttributeUsage(AttributeTargets.Field)]
public sealed class TranslatableValueAttribute : Attribute
{
    /// <summary>
    /// Whether or not this singe enum field will be included in translation packs.
    /// </summary>
    public bool IsPrioritizedTranslation { get; set; } = true;

    /// <summary>
    /// Default value for the default language for this enum field's translation.
    /// </summary>
    public string? Original { get; }

    /// <summary>
    /// Extra description for a value.
    /// </summary>
    public string? Description { get; set; }

    public TranslatableValueAttribute() { }
    public TranslatableValueAttribute(string original)
    {
        Original = original;
    }
}