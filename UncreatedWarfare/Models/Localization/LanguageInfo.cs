using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Uncreated.Warfare.Models.Localization;

[Table("lang_info")]
public class LanguageInfo
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    [Column("pk")]
    public int Key { get; set; }

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
}
