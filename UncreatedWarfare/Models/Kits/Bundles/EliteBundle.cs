using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Stripe;
using Uncreated.Warfare.Models.Factions;

namespace Uncreated.Warfare.Models.Kits.Bundles;

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

    [ForeignKey(nameof(Faction))]
    [Column("Faction")]
    public uint? FactionId { get; set; }
    
    public Faction? Faction { get; set; }

    /// <summary>
    /// In US dollars.
    /// </summary>
    [Required]
    public decimal Cost { get; set; }

    public IReadOnlyList<KitEliteBundle> Kits { get; set; }

    [NotMapped]
    public Product? Product { get; set; }
}
