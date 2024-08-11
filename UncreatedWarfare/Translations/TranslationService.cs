using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using Microsoft.Extensions.DependencyInjection;
using Uncreated.Warfare.Translations.Languages;

namespace Uncreated.Warfare.Translations;
public class TranslationService : ITranslationService
{
    public const string TranslationsFolder = "Translations";

    private readonly ConcurrentDictionary<Type, TranslationCollection> _collections;
    private readonly IServiceProvider _serviceProvider;
    public IReadOnlyDictionary<Type, TranslationCollection> TranslationCollections { get; }
    public ITranslationValueFormatter ValueFormatter { get; }
    public LanguageService LanguageService { get; }
    public TranslationService(IServiceProvider serviceProvider)
    {
        _collections = new ConcurrentDictionary<Type, TranslationCollection>();
        _serviceProvider = serviceProvider;

        LanguageService = serviceProvider.GetRequiredService<LanguageService>();
        ValueFormatter = serviceProvider.GetRequiredService<ITranslationValueFormatter>();

        TranslationCollections = new ReadOnlyDictionary<Type, TranslationCollection>(_collections);
    }

    public T Get<T>() where T : TranslationCollection, new()
    {
        TranslationCollection c = _collections.GetOrAdd(typeof(T), _ => new T());

        c.TryInitialize(this, _serviceProvider);
        return (T)c;
    }
}

public interface ITranslationService
{
    /// <summary>
    /// Dictionary of all translation collections with their types as a key.
    /// </summary>
    IReadOnlyDictionary<Type, TranslationCollection> TranslationCollections { get; }

    /// <summary>
    /// Service used to format translation arguments.
    /// </summary>
    ITranslationValueFormatter ValueFormatter { get; }

    /// <summary>
    /// Service used to handle per-player language and culture settings.
    /// </summary>
    LanguageService LanguageService { get; }

    /// <summary>
    /// Get a translation collection from this provider.
    /// </summary>
    T Get<T>() where T : TranslationCollection, new();
}