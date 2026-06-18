using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;
using Uncreated.Warfare.Models.Assets;
using Uncreated.Warfare.Teams;

namespace Uncreated.Warfare.Models.Factions;

[Table("faction_assets")]
public class FactionAsset
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    [Column("pk")]
    public uint Id { get; set; }

    [Required, JsonIgnore]
    public Faction Faction { get; set; } = null!;

    [Required]
    [Column("Faction")]
    [ForeignKey(nameof(Faction))]
    public uint FactionId { get; set; }

    [JsonConverter(typeof(JsonStringEnumConverter<RedirectType>))]
    public RedirectType Redirect { get; set; }

    public UnturnedAssetReference Asset { get; set; }

    [MaxLength(32), JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? VariantKey { get; set; }
}