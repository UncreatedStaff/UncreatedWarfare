using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;
using Uncreated.Warfare.Models.Users;

namespace Uncreated.Warfare.Models.Localization;

#nullable disable

[Table("lang_credits")]
public class LanguageContributor
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    [Column("pk")]
    public uint Id { get; set; }

    [Required, JsonIgnore]
    public LanguageInfo Language { get; set; }

    [Required]
    [Column("Language")]
    [ForeignKey(nameof(Language))]
    public uint LanguageId { get; set; }

    [Required]
    [ForeignKey(nameof(ContributorData))]
    [Column("Contributor")]
    public ulong Contributor { get; set; }

    [Required, JsonIgnore]
    public WarfareUserData ContributorData { get; set; }
}