using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Globalization;
using System.Text.Json.Serialization;
using Uncreated.Warfare.Translations;
using Uncreated.Warfare.Translations.Languages;
using Uncreated.Warfare.Translations.Util;
using Uncreated.Warfare.Translations.ValueFormatters;

namespace Uncreated.Warfare.Models.Localization;

[Table("lang_info")]
public class LanguageInfo : ITranslationArgument, IEquatable<LanguageInfo>
{
    public static readonly SpecialFormat FormatDisplayName = new SpecialFormat("Display Name", "d");
    public static readonly SpecialFormat FormatCode = new SpecialFormat("Code", "c");

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

    public bool SupportsPluralization { get; set; }

    public bool RequiresIMGUI { get; set; }

    [Column(TypeName = "char(5)")]
    public string? FallbackTranslationLanguageCode { get; set; }

    [MaxLength(32)]
    public string? SteamLanguageName { get; set; }

    public IList<LanguageAlias> Aliases { get; set; } = null!;
    public IList<LanguageContributor> Contributors { get; set; } = null!;
    public IList<LanguageCulture> SupportedCultures { get; set; } = null!;

    [JsonIgnore, NotMapped]
    public bool IsDefault { get; internal set; }

    [NotMapped]
    // todo: better support calculation
    public float Support { get => IsDefault ? 1 : 0; }

    public LanguageInfo() { }

    public LanguageInfo(string tempCode, LanguageService langService)
    {
        Code = tempCode;
        DisplayName = tempCode;
        HasTranslationSupport = false;
        Aliases = new List<LanguageAlias>(0);
        Contributors = new List<LanguageContributor>(0);
        SupportedCultures = new List<LanguageCulture>(0);
        UpdateIsDefault(langService);
    }

    internal void UpdateIsDefault(LanguageService langService)
    {
        IsDefault = langService.DefaultLanguageCode.Equals(Code, StringComparison.OrdinalIgnoreCase);
    }

    /// <inheritdoc />
    public string Translate(ITranslationValueFormatter formatter, in ValueFormatParameters parameters)
    {
        return FormatCode.Match(in parameters) ? Code : DisplayName;
    }

    /// <summary>
    /// If this language goes with <paramref name="culture"/>.
    /// </summary>
    public bool SupportsCulture(CultureInfo culture)
    {
        if (SupportedCultures == null) return false;
        for (int i = 0; i < SupportedCultures.Count; ++i)
            if (SupportedCultures[i].CultureCode.Equals(culture.Name, StringComparison.Ordinal))
                return true;

        return false;
    }

    /// <summary>
    /// Gets the language in Unturned's naming scheme, or <see langword="null"/> if Unturned (Steam) doesn't support this language.
    /// </summary>
    public string? GetUnturnedLanguageName() => SteamLanguageName == null
        ? null
        : (SteamLanguageName.Length == 0
            ? SteamLanguageName
            : (char.ToUpperInvariant(SteamLanguageName[0]) + SteamLanguageName.Substring(1)));

    /// <inheritdoc />
    public override string ToString() => $"{DisplayName} {{{Code}}}";

    /// <inheritdoc />
    public bool Equals(LanguageInfo? other)
    {
        return other is not null && (ReferenceEquals(this, other) || Code.Equals(other.Code, StringComparison.OrdinalIgnoreCase));
    }

    /// <inheritdoc />
    public override bool Equals(object? obj)
    {
        return obj is LanguageInfo info && Equals(info);
    }

    // ReSharper disable once NonReadonlyMemberInGetHashCode
    /// <inheritdoc />
    public override int GetHashCode() => unchecked( (int)Key );

    /// <summary>
    /// Compare two <see cref="LanguageInfo"/> objects.
    /// </summary>
    public static bool operator ==(LanguageInfo? left, LanguageInfo? right) => Equals(left, right);
    
    /// <summary>
    /// Compare two <see cref="LanguageInfo"/> objects.
    /// </summary>
    public static bool operator !=(LanguageInfo? left, LanguageInfo? right) => !Equals(left, right);
}