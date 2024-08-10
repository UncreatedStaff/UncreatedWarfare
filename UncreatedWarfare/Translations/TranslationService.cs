using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using Microsoft.Extensions.DependencyInjection;

namespace Uncreated.Warfare.Translations;
public class TranslationService : ITranslationService
{
    public const string TranslationsFolder = "Translations";

    private readonly ConcurrentDictionary<Type, TranslationCollection> _collections;
    private readonly IServiceProvider _serviceProvider;
    public IReadOnlyDictionary<Type, TranslationCollection> TranslationCollections { get; }
    public ITranslationValueFormatter ValueFormatter { get; private set; }
    public TranslationService(IServiceProvider serviceProvider)
    {
        _collections = new ConcurrentDictionary<Type, TranslationCollection>();
        _serviceProvider = serviceProvider;

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
    /// Get a translation collection from this provider.
    /// </summary>
    T Get<T>() where T : TranslationCollection, new();
}