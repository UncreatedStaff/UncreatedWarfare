using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace Uncreated.Warfare.Models.Kits.Bundles;

[Table("kits_bundle_items")]
public class KitEliteBundle
{
    [Required]
    [JsonIgnore]
    public EliteBundle Bundle { get; set; }

    [Required]
    [ForeignKey(nameof(Bundle))]
    [Column("Bundle")]
    public uint BundleId { get; set; }

    [Required]
    [JsonIgnore]
    public Kit Kit { get; set; }

    [Required]
    [ForeignKey(nameof(Kit))]
    [Column("Kit")]
    public uint KitId { get; set; }
}
