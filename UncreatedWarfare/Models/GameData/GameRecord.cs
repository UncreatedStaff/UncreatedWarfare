using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Uncreated.Warfare.Gamemodes;
using Uncreated.Warfare.Models.Factions;

namespace Uncreated.Warfare.Models.GameData;

[Table("stats_games")]
public class GameRecord
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public ulong GameId { get; set; }
    public int Season { get; set; }
    public int Map { get; set; }
    public DateTimeOffset StartTimestamp { get; set; }
    public DateTimeOffset? EndTimestamp { get; set; }

    [Required]
    [DefaultValue(nameof(GamemodeType.Undefined))]
    public GamemodeType Gamemode { get; set; }

    [ForeignKey(nameof(WinnerFaction))]
    [Column("Winner")]
    public uint? WinnerFactionId { get; set; }
    public Faction? WinnerFaction { get; set; }

    public IList<SessionRecord> Sessions { get; set; }
}
