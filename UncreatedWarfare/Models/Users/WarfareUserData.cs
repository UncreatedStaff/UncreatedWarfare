using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Net;
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
    public DateTime? FirstJoined { get; set; }

    /// <remarks>UTC</remarks>
    public DateTime? LastJoined { get; set; }
    
    public HWID? LastHWID { get; set; }
    
    public IPAddress? LastIPAddress { get; set; }
}