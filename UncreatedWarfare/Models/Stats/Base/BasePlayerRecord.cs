using Microsoft.EntityFrameworkCore;
using System;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Uncreated.Warfare.Database.Automation;
using Uncreated.Warfare.Models.GameData;
using Uncreated.Warfare.Models.Users;

namespace Uncreated.Warfare.Models.Stats.Base;

[NotMapped]
public abstract class BasePlayerRecord
{
    private Vector3 _position;

    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public ulong Id { get; set; }

    [DefaultValue((byte)0)]
    [Index]
    public byte Team { get; set; }

    [ForeignKey(nameof(PlayerData))]
    [Column("Steam64")]
    public ulong? Steam64 { get; set; }

    public WarfareUserData? PlayerData { get; set; }

    [ForeignKey(nameof(Session))]
    [Column("Session")]
    public ulong? SessionId { get; set; }

    public SessionRecord? Session { get; set; }

    [NotMapped]
    public Vector3 Position
    {
        get => _position;
        set => _position = value;
    }

    public float PositionX
    {
        get => _position.x;
        set => _position.x = value;
    }
    public float PositionY
    {
        get => _position.y;
        set => _position.y = value;
    }
    public float PositionZ
    {
        get => _position.z;
        set => _position.z = value;
    }

    [StringLength(255)]
    [Required]
    public string NearestLocation { get; set; }

    [Column("TimestampUTC")]
    public DateTimeOffset Timestamp { get; set; }

    public static void Map<TEntity>(ModelBuilder modelBuilder) where TEntity : BasePlayerRecord, new()
    {
        modelBuilder.Entity<WarfareUserData>()
            .HasMany<TEntity>()
            .WithOne(x => x.PlayerData!)
            .OnDelete(DeleteBehavior.NoAction);

        modelBuilder.Entity<SessionRecord>()
            .HasMany<TEntity>()
            .WithOne(x => x.Session!)
            .OnDelete(DeleteBehavior.NoAction);
    }
}
