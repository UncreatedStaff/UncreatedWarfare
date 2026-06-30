using System;

namespace Uncreated.Warfare.Translations.Storage;

/// <summary>
/// Options for writing translations to file.
/// </summary>
[Flags]
public enum WriteTranslationsOptions
{
    /// <summary>
    /// Writes missing values in the default language.
    /// </summary>
    WriteMissingValues = 1,

    /// <summary>
    /// Don't write comments or extra whitespace.
    /// </summary>
    Minimal = 2,

    /// <summary>
    /// Only write translations that are marked as prioritized.
    /// </summary>
    PrioritizedOnly = 4,

    /// <summary>
    /// Default options, only including given translations.
    /// </summary>
    Default = 0
}
