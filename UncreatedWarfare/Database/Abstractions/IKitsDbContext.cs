using Microsoft.EntityFrameworkCore;
using Uncreated.Warfare.Models.Factions;
using Uncreated.Warfare.Models.Kits;
using Uncreated.Warfare.Models.Kits.Bundles;
using Uncreated.Warfare.Players;

namespace Uncreated.Warfare.Database.Abstractions;
public interface IKitsDbContext : IDbContext
{
    DbSet<Kit> Kits { get; }
    DbSet<KitAccess> KitAccess { get; }
    DbSet<KitHotkey> KitHotkeys { get; }
    DbSet<KitLayoutTransformation> KitLayoutTransformations { get; }
    DbSet<KitFavorite> KitFavorites { get; }
    DbSet<EliteBundle> EliteBundles { get; }
    public static void ConfigureModels(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Kit>()
            .HasKey(x => x.PrimaryKey);

        modelBuilder.Entity<Kit>()
            .HasMany(x => x.FactionFilter)
            .WithOne(x => x.Kit);
        modelBuilder.Entity<Kit>()
            .HasMany(x => x.MapFilter)
            .WithOne(x => x.Kit);
        modelBuilder.Entity<Kit>()
            .HasMany(x => x.Skillsets)
            .WithOne(x => x.Kit);
        modelBuilder.Entity<Kit>()
            .HasMany(x => x.Translations)
            .WithOne(x => x.Kit);
        modelBuilder.Entity<Kit>()
            .HasMany(x => x.ItemModels)
            .WithOne(x => x.Kit);
        modelBuilder.Entity<Kit>()
            .HasMany(x => x.UnlockRequirementsModels)
            .WithOne(x => x.Kit);

        modelBuilder.Entity<Kit>()
            .HasMany<KitAccess>()
            .WithOne(x => x.Kit);

        modelBuilder.Entity<Kit>()
            .HasMany<KitHotkey>()
            .WithOne(x => x.Kit);

        modelBuilder.Entity<Kit>()
            .HasMany<KitLayoutTransformation>()
            .WithOne(x => x.Kit);
        
        modelBuilder.Entity<KitLayoutTransformation>()
            .HasIndex(x => x.Steam64);

        modelBuilder.Entity<Kit>()
            .HasMany<KitFavorite>()
            .WithOne(x => x.Kit);

        modelBuilder.Entity<KitFavorite>()
            .HasIndex(x => x.Steam64);

        modelBuilder.Entity<Faction>()
            .HasMany<KitFilteredFaction>()
            .WithOne(x => x.Faction);

        modelBuilder.Entity<KitSkillset>()
            .Property(x => x.Skill)
            .HasColumnType(Skillset.SkillSqlEnumType);

        modelBuilder.Entity<Kit>()
            .HasMany(x => x.Bundles)
            .WithOne(x => x.Kit);

        modelBuilder.Entity<EliteBundle>()
            .HasMany(x => x.Kits)
            .WithOne(x => x.Bundle);

        modelBuilder.Entity<KitEliteBundle>()
            .HasKey(x => new { x.KitId, x.BundleId });
        modelBuilder.Entity<KitAccess>()
            .HasKey(x => new { x.KitId, x.Steam64 });
        modelBuilder.Entity<KitFavorite>()
            .HasKey(x => new { x.KitId, x.Steam64 });
        modelBuilder.Entity<KitFilteredFaction>()
            .HasKey(x => new { x.KitId, x.FactionId });
        modelBuilder.Entity<KitFilteredMap>()
            .HasKey(x => new { x.KitId, x.Map });
        modelBuilder.Entity<KitHotkey>()
            .HasKey(x => new { x.KitId, x.Slot, x.Steam64 });
        modelBuilder.Entity<KitTranslation>()
            .HasKey(x => new { x.KitId, x.LanguageId });
    }
}