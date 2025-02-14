using Microsoft.EntityFrameworkCore;
using Uncreated.Warfare.Models.Factions;
using Uncreated.Warfare.Models.Kits;
using Uncreated.Warfare.Models.Kits.Bundles;
using Uncreated.Warfare.Models.Users;
using Uncreated.Warfare.Players.Skillsets;

namespace Uncreated.Warfare.Database.Abstractions;
public interface IKitsDbContext : IDbContext
{
    DbSet<KitModel> Kits { get; }
    DbSet<KitAccess> KitAccess { get; }
    DbSet<KitHotkey> KitHotkeys { get; }
    DbSet<KitLayoutTransformation> KitLayoutTransformations { get; }
    DbSet<KitFavorite> KitFavorites { get; }
    DbSet<EliteBundle> EliteBundles { get; }
    public static void ConfigureModels(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<KitModel>()
            .HasKey(x => x.PrimaryKey);

        modelBuilder.Entity<KitModel>()
            .HasIndex(x => x.Id)
            .IsUnique(true);

        modelBuilder.Entity<KitModel>()
            .HasMany(x => x.FactionFilter)
            .WithOne()
            .HasForeignKey(x => x.KitId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<KitModel>()
            .HasMany(x => x.MapFilter)
            .WithOne()
            .HasForeignKey(x => x.KitId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<KitModel>()
            .HasMany(x => x.Skillsets)
            .WithOne()
            .HasForeignKey(x => x.KitId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<KitModel>()
            .HasMany(x => x.Translations)
            .WithOne()
            .HasForeignKey(x => x.KitId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<KitModel>()
            .HasMany(x => x.Items)
            .WithOne()
            .HasForeignKey(x => x.KitId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<KitModel>()
            .HasMany(x => x.UnlockRequirements)
            .WithOne()
            .HasForeignKey(x => x.KitId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<KitModel>()
            .HasMany(x => x.Access)
            .WithOne(x => x.Kit)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<KitModel>()
            .HasMany<KitHotkey>()
            .WithOne(x => x.Kit)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<KitModel>()
            .HasMany<KitLayoutTransformation>()
            .WithOne()
            .HasForeignKey(x => x.KitId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<WarfareUserData>()
            .HasMany<KitLayoutTransformation>()
            .WithOne(x => x.PlayerData)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<KitModel>()
            .HasMany(x => x.Favorites)
            .WithOne(x => x.Kit)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<WarfareUserData>()
            .HasMany<KitFavorite>()
            .WithOne(x => x.PlayerData)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<WarfareUserData>()
            .HasMany<KitAccess>()
            .WithOne(x => x.PlayerData)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<WarfareUserData>()
            .HasMany<KitHotkey>()
            .WithOne(x => x.PlayerData)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<Faction>()
            .HasMany<KitFilteredFaction>()
            .WithOne()
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<EliteBundle>()
            .HasOne(x => x.Faction)
            .WithMany()
            .OnDelete(DeleteBehavior.SetNull);

        modelBuilder.Entity<KitSkillset>()
            .Property(x => x.Skill)
            .HasColumnType(Skillset.SkillSqlEnumType);

        modelBuilder.Entity<KitModel>()
            .HasMany(x => x.Bundles)
            .WithOne(x => x.Kit)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<Faction>()
            .HasMany<KitModel>()
            .WithOne(x => x.Faction!)
            .IsRequired(false)
            .OnDelete(DeleteBehavior.SetNull);

        modelBuilder.Entity<EliteBundle>()
            .HasMany(x => x.Kits)
            .WithOne(x => x.Bundle)
            .OnDelete(DeleteBehavior.Cascade);

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