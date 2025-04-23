using StackCleaner;
using System;
using System.Collections.Generic;
using Uncreated.Warfare.Models.Localization;
using Uncreated.Warfare.Translations;
using Uncreated.Warfare.Translations.Languages;
using Uncreated.Warfare.Translations.Storage;

namespace Uncreated.Warfare.Tests.Utility;

public class TestTranslationCollection : TranslationCollection
{
    /// <inheritdoc />
    public override ITranslationStorage Storage { get; } = new TestTranslationStorage();

    /// <inheritdoc />
    public override string ToString()
    {
        return "Tests";
    }
}

public class TestTranslationStorage : ITranslationStorage
{
    /// <inheritdoc />
    public void Save(IEnumerable<Translation> translations, LanguageInfo language = null)
    {

    }

    /// <inheritdoc />
    public IReadOnlyDictionary<TranslationLanguageKey, string> Load()
    {
        return new Dictionary<TranslationLanguageKey, string>(0);
    }

    /// <inheritdoc />
    public override string ToString()
    {
        return "Tests";
    }
}

public class TestTranslationService : ITranslationService
{
    /// <inheritdoc />
    public IReadOnlyDictionary<Type, TranslationCollection> TranslationCollections => throw new NotImplementedException();

    /// <inheritdoc />
    public ITranslationValueFormatter ValueFormatter => throw new NotImplementedException();

    /// <inheritdoc />
    public LanguageService LanguageService => throw new NotImplementedException();

    /// <inheritdoc />
    public LanguageSets SetOf => throw new NotImplementedException();

    /// <inheritdoc />
    public StackColorFormatType TerminalColoring => StackColorFormatType.ExtendedANSIColor;

    /// <inheritdoc />
    public T Get<T>() where T : TranslationCollection, new()
    {
        throw new NotImplementedException();
    }

    /// <inheritdoc />
    public void ReloadAll()
    {

    }
}
