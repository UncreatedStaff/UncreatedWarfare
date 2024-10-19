using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Uncreated.Warfare.Models.Users;

[Table("user_permissions"), Index(nameof(Steam64))]
public class Permission
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    [Column("pk")]
    public uint PrimaryKey { get; set; }

    public ulong Steam64 { get; set; }

    public bool IsGroup { get; set; }

    [Required]
    [StringLength(128)]
    public string PermissionOrGroup { get; set; }
}
