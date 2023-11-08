using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using OpenMod.EntityFrameworkCore;
using OpenMod.EntityFrameworkCore.Configurator;
using OpenMod.EntityFrameworkCore.MySql;
using System;
using Uncreated.Warfare.API.Items;
using Uncreated.Warfare.API.Permissions;
using Uncreated.Warfare.Database.ValueConverters;
using Uncreated.Warfare.Models.Localization;
using Uncreated.Warfare.Models.Teams;
using Uncreated.Warfare.Models.Users;

namespace Uncreated.Warfare.Database;
public class WarfareDbContext : OpenModDbContext<WarfareDbContext>
{
    public DbSet<Faction> Factions => Set<Faction>();
    public DbSet<WarfareUserData> UserData => Set<WarfareUserData>();
    public DbSet<LanguageInfo> Languages => Set<LanguageInfo>();
    public DbSet<LanguagePreferences> LanguagePreferences => Set<LanguagePreferences>();

    public WarfareDbContext(IServiceProvider serviceProvider) : base(serviceProvider) { }
    public WarfareDbContext(IDbContextConfigurator configurator, IServiceProvider serviceProvider)
        : base(configurator, serviceProvider) { }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        /* FACTIONS */
        modelBuilder.WithAssetReferenceConverter<FactionAsset>(x => x.Asset);

        modelBuilder.WithEnumConverter<FactionAsset, ItemRedirect>(x => x.Redirect);

        modelBuilder.Entity<Faction>()
            .HasMany(x => x.Assets)
            .WithOne(x => x.Faction);

        modelBuilder.Entity<Faction>()
            .HasMany(x => x.Translations)
            .WithOne(x => x.Faction);

        modelBuilder.Entity<FactionLocalization>()
            .HasOne(x => x.Language);

        modelBuilder.Entity<FactionAsset>()
            .Property(x => x.Redirect)
            .HasConversion(new EnumToStringConverter<ItemRedirect>())
            .HasColumnType(DatabaseHelper.EnumType(ItemRedirect.None));

        /* LANGUAGE INFO */
        modelBuilder.Entity<LanguageInfo>()
            .Property(x => x.HasTranslationSupport)
            .HasDefaultValue(false);

        modelBuilder.Entity<LanguageInfo>()
            .Property(x => x.RequiresIMGUI)
            .HasDefaultValue(false);

        modelBuilder.Entity<LanguageInfo>()
            .HasMany(x => x.Aliases)
            .WithOne(x => x.Language)
            .IsRequired(true);

        modelBuilder.Entity<LanguageInfo>()
            .HasMany(x => x.Contributors)
            .WithOne(x => x.Language)
            .IsRequired(true);

        modelBuilder.Entity<LanguageInfo>()
            .HasMany(x => x.SupportedCultures)
            .WithOne(x => x.Language)
            .IsRequired(true);

        modelBuilder.Entity<LanguagePreferences>()
            .Property(x => x.UseCultureForCommandInput)
            .HasDefaultValue(false);

        /* USER DATA */
        modelBuilder.WithEnumConverter<WarfareUserData, PermissionLevel>(x => x.PermissionLevel, PermissionLevel.Member);
    }
}

public class WarfareDbContextFactory : OpenModMySqlDbContextFactory<WarfareDbContext> { }