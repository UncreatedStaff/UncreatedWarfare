using Microsoft.EntityFrameworkCore;
using Uncreated.Warfare.Models.Kits;
using Uncreated.Warfare.Players;

namespace Uncreated.Warfare.Database.Abstractions;
public interface IKitsDbContext : IDbContext
{
    DbSet<Kit> Kits { get; }
    DbSet<KitAccess> KitAccess { get; }
    DbSet<KitHotkey> KitHotkeys { get; }
    DbSet<KitLayoutTransformation> KitLayoutTransformations { get; }
    public static void ConfigureModels(ModelBuilder modelBuilder)
    {
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

        modelBuilder.Entity<KitAccess>()
            .HasOne(x => x.Kit);
        modelBuilder.Entity<KitAccess>()
            .HasIndex(x => x.Steam64);

        modelBuilder.Entity<KitHotkey>()
            .HasOne(x => x.Kit);
        modelBuilder.Entity<KitHotkey>()
            .HasIndex(x => x.Steam64);

        modelBuilder.Entity<KitLayoutTransformation>()
            .HasOne(x => x.Kit);
        modelBuilder.Entity<KitLayoutTransformation>()
            .HasIndex(x => x.Steam64);

        modelBuilder.Entity<KitFilteredFaction>()
            .HasOne(x => x.Faction);

        modelBuilder.Entity<KitSkillset>()
            .Property(x => x.Skill)
            .HasColumnType(Skillset.SkillSqlEnumType);
    }
}
