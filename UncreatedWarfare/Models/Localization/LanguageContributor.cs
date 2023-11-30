using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Uncreated.Warfare.Models.Users;

namespace Uncreated.Warfare.Models.Localization;

[Table("lang_credits")]
public class LanguageContributor
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    [Column("pk")]
    public uint Id { get; set; }

    [Required]
    public LanguageInfo Language { get; set; } = null!;

    [Required]
    [Column("Language")]
    [ForeignKey(nameof(Language))]
    public uint LanguageId { get; set; }

    [Required]
    [ForeignKey(nameof(ContributorData))]
    [Column("Contributor")]
    public ulong Contributor { get; set; }

    [Required]
    public WarfareUserData ContributorData { get; set; }
}