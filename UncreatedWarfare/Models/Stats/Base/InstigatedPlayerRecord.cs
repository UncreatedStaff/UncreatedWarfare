using Microsoft.EntityFrameworkCore;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations.Schema;
using Uncreated.Warfare.Models.GameData;
using Uncreated.Warfare.Models.Users;
using UnityEngine;

namespace Uncreated.Warfare.Models.Stats.Base;
public abstract class InstigatedPlayerRecord : BasePlayerRecord
{
    private Vector3 _instigatorPosition;
    private bool _instigatorPosHasValue;

    [DefaultValue(null)]
    [ForeignKey(nameof(InstigatorData))]
    [Column("Instigator")]
    public ulong? Instigator { get; set; }
    public WarfareUserData? InstigatorData { get; set; }

    [ForeignKey(nameof(InstigatorSession))]
    [Column("InstigatorSession")]
    public ulong? InstigatorSessionId { get; set; }
    public SessionRecord? InstigatorSession { get; set; }

    [NotMapped]
    public Vector3? InstigatorPosition
    {
        get => _instigatorPosHasValue ? _instigatorPosition : null;
        set
        {
            if (value.HasValue)
            {
                _instigatorPosition = value.Value;
                _instigatorPosHasValue = true;
            }
            else
                _instigatorPosHasValue = false;
        }
    }

    public float? InstigatorPositionX
    {
        get => _instigatorPosHasValue ? _instigatorPosition.x : null;
        set => SetPos(0, value);
    }
    public float? InstigatorPositionY
    {
        get => _instigatorPosHasValue ? _instigatorPosition.y : null;
        set => SetPos(1, value);
    }
    public float? InstigatorPositionZ
    {
        get => _instigatorPosHasValue ? _instigatorPosition.z : null;
        set => SetPos(2, value);
    }

    private void SetPos(int index, float? value)
    {
        if (value.HasValue)
        {
            _instigatorPosition[index] = value.Value;
            _instigatorPosHasValue = true;
        }
        else
            _instigatorPosHasValue = false;
    }

    public new static void Map<TEntity>(ModelBuilder modelBuilder) where TEntity : InstigatedPlayerRecord, new()
    {
        BasePlayerRecord.Map<TEntity>(modelBuilder);

        modelBuilder.Entity<TEntity>()
            .HasOne(x => x.InstigatorData)
            .WithMany()
            .IsRequired(false)
            .OnDelete(DeleteBehavior.NoAction);

        modelBuilder.Entity<TEntity>()
            .HasOne(x => x.InstigatorSession)
            .WithMany()
            .IsRequired(false)
            .OnDelete(DeleteBehavior.NoAction);
    }
}
