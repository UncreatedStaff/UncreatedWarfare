using Microsoft.EntityFrameworkCore;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations.Schema;
using Uncreated.Warfare.Models.GameData;
using Uncreated.Warfare.Models.Users;
using UnityEngine;

namespace Uncreated.Warfare.Models.Stats.Base;
public abstract class RelatedPlayerRecord : InstigatedPlayerRecord
{
    private Vector3 _relatedPlayerPosition;
    private bool _relatedPlayerPosHasValue;

    [DefaultValue(null)]
    [ForeignKey(nameof(RelatedPlayerData))]
    [Column("RelatedPlayer")]
    public ulong? RelatedPlayer { get; set; }
    public WarfareUserData? RelatedPlayerData { get; set; }

    [ForeignKey(nameof(RelatedPlayerSession))]
    [Column("RelatedPlayerSession")]
    public ulong? RelatedPlayerSessionId { get; set; }
    public SessionRecord? RelatedPlayerSession { get; set; }

    [NotMapped]
    public Vector3? RelatedPlayerPosition
    {
        get => _relatedPlayerPosHasValue ? _relatedPlayerPosition : null;
        set
        {
            if (value.HasValue)
            {
                _relatedPlayerPosition = value.Value;
                _relatedPlayerPosHasValue = true;
            }
            else
                _relatedPlayerPosHasValue = false;
        }
    }

    public float? RelatedPlayerPositionX
    {
        get => _relatedPlayerPosHasValue ? _relatedPlayerPosition.x : null;
        set => SetPos(0, value);
    }
    public float? RelatedPlayerPositionY
    {
        get => _relatedPlayerPosHasValue ? _relatedPlayerPosition.y : null;
        set => SetPos(1, value);
    }
    public float? RelatedPlayerPositionZ
    {
        get => _relatedPlayerPosHasValue ? _relatedPlayerPosition.z : null;
        set => SetPos(2, value);
    }

    private void SetPos(int index, float? value)
    {
        if (value.HasValue)
        {
            _relatedPlayerPosition[index] = value.Value;
            _relatedPlayerPosHasValue = true;
        }
        else
            _relatedPlayerPosHasValue = false;
    }

    public new static void Map<TEntity>(ModelBuilder modelBuilder) where TEntity : RelatedPlayerRecord, new()
    {
        InstigatedPlayerRecord.Map<TEntity>(modelBuilder);

        modelBuilder.Entity<WarfareUserData>()
            .HasMany<TEntity>()
            .WithOne(x => x.RelatedPlayerData!)
            .OnDelete(DeleteBehavior.NoAction);

        modelBuilder.Entity<SessionRecord>()
            .HasMany<TEntity>()
            .WithOne(x => x.RelatedPlayerSession!)
            .OnDelete(DeleteBehavior.NoAction);
    }
}
