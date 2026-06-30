using System;

namespace Uncreated.Warfare.Translations.Storage;

/// <summary>
/// Delegate type for <see cref="IEnumTranslationStorage{TEnum}.OnNeedsUpdating"/>.
/// </summary>
/// <param name="translations">The updated translations for all langauges.</param>
public delegate void EnumTranslationsUpdated<TEnum>(IReadOnlyDictionary<string, IReadOnlyDictionary<TEnum, string>> translations)
    where TEnum : unmanaged, Enum;

/// <summary>
/// Handles saving and loading translations for enum values.
/// </summary>
public interface IEnumTranslationStorage<TEnum> where TEnum : unmanaged, Enum
{
    /// <summary>
    /// Invoked when the file is changed and cached translations need to be updated.
    /// </summary>
    event EnumTranslationsUpdated<TEnum>? OnNeedsUpdating;

    /// <summary>
    /// Reads enum translations for all languages.
    /// </summary>
    IReadOnlyDictionary<string, IReadOnlyDictionary<TEnum, string>> Load();

    /// <summary>
    /// Writes all enum values to storage.
    /// </summary>
    /// <param name="languageCode">Language to write.</param>
    /// <param name="translations">List of all translations available for writing.</param>
    /// <param name="defaultTranslations">Default values for translations that might be missing when using <see cref="WriteTranslationsOptions.WriteMissingValues"/>.</param>
    /// <param name="baseFolder">Optionally override the base translations folder.</param>
    /// <param name="options">Options to change the writing behavior.</param>
    void Save(
        string languageCode,
        IReadOnlyDictionary<TEnum, string> translations,
        IReadOnlyDictionary<TEnum, string>? defaultTranslations,
        string? baseFolder = null,
        WriteTranslationsOptions options = WriteTranslationsOptions.Default
    );
}