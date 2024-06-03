using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Uncreated.Warfare.Models.Authentication;

[Table("homebase_auth_keys")]
public class HomebaseAuthenticationKey
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int Id { get; set; }

    [Required]
    [Column(TypeName = "char(32)")]
    public string AuthKey { get; set; }

    [StringLength(16)]
    public string Identity { get; set; }
    public byte Region { get; set; }
    public DateTimeOffset LastConnectTime { get; set; }
}
