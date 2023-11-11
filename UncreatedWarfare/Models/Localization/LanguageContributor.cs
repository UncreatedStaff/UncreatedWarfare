using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Uncreated.Warfare.Models.Localization;

[Table("lang_credits")]
public class LanguageContributor
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    [Column("pk")]
    public uint Id { get; set; }

    [Column("Langauge")]
    public LanguageInfo Language { get; set; } = null!;
    public ulong Contributor { get; set; }
}