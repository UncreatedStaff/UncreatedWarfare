using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Uncreated.Warfare.Models.Kits;

[Table("kits_map_filters")]
public class KitFilteredMap : ICloneable
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    [Column("pk")]
    public uint Id { get; set; }

    [Required]
    public Kit Kit { get; set; }

    [Required]
    public uint Map { get; set; }

    public object Clone()
    {
        return new KitFilteredMap
        {
            Map = Map,
            Kit = Kit
        };
    }
}
