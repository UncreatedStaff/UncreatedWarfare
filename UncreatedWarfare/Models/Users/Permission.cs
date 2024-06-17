using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Uncreated.Warfare.Database.Automation;

namespace Uncreated.Warfare.Models.Users;

[Table("user_permissions")]
public class Permission
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    [Column("pk")]
    public uint PrimaryKey { get; set; }

    [Index]
    public ulong Steam64 { get; set; }

    public bool IsGroup { get; set; }

    [Required]
    [StringLength(128)]
    public string PermissionOrGroup { get; set; }
}
