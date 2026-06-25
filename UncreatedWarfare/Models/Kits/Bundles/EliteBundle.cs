using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Uncreated.Warfare.Models.Factions;

namespace Uncreated.Warfare.Models.Kits.Bundles;

#nullable disable

[Table("kits_bundles")]
public class EliteBundle
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    [Column("pk")]
    public uint PrimaryKey { get; set; }

    [Required]
    [StringLength(25)]
    public string Id { get; set; }

    [Required]
    [StringLength(50)]
    public string DisplayName { get; set; }

    [Required]
    [StringLength(255)]
    public string Description { get; set; }

#nullable restore

    [ForeignKey(nameof(Faction))]
    [Column("Faction")]
    public uint? FactionId { get; set; }

    public Faction? Faction { get; set; }

#nullable disable
    /// <summary>
    /// In US dollars.
    /// </summary>
    [Required]
    public decimal Cost { get; set; }

    public IReadOnlyList<KitEliteBundle> Kits { get; set; }
}
