using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Uncreated.Warfare.Models.Users;

[Table("moderation_global_ban_whitelist")]
public class GlobalBanWhitelist
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public ulong Steam64 { get; set; }

    [Column("EffectiveTimeUTC")]
    public DateTimeOffset EffectiveTime { get; set; }
}
