﻿using Microsoft.EntityFrameworkCore;
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
public class WarfareDbContext : DbContext, IUserDataDbContext, ILanguageDbContext, IKitsDbContext, IStatsDbContext, IGameDataDbContext, IBuildablesDbContext, IWhitelistDbContext
{
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

    public WarfareDbContext(ILogger<WarfareDbContext> logger, DbContextOptions<WarfareDbContext> options) : base(options)
    {
        _logger = logger;
    }

    /// <summary>
    /// Used to auto-fill the 'options' parameters.
    /// </summary>
    internal static DbContextOptions<WarfareDbContext> GetOptions(IConfiguration sysConfig)
    {
        DbContextOptionsBuilder<WarfareDbContext> builder = new DbContextOptionsBuilder<WarfareDbContext>();
        Configure(builder, sysConfig);
        return builder.Options;
    }

    /* configure database settings */
    private static void Configure(DbContextOptionsBuilder optionsBuilder, IConfiguration sysConfig)
    {
        IConfiguration databaseSection = sysConfig.GetSection("database");

        string? connectionStringType = databaseSection["connection_string_name"];

        if (string.IsNullOrWhiteSpace(connectionStringType))
            connectionStringType = "warfare-db";

        string? connectionString = sysConfig.GetConnectionString(connectionStringType);

        if (string.IsNullOrWhiteSpace(connectionString))
            throw new InvalidOperationException($"Missing connection string: \"{connectionStringType}\".");

        bool sensitiveDataLogging = false;
        if (databaseSection.GetValue<bool>("sensitive_data_logging"))
        {
            sensitiveDataLogging = true;
            L.Log("Sensitive data logging is enabled.");
        }

        optionsBuilder.UseMySql(connectionString, x => x
            .CharSet(CharSet.Utf8Mb4)
            .CharSetBehavior(CharSetBehavior.AppendToAllColumns));

        if (sensitiveDataLogging)
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
}
#pragma warning restore CS8644