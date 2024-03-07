using Microsoft.EntityFrameworkCore;
using Uncreated.Warfare.Models.Stats.Base;
using Uncreated.Warfare.Models.Stats.Records;

namespace Uncreated.Warfare.Database.Abstractions;
public interface IStatsDbContext : IDbContext
{
    DbSet<DeathRecord> DeathRecords { get; }
    DbSet<DamageRecord> DamageRecords { get; }
    DbSet<AidRecord> AidRecords { get; }
    DbSet<FobRecord> FobRecords { get; }
    DbSet<FobItemRecord> FobItemRecords { get; }
    public static void ConfigureModels(ModelBuilder modelBuilder)
    {
        RelatedPlayerRecord.Map<DeathRecord>(modelBuilder);

        RelatedPlayerRecord.Map<DamageRecord>(modelBuilder);

        InstigatedPlayerRecord.Map<AidRecord>(modelBuilder);

        InstigatedPlayerRecord.Map<FobRecord>(modelBuilder);

        InstigatedPlayerRecord.Map<FobItemRecord>(modelBuilder);

        BasePlayerRecord.Map<FobItemBuilderRecord>(modelBuilder);

        modelBuilder.Entity<FobRecord>()
            .HasMany(x => x.Items)
            .WithOne(x => x.Fob)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<FobItemRecord>()
            .HasMany(x => x.Builders)
            .WithOne(x => x.FobItem)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<DeathRecord?>()
            .HasOne(x => x!.KillShot)
            .WithOne();
    }
}
