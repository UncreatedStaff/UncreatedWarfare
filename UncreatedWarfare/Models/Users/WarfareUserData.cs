using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Uncreated.Warfare.API.Permissions;

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

    [Required]
    public PermissionLevel PermissionLevel { get; set; }
    public DateTime? FirstJoined { get; set; }
    public DateTime? LastJoined { get; set; }
}
