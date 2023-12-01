using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Globalization;
using System.Text.Json.Serialization;

namespace Uncreated.Warfare.Models.Localization;

[Table("lang_info")]
public class LanguageInfo : ITranslationArgument
{
    private int _totalDefaultTranslations;
    private Dictionary<TranslationSection, int>? _totalSectionedDefaultTranslations;

    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    [Column("pk")]
    public uint Key { get; set; }

    [Required]
    [Column(TypeName = "char(5)")]
    public string Code { get; set; } = null!;

    [Required]
    [MaxLength(64)]
    public string DisplayName { get; set; } = null!;

    [MaxLength(64)]
    public string? NativeName { get; set; }

    [MaxLength(16)]
    public string? DefaultCultureCode { get; set; }

    public bool HasTranslationSupport { get; set; }

    public bool RequiresIMGUI { get; set; }

    [Column(TypeName = "char(5)")]
    public string? FallbackTranslationLanguageCode { get; set; }

    [MaxLength(32)]
    public string? SteamLanguageName { get; set; }

    public IList<LanguageAlias> Aliases { get; set; } = null!;
    public IList<LanguageContributor> Contributors { get; set; } = null!;
    public IList<LanguageCulture> SupportedCultures { get; set; } = null!;

    [JsonIgnore]
    public bool IsDefault => L.Default.Equals(Code, StringComparison.OrdinalIgnoreCase);

    [JsonIgnore]
    internal float Support => IsDefault ? 1f : ((float)TotalDefaultTranslations / Warfare.Localization.TotalDefaultTranslations);

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


    [FormatDisplay("Display Name")]
    public const string FormatDisplayName = "d";

    [FormatDisplay("Key Code")]
    public const string FormatKey = "k";
    public string Translate(LanguageInfo language, string? format, UCPlayer? target, CultureInfo? culture, ref TranslationFlags flags)
    {
        if (format is not null && format.Equals(FormatKey, StringComparison.Ordinal))
            return Code;
        return DisplayName;
    }

    public string? GetUnturnedLanguageName() => SteamLanguageName == null
        ? null
        : (SteamLanguageName.Length == 0
            ? SteamLanguageName
            : (char.ToUpperInvariant(SteamLanguageName[0]) + SteamLanguageName.Substring(1)));
    public override string ToString() => $"{DisplayName} {{{Code}}}";
    public bool Equals(LanguageInfo? other) => other is not null && (ReferenceEquals(this, other) || Code.Equals(other.Code, StringComparison.OrdinalIgnoreCase));
    public override bool Equals(object? obj) => obj is LanguageInfo info && Equals(info);
    // ReSharper disable once NonReadonlyMemberInGetHashCode
    public override int GetHashCode() => unchecked((int)Key);
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
        if (SupportedCultures == null) return false;
        for (int i = 0; i < SupportedCultures.Count; ++i)
            if (SupportedCultures[i].CultureCode.Equals(culture.Name, StringComparison.Ordinal))
                return true;

        return false;
    }
}