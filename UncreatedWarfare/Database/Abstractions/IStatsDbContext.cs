using Microsoft.EntityFrameworkCore;
using Uncreated.Warfare.Models.Stats.Base;
using Uncreated.Warfare.Models.Stats.Records;

namespace Uncreated.Warfare.Database.Abstractions;
public interface IStatsDbContext
{
    DbSet<DeathRecord> DeathRecords { get; }
    DbSet<DamageRecord> DamageRecords { get; }
    DbSet<AidRecord> AidRecords { get; }
    public static void ConfigureModels(ModelBuilder modelBuilder)
    {
        RelatedPlayerRecord.Map<DeathRecord>(modelBuilder);

        RelatedPlayerRecord.Map<DamageRecord>(modelBuilder);

        InstigatedPlayerRecord.Map<AidRecord>(modelBuilder);
    }
}
