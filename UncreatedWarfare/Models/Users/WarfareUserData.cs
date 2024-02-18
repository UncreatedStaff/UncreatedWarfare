using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Uncreated.Warfare.Moderation;
using Uncreated.Warfare.Permissions;

namespace Uncreated.Warfare.Models.Users;

[Table("users")]
public class WarfareUserData
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    [Required]
    public ulong Steam64 { get; set; }

    [Required]
    [MaxLength(48)]
    public string PlayerName { get; set; } = null!;

    [Required]
    [MaxLength(30)]
    public string CharacterName { get; set; } = null!;

    [Required]
    [MaxLength(30)]
    public string NickName { get; set; } = null!;

    [MaxLength(30)]
    public string? DisplayName { get; set; }
    
    [Required]
    public PermissionLevel PermissionLevel { get; set; } = PermissionLevel.Member;

    /// <remarks>UTC</remarks>
    public DateTimeOffset? FirstJoined { get; set; }

    /// <remarks>UTC</remarks>
    public DateTimeOffset? LastJoined { get; set; }

    [DefaultValue(0ul)]
    public ulong DiscordId { get; set; }

    public IList<PlayerHWID> HWIDs { get; set; }
    public IList<PlayerIPAddress> IPAddresses { get; set; }
}