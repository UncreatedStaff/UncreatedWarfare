using System;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Pomelo.EntityFrameworkCore.MySql.Infrastructure;
using Pomelo.EntityFrameworkCore.MySql.Storage;
using Uncreated.Warfare.Database.Abstractions;
using Uncreated.Warfare.Database.Automation;
using Uncreated.Warfare.Models.Factions;
using Uncreated.Warfare.Models.Kits;
using Uncreated.Warfare.Models.Kits.Bundles;
using Uncreated.Warfare.Models.Localization;
using Uncreated.Warfare.Models.Users;

namespace Uncreated.Warfare.Database;
public class WarfareDbContext : DbContext, IFactionDbContext, IUserDataDbContext, ILanguageDbContext, IKitsDbContext
{
    internal static string? ConnStringOverride = null;

    public DbSet<LanguageInfo> Languages => Set<LanguageInfo>();
    public DbSet<LanguagePreferences> LanguagePreferences => Set<LanguagePreferences>();
    public DbSet<WarfareUserData> UserData => Set<WarfareUserData>();
    public DbSet<Faction> Factions => Set<Faction>();
    public DbSet<Kit> Kits => Set<Kit>();
    public DbSet<KitAccess> KitAccess => Set<KitAccess>();
    public DbSet<KitHotkey> KitHotkeys => Set<KitHotkey>();
    public DbSet<KitLayoutTransformation> KitLayoutTransformations => Set<KitLayoutTransformation>();
    public DbSet<KitFavorite> KitFavorites => Set<KitFavorite>();
    public DbSet<EliteBundle> EliteBundles => Set<EliteBundle>();

    /* configure database settings */
    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        string connectionString = ConnStringOverride ?? UCWarfare.Config.SqlConnectionString ?? (UCWarfare.Config.RemoteSQL ?? UCWarfare.Config.SQL).GetConnectionString("UCWarfare", true, true);

        optionsBuilder.UseMySql(connectionString, x => x
            .CharSet(CharSet.Utf8Mb4)
            .CharSetBehavior(CharSetBehavior.AppendToAllColumns));

        optionsBuilder.EnableSensitiveDataLogging();

        IDbContextOptionsBuilderInfrastructure settings = optionsBuilder;
        
        // for some reason default logging completely crashes the server
        CoreOptionsExtension extension = (optionsBuilder.Options.FindExtension<CoreOptionsExtension>() ?? new CoreOptionsExtension()).WithLoggerFactory(new L.UCLoggerFactory());
        settings.AddOrUpdateExtension(extension);
    }

    /* further configure models than what's possible with attributes */
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        ILanguageDbContext.ConfigureModels(modelBuilder);
        IFactionDbContext.ConfigureModels(modelBuilder);
        IUserDataDbContext.ConfigureModels(modelBuilder);
        IKitsDbContext.ConfigureModels(modelBuilder);

        /* Adds preset value converters */
        WarfareDatabaseReflection.ApplyValueConverterConfig(modelBuilder);

        Console.WriteLine("Model created.");
    }
}
