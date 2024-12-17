using Microsoft.EntityFrameworkCore;
using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Uncreated.Warfare.Models.Users;

#nullable disable

[Table("warfare_user_pending_accout_links")]
[Index(nameof(Token), IsUnique = true)]
[Index(nameof(Steam64))]
[Index(nameof(DiscordId))]
public class SteamDiscordPendingLink
{
    [Key, DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    [Column("pk")]
    public uint Id { get; set; }

    public ulong? Steam64 { get; set; }
    public ulong? DiscordId { get; set; }

    [Required, Column(TypeName = "char(9)")]
    public string Token { get; set; }

    [Column("StartedTimestampUTC")]
    public DateTimeOffset StartedTimestamp { get; set; }

    [Column("ExpiryTimestampUTC")]
    public DateTimeOffset ExpiryTimestamp { get; set; }
}