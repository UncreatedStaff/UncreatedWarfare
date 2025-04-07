using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using StackCleaner;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using Uncreated.Warfare.Players.Management;
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
    public StackColorFormatType TerminalColoring { get; }
    public LanguageSets SetOf { get; }
    public TranslationService(IServiceProvider serviceProvider, IConfiguration systemConfig)
    {
        IPlayerService playerService = serviceProvider.GetRequiredService<IPlayerService>();

        _collections = new ConcurrentDictionary<Type, TranslationCollection>();
        _serviceProvider = serviceProvider;

        LanguageService = serviceProvider.GetRequiredService<LanguageService>();
        ValueFormatter = serviceProvider.GetRequiredService<ITranslationValueFormatter>();

        if (systemConfig.GetValue<bool>("translations:auto_reset_translations"))
        {
            WarfareModule module = serviceProvider.GetRequiredService<WarfareModule>();
            string translationsFolder = Path.Combine(module.HomeDirectory, TranslationsFolder, LanguageService.GetDefaultLanguage().Code);

            if (Directory.Exists(translationsFolder))
                Directory.Delete(translationsFolder, true);
        }

        TranslationCollections = new ReadOnlyDictionary<Type, TranslationCollection>(_collections);

        SetOf = new LanguageSets(playerService);

        TerminalColoring = systemConfig.GetValue("logging:terminal_coloring", StackColorFormatType.ExtendedANSIColor);
    }

    public T Get<T>() where T : TranslationCollection, new()
    {
        TranslationCollection c = _collections.GetOrAdd(typeof(T), _ => new T());

        c.TryInitialize(this, _serviceProvider);
        return (T)c;
    }

    public void ReloadAll()
    {
        foreach (TranslationCollection collection in _collections.Values)
        {
            collection.Reload();
        }
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
    /// Accessor for enumerating certain player groups based on their language settings.
    /// </summary>
    LanguageSets SetOf { get; }

    /// <summary>
    /// The coloring style used for terminals.
    /// </summary>
    /// <remarks>Only <see cref="StackColorFormatType.ANSIColor"/> and <see cref="StackColorFormatType.ExtendedANSIColor"/> is supported.</remarks>
    StackColorFormatType TerminalColoring { get; }

    /// <summary>
    /// Get a translation collection from this provider.
    /// </summary>
    T Get<T>() where T : TranslationCollection, new();

    /// <summary>
    /// Reload all translation collections.
    /// </summary>
    void ReloadAll();
}