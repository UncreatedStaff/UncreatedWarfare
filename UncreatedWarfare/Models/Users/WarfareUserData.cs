using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Uncreated.Warfare.Moderation;

namespace Uncreated.Warfare.Models.Users;

[Table(DatabaseInterface.TableUserData)]
public class WarfareUserData
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    [Required]
    [Column(DatabaseInterface.ColumnUserDataSteam64)]
    public ulong Steam64 { get; set; }

    [Required]
    [MaxLength(48)]
    [Column(DatabaseInterface.ColumnUserDataPlayerName)]
    public string PlayerName { get; set; } = null!;

    [Required]
    [MaxLength(30)]
    [Column(DatabaseInterface.ColumnUserDataCharacterName)]
    public string CharacterName { get; set; } = null!;

    [Required]
    [MaxLength(30)]
    [Column(DatabaseInterface.ColumnUserDataNickName)]
    public string NickName { get; set; } = null!;

    [MaxLength(30)]
    [Column(DatabaseInterface.ColumnUserDataDisplayName)]
    public string? DisplayName { get; set; }

    /// <remarks>UTC</remarks>
    [Column(DatabaseInterface.ColumnUserDataFirstJoined)]
    public DateTimeOffset? FirstJoined { get; set; }

    /// <remarks>UTC</remarks>
    [Column(DatabaseInterface.ColumnUserDataLastJoined)]
    public DateTimeOffset? LastJoined { get; set; }

    [DefaultValue(0ul)]
    [Column(DatabaseInterface.ColumnUserDataDiscordId)]
    public ulong DiscordId { get; set; }

    public IList<PlayerHWID> HWIDs { get; set; }
    public IList<PlayerIPAddress> IPAddresses { get; set; }
}