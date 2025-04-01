using System.Collections.Generic;
using Uncreated.Warfare.Models.Localization;

namespace Uncreated.Warfare.Translations.Storage;
public interface ITranslationStorage
{
    void Save(IEnumerable<Translation> translations, LanguageInfo? language = null);
    IReadOnlyDictionary<TranslationLanguageKey, string> Load();
}