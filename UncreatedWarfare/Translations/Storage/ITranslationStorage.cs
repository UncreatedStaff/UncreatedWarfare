using Uncreated.Warfare.Models.Localization;

namespace Uncreated.Warfare.Translations.Storage;

/// <summary>
/// Delegate type for <see cref="ITranslationStorage.OnNeedsUpdating"/>.
/// </summary>
/// <param name="translations">The updated translations for all languages.</param>
public delegate void TranslationsUpdated(IReadOnlyDictionary<TranslationLanguageKey, string> translations);

/// <summary>
/// Manages reading and writing translation collections.
/// </summary>
public interface ITranslationStorage
{
    /// <summary>
    /// Invoked when the file is changed and cached translations need to be updated.
    /// </summary>
    event TranslationsUpdated? OnNeedsUpdating;

    /// <summary>
    /// Reads all translations from the storage medium for all languages.
    /// </summary>
    IReadOnlyDictionary<TranslationLanguageKey, string> Load();

    /// <summary>
    /// Writes all translations to the storage medium for a given language.
    /// </summary>
    /// <param name="translations">List of translations to save.</param>
    /// <param name="language">The language to save, or <see langword="null"/> for the default language.</param>
    /// <param name="baseFolder">Optionally override the base translations folder.</param>
    /// <param name="options">Options to change the writing behavior.</param>
    void Save(
        IEnumerable<Translation> translations,
        LanguageInfo? language = null,
        string? baseFolder = null,
        WriteTranslationsOptions options = WriteTranslationsOptions.Default
    );
}