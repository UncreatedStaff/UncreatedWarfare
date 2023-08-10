using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text.Json.Serialization;
using Uncreated.SQL;

namespace Uncreated.Warfare;
public class LanguageInfo : ITranslationArgument, IEquatable<LanguageInfo>
{
    [JsonIgnore]
    private int _totalDefaultTranslations;
    [JsonIgnore]
    private Dictionary<TranslationSection, int>? _totalSectionedDefaultTranslations;


    [JsonPropertyName("code")]
    public string LanguageCode { get; }

    [JsonPropertyName("id")]
    public PrimaryKey PrimaryKey { get; internal set; }

    [JsonPropertyName("display_name")]
    public string DisplayName { get; set; }

    [JsonPropertyName("native_name")]
    public string NativeName { get; set; }

    [JsonPropertyName("has_translation_support")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public bool HasTranslationSupport { get; set; }

    [JsonPropertyName("default_culture_code")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? DefaultCultureCode { get; set; }

    [JsonPropertyName("fallback_translation_language_code")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? FallbackTranslationLanguageCode { get; set; }

    [JsonPropertyName("steam_language_name")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? SteamLanguageName { get; set; }

    [JsonPropertyName("requires_imgui")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public bool RequiresIMGUI { get; set; }

    [JsonPropertyName("aliases")]
    public string[] Aliases { get; set; } = Array.Empty<string>();

    [JsonPropertyName("available_culture_codes")]
    public string[] AvailableCultureCodes { get; set; } = Array.Empty<string>();

    [JsonPropertyName("credits")]
    public ulong[] Credits { get; set; } = Array.Empty<ulong>();

    [JsonIgnore]
    public bool IsDefault { get; }

    [JsonIgnore]
    internal float Support => IsDefault ? 1f : ((float)TotalDefaultTranslations / Localization.TotalDefaultTranslations);

    [JsonIgnore]
    internal int TotalDefaultTranslations
    {
        get
        {
            if (_totalDefaultTranslations == 0 && _totalSectionedDefaultTranslations != null)
            {
                foreach (int val in _totalSectionedDefaultTranslations.Values)
                    _totalDefaultTranslations += val;
            }
            return _totalDefaultTranslations;
        }
        set => _totalDefaultTranslations = value;
    }

    [JsonConstructor]
    public LanguageInfo(PrimaryKey primaryKey, string languageCode)
    {
        PrimaryKey = primaryKey;
        LanguageCode = languageCode;
        IsDefault = L.Default.Equals(languageCode, StringComparison.OrdinalIgnoreCase);
    }


    [JsonIgnore]
    [FormatDisplay("Display Name")]
    public const string FormatDisplayName = "d";

    [JsonIgnore]
    [FormatDisplay("Key Code")]
    public const string FormatKey = "k";
    public string Translate(LanguageInfo language, string? format, UCPlayer? target, CultureInfo? culture, ref TranslationFlags flags)
    {
        if (format is not null && format.Equals(FormatKey, StringComparison.Ordinal))
            return LanguageCode;
        return DisplayName;
    }

    public string? GetUnturnedLanguageName() => SteamLanguageName == null
        ? null
        : (SteamLanguageName.Length == 0
            ? SteamLanguageName
            : (char.ToUpperInvariant(SteamLanguageName[0]) + SteamLanguageName.Substring(1)));
    public override string ToString() => $"{DisplayName} {{{LanguageCode}}}";
    public bool Equals(LanguageInfo? other) => !ReferenceEquals(null, other) && (ReferenceEquals(this, other) || LanguageCode.Equals(other.LanguageCode, StringComparison.OrdinalIgnoreCase));
    public override bool Equals(object? obj) => obj is LanguageInfo info && Equals(info);
    // ReSharper disable once NonReadonlyMemberInGetHashCode
    public override int GetHashCode() => PrimaryKey.Key;
    internal void IncrementSection(TranslationSection section, int amt)
    {
        if (IsDefault || amt <= 0)
            return;
        _totalSectionedDefaultTranslations ??= new Dictionary<TranslationSection, int>(6);
        if (_totalSectionedDefaultTranslations.TryGetValue(section, out int value))
            _totalSectionedDefaultTranslations[section] = value + amt;
        else _totalSectionedDefaultTranslations.Add(section, amt);
        if (_totalDefaultTranslations != 0)
            _totalDefaultTranslations += amt;
    }
    internal void ClearSection(TranslationSection section)
    {
        if (IsDefault) return;
        _totalSectionedDefaultTranslations?.Remove(section);
        _totalDefaultTranslations = 0;
    }
    public static bool operator ==(LanguageInfo? left, LanguageInfo? right) => Equals(left, right);
    public static bool operator !=(LanguageInfo? left, LanguageInfo? right) => !Equals(left, right);

    public bool SupportsCulture(CultureInfo culture)
    {
        if (AvailableCultureCodes == null) return false;
        for (int i = 0; i < AvailableCultureCodes.Length; ++i)
            if (AvailableCultureCodes[i].Equals(culture.Name, StringComparison.Ordinal))
                return true;

        return false;
    }
}
