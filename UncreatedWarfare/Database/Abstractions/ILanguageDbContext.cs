using Microsoft.EntityFrameworkCore;
using Uncreated.Warfare.Models.Localization;
using Uncreated.Warfare.Models.Users;

namespace Uncreated.Warfare.Database.Abstractions;
public interface ILanguageDbContext : IDbContext
{
    DbSet<LanguageInfo> Languages { get; }
    DbSet<LanguagePreferences> LanguagePreferences { get; }

    public static void ConfigureModels(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<LanguageInfo>()
            .Property(x => x.HasTranslationSupport)
            .HasDefaultValue(false);

        modelBuilder.Entity<LanguageInfo>()
            .Property(x => x.RequiresIMGUI)
            .HasDefaultValue(false);

        modelBuilder.Entity<LanguageInfo>()
            .HasMany(x => x.Aliases)
            .WithOne(x => x.Language)
            .IsRequired(true)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<LanguageInfo>()
            .HasMany(x => x.Contributors)
            .WithOne(x => x.Language)
            .IsRequired(true)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<LanguageInfo>()
            .HasMany(x => x.SupportedCultures)
            .WithOne(x => x.Language)
            .IsRequired(true)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<WarfareUserData>()
            .HasMany<LanguageContributor>()
            .WithOne(x => x.ContributorData)
            .OnDelete(DeleteBehavior.NoAction);

        modelBuilder.Entity<WarfareUserData>()
            .HasMany<LanguagePreferences>()
            .WithOne(x => x.PlayerData)
            .OnDelete(DeleteBehavior.NoAction);

        modelBuilder.Entity<LanguagePreferences>()
            .Property(x => x.UseCultureForCommandInput)
            .HasDefaultValue(false);
    }
}
