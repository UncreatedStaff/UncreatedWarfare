using Microsoft.EntityFrameworkCore;
using OpenMod.EntityFrameworkCore;
using OpenMod.EntityFrameworkCore.Configurator;
using System;
using Uncreated.Warfare.Automation;
using Uncreated.Warfare.Models.Localization;

namespace Uncreated.Warfare.Database.DbContexts;

/// <summary>
/// <see cref="LanguageInfo"/>
/// </summary>
[WarfareDatabaseContext]
public class LanguageInfoDbContext : OpenModDbContext<LanguageInfoDbContext>
{
    public LanguageInfoDbContext(IServiceProvider serviceProvider) : base(serviceProvider) { }
    public LanguageInfoDbContext(IDbContextConfigurator configurator, IServiceProvider serviceProvider)
        : base(configurator, serviceProvider) { }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<LanguageInfo>()
            .Property(x => x.HasTranslationSupport)
            .HasDefaultValue(false);

        modelBuilder.Entity<LanguageInfo>()
            .Property(x => x.RequiresIMGUI)
            .HasDefaultValue(false);

        modelBuilder.Entity<LanguagePreferences>()
            .Property(x => x.UseCultureForCommandInput)
            .HasDefaultValue(false);
    }
}
