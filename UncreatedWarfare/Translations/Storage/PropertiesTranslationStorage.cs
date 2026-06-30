using DanielWillett.JavaPropertiesParser;
using DanielWillett.ReflectionTools;
using DanielWillett.ReflectionTools.Formatting;
using Microsoft.Extensions.FileProviders;
using System;
using System.Collections.Immutable;
using System.Globalization;
using System.IO;
using System.Reflection;
using System.Text;
using Uncreated.Warfare.Configuration;
using Uncreated.Warfare.Models.Localization;
using Uncreated.Warfare.Translations.Languages;
using Uncreated.Warfare.Translations.Util;
using Uncreated.Warfare.Util;
using Uncreated.Warfare.Util.List;

namespace Uncreated.Warfare.Translations.Storage;

/// <summary>
/// Implementation of <see cref="ITranslationStorageFactory"/> that uses the Java Properties format which works nicely for translations. 
/// </summary>
public sealed class PropertiesTranslationStorageFactory : ITranslationStorageFactory
{
    private readonly WarfareModule _module;
    private readonly LanguageService _languageService;
    private readonly string _rootFolder;
    private readonly ILogger _enumLogger;
    private readonly ILogger _collectionLogger;

    public PropertiesTranslationStorageFactory(WarfareModule module, LanguageService languageService, ILoggerFactory loggerFactory)
    {
        _module = module;
        _languageService = languageService;

        _rootFolder = Path.Combine(module.HomeDirectory, TranslationService.TranslationsFolder);
        
        // file scoped type names aren't preserved
        _collectionLogger = loggerFactory.CreateLogger("Uncreated.Warfare.Translations.Storage.PropertiesTranslationStorage");
        _enumLogger = loggerFactory.CreateLogger("Uncreated.Warfare.Translations.Storage.PropertiesEnumTranslationStorage");
    }

    public ITranslationStorage Create(TranslationCollection collection)
    {
        string fileName = collection.Name.Replace('\\', '/');

        if (!fileName.EndsWith(".properties", StringComparison.OrdinalIgnoreCase))
            fileName += ".properties";

        if (fileName.StartsWith('/'))
            fileName = fileName[1..];

        return new PropertiesTranslationStorage(
            _rootFolder,
            fileName,
            _module.FileProvider,
            _languageService,
            _collectionLogger
        );
    }

    public IEnumTranslationStorage<TEnum> CreateEnumStorage<TEnum>() where TEnum : unmanaged, Enum
    {
        TranslatableAttribute? info = ReflectionUtility.GetTypeDescriptorAttribute<TranslatableAttribute>(typeof(TEnum));

        string fileName;
        if (info?.FileName != null)
        {
            fileName = info.FileName;
            fileName = fileName.Replace('\\', '/');

            if (!fileName.EndsWith(".properties", StringComparison.OrdinalIgnoreCase))
                fileName += ".properties";

            if (fileName.StartsWith('/'))
                fileName = fileName[1..];

            fileName = "Enums/" + fileName;
        }
        else
        {
            fileName = "Enums/" + (typeof(TEnum).FullName ?? typeof(TEnum).Name) + ".properties";
        }

        return new PropertiesEnumTranslationStorage<TEnum>(
            _rootFolder,
            fileName,
            _module.FileProvider,
            _languageService,
            _enumLogger
        );
    }
}

file sealed class PropertiesTranslationStorage : ITranslationStorage, IDisposable
{
    private readonly LanguageService _languageService;
    private readonly ILogger _logger;

    private readonly string _fileName;
    private readonly string _basePath;
    private IDisposable? _changeToken;
    private DateTime _lastIntentionalWriteUtc;

    public event TranslationsUpdated? OnNeedsUpdating;

    public PropertiesTranslationStorage(
        string basePath,
        string fileName,
        IFileProvider fileProvider,
        LanguageService languageService,
        ILogger logger)
    {
        _basePath = basePath;
        _fileName = fileName;

        if (Path.DirectorySeparatorChar == '\\')
            _fileName = _fileName.Replace('/', '\\');

        _languageService = languageService;
        _logger = logger;

        //              | language
        // Translations/*/Name.properties

        string filter = basePath + "/*/" + fileName;

        _changeToken = ConfigurationHelper.ListenForFileUpdate(fileProvider, filter, HandleFileUpdated);
    }

    private void HandleFileUpdated()
    {
        // ignore changes made by Save()
        if ((DateTime.UtcNow - _lastIntentionalWriteUtc).TotalSeconds <= 5)
        {
            _logger.LogInformation($"Change made to translation {_fileName} ignored because it recently saved.");
            return;
        }

        _logger.LogInformation($"Change detected to translation {_fileName}, reloading.");
        IReadOnlyDictionary<TranslationLanguageKey, string> newData = Load();
        OnNeedsUpdating?.Invoke(newData);
    }

    private string GetFilePath(string langCode)
    {
        return Path.Combine(_basePath, langCode, _fileName);
    }

    public IReadOnlyDictionary<TranslationLanguageKey, string> Load()
    {
        // ensure default directory is there
        string defaultDir = Path.Combine(_basePath, _languageService.DefaultLanguageCode);
        Directory.CreateDirectory(defaultDir);

        // find and parse files
        string[] languageDirs = Directory.GetDirectories(_basePath, "*", SearchOption.TopDirectoryOnly);

        Dictionary<TranslationLanguageKey, string> translationDict = new Dictionary<TranslationLanguageKey, string>(32);

        foreach (string languageDirectory in languageDirs)
        {
            string languageId = Path.GetFileName(languageDirectory)!;
            if (languageId.Length != 5)
                continue;

            string languageFileName = GetFilePath(languageId);

            if (!File.Exists(languageFileName))
                continue;

            using PropertiesReader reader = new PropertiesReader(languageFileName);

            while (reader.TryReadPair(out string key, out string value))
            {
                TranslationLanguageKey dictKey = new TranslationLanguageKey(languageId, key);
                if (!translationDict.TryAdd(dictKey, value))
                {
                    _logger.LogWarning($"Duplicate key {key} in {_fileName} ({languageId}).");
                }
            }
        }

        return translationDict;
    }

    public void Save(
        IEnumerable<Translation> translations,
        LanguageInfo? language = null,
        string? baseFolder = null,
        WriteTranslationsOptions options = WriteTranslationsOptions.Default)
    {
        string langCode = language?.Code ?? _languageService.DefaultLanguageCode;

        string path = baseFolder == null ? GetFilePath(langCode) : Path.Combine(baseFolder, langCode, _fileName);

        using IEnumerator<Translation> enumerator = translations.GetEnumerator();
        if (!enumerator.MoveNext())
        {
            try
            {
                File.Delete(path);
            }
            catch (FileNotFoundException) { }
            catch (DirectoryNotFoundException) { }
            return;
        }

        _lastIntentionalWriteUtc = DateTime.UtcNow;
        
        bool wroteAny = false;

        using (PropertiesWriter writer = new PropertiesWriter(path))
        {
            bool skipNewLine = true;
            do
            {
                if (!skipNewLine && (options & WriteTranslationsOptions.Minimal) == 0)
                    writer.WriteLine();

                if (WriteTranslation(writer, enumerator.Current!, language, options))
                {
                    wroteAny = true;
                    skipNewLine = false;
                }
                else
                {
                    skipNewLine = true;
                }
            } while (enumerator.MoveNext());

            if (writer.Stream is FileStream fs)
            {
                fs.Flush(true);
                Thread.Sleep(50);
            }
        }

        if (!wroteAny)
        {
            try
            {
                File.Delete(path);
            }
            catch (FileNotFoundException) { }
            catch (DirectoryNotFoundException) { }
        }
    }

    private static bool WriteTranslation(PropertiesWriter writer, Translation translation, LanguageInfo? language, WriteTranslationsOptions options)
    {
        translation.AssertInitialized();

        bool minimal = (options & WriteTranslationsOptions.Minimal) != 0;

        Type type = translation.GetType();

        string value = translation.Original.RawValue;
        if (language != null && translation.Table.TryGetValue(language.Code, out TranslationValue valueEntry))
        {
            value = valueEntry.RawValue;
        }
        else if ((options & WriteTranslationsOptions.WriteMissingValues) == 0)
        {
            return false;
        }

        if ((options & WriteTranslationsOptions.PrioritizedOnly) != 0 && !translation.Data.IsPriorityTranslation)
        {
            return false;
        }

        if (!minimal)
        {
            if (!string.IsNullOrWhiteSpace(translation.Data.Description))
            {
                writer.WriteComment("Description: " + translation.Data.Description);
            }

            // write argument descriptions
            StringBuilder argBuilder = new StringBuilder();
            for (int i = 0; i < translation.ArgumentCount; ++i)
            {
                Type argumentType = type.GenericTypeArguments[i];
                argBuilder
                    .Append(" {")
                    .Append(i.ToString(CultureInfo.InvariantCulture))
                    .Append("} - ")
                    .Append(Accessor.Formatter.Format(argumentType, refMode: ByRefTypeMode.Ignore));

                ArgumentFormat fmt = translation.GetArgumentFormat(i);
                if (fmt.FormatDisplayName != null)
                {
                    argBuilder.Append(" (Format: ").Append(fmt.FormatDisplayName).Append(')');
                }
                else if (fmt.Format != null)
                {
                    argBuilder.Append(" (Format: \"").Append(fmt.Format).Append("\")");
                }

                if (translation.Data.ParameterDescriptions is { } descs && descs.Length > i && !string.IsNullOrWhiteSpace(descs[i]))
                {
                    argBuilder.Append(" | ").Append(descs[i]);
                }

                if (fmt.FormatAddons is { Length: > 0 })
                {
                    writer.WriteComment(argBuilder.ToString());
                    argBuilder.Clear();
                    if (fmt.FormatAddons.Length == 1)
                    {
                        argBuilder
                            .Append(" | Addon: ")
                            .Append(fmt.FormatAddons[0].DisplayName ?? Accessor.Formatter.Format(fmt.FormatAddons[0].GetType()));
                        writer.WriteComment(argBuilder.ToString());
                    }
                    else
                    {
                        argBuilder.Append(" | Addons:");
                        writer.WriteComment(argBuilder.ToString());
                        argBuilder.Clear();
                        for (int a = 0; a < fmt.FormatAddons.Length; ++a)
                        {
                            argBuilder
                                .Append(" |  - ")
                                .Append(fmt.FormatAddons[a].DisplayName ?? Accessor.Formatter.Format(fmt.FormatAddons[a].GetType()));
                            writer.WriteComment(argBuilder.ToString());
                            argBuilder.Clear();
                        }
                    }
                }
                else
                {
                    writer.WriteComment(argBuilder.ToString());
                }
                argBuilder.Clear();
            }

            // write default value if it doesn't match current value.
            if (!translation.Original.RawValue.Equals(value, StringComparison.Ordinal))
            {
                writer.WriteComment("Default: " + translation.Original.RawValue);
            }
        }

        writer.WriteKeyValue(translation.Key, value);
        return true;
    }

    public void Dispose()
    {
        Interlocked.Exchange(ref _changeToken, null)?.Dispose();
    }
}

file sealed class PropertiesEnumTranslationStorage<TEnum> : IEnumTranslationStorage<TEnum>, IDisposable where TEnum : unmanaged, Enum
{
    private readonly ILogger _logger;

    private readonly string _fileName;
    private readonly LanguageService _languageService;
    private readonly string _basePath;
    private IDisposable? _changeToken;
    private DateTime _lastIntentionalWriteUtc;

    public event EnumTranslationsUpdated<TEnum>? OnNeedsUpdating;

    public PropertiesEnumTranslationStorage(
        string basePath,
        string fileName,
        IFileProvider fileProvider,
        LanguageService languageService,
        ILogger logger)
    {
        _basePath = basePath;
        _fileName = fileName;
        _languageService = languageService;

        if (Path.DirectorySeparatorChar == '\\')
            _fileName = _fileName.Replace('/', '\\');

        _logger = logger;

        //              | language
        // Translations/*/Enums/FileName.properties

        string filter = basePath + "/*/" + fileName;

        _changeToken = ConfigurationHelper.ListenForFileUpdate(fileProvider, filter, HandleFileUpdated);
    }

    private void HandleFileUpdated()
    {
        // ignore changes made by Save()
        if ((DateTime.UtcNow - _lastIntentionalWriteUtc).TotalSeconds <= 5)
        {
            _logger.LogInformation($"Change made to enum translation {typeof(TEnum)} ignored because it recently saved.");
            return;
        }

        _logger.LogInformation($"Change detected to enum translation {typeof(TEnum)}, reloading.");
        IReadOnlyDictionary<string, IReadOnlyDictionary<TEnum, string>> newData = Load();
        OnNeedsUpdating?.Invoke(newData);
    }

    public IReadOnlyDictionary<string, IReadOnlyDictionary<TEnum, string>> Load()
    {
        LinearDictionary<string, IReadOnlyDictionary<TEnum, string>> tableTranslations
            = new LinearDictionary<string, IReadOnlyDictionary<TEnum, string>>(4);

        ImmutableDictionary<TEnum, string>.Builder bldr = ImmutableDictionary.CreateBuilder<TEnum, string>();

        foreach (string langFolder in Directory.EnumerateDirectories(_basePath, "*", SearchOption.TopDirectoryOnly))
        {
            string dirName = Path.GetFileName(langFolder);

            if (dirName.Length != 5)
                continue;

            if (!ReadTranslations(dirName, langFolder, bldr))
            {
                continue;
            }

            tableTranslations.Add(dirName, bldr.ToImmutable());
            
            bldr.Clear();
        }

        return tableTranslations;
    }

    public void Save(
        string languageCode,
        IReadOnlyDictionary<TEnum, string> translations,
        IReadOnlyDictionary<TEnum, string>? defaultTranslations,
        string? baseFolder = null,
        WriteTranslationsOptions options = WriteTranslationsOptions.Default)
    {
        _lastIntentionalWriteUtc = DateTime.UtcNow;

        string path = Path.Combine(baseFolder ?? _basePath, languageCode, _fileName);

        if (translations.Count == 0 && (options & WriteTranslationsOptions.WriteMissingValues) == 0)
        {
            try
            {
                File.Delete(path);
            }
            catch (FileNotFoundException) { }
            catch (DirectoryNotFoundException) { }
            return;
        }

        Type enumType = typeof(TEnum);

        TranslatableAttribute? info = ReflectionUtility.GetTypeDescriptorAttribute<TranslatableAttribute>(enumType);

        if (info != null
            && (options & WriteTranslationsOptions.PrioritizedOnly) != 0
            && !info.IsPrioritizedTranslation)
        {
            try
            {
                File.Delete(path);
            }
            catch (FileNotFoundException) { }
            catch (DirectoryNotFoundException) { }
            return;
        }

        bool minimal = (options & WriteTranslationsOptions.Minimal) != 0;

        using PropertiesWriter writer = new PropertiesWriter(path);

        if (!minimal)
        {
            writer.WriteComment(enumType.FullName!, PropertiesCommentType.Exclamation);
            writer.WriteComment($" in {enumType.Assembly.GetName().Name}.dll", PropertiesCommentType.Exclamation);
            writer.WriteLine();
            if (!string.IsNullOrEmpty(info?.Description))
            {
                writer.WriteComment(info.Description);
            }
            writer.WriteLine();
        }

        HashSet<TEnum> used = new HashSet<TEnum>();
        foreach (TEnum val in EnumUtility.GetEnumValuesArray<TEnum>())
        {
            if (!used.Add(val))
                continue;

            translations.TryGetValue(val, out string? value);
            if ((options & WriteTranslationsOptions.WriteMissingValues) == 0 && value == null)
                continue;

            FieldInfo? field = EnumUtility.GetField(val);
            TranslatableValueAttribute? attr = field?.GetAttributeSafe<TranslatableValueAttribute>();
            if ((options & WriteTranslationsOptions.PrioritizedOnly) != 0
                && attr is { IsPrioritizedTranslation: false })
            {
                continue;
            }

            if (!minimal && attr?.Description != null)
            {
                writer.WriteLine();
                writer.WriteComment(attr.Description);
            }

            string name = EnumUtility.GetName(val) ?? val.ToString();
            if (value == null)
            {
                defaultTranslations?.TryGetValue(val, out value);
                value ??= name;
            }
            
            writer.WriteKeyValue(name, value);
        }

        if (writer.Stream is FileStream fs)
        {
            fs.Flush(true);
        }
    }

    private bool ReadTranslations(string languageCode, string folder, IDictionary<TEnum, string> output)
    {
        bool isDefault = string.Equals(_languageService.DefaultLanguageCode, languageCode, StringComparison.OrdinalIgnoreCase);

        string path = Path.Combine(folder, _fileName);

        bool found = false;

        try
        {
            using PropertiesReader reader = new PropertiesReader(path);
            found = true;
            while (reader.TryReadPair(out string key, out string value))
            {
                // warn for legacy %NAME% entry
                if (key.Equals("%NAME%", StringComparison.OrdinalIgnoreCase))
                {
                    _logger.LogWarning($"Legacy %NAME% translation for enum type {typeof(TEnum)} ({languageCode}).");
                    continue;
                }

                if (Enum.TryParse(key, false, out TEnum enumKeyValue))
                {
                    if (!output.TryAdd(enumKeyValue, value))
                    {
                        _logger.LogWarning($"Duplicate enum key for type {typeof(TEnum)} ({languageCode}): \"{EnumUtility.GetName(enumKeyValue) ?? enumKeyValue.ToString()}\".");
                    }
                }
                else
                {
                    _logger.LogWarning($"Unknown enum key for type {typeof(TEnum)} ({languageCode}): \"{value}\".");
                }
            }
        }
        catch (FileNotFoundException) { }
        catch (DirectoryNotFoundException) { }

        if (isDefault)
        {
            FieldInfo[] fields = typeof(TEnum).GetFields();
            foreach (FieldInfo field in fields)
            {
                if (!field.IsLiteral
                    || field.GetAttributeSafe<TranslatableValueAttribute>() is not { } attr
                    || attr.Original == null)
                {
                    continue;
                }

                object val = field.GetValue(null);
                TEnum enumVal = (TEnum)val;
                output.TryAdd(enumVal, attr.Original);
            }
        }

        return found || isDefault;
    }

    public void Dispose()
    {
        Interlocked.Exchange(ref _changeToken, null)?.Dispose();
    }
}