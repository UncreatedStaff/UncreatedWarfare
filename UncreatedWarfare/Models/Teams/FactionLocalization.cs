using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Uncreated.Warfare.Models.Teams;

[Table("faction_translations")]
public class FactionLocalization
{
    public int Language { get; set; }

    [Required]
    [MaxLength(16)]
    public string CultureCode { get; set; }
}
