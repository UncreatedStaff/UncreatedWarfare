using Microsoft.EntityFrameworkCore;
using Uncreated.Warfare.Models.Buildables;
using Uncreated.Warfare.Models.Seasons;

namespace Uncreated.Warfare.Database.Abstractions;
public interface IBuildablesDbContext : ISeasonsDbContext
{
    DbSet<BuildableSave> Saves { get; }

    public new static void ConfigureModels(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<BuildableStorageItem>()
            .HasKey(x => new { x.Save, x.PositionX, x.PositionY });

        modelBuilder.Entity<BuildableInstanceId>()
            .HasKey(x => new { x.Save, x.RegionId });

        modelBuilder.Entity<BuildableSave>()
            .HasMany(x => x.Items)
            .WithOne(x => x.Save!)
            .IsRequired(false)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<BuildableSave>()
            .HasMany(x => x.InstanceIds)
            .WithOne(x => x.Save!)
            .IsRequired(false)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<BuildableSave>()
            .HasOne(x => x.DisplayData)
            .WithOne(x => x!.Save!)
            .IsRequired(false)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<MapData>()
            .HasMany<BuildableSave>()
            .WithOne(x => x.Map!)
            .IsRequired(false)
            .OnDelete(DeleteBehavior.SetNull);
    }
}
