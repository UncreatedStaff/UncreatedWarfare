using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Uncreated.Warfare.Models.Teams;

[Table("factions")]
public class Faction
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    [Column("pk")]
    public int Key { get; set; }

    [Required]
    [MaxLength(16)]
    public string Id { get; set; }
    
    [MaxLength(32)]
    public string? Name { get; set; }

    [MaxLength(24)]
    public string? ShortName { get; set; }

    [MaxLength(8)]
    public string? Abbreviation { get; set; }

    [Column(TypeName = "char(6)")]
    public string? HexColor { get; set; }

    [MaxLength(25)]
    public string? UnarmedKit { get; set; }

    [MaxLength(128)]
    public string? FlagImageUrl { get; set; }
    
    public int? SpriteIndex { get; set; }

    [MaxLength(64)]
    public string? Emoji { get; set; }

    public IList<FactionAsset>? Assets { get; set; }
}