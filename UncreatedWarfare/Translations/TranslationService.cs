using DanielWillett.ReflectionTools;
using Humanizer;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using StackCleaner;
using System;
using System.Collections.Concurrent;
using System.Collections.Immutable;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.IO.Compression;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;
using Uncreated.Warfare.Models.Localization;
using Uncreated.Warfare.Players.Management;
using Uncreated.Warfare.Plugins;
using Uncreated.Warfare.Services;
using Uncreated.Warfare.Translations.Languages;
using Uncreated.Warfare.Translations.Storage;
using YamlDotNet.Core.Tokens;
using CompressionLevel = System.IO.Compression.CompressionLevel;

namespace Uncreated.Warfare.Translations;

public class TranslationService : ITranslationService, IDisposable, IHostedService
{
    public const string TranslationsFolder = "Translations";

    private readonly ConcurrentDictionary<Type, TranslationCollection> _collections;
    private readonly IServiceProvider _serviceProvider;
    private ImmutableArray<Type> _enumTypes;

    public IReadOnlyDictionary<Type, TranslationCollection> TranslationCollections { get; }
    public ITranslationValueFormatter ValueFormatter { get; }
    public ITranslationStorageFactory Storage { get; }
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

        Storage = serviceProvider.GetRequiredService<ITranslationStorageFactory>();

        TranslationCollections = new ReadOnlyDictionary<Type, TranslationCollection>(_collections);

        SetOf = new LanguageSets(playerService);

        TerminalColoring = systemConfig.GetValue("logging:terminal_coloring", StackColorFormatType.ExtendedANSIColor);

        // hard-coded translatable enum types from the game

        ImmutableArray<Type>.Builder extBldr = ImmutableArray.CreateBuilder<Type>();

        extBldr.Add(typeof(EAssetType));
        TypeDescriptor.AddAttributes(typeof(EAssetType), new TranslatableAttribute("Unturned Asset Category")
        {
            IsPrioritizedTranslation = false,
            Description = "Category for assets to sort their IDs."
        });

        extBldr.Add(typeof(EDamageOrigin));
        TypeDescriptor.AddAttributes(typeof(EDamageOrigin), new TranslatableAttribute("Unturned Damage Origin")
        {
            IsPrioritizedTranslation = false,
            Description = "Hint for how damage was done."
        });

        extBldr.Add(typeof(EFiremode));
        TypeDescriptor.AddAttributes(typeof(EFiremode), new TranslatableAttribute("Unturned Fire Mode")
        {
            IsPrioritizedTranslation = false,
            Description = "A gun's fire mode."
        });

        extBldr.Add(typeof(ELimb));
        TypeDescriptor.AddAttributes(typeof(ELimb), new TranslatableAttribute("Unturned Limb")
        {
            Description = "A player's limb (arm, leg, etc) where they can be shot."
        });

        extBldr.Add(typeof(EPlayerDefense));
        TypeDescriptor.AddAttributes(typeof(EPlayerDefense), new TranslatableAttribute("Unturned Defense Skill")
        {
            IsPrioritizedTranslation = false,
            Description = "Defense skill names."
        });

        extBldr.Add(typeof(EPlayerOffense));
        TypeDescriptor.AddAttributes(typeof(EPlayerOffense), new TranslatableAttribute("Unturned Offense Skill")
        {
            IsPrioritizedTranslation = false,
            Description = "Offense skill names."
        });

        extBldr.Add(typeof(EPlayerSupport));
        TypeDescriptor.AddAttributes(typeof(EPlayerSupport), new TranslatableAttribute("Unturned Support Skill")
        {
            IsPrioritizedTranslation = false,
            Description = "Support skill names."
        });

        _enumTypes = extBldr.DrainToImmutable();

        if (systemConfig.GetValue<bool>("translations:auto_reset_translations"))
        {
            ResetToDefaults();
        }
    }

    UniTask IHostedService.StartAsync(CancellationToken token)
    {
        WarmupTranslations();
        return UniTask.CompletedTask;
    }

    UniTask IHostedService.StopAsync(CancellationToken token)
    {
        return UniTask.CompletedTask;
    }

    private void ResetToDefaults()
    {
        WarfareModule module = _serviceProvider.GetRequiredService<WarfareModule>();
        string translationsFolder = Path.Combine(module.HomeDirectory, TranslationsFolder, LanguageService.GetDefaultLanguage().Code);

        foreach (string info in Directory.EnumerateFileSystemEntries(translationsFolder, "*", SearchOption.TopDirectoryOnly))
        {
            if (Path.GetFileName(info) == "Enums")
            {
                foreach (string file in Directory.EnumerateFileSystemEntries(info, "*", SearchOption.TopDirectoryOnly))
                {
                    // don't delete "Unturned ..." enums
                    if (Path.GetFileName(file.AsSpan()).StartsWith("Unturned "))
                        continue;

                    if (Directory.Exists(file))
                        Directory.Delete(file, true);
                    else if (File.Exists(file) && string.Equals(Path.GetExtension(file), ".properties", StringComparison.OrdinalIgnoreCase))
                        File.Delete(file);
                }

                continue;
            }

            if (Directory.Exists(info))
                Directory.Delete(info, true);
            else if (File.Exists(info) && string.Equals(Path.GetExtension(info), ".properties", StringComparison.OrdinalIgnoreCase))
                File.Delete(info);
        }
    }

    /// <summary>
    /// Makes sure files exist for all collections and enums on startup instead of as they're used.
    /// </summary>
    [MethodImpl(MethodImplOptions.NoInlining)]
    private void WarmupTranslations()
    {
        WarfarePluginLoader? pluginLoader = _serviceProvider.GetService<WarfarePluginLoader>();

        ImmutableArray<Assembly> assemblies = pluginLoader?.AllAssemblies ?? [ Assembly.GetExecutingAssembly() ];

        Type baseType = typeof(TranslationCollection);

        // placeholder values
        LanguageInfo language = LanguageService.GetDefaultLanguage();

        ImmutableArray<Type>.Builder enumBuilder = ImmutableArray.CreateBuilder<Type>(64);

        enumBuilder.AddRange(_enumTypes);

        foreach (Assembly asm in assemblies)
        {
            Type?[] types;

            try
            {
                types = asm.GetTypes();
            }
            catch (ReflectionTypeLoadException ex)
            {
                types = ex.Types;
            }

            foreach (Type? type in types)
            {
                if (type == null)
                    continue;

                if (type.IsEnum && type.IsDefinedSafe<TranslatableAttribute>())
                {
                    IEnumFormatter? fmt = ValueFormatter.GetValueFormatter(type) as IEnumFormatter;
                    fmt?.GetValue(Activator.CreateInstance(type), language);
                    enumBuilder.Add(type);
                    continue;
                }

                if (!type.IsSubclassOf(baseType) || type.IsIgnored() || type == typeof(TranslationCollection) || type.IsAbstract)
                {
                    continue;
                }

                _ = GetCollectionFromType(type);
            }
        }

        foreach (Type type in _enumTypes)
        {
            IEnumFormatter? fmt = ValueFormatter.GetValueFormatter(type) as IEnumFormatter;
            fmt?.GetValue(Activator.CreateInstance(type), language);
        }

        _enumTypes = enumBuilder.DrainToImmutable();
    }

    public T Get<T>() where T : TranslationCollection, new()
    {
        TranslationCollection c = _collections.GetOrAdd(typeof(T), _ => new T());

        c.TryInitialize(this, _serviceProvider);
        return (T)c;
    }

    private TranslationCollection GetCollectionFromType(Type type)
    {
        TranslationCollection c = _collections.GetOrAdd(type, _ => (TranslationCollection)Activator.CreateInstance(type));

        c.TryInitialize(this, _serviceProvider);
        return c;
    }

    public void ReloadAll()
    {
        foreach (TranslationCollection collection in _collections.Values)
        {
            collection.Reload();
        }
    }
    
    public Task ExportAsync(LanguageInfo language, string location, CancellationToken token = default)
    {
        string homeDir = _serviceProvider.GetRequiredService<WarfareModule>().HomeDirectory;

        return Task.Run(() =>
        {
            string baseFolder = Path.Combine(homeDir, "Cache", "LangPack");
            try
            {
                Directory.Delete(baseFolder, true);
            }
            catch (DirectoryNotFoundException) { }

            Directory.CreateDirectory(baseFolder);

            foreach (TranslationCollection collection in _collections.Values)
            {
                collection.Storage.Save(
                    collection.Translations.Values,
                    language,
                    baseFolder,
                    WriteTranslationsOptions.PrioritizedOnly | WriteTranslationsOptions.WriteMissingValues
                );
            }

            foreach (Type enumType in _enumTypes)
            {
                if (ValueFormatter.GetValueFormatter(enumType) is not IEnumFormatter enumFormatter)
                    continue;

                EnumSaveVisitor v;
                v.BaseFolder = baseFolder;
                v.LanguageCode = language.Code;
                v.DefaultLanguageCode = LanguageService.DefaultLanguageCode;
                enumFormatter.Visit(ref v);
            }

            try
            {
                using (ZipArchive archive = new ZipArchive(new FileStream(
                           location,
                           FileMode.Create,
                           FileAccess.Write,
                           FileShare.Read,
                           16384
                       ), ZipArchiveMode.Create, leaveOpen: false, Encoding.UTF8))
                {
                    ZipFolder(archive, Path.Combine(baseFolder, language.Code), language.DisplayName, token);
                }
            }
            catch (OperationCanceledException) when (token.IsCancellationRequested)
            {
                Directory.Delete(baseFolder, true);
                throw;
            }

            Directory.Delete(baseFolder, true);

        }, token);
    }

    private struct EnumSaveVisitor : IEnumFormatterVisitor
    {
        public string BaseFolder;
        public string LanguageCode;
        public string DefaultLanguageCode;

        public void Accept<TEnum>(IEnumFormatter<TEnum> formatter) where TEnum : unmanaged, Enum
        {
            IEnumTranslationStorage<TEnum>? storage = formatter.Storage;
            if (storage == null)
                return;

            IReadOnlyDictionary<string, IReadOnlyDictionary<TEnum, string>> loadStorage = storage.Load();
            if (!loadStorage.TryGetValue(LanguageCode, out IReadOnlyDictionary<TEnum, string> values))
                values = ImmutableDictionary<TEnum, string>.Empty;

            loadStorage.TryGetValue(DefaultLanguageCode, out IReadOnlyDictionary<TEnum, string> defaultValues);

            storage.Save(LanguageCode, values, defaultValues, BaseFolder, WriteTranslationsOptions.PrioritizedOnly | WriteTranslationsOptions.WriteMissingValues);
        }
    }

    private static void ZipFolder(ZipArchive archive, string folder, string entryName, CancellationToken token)
    {
        token.ThrowIfCancellationRequested();
        foreach (string dirOrFile in Directory.EnumerateFileSystemEntries(folder, "*", SearchOption.TopDirectoryOnly))
        {
            string fileName = entryName + "/" + Path.GetFileName(dirOrFile);
            if (Directory.Exists(dirOrFile))
            {
                ZipFolder(archive, dirOrFile, fileName, token);
                continue;
            }

            token.ThrowIfCancellationRequested();
            archive.CreateEntryFromFile(dirOrFile, fileName, CompressionLevel.Optimal);
        }
    }

    public void Dispose()
    {
        do
        {
            foreach (Type type in _collections.Keys)
            {
                if (_collections.TryRemove(type, out TranslationCollection collection))
                {
                    collection.Dispose();
                }
            }
        } while (_collections.Count > 0);
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
    /// The storage method used for storing translations and enums.
    /// </summary>
    ITranslationStorageFactory Storage { get; }

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

    /// <summary>
    /// Exports all translations as a zip file, overwriting the file currently at <paramref name="location"/> if it exists.
    /// </summary>
    Task ExportAsync(LanguageInfo language, string location, CancellationToken token = default);
}