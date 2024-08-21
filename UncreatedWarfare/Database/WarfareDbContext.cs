using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.Configuration;
using Pomelo.EntityFrameworkCore.MySql.Infrastructure;
using Pomelo.EntityFrameworkCore.MySql.Storage;
using System;
using Uncreated.Warfare.Database.Abstractions;
using Uncreated.Warfare.Database.Automation;
using Uncreated.Warfare.Logging;
using Uncreated.Warfare.Models;
using Uncreated.Warfare.Models.Authentication;
using Uncreated.Warfare.Models.Buildables;
using Uncreated.Warfare.Models.Factions;
using Uncreated.Warfare.Models.GameData;
using Uncreated.Warfare.Models.Kits;
using Uncreated.Warfare.Models.Kits.Bundles;
using Uncreated.Warfare.Models.Localization;
using Uncreated.Warfare.Models.Seasons;
using Uncreated.Warfare.Models.Stats.Records;
using Uncreated.Warfare.Models.Users;
using Uncreated.Warfare.Moderation;
using Uncreated.Warfare.Services;

namespace Uncreated.Warfare.Database;
#pragma warning disable CS8644
public class WarfareDbContext : DbContext, IUserDataDbContext, ILanguageDbContext, IKitsDbContext, IStatsDbContext, IGameDataDbContext, IBuildablesDbContext, IWhitelistDbContext, IHostedService
{
    private readonly string _connectionString;
    private readonly bool _sensitiveDataLogging;

    private readonly ILogger<WarfareDbContext> _logger;
    
    public DbSet<LanguageInfo> Languages => Set<LanguageInfo>();
    public DbSet<LanguagePreferences> LanguagePreferences => Set<LanguagePreferences>();
    public DbSet<WarfareUserData> UserData => Set<WarfareUserData>();
    public DbSet<PlayerIPAddress> IPAddresses => Set<PlayerIPAddress>();
    public DbSet<PlayerHWID> HWIDs => Set<PlayerHWID>();
    public DbSet<Faction> Factions => Set<Faction>();
    public DbSet<Kit> Kits => Set<Kit>();
    public DbSet<KitAccess> KitAccess => Set<KitAccess>();
    public DbSet<KitHotkey> KitHotkeys => Set<KitHotkey>();
    public DbSet<KitLayoutTransformation> KitLayoutTransformations => Set<KitLayoutTransformation>();
    public DbSet<KitFavorite> KitFavorites => Set<KitFavorite>();
    public DbSet<EliteBundle> EliteBundles => Set<EliteBundle>();
    public DbSet<GameRecord> Games => Set<GameRecord>();
    public DbSet<SessionRecord> Sessions => Set<SessionRecord>();
    public DbSet<MapData> Maps => Set<MapData>();
    public DbSet<SeasonData> Seasons => Set<SeasonData>();
    public DbSet<FobRecord> FobRecords => Set<FobRecord>();
    public DbSet<FobItemRecord> FobItemRecords => Set<FobItemRecord>();
    public DbSet<DeathRecord> DeathRecords => Set<DeathRecord>();
    public DbSet<DamageRecord> DamageRecords => Set<DamageRecord>();
    public DbSet<AidRecord> AidRecords => Set<AidRecord>();
    public DbSet<Permission> Permissions => Set<Permission>();
    public DbSet<BuildableSave> Saves => Set<BuildableSave>();
    public DbSet<ItemWhitelist> Whitelists => Set<ItemWhitelist>();

    public WarfareDbContext(IConfiguration sysConfig, ILogger<WarfareDbContext> logger)
    {
        _logger = logger;

        IConfiguration databaseSection = sysConfig.GetSection("database");

        string? connectionStringType = databaseSection["connection_string_name"];

        if (string.IsNullOrWhiteSpace(connectionStringType))
            connectionStringType = "warfare-db";

        string? connectionString = sysConfig.GetConnectionString(connectionStringType);

        if (string.IsNullOrWhiteSpace(connectionString))
            throw new InvalidOperationException($"Missing connection string: \"{connectionStringType}\".");

        if (databaseSection.GetValue<bool>("sensitive_data_logging"))
        {
            _logger.LogInformation("Sensitive data logging is enabled.");
            _sensitiveDataLogging = true;
        }

        _connectionString = connectionString;
    }

    /* configure database settings */
    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        optionsBuilder.UseMySql(_connectionString, x => x
            .CharSet(CharSet.Utf8Mb4)
            .CharSetBehavior(CharSetBehavior.AppendToAllColumns));

        if (_sensitiveDataLogging)
        {
            optionsBuilder.EnableSensitiveDataLogging();
        }

        IDbContextOptionsBuilderInfrastructure settings = optionsBuilder;
        
        // for some reason default logging completely crashes the server
        CoreOptionsExtension extension = (
                optionsBuilder.Options.FindExtension<CoreOptionsExtension>() ?? new CoreOptionsExtension()
            ).WithLoggerFactory(new L.UCLoggerFactory { DebugLogging = false });

        settings.AddOrUpdateExtension(extension);
    }

    /* further configure models than what's possible with attributes */
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        ILanguageDbContext.ConfigureModels(modelBuilder);
        IFactionDbContext.ConfigureModels(modelBuilder);
        IBuildablesDbContext.ConfigureModels(modelBuilder);
        IUserDataDbContext.ConfigureModels(modelBuilder);
        IKitsDbContext.ConfigureModels(modelBuilder);
        IStatsDbContext.ConfigureModels(modelBuilder);
        ISeasonsDbContext.ConfigureModels(modelBuilder);
        IGameDataDbContext.ConfigureModels(modelBuilder);
        IWhitelistDbContext.ConfigureModels(modelBuilder);

        modelBuilder.Entity<HomebaseAuthenticationKey>();

        /* Adds preset value converters */
        WarfareDatabaseReflection.ApplyValueConverterConfig(modelBuilder, _logger);

        _logger.LogInformation("Model created.");
    }

    async UniTask IHostedService.StartAsync(CancellationToken token)
    {
        try
        {
            await Database.MigrateAsync(token);
        }
        finally
        {
            await DisposeAsync();
        }
    }

    UniTask IHostedService.StopAsync(CancellationToken token)
    {
        return DisposeAsync().AsUniTask();
    }
}
#pragma warning restore CS8644