using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Uncreated.Warfare.Models.Factions;
using Uncreated.Warfare.Models.Kits;
using Uncreated.Warfare.Models.Seasons;
using Uncreated.Warfare.Models.Users;

namespace Uncreated.Warfare.Models.GameData;

[Table("stats_sessions")]
public class SessionRecord
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public ulong SessionId { get; set; }

    [Required]
    [ForeignKey(nameof(PlayerData))]
    [Column("Steam64")]
    public ulong Steam64 { get; set; }

    [Required]
    public WarfareUserData PlayerData { get; set; }

    [Required]
    [ForeignKey(nameof(Game))]
    [Column("Game")]
    public ulong GameId { get; set; }

    [Required]
    public GameRecord Game { get; set; }

    [Required]
    [ForeignKey(nameof(Season))]
    [Column("Season")]
    public int SeasonId { get; set; }

    [Required]
    public SeasonData Season { get; set; }

    [Required]
    [ForeignKey(nameof(Map))]
    [Column("Map")]
    public int MapId { get; set; }

    [Required]
    public MapData Map { get; set; }
    public byte Team { get; set; }

    [Column("StartedTimestampUTC")]
    public DateTimeOffset StartedTimestamp { get; set; }

    [Column("EndedTimestampUTC")]
    public DateTimeOffset? EndedTimestamp { get; set; }

    public double LengthSeconds
    {
        get => EndedTimestamp.HasValue ? (EndedTimestamp.Value - StartedTimestamp).TotalSeconds : 0d;
        // ReSharper disable once ValueParameterNotUsed
        set { }
    }

    [ForeignKey(nameof(PreviousSession))]
    [Column("PreviousSession")]
    public ulong? PreviousSessionId { get; set; }
    public SessionRecord? PreviousSession { get; set; }

    [ForeignKey(nameof(NextSession))]
    [Column("NextSession")]
    public ulong? NextSessionId { get; set; }
    public SessionRecord? NextSession { get; set; }

    [ForeignKey(nameof(Faction))]
    [Column("Faction")]
    public uint? FactionId { get; set; }
    public Faction? Faction { get; set; }

    [ForeignKey(nameof(Kit))]
    [Column("Kit")]
    public uint? KitId { get; set; }
    public Kit? Kit { get; set; }

    public bool FinishedGame { get; set; }
    public bool UnexpectedTermination { get; set; }
}
