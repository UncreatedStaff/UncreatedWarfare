namespace Uncreated.Warfare.Translations;
public readonly struct TranslationData
{
    public string? Description { get; }
    public string?[]? ParameterDescriptions { get; }

    /// <summary>
    /// Whether or not to export this translation in a translation pack.
    /// Would be <see langword="false"/> in cases such as admin commands, when normal players wouldn't see the translations.
    /// </summary>
    public bool IsPriorityTranslation { get; }
    public TranslationData(string? description, string?[]? parameterDescriptions, bool isPriorityTranslation)
    {
        Description = description;
        ParameterDescriptions = parameterDescriptions;
        IsPriorityTranslation = isPriorityTranslation;
    }
}