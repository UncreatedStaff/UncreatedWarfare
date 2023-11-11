using Microsoft.EntityFrameworkCore;
using Uncreated.Warfare.Models.Localization;

namespace Uncreated.Warfare.Database.Abstractions;
public interface ILanguageDbContext : IDbContext
{
    DbSet<Models.Localization.LanguageInfo> Languages { get; }
    DbSet<LanguagePreferences> LanguagePreferences { get; }

    public static void ConfigureModels(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Models.Localization.LanguageInfo>()
            .Property(x => x.HasTranslationSupport)
            .HasDefaultValue(false);

        modelBuilder.Entity<Models.Localization.LanguageInfo>()
            .Property(x => x.RequiresIMGUI)
            .HasDefaultValue(false);

        modelBuilder.Entity<Models.Localization.LanguageInfo>()
            .HasMany(x => x.Aliases)
            .WithOne(x => x.Language)
            .IsRequired(true);

        modelBuilder.Entity<Models.Localization.LanguageInfo>()
            .HasMany(x => x.Contributors)
            .WithOne(x => x.Language)
            .IsRequired(true);

        modelBuilder.Entity<Models.Localization.LanguageInfo>()
            .HasMany(x => x.SupportedCultures)
            .WithOne(x => x.Language)
            .IsRequired(true);

        modelBuilder.Entity<LanguagePreferences>()
            .Property(x => x.UseCultureForCommandInput)
            .HasDefaultValue(false);
    }
}
