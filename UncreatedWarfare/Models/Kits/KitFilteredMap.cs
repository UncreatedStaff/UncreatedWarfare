﻿using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Uncreated.Warfare.Models.Kits;

[Table("kits_map_filters")]
public class KitFilteredMap : ICloneable
{
    [Required]
    public Kit Kit { get; set; }

    [Required]
    [ForeignKey(nameof(Kit))]
    [Column("Kit")]
    public uint KitId { get; set; }

    [Required]
    [Column("Map")]
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
