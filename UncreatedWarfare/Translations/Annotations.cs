using System;

namespace Uncreated.Warfare.Translations;

[AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct, Inherited = false)]
public sealed class TranslationCollectionAttribute : Attribute
{
    public string FileName { get; }
    public TranslationCollectionAttribute(string fileName)
    {
        FileName = fileName;
    }
}

[AttributeUsage(AttributeTargets.Field | AttributeTargets.Property)]
public sealed class TranslationDataAttribute : Attribute
{
    public string? Key { get; set; }
    public string? Description { get; set; }
    public string?[]? Parameters { get; set; }
    
    /// <summary>
    /// Whether or not to export this translation in a translation pack.
    /// Would be <see langword="false"/> in cases such as admin commands, when normal players wouldn't see the translations.
    /// </summary>
    public bool IsPriorityTranslation { get; set; }
}