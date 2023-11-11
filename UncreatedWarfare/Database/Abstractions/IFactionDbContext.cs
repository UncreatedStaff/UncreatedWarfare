using Microsoft.EntityFrameworkCore;
using Uncreated.Warfare.Models.Factions;

namespace Uncreated.Warfare.Database.Abstractions;
public interface IFactionDbContext : IDbContext
{
    DbSet<Faction> Factions { get; }

    public static void ConfigureModels(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Faction>()
            .HasMany(x => x.Assets)
            .WithOne(x => x.Faction);

        modelBuilder.Entity<Faction>()
            .HasMany(x => x.Translations)
            .WithOne(x => x.Faction);

        modelBuilder.Entity<FactionLocalization>()
            .HasOne(x => x.Language);
    }
}
