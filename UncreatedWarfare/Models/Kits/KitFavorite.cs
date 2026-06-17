using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;
using Uncreated.Warfare.Models.Users;

namespace Uncreated.Warfare.Models.Kits;

[Table("kits_favorites")]
public class KitFavorite
{

    [Required, JsonIgnore]
    public KitModel Kit { get; set; }

    [Required]
    [ForeignKey(nameof(Kit))]
    [Column("Kit")]
    public uint KitId { get; set; }

    [Required]
    [ForeignKey(nameof(PlayerData))]
    [Column("Steam64")]
    public ulong Steam64 { get; set; }

    [Required, JsonIgnore]
    public WarfareUserData PlayerData { get; set; }
}
