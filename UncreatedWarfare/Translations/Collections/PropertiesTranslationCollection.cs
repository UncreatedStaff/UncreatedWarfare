using System;
using Uncreated.Warfare.Translations.Storage;

namespace Uncreated.Warfare.Translations.Collections;

public abstract class PropertiesTranslationCollection : TranslationCollection
{
    private ITranslationStorage _storage = null!;

    /// <summary>
    /// Name of the translation file without an extension.
    /// </summary>
    protected abstract string FileName { get; }
    public override ITranslationStorage Storage => _storage;
    protected override void Initialize(ITranslationService translationService, IServiceProvider serviceProvider)
    {
        _storage = new PropertiesTranslationStorage(FileName, serviceProvider);
        base.Initialize(translationService, serviceProvider);
    }
}