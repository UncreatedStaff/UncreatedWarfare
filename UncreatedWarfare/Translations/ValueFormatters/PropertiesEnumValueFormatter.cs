using DanielWillett.JavaPropertiesParser;
using DanielWillett.ReflectionTools;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Uncreated.Warfare.Models.Localization;
using Uncreated.Warfare.Translations.Languages;

namespace Uncreated.Warfare.Translations.ValueFormatters;

/// <summary>
/// Translates enums using their .properties files in "Translations/[LANG]/Enums/<typeparamref name="TEnum"/>".
/// </summary>
/// <typeparam name="TEnum">The enum type to translate.</typeparam>
public class PropertiesEnumValueFormatter<TEnum> : IEnumFormatter<TEnum> where TEnum : unmanaged, Enum
{
    private readonly LanguageService _languageService;
    private readonly ILogger _logger;
    private readonly string _filePathBegin;
    private readonly string _filePathEnd;

    private Dictionary<string, Dictionary<TEnum, string>>? _translations;
    private Dictionary<string, string>? _nameTranslations;

    public PropertiesEnumValueFormatter(WarfareModule module, LanguageService languageService, ILoggerFactory logger)
    {
        _languageService = languageService;
        _logger = logger.CreateLogger("Uncreated.Warfare.Translations.ValueFormatters.PropertiesEnumValueFormatter");

        _filePathBegin = Path.Combine(module.HomeDirectory, TranslationService.TranslationsFolder);
        _filePathEnd = Path.Combine("Enums", typeof(TEnum).FullName + ".properties");
    }

    public string GetName(LanguageInfo language)
    {
        CheckTranslations();
        if (_nameTranslations!.TryGetValue(language.Code, out string name))
        {
            return name;
        }

        if (!language.IsDefault && _nameTranslations.TryGetValue(_languageService.DefaultLanguageCode, out name))
        {
            return name;
        }

        return _nameTranslations.Values.FirstOrDefault() ?? typeof(TEnum).Name;
    }
    
    public string GetValue(object value, LanguageInfo language)
    {
        return GetValue((TEnum)value, language);
    }
    public string GetValue(TEnum value, LanguageInfo language)
    {
        CheckTranslations();
        if (_translations!.TryGetValue(language.Code, out Dictionary<TEnum, string> table))
        {
            if (table.TryGetValue(value, out string translation))
            {
                return translation;
            }
        }

        if (!language.IsDefault && _translations.TryGetValue(_languageService.DefaultLanguageCode, out table))
        {
            return table.TryGetValue(value, out string translation) ? translation : value.ToString();
        }

        Dictionary<TEnum, string>? dict = _translations.Values.FirstOrDefault();
        return dict != null && dict.TryGetValue(value, out string t) ? t : value.ToString();
    }

    public string Format(object value, in ValueFormatParameters parameters)
    {
        return GetValue((TEnum)value, parameters.Language);
    }

    public string Format(TEnum value, in ValueFormatParameters parameters)
    {
        return GetValue(value, parameters.Language);
    }

    private void CheckTranslations()
    {
        if (_translations != null)
        {
            return;
        }

        lock (this)
        {
            if (_translations == null)
                ReadTranslations();
        }
    }

    private void ReadTranslations()
    {
        string[] folders = Directory.GetDirectories(_filePathBegin, "*", SearchOption.TopDirectoryOnly);

        Dictionary<string, string> nameTranslations = new Dictionary<string, string>(4);
        Dictionary<string, Dictionary<TEnum, string>> tableTranslations = new Dictionary<string, Dictionary<TEnum, string>>(4);

        for (int i = 0; i < folders.Length; ++i)
        {
            string dir = folders[i];
            string dirName = Path.GetFileName(dir);

            if (dirName.Length != 5)
                continue;

            ReadTranslations(dirName, out Dictionary<TEnum, string>? table, out string? name);
            if (table == null && name == null)
                continue;

            if (table != null)
                tableTranslations.Add(dirName, table);
            if (name != null)
                nameTranslations.Add(dirName, name);
        }

        _nameTranslations = nameTranslations;
        _translations = tableTranslations;
    }

    private void ReadTranslations(string languageCode, out Dictionary<TEnum, string>? table, out string? name)
    {
        bool isDefault = languageCode.Equals(_languageService.DefaultLanguageCode, StringComparison.OrdinalIgnoreCase);
        string path = Path.Combine(_filePathBegin, languageCode, _filePathEnd);

        Dictionary<TEnum, string>? translationTable = null;
        string? nameTranslation = null;
        if (File.Exists(path))
        {
            using PropertiesReader reader = new PropertiesReader(path);
            while (reader.TryReadPair(out string key, out string value))
            {
                if (key.Equals("%NAME%", StringComparison.OrdinalIgnoreCase))
                {
                    nameTranslation = value;
                }
                else if (Enum.TryParse(key, false, out TEnum enumKeyValue))
                {
                    if (!(translationTable ??= new Dictionary<TEnum, string>(16)).TryAdd(enumKeyValue, value))
                    {
                        _logger.LogWarning("Duplicate enum key for type {0}: \"{1}\".", Accessor.Formatter.Format<TEnum>(), enumKeyValue.ToString());
                    }
                }
                else
                {
                    _logger.LogWarning("Unknown enum key for type {0}: \"{1}\".", Accessor.Formatter.Format<TEnum>(), value);
                }
            }

            if (nameTranslation == null)
            {
                _logger.LogWarning("Missing %NAME% translation for enum type {0}.", Accessor.Formatter.Format<TEnum>());
            }
        }

        if (isDefault)
        {
            WriteTranslations(path, nameTranslation, translationTable, true);
        }

        table = translationTable;
        name = nameTranslation;
    }

    private static void WriteTranslations(string path, string? nameTranslation, IReadOnlyDictionary<TEnum, string>? translationTable, bool writeAll)
    {
        using PropertiesWriter writer = new PropertiesWriter(path);

        Type enumType = typeof(TEnum);

        writer.WriteComment(enumType.FullName!, PropertiesCommentType.Exclamation);
        writer.WriteComment($" in {enumType.Assembly.GetName().Name}.dll", PropertiesCommentType.Exclamation);
        writer.WriteLine();

        if (writeAll || nameTranslation != null)
        {
            writer.WriteComment("This represents a generic name of the set of values.", PropertiesCommentType.Hashtag);
            writer.WriteKeyValue("%NAME%", nameTranslation ?? enumType.Name);
            writer.WriteLine();
        }

        if (!writeAll && translationTable == null)
            return;
        
        TEnum[] values = (TEnum[])Enum.GetValues(enumType);
        foreach (TEnum val in values)
        {
            string? value = null;
            if (!writeAll && (translationTable == null || !translationTable.TryGetValue(val, out value)))
                continue;
            
            writer.WriteKey(val.ToString());
            writer.WriteValue(value ?? val.ToString());
        }
    }
}
