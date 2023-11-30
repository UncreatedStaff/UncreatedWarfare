using Microsoft.EntityFrameworkCore;
using Uncreated.Warfare.Models.Seasons;

namespace Uncreated.Warfare.Database.Abstractions;
public interface ISeasonsDbContext
{
    DbSet<MapData> Maps { get; }
    DbSet<SeasonData> Seasons { get; }
    public static void ConfigureModels(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<MapData>()
            .HasMany(x => x.Dependencies)
            .WithOne(x => x.Map)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<SeasonData>()
            .HasMany(x => x.Maps)
            .WithOne(x => x.SeasonReleased)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<MapWorkshopDependency>()
            .HasOne(x => x.Map)
            .WithMany(x => x.Dependencies);

        modelBuilder.Entity<MapWorkshopDependency>()
            .HasKey(x => new { x.MapId, x.WorkshopId });
    }
}
