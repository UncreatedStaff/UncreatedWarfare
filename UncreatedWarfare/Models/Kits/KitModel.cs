using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;
using Uncreated.Warfare.Configuration;
using Uncreated.Warfare.Kits;
using Uncreated.Warfare.Models.Factions;
using Uncreated.Warfare.Models.Kits.Bundles;

namespace Uncreated.Warfare.Models.Kits;

[Table("kits")]
public class KitModel
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    [Column("pk")]
    public uint PrimaryKey { get; set; }

    [JsonIgnore]
    public Faction? Faction { get; set; }

    [ForeignKey(nameof(Faction))]
    [Column("Faction")]
    public uint? FactionId { get; set; }

    [Required]
    [StringLength(25)]
    public required string Id { get; set; }

    [Required]
    [JsonConverter(typeof(JsonStringEnumConverter))]
    [CommandSettable]
    public Class Class { get; set; }

    [Required]
    [JsonConverter(typeof(JsonStringEnumConverter))]
    [CommandSettable]
    public Branch Branch { get; set; }

    [Required]
    [JsonConverter(typeof(JsonStringEnumConverter))]
    [CommandSettable]
    public KitType Type { get; set; }

    public bool Disabled { get; set; }

    [CommandSettable("NitroBooster")]
    public bool RequiresNitro { get; set; }

    public bool MapFilterIsWhitelist { get; set; }

    public bool FactionFilterIsWhitelist { get; set; }

    [CommandSettable]
    public int Season { get; set; }

    [DefaultValue(0f)]
    [CommandSettable]
    public float RequestCooldown { get; set; }
    
    [CommandSettable]
    public int? MinRequiredSquadMembers { get; set; }
    
    [DefaultValue(true)]
    [CommandSettable]
    public bool RequiresSquad { get; set; }

    [DefaultValue(0)]
    [CommandSettable]
    public int CreditCost { get; set; }

    [DefaultValue(0)]
    [CommandSettable]
    public decimal PremiumCost { get; set; }

    [DefaultValue(SquadLevel.Member)]
    [CommandSettable]
    public SquadLevel SquadLevel { get; set; }

    [StringLength(128)]
    [CommandSettable]
    public string? Weapons { get; set; }

    public DateTimeOffset CreatedAt { get; set; }

    public ulong Creator { get; set; }

    public DateTimeOffset LastEditedAt { get; set; }

    public ulong LastEditor { get; set; }

#nullable disable

    public List<KitFilteredFaction> FactionFilter { get; set; }
    public List<KitFilteredMap> MapFilter { get; set; }
    public List<KitSkillset> Skillsets { get; set; }
    public List<KitTranslation> Translations { get; set; }
    public List<KitItemModel> Items { get; set; }
    public List<KitUnlockRequirement> UnlockRequirements { get; set; }
    public List<KitEliteBundle> Bundles { get; set; }
    public List<KitDelay> Delays { get; set; }
    public List<KitAccess> Access { get; set; }
    public List<KitFavorite> Favorites { get; set; }

#nullable restore

}