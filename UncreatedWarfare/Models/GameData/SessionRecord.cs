using System;
using System.ComponentModel;
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
    public int Team { get; set; }

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

    [Column("SquadName")]
    [StringLength(32)]
    [DefaultValue(null)]
    public string? SquadName { get; set; }
    
    [ForeignKey(nameof(SquadLeaderData))]
    [Column("SquadLeader")]
    [DefaultValue(null)]
    public ulong? SquadLeader { get; set; }

    [DefaultValue(null)]
    public WarfareUserData? SquadLeaderData { get; set; }

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
    public KitModel? Kit { get; set; }

    [StringLength(25)]
    public string? KitName { get; set; }

    public bool StartedGame { get; set; }
    public bool FinishedGame { get; set; }
    public bool UnexpectedTermination { get; set; }


    [NotMapped]
    internal int EventCount;

    public void MarkDirty() => ++EventCount;
}
