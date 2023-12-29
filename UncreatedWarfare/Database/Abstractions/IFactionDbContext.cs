using Microsoft.EntityFrameworkCore;
using Uncreated.Warfare.Models.Factions;
using Uncreated.Warfare.Models.Localization;

namespace Uncreated.Warfare.Database.Abstractions;
public interface IFactionDbContext : IDbContext
{
    DbSet<Faction> Factions { get; }

    public static void ConfigureModels(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Faction>()
            .HasMany(x => x.Assets)
            .WithOne(x => x.Faction)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<Faction>()
            .HasMany(x => x.Translations)
            .WithOne(x => x.Faction)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<LanguageInfo>()
            .HasMany<FactionLocalization>()
            .WithOne(x => x.Language)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<Faction>()
            .HasOne(x => x.UnarmedKit!)
            .WithMany()
            .OnDelete(DeleteBehavior.SetNull);
    }
}